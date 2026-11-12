// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace System.Windows.Controls.Ribbon
{
    public class RibbonGalleryItem : ContentControl
    {
        public static readonly RoutedEvent SelectedEvent = Selector.SelectedEvent.AddOwner(typeof(RibbonGalleryItem));
        public static readonly RoutedEvent UnselectedEvent = Selector.UnselectedEvent.AddOwner(typeof(RibbonGalleryItem));

        public static readonly DependencyProperty IsSelectedProperty =
            Selector.IsSelectedProperty.AddOwner(typeof(RibbonGalleryItem),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsSelectedChanged));

        public static readonly DependencyProperty IsHighlightedProperty =
            DependencyProperty.Register(nameof(IsHighlighted), typeof(bool), typeof(RibbonGalleryItem), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsPressedProperty =
            DependencyProperty.Register(nameof(IsPressed), typeof(bool), typeof(RibbonGalleryItem), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty MouseOverBorderBrushProperty = RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonGalleryItem));
        public static readonly DependencyProperty MouseOverBackgroundProperty = RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonGalleryItem));
        public static readonly DependencyProperty PressedBorderBrushProperty = RibbonControlService.PressedBorderBrushProperty.AddOwner(typeof(RibbonGalleryItem));
        public static readonly DependencyProperty PressedBackgroundProperty = RibbonControlService.PressedBackgroundProperty.AddOwner(typeof(RibbonGalleryItem));
        public static readonly DependencyProperty CheckedBorderBrushProperty = RibbonControlService.CheckedBorderBrushProperty.AddOwner(typeof(RibbonGalleryItem));
        public static readonly DependencyProperty CheckedBackgroundProperty = RibbonControlService.CheckedBackgroundProperty.AddOwner(typeof(RibbonGalleryItem));
        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonGalleryItem));
        public static readonly DependencyProperty KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonGalleryItem));

        public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
        public bool IsHighlighted { get => (bool)GetValue(IsHighlightedProperty); set => SetValue(IsHighlightedProperty, value); }
        public bool IsPressed { get => (bool)GetValue(IsPressedProperty); set => SetValue(IsPressedProperty, value); }
        public Brush MouseOverBorderBrush { get => (Brush)GetValue(MouseOverBorderBrushProperty); set => SetValue(MouseOverBorderBrushProperty, value); }
        public Brush MouseOverBackground { get => (Brush)GetValue(MouseOverBackgroundProperty); set => SetValue(MouseOverBackgroundProperty, value); }
        public Brush PressedBorderBrush { get => (Brush)GetValue(PressedBorderBrushProperty); set => SetValue(PressedBorderBrushProperty, value); }
        public Brush PressedBackground { get => (Brush)GetValue(PressedBackgroundProperty); set => SetValue(PressedBackgroundProperty, value); }
        public Brush CheckedBorderBrush { get => (Brush)GetValue(CheckedBorderBrushProperty); set => SetValue(CheckedBorderBrushProperty, value); }
        public Brush CheckedBackground { get => (Brush)GetValue(CheckedBackgroundProperty); set => SetValue(CheckedBackgroundProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);
        public string KeyTip { get => (string)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

        public event RoutedEventHandler Selected { add => AddHandler(SelectedEvent, value); remove => RemoveHandler(SelectedEvent, value); }
        public event RoutedEventHandler Unselected { add => AddHandler(UnselectedEvent, value); remove => RemoveHandler(UnselectedEvent, value); }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = (RibbonGalleryItem)d;
            if ((bool)e.NewValue)
            {
                item.OnSelected(new RoutedEventArgs(SelectedEvent, item));
            }
            else
            {
                item.OnUnselected(new RoutedEventArgs(UnselectedEvent, item));
            }
        }

        protected virtual void OnSelected(RoutedEventArgs e) => RaiseEvent(e);
        protected virtual void OnUnselected(RoutedEventArgs e) => RaiseEvent(e);

        protected override Size MeasureOverride(Size constraint)
        {
            if (VisualChildrenCount > 0) return base.MeasureOverride(constraint);
            string text = Content?.ToString() ?? string.Empty;
            return new Size(Math.Min(Math.Max(32.0, text.Length * 7.2 + 16.0), constraint.Width), Math.Min(24.0, constraint.Height));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (VisualChildrenCount > 0) { base.OnRender(drawingContext); return; }
            RibbonVisualDefaults.DrawLabelAndChrome(drawingContext, this, RenderSize, Content?.ToString(), IsHighlighted || IsMouseOver, IsPressed || IsSelected);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonGalleryItemAutomationPeer(this);
    }

    public class RibbonGalleryCategory : HeaderedItemsControl
    {
        public static readonly DependencyProperty ColumnsStretchToFillProperty =
            DependencyProperty.Register(nameof(ColumnsStretchToFill), typeof(bool), typeof(RibbonGalleryCategory), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsSharedColumnSizeScopeProperty =
            DependencyProperty.Register(nameof(IsSharedColumnSizeScope), typeof(bool), typeof(RibbonGalleryCategory), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty MaxColumnCountProperty =
            DependencyProperty.Register(nameof(MaxColumnCount), typeof(int), typeof(RibbonGalleryCategory), new FrameworkPropertyMetadata(int.MaxValue));

        public static readonly DependencyProperty MinColumnCountProperty =
            DependencyProperty.Register(nameof(MinColumnCount), typeof(int), typeof(RibbonGalleryCategory), new FrameworkPropertyMetadata(1));

        public static readonly DependencyProperty HeaderVisibilityProperty =
            DependencyProperty.Register(nameof(HeaderVisibility), typeof(Visibility), typeof(RibbonGalleryCategory), new FrameworkPropertyMetadata(Visibility.Visible));

        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonGalleryCategory));

        public bool ColumnsStretchToFill { get => (bool)GetValue(ColumnsStretchToFillProperty); set => SetValue(ColumnsStretchToFillProperty, value); }
        public bool IsSharedColumnSizeScope { get => (bool)GetValue(IsSharedColumnSizeScopeProperty); set => SetValue(IsSharedColumnSizeScopeProperty, value); }
        public int MaxColumnCount { get => (int)GetValue(MaxColumnCountProperty); set => SetValue(MaxColumnCountProperty, value); }
        public int MinColumnCount { get => (int)GetValue(MinColumnCountProperty); set => SetValue(MinColumnCountProperty, value); }
        public Visibility HeaderVisibility { get => (Visibility)GetValue(HeaderVisibilityProperty); set => SetValue(HeaderVisibilityProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);

        protected override bool IsItemItsOwnContainerOverride(object item) => item is RibbonGalleryItem;
        protected override DependencyObject GetContainerForItemOverride() => new RibbonGalleryItem();

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonGalleryCategoryAutomationPeer(this);
    }

    public class RibbonGallery : ItemsControl, ICommandSource
    {
        public static readonly RoutedEvent SelectionChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(SelectionChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object>), typeof(RibbonGallery));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(RibbonGallery),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        public static readonly DependencyProperty SelectedValueProperty =
            Selector.SelectedValueProperty.AddOwner(typeof(RibbonGallery));

        public static readonly DependencyProperty SelectedValuePathProperty =
            Selector.SelectedValuePathProperty.AddOwner(typeof(RibbonGallery));

        private static readonly DependencyPropertyKey HighlightedItemPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HighlightedItem), typeof(object), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty HighlightedItemProperty = HighlightedItemPropertyKey.DependencyProperty;

        public static readonly DependencyProperty CommandProperty = ButtonBase.CommandProperty.AddOwner(typeof(RibbonGallery));
        public static readonly DependencyProperty CommandParameterProperty = ButtonBase.CommandParameterProperty.AddOwner(typeof(RibbonGallery));
        public static readonly DependencyProperty CommandTargetProperty = ButtonBase.CommandTargetProperty.AddOwner(typeof(RibbonGallery));
        public static readonly DependencyProperty ColumnsStretchToFillProperty = RibbonGalleryCategory.ColumnsStretchToFillProperty.AddOwner(typeof(RibbonGallery));
        public static readonly DependencyProperty IsSharedColumnSizeScopeProperty = RibbonGalleryCategory.IsSharedColumnSizeScopeProperty.AddOwner(typeof(RibbonGallery));
        public static readonly DependencyProperty MaxColumnCountProperty = RibbonGalleryCategory.MaxColumnCountProperty.AddOwner(typeof(RibbonGallery));
        public static readonly DependencyProperty MinColumnCountProperty = RibbonGalleryCategory.MinColumnCountProperty.AddOwner(typeof(RibbonGallery));
        public static readonly DependencyProperty CanUserFilterProperty = DependencyProperty.Register(nameof(CanUserFilter), typeof(bool), typeof(RibbonGallery), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty FilterPaneContentProperty = DependencyProperty.Register(nameof(FilterPaneContent), typeof(object), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty FilterPaneContentTemplateProperty = DependencyProperty.Register(nameof(FilterPaneContentTemplate), typeof(DataTemplate), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty FilterItemContainerStyleProperty = DependencyProperty.Register(nameof(FilterItemContainerStyle), typeof(Style), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty FilterItemTemplateProperty = DependencyProperty.Register(nameof(FilterItemTemplate), typeof(DataTemplate), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty FilterMenuButtonStyleProperty = DependencyProperty.Register(nameof(FilterMenuButtonStyle), typeof(Style), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty GalleryItemStyleProperty = DependencyProperty.Register(nameof(GalleryItemStyle), typeof(Style), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty GalleryItemTemplateProperty = DependencyProperty.Register(nameof(GalleryItemTemplate), typeof(DataTemplate), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonGallery));

        public object SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
        public object SelectedValue { get => GetValue(SelectedValueProperty); set => SetValue(SelectedValueProperty, value); }
        public string SelectedValuePath { get => (string)GetValue(SelectedValuePathProperty); set => SetValue(SelectedValuePathProperty, value); }
        public object HighlightedItem { get => GetValue(HighlightedItemProperty); internal set => SetValue(HighlightedItemPropertyKey, value); }
        public ICommand Command { get => (ICommand)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
        public object CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
        public IInputElement CommandTarget { get => (IInputElement)GetValue(CommandTargetProperty); set => SetValue(CommandTargetProperty, value); }
        public bool ColumnsStretchToFill { get => (bool)GetValue(ColumnsStretchToFillProperty); set => SetValue(ColumnsStretchToFillProperty, value); }
        public bool IsSharedColumnSizeScope { get => (bool)GetValue(IsSharedColumnSizeScopeProperty); set => SetValue(IsSharedColumnSizeScopeProperty, value); }
        public int MaxColumnCount { get => (int)GetValue(MaxColumnCountProperty); set => SetValue(MaxColumnCountProperty, value); }
        public int MinColumnCount { get => (int)GetValue(MinColumnCountProperty); set => SetValue(MinColumnCountProperty, value); }
        public bool CanUserFilter { get => (bool)GetValue(CanUserFilterProperty); set => SetValue(CanUserFilterProperty, value); }
        public object FilterPaneContent { get => GetValue(FilterPaneContentProperty); set => SetValue(FilterPaneContentProperty, value); }
        public DataTemplate FilterPaneContentTemplate { get => (DataTemplate)GetValue(FilterPaneContentTemplateProperty); set => SetValue(FilterPaneContentTemplateProperty, value); }
        public Style FilterItemContainerStyle { get => (Style)GetValue(FilterItemContainerStyleProperty); set => SetValue(FilterItemContainerStyleProperty, value); }
        public DataTemplate FilterItemTemplate { get => (DataTemplate)GetValue(FilterItemTemplateProperty); set => SetValue(FilterItemTemplateProperty, value); }
        public Style FilterMenuButtonStyle { get => (Style)GetValue(FilterMenuButtonStyleProperty); set => SetValue(FilterMenuButtonStyleProperty, value); }
        public Style GalleryItemStyle { get => (Style)GetValue(GalleryItemStyleProperty); set => SetValue(GalleryItemStyleProperty, value); }
        public DataTemplate GalleryItemTemplate { get => (DataTemplate)GetValue(GalleryItemTemplateProperty); set => SetValue(GalleryItemTemplateProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);

        public event RoutedPropertyChangedEventHandler<object> SelectionChanged
        {
            add => AddHandler(SelectionChangedEvent, value);
            remove => RemoveHandler(SelectionChangedEvent, value);
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gallery = (RibbonGallery)d;
            gallery.RaiseEvent(new RoutedPropertyChangedEventArgs<object>(e.OldValue, e.NewValue, SelectionChangedEvent));
            gallery.Command?.Execute(gallery.CommandParameter ?? e.NewValue);
        }

        protected override bool IsItemItsOwnContainerOverride(object item) => item is RibbonGalleryCategory;
        protected override DependencyObject GetContainerForItemOverride() => new RibbonGalleryCategory();

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonGalleryAutomationPeer(this);
    }

    public class RibbonApplicationMenuItem : RibbonMenuItem
    {
        public static readonly DependencyProperty LevelProperty =
            DependencyProperty.Register(nameof(Level), typeof(int), typeof(RibbonApplicationMenuItem), new FrameworkPropertyMetadata(0));

        public int Level
        {
            get => (int)GetValue(LevelProperty);
            set => SetValue(LevelProperty, value);
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
            => item is RibbonApplicationMenuItem || item is RibbonApplicationSplitMenuItem || item is RibbonSeparator || item is RibbonGallery;

        protected override DependencyObject GetContainerForItemOverride() => new RibbonApplicationMenuItem();
    }

    public class RibbonApplicationSplitMenuItem : RibbonSplitMenuItem
    {
        protected override bool IsItemItsOwnContainerOverride(object item)
            => item is RibbonApplicationMenuItem || item is RibbonApplicationSplitMenuItem || item is RibbonSeparator || item is RibbonGallery;

        protected override DependencyObject GetContainerForItemOverride() => new RibbonApplicationMenuItem();
    }

    public class RibbonApplicationMenu : RibbonMenuButton
    {
        public static readonly DependencyProperty FooterPaneContentProperty =
            DependencyProperty.Register(nameof(FooterPaneContent), typeof(object), typeof(RibbonApplicationMenu), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty FooterPaneContentTemplateProperty =
            DependencyProperty.Register(nameof(FooterPaneContentTemplate), typeof(DataTemplate), typeof(RibbonApplicationMenu), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty AuxiliaryPaneContentProperty =
            DependencyProperty.Register(nameof(AuxiliaryPaneContent), typeof(object), typeof(RibbonApplicationMenu), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty AuxiliaryPaneContentTemplateProperty =
            DependencyProperty.Register(nameof(AuxiliaryPaneContentTemplate), typeof(DataTemplate), typeof(RibbonApplicationMenu), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty MenuHeaderVisibilityProperty =
            DependencyProperty.Register(nameof(MenuHeaderVisibility), typeof(Visibility), typeof(RibbonApplicationMenu), new FrameworkPropertyMetadata(Visibility.Visible));

        public object FooterPaneContent { get => GetValue(FooterPaneContentProperty); set => SetValue(FooterPaneContentProperty, value); }
        public DataTemplate FooterPaneContentTemplate { get => (DataTemplate)GetValue(FooterPaneContentTemplateProperty); set => SetValue(FooterPaneContentTemplateProperty, value); }
        public object AuxiliaryPaneContent { get => GetValue(AuxiliaryPaneContentProperty); set => SetValue(AuxiliaryPaneContentProperty, value); }
        public DataTemplate AuxiliaryPaneContentTemplate { get => (DataTemplate)GetValue(AuxiliaryPaneContentTemplateProperty); set => SetValue(AuxiliaryPaneContentTemplateProperty, value); }
        public Visibility MenuHeaderVisibility { get => (Visibility)GetValue(MenuHeaderVisibilityProperty); set => SetValue(MenuHeaderVisibilityProperty, value); }

        protected override bool IsItemItsOwnContainerOverride(object item)
            => item is RibbonApplicationMenuItem || item is RibbonApplicationSplitMenuItem || item is RibbonSeparator || item is RibbonGallery;

        protected override DependencyObject GetContainerForItemOverride() => new RibbonApplicationMenuItem();

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonApplicationMenuAutomationPeer(this);
    }

    public class RibbonQuickAccessToolBar : ItemsControl
    {
        private static readonly DependencyPropertyKey HasOverflowItemsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasOverflowItems), typeof(bool), typeof(RibbonQuickAccessToolBar), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty HasOverflowItemsProperty = HasOverflowItemsPropertyKey.DependencyProperty;

        public static readonly DependencyProperty IsOverflowOpenProperty =
            DependencyProperty.Register(nameof(IsOverflowOpen), typeof(bool), typeof(RibbonQuickAccessToolBar), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty CustomizeMenuButtonProperty =
            DependencyProperty.Register(nameof(CustomizeMenuButton), typeof(RibbonMenuButton), typeof(RibbonQuickAccessToolBar), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonQuickAccessToolBar));

        public bool HasOverflowItems { get => (bool)GetValue(HasOverflowItemsProperty); internal set => SetValue(HasOverflowItemsPropertyKey, value); }
        public bool IsOverflowOpen { get => (bool)GetValue(IsOverflowOpenProperty); set => SetValue(IsOverflowOpenProperty, value); }
        public RibbonMenuButton CustomizeMenuButton { get => (RibbonMenuButton)GetValue(CustomizeMenuButtonProperty); set => SetValue(CustomizeMenuButtonProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            RibbonControlService.SetIsInQuickAccessToolBar(element, true);
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);
            RibbonControlService.SetIsInQuickAccessToolBar(element, false);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (VisualChildrenCount > 0) return base.MeasureOverride(constraint);
            double w = 0.0;
            double h = 24.0;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is UIElement child)
                {
                    child.Measure(constraint);
                    w += child.DesiredSize.Width + 2.0;
                    h = Math.Max(h, child.DesiredSize.Height);
                }
            }
            return new Size(w, h);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonQuickAccessToolBarAutomationPeer(this);
    }

    public class RibbonContextMenu : ContextMenu
    {
        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonContextMenu));
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);

        protected override bool IsItemItsOwnContainerOverride(object item)
            => item is RibbonMenuItem || item is RibbonSplitMenuItem || item is RibbonSeparator;

        protected override DependencyObject GetContainerForItemOverride() => new RibbonMenuItem();
    }
}
