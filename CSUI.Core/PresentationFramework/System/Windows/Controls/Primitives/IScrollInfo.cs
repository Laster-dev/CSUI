// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows;
using System.Windows.Media;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Represents the main scrolling region inside a ScrollViewer control.
    /// Defines communication contract between a scrolling host (such as ScrollViewer)
    /// and a content element or virtualizing panel that supports logical/pixel scrolling.
    /// </summary>
    public interface IScrollInfo
    {
        /// <summary>
        /// Scrolls up within content by one logical unit.
        /// </summary>
        void LineUp();

        /// <summary>
        /// Scrolls down within content by one logical unit.
        /// </summary>
        void LineDown();

        /// <summary>
        /// Scrolls left within content by one logical unit.
        /// </summary>
        void LineLeft();

        /// <summary>
        /// Scrolls right within content by one logical unit.
        /// </summary>
        void LineRight();

        /// <summary>
        /// Scrolls up within content by one page.
        /// </summary>
        void PageUp();

        /// <summary>
        /// Scrolls down within content by one page.
        /// </summary>
        void PageDown();

        /// <summary>
        /// Scrolls left within content by one page.
        /// </summary>
        void PageLeft();

        /// <summary>
        /// Scrolls right within content by one page.
        /// </summary>
        void PageRight();

        /// <summary>
        /// Scrolls up within content after a user clicks the mouse wheel button up.
        /// </summary>
        void MouseWheelUp();

        /// <summary>
        /// Scrolls down within content after a user clicks the mouse wheel button down.
        /// </summary>
        void MouseWheelDown();

        /// <summary>
        /// Scrolls left within content after a user clicks the mouse wheel button left.
        /// </summary>
        void MouseWheelLeft();

        /// <summary>
        /// Scrolls right within content after a user clicks the mouse wheel button right.
        /// </summary>
        void MouseWheelRight();

        /// <summary>
        /// Sets the amount of horizontal offset.
        /// </summary>
        void SetHorizontalOffset(double offset);

        /// <summary>
        /// Sets the amount of vertical offset.
        /// </summary>
        void SetVerticalOffset(double offset);

        /// <summary>
        /// Forces content to scroll until the coordinate space of a Visual object is visible.
        /// </summary>
        Rect MakeVisible(Visual visual, Rect rectangle);

        /// <summary>
        /// Gets or sets a value indicating whether scrolling on the vertical axis is possible.
        /// </summary>
        bool CanVerticallyScroll { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether scrolling on the horizontal axis is possible.
        /// </summary>
        bool CanHorizontallyScroll { get; set; }

        /// <summary>
        /// Gets the horizontal size of the extent.
        /// </summary>
        double ExtentWidth { get; }

        /// <summary>
        /// Gets the vertical size of the extent.
        /// </summary>
        double ExtentHeight { get; }

        /// <summary>
        /// Gets the horizontal size of the viewport for this content.
        /// </summary>
        double ViewportWidth { get; }

        /// <summary>
        /// Gets the vertical size of the viewport for this content.
        /// </summary>
        double ViewportHeight { get; }

        /// <summary>
        /// Gets the horizontal offset of the scrolled content.
        /// </summary>
        double HorizontalOffset { get; }

        /// <summary>
        /// Gets the vertical offset of the scrolled content.
        /// </summary>
        double VerticalOffset { get; }

        /// <summary>
        /// Gets or sets a ScrollViewer element that controls scrolling behavior.
        /// </summary>
        ScrollViewer ScrollOwner { get; set; }
    }
}
