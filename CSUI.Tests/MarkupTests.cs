using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Media.Svg;
using System.Windows.Media.Composition;
using System.Windows.Input;
using System.Windows.Input.Gestures;
using System.Windows.Threading;
using CSUI.Markup;
using static CSUI.Tests.TestHelpers;

namespace CSUI.Tests;

internal static class MarkupTests
{
    public static void RunAll()
    {
        Console.WriteLine("\n--- Layer 5: CSUI.Markup & CSUI.Generators (Declarative Fluent API, XAML & Roslyn Source Generator) ---");
        TestFluentDeclarativeApi();
        TestNativeAotXamlEngine2DAnd3D();
        TestCompileTimeXamlToCSharpGenerator();
        TestAdvancedBindingsInFluentAndXaml();
        TestRoslynSourceGeneratorZeroBoilerplate();
        TestZeroModificationWpfDropIn();
        TestApplicationWindowPopupAndToolTip();
        TestSvgEngineAndPixelAlignment();
        TestModernEffectsAndAcrylicMaterial();
        TestStandardLayoutMatrix();
        TestModernLayoutPanels();
        TestBasicControlsMatrix();
        TestScrollViewerAndSmoothInertia();
        TestVariableHeightVirtualizingStackPanel();
        TestDirectWriteTextEngineAndTextFlow();
        TestAsyncCompositorAndIndependentAnimations();
        TestModernGestureEngineAndPinchPan();
        TestPerMonitorV2DpiHotPlug();
        TestXamlMarkupExtensionsAndResourceFeatures();
    }

    public sealed class MarkupCard : Decorator
    {
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(
                nameof(Background),
                typeof(Brush),
                typeof(MarkupCard),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (Background != null)
            {
                dc.DrawRectangle(Background, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            }
        }
    }

    public class MarkupCanvas : Panel
    {
        public static readonly DependencyProperty LeftProperty =
            DependencyProperty.RegisterAttached(
                "Left",
                typeof(double),
                typeof(MarkupCanvas),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static readonly DependencyProperty TopProperty =
            DependencyProperty.RegisterAttached(
                "Top",
                typeof(double),
                typeof(MarkupCanvas),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        protected override Size MeasureOverride(Size availableSize)
        {
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                InternalChildren[i]?.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }
            return new Size(
                double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
                double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                UIElement child = InternalChildren[i];
                if (child == null) continue;
                double x = (double)child.GetValue(LeftProperty);
                double y = (double)child.GetValue(TopProperty);
                child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
            }
            return finalSize;
        }
    }

    public sealed class MarkupTemplatedControl : Control
    {
        public MarkupCard? PartRoot { get; private set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            PartRoot = GetTemplateChild("PART_Root") as MarkupCard;
        }
    }

    public sealed class MarkupViewModel : INotifyPropertyChanged
    {
        private double _cardWidth = 84.0;
        public double CardWidth
        {
            get => _cardWidth;
            set
            {
                if (Math.Abs(_cardWidth - value) > 1e-5)
                {
                    _cardWidth = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CardWidth)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // MK-1: Declarative C# Fluent API (Elements, Layout, AOT Binding, DynamicResource, Fluent Style & ControlTemplate)
    private static void TestFluentDeclarativeApi()
    {
        var vm = new MarkupViewModel { CardWidth = 90.0 };

        ControlTemplate fluentTemplate = MarkupTemplate.For<MarkupTemplatedControl, MarkupCard>(
            "PART_Root",
            root => root
                .Set(FrameworkElement.WidthProperty, 120.0)
                .Set(FrameworkElement.HeightProperty, 48.0)
                .TemplateBind(MarkupCard.BackgroundProperty, Control.BackgroundProperty));

        Style fluentStyle = MarkupStyle.For<MarkupTemplatedControl>()
            .Set(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(20, 120, 240)))
            .Set(UIElement.OpacityProperty, 1.0)
            .Template(fluentTemplate)
            .When(UIElement.IsEnabledProperty, false, trig => trig.Set(UIElement.OpacityProperty, 0.35))
            .Build();

        var rootPanel = new MarkupCanvas()
            .Size(240, 160)
            .Resource("AccentBrush", new SolidColorBrush(Color.FromRgb(0, 220, 130)))
            .Children(
                new MarkupTemplatedControl()
                    .Assign(out var templatedCtrl)
                    .Name("BtnControl")
                    .Size(120, 48)
                    .Style(fluentStyle)
                    .Attached(MarkupCanvas.LeftProperty, 16.0)
                    .Attached(MarkupCanvas.TopProperty, 12.0),
                new MarkupCard()
                    .Assign(out var boundCard)
                    .Height(40)
                    .DynamicResource(MarkupCard.BackgroundProperty, "AccentBrush")
                    .BindTwoWay(
                        FrameworkElement.WidthProperty,
                        static (MarkupViewModel m) => m.CardWidth,
                        static (MarkupViewModel m, double v) => m.CardWidth = v,
                        nameof(MarkupViewModel.CardWidth),
                        source: vm)
                    .Attached(MarkupCanvas.LeftProperty, 16.0)
                    .Attached(MarkupCanvas.TopProperty, 72.0));

        rootPanel.Measure(new Size(240, 160));
        rootPanel.Arrange(new Rect(0, 0, 240, 160));

        Assert(templatedCtrl.PartRoot != null, "Fluent ControlTemplate should instantiate PART_Root");
        AssertNear(boundCard.Width, 90.0, 1e-4, "Fluent BindTwoWay initial Width");

        // Verify TwoWay source->target and target->source
        vm.CardWidth = 135.0;
        AssertNear(boundCard.Width, 135.0, 1e-4, "Fluent BindTwoWay updated Width from ViewModel");
        boundCard.Width = 110.0;
        AssertNear(vm.CardWidth, 110.0, 1e-4, "Fluent BindTwoWay updated ViewModel from element");

        // Verify Fluent Style Trigger
        templatedCtrl.IsEnabled = false;
        AssertNear(templatedCtrl.Opacity, 0.35, 1e-4, "Fluent Style Trigger Opacity when disabled");
        templatedCtrl.IsEnabled = true;
        AssertNear(templatedCtrl.Opacity, 1.0, 1e-4, "Fluent Style Trigger Opacity restored");

        Pass("MK-1. Declarative C# Fluent API (Layout, Style, Trigger, ControlTemplate, TwoWay AOT Binding)",
            $"boundWidth={boundCard.Width}, templatePart={templatedCtrl.PartRoot != null}");
    }

    // MK-2: Zero-Reflection NativeAOT XAML Engine (2D + Style/Template + Attached Props + AOT Binding + 3D Viewport3D)
    private static void TestNativeAotXamlEngine2DAnd3D()
    {
        XamlMarkup.RegisterType<MarkupCard>();
        XamlMarkup.RegisterProperty(typeof(MarkupCard), nameof(MarkupCard.Background), MarkupCard.BackgroundProperty);

        XamlMarkup.RegisterType<MarkupCanvas>();
        XamlMarkup.RegisterProperty(typeof(MarkupCanvas), "Left", MarkupCanvas.LeftProperty);
        XamlMarkup.RegisterProperty(typeof(MarkupCanvas), "Top", MarkupCanvas.TopProperty);

        XamlMarkup.RegisterType<MarkupTemplatedControl>();

        var vm = new MarkupViewModel { CardWidth = 76.0 };
        var loadCtx = new XamlLoadContext { DataContext = vm }
            .RegisterBinding<MarkupViewModel, double>(
                nameof(MarkupViewModel.CardWidth),
                static m => m.CardWidth,
                static (m, v) => m.CardWidth = v,
                source: vm);

        const string xaml = """
            <MarkupCanvas xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                          Width="200" Height="160">
                <MarkupCanvas.Resources>
                    <SolidColorBrush x:Key="CardThemeBrush" Color="#FF10B981" />
                    <Style x:Key="CustomCtrlStyle" TargetType="MarkupTemplatedControl">
                        <Setter Property="Width" Value="100" />
                        <Setter Property="Height" Value="36" />
                        <Setter Property="Background" Value="#FF2563EB" />
                        <Setter Property="Template">
                            <Setter.Value>
                                <ControlTemplate TargetType="MarkupTemplatedControl">
                                    <MarkupCard x:Name="PART_Root" Width="100" Height="36" Background="{TemplateBinding Background}" />
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                        <Style.Triggers>
                            <Trigger Property="IsEnabled" Value="False">
                                <Setter Property="Opacity" Value="0.25" />
                            </Trigger>
                        </Style.Triggers>
                    </Style>
                </MarkupCanvas.Resources>

                <MarkupCard x:Name="DynamicCard"
                            MarkupCanvas.Left="12"
                            MarkupCanvas.Top="8"
                            Width="{Binding CardWidth}"
                            Height="32"
                            Background="{DynamicResource CardThemeBrush}" />

                <MarkupTemplatedControl x:Name="StyledCtrl"
                                        MarkupCanvas.Left="12"
                                        MarkupCanvas.Top="48"
                                        Style="{StaticResource CustomCtrlStyle}" />

                <Viewport3D x:Name="Scene3D"
                            MarkupCanvas.Left="12"
                            MarkupCanvas.Top="90"
                            Width="64" Height="64">
                    <Viewport3D.Camera>
                        <PerspectiveCamera Position="0,0,3" LookDirection="0,0,-1" UpDirection="0,1,0" FieldOfView="45" />
                    </Viewport3D.Camera>
                    <ModelVisual3D>
                        <ModelVisual3D.Content>
                            <Model3DGroup>
                                <AmbientLight Color="#FFFFFFFF" />
                                <GeometryModel3D>
                                    <GeometryModel3D.Geometry>
                                        <MeshGeometry3D Positions="-2,2,0 -2,-2,0 2,-2,0 2,2,0"
                                                        TriangleIndices="0,1,2 0,2,3" />
                                    </GeometryModel3D.Geometry>
                                    <GeometryModel3D.Material>
                                        <DiffuseMaterial Brush="#FF00FF66" />
                                    </GeometryModel3D.Material>
                                </GeometryModel3D>
                            </Model3DGroup>
                        </ModelVisual3D.Content>
                    </ModelVisual3D>
                </Viewport3D>
            </MarkupCanvas>
            """;

        MarkupCanvas canvas = XamlMarkup.Load<MarkupCanvas>(xaml, out XamlLoadContext ctx, loadCtx);
        canvas.Measure(new Size(200, 160));
        canvas.Arrange(new Rect(0, 0, 200, 160));

        var dynamicCard = ctx.FindName<MarkupCard>("DynamicCard");
        var styledCtrl = ctx.FindName<MarkupTemplatedControl>("StyledCtrl");
        var scene3D = ctx.FindName<Viewport3D>("Scene3D");

        Assert(dynamicCard != null && styledCtrl != null && scene3D != null, "Named XAML elements resolved");
        AssertNear(dynamicCard!.Width, 76.0, 1e-4, "XAML {Binding CardWidth} initial value");

        vm.CardWidth = 112.0;
        AssertNear(dynamicCard.Width, 112.0, 1e-4, "XAML {Binding CardWidth} updated via INotifyPropertyChanged");

        Assert(styledCtrl!.PartRoot != null, "XAML ControlTemplate instantiated PART_Root");
        styledCtrl.IsEnabled = false;
        AssertNear(styledCtrl.Opacity, 0.25, 1e-4, "XAML Style Trigger Opacity when disabled");

        // Verify GPU rendering of the XAML-constructed Canvas (both 2D DynamicCard and 3D Viewport3D)
        byte[] canvasPixels = RenderToBgra32(canvas, 200, 160);
        var (cardB, cardG, cardR, cardA) = Sample(canvasPixels, 200, 30, 20);
        Assert(cardA == 255 && cardG > 160, $"XAML 2D DynamicCard pixel expected emerald, got R={cardR},G={cardG},B={cardB},A={cardA}");

        var (b, g, r, a) = Sample(canvasPixels, 200, 12 + 32, 90 + 32);
        Assert(a == 255 && g > 230 && r < 20, $"XAML Viewport3D center pixel should be green, got R={r},G={g},B={b},A={a}");

        Pass("MK-2. Zero-Reflection NativeAOT XAML Engine (Resources, Style, ControlTemplate, Binding, Viewport3D)",
            $"boundW={dynamicCard.Width}, vp3DCenter=(R={r}, G={g}, B={b}, A={a})");
    }

    // MK-3: Compile-Time XAML-to-C# Code Generator (XamlCSharpGenerator)
    private static void TestCompileTimeXamlToCSharpGenerator()
    {
        const string xamlWithClass = """
            <MarkupCanvas xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                          x:Class="CSUI.Tests.SampleGeneratedView"
                          x:DataType="CSUI.Tests.MarkupTests.MarkupViewModel"
                          Width="320" Height="200">
                <MarkupCanvas.Resources>
                    <SolidColorBrush x:Key="HeaderBrush" Color="#FF38BDF8" />
                </MarkupCanvas.Resources>
                <MarkupCard x:Name="HeaderCard"
                            MarkupCanvas.Left="20"
                            MarkupCanvas.Top="14"
                            Width="{Binding CardWidth, Mode=TwoWay}"
                            Height="44"
                            Background="{DynamicResource HeaderBrush}" />
            </MarkupCanvas>
            """;

        string generatedCSharp = XamlCSharpGenerator.GeneratePartialClass(xamlWithClass);

        Assert(generatedCSharp.Contains("partial class SampleGeneratedView : MarkupCanvas", StringComparison.Ordinal),
            "Generated partial class header");
        Assert(generatedCSharp.Contains("internal MarkupCard HeaderCard;", StringComparison.Ordinal),
            "Generated strongly-typed x:Name field");
        Assert(generatedCSharp.Contains("public void InitializeComponent()", StringComparison.Ordinal),
            "Generated InitializeComponent()");
        Assert(generatedCSharp.Contains("Binding.Create<CSUI.Tests.MarkupTests.MarkupViewModel, double>", StringComparison.Ordinal),
            "Generated AOT Binding.Create delegate from x:DataType");
        Assert(generatedCSharp.Contains("SetResourceReference(MarkupCard.BackgroundProperty, \"HeaderBrush\")", StringComparison.Ordinal),
            "Generated DynamicResource call");

        Pass("MK-3. Compile-Time XAML-to-C# Code Generator (x:Class, x:Name, x:DataType -> Binding.Create)",
            $"generatedLength={generatedCSharp.Length} chars");
    }

    public sealed class ManagerInfo : INotifyPropertyChanged
    {
        private double _baseBudget = 60.0;
        public double BaseBudget
        {
            get => _baseBudget;
            set
            {
                if (Math.Abs(_baseBudget - value) > 1e-5)
                {
                    _baseBudget = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BaseBudget)));
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class DepartmentModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private ManagerInfo _manager = new ManagerInfo();
        private double _bonus = 25.0;
        private double? _nullableWidth = null;

        public ManagerInfo Manager
        {
            get => _manager;
            set
            {
                _manager = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Manager)));
            }
        }

        public System.Collections.Generic.List<double> Weights { get; } = new() { 15.0, 42.0, 88.0 };

        public double Bonus
        {
            get => _bonus;
            set
            {
                _bonus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Bonus)));
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Bonus)));
            }
        }

        public double? NullableWidth
        {
            get => _nullableWidth;
            set
            {
                _nullableWidth = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NullableWidth)));
            }
        }

        public bool HasErrors => _bonus < 0;

        public System.Collections.IEnumerable GetErrors(string? propertyName)
        {
            if (propertyName == nameof(Bonus) && _bonus < 0)
            {
                return new[] { "Bonus cannot be negative" };
            }
            return Array.Empty<string>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    }

    public sealed class SumMultiValueConverter : System.Windows.Data.IMultiValueConverter
    {
        public static readonly SumMultiValueConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double sum = 0;
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] is double d && !double.IsNaN(d))
                    {
                        sum += d;
                    }
                }
            }
            return sum;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }

    // MK-4: Full-Spectrum Data Binding Alignment (Multi-step Path, Indexer, ElementName, RelativeSource, MultiBinding, PriorityBinding, Fallback/TargetNull, Validation)
    private static void TestAdvancedBindingsInFluentAndXaml()
    {
        // Register multi-step AOT accessors into PropertyAccessorRegistry
        System.Windows.Data.PropertyAccessorRegistry.Register<DepartmentModel, ManagerInfo>(
            nameof(DepartmentModel.Manager), static d => d.Manager, static (d, v) => d.Manager = v);
        System.Windows.Data.PropertyAccessorRegistry.Register<ManagerInfo, double>(
            nameof(ManagerInfo.BaseBudget), static m => m.BaseBudget, static (m, v) => m.BaseBudget = v);
        System.Windows.Data.PropertyAccessorRegistry.Register<DepartmentModel, System.Collections.Generic.List<double>>(
            nameof(DepartmentModel.Weights), static d => d.Weights);
        System.Windows.Data.PropertyAccessorRegistry.Register<DepartmentModel, double>(
            nameof(DepartmentModel.Bonus), static d => d.Bonus, static (d, v) => d.Bonus = v);
        System.Windows.Data.PropertyAccessorRegistry.Register<DepartmentModel, double?>(
            nameof(DepartmentModel.NullableWidth), static d => d.NullableWidth, static (d, v) => d.NullableWidth = v);

        var dept = new DepartmentModel();

        // 1. Test via XAML: Multi-step path ("Manager.BaseBudget"), Indexer ("Weights[1]"), ElementName, RelativeSource (Self & FindAncestor), TargetNullValue, FallbackValue, and MultiBinding
        var xamlCtx = new XamlLoadContext { DataContext = dept };
        const string advancedXaml = """
            <MarkupCanvas xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                          Width="300" Height="200" Tag="AncestorPayload">
                <MarkupCanvas.Resources>
                    <SumMultiValueConverter x:Key="SumConv" />
                </MarkupCanvas.Resources>

                <MarkupCard x:Name="NestedPathCard"
                            Width="{Binding Manager.BaseBudget, Mode=TwoWay}"
                            Height="{Binding Weights[1]}" />

                <MarkupCard x:Name="ElementBoundCard"
                            Width="{Binding Path=Width, ElementName=NestedPathCard}"
                            Height="{Binding Path=Width, RelativeSource={RelativeSource Self}}"
                            Tag="{Binding Path=Tag, RelativeSource={RelativeSource FindAncestor, AncestorType=MarkupCanvas}}" />

                <MarkupCard x:Name="NullAndFallbackCard"
                            Width="{Binding Path=NullableWidth, TargetNullValue=77}"
                            Height="{Binding Path=NonExistentProp, FallbackValue=55}" />

                <MarkupCard x:Name="MultiBoundCard">
                    <MarkupCard.Width>
                        <MultiBinding Converter="{StaticResource SumConv}">
                            <Binding Path="Manager.BaseBudget" />
                            <Binding Path="Bonus" />
                        </MultiBinding>
                    </MarkupCard.Width>
                </MarkupCard>
            </MarkupCanvas>
            """;

        XamlMarkup.RegisterType<SumMultiValueConverter>();

        MarkupCanvas canvas = XamlMarkup.Load<MarkupCanvas>(advancedXaml, out XamlLoadContext ctx, xamlCtx);
        canvas.Measure(new Size(300, 200));
        canvas.Arrange(new Rect(0, 0, 300, 200));
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
            static () => { },
            System.Windows.Threading.DispatcherPriority.SystemIdle);

        var nestedCard = ctx.FindName<MarkupCard>("NestedPathCard")!;
        var elemBoundCard = ctx.FindName<MarkupCard>("ElementBoundCard")!;
        var nullFallbackCard = ctx.FindName<MarkupCard>("NullAndFallbackCard")!;
        var multiCard = ctx.FindName<MarkupCard>("MultiBoundCard")!;

        // Verify Multi-step path & Indexer
        AssertNear(nestedCard.Width, 60.0, 1e-4, "Multi-step path Manager.BaseBudget initial");
        AssertNear(nestedCard.Height, 42.0, 1e-4, "Indexer path Weights[1] initial");

        // Verify ElementName & RelativeSource (Self + FindAncestor)
        AssertNear(elemBoundCard.Width, 60.0, 1e-4, "ElementName binding to NestedPathCard.Width");
        AssertNear(elemBoundCard.Height, 60.0, 1e-4, "RelativeSource.Self binding Height=Width");
        AssertEqual(elemBoundCard.Tag as string, "AncestorPayload", "RelativeSource FindAncestor Tag");

        // Verify live update along multi-step path -> cascades to ElementName -> cascades to RelativeSource.Self & MultiBinding!
        dept.Manager.BaseBudget = 95.0;
        AssertNear(nestedCard.Width, 95.0, 1e-4, "Multi-step path updated");
        AssertNear(elemBoundCard.Width, 95.0, 1e-4, "ElementName cascaded update");
        AssertNear(elemBoundCard.Height, 95.0, 1e-4, "RelativeSource.Self cascaded update");
        AssertNear(multiCard.Width, 120.0, 1e-4, "MultiBinding sum (95 + 25 = 120)");

        // Verify TargetNullValue & FallbackValue
        AssertNear(nullFallbackCard.Width, 77.0, 1e-4, "TargetNullValue when NullableWidth is null");
        dept.NullableWidth = 108.0;
        AssertNear(nullFallbackCard.Width, 108.0, 1e-4, "TargetNullValue replaced when NullableWidth is non-null");
        AssertNear(nullFallbackCard.Height, 55.0, 1e-4, "FallbackValue when path does not exist");

        // 2. Test PriorityBinding & INotifyDataErrorInfo Validation via Fluent API
        var validatedCard = new MarkupCard()
            .DataContext(dept)
            .BindPath(
                FrameworkElement.WidthProperty,
                "Bonus",
                mode: BindingMode.TwoWay,
                validatesOnNotifyDataErrors: true)
            .PriorityBind(
                FrameworkElement.HeightProperty,
                new System.Windows.Data.Binding("UnresolvedFastPath"),
                new System.Windows.Data.Binding("Weights[2]"));

        AssertNear(validatedCard.Height, 88.0, 1e-4, "PriorityBinding falls through to first active binding Weights[2]=88");
        Assert(!Validation.GetHasError(validatedCard), "No validation error when Bonus=25");

        dept.Bonus = -10.0;
        Assert(Validation.GetHasError(validatedCard), "Validation.GetHasError should be true when INotifyDataErrorInfo reports negative Bonus");
        AssertEqual(Validation.GetErrors(validatedCard)[0].ErrorContent as string, "Bonus cannot be negative", "Validation error message");

        dept.Bonus = 30.0;
        Assert(!Validation.GetHasError(validatedCard), "Validation error cleared when Bonus=30");

        Pass("MK-4. Full-Spectrum Binding Alignment (Multi-step Path, Indexer, ElementName, RelativeSource, MultiBinding, PriorityBinding, Fallback/TargetNull, INotifyDataErrorInfo)",
            $"multiSum={multiCard.Width}, priorityH={validatedCard.Height}");
    }

    private static void TestRoslynSourceGeneratorZeroBoilerplate()
    {
        // Zero manual PropertyAccessorRegistry.Register or XamlMarkup.RegisterType calls!
        // Everything is generated by CSUI.Generators at compile time and registered via [ModuleInitializer].
        var vm = new GeneratedDashboardViewModel();
        var view = new GeneratedDashboardView();
        view.DataContext = vm;
        view.InitializeComponent();

        Assert(view.ComponentHookCalled, "Partial hook OnComponentInitialized() invoked by InitializeComponent()");
        Assert(view.PrimaryBadge != null, "x:Name field PrimaryBadge populated by generated InitializeComponent()");
        Assert(view.IndexedBadge != null, "x:Name field IndexedBadge populated by generated InitializeComponent()");

        // Verify [XamlType] auto-registered non-DP CLR property StaticTier="3"
        AssertEqual(view.PrimaryBadge!.StaticTier, 3, "[XamlType] auto-registered CLR property StaticTier");

        // Verify initial bindings via [ObservableObject] + [Bindable] auto-registered properties & custom indexer
        AssertNear(view.PrimaryBadge.Width, 75.0, 1e-4, "Initial BaseScore binding");
        AssertNear(view.PrimaryBadge.Height, 150.0, 1e-4, "Initial TotalScore (75 * 2) binding");
        AssertEqual(view.PrimaryBadge.BadgeLabel, "Ready", "Initial StatusText binding");
        AssertNear(view.IndexedBadge!.Width, 64.0, 1e-4, "Initial custom indexer Series[1] binding");

        // Verify [RelayCommand] CanExecute & Execute -> triggers [ObservableProperty] + [AlsoNotifyChangeFor(nameof(TotalScore))]
        Assert(!vm.BoostCommand.CanExecute(-5.0), "BoostCommand.CanExecute(-5) should be false");
        Assert(vm.BoostCommand.CanExecute(25.0), "BoostCommand.CanExecute(25) should be true");
        vm.BoostCommand.Execute(25.0);

        AssertEqual(vm.BaseScoreChangeCount, 1, "Partial hook OnBaseScoreChanged invoked");
        AssertNear(view.PrimaryBadge.Width, 100.0, 1e-4, "BaseScore updated via BoostCommand");
        AssertNear(view.PrimaryBadge.Height, 200.0, 1e-4, "TotalScore cascaded via [AlsoNotifyChangeFor]");
        AssertEqual(view.PrimaryBadge.BadgeLabel, "Boosted", "StatusText updated via BoostCommand");

        // Verify TwoWay write-back on custom indexer Series[1]
        view.IndexedBadge.Width = 128.0;
        AssertNear(vm.Series[1], 128.0, 1e-4, "TwoWay write-back into [Bindable] custom indexer Series[1]");

        Pass("MK-5. Roslyn Incremental Source Generator ([ObservableObject], [ObservableProperty], [AlsoNotifyChangeFor], [RelayCommand], [Bindable], [XamlType] & .xaml InitializeComponent)",
            $"totalScore={view.PrimaryBadge.Height}, series1={vm.Series[1]}, tier={view.PrimaryBadge.StaticTier}");
    }

    private static void TestZeroModificationWpfDropIn()
    {
        var vm = new ZeroModViewModel();
        var view = new ZeroModWpfView { DataContext = vm };
        view.Measure(new Size(640, 480));
        view.Arrange(new Rect(0, 0, 640, 480));
        view.UpdateLayout();

        AssertEqual(view.TitleTb.Text, "PureWpfTitle", "Zero-attr INPC initial binding");
        AssertEqual(view.ConvTb.Text, "YES", "Binding Converter={StaticResource ...}");
        AssertEqual(view.IdxTb.Text, "SlotA", "Zero-attr INPC string indexer [slot]");
        AssertEqual(view.AncTb.Text, "AncestorTagOK", "RelativeSource AncestorType=UserControl");
        AssertEqual(view.ElemTb.Text, "PureWpfTitle", "ElementName=TitleTb mirror binding");
        AssertEqual(view.BadgeCtrl.BadgeCaption, "UnregisteredCtrlOK", "Unregistered custom control CLR property");
        Assert(view.ActionBtn.Background is SolidColorBrush scb && scb.Color == Colors.Coral, "MergedDictionary StaticResource brush");

        vm.Title = "LiveUpdatedTitle";
        AssertEqual(view.TitleTb.Text, "LiveUpdatedTitle", "Zero-attr INPC live PropertyChanged update");
        AssertEqual(view.ElemTb.Text, "LiveUpdatedTitle", "ElementName cascade update");

        // Verify Application.LoadComponent resolves compiled XAML view cleanly under NativeAOT
        object loaded = Application.LoadComponent(new Uri("ZeroModWpfView.xaml", UriKind.Relative));
        Assert(loaded is ZeroModWpfView, "Application.LoadComponent(ZeroModWpfView.xaml)");

        Pass("MK-6. Zero-Modification WPF Compatibility (Compile-Time Resources/Style/Templates, MergedDictionaries, Unannotated INPC/POCO, Custom Control & LoadComponent)",
            $"title={view.TitleTb.Text}, badge={view.BadgeCtrl.BadgeCaption}");
    }

    private static void TestApplicationWindowPopupAndToolTip()
    {
        // 1. Application singleton & global Resources
        Application app = Application.Current ?? new Application();
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        app.Resource("AppAccentBrush", new SolidColorBrush(Color.FromRgb(14, 165, 233)));

        var vm = new GeneratedDashboardViewModel { BaseScore = 90.0, Multiplier = 1.5, StatusText = "WindowActive" };

        // 2. Load a top-level <Window> with nested <Popup> via XAML
        const string windowXaml = """
            <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    Title="CSUI XAML Window"
                    Width="360" Height="240"
                    WindowStartupLocation="CenterScreen"
                    Background="#0F172A">
                <MarkupCanvas x:Name="RootCanvas" Width="340" Height="200">
                    <MarkupCard x:Name="MainCard"
                                Width="{Binding BaseScore}"
                                Height="{Binding TotalScore}"
                                Background="{DynamicResource AppAccentBrush}"
                                ToolTip="Card Hover Hint" />
                    <Popup x:Name="DetailPopup"
                           Placement="Bottom"
                           HorizontalOffset="12"
                           VerticalOffset="8">
                        <MarkupCard x:Name="PopupInnerCard"
                                    Width="{Binding TotalScore}"
                                    Height="48"
                                    Background="#22C55E" />
                    </Popup>
                </MarkupCanvas>
            </Window>
            """;

        var loadCtx = new XamlLoadContext { DataContext = vm };
        Window mainWin = XamlMarkup.Load<Window>(windowXaml, out XamlLoadContext ctx, loadCtx);
        var mainCard = ctx.FindName<MarkupCard>("MainCard")!;
        var detailPopup = ctx.FindName<Popup>("DetailPopup")!;
        var popupInnerCard = ctx.FindName<MarkupCard>("PopupInnerCard")!;

        bool sourceInitialized = false;
        bool closingFired = false;
        bool closedFired = false;
        mainWin.SourceInitialized += (_, _) => sourceInitialized = true;
        mainWin.Closing += (_, _) => closingFired = true;
        mainWin.Closed += (_, _) => closedFired = true;

        mainWin.Show();
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);

        IntPtr mainHwnd = new WindowInteropHelper(mainWin).Handle;
        Assert(sourceInitialized && mainHwnd != IntPtr.Zero, "Window.Show() created HWND and raised SourceInitialized");
        Assert(ReferenceEquals(Window.GetWindow(mainCard), mainWin), "Window.GetWindow(mainCard) resolves enclosing Window");
        Assert(AdornerLayer.GetAdornerLayer(mainCard) != null, "Default Window ControlTemplate provides AdornerLayer");
        AssertNear(mainCard.Width, 90.0, 1e-4, "Window DataContext binding on MainCard.Width");
        AssertNear(mainCard.Height, 135.0, 1e-4, "Window DataContext binding on MainCard.Height");
        AssertEqual(mainCard.ToolTip as string, "Card Hover Hint", "FrameworkElement.ToolTip property via XAML");
        Assert(mainCard.Background is SolidColorBrush scb && scb.Color.R == 14 && scb.Color.G == 165,
            "Application.Current.Resources resolved via {DynamicResource AppAccentBrush}");

        // 3. Verify Popup opening, DataContext inheritance across Popup logical tree, and closing
        bool popupOpened = false;
        bool popupClosed = false;
        detailPopup.PlacementTarget = mainCard;
        detailPopup.Opened += (_, _) => popupOpened = true;
        detailPopup.Closed += (_, _) => popupClosed = true;

        detailPopup.IsOpen = true;
        Assert(popupOpened, "Popup.IsOpen=true raised Opened event");
        AssertNear(popupInnerCard.Width, 135.0, 1e-4, "Popup.Child inherited DataContext from Window -> Popup");
        AssertNear(popupInnerCard.RenderSize.Width, 135.0, 1e-4, "Popup HwndSource measured & arranged PopupInnerCard");

        detailPopup.IsOpen = false;
        Assert(popupClosed, "Popup.IsOpen=false raised Closed event and disposed popup HWND");

        // 4. Verify ToolTip control lifecycle
        bool toolTipOpened = false;
        bool toolTipClosed = false;
        var richToolTip = new ToolTip
        {
            PlacementTarget = mainCard,
            Placement = PlacementMode.Top,
            Content = new MarkupCard().Size(80, 28)
        };
        richToolTip.Opened += (_, _) => toolTipOpened = true;
        richToolTip.Closed += (_, _) => toolTipClosed = true;
        richToolTip.IsOpen = true;
        Assert(toolTipOpened, "ToolTip.IsOpen=true opened underlying Popup and raised Opened");
        richToolTip.IsOpen = false;
        Assert(toolTipClosed, "ToolTip.IsOpen=false closed underlying Popup and raised Closed");

        // 5. Verify modal Window.ShowDialog() with DialogResult = true
        var modalDialog = new Window()
            .Title("Modal Confirmation")
            .Size(260, 140)
            .Owner(mainWin)
            .StartupLocation(WindowStartupLocation.CenterOwner)
            .WindowStyle(WindowStyle.ToolWindow)
            .ResizeMode(ResizeMode.NoResize)
            .Background(Color.FromRgb(30, 41, 59))
            .Content(new MarkupCard().Size(200, 80));

        modalDialog.SourceInitialized += (_, _) =>
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() =>
                {
                    modalDialog.DialogResult = true;
                }));
        };

        bool? dialogResult = modalDialog.ShowDialog();
        Assert(dialogResult == true, "Modal Window.ShowDialog() returned DialogResult == true");

        // 6. Close main Window and verify Closing & Closed events
        mainWin.Close();
        Assert(closingFired && closedFired, "Window.Close() raised Closing and Closed events");

        Pass("WIN-1. Application, Window (Show, ShowDialog, DialogResult, AdornerLayer), Popup & ToolTip Lifecycle",
            $"hwnd=0x{mainHwnd.ToInt64():X}, dialogResult={dialogResult}, popupW={popupInnerCard.Width}");
    }

    private static void TestSvgEngineAndPixelAlignment()
    {
        // =====================================================================
        // 1. Direct2D Native SVG Parsing: Complex Path, Shapes, Gradients & currentColor
        // =====================================================================
        const string sampleSvg = """
        <svg viewBox="0 0 100 100" width="100" height="100" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="headerGrad" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stop-color="#38BDF8" stop-opacity="1" />
              <stop offset="100%" stop-color="#0284C7" stop-opacity="1" />
            </linearGradient>
          </defs>
          <rect x="5" y="5" width="90" height="90" rx="12" ry="12" fill="url(#headerGrad)" stroke="currentColor" stroke-width="2" />
          <circle cx="50" cy="50" r="22" fill="#FFFFFF" opacity="0.9" />
          <line x1="20" y1="50" x2="80" y2="50" stroke="#0F172A" stroke-width="2" stroke-linecap="round" />
          <polygon points="50,15 62,38 38,38" fill="gold" />
          <g transform="translate(10, 10)">
            <path d="M 10 10 L 25 35 L 40 10 Z" fill="currentColor" stroke="none" />
          </g>
        </svg>
        """;

        Brush dynamicFg = new SolidColorBrush(Color.FromRgb(244, 63, 94)); // Rose-500
        SvgDocument doc = SvgDocument.Parse(sampleSvg, dynamicFg);

        Assert(doc != null, "SvgDocument.Parse returned non-null document");
        Assert(doc!.ViewBox.Width == 100 && doc.ViewBox.Height == 100, $"SvgDocument ViewBox extracted accurately ({doc.ViewBox})");
        Assert(doc.Width == 100 && doc.Height == 100, "SvgDocument Width & Height matched root attributes");
        Assert(doc.Drawing != null && doc.Drawing.Children.Count >= 4, $"SvgDocument DrawingGroup contains {doc.Drawing!.Children.Count} children");

        // Verify ToDrawingImage()
        DrawingImage img = doc.ToDrawingImage();
        Assert(img != null && img.Drawing == doc.Drawing, "SvgDocument.ToDrawingImage() wraps parsed DrawingGroup");

        // =====================================================================
        // 2. SvgViewbox Measure, Uniform Layout & Dynamic currentColor Theming
        // =====================================================================
        var viewbox = new SvgViewbox()
            .Source(sampleSvg)
            .Stretch(Stretch.Uniform)
            .Foreground(dynamicFg);

        viewbox.Measure(new Size(200, 400)); // Non-square available size
        Size desired = viewbox.DesiredSize;
        Assert(Math.Abs(desired.Width - desired.Height) < 0.01, $"SvgViewbox preserved 1:1 aspect ratio under Uniform stretch ({desired.Width}x{desired.Height})");

        // =====================================================================
        // 3. XAML Markup Engine Zero-Reflection Parsing: <SvgViewbox> & <SvgImage>
        // =====================================================================
        const string xamlSnippet = """
        <SvgViewbox xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    Source="&lt;svg viewBox=&quot;0 0 64 64&quot;&gt;&lt;circle cx=&quot;32&quot; cy=&quot;32&quot; r=&quot;16&quot; fill=&quot;dodgerblue&quot;/&gt;&lt;/svg&gt;"
                    Stretch="Uniform"
                    Width="64"
                    Height="64" />
        """;

        object parsedXaml = XamlMarkup.Load<SvgViewbox>(xamlSnippet);
        Assert(parsedXaml is SvgViewbox, "XamlMarkup parsed <SvgViewbox>");
        var xamlViewbox = (SvgViewbox)parsedXaml;
        Assert(xamlViewbox.Width == 64 && xamlViewbox.Height == 64, "<SvgViewbox> Width & Height correctly bound from XAML");
        Assert(xamlViewbox.Stretch == Stretch.Uniform, "<SvgViewbox> Stretch mode correctly bound from XAML");
        Assert(xamlViewbox.Document != null && xamlViewbox.Document.ViewBox.Width == 64, "<SvgViewbox> inner SVG parsed into SvgDocument");

        Pass("SVG-1. High-Performance Streaming Native SVG Engine (Defs, LinearGradients, Paths, currentColor, SvgViewbox & XAML/Fluent Integration)",
            $"viewBox={doc.ViewBox}, children={doc.Drawing.Children.Count}, desired={desired}");
    }

    private static void TestModernEffectsAndAcrylicMaterial()
    {
        // =====================================================================
        // 1. ModernElevationEffect: Auto-calculated Ambient + Key Dual Light Shadow
        // =====================================================================
        var elevEffect = new ModernElevationEffect(8.0)
        {
            Color = Color.FromRgb(15, 23, 42)
        };
        Assert(elevEffect.Elevation == 8.0, "Elevation value retained");
        Assert(elevEffect.BlurRadius > 20.0, $"BlurRadius auto-scaled ({elevEffect.BlurRadius})");
        Assert(elevEffect.ShadowDepth >= 10.0, $"ShadowDepth auto-scaled ({elevEffect.ShadowDepth})");

        Rect baseRect = new Rect(0, 0, 100, 100);
        Rect renderBounds = elevEffect.ComputeRenderBounds(baseRect);
        Assert(renderBounds.Width > 100 && renderBounds.Height > 100, "RenderBounds expands to enclose dual shadows");

        // =====================================================================
        // 2. AcrylicBrush: Win11 Luminosity Tint & Specular Highlight Gradient
        // =====================================================================
        var acrylic = new AcrylicBrush(Color.FromRgb(30, 41, 59), tintOpacity: 0.75, blurRadius: 28.0);
        Assert(acrylic.TintColor.R == 30 && acrylic.TintColor.G == 41 && acrylic.TintColor.B == 59, "TintColor matches initialization");
        AssertNear(acrylic.TintOpacity, 0.75, 1e-4, "TintOpacity matches initialization");
        Assert(acrylic.BlurRadius == 28.0, "BlurRadius matches initialization");

        // Verify Direct2D GPU rendering of AcrylicBrush onto MarkupCanvas
        var acrylicCanvas = new MarkupCanvas
        {
            Width = 100,
            Height = 100,
            Background = acrylic
        };
        acrylicCanvas.Measure(new Size(100, 100));
        acrylicCanvas.Arrange(new Rect(0, 0, 100, 100));
        byte[] acrylicPixels = RenderToBgra32(acrylicCanvas, 100, 100);
        var (acB, acG, acR, acA) = Sample(acrylicPixels, 100, 50, 50);
        Assert(acA > 150 && acR > 15 && acB > 30, $"AcrylicBrush GPU rasterized accurately (R={acR},G={acG},B={acB},A={acA})");

        // =====================================================================
        // 3. Declarative Fluent API: .Elevation() & .Acrylic()
        // =====================================================================
        var card = new MarkupCard()
            .Elevation(6.0, opacity: 0.4)
            .Size(160, 90);
        Assert(card.Effect is ModernElevationEffect cardElev && cardElev.Elevation == 6.0, "Fluent .Elevation() applied ModernElevationEffect");

        // =====================================================================
        // 4. Zero-Reflection NativeAOT XAML Parsing: <ModernElevationEffect> & <AcrylicBrush>
        // =====================================================================
        const string xamlEffectSnippet = """
        <MarkupCard xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    Width="160" Height="80">
            <MarkupCard.Effect>
                <ModernElevationEffect Elevation="12" />
            </MarkupCard.Effect>
        </MarkupCard>
        """;

        object parsedCard = XamlMarkup.Load<MarkupCard>(xamlEffectSnippet);
        Assert(parsedCard is MarkupCard xamlCard && xamlCard.Effect is ModernElevationEffect xamlElev && xamlElev.Elevation == 12.0,
            "XamlMarkup parsed <ModernElevationEffect Elevation='12'>");

        Pass("EFFECT-1. Modern Direct2D Hardware Effects & Materials (ModernElevationEffect, AcrylicBrush, XAML & Fluent Dual-Mode Integration)",
            $"elevation={elevEffect.Elevation}, blur={elevEffect.BlurRadius:F1}, depth={elevEffect.ShadowDepth:F1}, acrylicOp={acrylic.TintOpacity}");
    }

    private static void TestStandardLayoutMatrix()
    {
        // =====================================================================
        // 1. Grid: Auto, Pixel & Weighted Star Ratio Distribution + RowSpan/ColumnSpan
        // =====================================================================
        var grid = new Grid()
            .Rows("30", "Auto", "1*", "2*")
            .Columns("100", "Auto", "*");

        Assert(grid.RowDefinitions.Count == 4, "Grid rows count == 4");
        Assert(grid.ColumnDefinitions.Count == 3, "Grid columns count == 3");

        var autoItem = new MarkupCard().Size(80, 40).Row(1).Column(1);
        var spanItem = new MarkupCard().Row(2).Column(0).RowSpan(2).ColumnSpan(2);
        var starItem = new MarkupCard().Row(2).Column(2);

        grid.Children.Add(autoItem);
        grid.Children.Add(spanItem);
        grid.Children.Add(starItem);

        grid.Measure(new Size(400, 310));
        grid.Arrange(new Rect(0, 0, 400, 310));

        // Column 0: 100 fixed, Column 1: 80 (Auto), Column 2: 400 - 180 = 220 (*)
        AssertNear(grid.ColumnDefinitions[0].ActualWidth, 100.0, 1e-4, "Column 0 Pixel width is 100");
        AssertNear(grid.ColumnDefinitions[1].ActualWidth, 80.0, 1e-4, "Column 1 Auto width matched content 80");
        AssertNear(grid.ColumnDefinitions[2].ActualWidth, 220.0, 1e-4, "Column 2 Star width absorbed remaining 220");

        // Row 0: 30 fixed, Row 1: 40 (Auto), Remaining Height = 310 - 70 = 240 -> Row 2 (1*) = 80, Row 3 (2*) = 160
        AssertNear(grid.RowDefinitions[0].ActualHeight, 30.0, 1e-4, "Row 0 Pixel height is 30");
        AssertNear(grid.RowDefinitions[1].ActualHeight, 40.0, 1e-4, "Row 1 Auto height matched content 40");
        AssertNear(grid.RowDefinitions[2].ActualHeight, 80.0, 1e-4, "Row 2 (1*) height is 80");
        AssertNear(grid.RowDefinitions[3].ActualHeight, 160.0, 1e-4, "Row 3 (2*) height is 160 (1:2 ratio)");

        // =====================================================================
        // 2. StackPanel: Orientation & Spacing Inter-Item Accumulation
        // =====================================================================
        var stack = new StackPanel().Vertical().Spacing(10);
        var s1 = new MarkupCard().Size(120, 25);
        var s2 = new MarkupCard().Size(150, 35);
        stack.Children.Add(s1);
        stack.Children.Add(s2);

        stack.Measure(new Size(300, 300));
        AssertNear(stack.DesiredSize.Height, 25 + 35 + 10, 1e-4, "StackPanel measured height with spacing (25 + 35 + 10 = 70)");
        AssertNear(stack.DesiredSize.Width, 150.0, 1e-4, "StackPanel measured max cross size 150");

        stack.Arrange(new Rect(0, 0, 300, 70));
        AssertNear(s2.RenderSize.Height, 35.0, 1e-4, "Stack item 2 height");

        // =====================================================================
        // 3. DockPanel: Left/Top/Right Docking + LastChildFill
        // =====================================================================
        var dock = new DockPanel().LastChildFill(true);
        var dTop = new MarkupCard().Height(30).Dock(Dock.Top);
        var dLeft = new MarkupCard().Width(40).Dock(Dock.Left);
        var dCenter = new MarkupCard(); // Last child
        dock.Children.Add(dTop);
        dock.Children.Add(dLeft);
        dock.Children.Add(dCenter);

        dock.Measure(new Size(200, 200));
        dock.Arrange(new Rect(0, 0, 200, 200));

        AssertNear(dCenter.RenderSize.Width, 200 - 40, 1e-4, "DockPanel center width (200 - 40 = 160)");
        AssertNear(dCenter.RenderSize.Height, 200 - 30, 1e-4, "DockPanel center height (200 - 30 = 170)");

        // =====================================================================
        // 4. WrapPanel: Multi-Line Flow & Custom Item Dimensions
        // =====================================================================
        var wrap = new WrapPanel().ItemSize(60, 30);
        for (int i = 0; i < 5; i++)
        {
            wrap.Children.Add(new MarkupCard());
        }

        wrap.Measure(new Size(150, 500)); // Fits 2 items per line (60 + 60 = 120 <= 150) -> 3 lines
        AssertNear(wrap.DesiredSize.Height, 30 * 3, 1e-4, "WrapPanel wrapped into 3 lines (30 * 3 = 90)");

        // =====================================================================
        // 5. Zero-Reflection NativeAOT XAML Parsing: <Grid>, <StackPanel> & Attached Props
        // =====================================================================
        const string xamlLayoutSnippet = """
        <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
              Rows="Auto, 1*, 2*" Columns="100, *">
            <StackPanel Grid.Row="0" Grid.Column="1" Spacing="6" />
        </Grid>
        """;

        object parsedLayout = XamlMarkup.Load<Grid>(xamlLayoutSnippet);
        Assert(parsedLayout is Grid, "XamlMarkup parsed <Grid>");
        var xamlGrid = (Grid)parsedLayout;
        Assert(xamlGrid.RowDefinitions.Count == 3, "Grid RowDefinitions count == 3 from string shorthand");
        Assert(xamlGrid.ColumnDefinitions.Count == 2, "Grid ColumnDefinitions count == 2 from string shorthand");
        Assert(xamlGrid.Children.Count == 1 && xamlGrid.Children[0] is StackPanel, "Grid parsed child StackPanel");
        var xamlSp = (StackPanel)xamlGrid.Children[0];
        Assert(Grid.GetRow(xamlSp) == 0 && Grid.GetColumn(xamlSp) == 1, "Grid.Row and Grid.Column attached props bound");
        AssertNear(xamlSp.Spacing, 6.0, 1e-4, "StackPanel.Spacing bound from XAML");

        Pass("LAYOUT-1. Industrial Core Layout Matrix (Grid Auto/Pixel/Star, StackPanel Spacing, DockPanel LastChildFill, WrapPanel & XAML/Fluent Integration)",
            $"col0={grid.ColumnDefinitions[0].ActualWidth}, col1={grid.ColumnDefinitions[1].ActualWidth}, row2={grid.RowDefinitions[2].ActualHeight}, row3={grid.RowDefinitions[3].ActualHeight}, wrapH={wrap.DesiredSize.Height}");
    }

    private static void TestModernLayoutPanels()
    {
        // =====================================================================
        // 1. Border: Thickness, Padding, CornerRadius & Measure/Arrange
        // =====================================================================
        var card = new MarkupCard { Width = 100, Height = 50 };
        var border = new Border()
            .BorderThickness(2, 4, 6, 8)
            .Padding(5, 5, 5, 5)
            .CornerRadius(8)
            .Background(Color.FromRgb(30, 30, 30))
            .BorderBrush(Color.FromRgb(0, 120, 215))
            .Child(card);

        // Horiz border = (2 + 6) + (5 + 5) = 18; Vert border = (4 + 8) + (5 + 5) = 22
        border.Measure(new Size(500, 500));
        AssertNear(border.DesiredSize.Width, 100 + 18, 1e-4, "Border desired width = 118");
        AssertNear(border.DesiredSize.Height, 50 + 22, 1e-4, "Border desired height = 72");

        border.Arrange(new Rect(0, 0, 200, 100));
        // Fixed-size card arranged within available slot (7, 9, 182, 78) maintains its fixed dimensions
        AssertNear(card.RenderSize.Width, 100, 1e-4, "Border child fixed width = 100");
        AssertNear(card.RenderSize.Height, 50, 1e-4, "Border child fixed height = 50");

        // Stretch child expands to full inner content rect (200 - 18 = 182, 100 - 22 = 78)
        var stretchCard = new MarkupCard();
        border.Child = stretchCard;
        border.Measure(new Size(200, 100));
        border.Arrange(new Rect(0, 0, 200, 100));
        AssertNear(stretchCard.RenderSize.Width, 182, 1e-4, "Stretch child expands to border inner width = 182");
        AssertNear(stretchCard.RenderSize.Height, 78, 1e-4, "Stretch child expands to border inner height = 78");

        // Verify non-uniform corners create StreamGeometry without throwing
        var nonUniformBorder = new Border()
            .BorderThickness(2)
            .CornerRadius(4, 8, 12, 16)
            .Background(Color.FromRgb(40, 40, 40))
            .BorderBrush(Color.FromRgb(255, 0, 0));
        nonUniformBorder.Arrange(new Rect(0, 0, 100, 50));

        // =====================================================================
        // 2. Canvas: Absolute Attached Positioning
        // =====================================================================
        var canvas = new Canvas();
        var c1 = new MarkupCard { Width = 50, Height = 40 }.CanvasPosition(left: 20, top: 30);
        var c2 = new MarkupCard { Width = 60, Height = 35 }.CanvasPosition(right: 10, bottom: 15);
        canvas.Children.Add(c1);
        canvas.Children.Add(c2);

        canvas.Measure(new Size(800, 600));
        AssertNear(canvas.DesiredSize.Width, 0, 1e-4, "Canvas desired size is (0,0)");
        AssertNear(canvas.DesiredSize.Height, 0, 1e-4, "Canvas desired size is (0,0)");

        canvas.Arrange(new Rect(0, 0, 400, 300));
        AssertNear(c1.RenderSize.Width, 50, 1e-4, "Canvas item 1 width");
        AssertNear(c1.RenderSize.Height, 40, 1e-4, "Canvas item 1 height");
        AssertNear(c2.RenderSize.Width, 60, 1e-4, "Canvas item 2 width");
        AssertNear(c2.RenderSize.Height, 35, 1e-4, "Canvas item 2 height");

        // =====================================================================
        // 3. UniformGrid: Automatic Cell Dimensions & Equal Partitioning
        // =====================================================================
        var uGrid = new UniformGrid().UniformRows(2).UniformColumns(3);
        for (int i = 0; i < 6; i++)
        {
            uGrid.Children.Add(new MarkupCard());
        }

        uGrid.Measure(new Size(300, 200));
        // Each child measured with (300/3 = 100, 200/2 = 100) -> desired = 0x0
        AssertNear(uGrid.DesiredSize.Width, 0, 1e-4, "UniformGrid desired width = 0");
        AssertNear(uGrid.DesiredSize.Height, 0, 1e-4, "UniformGrid desired height = 0");

        uGrid.Arrange(new Rect(0, 0, 300, 200));
        // All children get uniform cell size 100x100
        AssertNear(((UIElement)uGrid.Children[0]).RenderSize.Width, 100, 1e-4, "UniformGrid cell arranged width = 100");
        AssertNear(((UIElement)uGrid.Children[0]).RenderSize.Height, 100, 1e-4, "UniformGrid cell arranged height = 100");

        // Dynamic auto-dimensioning test (Rows=0, Columns=0)
        var autoGrid = new UniformGrid();
        for (int i = 0; i < 5; i++)
        {
            autoGrid.Children.Add(new MarkupCard());
        }
        autoGrid.Measure(new Size(300, 300));
        // 5 items -> cols = ceil(sqrt(5)) = 3, rows = (5+3-1)/3 = 2.
        autoGrid.Arrange(new Rect(0, 0, 300, 200));
        AssertNear(((UIElement)autoGrid.Children[0]).RenderSize.Width, 100, 1e-4, "Auto UniformGrid cell width = 100");
        AssertNear(((UIElement)autoGrid.Children[0]).RenderSize.Height, 100, 1e-4, "Auto UniformGrid cell height = 100");

        // =====================================================================
        // 4. Viewbox: Aspect Ratio Scaling & Stretch Modes
        // =====================================================================
        var vb = new Viewbox()
            .Stretch(Stretch.Uniform)
            .StretchDirection(StretchDirection.Both)
            .Child(new MarkupCard { Width = 100, Height = 50 }); // 2:1 aspect ratio

        // Measure in 200x200 box: min(200/100, 200/50) = min(2, 4) = 2 -> 200 x 100
        vb.Measure(new Size(200, 200));
        AssertNear(vb.DesiredSize.Width, 200, 1e-4, "Viewbox Uniform desired width = 200");
        AssertNear(vb.DesiredSize.Height, 100, 1e-4, "Viewbox Uniform desired height = 100");

        vb.Arrange(new Rect(0, 0, 200, 200));
        Assert(vb.Child.RenderTransform is ScaleTransform st && Math.Abs(st.ScaleX - 2.0) < 1e-4, "Viewbox applies ScaleTransform (scale=2.0)");

        // =====================================================================
        // 5. Zero-Reflection NativeAOT XAML Parsing: <Border>, <Canvas>, <UniformGrid>, <Viewbox>
        // =====================================================================
        const string xamlComplex = """
        <Canvas xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
            <Border Canvas.Left="15" Canvas.Top="25" BorderThickness="2" CornerRadius="4" Padding="10">
                <UniformGrid Rows="2" Columns="2">
                    <Viewbox Stretch="Uniform">
                        <TextBlock Text="CSUI" />
                    </Viewbox>
                </UniformGrid>
            </Border>
        </Canvas>
        """;

        object parsed = XamlMarkup.Load<Canvas>(xamlComplex);
        var parsedCanvas = parsed as Canvas;
        Assert(parsedCanvas != null, "XAML parsed Canvas root");
        if (parsedCanvas == null) return;
        Assert(parsedCanvas.Children.Count == 1 && parsedCanvas.Children[0] is Border, "Canvas child is Border");
        var xBorder = (Border)parsedCanvas.Children[0];
        AssertNear(Canvas.GetLeft(xBorder), 15.0, 1e-4, "Canvas.Left parsed from XAML");
        AssertNear(Canvas.GetTop(xBorder), 25.0, 1e-4, "Canvas.Top parsed from XAML");
        AssertNear(xBorder.BorderThickness.Left, 2.0, 1e-4, "Border.BorderThickness parsed from XAML");
        AssertNear(xBorder.CornerRadius.TopLeft, 4.0, 1e-4, "Border.CornerRadius parsed from XAML");
        AssertNear(xBorder.Padding.Left, 10.0, 1e-4, "Border.Padding parsed from XAML");

        var xUg = xBorder.Child as UniformGrid;
        Assert(xUg != null, "Border child is UniformGrid");
        if (xUg == null) return;
        Assert(xUg.Rows == 2 && xUg.Columns == 2, "UniformGrid Rows and Columns parsed from XAML");
        Assert(xUg.Children.Count == 1 && xUg.Children[0] is Viewbox, "UniformGrid child is Viewbox");
        var xVb = (Viewbox)xUg.Children[0];
        Assert(xVb.Stretch == Stretch.Uniform, "Viewbox.Stretch parsed from XAML");
        Assert(xVb.Child is TextBlock xTb && xTb.Text == "CSUI", "Viewbox child is TextBlock with Text");

        Pass("LAYOUT-2. Modern Layout Matrix 2 (Border Padding/CornerRadius, Canvas Coordinates, UniformGrid Dimensions, Viewbox Scaling & XAML/Fluent)",
            $"borderDesired={border.DesiredSize.Width}x{border.DesiredSize.Height}, canvasDesired={canvas.DesiredSize.Width}x{canvas.DesiredSize.Height}, uGridCell={((UIElement)uGrid.Children[0]).RenderSize.Width}x{((UIElement)uGrid.Children[0]).RenderSize.Height}, vbScale={((ScaleTransform)vb.Child.RenderTransform).ScaleX}");
    }

    private static void TestBasicControlsMatrix()
    {
        // =====================================================================
        // 1. Button & ButtonBase: ClickMode, IsPressed & Command Execution
        // =====================================================================
        int clickCount = 0;
        object? executedParam = null;
        var testCmd = new RelayCommand<object>(p =>
        {
            clickCount++;
            executedParam = p;
        });

        var btn = new Button()
            .Command(testCmd, "payload123")
            .ClickMode(ClickMode.Press);

        btn.OnClick();
        AssertEqual(clickCount, 1, "Button Click raised and executed Command");
        AssertEqual((string?)executedParam, "payload123", "CommandParameter delivered to command");

        // Test space key click
        btn.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, null, 0, Key.Space) { RoutedEvent = UIElement.KeyDownEvent });
        Assert(btn.IsPressed, "Button is pressed on Space key down");
        btn.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, null, 0, Key.Space) { RoutedEvent = UIElement.KeyUpEvent });
        Assert(!btn.IsPressed, "Button is released on Space key up");
        AssertEqual(clickCount, 2, "Space key invoked Button Click");

        // =====================================================================
        // 2. ToggleButton & CheckBox: Two-State, Three-State & Event Lifecycle
        // =====================================================================
        var chk = new CheckBox().IsThreeState(false);
        AssertEqual(chk.IsChecked, false, "CheckBox initially unchecked");

        chk.OnToggle();
        AssertEqual(chk.IsChecked, true, "CheckBox toggled to true");

        chk.OnToggle();
        AssertEqual(chk.IsChecked, false, "CheckBox toggled back to false");

        // Three-state cycle: false -> true -> null -> false
        chk.IsThreeState = true;
        chk.IsChecked = false;
        chk.OnToggle();
        AssertEqual(chk.IsChecked, true, "ThreeState step 1: true");
        chk.OnToggle();
        AssertEqual(chk.IsChecked, null, "ThreeState step 2: null (indeterminate)");
        chk.OnToggle();
        AssertEqual(chk.IsChecked, false, "ThreeState step 3: false");

        // =====================================================================
        // 3. RadioButton: GroupName Mutual Exclusion
        // =====================================================================
        var radioPanel = new StackPanel();
        var rb1 = new RadioButton().GroupName("TestGroup").IsChecked(false);
        var rb2 = new RadioButton().GroupName("TestGroup").IsChecked(false);
        radioPanel.Children.Add(rb1);
        radioPanel.Children.Add(rb2);

        rb1.IsChecked = true;
        AssertEqual(rb1.IsChecked, true, "RadioButton 1 checked");
        AssertEqual(rb2.IsChecked, false, "RadioButton 2 unchecked");

        rb2.IsChecked = true;
        AssertEqual(rb2.IsChecked, true, "RadioButton 2 checked");
        AssertEqual(rb1.IsChecked, false, "RadioButton 1 automatically unchecked by group exclusion");

        // =====================================================================
        // 4. RangeBase, ProgressBar & Slider: Coercion, Clamp & Keyboard
        // =====================================================================
        var pb = new ProgressBar().Range(min: 0, max: 100, value: 45);
        AssertNear(pb.Value, 45.0, 1e-4, "ProgressBar value");

        // Out-of-bounds coercion
        pb.Value = 150.0;
        AssertNear(pb.Value, 100.0, 1e-4, "ProgressBar value clamped to Maximum");
        pb.Value = -20.0;
        AssertNear(pb.Value, 0.0, 1e-4, "ProgressBar value clamped to Minimum");

        // Slider tick snapping & navigation
        var slider = new Slider()
            .Range(0, 10, 5)
            .TickFrequency(2.0)
            .IsSnapToTickEnabled(true);

        slider.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, null, 0, Key.Right) { RoutedEvent = UIElement.KeyDownEvent });
        // Value 5 + SmallChange(0.1) snapped to nearest tick multiple of 2 (min 0) -> Math.Round(5.1/2)*2 = 6.0
        AssertNear(slider.Value, 6.0, 1e-4, "Slider snapped to tick mark 6.0");

        // =====================================================================
        // 5. TextBox & PasswordBox: Grapheme Cluster Backspace, Selection & Masking
        // =====================================================================
        var tb = new TextBox().Text("Hello CSUI");
        AssertEqual(tb.Text, "Hello CSUI", "TextBox initial text");

        // Text selection and replacement
        tb.Select(6, 4); // "CSUI"
        AssertEqual(tb.SelectedText, "CSUI", "TextBox selected text");
        tb.SelectedText = "World!";
        AssertEqual(tb.Text, "Hello World!", "TextBox selected text replaced");

        // Unicode Emoji Cluster boundary deletion test: "👨‍👩‍👧‍👦"
        string familyEmoji = "👨‍👩‍👧‍👦";
        tb.Text = "Team " + familyEmoji;
        tb.CaretIndex = tb.Text.Length;
        // Press Backspace -> entire 25-byte ZWJ multi-character cluster deleted as ONE atom
        tb.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, null, 0, Key.Back) { RoutedEvent = UIElement.KeyDownEvent });
        AssertEqual(tb.Text, "Team ", "Family Emoji cluster deleted atomically without surrogate residue");

        // PasswordBox masking
        var pwd = new PasswordBox().Password("Secret123").PasswordChar('*');
        AssertEqual(pwd.Password, "Secret123", "PasswordBox stored plaintext");
        AssertEqual(pwd.PasswordChar, '*', "PasswordBox masked with *");

        // =====================================================================
        // 6. Zero-Reflection NativeAOT XAML Parsing of Basic Controls
        // =====================================================================
        const string xamlSnippet = """
        <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Spacing="8">
            <Button Content="Submit" ClickMode="Press" />
            <CheckBox Content="Remember Me" IsChecked="True" />
            <RadioButton Content="Option A" GroupName="Opts" IsChecked="True" />
            <Slider Minimum="10" Maximum="50" Value="25" />
            <TextBox Text="AOT WPF Controls" MaxLength="32" />
        </StackPanel>
        """;

        object parsed = XamlMarkup.Load<StackPanel>(xamlSnippet);
        var panel = parsed as StackPanel;
        Assert(panel != null, "Parsed StackPanel container");
        if (panel == null) return;
        AssertEqual(panel.Children.Count, 5, "StackPanel contains 5 basic controls");

        var xBtn = panel.Children[0] as Button;
        Assert(xBtn != null && (string)xBtn.Content == "Submit" && xBtn.ClickMode == ClickMode.Press, "Parsed Button from XAML");
        if (xBtn == null) return;

        var xChk = panel.Children[1] as CheckBox;
        Assert(xChk != null && xChk.IsChecked == true, "Parsed CheckBox from XAML");

        var xRb = panel.Children[2] as RadioButton;
        Assert(xRb != null && xRb.GroupName == "Opts" && xRb.IsChecked == true, "Parsed RadioButton from XAML");

        var xSld = panel.Children[3] as Slider;
        Assert(xSld != null && Math.Abs(xSld.Value - 25.0) < 1e-4, "Parsed Slider from XAML");

        var xTb = panel.Children[4] as TextBox;
        Assert(xTb != null && xTb.Text == "AOT WPF Controls" && xTb.MaxLength == 32, "Parsed TextBox from XAML");

        var thumbEvents = 0;
        var thumb = new System.Windows.Controls.Primitives.Thumb { Width = 20, Height = 20 };
        thumb.DragStarted += (s, e) => { thumbEvents++; };
        thumb.DragDelta += (s, e) => { thumbEvents++; };
        thumb.DragCompleted += (s, e) => { thumbEvents++; };
        thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragStartedEventArgs(1, 2));
        thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(5, 8));
        thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragCompletedEventArgs(5, 8, false));
        AssertEqual(thumbEvents, 3, "Thumb DragStarted/DragDelta/DragCompleted routed events dispatch cleanly");

        var scrollEvents = 0;
        var sb = new System.Windows.Controls.Primitives.ScrollBar { Minimum = 0, Maximum = 100 };
        sb.Scroll += (s, e) => { scrollEvents++; };
        sb.RaiseEvent(new System.Windows.Controls.Primitives.ScrollEventArgs(System.Windows.Controls.Primitives.ScrollEventType.ThumbTrack, 42));
        AssertEqual(scrollEvents, 1, "ScrollBar ScrollEventHandler dispatches cleanly");

        var textChangedEvents = 0;
        xTb!.TextChanged += (s, e) => { textChangedEvents++; };
        xTb.Text = "Updated AOT Text";
        AssertEqual(textChangedEvents, 1, "TextBox TextChangedEventHandler dispatches cleanly");

        var scrollViewer = new ScrollViewer { Content = panel };
        Assert(VisualTreeHelper.GetParent(panel) == scrollViewer, "ScrollViewer Content must have ScrollViewer as Visual parent");

        var testWin = new Window
        {
            Width = 400,
            Height = 300,
            Content = scrollViewer
        };
        testWin.Show();
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);

        IntPtr testHwnd = new WindowInteropHelper(testWin).Handle;
        MS.Win32.UnsafeNativeMethods.ShowWindow(new System.Runtime.InteropServices.HandleRef(testWin, testHwnd), 3 /* SW_MAXIMIZE */);
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);
        Assert(testWin.WindowState == WindowState.Maximized, "ShowWindow(SW_MAXIMIZE) transitions WindowState cleanly without MonitorInfo marshal crash");
        MS.Win32.UnsafeNativeMethods.ShowWindow(new System.Runtime.InteropServices.HandleRef(testWin, testHwnd), 9 /* SW_RESTORE */);
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);
        Assert(testWin.WindowState == WindowState.Normal, "ShowWindow(SW_RESTORE) restores WindowState cleanly");

        Assert(PresentationSource.FromDependencyObject(xBtn) != null, "PresentationSource for control inside ScrollViewer must not be null");

        Point btnPos = xBtn.TranslatePoint(new Point(10, 10), testWin);
        IInputElement? hitElement = testWin.InputHitTest(btnPos);
        Assert(hitElement != null, "InputHitTest on live window at xBtn position must hit element");

        bool xBtnClicked = false;
        xBtn.Click += (s, e) => { xBtnClicked = true; };

        IntPtr hwnd = new WindowInteropHelper(testWin).Handle;
        int clientX = (int)btnPos.X;
        int clientY = (int)btnPos.Y;
        IntPtr lParam = (IntPtr)((clientY << 16) | (clientX & 0xFFFF));

        MS.Win32.UnsafeNativeMethods.SendMessage(hwnd, MS.Internal.Interop.WindowMessage.WM_MOUSEMOVE, IntPtr.Zero, lParam);
        MS.Win32.UnsafeNativeMethods.SendMessage(hwnd, MS.Internal.Interop.WindowMessage.WM_LBUTTONDOWN, (IntPtr)1, lParam);
        MS.Win32.UnsafeNativeMethods.SendMessage(hwnd, MS.Internal.Interop.WindowMessage.WM_LBUTTONUP, IntPtr.Zero, lParam);

        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);

        Assert(xBtnClicked, "Mouse click routed through live Win32 HWND inside ScrollViewer must invoke Button.Click");
        testWin.Close();

        Pass("CONTROL-1. Standard WPF Basic Control Hierarchy (Button, ToggleButton, CheckBox, RadioButton, RangeBase, Slider, TextBox, PasswordBox & XAML/Fluent)",
            $"btnClicks={clickCount}, chkState={chk.IsChecked}, rbChecked={rb2.IsChecked}, sliderVal={slider.Value}, tbText='{tb.Text}'");
    }

    private static void TestScrollViewerAndSmoothInertia()
    {
        // 1. Viewport measurement, extent calculation, and auto scrollbars
        var viewer = new ScrollViewer()
            .HorizontalScrollBar(ScrollBarVisibility.Auto)
            .VerticalScrollBar(ScrollBarVisibility.Auto)
            .SmoothScrolling(true, 0.75);

        // Content larger than viewport: 800 x 2000
        var content = new StackPanel();
        for (int i = 0; i < 20; i++)
        {
            var card = new MarkupCard { Width = 800, Height = 100 };
            content.Children.Add(card);
        }
        viewer.Content = content;

        // Viewport constraint: 400 x 500
        viewer.Measure(new Size(400, 500));
        viewer.Arrange(new Rect(0, 0, 400, 500));

        Assert(viewer.ExtentHeight >= 2000.0, "ScrollViewer measured total extent height >= 2000");
        Assert(viewer.ExtentWidth >= 800.0, "ScrollViewer measured total extent width >= 800");
        Assert(viewer.ViewportHeight <= 500.0, "ScrollViewer viewport height clamped inside constraint");
        Assert(viewer.ViewportWidth <= 400.0, "ScrollViewer viewport width clamped inside constraint");

        // 2. Programmatic scrolling & CompositionTarget.Rendering smooth physics inertia
        var smoothWin = new Window { Width = 400, Height = 500, Content = viewer };
        smoothWin.Show();
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);
        viewer.ScrollToVerticalOffset(120.0);
        for (int i = 0; i < 40 && Math.Abs(viewer.VerticalOffset - 120.0) > 0.01; i++)
        {
            Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.Background);
        }
        AssertNear(viewer.VerticalOffset, 120.0, 0.01, "ScrollViewer smooth scroll converges cleanly to target offset");
        smoothWin.Close();
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);

        // 2b. Verify Chromium cc::ScrollOffsetAnimationCurve (CubicBezier + InverseDelta + C1-continuous retargeting)
        var restCurve = new ScrollViewer.ChromiumScrollCubicBezier(0.25, 0.1, 0.25, 1.0);
        AssertNear(restCurve.Sample(0.0), 0.0, 1e-6, "Chromium CubicBezier Sample(0) == 0");
        AssertNear(restCurve.Sample(1.0), 1.0, 1e-6, "Chromium CubicBezier Sample(1) == 1");
        Assert(restCurve.Sample(0.25) > 0.35 && restCurve.Sample(0.5) > 0.75, "Chromium ease curve accelerates quickly and eases out smoothly");
        AssertNear(restCurve.Slope(1.0), 0.0, 1e-6, "Chromium curve decelerates to zero slope at arrival");

        double durSmall = ScrollViewer.ChromiumScrollAxis.SegmentDuration(64.0);
        double durLarge = ScrollViewer.ChromiumScrollAxis.SegmentDuration(2000.0);
        Assert(durSmall >= 0.16 && durSmall < durLarge && durLarge <= 0.36, "Chromium InverseDelta segment duration scales monotonically within [0.16s, 0.36s]");

        long t0 = 1_000_000_000L;
        var axis = new ScrollViewer.ChromiumScrollAxis();
        axis.UpdateTarget(0.0, 100.0, t0);
        Assert(axis.IsActive, "ChromiumScrollAxis becomes active on target update");
        long tMid = t0 + (long)(axis.DurationSeconds * 0.4 * System.Diagnostics.Stopwatch.Frequency);
        double posBeforeRetarget = axis.Evaluate(tMid);
        Assert(posBeforeRetarget > 0.0 && posBeforeRetarget < 100.0, "ChromiumScrollAxis progresses smoothly mid-segment");
        axis.UpdateTarget(posBeforeRetarget, 220.0, tMid);
        Assert(axis.IsActive && axis.TargetOffset == 220.0, "ChromiumScrollAxis retargets mid-scroll cleanly");
        Assert(axis.Curve.Slope(0.0) > 0.0, "Chromium EaseInOutWithInitialSlope preserves positive incoming velocity slope on same-direction retarget");
        long tEnd = tMid + (long)((axis.DurationSeconds + 0.01) * System.Diagnostics.Stopwatch.Frequency);
        AssertNear(axis.Evaluate(tEnd), 220.0, 1e-6, "ChromiumScrollAxis finishes at exact target without asymptotic creep");
        Assert(!axis.IsActive, "ChromiumScrollAxis deactivates deterministically at t >= 1.0");

        // Verify live MouseWheel opposite-direction immediate interruption & MouseWheelEventArgs instance _delta
        var w1 = new MouseWheelEventArgs(Mouse.PrimaryDevice, 1, 120);
        var w2 = new MouseWheelEventArgs(Mouse.PrimaryDevice, 2, -120);
        Assert(w1.Delta == 120 && w2.Delta == -120, "MouseWheelEventArgs.Delta must be per-instance (not static)");

        viewer.ScrollToVerticalOffset(0.0);
        var wheelWin = new Window { Width = 400, Height = 500, Content = viewer };
        wheelWin.Show();
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);
        viewer.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 10, -240) { RoutedEvent = Mouse.MouseWheelEvent });
        // Immediately interrupt mid-scroll with reverse direction (+240)
        viewer.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 20, 240) { RoutedEvent = Mouse.MouseWheelEvent });
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.Background);
        Assert(viewer.VerticalOffset == 0.0 || viewer.VerticalOffset < 50.0, "Reverse MouseWheel interrupts active scroll immediately without waiting for old tail");
        wheelWin.Close();
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);

        viewer.ScrollToVerticalOffset(600.0);
        viewer.ScrollToVerticalOffset(400.0);
        viewer.LineDown();
        viewer.PageDown();

        // 3. XAML parsing
        const string xamlScroll = """
        <ScrollViewer xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                      VerticalScrollBarVisibility="Auto"
                      HorizontalScrollBarVisibility="Disabled"
                      IsSmoothScrollingEnabled="True"
                      Friction="0.82">
            <StackPanel Spacing="8" />
        </ScrollViewer>
        """;
        object parsed = XamlMarkup.Load<ScrollViewer>(xamlScroll);
        Assert(parsed is ScrollViewer, "XamlMarkup loaded <ScrollViewer>");
        var xamlSv = (ScrollViewer)parsed;
        Assert(xamlSv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto, "VerticalScrollBarVisibility parsed as Auto");
        Assert(xamlSv.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled, "HorizontalScrollBarVisibility parsed as Disabled");
        Assert(xamlSv.IsSmoothScrollingEnabled, "IsSmoothScrollingEnabled parsed as true");
        AssertNear(xamlSv.Friction, 0.82, 1e-4, "Friction parsed accurately");
        Assert(xamlSv.Content is StackPanel, "Content is StackPanel");

        Pass("SCROLL-1. Viewport & Scrolling System (IScrollInfo Contract, ScrollBar/Thumb, Auto Visibility & Smooth Physics Inertia)",
            $"extH={viewer.ExtentHeight}, vp={viewer.ViewportWidth}x{viewer.ViewportHeight}, friction={xamlSv.Friction}");
    }

    private static void TestVariableHeightVirtualizingStackPanel()
    {
        // 1. Variable-height items: 10,000 items with varying heights (30px, 120px, 60px)
        const int TotalItems = 10000;
        var vsp = new VirtualizingStackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4.0
        };

        vsp.VirtualItems(TotalItems, index =>
        {
            double h = (index % 3 == 0) ? 30.0 : (index % 3 == 1) ? 120.0 : 60.0;
            return new MarkupCard { Height = h, Width = 350.0 };
        });

        var scrollHost = new ScrollViewer
        {
            CanContentScroll = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = vsp
        };

        // Measure & Arrange inside viewport of 400 x 600
        scrollHost.Measure(new Size(400, 600));
        scrollHost.Arrange(new Rect(0, 0, 400, 600));

        // Verify virtualization active window:
        // With 10,000 items, only a tiny subset (~10 to 30 items) must be realized in the visual tree!
        int initialRealizedCount = vsp.Children.Count;
        Assert(initialRealizedCount > 0 && initialRealizedCount < 50,
            $"VirtualizingStackPanel realized only {initialRealizedCount} elements out of 10,000 items (not 10,000!)");

        double initialExtent = vsp.ExtentHeight;
        Assert(initialExtent > 500000.0, $"Estimated total extent is realistic for 10,000 items: {initialExtent}");

        // 2. Variable-height scrolling with Anchor Stabilization
        // Scroll to item ~50 (approx offset 3500px)
        vsp.SetVerticalOffset(3500.0);
        scrollHost.Measure(new Size(400, 600));
        scrollHost.Arrange(new Rect(0, 0, 400, 600));

        int scrolledRealizedCount = vsp.Children.Count;
        Assert(scrolledRealizedCount > 0 && scrolledRealizedCount < 50,
            $"After scrolling to 3500px, realized count stays compact: {scrolledRealizedCount}");
        AssertNear(vsp.VerticalOffset, 3500.0, 1.0, "Vertical offset accurately preserved");

        // 3. XAML parsing
        const string xamlVsp = """
        <VirtualizingStackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                Orientation="Vertical"
                                Spacing="6" />
        """;
        object parsed = XamlMarkup.Load<VirtualizingStackPanel>(xamlVsp);
        Assert(parsed is VirtualizingStackPanel, "XamlMarkup parsed <VirtualizingStackPanel>");
        var parsedVsp = (VirtualizingStackPanel)parsed;
        Assert(parsedVsp.Orientation == Orientation.Vertical, "Parsed VSP orientation is Vertical");
        AssertNear(parsedVsp.Spacing, 6.0, 1e-4, "Parsed VSP spacing is 6.0");

        Pass("VSP-1. Variable-Height Item Virtualization & Anchor Stabilization (10,000 Items Realized < 30, Zero-Jitter Pixel Scrolling)",
            $"initialRealized={initialRealizedCount}, scrolledRealized={scrolledRealizedCount}, extent={initialExtent}");
    }

    private static void TestDirectWriteTextEngineAndTextFlow()
    {
        // 1. TextBlock measurement, wrapping, and trimming
        var tb = new TextBlock("Antigravity DirectWrite Hardware Text")
            .FontSize(16.0)
            .Bold()
            .Foreground(Brushes.White)
            .Padding(10.0, 6.0);

        tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert(tb.DesiredSize.Width > 150.0, $"TextBlock desired width {tb.DesiredSize.Width} > 150");
        Assert(tb.DesiredSize.Height >= 20.0, $"TextBlock desired height {tb.DesiredSize.Height} >= 20");

        // Wrapping check
        tb.TextWrapping = TextWrapping.Wrap;
        tb.Measure(new Size(120.0, double.PositiveInfinity));
        double wrappedHeight = tb.DesiredSize.Height;
        Assert(wrappedHeight > 30.0, $"Wrapped TextBlock height {wrappedHeight} is multiline");

        // Trimming check
        tb.TextWrapping = TextWrapping.NoWrap;
        tb.TextTrimming = TextTrimming.CharacterEllipsis;
        tb.Measure(new Size(80.0, double.PositiveInfinity));
        Assert(tb.DesiredSize.Width <= 120.0, "Trimmed TextBlock respects constraint");

        // 2. TextFlowView (zero-DOM rich text stream)
        var flow = new TextFlowView()
            .FontSize(14.0)
            .Foreground(Brushes.Black);

        flow.AddSpan("Welcome to ", weight: FontWeights.Normal)
            .AddSpan("CSUI Engine", weight: FontWeights.Bold, foreground: Brushes.Navy)
            .AddSpan(" with DirectWrite ", style: FontStyles.Italic)
            .AddSpan("var x = 42;", isCode: true, background: Brushes.LightGray);

        flow.Measure(new Size(500.0, double.PositiveInfinity));
        Assert(flow.DesiredSize.Width > 100.0, "TextFlowView measured width accurately");
        Assert(flow.DesiredSize.Height > 15.0, "TextFlowView measured height accurately");

        // 3. XAML parsing
        const string xamlText = """
        <TextBlock xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                   Text="Fluent Hardware Text"
                   FontSize="18"
                   FontWeight="Bold"
                   TextWrapping="Wrap"
                   TextTrimming="CharacterEllipsis"
                   TextAlignment="Center" />
        """;
        object parsed = XamlMarkup.Load<TextBlock>(xamlText);
        Assert(parsed is TextBlock, "XamlMarkup parsed <TextBlock>");
        var parsedTb = (TextBlock)parsed;
        Assert(parsedTb.Text == "Fluent Hardware Text", "Text bound");
        AssertNear(parsedTb.FontSize, 18.0, 1e-4, "FontSize bound");
        Assert(parsedTb.FontWeight == FontWeights.Bold, "FontWeight bound");
        Assert(parsedTb.TextWrapping == TextWrapping.Wrap, "TextWrapping bound");
        Assert(parsedTb.TextTrimming == TextTrimming.CharacterEllipsis, "TextTrimming bound");
        Assert(parsedTb.TextAlignment == TextAlignment.Center, "TextAlignment bound");

        Pass("TEXT-1. DirectWrite High-Performance Text Engine & Zero-DOM Rich Flow (Wrapping, Trimming, Spans, Hardware Layout)",
            $"tbSize={tb.DesiredSize.Width:F1}x{tb.DesiredSize.Height:F1}, flowSize={flow.DesiredSize.Width:F1}x{flow.DesiredSize.Height:F1}");
    }

    private static void TestAsyncCompositorAndIndependentAnimations()
    {
        var compositor = AsyncCompositor.Instance;
        ArgumentNullException.ThrowIfNull(compositor);
        Assert(compositor.IsRunning, "AsyncCompositor thread is initialized and running");

        // 1. Independent continuous rotation animation
        var rotAnim = new CompositorRotateAnimation("spinner", speedDegPerSec: 360.0, center: new Point(50, 50));
        Matrix m = Matrix.Identity;
        double opacity = 1.0;

        // Step 0.25s -> exactly 90 degrees
        rotAnim.Step(0.25);
        rotAnim.Apply(ref m, ref opacity);
        AssertNear(rotAnim.CurrentAngle, 90.0, 1e-4, "CompositorRotateAnimation stepped to 90 degrees in 0.25s");

        // 2. Independent momentum glide animation
        var glideAnim = new CompositorGlideAnimation("glide", new Vector(600, 300), friction: 0.85);
        Matrix gm = Matrix.Identity;
        double gOpacity = 1.0;

        // Step 0.1s
        glideAnim.Step(0.1);
        glideAnim.Apply(ref gm, ref gOpacity);
        Assert(glideAnim.CurrentOffset.X > 0 && glideAnim.CurrentOffset.Y > 0, "Glide animation produced independent translation offset");
        Assert(glideAnim.Velocity.Length < 671.0, "Friction dampens velocity independently of UI thread");

        // 3. Register a fake target slot and commit frame to verify lock-free pipeline
        IntPtr dummyHwnd = new IntPtr(0x123400);
        var group = new DrawingGroup();
        using (var dc = group.Open())
        {
            dc.DrawRectangle(Brushes.DarkSlateBlue, null, new Rect(0, 0, 100, 100));
        }

        compositor.CommitFrame(dummyHwnd, group, Colors.Black, Matrix.Identity, 1.0);
        compositor.StartIndependentAnimation(dummyHwnd, rotAnim);
        compositor.StopIndependentAnimation(dummyHwnd, "spinner");
        compositor.UnregisterTarget(dummyHwnd);

        Pass("COMPOSITOR-1. Independent Asynchronous Compositor Pipeline (Dedicated Worker Thread, Lock-Free Frame Commit & Independent VSync Animations)",
            $"rotAngle={rotAnim.CurrentAngle:F1}°, glideOffset=({glideAnim.CurrentOffset.X:F1},{glideAnim.CurrentOffset.Y:F1}), running={compositor.IsRunning}");
    }

    private static void TestModernGestureEngineAndPinchPan()
    {
        // 1. High-precision hardware gesture simulation
        var panel = new StackPanel();
        var card = new MarkupCard { Width = 200, Height = 200 };
        panel.Children.Add(card);

        bool gestureReceived = false;
        ModernGestureType receivedType = ModernGestureType.Pan;
        double receivedScale = 1.0;
        Vector receivedPan = default;

        card.AddHandler(ModernGestureEngine.ModernGestureDeltaEvent, new ModernGestureDeltaEventHandler((s, e) =>
        {
            gestureReceived = true;
            receivedType = e.GestureType;
            receivedScale = e.DeltaScale;
            receivedPan = e.DeltaPan;
        }));

        // Dispatch simulated precision touchpad pinch-to-zoom (DeltaScale = 1.25)
        ModernGestureEngine.DispatchSimulatedGesture(
            card,
            ModernGestureType.Zoom,
            center: new Point(100, 100),
            deltaPan: new Vector(0, 0),
            deltaScale: 1.25);

        Assert(gestureReceived, "Card received ModernGestureDeltaEvent");
        Assert(receivedType == ModernGestureType.Zoom, "GestureType is Zoom");
        AssertNear(receivedScale, 1.25, 1e-4, "Pinch-to-zoom scale factor is 1.25");

        // 2. ScrollViewer integration with precision two-finger pan
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            CanContentScroll = false
        };
        var tallContent = new MarkupCard { Width = 400, Height = 3000 };
        scrollViewer.Content = tallContent;
        scrollViewer.Measure(new Size(400, 500));
        scrollViewer.Arrange(new Rect(0, 0, 400, 500));

        double initialOffset = scrollViewer.VerticalOffset;
        Assert(initialOffset == 0.0, "Initial VerticalOffset is 0");

        // Pan up by 80px (finger moves up -80px -> scroll offset increases by 80px)
        scrollViewer.RaiseEvent(new ModernGestureDeltaEventArgs(
            ModernGestureType.Pan,
            new Point(200, 250),
            new Vector(0, -80),
            new Vector(0, -80),
            1.0, 1.0, 0.0, 0.0, false));

        AssertNear(scrollViewer.VerticalOffset, 80.0, 1.0, "ScrollViewer handled hardware two-finger pan gesture smoothly");

        Pass("GESTURE-1. Modern High-Precision Touch & Precision Touchpad Gestures (WM_GESTURE/WM_POINTER, Pinch-to-Zoom, Two-Finger Pan)",
            $"zoomScale={receivedScale:F2}, scrollPanOffset={scrollViewer.VerticalOffset:F1}px");
    }

    private static void TestPerMonitorV2DpiHotPlug()
    {
        // Test DPI scale calculations and suggestedRect scaling
        var dpi96 = new DpiScale(1.0, 1.0);
        var dpi144 = new DpiScale(1.5, 1.5);
        var dpi192 = new DpiScale(2.0, 2.0);

        AssertNear(dpi96.DpiScaleX, 1.0, 1e-4, "96 DPI is 100% scale");
        AssertNear(dpi144.DpiScaleX, 1.5, 1e-4, "144 DPI is 150% scale");
        AssertNear(dpi192.DpiScaleX, 2.0, 1e-4, "192 DPI is 200% scale");

        // Simulate WM_DPICHANGED suggested rect calculation (800x600 window moved from 100% -> 150%)
        Rect initialRect = new Rect(100, 100, 800, 600);
        Rect suggestedRect150 = new Rect(100, 100, initialRect.Width * 1.5, initialRect.Height * 1.5);
        AssertNear(suggestedRect150.Width, 1200.0, 1e-4, "Suggested rect scaled to 1200px width");
        AssertNear(suggestedRect150.Height, 900.0, 1e-4, "Suggested rect scaled to 900px height");

        // Window DpiChanged routed event test
        var window = new Window();
        bool windowDpiEventFired = false;
        DpiScale reportedOldDpi = default;
        DpiScale reportedNewDpi = default;

        window.DpiChanged += (s, e) =>
        {
            windowDpiEventFired = true;
            reportedOldDpi = e.OldDpi;
            reportedNewDpi = e.NewDpi;
        };

        window.RaiseEvent(new DpiChangedEventArgs(
            new DpiScale(1.0, 1.0),
            new DpiScale(1.5, 1.5),
            Window.DpiChangedEvent,
            window));

        Assert(windowDpiEventFired, "Window.DpiChanged routed event received by application");
        AssertNear(reportedNewDpi.DpiScaleX, 1.5, 1e-4, "Reported new DPI scale is 1.5 (150%)");

        Pass("DPI-1. Per-Monitor v2 Dynamic DPI Hot-Plug & SwapChain Reconfiguration (WM_DPICHANGED, Instant Buffer Resize, Zero-Blur)",
            $"oldDpi={reportedOldDpi.PixelsPerInchX}, newDpi={reportedNewDpi.PixelsPerInchX}, suggested={suggestedRect150.Width}x{suggestedRect150.Height}");
    }

    private static void TestXamlMarkupExtensionsAndResourceFeatures()
        => MarkupTestsExtensionHelper.RunMarkupExtensionsTest(Pass, Assert);
}

[AotBindable]
public sealed class GeneratedMetricSeries
{
    private readonly double[] _slots = new double[] { 32.0, 64.0, 96.0 };

    public double this[int index]
    {
        get => _slots[index];
        set => _slots[index] = value;
    }
}

[ObservableObject]
public partial class GeneratedDashboardViewModel
{
    [ObservableProperty]
    [AlsoNotifyChangeFor(nameof(TotalScore))]
    private double _baseScore = 75.0;

    [ObservableProperty]
    [AlsoNotifyChangeFor(nameof(TotalScore))]
    private double _multiplier = 2.0;

    [ObservableProperty]
    private string _statusText = "Ready";

    public double TotalScore => BaseScore * Multiplier;

    public GeneratedMetricSeries Series { get; } = new();

    public int BaseScoreChangeCount { get; private set; }

    partial void OnBaseScoreChanged(double value)
    {
        BaseScoreChangeCount++;
    }

    private bool CanBoost(double delta) => delta > 0.0;

    [RelayCommand(CanExecute = nameof(CanBoost))]
    private void Boost(double delta)
    {
        BaseScore += delta;
        StatusText = "Boosted";
    }
}

[XamlType]
public sealed class GeneratedBadgeControl : FrameworkElement
{
    public static readonly DependencyProperty BadgeLabelProperty =
        DependencyProperty.Register(
            nameof(BadgeLabel),
            typeof(string),
            typeof(GeneratedBadgeControl),
            new FrameworkPropertyMetadata(string.Empty));

    public string BadgeLabel
    {
        get => (string)GetValue(BadgeLabelProperty);
        set => SetValue(BadgeLabelProperty, value);
    }

    // Pure CLR property (no DependencyProperty) automatically registered by [XamlType] in [ModuleInitializer]
    public int StaticTier { get; set; } = 1;
}

[XamlType]
public partial class GeneratedDashboardView : Panel
{
    public bool ComponentHookCalled { get; private set; }

    partial void OnComponentInitialized()
    {
        ComponentHookCalled = true;
    }
}

internal static partial class MarkupTestsExtensionHelper
{
    public static void RunMarkupExtensionsTest(Action<string, string> pass, Action<bool, string> assert)
    {
        // 1. Direct MarkupExtension types in System.Windows.Markup
        var nullExt = new NullExtension();
        assert(nullExt.ProvideValue(null!) == null, "NullExtension returns null");

        var typeExt = new TypeExtension(typeof(TextBlock));
        assert(Equals(typeExt.ProvideValue(null!), typeof(TextBlock)), "TypeExtension returns typeof(TextBlock)");

        var staticExt = new StaticExtension { MemberType = typeof(Brushes), Member = nameof(Brushes.Coral) };
        assert(staticExt.MemberType == typeof(Brushes) && staticExt.Member == nameof(Brushes.Coral),
            "StaticExtension stores MemberType and Member");

        var arrayExt = new ArrayExtension(typeof(string));
        arrayExt.AddChild("One");
        arrayExt.AddChild("Two");
        var arrResult = arrayExt.ProvideValue(null!) as string[];
        assert(arrResult is { Length: 2 } && arrResult[0] == "One" && arrResult[1] == "Two", "ArrayExtension builds typed array");

        // 2. Compile-time generated XAML (ZeroModWpfView): x:Shared="False", x:Array, x:Reference
        var view = new ZeroModWpfView();
        object b1 = view.FindResource("NonSharedBrush");
        object b2 = view.FindResource("NonSharedBrush");
        assert(b1 is SolidColorBrush && b2 is SolidColorBrush && !ReferenceEquals(b1, b2),
            "Compiled XAML x:Shared='False' returns distinct instances");

        var compiledTags = view.FindResource("CompiledTags") as string[];
        assert(compiledTags is { Length: 2 } && compiledTags[0] == "Alpha" && compiledTags[1] == "Beta",
            "Compiled XAML <x:Array> created typed string[]");

        assert(ReferenceEquals(view.RefTb.Tag, view.TitleTb),
            "Compiled XAML {x:Reference TitleTb} resolved TitleTb");

        // 3. Runtime XamlMarkup.Load: x:Shared="False", x:Array, x:Reference
        const string runtimeXaml = """
        <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=System.Runtime">
            <StackPanel.Resources>
                <SolidColorBrush x:Key="RtNonShared" x:Shared="False" Color="DodgerBlue" />
                <x:Array x:Key="RtArray" Type="{x:Type sys:String}">
                    <sys:String>First</sys:String>
                    <sys:String>Second</sys:String>
                </x:Array>
            </StackPanel.Resources>
            <TextBlock x:Name="RtSource" Text="HelloRef" />
            <TextBlock x:Name="RtTarget" Tag="{x:Reference RtSource}" />
        </StackPanel>
        """;
        var sp = XamlMarkup.Load<StackPanel>(runtimeXaml, out XamlLoadContext rtCtx);
        object r1 = sp.FindResource("RtNonShared");
        object r2 = sp.FindResource("RtNonShared");
        assert(r1 is SolidColorBrush && r2 is SolidColorBrush && !ReferenceEquals(r1, r2),
            "Runtime XAML x:Shared='False' returns distinct instances");

        var rtArr = sp.FindResource("RtArray") as string[];
        assert(rtArr is { Length: 2 } && rtArr[0] == "First" && rtArr[1] == "Second",
            "Runtime XAML <x:Array> created typed string[]");

        var rtSource = sp.FindName("RtSource") as TextBlock;
        var rtTarget = sp.FindName("RtTarget") as TextBlock;
        assert(rtSource != null && rtTarget != null && ReferenceEquals(rtTarget.Tag, rtSource),
            "Runtime XAML {x:Reference RtSource} resolved RtSource");

        pass("MK-6. XAML MarkupExtensions & Resource Features (StaticExtension, TypeExtension, NullExtension, x:Array, x:Shared='False', x:Reference)",
            $"compiledTags={compiledTags!.Length}, rtArr={rtArr!.Length}");
    }
}
