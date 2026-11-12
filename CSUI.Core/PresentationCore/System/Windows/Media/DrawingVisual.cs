// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//



#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// A DrawingVisual is a Visual that can be used to render Vector graphics on the screen.
    /// The content is persistet by the System.
    /// </summary>
    public class DrawingVisual : ContainerVisual
    {
        // bbox in inner coordinate space. Note that this bbox does not
        // contain the childrens extent.
        private IDrawingContent _content;

        /// <summary>
        /// HitTestCore implements precise hit testing against render contents
        /// </summary>
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            ArgumentNullException.ThrowIfNull(hitTestParameters);

            if (_content != null)
            {                
                if (_content.HitTestPoint(hitTestParameters.HitPoint))
                {
                    return new PointHitTestResult(this, hitTestParameters.HitPoint);
                }
            }

            return null;
        }

        /// <summary>
        /// HitTestCore implements precise hit testing against render contents
        /// </summary>
        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            ArgumentNullException.ThrowIfNull(hitTestParameters);

            if ((_content != null) && GetHitTestBounds().IntersectsWith(hitTestParameters.Bounds))
            {                 
                IntersectionDetail intersectionDetail;

                intersectionDetail = _content.HitTestGeometry(hitTestParameters.InternalHitGeometry);
                Debug.Assert(intersectionDetail != IntersectionDetail.NotCalculated);

                if (intersectionDetail != IntersectionDetail.Empty)
                {
                    return new GeometryHitTestResult(this, intersectionDetail);
                }
}

            return null;
        }


        /// <summary>
        /// Opens the DrawingVisual for rendering. The returned DrawingContext can be used to
        /// render into the DrawingVisual.
        /// </summary>
        public DrawingContext RenderOpen()
        {
            VerifyAPIReadWrite();

            return new VisualDrawingContext(this);
        }

        /// <summary>
        /// Called from the DrawingContext when the DrawingContext is closed.
        /// </summary>
        internal override void RenderClose(IDrawingContent newContent)
        {
            IDrawingContent oldContent = _content;
            if (oldContent != null)
            {
                oldContent.PropagateChangedHandler(ContentsChangedHandler, adding: false);
            }
            _content = newContent;
            if (_content != null)
            {
                _content.PropagateChangedHandler(ContentsChangedHandler, adding: true);
            }
            SetFlagsOnAllChannels(true, VisualProxyFlags.IsContentDirty);
            PropagateChangedFlags();
        }


        /// <summary>
        /// Returns the bounding box of the content.
        /// </summary>
        internal override Rect GetContentBounds()
        {            
            if (_content != null)
            {
                Rect resultRect = Rect.Empty;
                MediaContext mediaContext = MediaContext.From(Dispatcher);                
                BoundsDrawingContextWalker ctx = mediaContext.AcquireBoundsDrawingContextWalker();

                resultRect = _content.GetContentBounds(ctx);
                mediaContext.ReleaseBoundsDrawingContextWalker(ctx);
                
                return resultRect;
            }
            else
            {
                return Rect.Empty;
            }
        }

        /// <summary>
        /// WalkContent - method which walks the content (if present) and calls out to the
        /// supplied DrawingContextWalker.
        /// </summary>
        /// <param name="walker">
        ///   DrawingContextWalker - the target of the calls which occur during
        ///   the content walk.
        /// </param>
        internal void WalkContent(DrawingContextWalker walker)
        {
            VerifyAPIReadOnly();

            _content?.WalkContent(walker);
        }

        
        /// <summary>
        /// GetDrawing - Returns the drawing content of this Visual.  
        /// </summary>
        /// <remarks>
        /// Changes to this DrawingGroup will not be propagated to the Visual's content.
        /// This method is called by both the Drawing property, and VisualTreeHelper.GetDrawing()
        /// </remarks>        
        internal override unsafe void RenderContentD2D(void* d2dContext)
        {
            if (_content is RenderData rd)
            {
                rd.RenderToD2D(d2dContext);
            }
            else if (_content != null)
            {
                Direct2DRenderWalker.Render(_content, d2dContext);
            }
        }

        internal override DrawingGroup GetDrawing()
        {
            // Need to determine if Visual.Drawing should return mutable content    

            VerifyAPIReadOnly();

            DrawingGroup drawingGroupContent = null;                  

            // Convert our content to a DrawingGroup, if content exists
            if (_content != null)
            {
                drawingGroupContent = DrawingServices.DrawingGroupFromRenderData((RenderData) _content);
            }

            return drawingGroupContent;
        }                  

        /// <summary>
        /// Drawing - Returns the drawing content of this Visual.  
        /// </summary>
        /// <remarks>
        /// Changes to this DrawingGroup will not propagated to the Visual's content.
        /// </remarks>
        public DrawingGroup Drawing
        {
            get
            {
                return GetDrawing();
            }                
        }  
    }
}

