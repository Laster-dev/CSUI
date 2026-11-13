using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CSUI.Previewer
{
    internal static class PreviewIpcServer
    {
        private const byte MsgRenderXaml = 1;
        private const byte MsgHitTest = 3;
        private const byte MsgShutdown = 255;

        private const byte RespFrame = 1;
        private const byte RespError = 2;
        private const byte RespHitTest = 3;

        private static Border s_lastHostBorder;
        private static double s_lastScale = 1.0;

        public static int Run()
        {
            // Ensure Application & Dispatcher exist on this STA thread for resource lookup / Aero2 styles
            if (Application.Current == null)
            {
                _ = new Application();
            }

            Dispatcher uiDispatcher = Dispatcher.CurrentDispatcher;
            using Stream stdin = Console.OpenStandardInput();
            using Stream stdout = Console.OpenStandardOutput();
            using var reader = new BinaryReader(stdin);
            using var writer = new BinaryWriter(stdout);
            object writeLock = new object();

            var readerThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        int msgType = stdin.ReadByte();
                        if (msgType < 0 || msgType == MsgShutdown)
                        {
                            uiDispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
                            break;
                        }

                        if (msgType == MsgRenderXaml)
                        {
                            int requestId = reader.ReadInt32();
                            double dpiScale = reader.ReadDouble();
                            int maxWidth = reader.ReadInt32();
                            int maxHeight = reader.ReadInt32();
                            string xaml = reader.ReadString();

                            uiDispatcher.Invoke(() =>
                            {
                                RenderAndRespond(requestId, dpiScale, maxWidth, maxHeight, xaml, writer, writeLock);
                            });
                        }
                        else if (msgType == MsgHitTest)
                        {
                            int requestId = reader.ReadInt32();
                            double x = reader.ReadDouble();
                            double y = reader.ReadDouble();

                            uiDispatcher.Invoke(() =>
                            {
                                HitTestAndRespond(requestId, x, y, writer, writeLock);
                            });
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    uiDispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
                }
                catch
                {
                    uiDispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
                }
            })
            {
                IsBackground = true,
                Name = "CSUI.Previewer.IpcReader"
            };

            readerThread.Start();
            Dispatcher.Run();
            return 0;
        }

        private static void RenderAndRespond(
            int requestId,
            double dpiScale,
            int maxWidth,
            int maxHeight,
            string xaml,
            BinaryWriter writer,
            object writeLock)
        {
            PreviewLoadResult loaded = PreviewDocumentLoader.LoadFromXaml(xaml);
            if (!loaded.Success)
            {
                WriteError(writer, writeLock, requestId, loaded.ErrorMessage ?? "Unknown XAML parse error.");
                return;
            }

            try
            {
                double scale = dpiScale > 0.1 && dpiScale <= 4.0 ? dpiScale : 1.0;

                double logicalW = !double.IsNaN(loaded.SuggestedWidth) && loaded.SuggestedWidth > 0
                    ? loaded.SuggestedWidth
                    : (maxWidth > 0 ? maxWidth : 800.0);
                double logicalH = !double.IsNaN(loaded.SuggestedHeight) && loaded.SuggestedHeight > 0
                    ? loaded.SuggestedHeight
                    : (maxHeight > 0 ? maxHeight : 600.0);

                logicalW = Math.Clamp(logicalW, 64.0, 2400.0);
                logicalH = Math.Clamp(logicalH, 64.0, 2400.0);

                var hostBorder = new Border
                {
                    Width = logicalW,
                    Height = logicalH,
                    Background = loaded.Background ?? Brushes.White,
                    Resources = loaded.Resources ?? new ResourceDictionary(),
                    Child = loaded.VisualContent
                };

                if (Math.Abs(scale - 1.0) > 0.001)
                {
                    hostBorder.LayoutTransform = new ScaleTransform(scale, scale);
                }

                int pixelW = Math.Max(1, (int)Math.Round(logicalW * scale));
                int pixelH = Math.Max(1, (int)Math.Round(logicalH * scale));

                hostBorder.Measure(new Size(pixelW, pixelH));
                hostBorder.Arrange(new Rect(0, 0, pixelW, pixelH));
                hostBorder.UpdateLayout();

                s_lastHostBorder = hostBorder;
                s_lastScale = scale;

                var rtb = new RenderTargetBitmap(pixelW, pixelH, 96.0, 96.0, PixelFormats.Pbgra32);
                rtb.Render(hostBorder);

                int stride = pixelW * 4;
                byte[] pixels = new byte[stride * pixelH];
                rtb.CopyPixels(pixels, stride, 0);

                lock (writeLock)
                {
                    writer.Write(RespFrame);
                    writer.Write(requestId);
                    writer.Write(loaded.DocumentTitle ?? string.Empty);
                    writer.Write(pixelW);
                    writer.Write(pixelH);
                    writer.Write(stride);
                    writer.Write(pixels.Length);
                    writer.Write(pixels);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                WriteError(writer, writeLock, requestId, ex.Message);
            }
        }

        private static void HitTestAndRespond(
            int requestId,
            double x,
            double y,
            BinaryWriter writer,
            object writeLock)
        {
            bool hit = false;
            double bx = 0, by = 0, bw = 0, bh = 0;
            string summary = "No element";

            if (s_lastHostBorder != null)
            {
                HitTestResult res = VisualTreeHelper.HitTest(s_lastHostBorder, new Point(x, y));
                DependencyObject cur = res?.VisualHit;
                FrameworkElement fe = null;
                while (cur != null)
                {
                    if (cur is FrameworkElement candidate && !ReferenceEquals(candidate, s_lastHostBorder))
                    {
                        fe = candidate;
                        break;
                    }
                    cur = VisualTreeHelper.GetParent(cur);
                }

                fe ??= s_lastHostBorder.Child as FrameworkElement;
                if (fe != null)
                {
                    hit = true;
                    Rect bounds = new Rect(0, 0, fe.RenderSize.Width, fe.RenderSize.Height);
                    try
                    {
                        bounds = fe.TransformToAncestor(s_lastHostBorder).TransformBounds(bounds);
                    }
                    catch
                    {
                    }

                    bx = bounds.X;
                    by = bounds.Y;
                    bw = bounds.Width;
                    bh = bounds.Height;

                    string namePart = string.IsNullOrEmpty(fe.Name) ? "" : $" x:Name=\"{fe.Name}\"";
                    string extra = fe switch
                    {
                        TextBlock tb when !string.IsNullOrEmpty(tb.Text) => $" | Text=\"{tb.Text}\"",
                        ContentControl cc when cc.Content is string s => $" | Content=\"{s}\"",
                        TextBox box when !string.IsNullOrEmpty(box.Text) => $" | Text=\"{box.Text}\"",
                        _ => ""
                    };
                    summary = $"<{fe.GetType().Name}{namePart}>  {fe.RenderSize.Width:F0}×{fe.RenderSize.Height:F0}  Margin={fe.Margin}{extra}";
                }
            }

            lock (writeLock)
            {
                writer.Write(RespHitTest);
                writer.Write(requestId);
                writer.Write(hit);
                writer.Write(bx);
                writer.Write(by);
                writer.Write(bw);
                writer.Write(bh);
                writer.Write(summary);
                writer.Flush();
            }
        }

        private static void WriteError(BinaryWriter writer, object writeLock, int requestId, string errorMessage)
        {
            lock (writeLock)
            {
                writer.Write(RespError);
                writer.Write(requestId);
                writer.Write(errorMessage ?? "Render error");
                writer.Flush();
            }
        }
    }
}
