// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Draws a border, background, or both around another element with optional rounded corners.
    /// </summary>
    public class Border : Decorator
    {
        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(
                nameof(BorderThickness),
                typeof(Thickness),
                typeof(Border),
                new FrameworkPropertyMetadata(
                    new Thickness(),
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(
                nameof(Padding),
                typeof(Thickness),
                typeof(Border),
                new FrameworkPropertyMetadata(
                    new Thickness(),
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(Border),
                new FrameworkPropertyMetadata(
                    new CornerRadius(),
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(
                nameof(BorderBrush),
                typeof(Brush),
                typeof(Border),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(
                nameof(Background),
                typeof(Brush),
                typeof(Border),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        public Thickness BorderThickness
        {
            get => (Thickness)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public Border()
        {
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Thickness border = BorderThickness;
            Thickness padding = Padding;
            double horizontalBorder = border.Left + border.Right + padding.Left + padding.Right;
            double verticalBorder = border.Top + border.Bottom + padding.Top + padding.Bottom;

            UIElement child = Child;
            if (child != null)
            {
                Size childConstraint = new Size(
                    Math.Max(0.0, constraint.Width - horizontalBorder),
                    Math.Max(0.0, constraint.Height - verticalBorder));

                child.Measure(childConstraint);
                Size childDesired = child.DesiredSize;

                return new Size(childDesired.Width + horizontalBorder, childDesired.Height + verticalBorder);
            }

            return new Size(horizontalBorder, verticalBorder);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Thickness border = BorderThickness;
            Thickness padding = Padding;
            double left = border.Left + padding.Left;
            double top = border.Top + padding.Top;
            double horizontalBorder = left + border.Right + padding.Right;
            double verticalBorder = top + border.Bottom + padding.Bottom;

            UIElement child = Child;
            if (child != null)
            {
                Rect childRect = new Rect(
                    left,
                    top,
                    Math.Max(0.0, finalSize.Width - horizontalBorder),
                    Math.Max(0.0, finalSize.Height - verticalBorder));

                child.Arrange(childRect);
            }

            return finalSize;
        }

        protected override void OnRender(DrawingContext dc)
        {
            Brush background = Background;
            Brush borderBrush = BorderBrush;
            Thickness border = BorderThickness;
            CornerRadius corners = CornerRadius;

            if (RenderSize.Width <= 0 || RenderSize.Height <= 0)
                return;

            bool snap = SnapsToDevicePixels || UseLayoutRounding;
            double rectW = snap ? Math.Round(RenderSize.Width) : RenderSize.Width;
            double rectH = snap ? Math.Round(RenderSize.Height) : RenderSize.Height;
            if (rectW <= 0 || rectH <= 0)
                return;

            Rect rect = new Rect(0, 0, rectW, rectH);

            bool isUniformCorners = Math.Abs(corners.TopLeft - corners.TopRight) < 0.001
                && Math.Abs(corners.TopLeft - corners.BottomLeft) < 0.001
                && Math.Abs(corners.TopLeft - corners.BottomRight) < 0.001;

            double uniformRadius = corners.TopLeft;

            if (isUniformCorners && uniformRadius <= 0.001)
            {
                double tLeft = Math.Max(0.0, snap && border.Left > 0 ? Math.Max(1.0, Math.Round(border.Left)) : border.Left);
                double tTop = Math.Max(0.0, snap && border.Top > 0 ? Math.Max(1.0, Math.Round(border.Top)) : border.Top);
                double tRight = Math.Max(0.0, snap && border.Right > 0 ? Math.Max(1.0, Math.Round(border.Right)) : border.Right);
                double tBottom = Math.Max(0.0, snap && border.Bottom > 0 ? Math.Max(1.0, Math.Round(border.Bottom)) : border.Bottom);

                // Clamp border thicknesses so opposite edges never overlap
                if (tLeft + tRight > rectW)
                {
                    double scale = rectW / (tLeft + tRight);
                    tLeft *= scale;
                    tRight *= scale;
                }
                if (tTop + tBottom > rectH)
                {
                    double scale = rectH / (tTop + tBottom);
                    tTop *= scale;
                    tBottom *= scale;
                }

                // Draw inner background so semi-transparent borders do not double-blend
                if (background != null)
                {
                    double innerW = Math.Max(0.0, rectW - tLeft - tRight);
                    double innerH = Math.Max(0.0, rectH - tTop - tBottom);
                    if (innerW > 0 && innerH > 0)
                    {
                        dc.DrawRectangle(background, null, new Rect(tLeft, tTop, innerW, innerH));
                    }
                }

                // Draw crisp pixel-aligned border strips without half-pixel stroke distortion
                if (borderBrush != null && (tLeft > 0 || tTop > 0 || tRight > 0 || tBottom > 0))
                {
                    if (tTop > 0)
                        dc.DrawRectangle(borderBrush, null, new Rect(0, 0, rectW, tTop));
                    if (tBottom > 0)
                        dc.DrawRectangle(borderBrush, null, new Rect(0, rectH - tBottom, rectW, tBottom));
                    double midH = Math.Max(0.0, rectH - tTop - tBottom);
                    if (midH > 0)
                    {
                        if (tLeft > 0)
                            dc.DrawRectangle(borderBrush, null, new Rect(0, tTop, tLeft, midH));
                        if (tRight > 0)
                            dc.DrawRectangle(borderBrush, null, new Rect(rectW - tRight, tTop, tRight, midH));
                    }
                }
            }
            else if (isUniformCorners && uniformRadius > 0.001)
            {
                // Rounded rectangle with uniform radius
                Pen borderPen = null;
                Rect drawRect = rect;
                if (borderBrush != null && border.Left > 0)
                {
                    double stroke = border.Left;
                    borderPen = new Pen(borderBrush, stroke);
                    drawRect = new Rect(
                        stroke * 0.5,
                        stroke * 0.5,
                        Math.Max(0.0, rect.Width - stroke),
                        Math.Max(0.0, rect.Height - stroke));
                }
                dc.DrawRoundedRectangle(background, borderPen, drawRect, uniformRadius, uniformRadius);
            }
            else
            {
                // Non-uniform corner radii using StreamGeometry
                StreamGeometry geometry = new StreamGeometry();
                using (StreamGeometryContext ctx = geometry.Open())
                {
                    BuildRoundedRectGeometry(ctx, rect, corners);
                }
                geometry.Freeze();

                Pen pen = (borderBrush != null && border.Left > 0) ? new Pen(borderBrush, border.Left) : null;
                dc.DrawGeometry(background, pen, geometry);
            }
        }

        private static void BuildRoundedRectGeometry(StreamGeometryContext ctx, Rect rect, CornerRadius corners)
        {
            double w = rect.Width;
            double h = rect.Height;

            double tl = Math.Min(corners.TopLeft, Math.Min(w * 0.5, h * 0.5));
            double tr = Math.Min(corners.TopRight, Math.Min(w * 0.5, h * 0.5));
            double br = Math.Min(corners.BottomRight, Math.Min(w * 0.5, h * 0.5));
            double bl = Math.Min(corners.BottomLeft, Math.Min(w * 0.5, h * 0.5));

            ctx.BeginFigure(new Point(rect.X + tl, rect.Y), isFilled: true, isClosed: true);

            // Top edge to Top-Right arc
            ctx.LineTo(new Point(rect.X + w - tr, rect.Y), isStroked: true, isSmoothJoin: true);
            if (tr > 0)
            {
                ctx.ArcTo(new Point(rect.X + w, rect.Y + tr), new Size(tr, tr), 0, false, SweepDirection.Clockwise, true, true);
            }

            // Right edge to Bottom-Right arc
            ctx.LineTo(new Point(rect.X + w, rect.Y + h - br), isStroked: true, isSmoothJoin: true);
            if (br > 0)
            {
                ctx.ArcTo(new Point(rect.X + w - br, rect.Y + h), new Size(br, br), 0, false, SweepDirection.Clockwise, true, true);
            }

            // Bottom edge to Bottom-Left arc
            ctx.LineTo(new Point(rect.X + bl, rect.Y + h), isStroked: true, isSmoothJoin: true);
            if (bl > 0)
            {
                ctx.ArcTo(new Point(rect.X, rect.Y + h - bl), new Size(bl, bl), 0, false, SweepDirection.Clockwise, true, true);
            }

            // Left edge to Top-Left arc
            ctx.LineTo(new Point(rect.X, rect.Y + tl), isStroked: true, isSmoothJoin: true);
            if (tl > 0)
            {
                ctx.ArcTo(new Point(rect.X + tl, rect.Y), new Size(tl, tl), 0, false, SweepDirection.Clockwise, true, true);
            }
        }
    }
}
