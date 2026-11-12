// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;

namespace System.Windows.Media
{
    /// <summary>
    /// Listener contract for GPU device removal/reset events (DXGI_ERROR_DEVICE_REMOVED).
    /// </summary>
    public interface IDeviceLossListener
    {
        void OnDeviceLost();
        void OnDeviceRestored();
    }

    /// <summary>
    /// Represents a cached GPU texture entry with LRU timestamp and VRAM footprint tracking.
    /// </summary>
    public sealed unsafe class GpuTextureEntry : IDisposable
    {
        public string Key { get; }
        public void* D2DBitmap { get; internal set; }
        public int PixelWidth { get; }
        public int PixelHeight { get; }
        public long ByteSize { get; }
        public long LastAccessTimestamp { get; internal set; }
        public bool IsPinned { get; set; }
        public bool OwnsNativePointer { get; set; } = true;

        public GpuTextureEntry(string key, void* bitmap, int width, int height, int bytesPerPixel = 4, bool ownsNativePointer = true)
        {
            Key = key;
            D2DBitmap = bitmap;
            PixelWidth = width;
            PixelHeight = height;
            ByteSize = (long)width * height * bytesPerPixel;
            LastAccessTimestamp = Stopwatch.GetTimestamp();
            OwnsNativePointer = ownsNativePointer;
        }

        public void Dispose()
        {
            if (D2DBitmap != null)
            {
                if (OwnsNativePointer)
                {
                    ComHelpers.Release((void*)D2DBitmap);
                }
                D2DBitmap = null;
            }
        }
    }

    /// <summary>
    /// Global high-performance GPU VRAM resource manager and device loss coordinator.
    /// Enforces texture budget caps with LRU eviction and coordinates whole-system device loss recovery.
    /// </summary>
    public sealed class GpuResourceCache
    {
        private static readonly Lazy<GpuResourceCache> s_instance = new(() => new GpuResourceCache());
        public static GpuResourceCache Instance => s_instance.Value;

        private readonly ConcurrentDictionary<string, GpuTextureEntry> _entries = new(StringComparer.Ordinal);
        private readonly List<WeakReference<IDeviceLossListener>> _listeners = new();
        private readonly object _listenerLock = new();

        private long _currentVramUsageBytes = 0;
        private long _maxVramBudgetBytes = 256 * 1024 * 1024; // 256 MB default budget

        public long CurrentVramUsageBytes => Volatile.Read(ref _currentVramUsageBytes);
        public long MaxVramBudgetBytes
        {
            get => _maxVramBudgetBytes;
            set => _maxVramBudgetBytes = Math.Max(16 * 1024 * 1024, value);
        }

        public int EntryCount => _entries.Count;

        private GpuResourceCache()
        {
        }

        public unsafe void RegisterTexture(string key, void* bitmapPtr, int width, int height, bool isPinned = false, bool ownsNativePointer = true)
        {
            if (string.IsNullOrEmpty(key) || bitmapPtr == null) return;

            var newEntry = new GpuTextureEntry(key, bitmapPtr, width, height, ownsNativePointer: ownsNativePointer) { IsPinned = isPinned };

            if (_entries.TryGetValue(key, out var oldEntry))
            {
                Interlocked.Add(ref _currentVramUsageBytes, -oldEntry.ByteSize);
                oldEntry.Dispose();
            }

            _entries[key] = newEntry;
            long newTotal = Interlocked.Add(ref _currentVramUsageBytes, newEntry.ByteSize);

            if (newTotal > _maxVramBudgetBytes)
            {
                EvictLru();
            }
        }

        public unsafe void* TryGetTexture(string key)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                entry.LastAccessTimestamp = Stopwatch.GetTimestamp();
                return entry.D2DBitmap;
            }
            return null;
        }

        public bool RemoveTexture(string key)
        {
            if (_entries.TryRemove(key, out var entry))
            {
                Interlocked.Add(ref _currentVramUsageBytes, -entry.ByteSize);
                entry.Dispose();
                return true;
            }
            return false;
        }

        public void Clear()
        {
            foreach (var kvp in _entries)
            {
                kvp.Value.Dispose();
            }
            _entries.Clear();
            Volatile.Write(ref _currentVramUsageBytes, 0);
        }

        private void EvictLru()
        {
            // Collect unpinned candidates sorted by timestamp
            var candidates = new List<GpuTextureEntry>();
            foreach (var kvp in _entries)
            {
                if (!kvp.Value.IsPinned)
                {
                    candidates.Add(kvp.Value);
                }
            }

            candidates.Sort((a, b) => a.LastAccessTimestamp.CompareTo(b.LastAccessTimestamp));

            for (int i = 0; i < candidates.Count; i++)
            {
                if (CurrentVramUsageBytes <= _maxVramBudgetBytes * 0.8) break; // Evict down to 80% watermark
                RemoveTexture(candidates[i].Key);
            }
        }

        // --- Device Loss Recovery Coordinator ---

        public void RegisterDeviceLossListener(IDeviceLossListener listener)
        {
            if (listener == null) return;
            lock (_listenerLock)
            {
                _listeners.Add(new WeakReference<IDeviceLossListener>(listener));
            }
        }

        public void UnregisterDeviceLossListener(IDeviceLossListener listener)
        {
            if (listener == null) return;
            lock (_listenerLock)
            {
                for (int i = _listeners.Count - 1; i >= 0; i--)
                {
                    if (!_listeners[i].TryGetTarget(out var target) || target == listener)
                    {
                        _listeners.RemoveAt(i);
                    }
                }
            }
        }

        public void BroadcastDeviceLost()
        {
            // First release all cached GPU textures so driver can tear down D3D device cleanly
            Clear();

            List<IDeviceLossListener> targets = new();
            lock (_listenerLock)
            {
                for (int i = _listeners.Count - 1; i >= 0; i--)
                {
                    if (_listeners[i].TryGetTarget(out var listener))
                    {
                        targets.Add(listener);
                    }
                    else
                    {
                        _listeners.RemoveAt(i);
                    }
                }
            }

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].OnDeviceLost();
            }
        }

        public void BroadcastDeviceRestored()
        {
            List<IDeviceLossListener> targets = new();
            lock (_listenerLock)
            {
                for (int i = _listeners.Count - 1; i >= 0; i--)
                {
                    if (_listeners[i].TryGetTarget(out var listener))
                    {
                        targets.Add(listener);
                    }
                    else
                    {
                        _listeners.RemoveAt(i);
                    }
                }
            }

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].OnDeviceRestored();
            }
        }
    }
}
