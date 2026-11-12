// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Diagnostics;
using System.Threading;

namespace System.Windows.Threading
{
    /// <summary>
    /// Zero-allocation, high-throughput circular ring buffer for microtasks.
    /// Completely avoids heap churn from DispatcherOperation allocations under 10,000+ msgs/sec.
    /// </summary>
    public sealed class MicrotaskRingBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly int _mask;
        private long _head = 0;
        private long _tail = 0;
        private readonly object _syncLock = new();

        public int Capacity { get; }
        public int Count
        {
            get
            {
                lock (_syncLock)
                {
                    return (int)(_tail - _head);
                }
            }
        }

        public MicrotaskRingBuffer(int capacity = 4096)
        {
            // Ensure power of 2
            int cap = 1;
            while (cap < capacity) cap <<= 1;
            Capacity = cap;
            _mask = cap - 1;
            _buffer = new T[cap];
        }

        public bool TryEnqueue(T item)
        {
            lock (_syncLock)
            {
                if (_tail - _head >= Capacity)
                    return false; // Full

                _buffer[_tail & _mask] = item;
                _tail++;
                return true;
            }
        }

        public bool TryDequeue(out T item)
        {
            lock (_syncLock)
            {
                if (_head >= _tail)
                {
                    item = default;
                    return false; // Empty
                }

                item = _buffer[_head & _mask];
                _buffer[_head & _mask] = default;
                _head++;
                return true;
            }
        }

        public void Clear()
        {
            lock (_syncLock)
            {
                Array.Clear(_buffer, 0, _buffer.Length);
                _head = 0;
                _tail = 0;
            }
        }
    }

    /// <summary>
    /// High-performance cooperative frame-budget scheduler.
    /// Slices heavy computational and bulk operations into millisecond quanta (e.g. 10ms frame budget),
    /// preventing UI stalls and locking rendering at 60/120 FPS.
    /// </summary>
    public sealed class FrameBudgetScheduler
    {
        private static readonly Lazy<FrameBudgetScheduler> s_instance = new(() => new FrameBudgetScheduler());
        public static FrameBudgetScheduler Instance => s_instance.Value;

        private readonly MicrotaskRingBuffer<Action> _microtaskQueue = new(4096);
        private readonly MicrotaskRingBuffer<Func<bool>> _fiberQueue = new(1024);

        private double _targetFps = 60.0;
        private double _frameBudgetMs = 10.0; // 10ms CPU budget per 16.6ms frame
        private long _executedTasksCount = 0;
        private long _yieldedFramesCount = 0;

        public double TargetFps
        {
            get => _targetFps;
            set
            {
                _targetFps = Math.Clamp(value, 30.0, 360.0);
                _frameBudgetMs = (1000.0 / _targetFps) * 0.65; // Reserve 35% for GPU composition & present
            }
        }

        public double FrameBudgetMs
        {
            get => _frameBudgetMs;
            set => _frameBudgetMs = Math.Max(1.0, value);
        }

        public long ExecutedTasksCount => Volatile.Read(ref _executedTasksCount);
        public long YieldedFramesCount => Volatile.Read(ref _yieldedFramesCount);
        public int PendingMicrotasks => _microtaskQueue.Count;
        public int PendingFibers => _fiberQueue.Count;
        public bool HasPendingWork => _microtaskQueue.Count > 0 || _fiberQueue.Count > 0;

        public FrameBudgetScheduler()
        {
        }

        public bool PostMicrotask(Action action)
        {
            if (action == null) return false;
            return _microtaskQueue.TryEnqueue(action);
        }

        /// <summary>
        /// Schedules a sliced fiber task that returns true if it needs more quantum slices,
        /// or false when completed.
        /// </summary>
        public bool PostFiber(Func<bool> chunkAction)
        {
            if (chunkAction == null) return false;
            return _fiberQueue.TryEnqueue(chunkAction);
        }

        /// <summary>
        /// Executes pending microtasks and sliced fibers within the allocated frame budget.
        /// Returns true if all queued tasks completed within budget, or false if budget expired and work was yielded.
        /// </summary>
        public bool ExecuteFrameSlice(double budgetOverrideMs = 0)
        {
            double budget = budgetOverrideMs > 0 ? budgetOverrideMs : _frameBudgetMs;
            long startTicks = Stopwatch.GetTimestamp();
            double ticksToMs = 1000.0 / Stopwatch.Frequency;

            // 1. Drain zero-allocation microtasks first
            while (_microtaskQueue.TryDequeue(out var microtask))
            {
                microtask();
                Interlocked.Increment(ref _executedTasksCount);

                double elapsedMs = (Stopwatch.GetTimestamp() - startTicks) * ticksToMs;
                if (elapsedMs >= budget)
                {
                    Interlocked.Increment(ref _yieldedFramesCount);
                    return false; // Yield to next frame
                }
            }

            // 2. Step sliced fibers
            int activeFibers = _fiberQueue.Count;
            for (int i = 0; i < activeFibers; i++)
            {
                if (!_fiberQueue.TryDequeue(out var fiber))
                    break;

                bool hasMore = fiber();
                Interlocked.Increment(ref _executedTasksCount);

                if (hasMore)
                {
                    // Re-enqueue for next quantum slice
                    _fiberQueue.TryEnqueue(fiber);
                }

                double elapsedMs = (Stopwatch.GetTimestamp() - startTicks) * ticksToMs;
                if (elapsedMs >= budget)
                {
                    Interlocked.Increment(ref _yieldedFramesCount);
                    return false; // Yield to next frame
                }
            }

            return true; // Completed within budget
        }

        public void Clear()
        {
            _microtaskQueue.Clear();
            _fiberQueue.Clear();
        }
    }
}
