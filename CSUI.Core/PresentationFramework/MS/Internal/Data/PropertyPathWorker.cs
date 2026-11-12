// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: Defines PropertyPathWorker object, workhorse for CLR bindings
//

using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;                      // Validation
using System.Windows.Data;
using System.Windows.Markup;

#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace MS.Internal.Data
{
    internal sealed class PropertyPathWorker : IWeakEventListener
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal PropertyPathWorker(PropertyPath path)
            : this(path, DataBindEngine.CurrentDataBindEngine)
        {
        }

        internal PropertyPathWorker(PropertyPath path, ClrBindingWorker host, bool isDynamic, DataBindEngine engine)
            : this(path, engine)
        {
            _host = host;
            _isDynamic = isDynamic;
        }

        private PropertyPathWorker(PropertyPath path, DataBindEngine engine)
        {
            _parent = path;
            _arySVS = new SourceValueState[path.Length];
            _engine = engine;

            // initialize each level to NullDataItem, so that the first real
            // item will force a change
            for (int i = _arySVS.Length - 1; i >= 0; --i)
            {
                _arySVS[i].item = BindingExpression.CreateReference(BindingExpression.NullDataItem);
            }
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        internal int Length { get { return _parent.Length; } }
        internal PropertyPathStatus Status { get { return _status; } }

        internal DependencyObject TreeContext
        {
            get { return BindingExpression.GetReference(_treeContext) as DependencyObject; }
            set { _treeContext = BindingExpression.CreateReference(value); }
        }

        internal void SetTreeContext(WeakReference wr)
        {
            _treeContext = BindingExpression.CreateReference(wr);
        }

        internal bool IsDBNullValidForUpdate
        {
            get
            {
                if (!_isDBNullValidForUpdate.HasValue)
                {
                    DetermineWhetherDBNullIsValid();
                }

                return _isDBNullValidForUpdate.Value;
            }
        }

        internal object SourceItem
        {
            get
            {
                int level = Length - 1;
                object item = (level >= 0) ? GetItem(level) : null;
                if (item == BindingExpression.NullDataItem)
                {
                    item = null;
                }

                return item;
            }
        }

        internal string SourcePropertyName
        {
            get
            {
                int level = Length - 1;

                if (level < 0)
                    return null;

                switch (SVI[level].type)
                {
                    case SourceValueType.Property:
                        // return the real name of the property
                        DependencyProperty dp;
                        IPropertyAccessor pa;

                        SetPropertyInfo(GetAccessor(level), out dp, out pa);
                        return (dp != null) ? dp.Name : pa?.PropertyName;

                    case SourceValueType.Indexer:
                        // return the indexer string, e.g. "[foo]"
                        string s = _parent.Path;
                        int lastBracketIndex = s.LastIndexOf('[');
                        return s.Substring(lastBracketIndex);
                }

                // in all other cases, no name is available
                return null;
            }
        }

        // true when we need to register for direct notification from the RawValue,
        // i.e. when it's a DO that we get to via a non-DP
        internal bool NeedsDirectNotification
        {
            get { return _needsDirectNotification; }
            private set
            {
                if (value)
                {
                    _dependencySourcesChanged = true;
                }
                _needsDirectNotification = value;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        //-------  common methods ------

        internal object GetItem(int level)
        {
            return BindingExpression.GetReference(_arySVS[level].item);
        }

        internal object GetAccessor(int level)
        {
            return _arySVS[level].info;
        }

        internal object[] GetIndexerArguments(int level)
        {
            object[] args = _arySVS[level].args;

            // unwrap the IList wrapper, if any
            IListIndexerArg wrapper;
            if (args != null && args.Length == 1 &&
                (wrapper = args[0] as IListIndexerArg) != null)
            {
                return new object[] { wrapper.Value };
            }

            return args;
        }

        internal Type GetType(int level)
        {
            return _arySVS[level].type;
        }

        //-------  target mode ------

        // Set the context for the path.  Use this method in "target" mode
        // to connect the path to a rootItem for a short time:
        //      using (path.SetContext(myItem))
        //      {
        //          ... call target-mode convenience methods ...
        //      }
        internal IDisposable SetContext(object rootItem)
        {
            if (_contextHelper == null)
                _contextHelper = new ContextHelper(this);

            _contextHelper.SetContext(rootItem);
            return _contextHelper;
        }

        //-------  source mode (should only be called by ClrBindingWorker) ------

        internal void AttachToRootItem(object rootItem)
        {
            _rootItem = BindingExpression.CreateReference(rootItem);
            UpdateSourceValueState(-1, null);
        }

        internal void DetachFromRootItem()
        {
            _rootItem = BindingExpression.NullDataItem;
            UpdateSourceValueState(-1, null);
            _rootItem = null;
        }

        internal object GetValue(object item, int level)
        {
            bool isExtendedTraceEnabled = IsExtendedTraceEnabled(TraceDataLevel.GetValue);
            DependencyProperty dp;
            IPropertyAccessor pa;
            object value = DependencyProperty.UnsetValue;
            SetPropertyInfo(_arySVS[level].info, out dp, out pa);

            switch (SVI[level].type)
            {
                case SourceValueType.Property:
                    if (pa != null)
                    {
                        value = pa.GetValue(item);
                    }
                    else if (dp != null)
                    {
                        DependencyObject d = (DependencyObject)item;
                        if (level != Length - 1 || _host == null || _host.TransfersDefaultValue)
                            value = d.GetValue(dp);
                        else if (!Helper.HasDefaultValue(d, dp))
                            value = d.GetValue(dp);
                        else
                            value = BindingExpression.IgnoreDefaultValue;
                    }
                    break;

                case SourceValueType.Indexer:
                    if (_arySVS[level].info is IIndexerAccessor ia)
                    {
                        value = ia.GetValue(item, _arySVS[level].args);
                    }
                    else
                    {
                        throw new NotSupportedException(SR.IndexedPropDescNotImplemented);
                    }
                    break;

                case SourceValueType.Direct:
                    value = item;
                    break;
            }

            if (isExtendedTraceEnabled)
            {
                object accessor = _arySVS[level].info;
                if (accessor == DependencyProperty.UnsetValue)
                    accessor = null;

                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GetValue(
                                        TraceData.Identify(_host.ParentBindingExpression),
                                        level,
                                        TraceData.Identify(item),
                                        TraceData.IdentifyAccessor(accessor),
                                        TraceData.Identify(value)),
                                    _host.ParentBindingExpression);
            }

            return value;
        }

        internal void SetValue(object item, object value)
        {
            bool isExtendedTraceEnabled = IsExtendedTraceEnabled(TraceDataLevel.GetValue);
            DependencyProperty dp;
            IPropertyAccessor pa;
            int level = _arySVS.Length - 1;
            SetPropertyInfo(_arySVS[level].info, out dp, out pa);

            if (isExtendedTraceEnabled)
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.SetValue(
                                        TraceData.Identify(_host.ParentBindingExpression),
                                        level,
                                        TraceData.Identify(item),
                                        TraceData.IdentifyAccessor(_arySVS[level].info),
                                        TraceData.Identify(value)),
                                    _host.ParentBindingExpression);
            }

            switch (SVI[level].type)
            {
                case SourceValueType.Property:
                    if (pa != null)
                    {
                        pa.SetValue(item, value);
                    }
                    else if (dp != null)
                    {
                        ((DependencyObject)item).SetValue(dp, value);
                    }
                    break;

                case SourceValueType.Indexer:
                    if (_arySVS[level].info is IIndexerAccessor ia)
                    {
                        ia.SetValue(item, _arySVS[level].args, value);
                    }
                    else
                    {
                        throw new NotSupportedException(SR.IndexedPropDescNotImplemented);
                    }
                    break;
            }
        }

        internal object RawValue()
        {
            object rawValue = RawValue(Length - 1);

            if (rawValue == AsyncRequestPending)
                rawValue = DependencyProperty.UnsetValue;     // the real value will arrive later

            return rawValue;
        }

        // Called by BE.UpdateTarget().  Re-fetch the value at each level.
        // If there's a difference, simulate a property-change at that level.
        internal void RefreshValue()
        {
            for (int k = 1; k < _arySVS.Length; ++k)
            {
                object oldValue = BindingExpression.GetReference(_arySVS[k].item);
                if (!ItemsControl.EqualsEx(oldValue, RawValue(k - 1)))
                {
                    UpdateSourceValueState(k - 1, null);
                    return;
                }
            }

            UpdateSourceValueState(Length - 1, null);
        }

        // return the source level where the change happened, or -1 if the
        // change is irrelevant.
        internal int LevelForPropertyChange(object item, string propertyName)
        {
            // This test must be thread-safe - it can get called on the "wrong" context.
            // It's read-only (good).  And if another thread changes the values it reads,
            // the worst that can happen is to schedule a transfer operation needlessly -
            // the operation itself won't do anything (since the test is repeated on the
            // right thread).

            bool isIndexer = propertyName == Binding.IndexerName;

            for (int k = 0; k < _arySVS.Length; ++k)
            {
                object o = BindingExpression.GetReference(_arySVS[k].item);
                if (o == BindingExpression.StaticSource)
                    o = null;

                if (o == item &&
                        (String.IsNullOrEmpty(propertyName) ||
                         (isIndexer && SVI[k].type == MS.Internal.Data.SourceValueType.Indexer) ||
                         String.Equals(SVI[k].propertyName, propertyName, StringComparison.OrdinalIgnoreCase)))
                {
                    return k;
                }
            }

            return -1;
        }

        internal void OnPropertyChangedAtLevel(int level)
        {
            UpdateSourceValueState(level, null);
        }

        internal void OnCurrentChanged(ICollectionView collectionView)
        {
            for (int k = 0; k < Length; ++k)
            {
                if (_arySVS[k].collectionView == collectionView)
                {
                    _host.CancelPendingTasks();

                    // update everything below that level
                    UpdateSourceValueState(k, collectionView);
                    break;
                }
            }
        }

        // determine if a source invalidation is relevant.
        // This method must be thread-safe - it can be called on any Dispatcher.
        internal bool UsesDependencyProperty(DependencyObject d, DependencyProperty dp)
        {
            if (dp == DependencyObject.DirectDependencyProperty)
            {
                // the only way we get notified about this property is when we
                // ask for it.
                return true;
            }

            // find the source level where the change happened
            for (int k = 0; k < _arySVS.Length; ++k)
            {
                if ((_arySVS[k].info == dp) && (BindingExpression.GetReference(_arySVS[k].item) == d))
                {
                    return true;
                }
            }

            return false;
        }

        internal void OnDependencyPropertyChanged(DependencyObject d, DependencyProperty dp, bool isASubPropertyChange)
        {
            if (dp == DependencyObject.DirectDependencyProperty)
            {
                // the only way we get notified about this property is when the raw
                // value reports a subProperty change.
                UpdateSourceValueState(_arySVS.Length, null, BindingExpression.NullDataItem, isASubPropertyChange);
                return;
            }

            // find the source level where the change happened
            int k;
            for (k = 0; k < _arySVS.Length; ++k)
            {
                if ((_arySVS[k].info == dp) && (BindingExpression.GetReference(_arySVS[k].item) == d))
                {
                    // update everything below that level
                    UpdateSourceValueState(k, null, BindingExpression.NullDataItem, isASubPropertyChange);
                    break;
                }
            }
        }

        internal void OnNewValue(int level, object value)
        {
            // optimistically assume the new value will fix previous path errors
            _status = PropertyPathStatus.Active;
            if (level < Length - 1)
                UpdateSourceValueState(level, null, value, false);
        }

        // for error reporting only
        internal SourceValueInfo GetSourceValueInfo(int level)
        {
            return SVI[level];
        }


        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        private bool IsDynamic { get { return _isDynamic; } }

        private SourceValueInfo[] SVI { get { return _parent.SVI; } }

        private DataBindEngine Engine { get { return _engine; } }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        // fill in the SourceValueState with updated infomation, starting at level k+1.
        // If view isn't null, also update the current item at level k.
        private void UpdateSourceValueState(int k, ICollectionView collectionView)
        {
            UpdateSourceValueState(k, collectionView, BindingExpression.NullDataItem, false);
        }

        // fill in the SourceValueState with updated infomation, starting at level k+1.
        // If view isn't null, also update the current item at level k.
        private void UpdateSourceValueState(int k, ICollectionView collectionView, object newValue, bool isASubPropertyChange)
        {
            // give host a chance to shut down the binding if the target has
            // gone away
            DependencyObject target = null;
            if (_host != null)
            {
                target = _host.CheckTarget();
                if (_rootItem != BindingExpression.NullDataItem && target == null)
                    return;
            }

            int initialLevel = k;
            object rawValue = null;

            // don't do a data transfer if there's no one to do it, or if this is
            // a (re-)activation with a new source item.   In the latter case,
            // the host binding decides whether to do a transfer.
            bool suppressTransfer = (_host == null) || (k < 0);

            // optimistically assume the new value will fix previous path errors
            _status = PropertyPathStatus.Active;

            // prepare to collect changes to dependency sources
            _dependencySourcesChanged = false;

            // Update the current item at level k, if requested
            if (collectionView != null)
            {
                Debug.Assert(0 <= k && k < _arySVS.Length && _arySVS[k].collectionView == collectionView, "bad parameters to UpdateSourceValueState");
                ReplaceItem(k, collectionView.CurrentItem, NoParent);
            }

            // update the remaining levels
            for (++k; k < _arySVS.Length; ++k)
            {
                isASubPropertyChange = false;   // sub-property changes only matter at the last level

                ICollectionView oldCollectionView = _arySVS[k].collectionView;

                // replace the item at level k using parent from level k-1
                rawValue = (newValue == BindingExpression.NullDataItem) ? RawValue(k - 1) : newValue;
                newValue = BindingExpression.NullDataItem;
                if (rawValue == AsyncRequestPending)
                {
                    _status = PropertyPathStatus.AsyncRequestPending;
                    break;      // we'll resume the loop after the request completes
                }
                else if (!suppressTransfer &&
                         rawValue == BindingExpressionBase.DisconnectedItem &&
                         _arySVS[k - 1].info == FrameworkElement.DataContextProperty)
                {
                    // don't transfer if {DisconnectedItem} shows up on the path
                    suppressTransfer = true;
                }

                ReplaceItem(k, BindingExpression.NullDataItem, rawValue);

                // replace view, if necessary
                ICollectionView newCollectionView = _arySVS[k].collectionView;
                if (oldCollectionView != newCollectionView && _host != null)
                {
                    _host.ReplaceCurrentItem(oldCollectionView, newCollectionView);
                }
            }

            // notify binding about what happened
            if (_host != null)
            {
                if (initialLevel < _arySVS.Length)
                {
                    // when something in the path changes, recompute whether we
                    // need direct notifications from the raw value
                    NeedsDirectNotification = _status == PropertyPathStatus.Active &&
                            _arySVS.Length > 0 &&
                            SVI[_arySVS.Length - 1].type != SourceValueType.Direct &&
                            !(_arySVS[_arySVS.Length - 1].info is DependencyProperty) &&
                            typeof(DependencyObject).IsAssignableFrom(_arySVS[_arySVS.Length - 1].type);
                }

                if (!suppressTransfer && _arySVS.Length > 0 &&
                    _arySVS[_arySVS.Length - 1].info == FrameworkElement.DataContextProperty &&
                    RawValue() == BindingExpressionBase.DisconnectedItem)
                {
                    // don't transfer if {DisconnectedItem} is the final value
                    suppressTransfer = true;
                }

                _host.NewValueAvailable(_dependencySourcesChanged, suppressTransfer, isASubPropertyChange);
            }

            GC.KeepAlive(target);   // keep target alive during changes (bug 956831)
        }

        // replace the item at level k with the given item, or with an item obtained from the given parent
        private void ReplaceItem(int k, object newO, object parent)
        {
            bool isExtendedTraceEnabled = IsExtendedTraceEnabled(TraceDataLevel.ReplaceItem);
            SourceValueState svs = new SourceValueState();

            object oldO = BindingExpression.GetReference(_arySVS[k].item);

            // stop listening to old item
            if (IsDynamic && SVI[k].type != SourceValueType.Direct)
            {
                INotifyPropertyChanged oldPC;
                DependencyProperty oldDP;
                IPropertyAccessor oldPA;
                PropertyPath.DowncastAccessor(_arySVS[k].info, out oldDP, out oldPA);

                if (oldO == BindingExpression.StaticSource)
                {
                    if (oldPA != null)
                    {
                        StaticPropertyChangedEventManager.RemoveHandler(oldPA.SourceType, OnStaticPropertyChanged, SVI[k].propertyName);
                    }
                }
                else if (oldDP != null)
                {
                    _dependencySourcesChanged = true;
                }
                else if ((oldPC = oldO as INotifyPropertyChanged) != null)
                {
                    PropertyChangedEventManager.RemoveHandler(oldPC, OnPropertyChanged, SVI[k].propertyName);
                }
            }

            // extra work at the last level
            if (_host != null && k == Length - 1)
            {
                // handle INotifyDataErrorInfo
                if (IsDynamic && _host.ValidatesOnNotifyDataErrors)
                {
                    INotifyDataErrorInfo indei = oldO as INotifyDataErrorInfo;
                    if (indei != null)
                    {
                        ErrorsChangedEventManager.RemoveHandler(indei, OnErrorsChanged);
                    }
                }
            }

            // clear the IsDBNullValid cache
            _isDBNullValidForUpdate = null;

            if (newO == null ||
                parent == DependencyProperty.UnsetValue ||
                parent == BindingExpression.NullDataItem ||
                parent == BindingExpressionBase.DisconnectedItem)
            {
                _arySVS[k].item = BindingExpression.ReplaceReference(_arySVS[k].item, newO);

                if (parent == DependencyProperty.UnsetValue ||
                    parent == BindingExpression.NullDataItem ||
                    parent == BindingExpressionBase.DisconnectedItem)
                {
                    _arySVS[k].collectionView = null;
                }

                if (isExtendedTraceEnabled)
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.ReplaceItemShort(
                                            TraceData.Identify(_host.ParentBindingExpression),
                                            k,
                                            TraceData.Identify(newO)),
                                        _host.ParentBindingExpression);
                }

                return;
            }

            // obtain the new item and its access info
            if (newO != BindingExpression.NullDataItem)
            {
                parent = newO;              // used by error reporting
                GetInfo(k, newO, ref svs);
                svs.collectionView = _arySVS[k].collectionView;
            }
            else
            {
                // Note: if we want to support binding to HasValue and/or Value
                // properties of nullable types, we need a way to find out if
                // the rawvalue is Nullable and pass that information here.

                DrillIn drillIn = SVI[k].drillIn;
                ICollectionView view = null;

                // first look for info on the parent
                if (drillIn != DrillIn.Always)
                {
                    GetInfo(k, parent, ref svs);
                }

                // if that fails, look for information on the view itself
                if (svs.info == null)
                {
                    view = CollectionViewSource.GetDefaultCollectionView(parent, TreeContext,
                        (x) =>
                        {
                            return BindingExpression.GetReference((k == 0) ? _rootItem : _arySVS[k - 1].item);
                        });

                    if (view != null && drillIn != DrillIn.Always)
                    {
                        if (view != parent)             // don't duplicate work
                            GetInfo(k, view, ref svs);
                    }
                }

                // if that fails, drill in to the current item
                if (svs.info == null && drillIn != DrillIn.Never && view != null)
                {
                    newO = view.CurrentItem;
                    if (newO != null)
                    {
                        GetInfo(k, newO, ref svs);
                        svs.collectionView = view;
                    }
                    else
                    {
                        // no current item: use previous info (if known)
                        svs = _arySVS[k];
                        svs.collectionView = view;

                        // currency is simply off the collection
                        svs.item = BindingExpression.ReplaceReference(svs.item, BindingExpression.NullDataItem);
                        if (svs.info == null)
                            svs.info = DependencyProperty.UnsetValue;
                    }
                }
            }

            // update info about new item
            if (svs.info == null)
            {
                svs.item = BindingExpression.ReplaceReference(svs.item, BindingExpression.NullDataItem);
                _arySVS[k] = svs;
                _status = PropertyPathStatus.PathError;
                ReportNoInfoError(k, parent);
                return;
            }

            _arySVS[k] = svs;
            newO = BindingExpression.GetReference(svs.item);

            if (isExtendedTraceEnabled)
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.ReplaceItemLong(
                                        TraceData.Identify(_host.ParentBindingExpression),
                                        k,
                                        TraceData.Identify(newO),
                                        TraceData.IdentifyAccessor(svs.info)),
                                    _host.ParentBindingExpression);
            }

            // start listening to new item
            if (IsDynamic && SVI[k].type != SourceValueType.Direct)
            {
                INotifyPropertyChanged newPC;
                DependencyProperty newDP;
                IPropertyAccessor newPA;
                PropertyPath.DowncastAccessor(svs.info, out newDP, out newPA);

                if (newO == BindingExpression.StaticSource)
                {
                    Type declaringType = newPA?.SourceType;
                    if (declaringType != null)
                    {
                        StaticPropertyChangedEventManager.AddHandler(declaringType, OnStaticPropertyChanged, SVI[k].propertyName);
                    }
                }
                else if (newDP != null)
                {
                    _dependencySourcesChanged = true;
                }
                else if ((newPC = newO as INotifyPropertyChanged) != null)
                {
                    PropertyChangedEventManager.AddHandler(newPC, OnPropertyChanged, SVI[k].propertyName);
                }
            }

            // extra work at the last level
            if (_host != null && k == Length - 1)
            {
                // set up the default transformer
                _host.SetupDefaultValueConverter(svs.type);

                // check for request to update a read-only property
                if (_host.IsReflective)
                {
                    CheckReadOnly(newO, svs.info);
                }

                // handle INotifyDataErrorInfo
                if (_host.ValidatesOnNotifyDataErrors)
                {
                    INotifyDataErrorInfo indei = newO as INotifyDataErrorInfo;
                    if (indei != null)
                    {
                        if (IsDynamic)
                        {
                            ErrorsChangedEventManager.AddHandler(indei, OnErrorsChanged);
                        }

                        _host.OnDataErrorsChanged(indei, SourcePropertyName);
                    }
                }
            }
        }

        private void ReportNoInfoError(int k, object parent)
        {
            // report cannot find info.  Ignore when in priority bindings.
            if (TraceData.IsEnabled)
            {
                BindingExpression bindingExpression = _host?.ParentBindingExpression;
                if (bindingExpression == null || !bindingExpression.IsInPriorityBindingExpression)
                {
                    SourceValueInfo svi = SVI[k];
                    bool inCollection = (svi.drillIn == DrillIn.Always);
                    string cs = (svi.type != SourceValueType.Indexer) ? svi.name : $"[{svi.name}]";
                    string ps = TraceData.DescribeSourceObject(parent);
                    string os = inCollection ? "current item of collection" : "object";

                    // if the parent is null, the path error probably only means the
                    // data provider hasn't produced any data yet.  When it does,
                    // the binding will try again and probably succeed.  Give milder
                    // feedback for this special case, so as not to alarm users unduly.
                    if (parent == null)
                    {
                        TraceData.TraceAndNotify(TraceEventType.Information, TraceData.NullItem(cs, os), bindingExpression);
                    }
                    // Similarly, if the parent is the NewItemPlaceholder.
                    else if (parent == CollectionView.NewItemPlaceholder)
                    {
                        TraceData.TraceAndNotify(TraceEventType.Information, TraceData.PlaceholderItem(cs, os), bindingExpression);
                    }
                    else
                    {
                        TraceEventType traceType = (bindingExpression != null) ? bindingExpression.TraceLevel : TraceEventType.Error;
                        TraceData.TraceAndNotify(traceType, TraceData.ClrReplaceItem(cs, ps, os), bindingExpression,
                            traceParameters: new object[] { bindingExpression },
                            eventParameters: new object[] { cs, parent, inCollection });
                    }
                }
            }
        }

        // determine if the cached state of the path is still correct.  This is
        // used to deduce whether event leapfrogging has occurred along the path
        // (i.e. something changed, but we haven't yet received the notification)
        internal bool IsPathCurrent(object rootItem)
        {
            if (Status != PropertyPathStatus.Active)
                return false;

            object item = rootItem;
            for (int level = 0, n = Length; level < n; ++level)
            {
                ICollectionView view = _arySVS[level].collectionView;
                if (view != null)
                {
                    item = view.CurrentItem;
                }

                if (PropertyPath.IsStaticProperty(_arySVS[level].info))
                {
                    // for static properties, we set svs.item to StaticSource
                    // at discovery time.  Do the same here.
                    item = BindingExpression.StaticSource;
                }

                if (!ItemsControl.EqualsEx(item, BindingExpression.GetReference(_arySVS[level].item))
                    && !IsNonIdempotentProperty(level - 1))
                {
                    return false;
                }

                if (level < n - 1)
                {
                    item = GetValue(item, level);
                }
            }

            return true;
        }

        // Certain properties are known to be non-idempotent, i.e. they return a
        // different value every time the getter is called.   For the purpose of
        // detecting event leapfrogging, the value produced by such a property
        // should be ignored.
        private bool IsNonIdempotentProperty(int level)
        {
            return false;
        }

        // look for property/indexer on the given item
        private void GetInfo(int k, object item, ref SourceValueState svs)
        {
#if DEBUG
            bool checkCacheResult = false;
#endif
            object oldItem = BindingExpression.GetReference(_arySVS[k].item);
            bool isExtendedTraceEnabled = IsExtendedTraceEnabled(TraceDataLevel.GetInfo);

            // optimization - only change info if the type changed
            Type oldType = oldItem?.GetType();
            Type newType = item?.GetType();
            Type sourceType = null;

            if (newType == oldType && oldItem != BindingExpression.NullDataItem)
            {
                svs = _arySVS[k];
                svs.item = BindingExpression.ReplaceReference(svs.item, item);

                if (isExtendedTraceEnabled)
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.GetInfo_Reuse(
                                            TraceData.Identify(_host.ParentBindingExpression),
                                            k,
                                            TraceData.IdentifyAccessor(svs.info)),
                                        _host.ParentBindingExpression);
                }
                return;
            }

            // if the new item is null, we won't find a property/indexer on it
            if (newType == null && SVI[k].type != SourceValueType.Direct)
            {
                svs.info = null;
                svs.args = null;
                svs.type = null;
                svs.item = BindingExpression.ReplaceReference(svs.item, item);

                if (isExtendedTraceEnabled)
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.GetInfo_Null(
                                            TraceData.Identify(_host.ParentBindingExpression),
                                            k),
                                        _host.ParentBindingExpression);
                }
                return;
            }

            // optimization - see if we've cached the answer
            int index;
            bool cacheAccessor = !PropertyPath.IsParameterIndex(SVI[k].name, out index);
            if (cacheAccessor)
            {
                AccessorInfo accessorInfo = Engine.AccessorTable[SVI[k].type, newType, SVI[k].name];
                if (accessorInfo != null)
                {
                    svs.info = accessorInfo.Accessor;
                    svs.type = accessorInfo.PropertyType;
                    svs.args = accessorInfo.Args;

                    if (PropertyPath.IsStaticProperty(svs.info))
                        item = BindingExpression.StaticSource;

                    svs.item = BindingExpression.ReplaceReference(svs.item, item);

                    if (IsDynamic && SVI[k].type == SourceValueType.Property && svs.info is DependencyProperty)
                    {
                        _dependencySourcesChanged = true;
                    }

                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.GetInfo_Cache(
                                                TraceData.Identify(_host.ParentBindingExpression),
                                                k,
                                                newType.Name,
                                                SVI[k].name,
                                                TraceData.IdentifyAccessor(svs.info)),
                                            _host.ParentBindingExpression);
                    }

#if DEBUG   // compute the answer the old-fashioned way, and compare
                    checkCacheResult = true;
#else
                    return;
#endif
                }
            }

            object info = null;
            object[] args = null;

            switch (SVI[k].type)
            {
                case SourceValueType.Property:
                    info = _parent.ResolvePropertyName(k, item, newType, TreeContext);

                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.GetInfo_Property(
                                                TraceData.Identify(_host.ParentBindingExpression),
                                                k,
                                                newType.Name,
                                                SVI[k].name,
                                                TraceData.IdentifyAccessor(info)),
                                            _host.ParentBindingExpression);
                    }

                    DependencyProperty dp;
                    IPropertyAccessor pa;
                    PropertyPath.DowncastAccessor(info, out dp, out pa);

                    if (dp != null)
                    {
                        sourceType = dp.PropertyType;
                        if (IsDynamic)
                        {
#if DEBUG
                        if (checkCacheResult)
                            Debug.Assert(_dependencySourcesChanged, "Cached accessor didn't change sources");
#endif
                            _dependencySourcesChanged = true;
                        }
                        break;
                    }
                    else if (pa != null)
                    {
                        sourceType = pa.PropertyType;
                    }
                    break;

                case SourceValueType.Indexer:
                    IndexerParameterInfo[] aryInfo = _parent.ResolveIndexerParams(k, TreeContext);
                    args = new object[aryInfo.Length];

                    string indexerName = (k > 0 && _arySVS[k - 1].info == (object)IndexerPropertyInfo.Instance)
                        ? SVI[k - 1].name
                        : null;
                    IIndexerAccessor ia = PropertyAccessorRegistry.FindIndexer(newType, indexerName);
                    if (ia != null && ia.TryMatchArgs(aryInfo, args))
                    {
                        info = ia;
                        sourceType = newType.IsArray ? newType.GetElementType() : ia.ElementType;
                    }

                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.GetInfo_Indexer(
                                                TraceData.Identify(_host.ParentBindingExpression),
                                                k,
                                                newType.Name,
                                                SVI[k].name,
                                                TraceData.IdentifyAccessor(info)),
                                            _host.ParentBindingExpression);
                    }

                    break;

                case SourceValueType.Direct:
                    if (!(item is ICollectionView) || _host == null || _host.IsValidValue(item))
                    {
                        info = DependencyProperty.UnsetValue;
                        sourceType = newType;

                        if (Length == 1 &&
                                item is Freezable &&    // subproperty notifications only arise from Freezables
                                item != TreeContext)    // avoid self-loops
                        {
                            info = DependencyObject.DirectDependencyProperty;
                            _dependencySourcesChanged = true;
                        }
                    }
                    break;
            }

#if DEBUG
            if (checkCacheResult)
            {
                StringBuilder sb = new StringBuilder();

                if (!Object.Equals(info, svs.info))
                    sb.AppendLine($"  Info is wrong: expected '{info}' got '{svs.info}'");

                if (sourceType != svs.type)
                    sb.AppendLine($"  Type is wrong: expected '{sourceType}' got '{svs.type}'");

                if (item != BindingExpression.GetReference(svs.item))
                    sb.AppendLine($"  Item is wrong: expected '{item}' got '{BindingExpression.GetReference(svs.item)}'");

                int len1 = (args != null) ? args.Length : 0;
                int len2 = (svs.args != null) ? svs.args.Length : 0;
                if (len1 == len2)
                {
                    for (int i=0; i<len1; ++i)
                    {
                        if (!Object.Equals(args[i], svs.args[i]))
                        {
                            sb.AppendLine($"  args[{i}] is wrong:  expected '{args[i]}' got '{svs.args[i]}'");
                        }
                    }
                }
                else
                    sb.AppendLine($"  Args are wrong: expected length '{len1}' got length '{len2}'");

                Debug.Assert(sb.Length == 0,
                    $"""
                     Accessor cache returned incorrect result for ({SVI[k].type},{newType.Name},{SVI[k].name})
                     {sb.ToString()}
                     """);

                return;
            }
#endif
            if (PropertyPath.IsStaticProperty(info))
                item = BindingExpression.StaticSource;

            svs.info = info;
            svs.args = args;
            svs.type = sourceType;
            svs.item = BindingExpression.ReplaceReference(svs.item, item);

            // cache the accessor answer
            if (cacheAccessor && info != null)
            {
                Engine.AccessorTable[SVI[k].type, newType, SVI[k].name] =
                            new AccessorInfo(info, sourceType, args);
            }
        }

        // return the raw value from level k
        private object RawValue(int k)
        {
            if (k < 0)
                return BindingExpression.GetReference(_rootItem);
            if (k >= _arySVS.Length)
                return DependencyProperty.UnsetValue;

            object item = BindingExpression.GetReference(_arySVS[k].item);
            object info = _arySVS[k].info;

            // try to get the value, unless (a) binding is being detached,
            // (b) no info - e.g. Nullable with no value, or (c) item expected
            // but not present - e.g. currency moved off the end.
            if (item != BindingExpression.NullDataItem && info != null && !(item == null && info != DependencyProperty.UnsetValue))
            {
                object o = DependencyProperty.UnsetValue;
                DependencyProperty dp = info as DependencyProperty;

                // if the binding is async, post a request to get the value
                if (!(dp != null || SVI[k].type == SourceValueType.Direct))
                {
                    if (_host != null && _host.AsyncGet(item, k))
                    {
                        _status = PropertyPathStatus.AsyncRequestPending;
                        return AsyncRequestPending;
                    }
                }

                try
                {
                    o = GetValue(item, k);
                }
                // Catch all exceptions.  There is no app code on the stack,
                // so the exception isn't actionable by the app.
                // Yet we don't want to crash the app.
                catch (Exception ex)    // if error getting value, we will use fallback/default instead
                {
                    if (CriticalExceptions.IsCriticalApplicationException(ex))
                        throw;
                    BindingOperations.LogException(ex);
                    _host?.ReportGetValueError(k, item, ex);
                }

                // catch the pseudo-exception as well
                if (o == IListIndexOutOfRange)
                {
                    o = DependencyProperty.UnsetValue;
                    _host?.ReportGetValueError(k, item, new ArgumentOutOfRangeException("index"));
                }

                return o;
            }

            _host?.ReportRawValueErrors(k, item, info);

            return DependencyProperty.UnsetValue;
        }

        private void SetPropertyInfo(object info, out DependencyProperty dp, out IPropertyAccessor pa)
        {
            PropertyPath.DowncastAccessor(info, out dp, out pa);
        }

        private void CheckReadOnly(object item, object info)
        {
            DependencyProperty dp;
            IPropertyAccessor pa;
            SetPropertyInfo(info, out dp, out pa);

            if (pa != null)
            {
                if (pa.IsReadOnly)
                    throw new InvalidOperationException(SR.Format(SR.CannotWriteToReadOnly, item.GetType(), pa.PropertyName));
            }
            else if (dp != null)
            {
                if (dp.ReadOnly)
                    throw new InvalidOperationException(SR.Format(SR.CannotWriteToReadOnly, item.GetType(), dp.Name));
            }
            else if (info is IIndexerAccessor ia)
            {
                if (ia.IsReadOnly)
                    throw new InvalidOperationException(SR.Format(SR.CannotWriteToReadOnly, item.GetType(), ia.IndexerName));
            }
        }

        // see whether DBNull is a valid value for update, and cache the answer
        private void DetermineWhetherDBNullIsValid()
        {
            _isDBNullValidForUpdate = false;
        }

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return false;   // this method is no longer used (but must remain, for compat)
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsExtendedTraceEnabled(TraceDataLevel.Events))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GotEvent(
                                        TraceData.Identify(_host.ParentBindingExpression),
                                        "PropertyChanged",
                                        TraceData.Identify(sender)),
                                    _host.ParentBindingExpression);
            }

            _host.OnSourcePropertyChanged(sender, e.PropertyName);
        }

        private void OnErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            if (e.PropertyName == SourcePropertyName)
            {
                _host.OnDataErrorsChanged((INotifyDataErrorInfo)sender, e.PropertyName);
            }
        }

        private void OnStaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsExtendedTraceEnabled(TraceDataLevel.Events))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GotEvent(
                                        TraceData.Identify(_host.ParentBindingExpression),
                                        "PropertyChanged",
                                        "(static)"),
                                    _host.ParentBindingExpression);
            }

            _host.OnSourcePropertyChanged(sender, e.PropertyName);
        }

        private bool IsExtendedTraceEnabled(TraceDataLevel level)
        {
            if (_host != null)
            {
                return TraceData.IsExtendedTraceEnabled(_host.ParentBindingExpression, level);
            }
            else
            {
                return false;
            }
        }

        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        // helper for setting context via the "using" pattern
        private class ContextHelper : IDisposable
        {
            private PropertyPathWorker _owner;

            public ContextHelper(PropertyPathWorker owner)
            {
                _owner = owner;
            }

            public void SetContext(object rootItem)
            {
                _owner.TreeContext = rootItem as DependencyObject;
                _owner.AttachToRootItem(rootItem);
            }

            void IDisposable.Dispose()
            {
                _owner.DetachFromRootItem();
                _owner.TreeContext = null;
                GC.SuppressFinalize(this);
            }
        }

        // wrapper for arguments to IList indexer
        internal sealed class IListIndexerArg
        {
            public IListIndexerArg(int arg)
            {
                _arg = arg;
            }

            public int Value { get { return _arg; } }

            private int _arg;
        }

        //------------------------------------------------------
        //
        //  Private Enums, Structs, Constants
        //
        //------------------------------------------------------

        private struct SourceValueState
        {
            public ICollectionView collectionView;
            public object item;
            public object info;             // DP, IPropertyAccessor, IIndexerAccessor, or IndexerPropertyInfo.Instance
            public Type type;               // Type of the value (useful for Arrays)
            public object[] args;           // for indexers
        }

        private static readonly Char[] s_comma = new Char[] { ',' };
        private static readonly Char[] s_dot = new Char[] { '.' };
        private static readonly object NoParent = new NamedObject("NoParent");
        private static readonly object AsyncRequestPending = new NamedObject("AsyncRequestPending");
        internal static readonly object IListIndexOutOfRange = new NamedObject("IListIndexOutOfRange");

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private PropertyPath _parent;
        private PropertyPathStatus _status;
        private object _treeContext;
        private object _rootItem;
        private SourceValueState[] _arySVS;
        private ContextHelper _contextHelper;
        private ClrBindingWorker _host;
        private DataBindEngine _engine;
        private bool _dependencySourcesChanged;
        private bool _isDynamic;
        private bool _needsDirectNotification;
        private bool? _isDBNullValidForUpdate;
    }
}

