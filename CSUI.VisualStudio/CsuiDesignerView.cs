using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace CSUI.VisualStudio
{
    internal sealed class CsuiDesignerView : Grid, IDisposable
    {
        private readonly IWpfTextViewHost _textViewHost;
        private readonly ITextBuffer _textBuffer;
        private readonly PreviewerClient _client;
        private readonly DispatcherTimer _debounceTimer;

        private readonly Image _previewImage;
        private readonly ScrollViewer _previewScroll;
        private readonly Border _errorOverlay;
        private readonly TextBlock _errorText;
        private readonly TextBlock _statusText;
        private readonly ComboBox _zoomBox;

        private readonly Grid _designerHost;
        private readonly Border _codeHost;
        private readonly GridSplitter _splitter;
        private ColumnDefinition _colLeft;
        private ColumnDefinition _colSplitter;
        private ColumnDefinition _colRight;
        private RowDefinition _rowTop;
        private RowDefinition _rowSplitter;
        private RowDefinition _rowBottom;

        private WriteableBitmap _bitmap;
        private bool _isHorizontalSplit = true;
        private bool _designerFirst = true;
        private bool _disposed;

        public CsuiDesignerView(IWpfTextViewHost textViewHost, ITextBuffer textBuffer, string filePath)
        {
            _textViewHost = textViewHost;
            _textBuffer = textBuffer;
            _client = new PreviewerClient(filePath);

            _client.FrameReceived += OnFrameReceived;
            _client.ErrorReceived += OnErrorReceived;

            // Preview Pane UI
            _statusText = new TextBlock
            {
                Text = "CSUI Live Preview",
                Foreground = new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0)
            };

            _zoomBox = new ComboBox
            {
                Width = 76,
                Height = 22,
                VerticalAlignment = VerticalAlignment.Center
            };
            _zoomBox.Items.Add("Fit");
            _zoomBox.Items.Add("50%");
            _zoomBox.Items.Add("75%");
            _zoomBox.Items.Add("100%");
            _zoomBox.Items.Add("125%");
            _zoomBox.Items.Add("150%");
            _zoomBox.SelectedIndex = 0;
            _zoomBox.SelectionChanged += (_, _) => ApplyZoom();

            var splitBtn = new Button { Content = "Split", Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(6, 0, 0, 0) };
            splitBtn.Click += (_, _) =>
            {
                _isHorizontalSplit = !_isHorizontalSplit;
                ApplyLayout();
            };

            var swapBtn = new Button { Content = "Swap", Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(6, 0, 0, 0) };
            swapBtn.Click += (_, _) =>
            {
                _designerFirst = !_designerFirst;
                ApplyLayout();
            };

            var refreshBtn = new Button { Content = "Refresh", Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(6, 0, 8, 0) };
            refreshBtn.Click += (_, _) => TriggerRender();

            var toolbar = new DockPanel
            {
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30)),
                LastChildFill = true
            };
            var rightStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            rightStack.Children.Add(_zoomBox);
            rightStack.Children.Add(splitBtn);
            rightStack.Children.Add(swapBtn);
            rightStack.Children.Add(refreshBtn);
            DockPanel.SetDock(rightStack, Dock.Right);
            toolbar.Children.Add(rightStack);
            toolbar.Children.Add(_statusText);

            _errorText = new TextBlock
            {
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            _errorOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x9E, 0x2A, 0x2B)),
                Padding = new Thickness(10, 6, 10, 6),
                Visibility = Visibility.Collapsed,
                Child = _errorText
            };

            _previewImage = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(_previewImage, BitmapScalingMode.HighQuality);

            _previewScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
                Content = _previewImage
            };

            var designerDock = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(toolbar, Dock.Top);
            DockPanel.SetDock(_errorOverlay, Dock.Top);
            designerDock.Children.Add(toolbar);
            designerDock.Children.Add(_errorOverlay);
            designerDock.Children.Add(_previewScroll);

            _designerHost = new Grid();
            _designerHost.Children.Add(designerDock);

            _codeHost = new Border
            {
                Child = _textViewHost.HostControl
            };

            _splitter = new GridSplitter
            {
                Background = new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x46)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Children.Add(_designerHost);
            Children.Add(_splitter);
            Children.Add(_codeHost);

            ApplyLayout();

            _debounceTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _debounceTimer.Tick += (_, _) =>
            {
                _debounceTimer.Stop();
                TriggerRender();
            };

            _textBuffer.Changed += OnTextBufferChanged;
            Loaded += (_, _) => TriggerRender();
        }

        private void ApplyLayout()
        {
            ColumnDefinitions.Clear();
            RowDefinitions.Clear();

            if (_isHorizontalSplit)
            {
                _colLeft = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
                _colSplitter = new ColumnDefinition { Width = new GridLength(5) };
                _colRight = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
                ColumnDefinitions.Add(_colLeft);
                ColumnDefinitions.Add(_colSplitter);
                ColumnDefinitions.Add(_colRight);

                SetRow(_designerHost, 0);
                SetRow(_splitter, 0);
                SetRow(_codeHost, 0);
                SetColumn(_splitter, 1);
                SetColumn(_designerHost, _designerFirst ? 0 : 2);
                SetColumn(_codeHost, _designerFirst ? 2 : 0);
            }
            else
            {
                _rowTop = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
                _rowSplitter = new RowDefinition { Height = new GridLength(5) };
                _rowBottom = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
                RowDefinitions.Add(_rowTop);
                RowDefinitions.Add(_rowSplitter);
                RowDefinitions.Add(_rowBottom);

                SetColumn(_designerHost, 0);
                SetColumn(_splitter, 0);
                SetColumn(_codeHost, 0);
                SetRow(_splitter, 1);
                SetRow(_designerHost, _designerFirst ? 0 : 2);
                SetRow(_codeHost, _designerFirst ? 2 : 0);
            }
        }

        private void ApplyZoom()
        {
            string selected = _zoomBox.SelectedItem as string;
            if (selected == "Fit" || string.IsNullOrEmpty(selected))
            {
                _previewImage.Stretch = Stretch.Uniform;
                _previewImage.LayoutTransform = Transform.Identity;
                return;
            }

            _previewImage.Stretch = Stretch.None;
            if (double.TryParse(selected.TrimEnd('%'), out double pct))
            {
                double s = pct / 100.0;
                _previewImage.LayoutTransform = new ScaleTransform(s, s);
            }
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
#pragma warning disable VSSDK007 // Fire-and-forget UI marshaling from background text buffer event
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (_disposed) return;
                _debounceTimer.Stop();
                _debounceTimer.Start();
            });
#pragma warning restore VSSDK007
        }

        private void TriggerRender()
        {
            if (_disposed) return;

            string xaml = _textBuffer.CurrentSnapshot.GetText();
            double dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            int vw = Math.Max(400, (int)_previewScroll.ActualWidth);
            int vh = Math.Max(300, (int)_previewScroll.ActualHeight);

            _statusText.Text = "Rendering...";
            _client.RequestRender(xaml, dpiScale, vw, vh);
        }

        private void OnFrameReceived(PreviewFrameData frame)
        {
#pragma warning disable VSSDK007 // Fire-and-forget UI marshaling from background IPC reader
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (_disposed) return;

                _errorOverlay.Visibility = Visibility.Collapsed;
                if (_bitmap == null || _bitmap.PixelWidth != frame.Width || _bitmap.PixelHeight != frame.Height)
                {
                    _bitmap = new WriteableBitmap(frame.Width, frame.Height, 96.0, 96.0, PixelFormats.Pbgra32, null);
                    _previewImage.Source = _bitmap;
                }

                _bitmap.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height), frame.Pixels, frame.Stride, 0);
                _statusText.Text = $"Live • {frame.Width}x{frame.Height} • {DateTime.Now:HH:mm:ss}";
            });
#pragma warning restore VSSDK007
        }

        private void OnErrorReceived(string errorMessage)
        {
#pragma warning disable VSSDK007 // Fire-and-forget UI marshaling from background IPC reader
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (_disposed) return;

                _errorText.Text = errorMessage;
                _errorOverlay.Visibility = Visibility.Visible;
                _statusText.Text = $"Error @ {DateTime.Now:HH:mm:ss}";
            });
#pragma warning restore VSSDK007
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _debounceTimer.Stop();
            _textBuffer.Changed -= OnTextBufferChanged;
            _client.Dispose();
        }
    }
}
