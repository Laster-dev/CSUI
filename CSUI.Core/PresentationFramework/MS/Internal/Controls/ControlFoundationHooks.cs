// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// CSUI Foundation-to-Controls decoupling contracts (zero concrete controls in CSUI.Core foundation).

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using MS.Utility;

#nullable disable

namespace MS.Internal.Controls
{
    /// <summary>
    /// Foundation contract implemented by hierarchical tree-item containers (e.g. TreeViewItem)
    /// so KeyboardNavigation, ItemsControl, and VirtualizingPanel need no concrete control reference.
    /// </summary>
    internal interface ITreeViewItemHost
    {
        Panel ItemsHost { get; }
        FrameworkElement TryGetHeaderElement();
        void PrepareItemContainer(object item, ItemsControl parentItemsControl);
        void ClearItemContainer(object item, ItemsControl parentItemsControl);
    }

    /// <summary>
    /// Foundation contract implemented by group containers (e.g. GroupItem).
    /// </summary>
    internal interface IGroupItemHost
    {
        ItemsControl ParentItemsControl { get; }
        Panel ItemsHost { get; set; }
        bool IsEmptyGroupItem { get; }
        ItemContainerGenerator Generator { get; }
        void SetGenerator(ItemContainerGenerator generator);
        void ClearValueFromParent();
        void PrepareItemContainer(object item, ItemsControl parentItemsControl);
        void ClearItemContainer(object item, ItemsControl parentItemsControl);
    }

    /// <summary>
    /// Foundation contract implemented by scroll host controls (e.g. ScrollViewer).
    /// </summary>
    internal interface IScrollHost
    {
        FrameworkElement AsFrameworkElement { get; }
        FrameworkElement GetViewportContentHost();
        bool CanContentScroll { get; }
        double HorizontalOffset { get; }
        double VerticalOffset { get; }
        double ExtentWidth { get; }
        double ExtentHeight { get; }
        double ViewportWidth { get; }
        double ViewportHeight { get; }
        void LineUp();
        void LineDown();
        void LineLeft();
        void LineRight();
        void PageUp();
        void PageDown();
        void PageLeft();
        void PageRight();
        void ScrollToTop();
        void ScrollToBottom();
        void ScrollToLeftEnd();
        void ScrollToRightEnd();
        void ScrollToHorizontalOffset(double offset);
        void ScrollToVerticalOffset(double offset);
        void UpdateLayout();
    }

    /// <summary>
    /// Foundation contract implemented by scroll content presenters (e.g. ScrollContentPresenter).
    /// </summary>
    internal interface IScrollContentPresenterHost
    {
        AdornerLayer AdornerLayer { get; }
        void HookupScrollingComponents();
    }

    /// <summary>
    /// Foundation contract implemented by text display controls (e.g. TextBlock).
    /// </summary>
    internal interface ITextDisplayElement
    {
        bool IsContentPresenterContainer { get; set; }
        string Text { get; set; }
        bool TryAddInline(object inline);
    }

    /// <summary>
    /// Foundation contract implemented by inline text elements (e.g. Inline / AnchoredBlock).
    /// </summary>
    internal interface IInlineHost
    {
        DependencyObject Parent { get; }
        BaselineAlignment BaselineAlignment { get; }
        bool IsAnchoredBlock { get; }
    }

    internal interface ITextBoxBaseHost
    {
        bool HasNonTextParentOpenedUndoUnit { get; }
    }

    internal interface IGroupItemAutomationPeerHost
    {
        void InvalidateGroupItemPeersContainingRecentlyRealizedPeers(List<ItemAutomationPeer> recentlyRealizedPeers);
    }

    internal interface ITextSearchHost
    {
        void DoSearch(string nextChar);
        void DeleteLastCharacter();
    }

    internal interface IHwndHost
    {
    }

    /// <summary>
    /// Optional delegates registered by concrete controls once implemented on top of CSUI.Core.
    /// </summary>
    internal static class ControlFoundationHooks
    {
        internal const char AccessKeyMarker = '_';

        internal static Action<DependencyObject, DependencyObject> TreeViewItemVirtualizingPropagationHelper { get; set; }
        internal static Func<DependencyObject, HierarchicalVirtualizationHeaderDesiredSizes> GetTreeViewItemHeaderDesiredSizes { get; set; }
        internal static Func<DependencyObject, bool> GetTreeViewItemMustDisableVirtualization { get; set; }
        internal static Func<DependencyObject, DependencyObject> GetTreeViewItemItemsHost { get; set; }

        internal static Func<DependencyObject> CreateGroupItemHook { get; set; }
        internal static Func<ItemsPanelTemplate> CreateDefaultStackPanelTemplateHook { get; set; }
        internal static Func<ItemsPanelTemplate> CreateDefaultVirtualizingStackPanelTemplateHook { get; set; }
        internal static Func<Type, object, TemplateKey> CreateTemplateKeyHook { get; set; }

        internal static DependencyProperty ContextMenuPropertyHook { get; set; }
        internal static DependencyProperty ToolTipPropertyHook { get; set; }
        internal static Func<DependencyObject, object> GetToolTipHook { get; set; }
        internal static Func<DependencyObject, FrameworkElement> GetLabeledByHook { get; set; }
        internal static Func<DependencyObject, bool> IsPopupHook { get; set; }
        internal static Func<DependencyObject, UIElement> GetPopupChildHook { get; set; }
        internal static Func<DependencyObject, FrameworkElement> GetPopupPlacementTargetHook { get; set; }
        internal static Func<object, bool> IsComboBoxItemHook { get; set; }
        internal static Func<DependencyObject, object> TryGetComboBoxSelectedItemHook { get; set; }
        internal static Func<DependencyObject, object> TryGetSelectorSelectionChangerHook { get; set; }
        internal static Func<DependencyObject, bool> IsTextBoxBaseHook { get; set; }
        internal static Func<DependencyObject, DependencyProperty> GetGridViewColumnCollectionPropertyHook { get; set; }
        internal static Func<DependencyObject, object> CreateValidationErrorPlaceholderHook { get; set; }
        internal static Func<ControlTemplate> CreateDefaultErrorTemplateHook { get; set; }
        internal static Action<FrameworkElement> CoercePopupHasDropShadowHook { get; set; }
        internal static readonly UncommonField<List<FrameworkElement>> RegisteredPopupsField = new UncommonField<List<FrameworkElement>>();

        internal static DependencyProperty TextBoxTextPropertyHook { get; set; }
        internal static DependencyProperty ComboBoxTextPropertyHook { get; set; }
        internal static Func<Type, bool> IsSelectorTypeHook { get; set; }
        internal static Func<Type, bool> IsValidControlTemplateTargetTypeHook { get; set; }
        internal static DependencyProperty NavigationServicePropertyHook { get; set; }
        internal static Func<Window, bool> IsNavigationWindowHook { get; set; }
        internal static object NavigationWindowStyleKeyHook { get; set; }
        internal static Func<AutomationPeer, bool> IsGridViewItemAutomationPeerHook { get; set; }
        internal static Func<ItemsControl, ITextSearchHost> EnsureTextSearchInstanceHook { get; set; }
        internal static Func<object, bool> IsHwndHostHook { get; set; }
        internal static Func<DependencyObject, bool> IsComboBoxHook { get; set; }
        internal static Func<DependencyObject, bool> IsGridViewRowPresenterBaseHook { get; set; }

        internal static DependencyProperty BlockLineHeightProperty { get; set; }
        internal static Func<DependencyObject, bool> GetParagraphKeepTogetherHook { get; set; }
        internal static Func<DependencyObject, bool> GetParagraphKeepWithNextHook { get; set; }
        internal static Func<DependencyObject, int> GetParagraphMinWidowLinesHook { get; set; }
        internal static Func<DependencyObject, int> GetParagraphMinOrphanLinesHook { get; set; }

        internal static DependencyProperty InlineTextDecorationsProperty { get; set; }
        internal static DependencyProperty InlineBaselineAlignmentProperty { get; set; }
        internal static Func<object, bool> IsInlineObjectHook { get; set; }
        internal static Func<object, object, bool> TryAddInlineHook { get; set; }

        private static RoutedEvent s_cleanUpVirtualizedItemEvent;

        internal static RoutedEvent CleanUpVirtualizedItemEvent
        {
            get
            {
                return s_cleanUpVirtualizedItemEvent ??= EventManager.RegisterRoutedEvent(
                    "CleanUpVirtualizedItem",
                    RoutingStrategy.Bubble,
                    typeof(CleanUpVirtualizedItemEventHandler),
                    typeof(VirtualizingPanel));
            }
            set => s_cleanUpVirtualizedItemEvent = value;
        }

        internal static DependencyProperty TextDisplayProperty;
        internal static Func<FrameworkElementFactory> CreateTextDisplayFactoryHook;
        internal static Func<FrameworkElement> CreateTextDisplayHook;

        internal static DependencyProperty AccessTextDisplayProperty;
        internal static Func<FrameworkElementFactory> CreateAccessTextFactoryHook;

        internal static string RemoveAccessKeyMarker(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                int index = text.IndexOf(AccessKeyMarker);
                if (index >= 0)
                {
                    return text.Remove(index, 1);
                }
            }
            return text;
        }

        internal static void RegisterTextDisplay(
            DependencyProperty textProperty,
            Func<FrameworkElementFactory> createFactory,
            Func<FrameworkElement> createElement)
        {
            TextDisplayProperty = textProperty;
            CreateTextDisplayFactoryHook = createFactory;
            CreateTextDisplayHook = createElement;
        }

        internal static void RegisterAccessTextDisplay(
            DependencyProperty accessTextProperty,
            Func<FrameworkElementFactory> createFactory)
        {
            AccessTextDisplayProperty = accessTextProperty;
            CreateAccessTextFactoryHook = createFactory;
        }

        // Decoupling hooks so Panel, StyleHelper, ObjectRef, and KeyboardNavigation
        // do not statically root ItemsControl or ItemContainerGenerator in apps that never use them.
        internal delegate ElementViewportPosition GetElementViewportPositionDelegate(
            FrameworkElement viewport,
            UIElement element,
            FocusNavigationDirection direction,
            bool fullyVisible,
            out Rect elementRect);

        internal static DependencyProperty ItemsPanelPropertyHook { get; set; }
        internal static DependencyProperty ItemForItemContainerPropertyHook { get; set; }
        internal static DependencyProperty HeaderedContentControlHeaderPropertyHook { get; set; }
        internal static DependencyProperty HeaderedItemsControlHeaderPropertyHook { get; set; }
        internal static DependencyProperty ItemsControlItemBindingGroupPropertyHook { get; set; }
        internal static Func<DependencyObject, DependencyObject> GetItemsOwnerHook { get; set; }
        internal static Action<Panel> OnPanelIsItemsHostChangedHook { get; set; }
        internal static Func<Panel, bool> IsPanelItemsHostHook { get; set; }
        internal static Func<Panel, IPanelItemContainerGenerator> ConnectPanelGeneratorHook { get; set; }
        internal static Func<DependencyObject, DependencyObject> TryGetTreeViewItemHeaderHook { get; set; }
        internal static GetElementViewportPositionDelegate GetElementViewportPositionHook { get; set; }
    }

    internal interface IPanelItemContainerGenerator
    {
        event ItemsChangedEventHandler ItemsChanged;
        IDisposable StartAt(GeneratorPosition position, GeneratorDirection direction);
        IDisposable StartAt(GeneratorPosition position, GeneratorDirection direction, bool allowStartAtRealizedItem);
        DependencyObject GenerateNext();
        DependencyObject GenerateNext(out bool isNewlyRealized);
        void PrepareItemContainer(DependencyObject container);
        void RemoveAll();
        int IndexFromGeneratorPosition(GeneratorPosition position);
        void Verify();
    }
}
