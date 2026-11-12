// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;

namespace MS.Internal.UIAutomationTypes
{
    internal static class SR
    {
        internal const string ElementNotAvailable = "The target element corresponds to UI that is no longer available (for example, the parent window has closed).";
        internal const string ElementNotEnabled = "The operation is not allowed on a nonenabled element.";
        internal const string LocalizedControlTypeButton = "button";
        internal const string LocalizedControlTypeCalendar = "calendar";
        internal const string LocalizedControlTypeCheckBox = "check box";
        internal const string LocalizedControlTypeComboBox = "combo box";
        internal const string LocalizedControlTypeCustom = "custom";
        internal const string LocalizedControlTypeDataGrid = "datagrid";
        internal const string LocalizedControlTypeDataItem = "dataitem";
        internal const string LocalizedControlTypeDocument = "document";
        internal const string LocalizedControlTypeEdit = "edit";
        internal const string LocalizedControlTypeGroup = "group";
        internal const string LocalizedControlTypeHeader = "header";
        internal const string LocalizedControlTypeHeaderItem = "header item";
        internal const string LocalizedControlTypeHyperlink = "hyperlink";
        internal const string LocalizedControlTypeImage = "image";
        internal const string LocalizedControlTypeListItem = "list item";
        internal const string LocalizedControlTypeListView = "list view";
        internal const string LocalizedControlTypeMenu = "menu";
        internal const string LocalizedControlTypeMenuBar = "menu bar";
        internal const string LocalizedControlTypeMenuItem = "menu item";
        internal const string LocalizedControlTypePane = "pane";
        internal const string LocalizedControlTypeProgressBar = "progress bar";
        internal const string LocalizedControlTypeRadioButton = "radio button";
        internal const string LocalizedControlTypeScrollBar = "scroll bar";
        internal const string LocalizedControlTypeSeparator = "separator";
        internal const string LocalizedControlTypeSlider = "slider";
        internal const string LocalizedControlTypeSpinner = "spinner";
        internal const string LocalizedControlTypeSplitButton = "split button";
        internal const string LocalizedControlTypeStatusBar = "status bar";
        internal const string LocalizedControlTypeTab = "tab";
        internal const string LocalizedControlTypeTabItem = "tab item";
        internal const string LocalizedControlTypeTable = "table";
        internal const string LocalizedControlTypeText = "text";
        internal const string LocalizedControlTypeThumb = "thumb";
        internal const string LocalizedControlTypeTitleBar = "title bar";
        internal const string LocalizedControlTypeToolBar = "tool bar";
        internal const string LocalizedControlTypeToolTip = "tool tip";
        internal const string LocalizedControlTypeTreeView = "tree view";
        internal const string LocalizedControlTypeTreeViewItem = "tree view item";
        internal const string LocalizedControlTypeWindow = "window";

        internal static string GetResourceString(string resourceKey, string defaultString = null)
        {
            return resourceKey switch
            {
                nameof(ElementNotAvailable) => ElementNotAvailable,
                nameof(ElementNotEnabled) => ElementNotEnabled,
                nameof(LocalizedControlTypeButton) => LocalizedControlTypeButton,
                nameof(LocalizedControlTypeCalendar) => LocalizedControlTypeCalendar,
                nameof(LocalizedControlTypeCheckBox) => LocalizedControlTypeCheckBox,
                nameof(LocalizedControlTypeComboBox) => LocalizedControlTypeComboBox,
                nameof(LocalizedControlTypeCustom) => LocalizedControlTypeCustom,
                nameof(LocalizedControlTypeDataGrid) => LocalizedControlTypeDataGrid,
                nameof(LocalizedControlTypeDataItem) => LocalizedControlTypeDataItem,
                nameof(LocalizedControlTypeDocument) => LocalizedControlTypeDocument,
                nameof(LocalizedControlTypeEdit) => LocalizedControlTypeEdit,
                nameof(LocalizedControlTypeGroup) => LocalizedControlTypeGroup,
                nameof(LocalizedControlTypeHeader) => LocalizedControlTypeHeader,
                nameof(LocalizedControlTypeHeaderItem) => LocalizedControlTypeHeaderItem,
                nameof(LocalizedControlTypeHyperlink) => LocalizedControlTypeHyperlink,
                nameof(LocalizedControlTypeImage) => LocalizedControlTypeImage,
                nameof(LocalizedControlTypeListItem) => LocalizedControlTypeListItem,
                nameof(LocalizedControlTypeListView) => LocalizedControlTypeListView,
                nameof(LocalizedControlTypeMenu) => LocalizedControlTypeMenu,
                nameof(LocalizedControlTypeMenuBar) => LocalizedControlTypeMenuBar,
                nameof(LocalizedControlTypeMenuItem) => LocalizedControlTypeMenuItem,
                nameof(LocalizedControlTypePane) => LocalizedControlTypePane,
                nameof(LocalizedControlTypeProgressBar) => LocalizedControlTypeProgressBar,
                nameof(LocalizedControlTypeRadioButton) => LocalizedControlTypeRadioButton,
                nameof(LocalizedControlTypeScrollBar) => LocalizedControlTypeScrollBar,
                nameof(LocalizedControlTypeSeparator) => LocalizedControlTypeSeparator,
                nameof(LocalizedControlTypeSlider) => LocalizedControlTypeSlider,
                nameof(LocalizedControlTypeSpinner) => LocalizedControlTypeSpinner,
                nameof(LocalizedControlTypeSplitButton) => LocalizedControlTypeSplitButton,
                nameof(LocalizedControlTypeStatusBar) => LocalizedControlTypeStatusBar,
                nameof(LocalizedControlTypeTab) => LocalizedControlTypeTab,
                nameof(LocalizedControlTypeTabItem) => LocalizedControlTypeTabItem,
                nameof(LocalizedControlTypeTable) => LocalizedControlTypeTable,
                nameof(LocalizedControlTypeText) => LocalizedControlTypeText,
                nameof(LocalizedControlTypeThumb) => LocalizedControlTypeThumb,
                nameof(LocalizedControlTypeTitleBar) => LocalizedControlTypeTitleBar,
                nameof(LocalizedControlTypeToolBar) => LocalizedControlTypeToolBar,
                nameof(LocalizedControlTypeToolTip) => LocalizedControlTypeToolTip,
                nameof(LocalizedControlTypeTreeView) => LocalizedControlTypeTreeView,
                nameof(LocalizedControlTypeTreeViewItem) => LocalizedControlTypeTreeViewItem,
                nameof(LocalizedControlTypeWindow) => LocalizedControlTypeWindow,
                _ => defaultString ?? resourceKey ?? string.Empty,
            };
        }

        internal static string Format(string resourceFormat, params object[] args)
        {
            return args == null ? resourceFormat : string.Format(CultureInfo.CurrentCulture, resourceFormat, args);
        }
    }
}
