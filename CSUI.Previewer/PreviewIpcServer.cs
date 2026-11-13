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
        private const byte MsgShutdown = 255;

        private const byte RespFrame = 1;
        private const byte RespError = 2;

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

                        if (msgType != MsgRenderXaml)
                        {
                            continue;
                        }

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
