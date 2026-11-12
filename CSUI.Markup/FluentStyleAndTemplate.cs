using System;
using System.Windows;
using System.Windows.Controls;

namespace CSUI.Markup
{
    public sealed class TriggerBuilder
    {
        private readonly Trigger _trigger;

        internal TriggerBuilder(Trigger trigger)
        {
            _trigger = trigger;
        }

        public TriggerBuilder Set(DependencyProperty property, object value, string targetName = null)
        {
            var setter = new Setter(property, value);
            if (!string.IsNullOrEmpty(targetName))
            {
                setter.TargetName = targetName;
            }
            _trigger.Setters.Add(setter);
            return this;
        }
    }

    public sealed class StyleBuilder<TTarget> where TTarget : FrameworkElement
    {
        private readonly Style _style;

        internal StyleBuilder(Style basedOn = null)
        {
            _style = basedOn != null
                ? new Style(typeof(TTarget), basedOn)
                : new Style(typeof(TTarget));
        }

        public StyleBuilder<TTarget> Set(DependencyProperty property, object value)
        {
            _style.Setters.Add(new Setter(property, value));
            return this;
        }

        public StyleBuilder<TTarget> When(DependencyProperty property, object value, Action<TriggerBuilder> configureTrigger)
        {
            var trigger = new Trigger
            {
                Property = property,
                Value = value
            };
            configureTrigger?.Invoke(new TriggerBuilder(trigger));
            _style.Triggers.Add(trigger);
            return this;
        }

        public StyleBuilder<TTarget> Template(ControlTemplate controlTemplate)
        {
            _style.Setters.Add(new Setter(Control.TemplateProperty, controlTemplate));
            return this;
        }

        public Style Build() => _style;

        public static implicit operator Style(StyleBuilder<TTarget> builder) => builder?._style;
    }

    public sealed class TemplateFactoryBuilder
    {
        public FrameworkElementFactory Factory { get; }

        internal TemplateFactoryBuilder(FrameworkElementFactory factory)
        {
            Factory = factory;
        }

        public TemplateFactoryBuilder Set(DependencyProperty property, object value)
        {
            Factory.SetValue(property, value);
            return this;
        }

        public TemplateFactoryBuilder TemplateBind(DependencyProperty targetProperty, DependencyProperty sourceProperty)
        {
            Factory.SetValue(targetProperty, new TemplateBindingExtension(sourceProperty));
            return this;
        }

        public TemplateFactoryBuilder Child(TemplateFactoryBuilder childBuilder)
        {
            if (childBuilder?.Factory != null)
            {
                Factory.AppendChild(childBuilder.Factory);
            }
            return this;
        }

        public TemplateFactoryBuilder Children(params TemplateFactoryBuilder[] children)
        {
            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i]?.Factory != null)
                    {
                        Factory.AppendChild(children[i].Factory);
                    }
                }
            }
            return this;
        }

        public ControlTemplate ToControlTemplate<TControl>() where TControl : Control
        {
            return new ControlTemplate(typeof(TControl))
            {
                VisualTree = Factory
            };
        }

        public static implicit operator FrameworkElementFactory(TemplateFactoryBuilder builder) => builder?.Factory;
    }

    public static class MarkupStyle
    {
        public static StyleBuilder<T> For<T>(Style basedOn = null) where T : FrameworkElement
        {
            return new StyleBuilder<T>(basedOn);
        }
    }

    public static class MarkupTemplate
    {
        public static TemplateFactoryBuilder Node<TElement>(string name = null)
            where TElement : DependencyObject, new()
        {
            return new TemplateFactoryBuilder(FrameworkElementFactory.Create<TElement>(name));
        }

        public static ControlTemplate For<TControl, TRoot>(
            string rootName = null,
            Action<TemplateFactoryBuilder> configureRoot = null)
            where TControl : Control
            where TRoot : DependencyObject, new()
        {
            var rootBuilder = Node<TRoot>(rootName);
            configureRoot?.Invoke(rootBuilder);
            return rootBuilder.ToControlTemplate<TControl>();
        }
    }
}
