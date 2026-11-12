// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;

namespace System.Windows.Automation.Peers
{
    public class RibbonAutomationPeer : SelectorAutomationPeer
    {
        public RibbonAutomationPeer(Ribbon owner) : base(owner) { }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
            => item is RibbonTab tab ? new RibbonTabDataAutomationPeer(tab, this) : new RibbonTabDataAutomationPeer(item, this);

        protected override string GetClassNameCore() => nameof(Ribbon);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ExpandCollapse)
                return new RibbonExpandCollapseProvider((Ribbon)Owner);
            return base.GetPattern(patternInterface);
        }

        private sealed class RibbonExpandCollapseProvider : IExpandCollapseProvider
        {
            private readonly Ribbon _ribbon;
            public RibbonExpandCollapseProvider(Ribbon ribbon) => _ribbon = ribbon;
            public ExpandCollapseState ExpandCollapseState => _ribbon.IsMinimized ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded;
            public void Expand() => _ribbon.IsMinimized = false;
            public void Collapse() => _ribbon.IsMinimized = true;
        }
    }

    public class RibbonWindowAutomationPeer : WindowAutomationPeer
    {
        public RibbonWindowAutomationPeer(RibbonWindow owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonWindow);
    }

    public class RibbonTabAutomationPeer : FrameworkElementAutomationPeer
    {
        public RibbonTabAutomationPeer(RibbonTab owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonTab);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;
    }

    public class RibbonTabDataAutomationPeer : SelectorItemAutomationPeer
    {
        public RibbonTabDataAutomationPeer(object item, RibbonAutomationPeer owner) : base(item, owner) { }
        protected override string GetClassNameCore() => nameof(RibbonTab);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;
    }

    public class RibbonTabHeaderAutomationPeer : FrameworkElementAutomationPeer
    {
        public RibbonTabHeaderAutomationPeer(RibbonTabHeader owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonTabHeader);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.HeaderItem;
    }

    public class RibbonTabHeaderItemsControlAutomationPeer : ItemsControlAutomationPeer
    {
        public RibbonTabHeaderItemsControlAutomationPeer(RibbonTabHeaderItemsControl owner) : base(owner) { }
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new RibbonTabDataAutomationPeer(item, null);
        protected override string GetClassNameCore() => nameof(RibbonTabHeaderItemsControl);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Header;
    }

    public class RibbonContextualTabGroupAutomationPeer : FrameworkElementAutomationPeer
    {
        public RibbonContextualTabGroupAutomationPeer(RibbonContextualTabGroup owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonContextualTabGroup);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Header;
    }

    public class RibbonContextualTabGroupItemsControlAutomationPeer : ItemsControlAutomationPeer
    {
        public RibbonContextualTabGroupItemsControlAutomationPeer(RibbonContextualTabGroupItemsControl owner) : base(owner) { }
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new RibbonTabDataAutomationPeer(item, null);
        protected override string GetClassNameCore() => nameof(RibbonContextualTabGroupItemsControl);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Header;
    }

    public class RibbonGroupAutomationPeer : ItemsControlAutomationPeer
    {
        public RibbonGroupAutomationPeer(RibbonGroup owner) : base(owner) { }
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new RibbonControlDataAutomationPeer(item, this);
        protected override string GetClassNameCore() => nameof(RibbonGroup);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }

    public class RibbonControlAutomationPeer : FrameworkElementAutomationPeer
    {
        public RibbonControlAutomationPeer(RibbonControl owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonControl);
    }

    public class RibbonControlDataAutomationPeer : ItemAutomationPeer
    {
        public RibbonControlDataAutomationPeer(object item, ItemsControlAutomationPeer parent) : base(item, parent) { }
        protected override string GetClassNameCore() => nameof(RibbonControl);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    }

    public class RibbonControlGroupAutomationPeer : ItemsControlAutomationPeer
    {
        public RibbonControlGroupAutomationPeer(RibbonControlGroup owner) : base(owner) { }
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new RibbonControlDataAutomationPeer(item, this);
        protected override string GetClassNameCore() => nameof(RibbonControlGroup);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }

    public class RibbonButtonAutomationPeer : ButtonAutomationPeer
    {
        public RibbonButtonAutomationPeer(RibbonButton owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonButton);
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? ((RibbonButton)Owner).Label ?? string.Empty : name;
        }
    }

    public class RibbonToggleButtonAutomationPeer : ToggleButtonAutomationPeer
    {
        public RibbonToggleButtonAutomationPeer(RibbonToggleButton owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonToggleButton);
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? ((RibbonToggleButton)Owner).Label ?? string.Empty : name;
        }
    }

    public class RibbonRadioButtonAutomationPeer : RadioButtonAutomationPeer
    {
        public RibbonRadioButtonAutomationPeer(RibbonRadioButton owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonRadioButton);
    }

    public class RibbonCheckBoxAutomationPeer : CheckBoxAutomationPeer
    {
        public RibbonCheckBoxAutomationPeer(RibbonCheckBox owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonCheckBox);
    }

    public class RibbonMenuButtonAutomationPeer : ItemsControlAutomationPeer, IExpandCollapseProvider
    {
        public RibbonMenuButtonAutomationPeer(RibbonMenuButton owner) : base(owner) { }
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new RibbonControlDataAutomationPeer(item, this);
        protected override string GetClassNameCore() => nameof(RibbonMenuButton);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Menu;

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ExpandCollapse)
                return this;
            return base.GetPattern(patternInterface);
        }

        public ExpandCollapseState ExpandCollapseState =>
            ((RibbonMenuButton)Owner).IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        public void Expand() => ((RibbonMenuButton)Owner).IsDropDownOpen = true;
        public void Collapse() => ((RibbonMenuButton)Owner).IsDropDownOpen = false;
    }

    public class RibbonSplitButtonAutomationPeer : RibbonMenuButtonAutomationPeer, IInvokeProvider, IToggleProvider
    {
        public RibbonSplitButtonAutomationPeer(RibbonSplitButton owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonSplitButton);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.SplitButton;

        public override object GetPattern(PatternInterface patternInterface)
        {
            var sb = (RibbonSplitButton)Owner;
            if (patternInterface == PatternInterface.Invoke && !sb.IsCheckable)
                return this;
            if (patternInterface == PatternInterface.Toggle && sb.IsCheckable)
                return this;
            return base.GetPattern(patternInterface);
        }

        void IInvokeProvider.Invoke() => ((RibbonSplitButton)Owner).OnAutomationInvoke();

        void IToggleProvider.Toggle()
        {
            var sb = (RibbonSplitButton)Owner;
            sb.IsChecked = !sb.IsChecked;
        }

        ToggleState IToggleProvider.ToggleState =>
            ((RibbonSplitButton)Owner).IsChecked ? ToggleState.On : ToggleState.Off;
    }

    public class RibbonMenuItemAutomationPeer : MenuItemAutomationPeer
    {
        public RibbonMenuItemAutomationPeer(RibbonMenuItem owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonMenuItem);
    }

    public class RibbonSplitMenuItemAutomationPeer : RibbonMenuItemAutomationPeer
    {
        public RibbonSplitMenuItemAutomationPeer(RibbonSplitMenuItem owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonSplitMenuItem);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.SplitButton;
    }

    public class RibbonSeparatorAutomationPeer : SeparatorAutomationPeer
    {
        public RibbonSeparatorAutomationPeer(RibbonSeparator owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonSeparator);
    }

    public class RibbonTextBoxAutomationPeer : TextBoxAutomationPeer
    {
        public RibbonTextBoxAutomationPeer(RibbonTextBox owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonTextBox);
    }

    public class RibbonComboBoxAutomationPeer : RibbonMenuButtonAutomationPeer
    {
        public RibbonComboBoxAutomationPeer(RibbonComboBox owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonComboBox);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ComboBox;
    }

    public class RibbonTwoLineTextAutomationPeer : FrameworkElementAutomationPeer
    {
        public RibbonTwoLineTextAutomationPeer(RibbonTwoLineText owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonTwoLineText);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
    }

    public class RibbonToolTipAutomationPeer : ToolTipAutomationPeer
    {
        public RibbonToolTipAutomationPeer(RibbonToolTip owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonToolTip);
    }

    public class RibbonGalleryAutomationPeer : ItemsControlAutomationPeer, ISelectionProvider
    {
        public RibbonGalleryAutomationPeer(RibbonGallery owner) : base(owner) { }
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new RibbonGalleryCategoryDataAutomationPeer(item, this);
        protected override string GetClassNameCore() => nameof(RibbonGallery);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Selection)
                return this;
            return base.GetPattern(patternInterface);
        }

        public bool CanSelectMultiple => false;
        public bool IsSelectionRequired => false;
        public IRawElementProviderSimple[] GetSelection() => System.Array.Empty<IRawElementProviderSimple>();
    }

    public class RibbonGalleryCategoryAutomationPeer : ItemsControlAutomationPeer
    {
        public RibbonGalleryCategoryAutomationPeer(RibbonGalleryCategory owner) : base(owner) { }
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new RibbonGalleryItemDataAutomationPeer(item, this);
        protected override string GetClassNameCore() => nameof(RibbonGalleryCategory);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }

    public class RibbonGalleryCategoryDataAutomationPeer : ItemAutomationPeer
    {
        public RibbonGalleryCategoryDataAutomationPeer(object item, RibbonGalleryAutomationPeer owner) : base(item, owner) { }
        protected override string GetClassNameCore() => nameof(RibbonGalleryCategory);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }

    public class RibbonGalleryItemAutomationPeer : FrameworkElementAutomationPeer, ISelectionItemProvider
    {
        public RibbonGalleryItemAutomationPeer(RibbonGalleryItem owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonGalleryItem);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.SelectionItem)
                return this;
            return base.GetPattern(patternInterface);
        }

        public bool IsSelected => ((RibbonGalleryItem)Owner).IsSelected;
        public IRawElementProviderSimple SelectionContainer => null;
        public void AddToSelection() => ((RibbonGalleryItem)Owner).IsSelected = true;
        public void RemoveFromSelection() => ((RibbonGalleryItem)Owner).IsSelected = false;
        public void Select() => ((RibbonGalleryItem)Owner).IsSelected = true;
    }

    public class RibbonGalleryItemDataAutomationPeer : ItemAutomationPeer, ISelectionItemProvider
    {
        public RibbonGalleryItemDataAutomationPeer(object item, ItemsControlAutomationPeer owner) : base(item, owner) { }
        protected override string GetClassNameCore() => nameof(RibbonGalleryItem);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.SelectionItem)
                return this;
            return base.GetPattern(patternInterface);
        }

        public bool IsSelected => Item is RibbonGalleryItem gi && gi.IsSelected;
        public IRawElementProviderSimple SelectionContainer => null;
        public void AddToSelection() { if (Item is RibbonGalleryItem gi) gi.IsSelected = true; }
        public void RemoveFromSelection() { if (Item is RibbonGalleryItem gi) gi.IsSelected = false; }
        public void Select() { if (Item is RibbonGalleryItem gi) gi.IsSelected = true; }
    }

    public class RibbonApplicationMenuAutomationPeer : RibbonMenuButtonAutomationPeer
    {
        public RibbonApplicationMenuAutomationPeer(RibbonApplicationMenu owner) : base(owner) { }
        protected override string GetClassNameCore() => nameof(RibbonApplicationMenu);
    }

    public class RibbonQuickAccessToolBarAutomationPeer : ItemsControlAutomationPeer
    {
        public RibbonQuickAccessToolBarAutomationPeer(RibbonQuickAccessToolBar owner) : base(owner) { }
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new RibbonControlDataAutomationPeer(item, this);
        protected override string GetClassNameCore() => nameof(RibbonQuickAccessToolBar);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;
    }
}
