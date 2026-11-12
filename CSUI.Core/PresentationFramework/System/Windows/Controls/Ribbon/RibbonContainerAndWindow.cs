// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Controls.Ribbon
{
    public class RibbonTabHeader : ContentControl
    {
        private static readonly DependencyPropertyKey IsRibbonTabSelectedPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsRibbonTabSelected), typeof(bool), typeof(RibbonTabHeader), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsRibbonTabSelectedProperty = IsRibbonTabSelectedPropertyKey.DependencyProperty;

        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonTabHeader));
        public static readonly DependencyProperty KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonTabHeader));

        public bool IsRibbonTabSelected
        {
            get => (bool)GetValue(IsRibbonTabSelectedProperty);
            internal set => SetValue(IsRibbonTabSelectedPropertyKey, value);
        }

        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);

        public string KeyTip
        {
            get => (string)GetValue(KeyTipProperty);
            set => SetValue(KeyTipProperty, value);
        }

        internal RibbonTab RibbonTab { get; set; }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTabHeaderAutomationPeer(this);
    }

    public class RibbonTabHeaderItemsControl : ItemsControl
    {
        protected override bool IsItemItsOwnContainerOverride(object item) => item is RibbonTabHeader;
        protected override DependencyObject GetContainerForItemOverride() => new RibbonTabHeader();
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTabHeaderItemsControlAutomationPeer(this);
    }

    public class RibbonContextualTabGroup : HeaderedItemsControl
    {
        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonContextualTabGroup));

        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonContextualTabGroupAutomationPeer(this);
    }

    public class RibbonContextualTabGroupItemsControl : ItemsControl
    {
        protected override bool IsItemItsOwnContainerOverride(object item) => item is RibbonContextualTabGroup;
        protected override DependencyObject GetContainerForItemOverride() => new RibbonContextualTabGroup();
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonContextualTabGroupItemsControlAutomationPeer(this);
    }

    public class RibbonGroup : HeaderedItemsControl
    {
        public static readonly DependencyProperty GroupSizeDefinitionsProperty =
            DependencyProperty.Register(nameof(GroupSizeDefinitions), typeof(RibbonGroupSizeDefinitionBaseCollection), typeof(RibbonGroup),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            ComboBox.IsDropDownOpenProperty.AddOwner(typeof(RibbonGroup));

        private static readonly DependencyPropertyKey IsCollapsedPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsCollapsed), typeof(bool), typeof(RibbonGroup), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsCollapsedProperty = IsCollapsedPropertyKey.DependencyProperty;

        public static readonly DependencyProperty LargeImageSourceProperty = RibbonControlService.LargeImageSourceProperty.AddOwner(typeof(RibbonGroup));
        public static readonly DependencyProperty SmallImageSourceProperty = RibbonControlService.SmallImageSourceProperty.AddOwner(typeof(RibbonGroup));
        public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty = RibbonControlService.CanAddToQuickAccessToolBarProperty.AddOwner(typeof(RibbonGroup));
        public static readonly DependencyProperty QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonGroup));
        public static readonly DependencyProperty MouseOverBorderBrushProperty = RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonGroup));
        public static readonly DependencyProperty MouseOverBackgroundProperty = RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonGroup));
        public static readonly DependencyProperty PressedBorderBrushProperty = RibbonControlService.PressedBorderBrushProperty.AddOwner(typeof(RibbonGroup));
        public static readonly DependencyProperty PressedBackgroundProperty = RibbonControlService.PressedBackgroundProperty.AddOwner(typeof(RibbonGroup));
        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonGroup));
        public static readonly DependencyProperty KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonGroup));

        public RibbonGroup()
        {
            GroupSizeDefinitions = new RibbonGroupSizeDefinitionBaseCollection();
        }

        public RibbonGroupSizeDefinitionBaseCollection GroupSizeDefinitions
        {
            get => (RibbonGroupSizeDefinitionBaseCollection)GetValue(GroupSizeDefinitionsProperty);
            set => SetValue(GroupSizeDefinitionsProperty, value);
        }

        public bool IsDropDownOpen { get => (bool)GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }
        public bool IsCollapsed { get => (bool)GetValue(IsCollapsedProperty); internal set => SetValue(IsCollapsedPropertyKey, value); }
        public ImageSource LargeImageSource { get => (ImageSource)GetValue(LargeImageSourceProperty); set => SetValue(LargeImageSourceProperty, value); }
        public ImageSource SmallImageSource { get => (ImageSource)GetValue(SmallImageSourceProperty); set => SetValue(SmallImageSourceProperty, value); }
        public bool CanAddToQuickAccessToolBar { get => (bool)GetValue(CanAddToQuickAccessToolBarProperty); set => SetValue(CanAddToQuickAccessToolBarProperty, value); }
        public object QuickAccessToolBarId { get => GetValue(QuickAccessToolBarIdProperty); set => SetValue(QuickAccessToolBarIdProperty, value); }
        public Brush MouseOverBorderBrush { get => (Brush)GetValue(MouseOverBorderBrushProperty); set => SetValue(MouseOverBorderBrushProperty, value); }
        public Brush MouseOverBackground { get => (Brush)GetValue(MouseOverBackgroundProperty); set => SetValue(MouseOverBackgroundProperty, value); }
        public Brush PressedBorderBrush { get => (Brush)GetValue(PressedBorderBrushProperty); set => SetValue(PressedBorderBrushProperty, value); }
        public Brush PressedBackground { get => (Brush)GetValue(PressedBackgroundProperty); set => SetValue(PressedBackgroundProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);
        public string KeyTip { get => (string)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

        private RibbonGroupSizeDefinitionBase _activeSizeDefinition;

        internal void ApplySizeDefinition(RibbonGroupSizeDefinitionBase def)
        {
            _activeSizeDefinition = def;
            IsCollapsed = def?.IsCollapsed == true;
            if (def is RibbonGroupSizeDefinition groupDef)
            {
                for (int i = 0; i < Items.Count && i < groupDef.ControlDefinitions.Count; i++)
                {
                    if (Items[i] is DependencyObject d)
                    {
                        RibbonControlService.SetControlSizeDefinition(d, groupDef.ControlDefinitions[i]);
                    }
                }
            }
            InvalidateMeasure();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
            => item is UIElement;

        protected override DependencyObject GetContainerForItemOverride()
            => new RibbonControl();

        protected override Size MeasureOverride(Size constraint)
        {
            if (VisualChildrenCount > 0)
            {
                return base.MeasureOverride(constraint);
            }

            if (IsCollapsed)
            {
                string headerText = Header?.ToString() ?? string.Empty;
                return new Size(Math.Max(64.0, headerText.Length * 7.0 + 20.0), 86.0);
            }

            double width = 12.0;
            double contentHeight = 0.0;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is UIElement child)
                {
                    child.Measure(new Size(double.PositiveInfinity, Math.Max(0.0, constraint.Height - 20.0)));
                    width += child.DesiredSize.Width + 4.0;
                    contentHeight = Math.Max(contentHeight, child.DesiredSize.Height);
                }
            }

            string hdr = Header?.ToString() ?? string.Empty;
            double minHeaderWidth = hdr.Length * 6.5 + 16.0;
            return new Size(Math.Max(width, minHeaderWidth), Math.Max(86.0, contentHeight + 22.0));
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            if (VisualChildrenCount > 0)
            {
                return base.ArrangeOverride(arrangeBounds);
            }

            if (!IsCollapsed)
            {
                double x = 6.0;
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i] is UIElement child)
                    {
                        double w = child.DesiredSize.Width;
                        double h = Math.Min(child.DesiredSize.Height, Math.Max(0.0, arrangeBounds.Height - 20.0));
                        child.Arrange(new Rect(x, 4.0, w, h));
                        x += w + 4.0;
                    }
                }
            }
            return arrangeBounds;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (VisualChildrenCount > 0 || drawingContext == null || RenderSize.Width <= 0 || RenderSize.Height <= 0)
            {
                base.OnRender(drawingContext);
                return;
            }

            drawingContext.DrawLine(
                RibbonVisualDefaults.SeparatorPen,
                new Point(RenderSize.Width - 0.5, 4.0),
                new Point(RenderSize.Width - 0.5, Math.Max(4.0, RenderSize.Height - 4.0)));

            string header = Header?.ToString();
            if (!string.IsNullOrEmpty(header))
            {
                var typeface = new Typeface(FontFamily ?? SystemFonts.MessageFontFamily, FontStyle, FontWeight, FontStretch);
                var ft = new FormattedText(
                    header,
                    CultureInfo.CurrentUICulture,
                    FlowDirection,
                    typeface,
                    11.0,
                    RibbonVisualDefaults.SubText,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                double x = Math.Max(4.0, (RenderSize.Width - ft.Width) * 0.5);
                double y = Math.Max(0.0, RenderSize.Height - ft.Height - 3.0);
                drawingContext.DrawText(ft, new Point(x, y));
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonGroupAutomationPeer(this);
    }

    public class RibbonTab : HeaderedItemsControl
    {
        public static readonly RoutedEvent SelectedEvent = Selector.SelectedEvent.AddOwner(typeof(RibbonTab));
        public static readonly RoutedEvent UnselectedEvent = Selector.UnselectedEvent.AddOwner(typeof(RibbonTab));

        public static readonly DependencyProperty IsSelectedProperty =
            Selector.IsSelectedProperty.AddOwner(typeof(RibbonTab),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsSelectedChanged));

        public static readonly DependencyProperty ContextualTabGroupHeaderProperty =
            DependencyProperty.Register(nameof(ContextualTabGroupHeader), typeof(object), typeof(RibbonTab), new FrameworkPropertyMetadata(null));

        private static readonly DependencyPropertyKey ContextualTabGroupPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ContextualTabGroup), typeof(RibbonContextualTabGroup), typeof(RibbonTab), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ContextualTabGroupProperty = ContextualTabGroupPropertyKey.DependencyProperty;

        public static readonly DependencyProperty GroupSizeReductionOrderProperty =
            DependencyProperty.Register(nameof(GroupSizeReductionOrder), typeof(StringCollection), typeof(RibbonTab), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonTab));
        public static readonly DependencyProperty KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonTab));

        public RibbonTab()
        {
            GroupSizeReductionOrder = new StringCollection();
        }

        public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
        public object ContextualTabGroupHeader { get => GetValue(ContextualTabGroupHeaderProperty); set => SetValue(ContextualTabGroupHeaderProperty, value); }
        public RibbonContextualTabGroup ContextualTabGroup { get => (RibbonContextualTabGroup)GetValue(ContextualTabGroupProperty); internal set => SetValue(ContextualTabGroupPropertyKey, value); }
        public StringCollection GroupSizeReductionOrder { get => (StringCollection)GetValue(GroupSizeReductionOrderProperty); set => SetValue(GroupSizeReductionOrderProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);
        public string KeyTip { get => (string)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }
        internal RibbonTabHeader RibbonTabHeader { get; set; }

        public event RoutedEventHandler Selected { add => AddHandler(SelectedEvent, value); remove => RemoveHandler(SelectedEvent, value); }
        public event RoutedEventHandler Unselected { add => AddHandler(UnselectedEvent, value); remove => RemoveHandler(UnselectedEvent, value); }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tab = (RibbonTab)d;
            bool isSelected = (bool)e.NewValue;
            if (tab.RibbonTabHeader != null)
            {
                tab.RibbonTabHeader.IsRibbonTabSelected = isSelected;
            }
            tab.RaiseEvent(new RoutedEventArgs(isSelected ? SelectedEvent : UnselectedEvent, tab));
            tab.Ribbon?.OnTabSelectionChanged(tab, isSelected);
        }

        protected override bool IsItemItsOwnContainerOverride(object item) => item is RibbonGroup;
        protected override DependencyObject GetContainerForItemOverride() => new RibbonGroup();

        protected override Size MeasureOverride(Size constraint)
        {
            if (VisualChildrenCount > 0)
            {
                return base.MeasureOverride(constraint);
            }

            double width = 0.0;
            double height = 88.0;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is UIElement group)
                {
                    group.Measure(new Size(double.PositiveInfinity, constraint.Height));
                    width += group.DesiredSize.Width;
                    height = Math.Max(height, group.DesiredSize.Height);
                }
            }
            return new Size(Math.Min(width, constraint.Width), Math.Min(height, constraint.Height));
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            if (VisualChildrenCount > 0)
            {
                return base.ArrangeOverride(arrangeBounds);
            }

            double x = 0.0;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is UIElement group)
                {
                    double w = group.DesiredSize.Width;
                    group.Arrange(new Rect(x, 0.0, w, arrangeBounds.Height));
                    x += w;
                }
            }
            return arrangeBounds;
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTabAutomationPeer(this);
    }

    public class StringCollection : ObservableCollection<string>
    {
    }

    public class Ribbon : Selector
    {
        public static readonly RoutedEvent CollapsedEvent =
            EventManager.RegisterRoutedEvent(nameof(Collapsed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Ribbon));

        public static readonly RoutedEvent ExpandedEvent =
            EventManager.RegisterRoutedEvent(nameof(Expanded), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Ribbon));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(object), typeof(Ribbon), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty IsMinimizedProperty =
            DependencyProperty.Register(nameof(IsMinimized), typeof(bool), typeof(Ribbon),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            ComboBox.IsDropDownOpenProperty.AddOwner(typeof(Ribbon));

        private static readonly DependencyPropertyKey IsCollapsedPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsCollapsed), typeof(bool), typeof(Ribbon),
                new FrameworkPropertyMetadata(false, OnIsCollapsedChanged));

        public static readonly DependencyProperty IsCollapsedProperty = IsCollapsedPropertyKey.DependencyProperty;

        public static readonly DependencyProperty QuickAccessToolBarProperty =
            DependencyProperty.Register(nameof(QuickAccessToolBar), typeof(RibbonQuickAccessToolBar), typeof(Ribbon),
                new FrameworkPropertyMetadata(null, OnQuickAccessToolBarChanged));

        public static readonly DependencyProperty ApplicationMenuProperty =
            DependencyProperty.Register(nameof(ApplicationMenu), typeof(RibbonApplicationMenu), typeof(Ribbon),
                new FrameworkPropertyMetadata(null, OnApplicationMenuChanged));

        private static readonly DependencyPropertyKey ContextualTabGroupsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ContextualTabGroups), typeof(ObservableCollection<RibbonContextualTabGroup>), typeof(Ribbon), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ContextualTabGroupsProperty = ContextualTabGroupsPropertyKey.DependencyProperty;

        public static readonly DependencyProperty HelpPaneContentProperty =
            DependencyProperty.Register(nameof(HelpPaneContent), typeof(object), typeof(Ribbon), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty HelpPaneContentTemplateProperty =
            DependencyProperty.Register(nameof(HelpPaneContentTemplate), typeof(DataTemplate), typeof(Ribbon), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ShowQuickAccessToolBarOnTopProperty =
            DependencyProperty.Register(nameof(ShowQuickAccessToolBarOnTop), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty MouseOverBorderBrushProperty = RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(Ribbon));
        public static readonly DependencyProperty MouseOverBackgroundProperty = RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(Ribbon));
        public static readonly DependencyProperty PressedBorderBrushProperty = RibbonControlService.PressedBorderBrushProperty.AddOwner(typeof(Ribbon));
        public static readonly DependencyProperty PressedBackgroundProperty = RibbonControlService.PressedBackgroundProperty.AddOwner(typeof(Ribbon));
        public static readonly DependencyProperty CheckedBorderBrushProperty = RibbonControlService.CheckedBorderBrushProperty.AddOwner(typeof(Ribbon));
        public static readonly DependencyProperty CheckedBackgroundProperty = RibbonControlService.CheckedBackgroundProperty.AddOwner(typeof(Ribbon));
        public static readonly DependencyProperty FocusedBorderBrushProperty = RibbonControlService.FocusedBorderBrushProperty.AddOwner(typeof(Ribbon));
        public static readonly DependencyProperty FocusedBackgroundProperty = RibbonControlService.FocusedBackgroundProperty.AddOwner(typeof(Ribbon));

        private readonly ObservableCollection<RibbonContextualTabGroup> _contextualTabGroups = new ObservableCollection<RibbonContextualTabGroup>();

        public Ribbon()
        {
            SetValue(ContextualTabGroupsPropertyKey, _contextualTabGroups);
            RibbonControlService.SetRibbon(this, this);
            Background = RibbonVisualDefaults.ChromeBackground;
            BorderBrush = RibbonVisualDefaults.ChromeBorder;

            CommandBindings.Add(new CommandBinding(RibbonCommands.MinimizeRibbonCommand, (_, _) => IsMinimized = true, (_, e) => e.CanExecute = !IsMinimized));
            CommandBindings.Add(new CommandBinding(RibbonCommands.MaximizeRibbonCommand, (_, _) => IsMinimized = false, (_, e) => e.CanExecute = IsMinimized));
            CommandBindings.Add(new CommandBinding(RibbonCommands.ShowQuickAccessToolBarAboveRibbonCommand, (_, _) => ShowQuickAccessToolBarOnTop = true));
            CommandBindings.Add(new CommandBinding(RibbonCommands.ShowQuickAccessToolBarBelowRibbonCommand, (_, _) => ShowQuickAccessToolBarOnTop = false));
        }

        public object Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
        public bool IsMinimized { get => (bool)GetValue(IsMinimizedProperty); set => SetValue(IsMinimizedProperty, value); }
        public bool IsDropDownOpen { get => (bool)GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }
        public bool IsCollapsed { get => (bool)GetValue(IsCollapsedProperty); internal set => SetValue(IsCollapsedPropertyKey, value); }
        public RibbonQuickAccessToolBar QuickAccessToolBar { get => (RibbonQuickAccessToolBar)GetValue(QuickAccessToolBarProperty); set => SetValue(QuickAccessToolBarProperty, value); }
        public RibbonApplicationMenu ApplicationMenu { get => (RibbonApplicationMenu)GetValue(ApplicationMenuProperty); set => SetValue(ApplicationMenuProperty, value); }
        public ObservableCollection<RibbonContextualTabGroup> ContextualTabGroups => _contextualTabGroups;
        public object HelpPaneContent { get => GetValue(HelpPaneContentProperty); set => SetValue(HelpPaneContentProperty, value); }
        public DataTemplate HelpPaneContentTemplate { get => (DataTemplate)GetValue(HelpPaneContentTemplateProperty); set => SetValue(HelpPaneContentTemplateProperty, value); }
        public bool ShowQuickAccessToolBarOnTop { get => (bool)GetValue(ShowQuickAccessToolBarOnTopProperty); set => SetValue(ShowQuickAccessToolBarOnTopProperty, value); }
        public Brush MouseOverBorderBrush { get => (Brush)GetValue(MouseOverBorderBrushProperty); set => SetValue(MouseOverBorderBrushProperty, value); }
        public Brush MouseOverBackground { get => (Brush)GetValue(MouseOverBackgroundProperty); set => SetValue(MouseOverBackgroundProperty, value); }
        public Brush PressedBorderBrush { get => (Brush)GetValue(PressedBorderBrushProperty); set => SetValue(PressedBorderBrushProperty, value); }
        public Brush PressedBackground { get => (Brush)GetValue(PressedBackgroundProperty); set => SetValue(PressedBackgroundProperty, value); }
        public Brush CheckedBorderBrush { get => (Brush)GetValue(CheckedBorderBrushProperty); set => SetValue(CheckedBorderBrushProperty, value); }
        public Brush CheckedBackground { get => (Brush)GetValue(CheckedBackgroundProperty); set => SetValue(CheckedBackgroundProperty, value); }
        public Brush FocusedBorderBrush { get => (Brush)GetValue(FocusedBorderBrushProperty); set => SetValue(FocusedBorderBrushProperty, value); }
        public Brush FocusedBackground { get => (Brush)GetValue(FocusedBackgroundProperty); set => SetValue(FocusedBackgroundProperty, value); }

        public event RoutedEventHandler Collapsed { add => AddHandler(CollapsedEvent, value); remove => RemoveHandler(CollapsedEvent, value); }
        public event RoutedEventHandler Expanded { add => AddHandler(ExpandedEvent, value); remove => RemoveHandler(ExpandedEvent, value); }

        private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ribbon = (Ribbon)d;
            ribbon.RaiseEvent(new RoutedEventArgs((bool)e.NewValue ? CollapsedEvent : ExpandedEvent, ribbon));
        }

        private static void OnQuickAccessToolBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is RibbonQuickAccessToolBar oldQat)
            {
                RibbonControlService.SetRibbon(oldQat, null);
            }
            if (e.NewValue is RibbonQuickAccessToolBar newQat)
            {
                RibbonControlService.SetRibbon(newQat, (Ribbon)d);
            }
        }

        private static void OnApplicationMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is RibbonApplicationMenu oldMenu)
            {
                RibbonControlService.SetRibbon(oldMenu, null);
            }
            if (e.NewValue is RibbonApplicationMenu newMenu)
            {
                RibbonControlService.SetRibbon(newMenu, (Ribbon)d);
            }
        }

        internal void OnTabSelectionChanged(RibbonTab tab, bool isSelected)
        {
            if (isSelected)
            {
                SelectedItem = tab;
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if (SelectedIndex < 0 && Items.Count > 0)
            {
                SelectedIndex = 0;
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is RibbonTab tab)
                {
                    bool shouldSelect = object.Equals(tab, SelectedItem);
                    if (tab.IsSelected != shouldSelect)
                    {
                        tab.IsSelected = shouldSelect;
                    }
                }
            }
            InvalidateMeasure();
            InvalidateVisual();
        }

        protected override bool IsItemItsOwnContainerOverride(object item) => item is RibbonTab;
        protected override DependencyObject GetContainerForItemOverride() => new RibbonTab();

        private const double TabHeaderStripHeight = 28.0;

        protected override Size MeasureOverride(Size constraint)
        {
            if (VisualChildrenCount > 0)
            {
                return base.MeasureOverride(constraint);
            }

            if (IsCollapsed)
            {
                return Size.Empty;
            }

            double maxGroupHeight = 0.0;
            double maxTabWidth = 0.0;
            if (!IsMinimized && SelectedItem is RibbonTab activeTab)
            {
                activeTab.Measure(new Size(constraint.Width, Math.Max(0.0, constraint.Height - TabHeaderStripHeight)));
                maxGroupHeight = activeTab.DesiredSize.Height;
                maxTabWidth = activeTab.DesiredSize.Width;
            }

            double desiredWidth = double.IsInfinity(constraint.Width) ? Math.Max(320.0, maxTabWidth) : constraint.Width;
            double desiredHeight = TabHeaderStripHeight + (IsMinimized ? 0.0 : Math.Max(88.0, maxGroupHeight));
            return new Size(desiredWidth, Math.Min(desiredHeight, constraint.Height));
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            if (VisualChildrenCount > 0)
            {
                return base.ArrangeOverride(arrangeBounds);
            }

            if (!IsCollapsed && !IsMinimized && SelectedItem is RibbonTab activeTab)
            {
                activeTab.Arrange(new Rect(0.0, TabHeaderStripHeight, arrangeBounds.Width, Math.Max(0.0, arrangeBounds.Height - TabHeaderStripHeight)));
            }
            return arrangeBounds;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (VisualChildrenCount > 0 || drawingContext == null || RenderSize.Width <= 0 || RenderSize.Height <= 0 || IsCollapsed)
            {
                base.OnRender(drawingContext);
                return;
            }

            drawingContext.DrawRectangle(Background ?? RibbonVisualDefaults.ChromeBackground, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            drawingContext.DrawLine(RibbonVisualDefaults.SeparatorPen, new Point(0, TabHeaderStripHeight - 0.5), new Point(RenderSize.Width, TabHeaderStripHeight - 0.5));
            drawingContext.DrawLine(RibbonVisualDefaults.SeparatorPen, new Point(0, RenderSize.Height - 0.5), new Point(RenderSize.Width, RenderSize.Height - 0.5));

            double x = 8.0;
            if (ApplicationMenu != null)
            {
                var appRect = new Rect(x, 3.0, 48.0, TabHeaderStripHeight - 5.0);
                drawingContext.DrawRoundedRectangle(RibbonVisualDefaults.AccentBlue, null, appRect, 2.0, 2.0);
                string appLabel = string.IsNullOrEmpty(ApplicationMenu.Label) ? "File" : ApplicationMenu.Label;
                var appFt = CreateHeaderText(appLabel, Brushes.White);
                drawingContext.DrawText(appFt, new Point(appRect.X + (appRect.Width - appFt.Width) * 0.5, appRect.Y + (appRect.Height - appFt.Height) * 0.5));
                x += 54.0;
            }

            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is RibbonTab tab)
                {
                    string headerText = tab.Header?.ToString() ?? $"Tab {i + 1}";
                    bool selected = object.Equals(tab, SelectedItem) || tab.IsSelected;
                    var ft = CreateHeaderText(headerText, selected ? RibbonVisualDefaults.AccentBlue : (Foreground ?? Brushes.Black));
                    double w = Math.Max(48.0, ft.Width + 20.0);
                    var tabRect = new Rect(x, 3.0, w, TabHeaderStripHeight - 3.0);
                    if (selected)
                    {
                        drawingContext.DrawRectangle(Brushes.White, RibbonVisualDefaults.BorderPen, tabRect);
                    }
                    drawingContext.DrawText(ft, new Point(tabRect.X + 10.0, tabRect.Y + (tabRect.Height - ft.Height) * 0.5));
                    x += w + 2.0;
                }
            }
        }

        private FormattedText CreateHeaderText(string text, Brush brush)
        {
            var typeface = new Typeface(FontFamily ?? SystemFonts.MessageFontFamily, FontStyle, FontWeight, FontStretch);
            return new FormattedText(
                text ?? string.Empty,
                CultureInfo.CurrentUICulture,
                FlowDirection,
                typeface,
                12.0,
                brush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonAutomationPeer(this);
    }

    public class RibbonWindow : Window
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonWindowAutomationPeer(this);
    }
}
