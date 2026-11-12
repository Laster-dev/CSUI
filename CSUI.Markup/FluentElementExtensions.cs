using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Media.Svg;

namespace CSUI.Markup
{
    /// <summary>
    /// Declarative C# Fluent Markup extensions for CSUI.Core DependencyObject, UIElement, FrameworkElement,
    /// Control, Panel, Decorator, AOT Data Binding, Animations, and Media3D.
    /// </summary>
    public static class FluentElementExtensions
    {
        // =====================================================================
        // 1. Variable Capture, Generic Property Setters & Configuration
        // =====================================================================

        public static T Assign<T>(this T element, out T variable)
        {
            variable = element;
            return element;
        }

        public static T With<T>(this T element, Action<T> configure)
        {
            ArgumentNullException.ThrowIfNull(element);
            configure?.Invoke(element);
            return element;
        }

        public static T Set<T>(this T target, DependencyProperty property, object value)
            where T : DependencyObject
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(property);
            target.SetValue(property, value);
            return target;
        }

        public static T Attached<T>(this T target, DependencyProperty attachedProperty, object value)
            where T : DependencyObject
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(attachedProperty);
            target.SetValue(attachedProperty, value);
            return target;
        }

        // =====================================================================
        // 2. FrameworkElement Sizing, Alignment & Layout
        // =====================================================================

        public static T Name<T>(this T element, string name)
            where T : FrameworkElement
        {
            element.Name = name;
            return element;
        }

        public static T Width<T>(this T element, double width)
            where T : FrameworkElement
        {
            element.Width = width;
            return element;
        }

        public static T Height<T>(this T element, double height)
            where T : FrameworkElement
        {
            element.Height = height;
            return element;
        }

        public static T Size<T>(this T element, double width, double height)
            where T : FrameworkElement
        {
            element.Width = width;
            element.Height = height;
            return element;
        }

        public static T MinSize<T>(this T element, double minWidth, double minHeight)
            where T : FrameworkElement
        {
            element.MinWidth = minWidth;
            element.MinHeight = minHeight;
            return element;
        }

        public static T MaxSize<T>(this T element, double maxWidth, double maxHeight)
            where T : FrameworkElement
        {
            element.MaxWidth = maxWidth;
            element.MaxHeight = maxHeight;
            return element;
        }

        public static T Margin<T>(this T element, double uniform)
            where T : FrameworkElement
        {
            element.Margin = new Thickness(uniform);
            return element;
        }

        public static T Margin<T>(this T element, double horizontal, double vertical)
            where T : FrameworkElement
        {
            element.Margin = new Thickness(horizontal, vertical, horizontal, vertical);
            return element;
        }

        public static T Margin<T>(this T element, double left, double top, double right, double bottom)
            where T : FrameworkElement
        {
            element.Margin = new Thickness(left, top, right, bottom);
            return element;
        }

        public static T Margin<T>(this T element, Thickness margin)
            where T : FrameworkElement
        {
            element.Margin = margin;
            return element;
        }

        public static T Align<T>(this T element, HorizontalAlignment horizontal, VerticalAlignment vertical)
            where T : FrameworkElement
        {
            element.HorizontalAlignment = horizontal;
            element.VerticalAlignment = vertical;
            return element;
        }

        public static T AlignHorizontal<T>(this T element, HorizontalAlignment alignment)
            where T : FrameworkElement
        {
            element.HorizontalAlignment = alignment;
            return element;
        }

        public static T AlignVertical<T>(this T element, VerticalAlignment alignment)
            where T : FrameworkElement
        {
            element.VerticalAlignment = alignment;
            return element;
        }

        public static T Center<T>(this T element)
            where T : FrameworkElement
        {
            element.HorizontalAlignment = HorizontalAlignment.Center;
            element.VerticalAlignment = VerticalAlignment.Center;
            return element;
        }

        public static T Cursor<T>(this T element, Cursor cursor)
            where T : FrameworkElement
        {
            element.Cursor = cursor;
            return element;
        }

        // =====================================================================
        // 3. UIElement Visual State, Transforms & Clipping
        // =====================================================================

        public static T Opacity<T>(this T element, double opacity)
            where T : UIElement
        {
            element.Opacity = opacity;
            return element;
        }

        public static T Visibility<T>(this T element, Visibility visibility)
            where T : UIElement
        {
            element.Visibility = visibility;
            return element;
        }

        public static T IsEnabled<T>(this T element, bool enabled)
            where T : UIElement
        {
            element.IsEnabled = enabled;
            return element;
        }

        public static T Clip<T>(this T element, Geometry clipGeometry)
            where T : UIElement
        {
            element.Clip = clipGeometry;
            return element;
        }

        public static T RenderTransform<T>(this T element, Transform transform, double originX = 0.5, double originY = 0.5)
            where T : UIElement
        {
            element.RenderTransform = transform;
            element.RenderTransformOrigin = new Point(originX, originY);
            return element;
        }

        // =====================================================================
        // 4. Control Appearance, Content, Decorator & Panel Composition
        // =====================================================================

        public static T Background<T>(this T control, Brush brush)
            where T : Control
        {
            control.Background = brush;
            return control;
        }

        public static T Background<T>(this T control, Color color)
            where T : Control
        {
            control.Background = new SolidColorBrush(color);
            return control;
        }

        public static T Background<T>(this T control, byte r, byte g, byte b)
            where T : Control
        {
            control.Background = new SolidColorBrush(Color.FromRgb(r, g, b));
            return control;
        }

        public static T Foreground<T>(this T control, Brush brush)
            where T : Control
        {
            control.Foreground = brush;
            return control;
        }

        public static T Foreground<T>(this T control, Color color)
            where T : Control
        {
            control.Foreground = new SolidColorBrush(color);
            return control;
        }

        public static T Border<T>(this T control, Brush borderBrush, double uniformThickness = 1.0)
            where T : Control
        {
            control.BorderBrush = borderBrush;
            control.BorderThickness = new Thickness(uniformThickness);
            return control;
        }

        public static T Border<T>(this T control, Brush borderBrush, Thickness borderThickness)
            where T : Control
        {
            control.BorderBrush = borderBrush;
            control.BorderThickness = borderThickness;
            return control;
        }

        public static T Padding<T>(this T control, double uniform)
            where T : Control
        {
            control.Padding = new Thickness(uniform);
            return control;
        }

        public static T Padding<T>(this T control, double horizontal, double vertical)
            where T : Control
        {
            control.Padding = new Thickness(horizontal, vertical, horizontal, vertical);
            return control;
        }

        public static T Padding<T>(this T control, double left, double top, double right, double bottom)
            where T : Control
        {
            control.Padding = new Thickness(left, top, right, bottom);
            return control;
        }

        public static T Font<T>(this T control, double fontSize, FontWeight? weight = null, string fontFamily = null)
            where T : Control
        {
            control.FontSize = fontSize;
            if (weight.HasValue)
            {
                control.FontWeight = weight.Value;
            }
            if (!string.IsNullOrEmpty(fontFamily))
            {
                control.FontFamily = new FontFamily(fontFamily);
            }
            return control;
        }

        public static T Template<T>(this T control, ControlTemplate template)
            where T : Control
        {
            control.Template = template;
            return control;
        }

        public static T Child<T>(this T decorator, UIElement child)
            where T : Decorator
        {
            decorator.Child = child;
            return decorator;
        }

        public static T Content<T>(this T contentControl, object content)
            where T : ContentControl
        {
            contentControl.Content = content;
            return contentControl;
        }

        public static T Header<T>(this T headeredControl, object header)
            where T : HeaderedContentControl
        {
            headeredControl.Header = header;
            return headeredControl;
        }

        public static T ItemsSource<T>(this T itemsControl, IEnumerable itemsSource)
            where T : ItemsControl
        {
            itemsControl.ItemsSource = itemsSource;
            return itemsControl;
        }

        public static T Children<T>(this T panel, params UIElement[] children)
            where T : Panel
        {
            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] != null)
                    {
                        panel.Children.Add(children[i]);
                    }
                }
            }
            return panel;
        }

        // =====================================================================
        // 5. Resources, Styles & NativeAOT Reflection-Free Data Binding
        // =====================================================================

        public static T DataContext<T>(this T element, object dataContext)
            where T : FrameworkElement
        {
            element.DataContext = dataContext;
            return element;
        }

        public static T Style<T>(this T element, Style style)
            where T : FrameworkElement
        {
            element.Style = style;
            return element;
        }

        public static T Resource<T>(this T element, object key, object value)
            where T : FrameworkElement
        {
            element.Resources[key] = value;
            return element;
        }

        public static T Resources<T>(this T element, Action<ResourceDictionary> configure)
            where T : FrameworkElement
        {
            configure?.Invoke(element.Resources);
            return element;
        }

        public static T DynamicResource<T>(this T element, DependencyProperty dp, object resourceKey)
            where T : FrameworkElement
        {
            element.SetResourceReference(dp, resourceKey);
            return element;
        }

        public static T StaticResource<T>(this T element, DependencyProperty dp, object resourceKey)
            where T : FrameworkElement
        {
            object value = element.TryFindResource(resourceKey);
            if (value != null && value != DependencyProperty.UnsetValue)
            {
                element.SetValue(dp, value);
            }
            return element;
        }

        public static T BindOneWay<T, TSource, TProp>(
            this T element,
            DependencyProperty dp,
            Func<TSource, TProp> getter,
            string propertyName,
            TSource source = default,
            IValueConverter converter = null,
            object converterParameter = null)
            where T : FrameworkElement
        {
            Binding binding = Binding.Create<TSource, TProp>(getter, setter: null, propertyName: propertyName);
            if (source != null)
            {
                binding.Source = source;
            }
            if (converter != null)
            {
                binding.Converter = converter;
                binding.ConverterParameter = converterParameter;
            }
            BindingOperations.SetBinding(element, dp, binding);
            return element;
        }

        public static T BindTwoWay<T, TSource, TProp>(
            this T element,
            DependencyProperty dp,
            Func<TSource, TProp> getter,
            Action<TSource, TProp> setter,
            string propertyName,
            TSource source = default,
            IValueConverter converter = null,
            object converterParameter = null)
            where T : FrameworkElement
        {
            Binding binding = Binding.Create<TSource, TProp>(getter, setter, propertyName);
            if (source != null)
            {
                binding.Source = source;
            }
            if (converter != null)
            {
                binding.Converter = converter;
                binding.ConverterParameter = converterParameter;
            }
            BindingOperations.SetBinding(element, dp, binding);
            return element;
        }

        /// <summary>
        /// Binds a DependencyProperty using a single or multi-step path (e.g. "Department.Manager.Name" or "Items[0]")
        /// resolved via <see cref="PropertyAccessorRegistry"/> or DependencyProperty without reflection.
        /// </summary>
        public static T BindPath<T>(
            this T element,
            DependencyProperty dp,
            string path,
            BindingMode mode = BindingMode.Default,
            object source = null,
            IValueConverter converter = null,
            object converterParameter = null,
            string stringFormat = null,
            object fallbackValue = null,
            object targetNullValue = null,
            UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.Default,
            int delay = 0,
            bool validatesOnDataErrors = false,
            bool validatesOnNotifyDataErrors = true,
            bool validatesOnExceptions = false)
            where T : FrameworkElement
        {
            var binding = CreateConfiguredBinding(
                path,
                mode,
                source,
                elementName: null,
                relativeSource: null,
                converter,
                converterParameter,
                stringFormat,
                fallbackValue,
                targetNullValue,
                updateSourceTrigger,
                delay,
                validatesOnDataErrors,
                validatesOnNotifyDataErrors,
                validatesOnExceptions);
            BindingOperations.SetBinding(element, dp, binding);
            return element;
        }

        /// <summary>
        /// Binds a DependencyProperty to another named element in the same NameScope (ElementName binding).
        /// </summary>
        public static T BindElement<T>(
            this T element,
            DependencyProperty dp,
            string elementName,
            string path,
            BindingMode mode = BindingMode.Default,
            IValueConverter converter = null,
            string stringFormat = null)
            where T : FrameworkElement
        {
            var binding = CreateConfiguredBinding(
                path,
                mode,
                source: null,
                elementName: elementName,
                relativeSource: null,
                converter: converter,
                converterParameter: null,
                stringFormat: stringFormat,
                fallbackValue: null,
                targetNullValue: null,
                updateSourceTrigger: UpdateSourceTrigger.Default,
                delay: 0,
                validatesOnDataErrors: false,
                validatesOnNotifyDataErrors: false,
                validatesOnExceptions: false);
            BindingOperations.SetBinding(element, dp, binding);
            return element;
        }

        /// <summary>
        /// Binds a DependencyProperty to another property on the same element (RelativeSource.Self).
        /// </summary>
        public static T BindSelf<T>(
            this T element,
            DependencyProperty dp,
            string path,
            BindingMode mode = BindingMode.OneWay,
            IValueConverter converter = null,
            string stringFormat = null)
            where T : FrameworkElement
        {
            var binding = CreateConfiguredBinding(
                path,
                mode,
                source: null,
                elementName: null,
                relativeSource: RelativeSource.Self,
                converter: converter,
                converterParameter: null,
                stringFormat: stringFormat,
                fallbackValue: null,
                targetNullValue: null,
                updateSourceTrigger: UpdateSourceTrigger.Default,
                delay: 0,
                validatesOnDataErrors: false,
                validatesOnNotifyDataErrors: false,
                validatesOnExceptions: false);
            BindingOperations.SetBinding(element, dp, binding);
            return element;
        }

        /// <summary>
        /// Binds a DependencyProperty to an ancestor of type <typeparamref name="TAncestor"/> in the visual tree.
        /// </summary>
        public static T BindAncestor<T, TAncestor>(
            this T element,
            DependencyProperty dp,
            string path,
            int ancestorLevel = 1,
            BindingMode mode = BindingMode.OneWay,
            IValueConverter converter = null,
            string stringFormat = null)
            where T : FrameworkElement
        {
            var rel = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TAncestor), ancestorLevel);
            var binding = CreateConfiguredBinding(
                path,
                mode,
                source: null,
                elementName: null,
                relativeSource: rel,
                converter: converter,
                converterParameter: null,
                stringFormat: stringFormat,
                fallbackValue: null,
                targetNullValue: null,
                updateSourceTrigger: UpdateSourceTrigger.Default,
                delay: 0,
                validatesOnDataErrors: false,
                validatesOnNotifyDataErrors: false,
                validatesOnExceptions: false);
            BindingOperations.SetBinding(element, dp, binding);
            return element;
        }

        /// <summary>
        /// Binds a DependencyProperty to its TemplatedParent inside a ControlTemplate.
        /// </summary>
        public static T BindTemplatedParent<T>(
            this T element,
            DependencyProperty dp,
            string path,
            BindingMode mode = BindingMode.OneWay,
            IValueConverter converter = null,
            string stringFormat = null)
            where T : FrameworkElement
        {
            var binding = CreateConfiguredBinding(
                path,
                mode,
                source: null,
                elementName: null,
                relativeSource: RelativeSource.TemplatedParent,
                converter: converter,
                converterParameter: null,
                stringFormat: stringFormat,
                fallbackValue: null,
                targetNullValue: null,
                updateSourceTrigger: UpdateSourceTrigger.Default,
                delay: 0,
                validatesOnDataErrors: false,
                validatesOnNotifyDataErrors: false,
                validatesOnExceptions: false);
            BindingOperations.SetBinding(element, dp, binding);
            return element;
        }

        /// <summary>
        /// Binds a DependencyProperty using a <see cref="MultiBinding"/> and <see cref="IMultiValueConverter"/>.
        /// </summary>
        public static T MultiBind<T>(
            this T element,
            DependencyProperty dp,
            IMultiValueConverter converter,
            string stringFormat = null,
            object fallbackValue = null,
            params BindingBase[] bindings)
            where T : FrameworkElement
        {
            var multi = new MultiBinding
            {
                Converter = converter,
                StringFormat = stringFormat
            };
            if (fallbackValue != null)
            {
                multi.FallbackValue = fallbackValue;
            }
            if (bindings != null)
            {
                for (int i = 0; i < bindings.Length; i++)
                {
                    if (bindings[i] != null)
                    {
                        multi.Bindings.Add(bindings[i]);
                    }
                }
            }
            BindingOperations.SetBinding(element, dp, multi);
            return element;
        }

        /// <summary>
        /// Binds a DependencyProperty using a <see cref="PriorityBinding"/>.
        /// </summary>
        public static T PriorityBind<T>(
            this T element,
            DependencyProperty dp,
            params BindingBase[] bindings)
            where T : FrameworkElement
        {
            var priority = new PriorityBinding();
            if (bindings != null)
            {
                for (int i = 0; i < bindings.Length; i++)
                {
                    if (bindings[i] != null)
                    {
                        priority.Bindings.Add(bindings[i]);
                    }
                }
            }
            BindingOperations.SetBinding(element, dp, priority);
            return element;
        }

        // Fluent Binding configuration extensions
        public static Binding WithConverter(this Binding binding, IValueConverter converter, object parameter = null)
        {
            binding.Converter = converter;
            binding.ConverterParameter = parameter;
            return binding;
        }

        public static Binding WithFormat(this Binding binding, string stringFormat)
        {
            binding.StringFormat = stringFormat;
            return binding;
        }

        public static Binding WithFallback(this Binding binding, object fallbackValue, object targetNullValue = null)
        {
            if (fallbackValue != null) binding.FallbackValue = fallbackValue;
            if (targetNullValue != null) binding.TargetNullValue = targetNullValue;
            return binding;
        }

        public static Binding WithTrigger(this Binding binding, UpdateSourceTrigger trigger, int delayMs = 0)
        {
            binding.UpdateSourceTrigger = trigger;
            if (delayMs > 0) binding.Delay = delayMs;
            return binding;
        }

        public static Binding WithValidation(
            this Binding binding,
            bool onExceptions = true,
            bool onDataErrors = true,
            bool onNotifyDataErrors = true,
            params ValidationRule[] customRules)
        {
            binding.ValidatesOnExceptions = onExceptions;
            binding.ValidatesOnDataErrors = onDataErrors;
            binding.ValidatesOnNotifyDataErrors = onNotifyDataErrors;
            if (customRules != null)
            {
                for (int i = 0; i < customRules.Length; i++)
                {
                    if (customRules[i] != null)
                    {
                        binding.ValidationRules.Add(customRules[i]);
                    }
                }
            }
            return binding;
        }

        internal static Binding CreateConfiguredBinding(
            string path,
            BindingMode mode,
            object source,
            string elementName,
            RelativeSource relativeSource,
            IValueConverter converter,
            object converterParameter,
            string stringFormat,
            object fallbackValue,
            object targetNullValue,
            UpdateSourceTrigger updateSourceTrigger,
            int delay,
            bool validatesOnDataErrors,
            bool validatesOnNotifyDataErrors,
            bool validatesOnExceptions)
        {
            var b = string.IsNullOrEmpty(path) ? new Binding() : new Binding(path);
            b.Mode = mode;
            if (source != null) b.Source = source;
            if (!string.IsNullOrEmpty(elementName)) b.ElementName = elementName;
            if (relativeSource != null) b.RelativeSource = relativeSource;
            if (converter != null)
            {
                b.Converter = converter;
                b.ConverterParameter = converterParameter;
            }
            if (!string.IsNullOrEmpty(stringFormat)) b.StringFormat = stringFormat;
            if (fallbackValue != null) b.FallbackValue = fallbackValue;
            if (targetNullValue != null) b.TargetNullValue = targetNullValue;
            b.UpdateSourceTrigger = updateSourceTrigger;
            if (delay > 0) b.Delay = delay;
            b.ValidatesOnDataErrors = validatesOnDataErrors;
            b.ValidatesOnNotifyDataErrors = validatesOnNotifyDataErrors;
            b.ValidatesOnExceptions = validatesOnExceptions;
            return b;
        }

        // =====================================================================
        // 6. Routed Events & Animations
        // =====================================================================

        public static T On<T>(this T element, RoutedEvent routedEvent, Delegate handler, bool handledEventsToo = false)
            where T : UIElement
        {
            element.AddHandler(routedEvent, handler, handledEventsToo);
            return element;
        }

        public static T OnMouseLeftDown<T>(this T element, MouseButtonEventHandler handler)
            where T : UIElement
        {
            element.MouseLeftButtonDown += handler;
            return element;
        }

        public static T OnMouseLeftUp<T>(this T element, MouseButtonEventHandler handler)
            where T : UIElement
        {
            element.MouseLeftButtonUp += handler;
            return element;
        }

        public static T OnMouseMove<T>(this T element, MouseEventHandler handler)
            where T : UIElement
        {
            element.MouseMove += handler;
            return element;
        }

        public static T OnLoaded<T>(this T element, RoutedEventHandler handler)
            where T : FrameworkElement
        {
            element.Loaded += handler;
            return element;
        }

        public static T AnimateDouble<T>(
            this T animatable,
            DependencyProperty dp,
            double from,
            double to,
            TimeSpan duration,
            IEasingFunction easingFunction = null)
            where T : IAnimatable
        {
            var anim = new DoubleAnimation(from, to, new Duration(duration))
            {
                EasingFunction = easingFunction
            };
            animatable.BeginAnimation(dp, anim);
            return animatable;
        }

        // =====================================================================
        // 7. Media3D Fluent Extensions
        // =====================================================================

        public static Viewport3D Camera(this Viewport3D viewport, Camera camera)
        {
            viewport.Camera = camera;
            return viewport;
        }

        public static Viewport3D Children3D(this Viewport3D viewport, params Visual3D[] visuals)
        {
            if (visuals != null)
            {
                for (int i = 0; i < visuals.Length; i++)
                {
                    if (visuals[i] != null)
                    {
                        viewport.Children.Add(visuals[i]);
                    }
                }
            }
            return viewport;
        }

        public static ContainerUIElement3D Children3D(this ContainerUIElement3D container, params Visual3D[] visuals)
        {
            if (visuals != null)
            {
                for (int i = 0; i < visuals.Length; i++)
                {
                    if (visuals[i] != null)
                    {
                        container.Children.Add(visuals[i]);
                    }
                }
            }
            return container;
        }

        public static T Transform3D<T>(this T visual3D, Transform3D transform)
            where T : Visual3D
        {
            visual3D.Transform = transform;
            return visual3D;
        }

        public static Viewport2DVisual3D Visual2D(this Viewport2DVisual3D vp2d3d, Visual visual)
        {
            vp2d3d.Visual = visual;
            return vp2d3d;
        }

        // =====================================================================
        // 8. Window, Application, Popup & ToolTip Fluent Extensions
        // =====================================================================

        public static T Title<T>(this T window, string title)
            where T : Window
        {
            window.Title = title;
            return window;
        }

        public static T StartupLocation<T>(this T window, WindowStartupLocation location)
            where T : Window
        {
            window.WindowStartupLocation = location;
            return window;
        }

        public static T WindowStyle<T>(this T window, WindowStyle style)
            where T : Window
        {
            window.WindowStyle = style;
            return window;
        }

        public static T WindowState<T>(this T window, WindowState state)
            where T : Window
        {
            window.WindowState = state;
            return window;
        }

        public static T ResizeMode<T>(this T window, ResizeMode mode)
            where T : Window
        {
            window.ResizeMode = mode;
            return window;
        }

        public static T SizeToContent<T>(this T window, SizeToContent sizeToContent)
            where T : Window
        {
            window.SizeToContent = sizeToContent;
            return window;
        }

        public static T Topmost<T>(this T window, bool topmost = true)
            where T : Window
        {
            window.Topmost = topmost;
            return window;
        }

        public static T ShowInTaskbar<T>(this T window, bool showInTaskbar = true)
            where T : Window
        {
            window.ShowInTaskbar = showInTaskbar;
            return window;
        }

        public static T WindowPosition<T>(this T window, double left, double top)
            where T : Window
        {
            window.Left = left;
            window.Top = top;
            return window;
        }

        public static T Owner<T>(this T window, Window owner)
            where T : Window
        {
            window.Owner = owner;
            return window;
        }

        public static T OnSourceInitialized<T>(this T window, EventHandler handler)
            where T : Window
        {
            window.SourceInitialized += handler;
            return window;
        }

        public static T OnContentRendered<T>(this T window, EventHandler handler)
            where T : Window
        {
            window.ContentRendered += handler;
            return window;
        }

        public static T OnClosing<T>(this T window, CancelEventHandler handler)
            where T : Window
        {
            window.Closing += handler;
            return window;
        }

        public static T OnClosed<T>(this T window, EventHandler handler)
            where T : Window
        {
            window.Closed += handler;
            return window;
        }

        public static T Backdrop<T>(this T window, WindowBackdropType backdropType)
            where T : Window
        {
            window.WindowBackdropType = backdropType;
            return window;
        }

        public static T ThemeMode<T>(this T window, ThemeMode mode)
            where T : Window
        {
            window.ThemeMode = mode;
            return window;
        }

        public static T ToolTip<T>(this T element, object toolTip)
            where T : FrameworkElement
        {
            element.ToolTip = toolTip;
            return element;
        }

        public static Popup Child(this Popup popup, UIElement child)
        {
            popup.Child = child;
            return popup;
        }

        public static Popup IsOpen(this Popup popup, bool isOpen)
        {
            popup.IsOpen = isOpen;
            return popup;
        }

        public static Popup Placement(this Popup popup, PlacementMode placement)
        {
            popup.Placement = placement;
            return popup;
        }

        public static Popup PlacementTarget(this Popup popup, UIElement target)
        {
            popup.PlacementTarget = target;
            return popup;
        }

        public static Popup Offset(this Popup popup, double horizontalOffset, double verticalOffset)
        {
            popup.HorizontalOffset = horizontalOffset;
            popup.VerticalOffset = verticalOffset;
            return popup;
        }

        public static Popup StaysOpen(this Popup popup, bool staysOpen = true)
        {
            popup.StaysOpen = staysOpen;
            return popup;
        }

        public static Popup OnOpened(this Popup popup, EventHandler handler)
        {
            popup.Opened += handler;
            return popup;
        }

        public static Popup OnClosed(this Popup popup, EventHandler handler)
        {
            popup.Closed += handler;
            return popup;
        }

        public static Application Resource(this Application app, object key, object value)
        {
            app.Resources[key] = value;
            return app;
        }

        // =====================================================================
        // SvgViewbox Declarative Fluent Extensions
        // =====================================================================

        public static SvgViewbox Source(this SvgViewbox viewbox, string source)
        {
            viewbox.Source = source;
            return viewbox;
        }

        public static SvgViewbox Stretch(this SvgViewbox viewbox, Stretch stretch)
        {
            viewbox.Stretch = stretch;
            return viewbox;
        }

        public static SvgViewbox Foreground(this SvgViewbox viewbox, Brush brush)
        {
            viewbox.Foreground = brush;
            return viewbox;
        }

        // =====================================================================
        // Modern Direct2D Effects & Acrylic Material Fluent Extensions
        // =====================================================================

        public static T Elevation<T>(this T element, double elevation = 4.0, double opacity = 0.35) where T : UIElement
        {
            var effect = new ModernElevationEffect(elevation);
            if (opacity > 0.0) effect.Opacity = opacity;
            element.Effect = effect;
            return element;
        }

        public static T ModernDropShadow<T>(this T element, double blurRadius = 16.0, double depth = 4.0, Color? color = null, double opacity = 0.35) where T : UIElement
        {
            element.Effect = new ModernElevationEffect
            {
                BlurRadius = blurRadius,
                ShadowDepth = depth,
                Color = color ?? Colors.Black,
                Opacity = opacity
            };
            return element;
        }

        public static Panel Acrylic(this Panel panel, Color tintColor, double tintOpacity = 0.72, double blurRadius = 24.0)
        {
            panel.Background = new AcrylicBrush(tintColor, tintOpacity, blurRadius);
            return panel;
        }

        public static Control Acrylic(this Control control, Color tintColor, double tintOpacity = 0.72, double blurRadius = 24.0)
        {
            control.Background = new AcrylicBrush(tintColor, tintOpacity, blurRadius);
            return control;
        }

        public static Window Acrylic(this Window window, Color tintColor, double tintOpacity = 0.72, double blurRadius = 24.0)
        {
            window.Background = new AcrylicBrush(tintColor, tintOpacity, blurRadius);
            return window;
        }

        // =====================================================================
        // Standard Layout Panels Fluent Extensions: Grid, StackPanel, DockPanel, WrapPanel
        // =====================================================================

        public static T Row<T>(this T element, int row) where T : UIElement
        {
            Grid.SetRow(element, row);
            return element;
        }

        public static T Column<T>(this T element, int column) where T : UIElement
        {
            Grid.SetColumn(element, column);
            return element;
        }

        public static T RowSpan<T>(this T element, int span) where T : UIElement
        {
            Grid.SetRowSpan(element, span);
            return element;
        }

        public static T ColumnSpan<T>(this T element, int span) where T : UIElement
        {
            Grid.SetColumnSpan(element, span);
            return element;
        }

        public static T Dock<T>(this T element, Dock dock) where T : UIElement
        {
            DockPanel.SetDock(element, dock);
            return element;
        }

        public static Grid Rows(this Grid grid, params string[] rowDefinitions)
        {
            ArgumentNullException.ThrowIfNull(rowDefinitions);
            grid.RowDefinitions.Clear();
            foreach (string r in rowDefinitions)
            {
                grid.RowDefinitions.Add(r);
            }
            return grid;
        }

        public static Grid Columns(this Grid grid, params string[] columnDefinitions)
        {
            ArgumentNullException.ThrowIfNull(columnDefinitions);
            grid.ColumnDefinitions.Clear();
            foreach (string c in columnDefinitions)
            {
                grid.ColumnDefinitions.Add(c);
            }
            return grid;
        }

        public static StackPanel Orientation(this StackPanel panel, Orientation orientation)
        {
            panel.Orientation = orientation;
            return panel;
        }

        public static StackPanel Horizontal(this StackPanel panel)
        {
            panel.Orientation = System.Windows.Controls.Orientation.Horizontal;
            return panel;
        }

        public static StackPanel Vertical(this StackPanel panel)
        {
            panel.Orientation = System.Windows.Controls.Orientation.Vertical;
            return panel;
        }

        public static StackPanel Spacing(this StackPanel panel, double spacing)
        {
            panel.Spacing = spacing;
            return panel;
        }

        public static DockPanel LastChildFill(this DockPanel panel, bool fill)
        {
            panel.LastChildFill = fill;
            return panel;
        }

        public static WrapPanel ItemSize(this WrapPanel panel, double width, double height)
        {
            panel.ItemWidth = width;
            panel.ItemHeight = height;
            return panel;
        }

        // Canvas positioning
        public static T CanvasLeft<T>(this T element, double left) where T : UIElement
        {
            Canvas.SetLeft(element, left);
            return element;
        }

        public static T CanvasTop<T>(this T element, double top) where T : UIElement
        {
            Canvas.SetTop(element, top);
            return element;
        }

        public static T CanvasRight<T>(this T element, double right) where T : UIElement
        {
            Canvas.SetRight(element, right);
            return element;
        }

        public static T CanvasBottom<T>(this T element, double bottom) where T : UIElement
        {
            Canvas.SetBottom(element, bottom);
            return element;
        }

        public static T CanvasPosition<T>(this T element, double? left = null, double? top = null, double? right = null, double? bottom = null) where T : UIElement
        {
            if (left.HasValue) Canvas.SetLeft(element, left.Value);
            if (top.HasValue) Canvas.SetTop(element, top.Value);
            if (right.HasValue) Canvas.SetRight(element, right.Value);
            if (bottom.HasValue) Canvas.SetBottom(element, bottom.Value);
            return element;
        }

        // UniformGrid
        public static UniformGrid UniformRows(this UniformGrid grid, int rows)
        {
            grid.Rows = rows;
            return grid;
        }

        public static UniformGrid UniformColumns(this UniformGrid grid, int columns)
        {
            grid.Columns = columns;
            return grid;
        }

        public static UniformGrid FirstColumn(this UniformGrid grid, int firstColumn)
        {
            grid.FirstColumn = firstColumn;
            return grid;
        }

        // Border & Viewbox Decorators
        public static Border BorderThickness(this Border border, double uniform)
        {
            border.BorderThickness = new Thickness(uniform);
            return border;
        }

        public static Border BorderThickness(this Border border, double left, double top, double right, double bottom)
        {
            border.BorderThickness = new Thickness(left, top, right, bottom);
            return border;
        }

        public static Border Padding(this Border border, double uniform)
        {
            border.Padding = new Thickness(uniform);
            return border;
        }

        public static Border Padding(this Border border, double left, double top, double right, double bottom)
        {
            border.Padding = new Thickness(left, top, right, bottom);
            return border;
        }

        public static Border CornerRadius(this Border border, double uniform)
        {
            border.CornerRadius = new CornerRadius(uniform);
            return border;
        }

        public static Border CornerRadius(this Border border, double tl, double tr, double br, double bl)
        {
            border.CornerRadius = new CornerRadius(tl, tr, br, bl);
            return border;
        }

        public static Border BorderBrush(this Border border, Brush brush)
        {
            border.BorderBrush = brush;
            return border;
        }

        public static Border BorderBrush(this Border border, Color color)
        {
            border.BorderBrush = new SolidColorBrush(color);
            return border;
        }

        public static Border Background(this Border border, Brush brush)
        {
            border.Background = brush;
            return border;
        }

        public static Border Background(this Border border, Color color)
        {
            border.Background = new SolidColorBrush(color);
            return border;
        }

        public static Border Child(this Border border, UIElement child)
        {
            border.Child = child;
            return border;
        }

        public static Viewbox Stretch(this Viewbox vb, Stretch stretch)
        {
            vb.Stretch = stretch;
            return vb;
        }

        public static Viewbox StretchDirection(this Viewbox vb, StretchDirection direction)
        {
            vb.StretchDirection = direction;
            return vb;
        }

        public static Viewbox Child(this Viewbox vb, UIElement child)
        {
            vb.Child = child;
            return vb;
        }

        // ScrollViewer & VirtualizingStackPanel
        public static ScrollViewer HorizontalScrollBar(this ScrollViewer viewer, ScrollBarVisibility visibility)
        {
            viewer.HorizontalScrollBarVisibility = visibility;
            return viewer;
        }

        public static ScrollViewer VerticalScrollBar(this ScrollViewer viewer, ScrollBarVisibility visibility)
        {
            viewer.VerticalScrollBarVisibility = visibility;
            return viewer;
        }

        public static ScrollViewer CanContentScroll(this ScrollViewer viewer, bool canScroll)
        {
            viewer.CanContentScroll = canScroll;
            return viewer;
        }

        public static ScrollViewer SmoothScrolling(this ScrollViewer viewer, bool enabled, double friction = 0.80)
        {
            viewer.IsSmoothScrollingEnabled = enabled;
            viewer.Friction = friction;
            return viewer;
        }

        public static VirtualizingStackPanel Orientation(this VirtualizingStackPanel panel, Orientation orientation)
        {
            panel.Orientation = orientation;
            return panel;
        }

        public static VirtualizingStackPanel Spacing(this VirtualizingStackPanel panel, double spacing)
        {
            panel.Spacing = spacing;
            return panel;
        }

        public static VirtualizingStackPanel VirtualItems(this VirtualizingStackPanel panel, int count, Func<int, UIElement> generator)
        {
            panel.VirtualItemCount = count;
            panel.ItemGenerator = generator;
            return panel;
        }

        // TextBlock
        public static TextBlock Text(this TextBlock block, string text)
        {
            block.Text = text;
            return block;
        }

        public static TextBlock FontSize(this TextBlock block, double size)
        {
            block.FontSize = size;
            return block;
        }

        public static TextBlock FontWeight(this TextBlock block, FontWeight weight)
        {
            block.FontWeight = weight;
            return block;
        }

        public static TextBlock Bold(this TextBlock block)
        {
            block.FontWeight = FontWeights.Bold;
            return block;
        }

        public static TextBlock Foreground(this TextBlock block, Brush brush)
        {
            block.Foreground = brush;
            return block;
        }

        public static TextBlock Background(this TextBlock block, Brush brush)
        {
            block.Background = brush;
            return block;
        }

        public static TextBlock TextWrapping(this TextBlock block, TextWrapping wrapping)
        {
            block.TextWrapping = wrapping;
            return block;
        }

        public static TextBlock TextTrimming(this TextBlock block, TextTrimming trimming)
        {
            block.TextTrimming = trimming;
            return block;
        }

        public static TextBlock TextAlignment(this TextBlock block, TextAlignment alignment)
        {
            block.TextAlignment = alignment;
            return block;
        }

        public static TextBlock Padding(this TextBlock block, double uniform)
        {
            block.Padding = new Thickness(uniform);
            return block;
        }

        public static TextBlock Padding(this TextBlock block, double horizontal, double vertical)
        {
            block.Padding = new Thickness(horizontal, vertical, horizontal, vertical);
            return block;
        }

        // TextFlowView
        public static TextFlowView FontSize(this TextFlowView view, double size)
        {
            view.FontSize = size;
            return view;
        }

        public static TextFlowView Foreground(this TextFlowView view, Brush brush)
        {
            view.Foreground = brush;
            return view;
        }

        public static TextFlowView LineSpacing(this TextFlowView view, double spacing)
        {
            view.LineSpacing = spacing;
            return view;
        }

        public static TextFlowView Padding(this TextFlowView view, double uniform)
        {
            view.Padding = new Thickness(uniform);
            return view;
        }
    }
}
