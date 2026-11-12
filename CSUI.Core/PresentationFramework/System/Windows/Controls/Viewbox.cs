// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Defines a content decorator that can stretch and scale a single child to fill the available space.
    /// </summary>
    public class Viewbox : Decorator
    {
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(
                nameof(Stretch),
                typeof(Stretch),
                typeof(Viewbox),
                new FrameworkPropertyMetadata(
                    Stretch.Uniform,
                    FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty StretchDirectionProperty =
            DependencyProperty.Register(
                nameof(StretchDirection),
                typeof(StretchDirection),
                typeof(Viewbox),
                new FrameworkPropertyMetadata(
                    StretchDirection.Both,
                    FrameworkPropertyMetadataOptions.AffectsMeasure));

        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public StretchDirection StretchDirection
        {
            get => (StretchDirection)GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        public Viewbox()
        {
        }

        public static Size ComputeScaleFactor(Size availableSize, Size contentSize, Stretch stretch, StretchDirection stretchDirection)
        {
            double scaleX = 1.0;
            double scaleY = 1.0;

            bool isConstrainedW = !double.IsPositiveInfinity(availableSize.Width);
            bool isConstrainedH = !double.IsPositiveInfinity(availableSize.Height);

            if ((stretch == Stretch.Uniform || stretch == Stretch.UniformToFill || stretch == Stretch.Fill)
                && (isConstrainedW || isConstrainedH))
            {
                scaleX = contentSize.Width <= 0.0 ? 0.0 : availableSize.Width / contentSize.Width;
                scaleY = contentSize.Height <= 0.0 ? 0.0 : availableSize.Height / contentSize.Height;

                if (!isConstrainedW)
                {
                    scaleX = scaleY;
                }
                else if (!isConstrainedH)
                {
                    scaleY = scaleX;
                }
                else
                {
                    switch (stretch)
                    {
                        case Stretch.Uniform:
                            double minScale = Math.Min(scaleX, scaleY);
                            scaleX = scaleY = minScale;
                            break;
                        case Stretch.UniformToFill:
                            double maxScale = Math.Max(scaleX, scaleY);
                            scaleX = scaleY = maxScale;
                            break;
                        case Stretch.Fill:
                            break;
                    }
                }

                switch (stretchDirection)
                {
                    case StretchDirection.UpOnly:
                        if (scaleX < 1.0) scaleX = 1.0;
                        if (scaleY < 1.0) scaleY = 1.0;
                        break;
                    case StretchDirection.DownOnly:
                        if (scaleX > 1.0) scaleX = 1.0;
                        if (scaleY > 1.0) scaleY = 1.0;
                        break;
                    case StretchDirection.Both:
                        break;
                }
            }

            return new Size(scaleX, scaleY);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            UIElement child = Child;
            if (child != null)
            {
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Size childSize = child.DesiredSize;
                Size scale = ComputeScaleFactor(constraint, childSize, Stretch, StretchDirection);
                return new Size(childSize.Width * scale.Width, childSize.Height * scale.Height);
            }

            return new Size();
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            UIElement child = Child;
            if (child != null)
            {
                Size childSize = child.DesiredSize;
                Size scale = ComputeScaleFactor(arrangeSize, childSize, Stretch, StretchDirection);

                child.Arrange(new Rect(new Point(0, 0), childSize));
                child.RenderTransform = new ScaleTransform(scale.Width, scale.Height);
            }

            return arrangeSize;
        }
    }
}
