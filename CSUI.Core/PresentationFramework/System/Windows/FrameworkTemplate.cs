// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Description:
//   A generic class that allow instantiation of a tree of Framework[Content]Elements.
//
//

using MS.Internal;                      // Helper
using MS.Utility;                       // ChildValueLookupList
using System.ComponentModel;            // DesignerSerializationVisibility, DefaultValueAttribute
using System.Collections;               // Hashtable
using System.Collections.Specialized;   // HybridDictionary
using System.Runtime.CompilerServices;  // ConditionalWeakTable
using System.Windows.Markup;     // XamlTemplateSerializer, ContentPropertyAttribute
using System.Windows.Diagnostics;
using System.Windows.Threading; // DispatcherObject
using System.Windows.Data;


#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace System.Windows
{
    /// <summary>
    ///     A generic class that allow instantiation of a
    ///     tree of Framework[Content]Elements.
    /// </summary>

    // The ContentProperty really isn't VisualTree in WPF4 and later.  Right now, we continue to lie for compat with Cider.
    [ContentProperty("VisualTree")]
    [Localizability(LocalizationCategory.NeverLocalize)] // All properties on template are not localizable
    public abstract class FrameworkTemplate : DispatcherObject, INameScope, ISealable, IHaveResources, IQueryAmbient
    {

        /// <summary>
        /// </summary>
        protected FrameworkTemplate()
        {
#if DEBUG
            _debugInstanceCounter = ++_globalDebugInstanceCounter;
#endif
        }

        #region PublicMethods



        /// <summary>
        ///     Subclasses must override this method if they need to
        ///     impose additional rules for the TemplatedParent
        /// </summary>
        protected virtual void ValidateTemplatedParent(FrameworkElement templatedParent)
        {
            Debug.Assert(templatedParent != null,
                "Must have a non-null FE TemplatedParent.");
        }

        #endregion PublicMethods

        #region PublicProperties

        /// <summary>
        ///     Says if this template has been sealed
        /// </summary>
        public bool IsSealed
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                return _sealed;
            }
        }

        /// <summary>
        ///     Root node of the template
        /// </summary>
        public FrameworkElementFactory VisualTree
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                return _templateRoot;
            }
            set
            {
                // Verify Context Access
                VerifyAccess();

                CheckSealed();
                ValidateVisualTree(value);

                _templateRoot = value;

            }
        }


        /// <summary>
        /// Indicates if the VisualTree property should be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeVisualTree()
        {
            // Verify Context Access
            VerifyAccess();

            return HasContent || VisualTree != null;
        }



        /*
        /// <summary>
        /// The content of this template.  The alternate form of template content is the VisualTree property.
        /// One of these two properties may be set, but not both.  Note that getting this property can cause
        /// the content to created, which could be expensive.  So to merely check if Content is set to a non-null
        /// value, use the HasContent property.
        /// </summary>

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public FrameworkElement Content
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                if( HasContent )
                {
                    return LoadTemplateContent() as FrameworkElement;
                }
                else
                {
                    return null;
                }
            }
        }


        /// <summary>
        /// Returns true if the Content property should be serialized
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeContent()
        {
            // Verify Context Access
            VerifyAccess();

            return HasContent || VisualTree != null;
        }


        /// <summary>
        /// Resets the Content property to null;
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ResetContent()
        {
            // Verify Context Access
            VerifyAccess();

            _templateRoot = null;
        }

        */


        /// <summary>
        ///     The collection of resources that can be
        ///     consumed by the container and its sub-tree.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Ambient]
        public ResourceDictionary Resources
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                if ( _resources == null )
                {
                    _resources = new ResourceDictionary
                    {
                        // A Template ResourceDictionary can be accessed across threads
                        CanBeAccessedAcrossThreads = true
                    };
                }

                if ( IsSealed )
                {
                    _resources.IsReadOnly = true;
                }

                return _resources;
            }
            set
            {
                // Verify Context Access
                VerifyAccess();

                if ( IsSealed )
                {
                    throw new InvalidOperationException(SR.Format(SR.CannotChangeAfterSealed, "Template"));
                }

                _resources = value;

                // A Template ResourceDictionary can be accessed across threads
                _resources?.CanBeAccessedAcrossThreads = true;
            }
        }

        ResourceDictionary IHaveResources.Resources
        {
            get { return Resources; }
            set { Resources = value; }
        }

        /// <summary>
        ///     Tries to find a Reosurce for the given resourceKey in the current
        ///     template's ResourceDictionary.
        /// </summary>
        internal object FindResource(object resourceKey, bool allowDeferredResourceReference, bool mustReturnDeferredResourceReference)
        {
            if ((_resources != null) && _resources.Contains(resourceKey))
            {
                bool canCache;
                return _resources.FetchResource(resourceKey, allowDeferredResourceReference, mustReturnDeferredResourceReference, out canCache);
            }
            return DependencyProperty.UnsetValue;
        }

        bool IQueryAmbient.IsAmbientPropertyAvailable(string propertyName)
        {
            // We want to make sure that StaticResource resolution checks the .Resources
            // Ie.  The Ambient search should look at Resources if it is set.
            // Even if it wasn't set from XAML (eg. the Ctor (or derived Ctor) added stuff)
            if (propertyName == "Resources" && _resources == null)
            {
                return false;
            }
            else if (propertyName == "Template")
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// FindName - Finds the element associated with the id defined under this control template.
        ///          Context of the FrameworkElement where this template is applied will be passed
        ///          as parameter.
        /// </summary>
        /// <param name="name">string name</param>
        /// <param name="templatedParent">context where this template is applied</param>
        /// <returns>the element associated with the Name</returns>
        public Object FindName(string name, FrameworkElement templatedParent)
        {
            // Verify Context Access
            VerifyAccess();

            ArgumentNullException.ThrowIfNull(templatedParent);

            if (this != templatedParent.TemplateInternal)
            {
                throw new InvalidOperationException(SR.TemplateFindNameInInvalidElement);
            }

            return StyleHelper.FindNameInTemplateContent(templatedParent, name, this);
        }

        #endregion PublicProperties


        #region INameScope

        /// <summary>
        /// Registers the name - Context combination
        /// </summary>
        /// <param name="name">Name to register</param>
        /// <param name="scopedElement">Element where name is defined</param>
        public void RegisterName(string name, object scopedElement)
        {
            // Verify Context Access
            VerifyAccess();

            _nameScope.RegisterName(name, scopedElement);
        }

        /// <summary>
        /// Unregisters the name - element combination
        /// </summary>
        /// <param name="name">Name of the element</param>
        public void UnregisterName(string name)
        {
            // Verify Context Access
            VerifyAccess();

            _nameScope.UnregisterName(name);
        }

        /// <summary>
        /// Find the element given name
        /// </summary>
        /// <param name="name">Name of the element</param>
        object INameScope.FindName(string name)
        {
            // Verify Context Access
            VerifyAccess();

            return _nameScope.FindName(name);
        }

        private NameScope _nameScope = new NameScope();

        #endregion INameScope




        #region NonPublicMethods


        //  ===========================================================================
        //  Validation methods
        //  ===========================================================================

        // Validate against the following rules
        // 1. The VisualTree's root must be a FrameworkElement.
        private void ValidateVisualTree(FrameworkElementFactory templateRoot)
        {
            // The VisualTree's root must be a FrameworkElement.
            if (templateRoot != null &&
                typeof(FrameworkContentElement).IsAssignableFrom(templateRoot.Type))
            {
                throw new ArgumentException(SR.Format(SR.VisualTreeRootIsFrameworkElement, nameof(FrameworkElement), templateRoot.Type.Name));
            }
        }

        //  ===========================================================================
        //  These methods are invoked when a Template is being sealed
        //  ===========================================================================

        #region Seal

        internal virtual void ProcessTemplateBeforeSeal()
        {
        }



        /// <summary>
        /// Seal this FrameworkTemplate
        /// </summary>
        public void Seal()
        {
            // Verify Context Access
            VerifyAccess();

            StyleHelper.SealTemplate(
                this,
                ref _sealed,
                _templateRoot,
                TriggersInternal,
                _resources,
                ChildIndexFromChildName,
                ref ChildRecordFromChildIndex,
                ref TriggerSourceRecordFromChildIndex,
                ref ContainerDependents,
                ref ResourceDependents,
                ref EventDependents,
                ref _triggerActions,
                ref _dataTriggerRecordFromBinding,
                ref _hasInstanceValues,
                ref _eventHandlersStore);
        }

        // Subclasses need to call this method before any changes to their state.
        internal void CheckSealed()
        {
            if (_sealed)
            {
                throw new InvalidOperationException(SR.Format(SR.CannotChangeAfterSealed, "Template"));
            }
        }

        // compute and cache the flags for ResourceReferences
        internal void SetResourceReferenceState()
        {
            Debug.Assert(!_sealed, "call this method before template is sealed");

            StyleHelper.SortResourceDependents(ref ResourceDependents);

            for (int i = 0; i < ResourceDependents.Count; ++i)
            {
                if (ResourceDependents[i].ChildIndex == 0)
                {
                    WriteInternalFlag(InternalFlags.HasContainerResourceReferences, true);
                }
                else
                {
                    WriteInternalFlag(InternalFlags.HasChildResourceReferences, true);
                }
            }
        }

        #endregion Seal

        //  ===========================================================================
        //  These methods are invoked to during a call call to
        //  FE.EnsureVisual or FCE.EnsureLogical
        //  ===========================================================================

        #region InstantiateSubTree

        //
        //  This method
        //  Creates the VisualTree
        //
        //[CodeAnalysis("AptcaMethodsShouldOnlyCallAptcaMethods")] //Tracking Bug: 29647
        internal bool ApplyTemplateContent(
            UncommonField<HybridDictionary[]> templateDataField,
            FrameworkElement container)
        {
#if STYLE_TRACE
            _timer.Begin();
#endif

            if (TraceDependencyProperty.IsEnabled)
            {
                TraceDependencyProperty.Trace(
                    TraceEventType.Start,
                    TraceDependencyProperty.ApplyTemplateContent,
                    container,
                    this);
            }


            ValidateTemplatedParent(container);

            bool visualsCreated = StyleHelper.ApplyTemplateContent(templateDataField, container,
                _templateRoot, _lastChildIndex,
                ChildIndexFromChildName, this);

            if (TraceDependencyProperty.IsEnabled)
            {
                TraceDependencyProperty.Trace(
                    TraceEventType.Stop,
                    TraceDependencyProperty.ApplyTemplateContent,
                    container,
                    this);
            }


#if STYLE_TRACE
            _timer.End();
            if (visualsCreated)
            {
                string label = container.ID;
                if (label == null || label.Length == 0)
                    label = container.GetHashCode().ToString();
                Console.WriteLine("  Style.VT created for {0} {1} in {2:f2} msec",
                    container.GetType().Name, label, _timer.TimeOfLastPeriod);
            }
#endif

            return visualsCreated;
        }

        #endregion InstantiateSubTree




        //+------------------------------------------------------------------------------------------------------
        //
        //  LoadContent
        //
        //  Load the content of a template, returning the root element of the content.  The second version
        //  loads the template content for use on an FE (i.e. under FrameworkElement.ApplyTemplate).  The
        //  first version is used by the Content property for serialization, no optimization is
        //  performed, and TemplateBinding's show up as TemplateBindingExpression's.  So it's just a normal
        //  tree.
        //
        //+------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Load the content of a template as an instance of an object.  Calling this multiple times
        /// will return separate instances.
        /// </summary>
        public DependencyObject LoadContent()
        {
            // Verify Context Access
            VerifyAccess();

            if (VisualTree != null)
            {
                if (!IsSealed)
                {
                    Seal();
                }
                FrameworkObject frameworkObject = VisualTree.InstantiateUnoptimizedTree();
                return frameworkObject.DO;
            }
            else
            {
                return LoadContent(null, null);
            }
        }


        internal DependencyObject LoadContent(
            DependencyObject container,
            List<DependencyObject> affectedChildren)
        {
            return null;
        }

        /// <summary>
        /// Indicate if this template has optimized content.
        /// </summary>
        public bool HasContent
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                return false;
            }
        }




        //  ===========================================================================
        //  These methods are invoked when the template has an alternate
        //  mechanism of generating a visual tree.
        //  ===========================================================================

        #region BuildVisualTree

        //
        //  This method
        //  1. Is an alternative approach to building a visual tree from a FEF
        //  2. Is used by ContentPresenter and Hyperlink to host their content
        //
        internal virtual bool BuildVisualTree(FrameworkElement container)
        {
            return false;
        }

        //
        //  This property
        //  1. Says if this Template is meant to use BuildVisualTree mechanism
        //     to generate a visual tree.
        //  2. Is used in the following scenario.
        //     We need to preserve the treeState cache on a container node
        //     even after all its logical children have been added. This is so that
        //     construction of the style visual tree nodes can consume the cache.
        //     This method helps us know whether we should retain the cache for
        //     special scenarios when the visual tree is being built via BuildVisualTree
        //
        internal bool CanBuildVisualTree
        {
            get { return ReadInternalFlag(InternalFlags.CanBuildVisualTree); }
            set { WriteInternalFlag(InternalFlags.CanBuildVisualTree, value); }
        }


        #endregion BuildVisualTree


        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeResources()
        {
            // Verify Context Access
            VerifyAccess();

            return _resources != null && _resources.Count > 0;
        }

        // Extracts the required flag and returns
        // bool to indicate if it is set or unset
        private bool ReadInternalFlag(InternalFlags reqFlag)
        {
            return (_flags & reqFlag) != 0;
        }

        // Sets or Unsets the required flag based on
        // the bool argument
        private void WriteInternalFlag(InternalFlags reqFlag, bool set)
        {
            if (set)
            {
                _flags |= reqFlag;
            }
            else
            {
                _flags &= (~reqFlag);
            }
        }

        #endregion NonPublicMethods

        #region NonPublicProperties

        //+----------------------------------------------------------------------------------------------------------------
        //
        //  SetTemplateParentValues
        //
        //  This method takes the "template parent values" (those that look like local values in the template), which
        //  are ordinarily shared, and sets them as local values on the FE/FCE that was just created.  This is used
        //  during serialization.
        //
        //+----------------------------------------------------------------------------------------------------------------

        internal static void SetTemplateParentValues(
                                                          string name,
                                                          object element,
                                                          FrameworkTemplate frameworkTemplate,
                                                          ref ProvideValueServiceProvider provideValueServiceProvider)
        {
            int childIndex;

            // Loop through the shared values, and set them onto the element.

            FrugalStructList<ChildRecord> childRecordFromChildIndex;
            HybridDictionary childIndexFromChildName;

            // Seal the template, and get the name->index and index->ChildRecord mappings

            if (!frameworkTemplate.IsSealed)
            {
                frameworkTemplate.Seal();
            }

            childIndexFromChildName = frameworkTemplate.ChildIndexFromChildName;
            childRecordFromChildIndex = frameworkTemplate.ChildRecordFromChildIndex;


            // Calculate the child index

            childIndex = StyleHelper.QueryChildIndexFromChildName(name, childIndexFromChildName);

            // Do we have a ChildRecord for this index (i.e., there's some property set on it)?

            if (childIndex < childRecordFromChildIndex.Count)
            {
                // Yes, get the record.

                ChildRecord child = (ChildRecord)childRecordFromChildIndex[childIndex];

                // Loop through the properties which are in some way set on this child

                for (int i = 0; i < child.ValueLookupListFromProperty.Count; i++)
                {
                    // And for each of those properties, loop through the potential values specified in the template
                    // for that property on that child.

                    for (int j = 0; j < child.ValueLookupListFromProperty.Entries[i].Value.Count; j++)
                    {
                        // Get this value (in valueLookup)

                        ChildValueLookup valueLookup;
                        valueLookup = (ChildValueLookup)child.ValueLookupListFromProperty.Entries[i].Value.List[j];

                        // See if this value is one that is considered to be locally set on the child element

                        if (valueLookup.LookupType == ValueLookupType.Simple
                            ||
                            valueLookup.LookupType == ValueLookupType.Resource
                            ||
                            valueLookup.LookupType == ValueLookupType.TemplateBinding)
                        {

                            // This shared value is for this element, so we'll set it.

                            object value = valueLookup.Value;

                            // If this is a TemplateBinding, put on an expression for it, so that it can
                            // be represented correctly (e.g. for serialization).  Otherwise, keep it as an ME.

                            if (valueLookup.LookupType == ValueLookupType.TemplateBinding)
                            {
                                value = new TemplateBindingExpression(value as TemplateBindingExtension);

                            }

                            // Dynamic resources need to be converted to an expression also.

                            else if (valueLookup.LookupType == ValueLookupType.Resource)
                            {
                                value = new ResourceReferenceExpression(value);
                            }

                            // Bindings are handled as just an ME

                            // Set the value directly onto the element.

                            MarkupExtension me = value as MarkupExtension;

                            if (me != null)
                            {
                                // This is provided for completeness, but really there's only a few
                                // MEs that survive TemplateBamlRecordReader.  E.g. NullExtension would
                                // have been converted to a null by now.  There's only a few MEs that
                                // are preserved, e.g. Binding and DynamicResource.  Other MEs, such as
                                // StaticResource, wouldn't be able to ProvideValue here, because we don't
                                // have a ParserContext.

                                if (provideValueServiceProvider == null)
                                {
                                    provideValueServiceProvider = new ProvideValueServiceProvider();
                                }

                                provideValueServiceProvider.SetData(element, valueLookup.Property);
                                value = me.ProvideValue(provideValueServiceProvider);
                                provideValueServiceProvider.ClearData();
                            }

                            (element as DependencyObject).SetValue(valueLookup.Property, value); //sharedDp.Dp, value );

                        }
                    }
                }
            }
        }






        //
        //  TargetType for ControlTemplate
        //
        internal virtual Type TargetTypeInternal
        {
            get { return null; }
        }

        // Subclasses must provide a way for the parser to directly set the
        // target type.
        internal abstract void SetTargetTypeInternal(Type targetType);

        //
        //  DataType for DataTemplate
        //
        internal virtual object DataTypeInternal
        {
            get { return null; }
        }

        #region ISealable

        /// <summary>
        /// Can this template be sealed
        /// </summary>
        bool ISealable.CanSeal
        {
            get { return true; }
        }

        /// <summary>
        /// Is this template sealed
        /// </summary>
        bool ISealable.IsSealed
        {
            get { return IsSealed; }
        }

        /// <summary>
        /// Seal this template
        /// </summary>
        void ISealable.Seal()
        {
            Seal();
        }

        #endregion ISealable

        //
        //  Collection of Triggers for a ControlTemplate
        //
        internal virtual TriggerCollection TriggersInternal
        {
            get { return null; }
        }

        //
        //  Says if this template contains any resource references
        //
        internal bool HasResourceReferences
        {
            get { return ResourceDependents.Count > 0; }
        }

        //
        //  Says if this template contains any resource references for properties on the container
        //
        internal bool HasContainerResourceReferences
        {
            get { return ReadInternalFlag(InternalFlags.HasContainerResourceReferences); }
        }

        //
        //  Says if this template contains any resource references for properties on children
        //
        internal bool HasChildResourceReferences
        {
            get { return ReadInternalFlag(InternalFlags.HasChildResourceReferences); }
        }

        //
        //  Says if this template contains any event handlers
        //
        internal bool HasEventDependents
        {
            get { return (EventDependents.Count > 0); }
        }

        //
        //  Says if this template contains any per-instance values
        //
        internal bool HasInstanceValues
        {
            get { return _hasInstanceValues; }
        }

        //
        // Says if we have anything listening for the Loaded or Unloaded
        // event (used for an optimization in FrameworkElement).
        //
        internal bool HasLoadedChangeHandler
        {
            get { return ReadInternalFlag(InternalFlags.HasLoadedChangeHandler); }
            set { WriteInternalFlag(InternalFlags.HasLoadedChangeHandler, value); }
        }


        //
        //  Store all the event handlers for this Style TargetType
        //
        internal EventHandlersStore EventHandlersStore
        {
            get { return _eventHandlersStore; }
        }

        //
        // Style and component connectors used during template
        // application.
        //
        internal IStyleConnector StyleConnector
        {
            get { return _styleConnector; }
            set { _styleConnector = value; }
        }
        internal IComponentConnector ComponentConnector
        {
            get { return _componentConnector; }
            set { _componentConnector = value; }
        }


        //
        // Prefetched values for static resources
        //
        internal object[] StaticResourceValues
        {
            get { return _staticResourceValues; }
            set { _staticResourceValues = value; }
        }

        internal bool HasXamlNodeContent
        {
            get { return false; }
        }

        internal HybridDictionary ChildIndexFromChildName
        {
            get { return _childIndexFromChildName; }
        }

        internal Dictionary<int, Type> ChildTypeFromChildIndex
        {
            get { return _childTypeFromChildIndex; }
        }

        internal int LastChildIndex
        {
            get { return _lastChildIndex; }
            set { _lastChildIndex = value; }
        }

        internal List<String> ChildNames
        {
            get { return _childNames; }
        }

        #endregion NonPublicProperties

        #region Data

        private InternalFlags _flags;
        private bool _sealed;    // passed by ref, so cannot use flags
        internal bool _hasInstanceValues; // passed by ref, so cannot use flags

        private IStyleConnector _styleConnector;
        private IComponentConnector _componentConnector;

        // If we're a FEF-based template, we'll have a _templateRoot.

        private FrameworkElementFactory _templateRoot;

        //
        //  Used to generate childIndex for each TemplateNode
        //
        private HybridDictionary _childIndexFromChildName = new HybridDictionary();
        private Dictionary<int, Type> _childTypeFromChildIndex = new Dictionary<int, Type>();
        private int _lastChildIndex = 1; // 0 means "self" (container), no instance ever has index 0
        private List<String> _childNames = new List<string>();

        //
        // Resource dictionary associated with this template.
        //
        internal ResourceDictionary _resources = null;

        //
        //  Used by EventTrigger: Maps a RoutedEventID to a set of TriggerAction objects
        //  to be performed.
        //
        internal HybridDictionary _triggerActions = null;

        //
        // Shared tables used during GetValue
        //
        internal FrugalStructList<ChildRecord> ChildRecordFromChildIndex = new FrugalStructList<ChildRecord>(); // Indexed by Child.ChildIndex

        //
        // Shared tables used during OnTriggerSourcePropertyInvalidated
        //
        internal FrugalStructList<ItemStructMap<TriggerSourceRecord>> TriggerSourceRecordFromChildIndex = new FrugalStructList<ItemStructMap<TriggerSourceRecord>>();

        // Dictionary of property triggers that have TriggerActions, keyed via DP.GlobalIndex affecting those triggers.
        //  Each trigger can be listed multiple times, if they are dependent on multiple properties.
        internal FrugalMap PropertyTriggersWithActions;

        //
        // Shared tables used during OnStyleInvalidated/OnTemplateInvalidated/InvalidateTree
        //

        // Properties driven on the container (by the Style) that should be
        // invalidated when the style gets applied/unapplied. These properties
        // could have been set via Style.SetValue or VisualTrigger.SetValue
        internal FrugalStructList<ContainerDependent> ContainerDependents = new FrugalStructList<ContainerDependent>();

        // Properties driven by a resource that should be invalidated
        // when a resource dictionary changes or when the tree changes
        // or when a Style is Invalidated
        internal FrugalStructList<ChildPropertyDependent> ResourceDependents = new FrugalStructList<ChildPropertyDependent>();

        // Data trigger information.  An entry for each Binding that appears in a
        // condition of a data trigger.
        // Synchronized: Covered by Style instance
        internal HybridDictionary _dataTriggerRecordFromBinding;

        // An entry for each Binding that appears in a DataTrigger with EnterAction or ExitAction
        //  This overlaps but should not be the same as _dataTriggerRecordFromBinding above:
        //   A DataTrigger can have Setters but no EnterAction/ExitAction.  (The reverse can also be true.)
        internal HybridDictionary DataTriggersWithActions = null;

        // It is possible for trigger events to occur before template expansion has
        //  taken place.  In these cases, we cannot resolve the appropriate target name
        //  at that time.  We defer invoking these actions until template expansion
        //  is complete.
        // The key to the dictionary are the individual object instances that this
        //  template applies to.  (We might be holding deferred actions for multiple
        //  objects.)
        // The value of the dictionary is the trigger object and one of its action
        //  lists stored in the struct DeferredAction.
        internal ConditionalWeakTable<DependencyObject,List<DeferredAction>> DeferredActions = null;


        // Keep track of the template children elements for which the template has a Loaded
        // or Unloaded listener.

        internal class TemplateChildLoadedFlags
        {
            public bool HasLoadedChangedHandler;
            public bool HasUnloadedChangedHandler;
        }

        internal HybridDictionary _TemplateChildLoadedDictionary = new HybridDictionary();


        //
        // Shared tables used during Event Routing
        //

        // Events driven by a this style. An entry for every childIndex that has associated events.
        // childIndex '0' is used to represent events set ont he style's TargetType. This data-structure
        // will be frequently looked up during event routing.
        internal ItemStructList<ChildEventDependent> EventDependents = new ItemStructList<ChildEventDependent>(1);

        // Used to store a private delegate that called back during event
        // routing so we can process EventTriggers
        private EventHandlersStore _eventHandlersStore = null;

        // Prefetched values for StaticResources
        private object[] _staticResourceValues = null;

#if STYLE_TRACE
        // Time that is used only when style tracing is enabled
        private MS.Internal.Utility.HFTimer _timer = new MS.Internal.Utility.HFTimer();
#endif

#if DEBUG
        // Debug counter for intelligent breakpoints.
        private static int _globalDebugInstanceCounter = 0;
        private int        _debugInstanceCounter;
#endif

        [Flags]
        private enum InternalFlags : uint
        {
            //Sealed                          = 0x00000001,
            //HasInstanceValues               = 0x00000002,
            CanBuildVisualTree = 0x00000004,
            HasLoadedChangeHandler = 0x00000008,
            HasContainerResourceReferences = 0x00000010,
            HasChildResourceReferences = 0x00000020,
        }

        #endregion Data
    }

}

