using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using static CSUI.Tests.TestHelpers;

namespace CSUI.Tests;

internal static class WindowsBaseTests
{
    public static void RunAll()
    {
        TestDispatcherPrioritiesAndFrame();
        TestDependencyPropertyEngine();
        TestFreezableLifecycle();
        TestWeakEventAndObservableCollection();
    }

    // WB-1: Dispatcher Priority Ordering, Nested DispatcherFrame, Cross-Thread Invoke & DispatcherTimer
    private static void TestDispatcherPrioritiesAndFrame()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        var order = new List<string>();

        dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => order.Add("Idle")));
        dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => order.Add("Background")));
        dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => order.Add("Loaded")));
        dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => order.Add("Render")));
        dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => order.Add("Normal")));
        dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => order.Add("Send")));

        // Flush down to ApplicationIdle
        dispatcher.Invoke(DispatcherPriority.ApplicationIdle, static () => { });

        AssertEqual(string.Join(",", order), "Send,Normal,Render,Loaded,Background,Idle", "Dispatcher priority order");

        // Nested DispatcherFrame + DispatcherTimer
        int timerTicks = 0;
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(5),
            DispatcherPriority.Normal,
            (s, _) =>
            {
                timerTicks++;
                if (timerTicks >= 2)
                {
                    ((DispatcherTimer)s!).Stop();
                    frame.Continue = false;
                }
            },
            dispatcher);
        timer.Start();
        Dispatcher.PushFrame(frame);
        Assert(timerTicks >= 2, $"Expected >= 2 timer ticks, got {timerTicks}");

        // Cross-thread Invoke onto main Dispatcher
        int crossThreadResult = 0;
        var bg = new Thread(() =>
        {
            crossThreadResult = dispatcher.Invoke(() => 42 + 58);
        });
        bg.IsBackground = true;
        bg.Start();
        while (bg.IsAlive)
        {
            dispatcher.Invoke(DispatcherPriority.Background, static () => { });
            Thread.Sleep(5);
        }
        AssertEqual(crossThreadResult, 100, "Cross-thread Dispatcher.Invoke result");

        Pass("WB-1. Dispatcher priorities, nested DispatcherFrame, DispatcherTimer & cross-thread Invoke",
            $"order={string.Join(">", order)}, timerTicks={timerTicks}");
    }

    private class SampleDo : FrameworkElement
    {
        public int ChangedCount;
        public int LastValue;

        public static readonly DependencyProperty ScoreProperty = DependencyProperty.Register(
            nameof(Score),
            typeof(int),
            typeof(SampleDo),
            new FrameworkPropertyMetadata(
                10,
                FrameworkPropertyMetadataOptions.None,
                (d, e) =>
                {
                    var s = (SampleDo)d;
                    s.ChangedCount++;
                    s.LastValue = (int)e.NewValue;
                },
                (d, baseVal) => Math.Clamp((int)baseVal, 0, 100)),
            val => (int)val >= -1000);

        public int Score
        {
            get => (int)GetValue(ScoreProperty);
            set => SetValue(ScoreProperty, value);
        }

        public static readonly DependencyProperty InheritedTagProperty = DependencyProperty.RegisterAttached(
            "InheritedTag",
            typeof(string),
            typeof(SampleDo),
            new FrameworkPropertyMetadata("DefaultRoot", FrameworkPropertyMetadataOptions.Inherits));

        private static readonly DependencyPropertyKey ReadOnlyCounterKey = DependencyProperty.RegisterReadOnly(
            "ReadOnlyCounter",
            typeof(int),
            typeof(SampleDo),
            new PropertyMetadata(7));

        public static readonly DependencyProperty ReadOnlyCounterProperty = ReadOnlyCounterKey.DependencyProperty;

        public void SetCounterInternal(int val) => SetValue(ReadOnlyCounterKey, val);

        // Auto-generated AOT default value check for 3D & layout value types
        public static readonly DependencyProperty AutoPoint3DProperty = DependencyProperty.Register(
            "AutoPoint3D", typeof(Point3D), typeof(SampleDo));

        public static readonly DependencyProperty AutoQuaternionProperty = DependencyProperty.Register(
            "AutoQuaternion", typeof(Quaternion), typeof(SampleDo));

        public static readonly DependencyProperty AutoThicknessProperty = DependencyProperty.Register(
            "AutoThickness", typeof(Thickness), typeof(SampleDo));

        public static readonly DependencyProperty AutoGridLengthProperty = DependencyProperty.Register(
            "AutoGridLength", typeof(GridLength), typeof(SampleDo));

        private readonly List<SampleDo> _children = new();

        public void AddChild(SampleDo child)
        {
            _children.Add(child);
            AddVisualChild(child);
            AddLogicalChild(child);
        }

        protected override int VisualChildrenCount => _children.Count;
        protected override Visual GetVisualChild(int index) => _children[index];
        protected internal override System.Collections.IEnumerator LogicalChildren => _children.GetEnumerator();
    }

    private sealed class DerivedSampleDo : SampleDo
    {
        static DerivedSampleDo()
        {
            ScoreProperty.OverrideMetadata(typeof(DerivedSampleDo), new FrameworkPropertyMetadata(25));
        }
    }

    // WB-2: DependencyObject & DependencyProperty Engine
    private static void TestDependencyPropertyEngine()
    {
        var obj = new SampleDo();
        AssertEqual(obj.Score, 10, "Default DP value");
        AssertEqual(obj.ReadLocalValue(SampleDo.ScoreProperty), DependencyProperty.UnsetValue, "Unset local value");

        // Coerce upper bound
        obj.Score = 250;
        AssertEqual(obj.Score, 100, "Coerced upper bound");
        AssertEqual(obj.ChangedCount, 1, "PropertyChangedCallback count");

        // Coerce lower bound
        obj.Score = -50;
        AssertEqual(obj.Score, 0, "Coerced lower bound");

        // ClearValue restores default
        obj.ClearValue(SampleDo.ScoreProperty);
        AssertEqual(obj.Score, 10, "Restored default after ClearValue");

        // ValidateValueCallback rejects < -1000
        AssertThrows<ArgumentException>(() => obj.Score = -5000, "ValidateValueCallback rejection");

        // ReadOnly DP
        AssertEqual((int)obj.GetValue(SampleDo.ReadOnlyCounterProperty), 7, "ReadOnly DP initial");
        AssertThrows<InvalidOperationException>(
            () => obj.SetValue(SampleDo.ReadOnlyCounterProperty, 99),
            "Setting ReadOnly DP via public DependencyProperty must throw");
        obj.SetCounterInternal(42);
        AssertEqual((int)obj.GetValue(SampleDo.ReadOnlyCounterProperty), 42, "ReadOnly DP updated via key");

        // OverrideMetadata on derived class
        var derived = new DerivedSampleDo();
        AssertEqual(derived.Score, 25, "OverrideMetadata default value on derived type");

        // Property inheritance across parent -> child
        var parent = new SampleDo();
        var child = new SampleDo();
        parent.AddChild(child);
        AssertEqual((string)child.GetValue(SampleDo.InheritedTagProperty), "DefaultRoot", "Initial inherited value");
        parent.SetValue(SampleDo.InheritedTagProperty, "PropagatedToChild");
        AssertEqual((string)child.GetValue(SampleDo.InheritedTagProperty), "PropagatedToChild", "Propagated inherited value");

        // Auto-generated AOT struct defaults
        AssertEqual((Point3D)obj.GetValue(SampleDo.AutoPoint3DProperty), default(Point3D), "Auto Point3D default");
        AssertEqual((Quaternion)obj.GetValue(SampleDo.AutoQuaternionProperty), default(Quaternion), "Auto Quaternion default");
        AssertEqual((Thickness)obj.GetValue(SampleDo.AutoThicknessProperty), default(Thickness), "Auto Thickness default");
        AssertEqual((GridLength)obj.GetValue(SampleDo.AutoGridLengthProperty), default(GridLength), "Auto GridLength default");

        Pass("WB-2. DependencyProperty engine (Coerce, Validate, Inherit, OverrideMetadata, ReadOnly, AOT defaults)",
            $"score={obj.Score}, derivedDefault={derived.Score}, inherited={child.GetValue(SampleDo.InheritedTagProperty)}");
    }

    // WB-3: Freezable Lifecycle (Clone, Sub-Property Notification, Deep Freeze, Post-Freeze Guard)
    private static void TestFreezableLifecycle()
    {
        var brush = new LinearGradientBrush(Colors.Red, Colors.Blue, 45.0);
        int changeNotifications = 0;
        brush.Changed += (_, _) => changeNotifications++;

        brush.GradientStops[0].Color = Colors.Green;
        Assert(changeNotifications >= 1, "Sub-Freezable mutation should notify parent Freezable");

        var clone = (LinearGradientBrush)brush.Clone();
        Assert(!clone.IsFrozen, "Clone of unfrozen Freezable must be unfrozen");
        AssertEqual(clone.GradientStops[0].Color, Colors.Green, "Cloned GradientStop color");

        Assert(brush.CanFreeze, "LinearGradientBrush should be freezable");
        brush.Freeze();
        Assert(brush.IsFrozen, "Brush IsFrozen");
        Assert(brush.GradientStops.IsFrozen && brush.GradientStops[0].IsFrozen, "Deep Freeze propagation to children");

        AssertThrows<InvalidOperationException>(
            () => brush.Opacity = 0.5,
            "Mutating frozen brush must throw InvalidOperationException");
        AssertThrows<InvalidOperationException>(
            () => brush.GradientStops[0].Offset = 0.5,
            "Mutating frozen child GradientStop must throw InvalidOperationException");

        // Clone of frozen Freezable is unfrozen and mutable
        var mutableCopy = (LinearGradientBrush)brush.Clone();
        Assert(!mutableCopy.IsFrozen, "Clone() of frozen Freezable must be unfrozen");
        mutableCopy.Opacity = 0.75;
        AssertNear(mutableCopy.Opacity, 0.75, 1e-6, "Mutable clone opacity");

        Pass("WB-3. Freezable deep Freeze, sub-property Changed propagation & Clone",
            $"notifications={changeNotifications}, frozen={brush.IsFrozen}");
    }

    private sealed class NotifyModel : INotifyPropertyChanged
    {
        private string _title = "Init";
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // WB-4: PropertyChangedEventManager / CollectionChangedEventManager WeakEvent delivery
    private static void TestWeakEventAndObservableCollection()
    {
        var model = new NotifyModel();
        string? observedProp = null;
        EventHandler<PropertyChangedEventArgs> propHandler = (_, e) => observedProp = e.PropertyName;
        PropertyChangedEventManager.AddHandler(model, propHandler, nameof(NotifyModel.Title));

        model.Title = "Updated";
        AssertEqual(observedProp, nameof(NotifyModel.Title), "PropertyChangedEventManager received Title update");

        PropertyChangedEventManager.RemoveHandler(model, propHandler, nameof(NotifyModel.Title));
        observedProp = null;
        model.Title = "AfterRemove";
        AssertEqual(observedProp, null, "PropertyChangedEventManager after RemoveHandler");

        var list = new ObservableCollection<int> { 1, 2 };
        int addEvents = 0;
        EventHandler<NotifyCollectionChangedEventArgs> colHandler = (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                addEvents++;
        };
        CollectionChangedEventManager.AddHandler(list, colHandler);
        list.Add(3);
        list.Add(4);
        AssertEqual(addEvents, 2, "CollectionChangedEventManager Add count");
        CollectionChangedEventManager.RemoveHandler(list, colHandler);

        Pass("WB-4. WeakEventManager (PropertyChangedEventManager & CollectionChangedEventManager)",
            $"addEvents={addEvents}");
    }
}
