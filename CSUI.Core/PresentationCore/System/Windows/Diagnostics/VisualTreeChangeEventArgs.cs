// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Diagnostics
{
    public enum VisualTreeChangeType
    {
        Add,
        Remove
    }

    public class VisualTreeChangeEventArgs : EventArgs
    {
        public DependencyObject Parent { get; private set; }
        public DependencyObject Child { get; private set; }
        public int ChildIndex { get; private set; }
        public VisualTreeChangeType ChangeType { get; private set; }

        public VisualTreeChangeEventArgs(
            DependencyObject parent,
            DependencyObject child,
            int childIndex,
            VisualTreeChangeType changeType)
        {
            Parent = parent;
            Child = child;
            ChildIndex = childIndex;
            ChangeType = changeType;
        }
    }
}
