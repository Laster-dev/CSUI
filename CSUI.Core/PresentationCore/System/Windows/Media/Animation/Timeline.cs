// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Internal;
using System.Collections;
using System.ComponentModel;
using System.Windows.Markup;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Animation
{
    [RuntimeNameProperty("Name")]
    public abstract partial class Timeline : Animatable
    {
        #region Construction

        protected Timeline()
        {
#if DEBUG
            lock (_debugLockObject)
            {
                _debugIdentity = ++_nextIdentity;
                WeakReference weakRef = new WeakReference(this);
                _objectTable[_debugIdentity] = weakRef;
            }
#endif
        }

        protected Timeline(TimeSpan? beginTime)
            : this()
        {
            BeginTime = beginTime;
        }

        protected Timeline(TimeSpan? beginTime, Duration duration)
            : this()
        {
            BeginTime = beginTime;
            Duration = duration;
        }

        protected Timeline(TimeSpan? beginTime, Duration duration, RepeatBehavior repeatBehavior)
            : this()
        {
            BeginTime = beginTime;
            Duration = duration;
            RepeatBehavior = repeatBehavior;
        }

        #endregion

        #region Freezable

        protected override bool FreezeCore(bool isChecking)
        {
            ValidateTimeline();
            return base.FreezeCore(isChecking);
        }

        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            Timeline sourceTimeline = (Timeline)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);

            CopyCommon(sourceTimeline);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            Timeline sourceTimeline = (Timeline)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);

            CopyCommon(sourceTimeline);
        }

        #endregion

        #region Properties

        private static void Timeline_PropertyChangedFunction(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            ((Timeline)d).PropertyChanged(e.Property);
        }

        #region AccelerationRatio Property

        public static readonly DependencyProperty AccelerationRatioProperty =
            DependencyProperty.Register(
                "AccelerationRatio",
                typeof(double),
                typeof(Timeline),
                new PropertyMetadata(
                    (double)0.0,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)),
                new ValidateValueCallback(ValidateAccelerationDecelerationRatio));

        public double AccelerationRatio
        {
            get => (double)GetValue(AccelerationRatioProperty);
            set => SetValue(AccelerationRatioProperty, value);
        }

        private static bool ValidateAccelerationDecelerationRatio(object value)
        {
            double newValue = (double)value;

            if (newValue < 0 || newValue > 1 || double.IsNaN(newValue))
            {
                throw new ArgumentException(SR.Timing_InvalidArgAccelAndDecel, nameof(value));
            }

            return true;
        }

        #endregion

        #region AutoReverse Property

        public static readonly DependencyProperty AutoReverseProperty =
            DependencyProperty.Register(
                "AutoReverse",
                typeof(bool),
                typeof(Timeline),
                new PropertyMetadata(
                    false,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)));

        [DefaultValue(false)]
        public bool AutoReverse
        {
            get => (bool)GetValue(AutoReverseProperty);
            set => SetValue(AutoReverseProperty, value);
        }

        #endregion

        #region BeginTime Property

        public static readonly DependencyProperty BeginTimeProperty =
            DependencyProperty.Register(
                "BeginTime",
                typeof(TimeSpan?),
                typeof(Timeline),
                new PropertyMetadata(
                    (TimeSpan?)TimeSpan.Zero,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)));

        public TimeSpan? BeginTime
        {
            get => (TimeSpan?)GetValue(BeginTimeProperty);
            set => SetValue(BeginTimeProperty, value);
        }

        #endregion

        #region DecelerationRatio Property

        public static readonly DependencyProperty DecelerationRatioProperty =
            DependencyProperty.Register(
                "DecelerationRatio",
                typeof(double),
                typeof(Timeline),
                new PropertyMetadata(
                    (double)0.0,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)),
                new ValidateValueCallback(ValidateAccelerationDecelerationRatio));

        public double DecelerationRatio
        {
            get => (double)GetValue(DecelerationRatioProperty);
            set => SetValue(DecelerationRatioProperty, value);
        }

        #endregion

        #region DesiredFrameRate Property

        public static readonly DependencyProperty DesiredFrameRateProperty =
            DependencyProperty.RegisterAttached(
                "DesiredFrameRate",
                typeof(Int32?),
                typeof(Timeline),
                new PropertyMetadata(
                    (Int32?)null,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)),
                new ValidateValueCallback(ValidateDesiredFrameRate));

        private static bool ValidateDesiredFrameRate(object value)
        {
            Int32? desiredFrameRate = (Int32?)value;

            return (!desiredFrameRate.HasValue || desiredFrameRate.Value > 0);
        }

        public static Int32? GetDesiredFrameRate(Timeline timeline)
        {
            ArgumentNullException.ThrowIfNull(timeline);

            return (Int32?)timeline.GetValue(DesiredFrameRateProperty);
        }

        public static void SetDesiredFrameRate(Timeline timeline, Int32? desiredFrameRate)
        {
            ArgumentNullException.ThrowIfNull(timeline);

            timeline.SetValue(DesiredFrameRateProperty, desiredFrameRate);
        }

        #endregion

        #region Duration Property

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                "Duration",
                typeof(Duration),
                typeof(Timeline),
                new PropertyMetadata(
                    Duration.Automatic,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)));

        public Duration Duration
        {
            get => (Duration)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        #endregion

        #region FillBehavior Property

        public static readonly DependencyProperty FillBehaviorProperty =
            DependencyProperty.Register(
                "FillBehavior",
                typeof(FillBehavior),
                typeof(Timeline),
                new PropertyMetadata(
                    FillBehavior.HoldEnd,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)),
                new ValidateValueCallback(ValidateFillBehavior));

        private static bool ValidateFillBehavior(object value)
        {
            return TimeEnumHelper.IsValidFillBehavior((FillBehavior)value);
        }

        public FillBehavior FillBehavior
        {
            get => (FillBehavior)GetValue(FillBehaviorProperty);
            set => SetValue(FillBehaviorProperty, value);
        }

        #endregion

        #region Name Property

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(
                "Name",
                typeof(string),
                typeof(Timeline),
                new PropertyMetadata(
                    (string)null,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)),
                new ValidateValueCallback(NameValidationHelper.NameValidationCallback));

        [DefaultValue((string)null)]
        [MergableProperty(false)]
        public string Name
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        #endregion

        #region RepeatBehavior Property

        public static readonly DependencyProperty RepeatBehaviorProperty =
            DependencyProperty.Register(
                "RepeatBehavior",
                typeof(RepeatBehavior),
                typeof(Timeline),
                new PropertyMetadata(
                    new RepeatBehavior(1.0),
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)));

        public RepeatBehavior RepeatBehavior
        {
            get => (RepeatBehavior)GetValue(RepeatBehaviorProperty);
            set => SetValue(RepeatBehaviorProperty, value);
        }

        #endregion

        #region SpeedRatio Property

        public static readonly DependencyProperty SpeedRatioProperty =
            DependencyProperty.Register(
                "SpeedRatio",
                typeof(double),
                typeof(Timeline),
                new PropertyMetadata(
                    (double)1.0,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)),
                new ValidateValueCallback(ValidateSpeedRatio));

        [DefaultValue((double)1.0)]
        public double SpeedRatio
        {
            get => (double)GetValue(SpeedRatioProperty);
            set => SetValue(SpeedRatioProperty, value);
        }

        private static bool ValidateSpeedRatio(object value)
        {
            double newValue = (double)value;

            if (newValue <= 0 || newValue > double.MaxValue || double.IsNaN(newValue))
            {
                throw new ArgumentException(SR.Timing_InvalidArgFinitePositive, nameof(value));
            }

            return true;
        }

        #endregion

        #endregion

        #region Methods

        protected internal virtual Clock AllocateClock()
        {
            return new Clock(this);
        }

        public Clock CreateClock()
        {
            return CreateClock(true);
        }

        public Clock CreateClock(bool hasControllableRoot)
        {
            return Clock.BuildClockTreeFromTimeline(this, hasControllableRoot);
        }

        protected internal Duration GetNaturalDuration(Clock clock)
        {
            return GetNaturalDurationCore(clock);
        }

        protected virtual Duration GetNaturalDurationCore(Clock clock)
        {
            return Duration.Automatic;
        }

        private void ValidateTimeline()
        {
            if (AccelerationRatio + DecelerationRatio > 1)
            {
                throw new InvalidOperationException(SR.Timing_AccelAndDecelGreaterThanOne);
            }
        }

        #endregion

        #region Events

        public event EventHandler CurrentStateInvalidated
        {
            add => AddEventHandler(CurrentStateInvalidatedKey, value);
            remove => RemoveEventHandler(CurrentStateInvalidatedKey, value);
        }

        public event EventHandler CurrentTimeInvalidated
        {
            add => AddEventHandler(CurrentTimeInvalidatedKey, value);
            remove => RemoveEventHandler(CurrentTimeInvalidatedKey, value);
        }

        public event EventHandler CurrentGlobalSpeedInvalidated
        {
            add => AddEventHandler(CurrentGlobalSpeedInvalidatedKey, value);
            remove => RemoveEventHandler(CurrentGlobalSpeedInvalidatedKey, value);
        }

        public event EventHandler Completed
        {
            add => AddEventHandler(CompletedKey, value);
            remove => RemoveEventHandler(CompletedKey, value);
        }

        public event EventHandler RemoveRequested
        {
            add => AddEventHandler(RemoveRequestedKey, value);
            remove => RemoveEventHandler(RemoveRequestedKey, value);
        }

        #endregion

        #region Internal implementation

        internal EventHandlersStore InternalEventHandlersStore =>
            EventHandlersStoreField.GetValue(this);

        internal void InternalOnFreezablePropertyChanged(Timeline originalTimeline, Timeline newTimeline)
        {
            OnFreezablePropertyChanged(originalTimeline, newTimeline);
        }

        internal bool InternalFreeze(bool isChecking)
        {
            return Freeze(this, isChecking);
        }

        internal void InternalReadPreamble()
        {
            ReadPreamble();
        }

        internal void InternalWritePostscript()
        {
            WritePostscript();
        }

        private void AddEventHandler(EventPrivateKey key, Delegate handler)
        {
            WritePreamble();

            EventHandlersStore store = EventHandlersStoreField.GetValue(this);

            if (store == null)
            {
                store = new EventHandlersStore();
                EventHandlersStoreField.SetValue(this, store);
            }

            store.Add(key, handler);
            WritePostscript();
        }

        private void CopyCommon(Timeline sourceTimeline)
        {
            EventHandlersStore sourceStore = EventHandlersStoreField.GetValue(sourceTimeline);
            if (sourceStore != null)
            {
                Debug.Assert(sourceStore.Count > 0);
                EventHandlersStoreField.SetValue(this, new EventHandlersStore(sourceStore));
            }
        }

        private void RemoveEventHandler(EventPrivateKey key, Delegate handler)
        {
            WritePreamble();

            EventHandlersStore store = EventHandlersStoreField.GetValue(this);
            if (store != null)
            {
                store.Remove(key, handler);
                if (store.Count == 0)
                {
                    EventHandlersStoreField.ClearValue(this);
                }

                WritePostscript();
            }
        }

        #region Debugging instrumentation

#if DEBUG

        internal void Dump()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder
            {
                Capacity = 1024
            };
            builder.Append("========================================\n");
            builder.Append("Timelines rooted at Timeline ");
            builder.Append(_debugIdentity);
            builder.Append('\n');
            builder.Append("----------------------------------------\n");
            BuildInfoRecursive(builder, 0);
            builder.Append("----------------------------------------\n");
            Trace.Write(builder.ToString());
        }

        internal static void DumpAll()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int timelineCount = 0;
            builder.Capacity = 1024;
            builder.Append("========================================\n");
            builder.Append("Timelines in the GC heap\n");
            builder.Append("----------------------------------------\n");

            lock (_debugLockObject)
            {
                if (_objectTable.Count > 0)
                {
                    int[] idTable = new int[_objectTable.Count];
                    _objectTable.Keys.CopyTo(idTable, 0);
                    Array.Sort(idTable);

                    for (int index = 0; index < idTable.Length; index++)
                    {
                        WeakReference weakRef = (WeakReference)_objectTable[idTable[index]];
                        Timeline timeline = (Timeline)weakRef.Target;
                        if (timeline != null)
                        {
                            timeline.BuildInfo(builder, 0, true);
                            timelineCount++;
                        }
                    }
                }
            }

            if (timelineCount == 0)
            {
                builder.Append("There are no Timelines in the GC heap.\n");
            }

            builder.Append("----------------------------------------\n");
            Trace.Write(builder.ToString());
        }

        internal void BuildInfoRecursive(System.Text.StringBuilder builder, int depth)
        {
            BuildInfo(builder, depth, true);

            depth++;
            TimelineGroup timelineGroup = this as TimelineGroup;

            if (timelineGroup != null)
            {
                TimelineCollection children = timelineGroup.Children;
                if (children != null)
                {
                    for (int childIndex = 0; childIndex < children.Count; childIndex++)
                    {
                        children.Internal_GetItem(childIndex).BuildInfoRecursive(builder, depth);
                    }
                }
            }
        }

        internal void BuildInfo(System.Text.StringBuilder builder, int depth, bool includeDebugID)
        {
            if (includeDebugID)
            {
                builder.Append(' ', depth);
                builder.Append("Timeline ");
                builder.Append(_debugIdentity);
            }
            builder.Append(" (");
            builder.Append(GetType().Name);

            if (Name != null)
            {
                builder.Append(", Name=\"");
                builder.Append(Name);
                builder.Append('"');
            }
            if (AccelerationRatio != 0.0f)
            {
                builder.Append(", AccelerationRatio = ");
                builder.Append(AccelerationRatio);
            }
            if (AutoReverse)
            {
                builder.Append(", AutoReverse = ");
                builder.Append(AutoReverse);
            }
            if (DecelerationRatio != 0.0f)
            {
                builder.Append(", DecelerationRatio = ");
                builder.Append(DecelerationRatio);
            }
            if (Duration != Duration.Automatic)
            {
                builder.Append(", Duration = ");
                builder.Append(Duration.ToString());
            }
            if (FillBehavior != FillBehavior.HoldEnd)
            {
                builder.Append(", FillBehavior = ");
                builder.Append(FillBehavior.ToString());
            }
            if (SpeedRatio != 1.0f)
            {
                builder.Append(", Speed = ");
                builder.Append(SpeedRatio);
            }
            builder.Append(")\n");
        }

        internal static Timeline Find(int id)
        {
            Timeline timeline = null;

            lock (_debugLockObject)
            {
                object handleReference = _objectTable[id];
                if (handleReference != null)
                {
                    WeakReference weakRef = (WeakReference)handleReference;
                    timeline = (Timeline)weakRef.Target;
                    if (timeline == null)
                    {
                        _objectTable.Remove(id);
                    }
                }
            }

            return timeline;
        }

        internal static void CleanKnownTimelinesTable()
        {
            lock (_debugLockObject)
            {
                Hashtable removeTable = new Hashtable();

                foreach (DictionaryEntry e in _objectTable)
                {
                    WeakReference weakRef = (WeakReference)e.Value;
                    if (weakRef.Target == null)
                    {
                        removeTable[e.Key] = weakRef;
                    }
                }

                foreach (DictionaryEntry e in removeTable)
                {
                    _objectTable.Remove(e.Key);
                }
            }
        }

#endif

        #endregion

        #region Data

        internal static readonly UncommonField<EventHandlersStore> EventHandlersStoreField = new UncommonField<EventHandlersStore>();

        internal static readonly EventPrivateKey CurrentGlobalSpeedInvalidatedKey = new EventPrivateKey();
        internal static readonly EventPrivateKey CurrentStateInvalidatedKey = new EventPrivateKey();
        internal static readonly EventPrivateKey CurrentTimeInvalidatedKey = new EventPrivateKey();
        internal static readonly EventPrivateKey CompletedKey = new EventPrivateKey();
        internal static readonly EventPrivateKey RemoveRequestedKey = new EventPrivateKey();

#if DEBUG
        internal int _debugIdentity;

        internal static int _nextIdentity;
        internal static Hashtable _objectTable = new Hashtable();
        internal static object _debugLockObject = new object();
#endif

        #endregion

        #endregion
    }
}
