// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Describes the type of change that occurred when a user scrolled.
    /// </summary>
    public enum ScrollEventType
    {
        EndScroll = 0,
        First = 1,
        LargeDecrement = 2,
        LargeIncrement = 3,
        Last = 4,
        SmallDecrement = 5,
        SmallIncrement = 6,
        ThumbPosition = 7,
        ThumbTrack = 8
    }

    /// <summary>
    /// Represents the method that handles a Scroll event on a ScrollBar.
    /// </summary>
    public delegate void ScrollEventHandler(object sender, ScrollEventArgs e);

    /// <summary>
    /// Provides data for a Scroll event that occurs when the user moves the Thumb of a ScrollBar.
    /// </summary>
    public class ScrollEventArgs : RoutedEventArgs
    {
        public ScrollEventType ScrollEventType { get; }
        public double NewValue { get; }

        public ScrollEventArgs(ScrollEventType scrollEventType, double newValue)
        {
            ScrollEventType = scrollEventType;
            NewValue = newValue;
            RoutedEvent = ScrollBar.ScrollEvent;
        }
    }
}
