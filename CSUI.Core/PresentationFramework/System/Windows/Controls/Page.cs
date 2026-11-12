// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;
using MS.Internal.Controls;

namespace System.Windows.Controls
{
    [ContentProperty(nameof(Content))]
    public class Page : FrameworkElement, IWindowService, IAddChild
    {
        public static readonly DependencyProperty ContentProperty =
            ContentControl.ContentProperty.AddOwner(
                typeof(Page),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnContentChanged));

        public static readonly DependencyProperty BackgroundProperty =
            Control.BackgroundProperty.AddOwner(
                typeof(Page),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ForegroundProperty =
            Control.ForegroundProperty.AddOwner(typeof(Page));

        public static readonly DependencyProperty FontFamilyProperty =
            Control.FontFamilyProperty.AddOwner(typeof(Page));

        public static readonly DependencyProperty FontSizeProperty =
            Control.FontSizeProperty.AddOwner(typeof(Page));

        public static readonly DependencyProperty TemplateProperty =
            Control.TemplateProperty.AddOwner(
                typeof(Page),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(Page),
                new FrameworkPropertyMetadata(null, OnTitleChanged));

        public static readonly DependencyProperty KeepAliveProperty = JournalEntry.KeepAliveProperty.AddOwner(typeof(Page));

        private UIElement _visualChild;
        private string _windowTitle;
        private double _windowWidth = double.NaN;
        private double _windowHeight = double.NaN;
        private bool _showsNavigationUI = true;

        static Page()
        {
            ControlFoundationHooks.IsValidControlTemplateTargetTypeHook = static t => typeof(Page).IsAssignableFrom(t);
            FocusableProperty.OverrideMetadata(typeof(Page), new FrameworkPropertyMetadata(false));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Page), new FrameworkPropertyMetadata(typeof(Page)));
        }

        public Page()
        {
        }

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public ControlTemplate Template
        {
            get => (ControlTemplate)GetValue(TemplateProperty);
            set => SetValue(TemplateProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => _windowTitle = value;
        }

        public double WindowWidth
        {
            get => _windowWidth;
            set => _windowWidth = value;
        }

        public double WindowHeight
        {
            get => _windowHeight;
            set => _windowHeight = value;
        }

        public bool ShowsNavigationUI
        {
            get => _showsNavigationUI;
            set => _showsNavigationUI = value;
        }

        public bool KeepAlive
        {
            get => JournalEntry.GetKeepAlive(this);
            set => JournalEntry.SetKeepAlive(this, value);
        }

        public NavigationService NavigationService => NavigationService.GetNavigationService(this);

        protected internal override IEnumerator LogicalChildren => new SingleChildEnumerator(Content);

        protected override int VisualChildrenCount => _visualChild != null ? 1 : 0;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0 || _visualChild == null)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _visualChild;
        }

        void IAddChild.AddChild(object obj)
        {
            VerifyAccess();
            if (Content == null || obj == null)
            {
                Content = obj;
            }
            else
            {
                throw new InvalidOperationException("Page cannot have multiple children.");
            }
        }

        void IAddChild.AddText(string str)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                Content = str;
            }
        }

        string IWindowService.Title
        {
            get => WindowTitle;
            set => WindowTitle = value;
        }

        double IWindowService.Width
        {
            get => WindowWidth;
            set => WindowWidth = value;
        }

        double IWindowService.Height
        {
            get => WindowHeight;
            set => WindowHeight = value;
        }

        bool IWindowService.UserResized => false;

        protected override Size MeasureOverride(Size constraint)
        {
            if (_visualChild != null)
            {
                _visualChild.Measure(constraint);
                return _visualChild.DesiredSize;
            }

            return new Size(0, 0);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _visualChild?.Arrange(new Rect(new Point(0, 0), arrangeBounds));
            return arrangeBounds;
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (Background is Brush bg && RenderSize.Width > 0 && RenderSize.Height > 0)
            {
                dc.DrawRectangle(bg, null, new Rect(new Point(0, 0), RenderSize));
            }
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Page page)
            {
                if (e.OldValue != null)
                {
                    page.RemoveLogicalChild(e.OldValue);
                }

                if (page._visualChild != null)
                {
                    page.RemoveVisualChild(page._visualChild);
                    page._visualChild = null;
                }

                if (e.NewValue != null)
                {
                    page.AddLogicalChild(e.NewValue);
                    if (e.NewValue is UIElement ui)
                    {
                        page._visualChild = ui;
                        page.AddVisualChild(ui);
                    }
                    else
                    {
                        var tb = new TextBlock { Text = e.NewValue.ToString() };
                        page._visualChild = tb;
                        page.AddVisualChild(tb);
                    }
                }
            }
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Page page && page.GetValue(Window.IWindowServiceProperty) is Window window && e.NewValue is string title)
            {
                window.Title = title;
            }
        }
    }
}

namespace System.Windows.Navigation
{
    public abstract class PageFunctionBase : Controls.Page
    {
        public static readonly DependencyProperty RemoveFromJournalProperty =
            DependencyProperty.Register(
                nameof(RemoveFromJournal),
                typeof(bool),
                typeof(PageFunctionBase),
                new FrameworkPropertyMetadata(false));

        public bool RemoveFromJournal
        {
            get => (bool)GetValue(RemoveFromJournalProperty);
            set => SetValue(RemoveFromJournalProperty, value);
        }

        protected virtual void Start()
        {
        }

        internal abstract void RaiseReturn(object result);
    }

    public class PageFunction<T> : PageFunctionBase
    {
        public event ReturnEventHandler<T> Return;

        public PageFunction()
        {
        }

        protected void OnReturn(ReturnEventArgs<T> e)
        {
            Return?.Invoke(this, e);
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        internal override void RaiseReturn(object result)
        {
            OnReturn(new ReturnEventArgs<T>((T)result));
        }
    }
}
