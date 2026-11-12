// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace System.Windows.Controls.Primitives
{
    public delegate void DragStartedEventHandler(object sender, DragStartedEventArgs e);
    public delegate void DragDeltaEventHandler(object sender, DragDeltaEventArgs e);
    public delegate void DragCompletedEventHandler(object sender, DragCompletedEventArgs e);

    public class DragStartedEventArgs : RoutedEventArgs
    {
        public double HorizontalOffset { get; }
        public double VerticalOffset { get; }

        public DragStartedEventArgs(double horizontalOffset, double verticalOffset)
        {
            HorizontalOffset = horizontalOffset;
            VerticalOffset = verticalOffset;
            RoutedEvent = Thumb.DragStartedEvent;
        }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler is DragStartedEventHandler handler)
            {
                handler(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }
    }

    public class DragDeltaEventArgs : RoutedEventArgs
    {
        public double HorizontalChange { get; }
        public double VerticalChange { get; }

        public DragDeltaEventArgs(double horizontalChange, double verticalChange)
        {
            HorizontalChange = horizontalChange;
            VerticalChange = verticalChange;
            RoutedEvent = Thumb.DragDeltaEvent;
        }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler is DragDeltaEventHandler handler)
            {
                handler(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }
    }

    public class DragCompletedEventArgs : RoutedEventArgs
    {
        public double HorizontalChange { get; }
        public double VerticalChange { get; }
        public bool Canceled { get; }

        public DragCompletedEventArgs(double horizontalChange, double verticalChange, bool canceled)
        {
            HorizontalChange = horizontalChange;
            VerticalChange = verticalChange;
            Canceled = canceled;
            RoutedEvent = Thumb.DragCompletedEvent;
        }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler is DragCompletedEventHandler handler)
            {
                handler(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }
    }

    /// <summary>
    /// Represents a control that can be dragged by the user, such as the slider in a ScrollBar.
    /// </summary>
    public partial class Thumb : Control
    {
        static partial void RegisterDefaultTheme();

        static Thumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Thumb), new FrameworkPropertyMetadata(typeof(Thumb)));
            RegisterDefaultTheme();
        }
        private static readonly DependencyPropertyKey IsDraggingPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsDragging),
                typeof(bool),
                typeof(Thumb),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsDraggingProperty =
            IsDraggingPropertyKey.DependencyProperty;

        public static readonly RoutedEvent DragStartedEvent =
            EventManager.RegisterRoutedEvent(nameof(DragStarted), RoutingStrategy.Bubble, typeof(DragStartedEventHandler), typeof(Thumb));

        public static readonly RoutedEvent DragDeltaEvent =
            EventManager.RegisterRoutedEvent(nameof(DragDelta), RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(Thumb));

        public static readonly RoutedEvent DragCompletedEvent =
            EventManager.RegisterRoutedEvent(nameof(DragCompleted), RoutingStrategy.Bubble, typeof(DragCompletedEventHandler), typeof(Thumb));

        private Point _originThumbPoint;
        private Point _originScreenCoordPosition;
        private Point _previousScreenCoordPosition;

        public bool IsDragging
        {
            get => (bool)GetValue(IsDraggingProperty);
            protected set => SetValue(IsDraggingPropertyKey, value);
        }

        public event DragStartedEventHandler DragStarted
        {
            add => AddHandler(DragStartedEvent, value);
            remove => RemoveHandler(DragStartedEvent, value);
        }

        public event DragDeltaEventHandler DragDelta
        {
            add => AddHandler(DragDeltaEvent, value);
            remove => RemoveHandler(DragDeltaEvent, value);
        }

        public event DragCompletedEventHandler DragCompleted
        {
            add => AddHandler(DragCompletedEvent, value);
            remove => RemoveHandler(DragCompletedEvent, value);
        }

        public Thumb()
        {
            Focusable = false;
        }

        public void CancelDrag()
        {
            if (IsDragging)
            {
                if (IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }
                EndDrag(true);
            }
        }

        private Point GetDragPosition(MouseEventArgs e)
        {
            return PresentationSource.CriticalFromVisual(this) != null
                ? PointToScreen(e.GetPosition(this))
                : e.GetPosition(null);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!IsDragging)
            {
                e.Handled = true;
                Focus();
                CaptureMouse();
                IsDragging = true;
                _originThumbPoint = e.GetPosition(this);
                _originScreenCoordPosition = GetDragPosition(e);
                _previousScreenCoordPosition = _originScreenCoordPosition;

                try
                {
                    RaiseEvent(new DragStartedEventArgs(_originThumbPoint.X, _originThumbPoint.Y));
                }
                catch
                {
                    CancelDrag();
                    throw;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsDragging)
            {
                if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
                {
                    Point currentPosition = GetDragPosition(e);
                    double horizontalChange = currentPosition.X - _previousScreenCoordPosition.X;
                    double verticalChange = currentPosition.Y - _previousScreenCoordPosition.Y;

                    if (horizontalChange != 0 || verticalChange != 0)
                    {
                        _previousScreenCoordPosition = currentPosition;
                        RaiseEvent(new DragDeltaEventArgs(horizontalChange, verticalChange));
                    }
                }
                else
                {
                    if (IsMouseCaptured)
                    {
                        ReleaseMouseCapture();
                    }
                    EndDrag(false);
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (IsMouseCaptured && IsDragging)
            {
                e.Handled = true;
                ReleaseMouseCapture();
                EndDrag(false);
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            if (IsDragging)
            {
                EndDrag(true);
            }
        }

        private void EndDrag(bool canceled)
        {
            IsDragging = false;
            RaiseEvent(new DragCompletedEventArgs(
                _previousScreenCoordPosition.X - _originScreenCoordPosition.X,
                _previousScreenCoordPosition.Y - _originScreenCoordPosition.Y,
                canceled));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (Template != null)
            {
                base.OnRender(drawingContext);
                return;
            }
            // Fluent modern pill thumb rendering
            double w = RenderSize.Width;
            double h = RenderSize.Height;
            if (w <= 0 || h <= 0) return;

            Color thumbColor;
            if (IsDragging)
            {
                thumbColor = Color.FromArgb(220, 110, 110, 115); // Pressed deep slate
            }
            else if (IsMouseOver)
            {
                thumbColor = Color.FromArgb(200, 140, 140, 145); // Hover
            }
            else
            {
                thumbColor = Color.FromArgb(140, 160, 160, 165); // Normal subtle
            }

            var brush = new SolidColorBrush(thumbColor);
            double radius = Math.Min(w, h) * 0.5;
            drawingContext.DrawRoundedRectangle(brush, null, new Rect(0, 0, w, h), radius, radius);
        }
    }
}
