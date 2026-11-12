// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using MS.Internal;
using MS.Internal.Controls;
using MS.Utility;

using SR = MS.Internal.PresentationFramework.SR;

namespace System.Windows
{
    public partial class FrameworkElement
    {
        internal new DependencyObject _parent;

        internal static readonly EventPrivateKey ResourcesChangedKey = new EventPrivateKey();
        internal static readonly EventPrivateKey InheritedPropertyChangedKey = new EventPrivateKey();

        private static readonly UncommonField<DependencyObject> InheritanceContextField = new UncommonField<DependencyObject>();
        private static readonly UncommonField<DependencyObject> MentorField = new UncommonField<DependencyObject>();

        /// <summary>
        /// Gets the logical parent element of this element.
        /// </summary>
        public DependencyObject Parent => ContextVerifiedGetParent();

        internal DependencyObject ContextVerifiedGetParent()
        {
            return _parent;
        }

        internal bool ShouldLookupImplicitStyles
        {
            get { return ReadInternalFlag(InternalFlags.ShouldLookupImplicitStyles); }
            set { WriteInternalFlag(InternalFlags.ShouldLookupImplicitStyles, value); }
        }

        internal bool IsStyleSetFromGenerator
        {
            get { return ReadInternalFlag2(InternalFlags2.IsStyleSetFromGenerator); }
            set { WriteInternalFlag2(InternalFlags2.IsStyleSetFromGenerator, value); }
        }

        internal bool HasLocalStyle
        {
            get { return ReadInternalFlag(InternalFlags.HasLocalStyle); }
            set { WriteInternalFlag(InternalFlags.HasLocalStyle, value); }
        }

        internal bool HasImplicitStyleFromResources
        {
            get { return ReadInternalFlag(InternalFlags.HasImplicitStyleFromResources); }
            set { WriteInternalFlag(InternalFlags.HasImplicitStyleFromResources, value); }
        }

        internal bool HasStyleEverBeenFetched
        {
            get { return ReadInternalFlag(InternalFlags.HasStyleEverBeenFetched); }
            set { WriteInternalFlag(InternalFlags.HasStyleEverBeenFetched, value); }
        }

        internal bool HasThemeStyleEverBeenFetched
        {
            get { return ReadInternalFlag(InternalFlags.HasThemeStyleEverBeenFetched); }
            set { WriteInternalFlag(InternalFlags.HasThemeStyleEverBeenFetched, value); }
        }

        internal bool HasStyleChanged
        {
            get { return ReadInternalFlag2(InternalFlags2.HasStyleChanged); }
            set { WriteInternalFlag2(InternalFlags2.HasStyleChanged, value); }
        }

        internal bool HasStyleInvalidated
        {
            get { return ReadInternalFlag2(InternalFlags2.HasStyleInvalidated); }
            set { WriteInternalFlag2(InternalFlags2.HasStyleInvalidated, value); }
        }

        internal bool HasTemplateChanged
        {
            get { return ReadInternalFlag2(InternalFlags2.HasTemplateChanged); }
            set { WriteInternalFlag2(InternalFlags2.HasTemplateChanged, value); }
        }

        internal bool HasTemplateGeneratedSubTree
        {
            get { return ReadInternalFlag(InternalFlags.HasTemplateGeneratedSubTree); }
            set { WriteInternalFlag(InternalFlags.HasTemplateGeneratedSubTree, value); }
        }

        internal bool StoresParentTemplateValues
        {
            get { return ReadInternalFlag(InternalFlags.StoresParentTemplateValues); }
            set { WriteInternalFlag(InternalFlags.StoresParentTemplateValues, value); }
        }

        internal bool PotentiallyHasMentees
        {
            get { return ReadInternalFlag(InternalFlags.PotentiallyHasMentees); }
            set { WriteInternalFlag(InternalFlags.PotentiallyHasMentees, value); }
        }

        internal bool AncestorChangeInProgress
        {
            get { return ReadInternalFlag(InternalFlags.AncestorChangeInProgress); }
            set { WriteInternalFlag(InternalFlags.AncestorChangeInProgress, value); }
        }

        internal bool HasNumberSubstitutionChanged
        {
            get { return ReadInternalFlag(InternalFlags.HasNumberSubstitutionChanged); }
            set { WriteInternalFlag(InternalFlags.HasNumberSubstitutionChanged, value); }
        }

        internal bool IsStyleUpdateInProgress
        {
            get { return ReadInternalFlag(InternalFlags.IsStyleUpdateInProgress); }
            set { WriteInternalFlag(InternalFlags.IsStyleUpdateInProgress, value); }
        }

        internal bool IsThemeStyleUpdateInProgress
        {
            get { return ReadInternalFlag(InternalFlags.IsThemeStyleUpdateInProgress); }
            set { WriteInternalFlag(InternalFlags.IsThemeStyleUpdateInProgress, value); }
        }

        internal object[] LoadedPending
        {
            get { return (object[])GetValue(LoadedPendingProperty); }
        }

        internal object[] UnloadedPending
        {
            get { return (object[])GetValue(UnloadedPendingProperty); }
        }

        internal bool ThisHasLoadedChangeEventHandler
        {
            get
            {
                EventHandlersStore store = EventHandlersStore;
                if (store != null)
                {
                    if (store.Contains(LoadedEvent) || store.Contains(UnloadedEvent))
                    {
                        return true;
                    }
                }

                if (Style != null && Style.HasLoadedChangeHandler)
                {
                    return true;
                }

                if (ThemeStyle != null && ThemeStyle.HasLoadedChangeHandler)
                {
                    return true;
                }

                if (TemplateInternal != null && TemplateInternal.HasLoadedChangeHandler)
                {
                    return true;
                }

                if (HasFefLoadedChangeHandler)
                {
                    return true;
                }

                return false;
            }
        }

        private bool HasFefLoadedChangeHandler
        {
            get
            {
                if (TemplatedParent == null)
                {
                    return false;
                }

                FrameworkElementFactory rootFef = BroadcastEventHelper.GetFEFTreeRoot(TemplatedParent);
                if (rootFef == null)
                {
                    return false;
                }

                FrameworkElementFactory fef = StyleHelper.FindFEF(rootFef, TemplateChildIndex);
                return fef != null && fef.HasLoadedChangeHandler;
            }
        }

        internal event EventHandler ResourcesChanged
        {
            add
            {
                EventHandlersStoreAdd(ResourcesChangedKey, value);
            }
            remove
            {
                EventHandlersStoreRemove(ResourcesChangedKey, value);
            }
        }

        internal event InheritedPropertyChangedEventHandler InheritedPropertyChanged
        {
            add
            {
                PotentiallyHasMentees = true;
                EventHandlersStoreAdd(InheritedPropertyChangedKey, value);
            }
            remove
            {
                EventHandlersStoreRemove(InheritedPropertyChangedKey, value);
            }
        }

        internal void RaiseInheritedPropertyChangedEvent(ref InheritablePropertyChangeInfo info)
        {
            EventHandlersStore store = EventHandlersStore;
            if (store != null)
            {
                Delegate d = store.Get(InheritedPropertyChangedKey);
                if (d != null)
                {
                    InheritedPropertyChangedEventArgs args = new InheritedPropertyChangedEventArgs(ref info);
                    ((InheritedPropertyChangedEventHandler)d)(this, args);
                }
            }
        }

        internal object FindResourceOnSelf(
            object resourceKey,
            bool allowDeferredResourceReference,
            bool mustReturnDeferredResourceReference)
        {
            ResourceDictionary resources = ResourcesField.GetValue(this);
            if (resources != null && resources.Contains(resourceKey))
            {
                return resources.FetchResource(
                    resourceKey,
                    allowDeferredResourceReference,
                    mustReturnDeferredResourceReference,
                    out _);
            }

            return DependencyProperty.UnsetValue;
        }

        internal void UpdateStyleProperty()
        {
            if (!HasStyleEverBeenFetched)
            {
                HasStyleEverBeenFetched = true;
            }

            if (!IsStyleUpdateInProgress)
            {
                IsStyleUpdateInProgress = true;
                try
                {
                    InvalidateProperty(StyleProperty);
                    HasStyleInvalidated = false;
                }
                finally
                {
                    IsStyleUpdateInProgress = false;
                }
            }
            else
            {
                throw new InvalidOperationException(SR.Format(SR.CyclicStyleReferenceDetected, this));
            }
        }

        internal void UpdateThemeStyleProperty()
        {
            if (!HasThemeStyleEverBeenFetched)
            {
                HasThemeStyleEverBeenFetched = true;
            }

            if (!IsThemeStyleUpdateInProgress)
            {
                IsThemeStyleUpdateInProgress = true;
                try
                {
                    Style themeStyle = StyleHelper.GetThemeStyle(this, null);
                    OnThemeStyleChanged(this, _themeStyleCache, themeStyle);
                }
                finally
                {
                    IsThemeStyleUpdateInProgress = false;
                }
            }
            else
            {
                throw new InvalidOperationException(SR.Format(SR.CyclicThemeStyleReferenceDetected, this));
            }
        }

        internal void OnAncestorChangedInternal(TreeChangeInfo parentInfo)
        {
            bool wasSelfInheritanceParent = IsSelfInheritanceParent;
            bool isSelfStyleInvalidated = false;

            if (!IsSelfInheritanceParent)
            {
                DependencyObject parent = GetFrameworkParent(this);
                InheritanceBehavior parentInheritanceBehavior = InheritanceBehavior.Default;

                if (parent != null)
                {
                    FrameworkObject parentFO = new FrameworkObject(parent, true);
                    parentInheritanceBehavior = parentFO.InheritanceBehavior;
                }

                if (!TreeWalkHelper.SkipNext(InheritanceBehavior) && !TreeWalkHelper.SkipNow(parentInheritanceBehavior))
                {
                    SynchronizeInheritanceParent(parent);
                }
                else
                {
                    SynchronizeInheritanceParent(null);
                }
            }

            if (this != parentInfo.Root)
            {
                HasStyleChanged = false;
                HasStyleInvalidated = false;
                HasTemplateChanged = false;

                if (parentInfo.IsAddOperation)
                {
                    FrameworkObject fo = new FrameworkObject(this);
                    fo.SetShouldLookupImplicitStyles();
                }

                if (IsInitialized && !HasLocalStyle)
                {
                    AncestorChangeInProgress = true;
                    try
                    {
                        UpdateStyleProperty();
                        isSelfStyleInvalidated = true;
                    }
                    finally
                    {
                        AncestorChangeInProgress = false;
                    }
                }
            }

            if (HasResourceReference)
            {
                AncestorChangeInProgress = true;
                try
                {
                    TreeWalkHelper.OnResourcesChanged(this, ResourcesChangeInfo.TreeChangeInfo, true);
                }
                finally
                {
                    AncestorChangeInProgress = false;
                }
            }

            ChildRecord childRecord = default;
            bool isChildRecordValid = false;
            if (TemplatedParent != null)
            {
                GetTemplatedParentChildRecord(TemplatedParent, TemplateChildIndex, out childRecord, out isChildRecordValid);
            }

            bool isSelfInheritanceParent = IsSelfInheritanceParent;
            FrugalObjectList<DependencyProperty> currentInheritableProperties =
                TreeWalkHelper.InvalidateTreeDependentProperties(
                    parentInfo,
                    this,
                    null,
                    Style,
                    ThemeStyle,
                    ref childRecord,
                    isChildRecordValid,
                    HasStyleChanged || isSelfStyleInvalidated,
                    isSelfInheritanceParent,
                    wasSelfInheritanceParent);

            parentInfo.InheritablePropertiesStack.Push(currentInheritableProperties);

            OnAncestorChanged();
        }

        internal void FireLoadedOnDescendentsInternal()
        {
            if (LoadedPending == null)
            {
                DependencyObject parent = Parent ?? VisualTreeHelper.GetParent(this);
                BroadcastEventHelper.AddLoadedCallback(this, parent);
            }
        }

        internal void FireUnloadedOnDescendentsInternal()
        {
            if (UnloadedPending == null)
            {
                DependencyObject parent = Parent ?? VisualTreeHelper.GetParent(this);
                BroadcastEventHelper.AddUnloadedCallback(this, parent);
            }
        }

        internal void ChangeSubtreeHasLoadedChangedHandler(DependencyObject mentor)
        {
            FrameworkObject foMentor = new FrameworkObject(mentor);
            bool hasLoadedChangeHandler = SubtreeHasLoadedChangeHandler;
            DependencyObject oldMentor = MentorField.GetValue(this);

            if (oldMentor != null && (oldMentor != mentor || !hasLoadedChangeHandler || Parent != null || VisualTreeHelper.GetParent(this) != null))
            {
                FrameworkObject foOld = new FrameworkObject(oldMentor);
                if (foOld.IsFE)
                {
                    foOld.FE.Loaded -= new RoutedEventHandler(OnMentorLoaded);
                    foOld.FE.Unloaded -= new RoutedEventHandler(OnMentorUnloaded);
                }
                else if (foOld.IsFCE)
                {
                    foOld.FCE.Loaded -= new RoutedEventHandler(OnMentorLoaded);
                    foOld.FCE.Unloaded -= new RoutedEventHandler(OnMentorUnloaded);
                }
                MentorField.ClearValue(this);
            }

            if (hasLoadedChangeHandler && foMentor.IsValid && Parent == null && VisualTreeHelper.GetParent(this) == null)
            {
                if (oldMentor != mentor)
                {
                    MentorField.SetValue(this, mentor);
                    if (foMentor.IsFE)
                    {
                        foMentor.FE.Loaded += new RoutedEventHandler(OnMentorLoaded);
                        foMentor.FE.Unloaded += new RoutedEventHandler(OnMentorUnloaded);
                    }
                    else if (foMentor.IsFCE)
                    {
                        foMentor.FCE.Loaded += new RoutedEventHandler(OnMentorLoaded);
                        foMentor.FCE.Unloaded += new RoutedEventHandler(OnMentorUnloaded);
                    }
                }
            }
        }

        private void OnMentorLoaded(object sender, RoutedEventArgs e)
        {
            if (sender == MentorField.GetValue(this))
            {
                BroadcastEventHelper.BroadcastLoadedSynchronously(this, IsLoaded);
            }
        }

        private void OnMentorUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender == MentorField.GetValue(this))
            {
                BroadcastEventHelper.BroadcastUnloadedSynchronously(this, IsLoaded);
            }
        }

        /// <summary>
        /// Adds the provided object to the logical tree of this element.
        /// </summary>
        protected internal void AddLogicalChild(object child)
        {
            if (child != null)
            {
                if (IsLogicalChildrenIterationInProgress)
                {
                    throw new InvalidOperationException(SR.CannotModifyLogicalChildrenDuringTreeWalk);
                }

                TryFireInitialized();

                HasLogicalChildren = true;

                FrameworkObject fo = new FrameworkObject(child as DependencyObject);
                fo.ChangeLogicalParent(this);
            }
        }

        /// <summary>
        /// Removes the specified element from the logical tree for this element.
        /// </summary>
        protected internal void RemoveLogicalChild(object child)
        {
            if (child != null)
            {
                if (IsLogicalChildrenIterationInProgress)
                {
                    throw new InvalidOperationException(SR.CannotModifyLogicalChildrenDuringTreeWalk);
                }

                FrameworkObject fo = new FrameworkObject(child as DependencyObject);
                if (fo.Parent == this)
                {
                    fo.ChangeLogicalParent(null);
                }

                IEnumerator children = LogicalChildren;
                HasLogicalChildren = (children != null && children.MoveNext());
            }
        }

        internal void ChangeLogicalParent(DependencyObject newParent)
        {
            VerifyAccess();

            if (newParent != null)
            {
                if (_parent != null && _parent != newParent)
                {
                    throw new InvalidOperationException(SR.HasLogicalParent);
                }

                ClearInheritanceContext();
            }

            if (newParent == _parent)
            {
                return;
            }

            DependencyObject oldParent = _parent;
            OnNewParent(newParent);

            BroadcastEventHelper.AddOrRemoveHasLoadedChangeHandlerFlag(this, oldParent, newParent);
            BroadcastEventHelper.BroadcastLoadedOrUnloadedEvent(this, oldParent, newParent);

            DependencyObject parentForTreeWalk = newParent ?? oldParent;
            if (parentForTreeWalk != null)
            {
                TreeWalkHelper.InvalidateOnTreeChange(this, null, parentForTreeWalk, isAddOperation: newParent != null);
            }

            if (newParent == null)
            {
                DependencyObject visualParent = VisualTreeHelper.GetParent(this);
                if (visualParent != null)
                {
                    TreeWalkHelper.InvalidateOnTreeChange(this, null, visualParent, isAddOperation: true);
                }
            }

            TryFireInitialized();
        }

        internal virtual void OnNewParent(DependencyObject newParent)
        {
            _parent = newParent;
            IsParentAnFE = (newParent is FrameworkElement);
        }

        internal override DependencyObject InheritanceContext
        {
            get { return InheritanceContextField.GetValue(this); }
        }

        internal override bool HasMultipleInheritanceContexts
        {
            get { return ReadInternalFlag2(InternalFlags2.HasMultipleInheritanceContexts); }
        }

        internal override bool ShouldProvideInheritanceContext(DependencyObject target, DependencyProperty property)
        {
            return property == null || (property != ContentControl.ContentProperty && property != Window.IWindowServiceProperty);
        }

        internal override void AddInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            if (Parent == null && VisualTreeHelper.GetParent(this) == null &&
                (property == null || (property != ContentControl.ContentProperty && property != Window.IWindowServiceProperty)) &&
                !FrameworkObject.IsEffectiveAncestor(this, context))
            {
                DependencyObject oldMentor = Helper.FindMentor(InheritanceContext);
                bool hasMultiple = HasMultipleInheritanceContexts;
                DependencyObject inheritanceContext = InheritanceContextField.GetValue(this);

                InheritanceContextHelper.AddInheritanceContext(
                    context,
                    this,
                    ref hasMultiple,
                    ref inheritanceContext);

                WriteInternalFlag2(InternalFlags2.HasMultipleInheritanceContexts, hasMultiple);
                if (inheritanceContext != null)
                {
                    InheritanceContextField.SetValue(this, inheritanceContext);
                }
                else
                {
                    InheritanceContextField.ClearValue(this);
                }

                DependencyObject newMentor = Helper.FindMentor(inheritanceContext);
                if (oldMentor != newMentor)
                {
                    ChangeSubtreeHasLoadedChangedHandler(newMentor);
                    if (oldMentor != null)
                    {
                        TreeWalkHelper.InvalidateOnTreeChange(this, null, oldMentor, isAddOperation: false);
                    }
                    if (newMentor != null)
                    {
                        TreeWalkHelper.InvalidateOnTreeChange(this, null, newMentor, isAddOperation: true);
                    }
                }
            }
        }

        internal override void RemoveInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            DependencyObject oldMentor = Helper.FindMentor(InheritanceContext);
            bool hasMultiple = HasMultipleInheritanceContexts;
            DependencyObject inheritanceContext = InheritanceContextField.GetValue(this);

            InheritanceContextHelper.RemoveInheritanceContext(
                context,
                this,
                ref hasMultiple,
                ref inheritanceContext);

            WriteInternalFlag2(InternalFlags2.HasMultipleInheritanceContexts, hasMultiple);
            if (inheritanceContext != null)
            {
                InheritanceContextField.SetValue(this, inheritanceContext);
            }
            else
            {
                InheritanceContextField.ClearValue(this);
            }

            DependencyObject newMentor = Helper.FindMentor(inheritanceContext);
            if (oldMentor != newMentor)
            {
                ChangeSubtreeHasLoadedChangedHandler(newMentor);
                if (oldMentor != null)
                {
                    TreeWalkHelper.InvalidateOnTreeChange(this, null, oldMentor, isAddOperation: false);
                }
                if (newMentor != null)
                {
                    TreeWalkHelper.InvalidateOnTreeChange(this, null, newMentor, isAddOperation: true);
                }
            }
        }

        internal void ClearInheritanceContext()
        {
            DependencyObject oldContext = InheritanceContextField.GetValue(this);
            if (oldContext != null || HasMultipleInheritanceContexts)
            {
                DependencyObject oldMentor = Helper.FindMentor(oldContext);
                InheritanceContextField.ClearValue(this);
                WriteInternalFlag2(InternalFlags2.HasMultipleInheritanceContexts, false);
                ChangeSubtreeHasLoadedChangedHandler(null);
                OnInheritanceContextChanged(EventArgs.Empty);

                if (oldMentor != null)
                {
                    TreeWalkHelper.InvalidateOnTreeChange(this, null, oldMentor, isAddOperation: false);
                }
            }
        }

        /// <summary>
        /// Finds an element that has the provided identifier name.
        /// </summary>
        public object FindName(string name)
        {
            return FindName(name, out _);
        }

        internal object FindName(string name, out DependencyObject scopeOwner)
        {
            INameScope nameScope = FindScope(this, out scopeOwner);
            if (nameScope != null)
            {
                return nameScope.FindName(name);
            }

            return null;
        }

        /// <summary>
        /// Provides an accessor that simplifies access to the NameScope registration method.
        /// </summary>
        public void RegisterName(string name, object scopedElement)
        {
            INameScope nameScope = FindScope(this);
            if (nameScope != null)
            {
                nameScope.RegisterName(name, scopedElement);
            }
            else
            {
                throw new InvalidOperationException(SR.Format(SR.NameScopeNotFound, name, "register"));
            }
        }

        /// <summary>
        /// Simplifies access to the NameScope de-registration method.
        /// </summary>
        public void UnregisterName(string name)
        {
            INameScope nameScope = FindScope(this);
            if (nameScope != null)
            {
                nameScope.UnregisterName(name);
            }
            else
            {
                throw new InvalidOperationException(SR.Format(SR.NameScopeNotFound, name, "unregister"));
            }
        }
    }
}
