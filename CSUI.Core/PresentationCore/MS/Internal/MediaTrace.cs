// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace MS.Internal
{
    internal class MediaTrace : IDisposable
    {
        public static readonly MediaTrace NodeFlag = new("Node Flags");
        public static readonly MediaTrace NodeOp = new("Node Operations");
        public static readonly MediaTrace NodeCreation = new("Node creation");
        public static readonly MediaTrace DrawingContextOp = new("Drawing Context Op");
        public static readonly MediaTrace ContainerOp = new("Drawing Visual Operations Operations");
        public static readonly MediaTrace Precompute = new("Precompute");
        public static readonly MediaTrace Render = new("Render");
        public static readonly MediaTrace RenderClipRectangle = new("Render red clip rectangle around Visual");
        public static readonly MediaTrace GraphWalker = new("GraphWalker");
        public static readonly MediaTrace DirtyRegion = new("DirtyRegion");
        public static readonly MediaTrace DirtyRegionStacks = new("DirtyRegionStacks");
        public static readonly MediaTrace RenderPass = new("RenderPass");
        public static readonly MediaTrace AnimationThread = new("AnimationThread");
        public static readonly MediaTrace ShowDirtyRegion = new("ShowDirtyRegion");
        public static readonly MediaTrace HwndTarget = new("HwndTarget");
        public static readonly MediaTrace QueueItems = new("MILQueueItems");
        public static readonly MediaTrace Statistics = new("Statistics");
        public static readonly MediaTrace ValidationQueue = new("Validate queue");

        public static class ChangeQueue
        {
            public static readonly MediaTrace ApplyChange = new("Change queue: Apply Change");
            public static readonly MediaTrace Enqueue = new("Change queue: Enqueue");
        }

        public MediaTrace(string switchName)
        {
        }

        public MediaTrace(string switchName, bool initialState)
        {
        }

        public void Trace(string message) { }
        public void TraceCallers(int depth) { }
        public void Indent() { }
        public void Unindent() { }
        public IDisposable Indented() => this;
        public IDisposable Indented(string message) => this;
        void IDisposable.Dispose() { }
        public void Enable() { }
        public void Disable() { }
        public bool IsEnabled => false;
    }
}
