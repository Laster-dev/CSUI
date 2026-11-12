// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows.Automation.Peers;
using System.Windows.Ink;
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Controls
{
    public class InkPresenter : Decorator
    {
        public static readonly DependencyProperty StrokesProperty =
            DependencyProperty.Register(
                nameof(Strokes),
                typeof(StrokeCollection),
                typeof(InkPresenter),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnStrokesChanged));

        public InkPresenter()
        {
            Strokes = new StrokeCollection();
        }

        public StrokeCollection Strokes
        {
            get => (StrokeCollection)GetValue(StrokesProperty);
            set => SetValue(StrokesProperty, value);
        }

        public void AttachVisuals(Visual visual, DrawingAttributes drawingAttributes)
        {
            InvalidateVisual();
        }

        public void DetachVisuals(Visual visual)
        {
            InvalidateVisual();
        }

        private static void OnStrokesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InkPresenter presenter)
            {
                if (e.OldValue is StrokeCollection oldStrokes)
                {
                    oldStrokes.StrokesChanged -= presenter.OnStrokesCollectionChanged;
                }

                if (e.NewValue is StrokeCollection newStrokes)
                {
                    newStrokes.StrokesChanged += presenter.OnStrokesCollectionChanged;
                }

                presenter.InvalidateVisual();
            }
        }

        private void OnStrokesCollectionChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            Strokes?.Draw(drawingContext);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new InkPresenterAutomationPeer(this);
    }
}
