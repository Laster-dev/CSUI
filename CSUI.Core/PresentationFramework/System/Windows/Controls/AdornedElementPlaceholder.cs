// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.ComponentModel;
using System.Windows.Markup;
using System.Windows.Media;
using MS.Internal.Controls;
using MS.Internal.PresentationFramework;

namespace System.Windows.Controls
{
    [ContentProperty("Child")]
    public class AdornedElementPlaceholder : FrameworkElement, IAddChild
    {
        public AdornedElementPlaceholder() : base()
        {
        }

        void IAddChild.AddChild(object value)
        {
            if (value == null)
            {
                return;
            }

            if (!(value is UIElement))
            {
                throw new ArgumentException(SR.Format(SR.UnexpectedParameterType, value.GetType(), typeof(UIElement)), nameof(value));
            }

            if (this.Child != null)
            {
                throw new ArgumentException(SR.Format(SR.CanOnlyHaveOneChild, this.GetType(), value.GetType()));
            }

            this.Child = (UIElement)value;
        }

        void IAddChild.AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

        public UIElement AdornedElement
        {
            get
            {
                TemplatedAdorner adorner = TemplatedAdorner;
                return adorner == null ? null : TemplatedAdorner.AdornedElement;
            }
        }

        [DefaultValue(null)]
        public virtual UIElement Child
        {
            get
            {
                return _child;
            }

            set
            {
                UIElement old = _child;

                if (old != value)
                {
                    RemoveVisualChild(old);
                    RemoveLogicalChild(old);
                    _child = value;
                    AddVisualChild(_child);
                    AddLogicalChild(value);

                    InvalidateMeasure();
                }
            }
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return (_child == null) ? 0 : 1;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (_child == null || index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.Visual_ArgumentOutOfRange);
            }

            return _child;
        }

        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                return new SingleChildEnumerator(_child);
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            if (TemplatedParent == null)
                throw new InvalidOperationException(SR.AdornedElementPlaceholderMustBeInTemplate);

            base.OnInitialized(e);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (TemplatedParent == null)
                throw new InvalidOperationException(SR.AdornedElementPlaceholderMustBeInTemplate);

            if (AdornedElement == null)
                return new Size(0, 0);

            Size desiredSize = AdornedElement.RenderSize;
            UIElement child = Child;

            if (child != null)
                child.Measure(desiredSize);

            return desiredSize;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            UIElement child = Child;

            if (child != null)
                child.Arrange(new Rect(arrangeBounds));

            return arrangeBounds;
        }

        private TemplatedAdorner TemplatedAdorner
        {
            get
            {
                if (_templatedAdorner == null)
                {
                    FrameworkElement templateParent = this.TemplatedParent as FrameworkElement;

                    if (templateParent != null)
                    {
                        _templatedAdorner = VisualTreeHelper.GetParent(templateParent) as TemplatedAdorner;

                        if (_templatedAdorner != null && _templatedAdorner.ReferenceElement == null)
                        {
                            _templatedAdorner.ReferenceElement = this;
                        }
                    }
                }
                return _templatedAdorner;
            }
        }

        private UIElement _child;
        private TemplatedAdorner _templatedAdorner;
    }
}
