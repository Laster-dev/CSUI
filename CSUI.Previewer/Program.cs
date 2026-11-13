using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CSUI.Previewer
{
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            string targetFile = null;
            bool topmost = false;
            bool verifyOnce = false;

            foreach (string arg in args)
            {
                if (string.Equals(arg, "--topmost", StringComparison.OrdinalIgnoreCase))
                {
                    topmost = true;
                }
                else if (string.Equals(arg, "--verify", StringComparison.OrdinalIgnoreCase))
                {
                    verifyOnce = true;
                }
                else if (!arg.StartsWith("--", StringComparison.Ordinal) && targetFile == null)
                {
                    targetFile = arg;
                }
            }

            if (targetFile != null)
            {
                targetFile = Path.GetFullPath(targetFile);
            }

            if (verifyOnce)
            {
                if (string.IsNullOrEmpty(targetFile) || !File.Exists(targetFile))
                {
                    Console.Error.WriteLine($"XAML file not found: {targetFile}");
                    return 1;
                }

                string xaml = PreviewDocumentLoader.ReadFileWithRetry(targetFile);
                PreviewLoadResult result = PreviewDocumentLoader.LoadFromXaml(xaml);
                if (!result.Success)
                {
                    Console.Error.WriteLine($"Preview load failed: {result.ErrorMessage}");
                    return 2;
                }

                Console.WriteLine($"[PASS] Preview loaded '{Path.GetFileName(targetFile)}' -> {result.VisualContent?.GetType().Name}, Title='{result.DocumentTitle}'");
                return 0;
            }

            var app = new Application();
            var host = new PreviewerHostWindow(targetFile, topmost);
            app.MainWindow = host;
            host.Show();
            return app.Run();
        }
    }

    internal sealed class PreviewerHostWindow : Window
    {
        private readonly string _targetFile;
        private readonly Border _previewSurface;
        private readonly Border _errorBanner;
        private readonly TextBlock _errorText;
        private readonly TextBlock _statusText;
        private readonly CheckBox _topmostToggle;
        private readonly DispatcherTimer _debounceTimer;
        private FileSystemWatcher _watcher;

        public PreviewerHostWindow(string targetFile, bool initialTopmost)
        {
            _targetFile = targetFile;
            Title = targetFile != null
                ? $"CSUI Live XAML Preview - {Path.GetFileName(targetFile)}"
                : "CSUI Live XAML Preview";
            Width = 1040;
            Height = 820;
            Topmost = initialTopmost;
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _statusText = new TextBlock
            {
                Text = targetFile != null ? $"Watching: {targetFile}" : "Usage: CSUI.Previewer <path-to-file.xaml> [--topmost]",
                Foreground = new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4)),
                VerticalAlignment = VerticalAlignment.Center
            };

            _topmostToggle = new CheckBox
            {
                Content = "置顶 (Topmost)",
                IsChecked = initialTopmost,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6)),
                VerticalAlignment = VerticalAlignment.Center
            };
            _topmostToggle.Checked += (_, _) => Topmost = true;
            _topmostToggle.Unchecked += (_, _) => Topmost = false;

            var refreshButton = new Button
            {
                Content = "刷新",
                Width = 72,
                Height = 24
            };
            refreshButton.Click += (_, _) => ReloadTarget();

            var toolbar = new DockPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30)),
                Height = 36,
                LastChildFill = true
            };
            var rightControls = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Margin = new Thickness(8, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            rightControls.Children.Add(_topmostToggle);
            rightControls.Children.Add(refreshButton);
            DockPanel.SetDock(rightControls, Dock.Right);
            toolbar.Children.Add(rightControls);

            _statusText.Margin = new Thickness(12, 0, 8, 0);
            toolbar.Children.Add(_statusText);

            _errorText = new TextBlock
            {
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            _errorBanner = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x9E, 0x2A, 0x2B)),
                Padding = new Thickness(12, 8, 12, 8),
                Visibility = Visibility.Collapsed,
                Child = _errorText
            };

            _previewSurface = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E))
            };

            var rootDock = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(toolbar, Dock.Top);
            DockPanel.SetDock(_errorBanner, Dock.Top);
            rootDock.Children.Add(toolbar);
            rootDock.Children.Add(_errorBanner);
            rootDock.Children.Add(_previewSurface);

            Content = rootDock;

            _debounceTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(90)
            };
            _debounceTimer.Tick += (_, _) =>
            {
                _debounceTimer.Stop();
                ReloadTarget();
            };

            if (!string.IsNullOrEmpty(_targetFile))
            {
                ReloadTarget();
                StartWatcher();
            }
            else
            {
                _previewSurface.Child = new TextBlock
                {
                    Text = "请通过命令行传入要实时预览的 .xaml 文件路径：\nCSUI.Previewer.exe D:\\CSUI\\CSUI.SampleApp\\MainWindow.xaml",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                    Margin = new Thickness(24),
                    TextWrapping = TextWrapping.Wrap
                };
            }

            Closed += (_, _) => _watcher?.Dispose();
        }

        private void StartWatcher()
        {
            string dir = Path.GetDirectoryName(_targetFile);
            string file = Path.GetFileName(_targetFile);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return;
            }

            _watcher = new FileSystemWatcher(dir)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            void OnFsEvent(string changedPath)
            {
                if (string.Equals(Path.GetFileName(changedPath), file, StringComparison.OrdinalIgnoreCase))
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        _debounceTimer.Stop();
                        _debounceTimer.Start();
                    });
                }
            }

            _watcher.Changed += (_, e) => OnFsEvent(e.FullPath);
            _watcher.Created += (_, e) => OnFsEvent(e.FullPath);
            _watcher.Renamed += (_, e) => OnFsEvent(e.FullPath);
        }

        private void ReloadTarget()
        {
            if (string.IsNullOrEmpty(_targetFile) || !File.Exists(_targetFile))
            {
                _errorText.Text = $"文件不存在: {_targetFile}";
                _errorBanner.Visibility = Visibility.Visible;
                _statusText.Text = "File missing";
                return;
            }

            string xaml;
            try
            {
                xaml = PreviewDocumentLoader.ReadFileWithRetry(_targetFile);
            }
            catch (Exception ex)
            {
                _errorText.Text = $"读取失败: {ex.Message}";
                _errorBanner.Visibility = Visibility.Visible;
                return;
            }

            PreviewLoadResult result = PreviewDocumentLoader.LoadFromXaml(xaml);
            if (!result.Success)
            {
                _errorText.Text = $"XAML 解析错误: {result.ErrorMessage}";
                _errorBanner.Visibility = Visibility.Visible;
                _statusText.Text = $"Error @ {DateTime.Now:HH:mm:ss}";
                return;
            }

            _errorBanner.Visibility = Visibility.Collapsed;
            _previewSurface.Resources = result.Resources ?? new ResourceDictionary();
            _previewSurface.Background = result.Background ?? new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            _previewSurface.Child = result.VisualContent;

            if (!string.IsNullOrWhiteSpace(result.DocumentTitle))
            {
                Title = $"CSUI Live XAML Preview - {Path.GetFileName(_targetFile)} ({result.DocumentTitle})";
            }

            if (!double.IsNaN(result.SuggestedWidth) && result.SuggestedWidth > 0 &&
                !double.IsNaN(result.SuggestedHeight) && result.SuggestedHeight > 0)
            {
                Width = Math.Max(360, result.SuggestedWidth);
                Height = Math.Max(260, result.SuggestedHeight + 36);
            }

            _statusText.Text = $"Live • {Path.GetFileName(_targetFile)} • Updated {DateTime.Now:HH:mm:ss}";
        }
    }
}
