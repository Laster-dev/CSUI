using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Svg;
using System.Windows.Threading;
using CSUI.Markup;

namespace CSUI.SampleApp
{
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            bool verifyMode = false;
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--verify", StringComparison.OrdinalIgnoreCase))
                    {
                        verifyMode = true;
                    }
                }
            }

            var app = new Application();
            var vm = new ShowcaseViewModel();
            var rootCard = BuildShowcaseRoot(vm, out ShowcaseScene3DController scene3D, out VectorShowcaseCanvas vectorCanvas, out Action refreshSubsystemList);

            var mainWindow = new Window()
                .Title("CSUI.Core NativeAOT Showcase — Direct2D / DirectWrite / WIC / Direct3D 11")
                .Size(1200, 760)
                .StartupLocation(WindowStartupLocation.CenterScreen)
                .Background(Color.FromRgb(15, 23, 42))
                .Content(rootCard);

            IntPtr capturedHwnd = IntPtr.Zero;
            mainWindow.SourceInitialized += (s, e) =>
            {
                capturedHwnd = new WindowInteropHelper(mainWindow).Handle;
            };

            // 60 FPS Animation & 3D Scene Loop
            int frameTicks = 0;
            DispatcherTimer animTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            animTimer.Tick += (s, e) =>
            {
                frameTicks++;
                vectorCanvas.AdvancePhase(0.06);
                scene3D.StepAnimation(0.016);

                if (verifyMode)
                {
                    if (frameTicks == 2)
                    {
                        // Exercise theme switch (Dark -> Light)
                        ApplyThemeResources(rootCard.Resources, isDark: false);
                        vm.IsDarkTheme = false;
                        // Exercise CollectionView live filter
                        vm.OnlyGpuSubsystems = true;
                        refreshSubsystemList();
                    }
                    else if (frameTicks == 3)
                    {
                        // Exercise camera projection switch (Perspective -> Orthographic) & back
                        scene3D.ToggleCameraProjection();
                        scene3D.ToggleCameraProjection();
                        // Exercise 3D ray hit test on the crystal
                        scene3D.HandleMouseDown(new Point(195, 250));
                        // Exercise theme switch back to Dark
                        ApplyThemeResources(rootCard.Resources, isDark: true);
                        vm.IsDarkTheme = true;
                    }
                    else if (frameTicks >= 5)
                    {
                        animTimer.Stop();
                        mainWindow.Close();
                    }
                }
            };

            mainWindow.ContentRendered += (s, e) => animTimer.Start();
            mainWindow.Closed += (s, e) => animTimer.Stop();

            app.Run(mainWindow);

            if (verifyMode)
            {
                Console.WriteLine($"[PASS] CSUI.SampleApp Application + Window showcase verified (frames={frameTicks}, meshHits={vm.MeshHitCount}, hwnd=0x{capturedHwnd.ToInt64():X}).");
            }
            return 0;
        }

        private static CardDecorator BuildShowcaseRoot(
            ShowcaseViewModel vm,
            out ShowcaseScene3DController scene3DController,
            out VectorShowcaseCanvas vectorCanvasOut,
            out Action refreshSubsystemListOut)
        {
            var root = new CardDecorator
            {
                Width = 1180,
                Height = 720,
                CornerRadius = 0,
                Padding = new Thickness(20)
            };
            ApplyThemeResources(root.Resources, isDark: true);
            root.SetResourceReference(CardDecorator.BackgroundProperty, "ThemeBgBrush");

            var mainLayout = new StackLayoutPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 14
            };
            root.Child = mainLayout;

            // =========================================================================
            // 1. Top Header Bar (Title + Theme Switcher + Camera/Rotation Controls)
            // =========================================================================
            var headerCard = CreateThemedCard(new Thickness(16, 12, 16, 12));
            var headerRow = new CanvasPanel { Height = 44 };
            headerCard.Child = headerRow;

            // CSUI Direct2D Native SVG Vector Logo
            const string logoSvg = """
            <svg viewBox="0 0 36 36" width="36" height="36">
              <defs>
                <linearGradient id="logoGrad" x1="0%" y1="0%" x2="100%" y2="100%">
                  <stop offset="0%" stop-color="#38BDF8"/>
                  <stop offset="100%" stop-color="#6366F1"/>
                </linearGradient>
              </defs>
              <rect x="2" y="2" width="32" height="32" rx="8" ry="8" fill="url(#logoGrad)"/>
              <path d="M 10 18 L 16 24 L 26 12" fill="none" stroke="#FFFFFF" stroke-width="3.2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            """;
            var logoViewbox = new SvgViewbox()
                .Source(logoSvg)
                .Size(36, 36)
                .Stretch(Stretch.Uniform);
            CanvasPanel.SetPos(logoViewbox, 0, 4);
            headerRow.Children.Add(logoViewbox);

            var titleStack = new StackLayoutPanel { Orientation = Orientation.Vertical, Spacing = 2 };
            var titleLabel = CreateThemedLabel("CSUI.Core 全栈硬件渲染与 NativeAOT 演示台", 18, isBold: true, "ThemeTextPrimaryBrush");
            var subtitleLabel = CreateThemedLabel("Direct2D 1.1  |  原生 SVG 矢量引擎  |  1px 物理像素对齐  |  DirectWrite  |  WIC  |  Direct3D 11 Media3D  |  零反射", 12, isBold: false, "ThemeTextSecondaryBrush");
            titleStack.Children.Add(titleLabel);
            titleStack.Children.Add(subtitleLabel);
            CanvasPanel.SetPos(titleStack, 46, 0);
            headerRow.Children.Add(titleStack);

            var buttonRow = new StackLayoutPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            var btnTheme = new ShowcaseButton("主题: 深色 (Dark)", 142, 34);
            var btnAutoRotate = new ShowcaseButton("3D 旋转: 开启", 132, 34);
            var btnCamera = new ShowcaseButton("切换透视 / 正交", 142, 34);
            var btnReset = new ShowcaseButton("重置 3D 视角", 122, 34);

            buttonRow.Children.Add(btnTheme);
            buttonRow.Children.Add(btnAutoRotate);
            buttonRow.Children.Add(btnCamera);
            buttonRow.Children.Add(btnReset);
            CanvasPanel.SetPos(buttonRow, 560, 5);
            headerRow.Children.Add(buttonRow);
            mainLayout.Children.Add(headerCard);

            // =========================================================================
            // 2. Center Two-Column Content Area (Left: 2D/Binding/WIC, Right: 3D D3D11)
            // =========================================================================
            var bodyCanvas = new CanvasPanel { Height = 540 };
            mainLayout.Children.Add(bodyCanvas);

            var leftColumn = new StackLayoutPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 12,
                Width = 450
            };
            CanvasPanel.SetPos(leftColumn, 0, 0);
            bodyCanvas.Children.Add(leftColumn);

            // --- Left Card A: Direct2D Vector Graphics, Gradients, StreamGeometry & WIC ---
            var vectorCard = CreateThemedCard(new Thickness(14, 12, 14, 12));
            var vectorStack = new StackLayoutPanel { Orientation = Orientation.Vertical, Spacing = 8 };
            vectorCard.Child = vectorStack;
            vectorStack.Children.Add(CreateThemedLabel("1. Direct2D 矢量图元 + StreamGeometry + WIC 动态位图", 13.5, isBold: true, "ThemeAccentBrush"));

            var vectorCanvas = new VectorShowcaseCanvas { Width = 420, Height = 132 };
            vectorCanvasOut = vectorCanvas;
            vectorStack.Children.Add(vectorCanvas);
            leftColumn.Children.Add(vectorCard);

            // --- Left Card B: NativeAOT Reflection-Free Data Binding (Binding.Create) ---
            var bindingCard = CreateThemedCard(new Thickness(14, 12, 14, 12));
            var bindingStack = new StackLayoutPanel { Orientation = Orientation.Vertical, Spacing = 7 };
            bindingCard.Child = bindingStack;
            bindingStack.Children.Add(CreateThemedLabel("2. NativeAOT 零反射双向数据绑定 (Binding.Create)", 13.5, isBold: true, "ThemeAccentBrush"));

            var speedReadout = CreateThemedLabel(string.Empty, 12.5, isBold: false, "ThemeTextPrimaryBrush")
                .BindOneWay(
                    DirectWriteLabel.TextProperty,
                    static (ShowcaseViewModel s) => s.RotationSpeed,
                    nameof(ShowcaseViewModel.RotationSpeed),
                    source: vm,
                    converter: SpeedFormatConverter.Instance);
            bindingStack.Children.Add(speedReadout);

            var speedSlider = new InteractiveSliderBar { Minimum = 0.0, Maximum = 180.0 }
                .Width(420)
                .BindTwoWay(
                    InteractiveSliderBar.ValueProperty,
                    static (ShowcaseViewModel s) => s.RotationSpeed,
                    static (ShowcaseViewModel s, double v) => s.RotationSpeed = v,
                    nameof(ShowcaseViewModel.RotationSpeed),
                    source: vm);
            bindingStack.Children.Add(speedSlider);

            var lightReadout = CreateThemedLabel(string.Empty, 12.5, isBold: false, "ThemeTextPrimaryBrush")
                .BindOneWay(
                    DirectWriteLabel.TextProperty,
                    static (ShowcaseViewModel s) => s.LightIntensity,
                    nameof(ShowcaseViewModel.LightIntensity),
                    source: vm,
                    converter: LightIntensityFormatConverter.Instance);
            bindingStack.Children.Add(lightReadout);

            var lightSlider = new InteractiveSliderBar
            {
                Minimum = 10.0,
                Maximum = 100.0,
                AccentBrush = new SolidColorBrush(Color.FromRgb(250, 204, 21))
            }
                .Width(420)
                .BindTwoWay(
                    InteractiveSliderBar.ValueProperty,
                    static (ShowcaseViewModel s) => s.LightIntensity,
                    static (ShowcaseViewModel s, double v) => s.LightIntensity = v,
                    nameof(ShowcaseViewModel.LightIntensity),
                    source: vm);
            bindingStack.Children.Add(lightSlider);

            var surfaceClickReadout = CreateThemedLabel(string.Empty, 12.5, isBold: true, "ThemeTextSecondaryBrush")
                .BindOneWay(
                    DirectWriteLabel.TextProperty,
                    static (ShowcaseViewModel s) => s.SurfaceClickCount,
                    nameof(ShowcaseViewModel.SurfaceClickCount),
                    source: vm,
                    converter: SurfaceClickFormatConverter.Instance);
            bindingStack.Children.Add(surfaceClickReadout);
            leftColumn.Children.Add(bindingCard);

            // --- Left Card C: CollectionView Live Filtering ---
            var collectionCard = CreateThemedCard(new Thickness(14, 10, 14, 10));
            var collectionStack = new StackLayoutPanel { Orientation = Orientation.Vertical, Spacing = 6 };
            collectionCard.Child = collectionStack;

            var filterHeaderRow = new CanvasPanel { Height = 28 };
            var filterTitle = CreateThemedLabel("3. CollectionView 实时过滤子系统状态", 13.5, isBold: true, "ThemeAccentBrush");
            CanvasPanel.SetPos(filterTitle, 0, 4);
            filterHeaderRow.Children.Add(filterTitle);

            var btnFilterGpu = new ShowcaseButton("筛选: 全部 (7)", 126, 26);
            CanvasPanel.SetPos(btnFilterGpu, 294, 0);
            filterHeaderRow.Children.Add(btnFilterGpu);
            collectionStack.Children.Add(filterHeaderRow);

            var itemsListStack = new StackLayoutPanel { Orientation = Orientation.Vertical, Spacing = 3 };
            collectionStack.Children.Add(itemsListStack);

            ICollectionView subsystemsView = CollectionViewSource.GetDefaultView(vm.Subsystems);
            subsystemsView.Filter = item =>
            {
                if (!vm.OnlyGpuSubsystems) return true;
                return item is SubsystemTelemetryItem telemetry && telemetry.IsGpuAccelerated;
            };

            void RebuildSubsystemList()
            {
                itemsListStack.Children.Clear();
                int shown = 0;
                foreach (object obj in subsystemsView)
                {
                    if (obj is SubsystemTelemetryItem item)
                    {
                        shown++;
                        var line = CreateThemedLabel(
                            $"• [{item.Category}] {item.Name} — {item.StatusText}",
                            11.5,
                            isBold: false,
                            "ThemeTextSecondaryBrush");
                        itemsListStack.Children.Add(line);
                    }
                }
                btnFilterGpu.LabelText = vm.OnlyGpuSubsystems ? $"仅 GPU ({shown})" : $"全部子系统 ({shown})";
            }

            RebuildSubsystemList();
            refreshSubsystemListOut = RebuildSubsystemList;

            btnFilterGpu.Click += (s, e) =>
            {
                vm.OnlyGpuSubsystems = !vm.OnlyGpuSubsystems;
                subsystemsView.Refresh();
                RebuildSubsystemList();
            };

            leftColumn.Children.Add(collectionCard);

            // --- Right Column: Hardware Direct3D 11 Viewport3D + Viewport2DVisual3D ---
            var viewportCard = CreateThemedCard(new Thickness(16, 12, 16, 12));
            viewportCard.Width = 674;
            viewportCard.Height = 540;
            CanvasPanel.SetPos(viewportCard, 466, 0);
            bodyCanvas.Children.Add(viewportCard);

            var rightStack = new StackLayoutPanel { Orientation = Orientation.Vertical, Spacing = 8 };
            viewportCard.Child = rightStack;

            var vpHeaderRow = new CanvasPanel { Height = 24 };
            var vpTitle = CreateThemedLabel("4. Direct3D 11 硬件管线 (Viewport3D + 四光源高光 + Viewport2DVisual3D 跨维交互)", 13.5, isBold: true, "ThemeAccentBrush");
            CanvasPanel.SetPos(vpTitle, 0, 2);
            vpHeaderRow.Children.Add(vpTitle);

            var camModeLabel = CreateThemedLabel(string.Empty, 12, isBold: false, "ThemeTextSecondaryBrush");
            Binding camModeBinding = Binding.Create<ShowcaseViewModel, string>(
                static s => s.CameraModeName,
                setter: null,
                propertyName: nameof(ShowcaseViewModel.CameraModeName));
            camModeBinding.Source = vm;
            BindingOperations.SetBinding(camModeLabel, DirectWriteLabel.TextProperty, camModeBinding);
            CanvasPanel.SetPos(camModeLabel, 455, 3);
            vpHeaderRow.Children.Add(camModeLabel);
            rightStack.Children.Add(vpHeaderRow);

            var scene3D = new ShowcaseScene3DController(vm, 640, 445);
            scene3DController = scene3D;

            var viewportHost = new CardDecorator
            {
                Width = 642,
                Height = 448,
                CornerRadius = 8,
                Padding = new Thickness(1),
                Background = new SolidColorBrush(Color.FromRgb(11, 16, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(45, 62, 98)),
                Child = scene3D.Viewport
            };

            viewportHost.MouseLeftButtonDown += (s, e) =>
            {
                Point pos = e.GetPosition(scene3D.Viewport);
                scene3D.HandleMouseDown(pos);
            };
            viewportHost.MouseMove += (s, e) =>
            {
                Point pos = e.GetPosition(scene3D.Viewport);
                scene3D.HandleMouseMove(pos, e.LeftButton == MouseButtonState.Pressed);
            };
            viewportHost.MouseLeftButtonUp += (s, e) => scene3D.HandleMouseUp();
            viewportHost.MouseWheel += (s, e) => scene3D.HandleMouseWheel(e.Delta);

            rightStack.Children.Add(viewportHost);

            var hintLabel = CreateThemedLabel(
                "操作提示: 左键点击左侧 3D 晶体触发 RayHitTest 重心坐标检测；点击右侧 3D 倾斜面板上的 2D 按钮触发跨维事件；按住空白处拖拽旋转，滚轮缩放。",
                11.5,
                isBold: false,
                "ThemeTextSecondaryBrush");
            rightStack.Children.Add(hintLabel);

            // =========================================================================
            // 3. Bottom Status Bar (Loaded via XamlMarkup.Load + AOT Binding Context)
            // =========================================================================
            XamlMarkup.RegisterType<CardDecorator>();
            XamlMarkup.RegisterProperty(typeof(CardDecorator), nameof(CardDecorator.Background), CardDecorator.BackgroundProperty);
            XamlMarkup.RegisterProperty(typeof(CardDecorator), nameof(CardDecorator.BorderBrush), CardDecorator.BorderBrushProperty);
            XamlMarkup.RegisterProperty(typeof(CardDecorator), nameof(CardDecorator.CornerRadius), CardDecorator.CornerRadiusProperty);
            XamlMarkup.RegisterProperty(typeof(CardDecorator), nameof(CardDecorator.Padding), CardDecorator.PaddingProperty);

            XamlMarkup.RegisterType<DirectWriteLabel>();
            XamlMarkup.RegisterProperty(typeof(DirectWriteLabel), nameof(DirectWriteLabel.Text), DirectWriteLabel.TextProperty);
            XamlMarkup.RegisterProperty(typeof(DirectWriteLabel), nameof(DirectWriteLabel.Foreground), DirectWriteLabel.ForegroundProperty);
            XamlMarkup.RegisterProperty(typeof(DirectWriteLabel), nameof(DirectWriteLabel.FontSize), DirectWriteLabel.FontSizeProperty);
            XamlMarkup.RegisterProperty(typeof(DirectWriteLabel), nameof(DirectWriteLabel.IsBold), DirectWriteLabel.IsBoldProperty);

            var xamlCtx = new XamlLoadContext { DataContext = vm }
                .RegisterBinding<ShowcaseViewModel, string>(
                    nameof(ShowcaseViewModel.LastHitSummary),
                    static s => s.LastHitSummary,
                    source: vm);

            const string statusBarXaml = """
                <CardDecorator xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                               CornerRadius="10"
                               Padding="16,9,16,9"
                               Background="{DynamicResource ThemeCardBrush}"
                               BorderBrush="{DynamicResource ThemeCardBorderBrush}">
                    <DirectWriteLabel x:Name="StatusLabel"
                                      FontSize="12.5"
                                      IsBold="True"
                                      Foreground="{DynamicResource ThemeTextPrimaryBrush}"
                                      Text="{Binding LastHitSummary}" />
                </CardDecorator>
                """;

            CardDecorator statusCard = XamlMarkup.Load<CardDecorator>(statusBarXaml, xamlCtx);
            mainLayout.Children.Add(statusCard);

            // Wire up ViewModel & Header Button handlers
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ShowcaseViewModel.LightIntensity))
                {
                    scene3D.UpdateLightingFromViewModel();
                }
            };

            btnTheme.Click += (s, e) =>
            {
                vm.IsDarkTheme = !vm.IsDarkTheme;
                ApplyThemeResources(root.Resources, vm.IsDarkTheme);
                btnTheme.LabelText = vm.IsDarkTheme ? "主题: 深色 (Dark)" : "主题: 浅色 (Light)";
            };

            btnAutoRotate.Click += (s, e) =>
            {
                vm.IsAutoRotating = !vm.IsAutoRotating;
                btnAutoRotate.LabelText = vm.IsAutoRotating ? "3D 旋转: 开启" : "3D 旋转: 暂停";
            };

            btnCamera.Click += (s, e) => scene3D.ToggleCameraProjection();
            btnReset.Click += (s, e) => scene3D.ResetView();

            return root;
        }

        private static CardDecorator CreateThemedCard(Thickness padding)
        {
            var card = new CardDecorator
            {
                CornerRadius = 10.0,
                Padding = padding,
                Effect = new ModernElevationEffect(3.0)
            };
            card.SetResourceReference(CardDecorator.BackgroundProperty, "ThemeCardBrush");
            card.SetResourceReference(CardDecorator.BorderBrushProperty, "ThemeCardBorderBrush");
            return card;
        }

        private static DirectWriteLabel CreateThemedLabel(string text, double fontSize, bool isBold, string foregroundResourceKey)
        {
            var lbl = new DirectWriteLabel
            {
                Text = text,
                FontSize = fontSize,
                IsBold = isBold
            };
            lbl.SetResourceReference(DirectWriteLabel.ForegroundProperty, foregroundResourceKey);
            return lbl;
        }

        private static void ApplyThemeResources(ResourceDictionary resources, bool isDark)
        {
            if (isDark)
            {
                resources["ThemeBgBrush"] = new SolidColorBrush(Color.FromRgb(10, 15, 28));
                resources["ThemeCardBrush"] = new SolidColorBrush(Color.FromRgb(19, 28, 49));
                resources["ThemeCardBorderBrush"] = new SolidColorBrush(Color.FromRgb(42, 58, 96));
                resources["ThemeTextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                resources["ThemeTextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                resources["ThemeAccentBrush"] = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            }
            else
            {
                resources["ThemeBgBrush"] = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                resources["ThemeCardBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                resources["ThemeCardBorderBrush"] = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                resources["ThemeTextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                resources["ThemeTextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(71, 85, 105));
                resources["ThemeAccentBrush"] = new SolidColorBrush(Color.FromRgb(2, 132, 199));
            }
        }
    }
}
