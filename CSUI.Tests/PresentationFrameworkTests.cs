using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using static CSUI.Tests.TestHelpers;

namespace CSUI.Tests;

internal static class PresentationFrameworkTests
{
    public static void RunAll()
    {
        TestFrameworkElementLayoutAndCustomPanel();
        TestResourceDictionaryStaticDynamicAndMerged();
        TestStylesTriggersAndControlTemplate();
        TestAotReflectionFreeDataBinding();
        TestContentAndItemsControlsWithCollectionView();
    }

    private sealed class HorizontalStackTestPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            double totalW = 0;
            double maxH = 0;
            foreach (UIElement child in InternalChildren)
            {
                child.Measure(availableSize);
                totalW += child.DesiredSize.Width;
                maxH = Math.Max(maxH, child.DesiredSize.Height);
            }
            return new Size(totalW, maxH);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double x = 0;
            foreach (UIElement child in InternalChildren)
            {
                double w = child.DesiredSize.Width;
                child.Arrange(new Rect(x, 0, w, finalSize.Height));
                x += w;
            }
            return finalSize;
        }
    }

    private sealed class BoxElement : FrameworkElement
    {
        public Brush? Fill { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (Fill != null)
                drawingContext.DrawRectangle(Fill, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));
        }
    }

    // PF-1: FrameworkElement Measure/Arrange, Margin, Min/Max, Alignment & Custom Panel/Decorator
    private static void TestFrameworkElementLayoutAndCustomPanel()
    {
        var panel = new HorizontalStackTestPanel { Width = 200, Height = 60 };
        var box1 = new BoxElement
        {
            Width = 50,
            Height = 40,
            Margin = new Thickness(5, 10, 5, 10),
            Fill = Brushes.Red
        };
        var box2 = new BoxElement
        {
            Width = 30,
            MinWidth = 70, // MinWidth overrides Width=30 -> 70
            Height = 60,
            Fill = Brushes.Lime
        };
        panel.Children.Add(box1);
        panel.Children.Add(box2);

        var decorator = new Decorator
        {
            Child = panel
        };

        decorator.Measure(new Size(300, 100));
        decorator.Arrange(new Rect(0, 0, 200, 60));

        // box1 DesiredSize = (50 + 5 + 5, 40 + 10 + 10) = (60, 60)
        AssertNear(box1.DesiredSize.Width, 60, 1e-4, "box1 DesiredSize.Width with Margin");
        AssertNear(box1.RenderSize.Width, 50, 1e-4, "box1 RenderSize.Width");
        AssertNear(box2.DesiredSize.Width, 70, 1e-4, "box2 DesiredSize.Width with MinWidth=70");
        AssertNear(box2.RenderSize.Width, 70, 1e-4, "box2 RenderSize.Width");

        byte[] px = RenderToBgra32(decorator, 200, 60);
        var pBox1 = Sample(px, 200, 25, 30); // inside box1
        var pBox2 = Sample(px, 200, 90, 30); // inside box2 ([60..130])
        Assert(pBox1.A == 255 && pBox1.R >= 245 && pBox1.G == 0, $"box1 pixel expected Red, got {pBox1}");
        Assert(pBox2.A == 255 && pBox2.G >= 245 && pBox2.R == 0, $"box2 pixel expected Lime, got {pBox2}");

        Pass("PF-1. FrameworkElement two-pass Layout (Margin, MinWidth, Decorator & custom Panel)",
            $"box1={box1.DesiredSize}, box2={box2.DesiredSize}");
    }

    // PF-2: ResourceDictionary Hierarchical Lookup, MergedDictionaries & DynamicResource
    private static void TestResourceDictionaryStaticDynamicAndMerged()
    {
        var merged = new ResourceDictionary();
        merged["AccentBrush"] = Brushes.DodgerBlue;

        var rootPanel = new HorizontalStackTestPanel();
        rootPanel.Resources.MergedDictionaries.Add(merged);
        rootPanel.Resources["DynamicHeight"] = 48.0;

        var child = new BoxElement();
        rootPanel.Children.Add(child);

        // Hierarchical TryFindResource from child up to rootPanel.Resources.MergedDictionaries
        object? foundBrush = child.TryFindResource("AccentBrush");
        Assert(ReferenceEquals(foundBrush, Brushes.DodgerBlue), "MergedDictionary resource lookup from child");

        // DynamicResource via SetResourceReference
        child.SetResourceReference(FrameworkElement.HeightProperty, "DynamicHeight");
        AssertNear(child.Height, 48.0, 1e-4, "Initial DynamicResource value");

        // Mutating dictionary entry dynamically updates target property
        rootPanel.Resources["DynamicHeight"] = 96.0;
        AssertNear(child.Height, 96.0, 1e-4, "Updated DynamicResource value after dictionary change");

        Pass("PF-2. ResourceDictionary (MergedDictionaries, tree lookup & DynamicResource updates)",
            $"dynamicHeight={child.Height}");
    }

    private sealed class TemplatedTestControl : Control
    {
        public BoxElement? PartSurface { get; private set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            PartSurface = GetTemplateChild("PART_Surface") as BoxElement;
        }
    }

    // PF-3: Style (Setters, BasedOn, Property Trigger) & ControlTemplate (AOT FrameworkElementFactory)
    private static void TestStylesTriggersAndControlTemplate()
    {
        var baseStyle = new Style(typeof(TemplatedTestControl));
        baseStyle.Setters.Add(new Setter(FrameworkElement.WidthProperty, 120.0));
        baseStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.CornflowerBlue));

        var derivedStyle = new Style(typeof(TemplatedTestControl), baseStyle);
        derivedStyle.Setters.Add(new Setter(FrameworkElement.HeightProperty, 45.0));

        // Property Trigger: when IsEnabled == false, Opacity becomes 0.4
        var disabledTrigger = new Trigger
        {
            Property = UIElement.IsEnabledProperty,
            Value = false
        };
        disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.4));
        derivedStyle.Triggers.Add(disabledTrigger);

        // ControlTemplate using AOT-safe FrameworkElementFactory.Create<T>()
        var template = new ControlTemplate(typeof(TemplatedTestControl));
        FrameworkElementFactory rootFactory = FrameworkElementFactory.Create<BoxElement>("PART_Surface");
        rootFactory.SetValue(FrameworkElement.WidthProperty, 120.0);
        rootFactory.SetValue(FrameworkElement.HeightProperty, 45.0);
        template.VisualTree = rootFactory;
        derivedStyle.Setters.Add(new Setter(Control.TemplateProperty, template));

        var ctrl = new TemplatedTestControl
        {
            Style = derivedStyle
        };
        ctrl.Measure(new Size(200, 100));
        ctrl.Arrange(new Rect(0, 0, 120, 45));

        AssertNear(ctrl.Width, 120.0, 1e-4, "Inherited BaseOn Style Width");
        AssertNear(ctrl.Height, 45.0, 1e-4, "Derived Style Height");
        Assert(ctrl.PartSurface != null, "ControlTemplate PART_Surface instantiated and resolved via GetTemplateChild");

        // Trigger activation and deactivation
        AssertNear(ctrl.Opacity, 1.0, 1e-4, "Opacity before trigger");
        ctrl.IsEnabled = false;
        AssertNear(ctrl.Opacity, 0.4, 1e-4, "Opacity when IsEnabled=false trigger active");
        ctrl.IsEnabled = true;
        AssertNear(ctrl.Opacity, 1.0, 1e-4, "Opacity restored when IsEnabled=true");

        Pass("PF-3. Style (BasedOn, Setters, Property Trigger) & AOT ControlTemplate instantiation",
            $"size={ctrl.Width}x{ctrl.Height}, partResolved={ctrl.PartSurface != null}");
    }

    private sealed class CounterViewModel : INotifyPropertyChanged
    {
        private double _value = 20.0;
        public double Value
        {
            get => _value;
            set
            {
                if (Math.Abs(_value - value) > 1e-6)
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class ScaleDoubleConverter : IValueConverter
    {
        public double Factor { get; set; } = 2.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (double)value * Factor;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (double)value / Factor;
    }

    // PF-4: NativeAOT Reflection-Free Data Binding (TwoWay, INotifyPropertyChanged, IValueConverter)
    private static void TestAotReflectionFreeDataBinding()
    {
        var vm = new CounterViewModel { Value = 35.0 };
        var element = new BoxElement { DataContext = vm };

        // TwoWay strongly-typed AOT Binding with Converter (Width = vm.Value * 2)
        Binding binding = Binding.Create<CounterViewModel, double>(
            static m => m.Value,
            static (m, v) => m.Value = v,
            nameof(CounterViewModel.Value));
        binding.Converter = new ScaleDoubleConverter { Factor = 2.0 };

        element.SetBinding(FrameworkElement.WidthProperty, binding);
        AssertNear(element.Width, 70.0, 1e-4, "Initial bound Width (35 * 2)");

        // Source -> Target via INotifyPropertyChanged
        vm.Value = 60.0;
        AssertNear(element.Width, 120.0, 1e-4, "Updated bound Width after INotifyPropertyChanged (60 * 2)");

        // Target -> Source (TwoWay)
        element.Width = 180.0;
        AssertNear(vm.Value, 90.0, 1e-4, "TwoWay target-to-source update via ConvertBack (180 / 2)");

        BindingOperations.ClearBinding(element, FrameworkElement.WidthProperty);
        vm.Value = 10.0;
        Assert(double.IsNaN(element.Width), "Width should reset to default NaN after ClearBinding");

        Pass("PF-4. NativeAOT reflection-free Data Binding (TwoWay, INPC, IValueConverter, ClearBinding)",
            $"finalVmValue={vm.Value}");
    }

    // PF-5: ContentControl, HeaderedContentControl, ItemsControl & CollectionView Filtering
    private static void TestContentAndItemsControlsWithCollectionView()
    {
        var hcc = new HeaderedContentControl
        {
            Header = "Section A",
            Content = "Payload Content"
        };
        AssertEqual(hcc.Header as string, "Section A", "HeaderedContentControl.Header");
        AssertEqual(hcc.Content as string, "Payload Content", "HeaderedContentControl.Content");

        var data = new ObservableCollection<int> { 10, 25, 30, 45, 50 };
        var ic = new ItemsControl
        {
            ItemsSource = data
        };
        AssertEqual(ic.Items.Count, 5, "ItemsControl initial count");

        // Filter via Items.Filter (even numbers only: 10, 30, 50)
        ic.Items.Filter = item => (int)item % 2 == 0;
        AssertEqual(ic.Items.Count, 3, "Filtered ItemsControl count");
        AssertEqual((int)ic.Items[0]!, 10, "First filtered item");
        AssertEqual((int)ic.Items[2]!, 50, "Last filtered item");

        // Live ObservableCollection addition respecting filter
        data.Add(60);
        data.Add(65);
        AssertEqual(ic.Items.Count, 4, "Filtered count after adding 60 and 65");

        Pass("PF-5. HeaderedContentControl, ItemsControl & live CollectionView filtering",
            $"filteredCount={ic.Items.Count}");
    }
}
