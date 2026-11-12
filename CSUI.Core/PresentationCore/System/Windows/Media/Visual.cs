// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description:
//      Defines a node in the composition scene graph.

using System.Windows.Threading;
using System.Windows.Diagnostics;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Interop;
using MS.Internal;
using MS.Internal.Media;

//------------------------------------------------------------------------------
// This section lists various things that we could improve on the Visual class.
//
// - (Pail) Don't allocate a managed Pail object.
// - (Finalizer) Currently we delete the cob explicitly when a node is removed from
//   the scene graph. However, we don't do this when we remove the root node.
//   currently this is done by the finalizer. If we clean explicitly up when we
//   remove the root node we won't need a finalizer.
//------------------------------------------------------------------------------


//------------------------------------------------------------------------------
// PUBLIC API EXPOSURE RULES
//
// If you expose a public/protected API you need to check a couple things:
//
// A) Call the correct version of VerifyAPI.  This checks the following
//      1) That the calling thread has entered the context of this object
//      2) That the current object is not disposed.
//      3) If another object is passed in, that it has the same
//         context affinity as this object.  This should be used for
//         arguments to the API
//      4) That the current permissions are acceptable.
//
// B) That other arguments are not disposed if needed.
//
//
//------------------------------------------------------------------------------



#nullable disable

using SR = MS.Internal.PresentationCore.SR;



namespace System.Windows.Media
{
    /// <summary>
    /// The Visual class is the base class for all Visual types. It provides
    /// services and properties that all Visuals have in common. Services include
    /// hit-testing, coordinate transformation, bounding box calculations. Properties
    /// are for example a transform property and an opacity property.
    ///
    /// Derived Visuals render their content first and then render the children, or in other
    /// words, the content of a Visual is always behind the content of its children.
    /// </summary>
    public abstract partial class Visual : DependencyObject
    {
        // --------------------------------------------------------------------
        //
        //   Constants
        //
        // --------------------------------------------------------------------

        #region Constants

        /// <summary>
        /// This is the dirty mask for a visual, set every time we marshall
        /// a visual to a channel and reset by the end of the render pass.
        /// </summary>
        private const VisualProxyFlags c_ProxyFlagsDirtyMask =
              VisualProxyFlags.IsSubtreeDirtyForRender
            | VisualProxyFlags.IsContentDirty
            | VisualProxyFlags.IsTransformDirty
            | VisualProxyFlags.IsGuidelineCollectionDirty
            | VisualProxyFlags.IsClipDirty
            | VisualProxyFlags.IsOpacityDirty
            | VisualProxyFlags.IsOpacityMaskDirty
            | VisualProxyFlags.IsOffsetDirty
            | VisualProxyFlags.IsEdgeModeDirty
            | VisualProxyFlags.IsEffectDirty
            | VisualProxyFlags.IsBitmapScalingModeDirty
            | VisualProxyFlags.IsScrollableAreaClipDirty
            | VisualProxyFlags.IsClearTypeHintDirty
            | VisualProxyFlags.IsCacheModeDirty
            | VisualProxyFlags.IsTextRenderingModeDirty
            | VisualProxyFlags.IsTextHintingModeDirty;


        /// <summary>
        /// This is the dirty mask for a visual, set every time we marshall
        /// a visual to a channel and reset by the end of the render pass.
        ///
        /// This mask is only for Viewport3D visual, since the contents
        /// of the Viewport3D are rendered during RenderContent, we
        /// need to set those flags as dirty if the visual is not on
        /// channel yet
        /// </summary>


        #endregion Constants



        // --------------------------------------------------------------------
        //
        //   Internal Constructor
        //
        // --------------------------------------------------------------------

        #region Internal Constructor

        /// <summary>
        /// This internal ctor is a hook to allow Visual subclasses
        /// to create their unique type of a visual resource.
        /// </summary>
        internal Visual(bool unused)

        {

#if DEBUG

            _parentIndex = -1;

#endif

        }


        #endregion Protected Constructor


        // --------------------------------------------------------------------
        //
        //   Protected Constructor
        //
        // --------------------------------------------------------------------

        #region Protected Constructor

        /// <summary>
        /// Ctor Visual
        /// </summary>
        protected Visual()

        {

#if DEBUG

            _parentIndex = -1;

#endif

        }


        #endregion Protected Constructor


        // --------------------------------------------------------------------
        //
        //   IResource implementation
        //
        // --------------------------------------------------------------------

        #region IResource implementation











        #endregion IResource implementation


        // --------------------------------------------------------------------
        //
        //   IElement implementation
        //
        // --------------------------------------------------------------------

        // --------------------------------------------------------------------
        //
        //   Internal Properties
        //
        // --------------------------------------------------------------------

        #region Internal Properties

        // Are we in the process of iterating the visual children.
        // This flag is set during a descendents walk, for property invalidation.
        internal bool IsVisualChildrenIterationInProgress
        {
            get { return CheckFlagsAnd(VisualFlags.IsVisualChildrenIterationInProgress); }

            set { SetFlags(value, VisualFlags.IsVisualChildrenIterationInProgress); }
        }


        /// <summary>
        /// The CompositionTarget marks the root element. The root element is responsible
        /// for posting renders.
        /// </summary>
        /// <remarks>
        /// The property getter is also used to ensure that the Visual is
        /// not used in multiple CompositionTargets.
        /// </remarks>
        internal bool IsRootElement
        {
            get
            {
                return CheckFlagsAnd(VisualFlags.ShouldPostRender);
            }

            set
            {
                SetFlags(value, VisualFlags.ShouldPostRender);
            }
        }
        #endregion Internal Properties



        // --------------------------------------------------------------------
        //
        //   Visual Content
        //
        // --------------------------------------------------------------------

        #region Visual Content

        /// <summary>
        /// Derived classes must override this method and return the bounding
        /// box of their content.
        /// </summary>
        internal virtual Rect GetContentBounds()
        {
            return Rect.Empty;
        }



        /// <summary>
        /// This method is overrided on Visuals that can instantiate IDrawingContext
        /// Currently, DrawingVisual and UIElement
        /// </summary>
        internal virtual void RenderClose(IDrawingContent newContent)
        {
        }

        /// <summary>
        /// VisualContentBounds returns the bounding box for the contents of the current visual.
        /// </summary>
        internal Rect VisualContentBounds
        {
            get
            {
                // Probably too restrictive. Let's see who wants it during OnRender.
                VerifyAPIReadWrite();

                return GetContentBounds();
            }
        }


        /// <summary>
        /// VisualDescendantBounds returns the union of all of the content bounding
        /// boxes for all of the descendants of the current visual,and also including
        /// the contents of the current visual. So we end up with the
        /// bounds of the whole sub-graph in inner space.
        /// </summary>
        internal Rect VisualDescendantBounds
        {
            get
            {
                // Probably too restrictive. Let's see who wants it during OnRender.
                VerifyAPIReadWrite();

                Rect bboxSubgraph = CalculateSubgraphBoundsInnerSpace();

                // If bounding box has NaN, then we set the bounding box to infinity.
                if (DoubleUtil.RectHasNaN(bboxSubgraph))
                {
                    bboxSubgraph.X = Double.NegativeInfinity;
                    bboxSubgraph.Y = Double.NegativeInfinity;
                    bboxSubgraph.Width = Double.PositiveInfinity;
                    bboxSubgraph.Height = Double.PositiveInfinity;
                }
                return bboxSubgraph;
            }
        }


        /// <summary>
        /// Computes the union of all content bounding boxes of this Visual's sub-graph
        /// in inner space. Note that the result includes the root Visual's content.
        ///
        /// Definition: Outer/Inner Space:
        ///
        ///      A Visual has a set of properties which include clip, transform, offset
        ///      and bitmap effect. Those properties affect in which space (coordinate
        ///      clip space) a Visual's vector graphics and sub-graph is interpreted.
        ///      Inner space is the space before applying any of the properties. Outer
        ///      space is the space where all the properties have been taken into account.
        ///      For example if the Visual draws a rectangle {0, 0, 100, 100} and the
        ///      Offset property is set to {20, 20} and the clip is set to {40, 40, 10, 10}
        ///      then the bounding box of the Visual in inner space is {0, 0, 100, 100} and
        ///      in outer space {60, 60, 10, 10} (start out with the bbox of {0, 0, 100, 100}
        ///      then apply the clip {40, 40, 10, 10} which leaves us with a bbox of
        ///      {40, 40, 10, 10} and finally apply the offset and we end up with a bbox
        ///      of {60, 60, 10, 10}
        /// </summary>
        internal Rect CalculateSubgraphBoundsInnerSpace()
        {
            return CalculateSubgraphBoundsInnerSpace(false);
        }

        /// <summary>
        /// Computes the union of all rendering bounding boxes of this Visual's sub-graph
        /// in inner space. Note that the result includes the root Visual's content.
        /// </summary>
        internal Rect CalculateSubgraphRenderBoundsInnerSpace()
        {
            return CalculateSubgraphBoundsInnerSpace(true);
        }

        /// <summary>
        /// Same as the parameterless CalculateSubgraphBoundsInnerSpace except it takes a
        /// boolean indicating whether or not to calculate the rendering bounds.
        /// If the renderBounds parameter is set to true then the render bounds are returned.
        /// The render bounds differ in that they treat zero area bounds as emtpy rectangles.
        ///
        /// 
        /// This is needed since MIL and the managed size differ about how big content bounds are
        /// WPF considers geometric bounds (i.e. it will union in points) while MIL considers anything
        /// with zero area to be empty.  This poses problems when looking for the exact size of a
        /// CyclicBrush.
        /// </summary>
        internal virtual Rect CalculateSubgraphBoundsInnerSpace(bool renderBounds)
        {
            Rect bboxSubgraph = Rect.Empty;

            // Recursively calculate sub-graph bounds of children of this node. We get
            // the bounds of each child Visual in outer space which is this Visual's
            // inner space and union them together.

            int count = VisualChildrenCount;

            for (int i = 0; i < count; i++)
            {
                Visual child = GetVisualChild(i);
                if (child != null)
                {
                    Rect bboxSubgraphChild = child.CalculateSubgraphBoundsOuterSpace(renderBounds);

                    bboxSubgraph.Union(bboxSubgraphChild);
                }
            }

            // Get the content bounds of the Visual.  In the case that we're interested in render
            // bounds (i.e. what MIL will consider the size of the object), we set 0 area rects
            // to be empty so that they don't union to create larger sized rects.
            Rect contentBounds = GetContentBounds();
            if (renderBounds && IsEmptyRenderBounds(ref contentBounds /* ref for perf - not modified */))
            {
                contentBounds = Rect.Empty;
            }

            // Union the content bounds to the sub-graph bounds so that we end up with the
            // bounds of the whole sub-graph in inner space and return it.
            bboxSubgraph.Union(contentBounds);

            return bboxSubgraph;
        }



        /// <summary>
        /// Computes the union of all content bounding boxes of this Visual's sub-graph
        /// in outer space. Note that the result includes the root Visual's content.
        /// For a definition of outer/inner space see CalculateSubgraphBoundsInnerSpace.
        /// </summary>
        internal Rect CalculateSubgraphBoundsOuterSpace()
        {
            return CalculateSubgraphBoundsOuterSpace(renderBounds: false);
        }

        /// <summary>
        /// Computes the union of all rendering bounding boxes of this Visual's sub-graph
        /// in outer space. Note that the result includes the root Visual's content.
        /// For a definition of outer/inner space see CalculateSubgraphBoundsInnerSpace.
        /// </summary>
        internal Rect CalculateSubgraphRenderBoundsOuterSpace()
        {
            return CalculateSubgraphBoundsOuterSpace(renderBounds: true);
        }

        /// <summary>
        /// Same as the parameterless CalculateSubgraphBoundsOuterSpace except it takes a
        /// boolean indicating whether or not to calculate the rendering bounds.
        /// If the renderBounds parameter is set to true then the render bounds are returned.
        /// The render bounds differ in that they treat zero area bounds as emtpy rectangles.
        ///
        /// 
        /// This is needed since MIL and the managed size differ about how big content bounds are
        /// WPF considers geometric bounds (i.e. it will union in points) while MIL considers anything
        /// with zero area to be empty.  This poses problems when looking for the exact size of a
        /// CyclicBrush.
        /// </summary>
        private Rect CalculateSubgraphBoundsOuterSpace(bool renderBounds)
        {
            Rect bboxSubgraph = Rect.Empty;

            // Get the inner space bounding box of this node and then transform it into outer
            // space.

            bboxSubgraph = CalculateSubgraphBoundsInnerSpace(renderBounds);

            // Apply Effect

            if (CheckFlagsAnd(VisualFlags.NodeHasEffect))
            {
                Rect effectBounds;

                Effect effect = EffectField.GetValue(this);
                if (effect != null)
                {
                    // The Effect always deals in unit bounds, so transform the
                    // unit rect and then map back into the world space bounds
                    // defined by bboxSubgraph.

                    Rect unitBounds = new Rect(0,0,1,1);
                    Rect unitTransformedBounds = effect.EffectMapping.TransformBounds(unitBounds);
                    effectBounds = Effect.UnitToWorld(unitTransformedBounds, bboxSubgraph);

                    bboxSubgraph.Union(effectBounds);
                }
                
            }

            // Apply Clip.

            Geometry clip = ClipField.GetValue(this);
            if (clip != null)
            {
                bboxSubgraph.Intersect(clip.Bounds);
            }

            // Apply Transform.
            Transform transform = TransformField.GetValue(this);

            if ((transform != null) && (!transform.IsIdentity))
            {
                Matrix m = transform.Value;
                MatrixUtil.TransformRect(ref bboxSubgraph, ref m);
            }

            // Apply Offset.
            if (!bboxSubgraph.IsEmpty)
            {
                bboxSubgraph.X += _offset.X;
                bboxSubgraph.Y += _offset.Y;
            }

            // Apply scrollable-area clip.
            Rect? scrollClip = ScrollableAreaClipField.GetValue(this);
            if (scrollClip.HasValue)
            {
                bboxSubgraph.Intersect(scrollClip.Value);
            }

            // If bounding box has NaN, then we set the bounding box to infinity.
            if (DoubleUtil.RectHasNaN(bboxSubgraph))
            {
                bboxSubgraph.X = Double.NegativeInfinity;
                bboxSubgraph.Y = Double.NegativeInfinity;
                bboxSubgraph.Width = Double.PositiveInfinity;
                bboxSubgraph.Height = Double.PositiveInfinity;
            }

            return bboxSubgraph;
        }

        /// <summary>
        /// This method returns true if the given WPF bounds will be considered
        /// empty in terms of rendering.  This is the case when the bounds describe
        /// a zero-area space.  bounds are passed by ref for speed but are not modified
        ///
        /// 
        /// See above CalculateSubgraphBounds* methods for more detail.  This helper method
        /// goes with them.
        /// </summary>
        private bool IsEmptyRenderBounds(ref Rect bounds)
        {
            return (bounds.Width <= 0 || bounds.Height <= 0);
        }

        #endregion Visual Content

        // --------------------------------------------------------------------
        //
        //   Resource Marshalling and Unmarshalling
        //
        // --------------------------------------------------------------------

        #region Resource Marshalling and Unmarshalling







        #endregion Resource Marshalling and Unmarshalling


        // --------------------------------------------------------------------
        //
        //   Access Verification
        //
        // --------------------------------------------------------------------

        #region Access Verification

        /// <summary>
        /// Applies various API checks
        /// </summary>
        internal void VerifyAPIReadOnly()
        {
            // Verify that we are executing on the right context
            VerifyAccess();
        }

        /// <summary>
        /// Applies various API checks
        /// </summary>
        internal void VerifyAPIReadOnly(DependencyObject value)
        {
            VerifyAPIReadOnly();

            // Make sure the value is on the same context as the visual.
            // AssertSameContext handles null and Dispatcher-free values.
            MediaSystem.AssertSameContext(this, value);
        }

        /// <summary>
        /// Applies various API checks for read/write
        /// </summary>
        internal void VerifyAPIReadWrite()
        {
            VerifyAPIReadOnly();

            // Verify the correct access permissions
            MediaContext.From(this.Dispatcher).VerifyWriteAccess();
        }

        /// <summary>
        /// Applies various API checks
        /// </summary>
        internal void VerifyAPIReadWrite(DependencyObject value)
        {
            VerifyAPIReadWrite();

            // Make sure the value is on the same context as the visual.
            // AssertSameContext handles null and Dispatcher-free values.
            MediaSystem.AssertSameContext(this, value);
        }

        #endregion Access Verification

        // --------------------------------------------------------------------
        //
        //   Pre-compute / render passes
        //
        // --------------------------------------------------------------------

        #region Pre-compute / render passes

        internal void Precompute()
        {
            if (CheckFlagsAnd(VisualFlags.IsSubtreeDirtyForPrecompute))
            {
                // Disable processing of the queue during blocking operations to prevent unrelated reentrancy.
                using(Dispatcher.DisableProcessing())
                {
                    MediaContext mediaContext = MediaContext.From(Dispatcher);

                    try
                    {
                        mediaContext.PushReadOnlyAccess();

                        Rect bboxSubgraph;

                        PrecomputeRecursive(out bboxSubgraph);
                    }
                    finally
                    {
                        mediaContext.PopReadOnlyAccess();
                    }
                }
            }
        }

        /// <summary>
        /// Derived class can do precomputations on their content by overriding this method.
        /// Derived classes must call the base class.
        /// </summary>
        internal virtual void PrecomputeContent()
        {
            _bboxSubgraph = GetHitTestBounds();

            // If bounding box has NaN, then we set the bounding box to infinity.
            if (DoubleUtil.RectHasNaN(_bboxSubgraph))
            {
                _bboxSubgraph.X = Double.NegativeInfinity;
                _bboxSubgraph.Y = Double.NegativeInfinity;
                _bboxSubgraph.Width = Double.PositiveInfinity;
                _bboxSubgraph.Height = Double.PositiveInfinity;
            }
        }

        internal void PrecomputeRecursive(out Rect bboxSubgraph)
        {
            // Simple loop detection to avoid stack overflow in cyclic Visual
            // scenarios. This fix is only aimed at mitigating a very common
            // VisualBrush scenario.
            bool canEnter = Enter();

            if (canEnter)
            {
                try
                {
                    if (CheckFlagsAnd(VisualFlags.IsSubtreeDirtyForPrecompute))
                    {
                        PrecomputeContent();

                        int childCount = VisualChildrenCount;

                        for (int i = 0; i < childCount; i++)
                        {
                            Visual child = GetVisualChild(i);
                            if (child != null)
                            {
                                Rect bboxSubgraphChild;

                                child.PrecomputeRecursive(out bboxSubgraphChild);

                                _bboxSubgraph.Union(bboxSubgraphChild);
                            }
                        }

                        SetFlags(false, VisualFlags.IsSubtreeDirtyForPrecompute);
                    }

                    // Bounding boxes are cached in inner space (below offset, transform, and clip).
                    // Before returning them we need
                    // to transform them into outer space.

                    bboxSubgraph = _bboxSubgraph;

                    Geometry clip = ClipField.GetValue(this);
                    if (clip != null)
                    {
                        bboxSubgraph.Intersect(clip.Bounds);
                    }

                    Transform transform = TransformField.GetValue(this);

                    if ((transform != null) && (!transform.IsIdentity))
                    {
                        Matrix m = transform.Value;
                        MatrixUtil.TransformRect(ref bboxSubgraph, ref m);
                    }

                    if (!bboxSubgraph.IsEmpty)
                    {
                        bboxSubgraph.X += _offset.X;
                        bboxSubgraph.Y += _offset.Y;
                    }

                    Rect? scrollClip = ScrollableAreaClipField.GetValue(this);
                    if (scrollClip.HasValue)
                    {
                        bboxSubgraph.Intersect(scrollClip.Value);
                    }

                    // If child's bounding box has NaN, then we set the bounding box to infinity.
                    if (DoubleUtil.RectHasNaN(bboxSubgraph))
                    {
                        bboxSubgraph.X = Double.NegativeInfinity;
                        bboxSubgraph.Y = Double.NegativeInfinity;
                        bboxSubgraph.Width = Double.PositiveInfinity;
                        bboxSubgraph.Height = Double.PositiveInfinity;
                    }
                }
                finally
                {
                    Exit();
                }
            }
            else
            {
                bboxSubgraph = new Rect();
            }
        }



        /// <summary>
        /// Enter is used for simple cycle detection in Visual. If the method returns false
        /// the Visual has already been entered and cannot be entered again. Matching invocation of Exit
        /// must be skipped if Enter returns false.
        /// </summary>
        internal bool Enter()
        {
            if (CheckFlagsAnd(VisualFlags.ReentrancyFlag))
            {
                return false;
            }
            else
            {
                SetFlags(true, VisualFlags.ReentrancyFlag);
                return true;
            }
        }

        /// <summary>
        /// Exits the Visual. For more details see Enter method.
        /// </summary>
        internal void Exit()
        {
            Debug.Assert(CheckFlagsAnd(VisualFlags.ReentrancyFlag)); // Exit must be matched with Enter. See Enter comments.
            SetFlags(false, VisualFlags.ReentrancyFlag);
        }














        #endregion Pre-compute / render passes



        // --------------------------------------------------------------------
        //
        //   Hit Testing
        //
        // --------------------------------------------------------------------

        #region Hit Testing

        internal class TopMostHitResult
        {
            internal HitTestResult _hitResult = null;

            internal HitTestResultBehavior HitTestResult(HitTestResult result)
            {
                _hitResult = result;

                return HitTestResultBehavior.Stop;
            }

        }


        /// <summary>
        /// Used by derived classes to invalidate their hit-test bounds.
        /// </summary>
        internal void InvalidateHitTestBounds()
        {
            VerifyAPIReadWrite();

            PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.None);
        }


        /// <summary>
        /// Derived classes return the hit-test bounding box from the
        /// GetHitTestBounds virtual. Visual uses the bounds to optimize
        /// hit-testing.
        /// </summary>
        internal virtual Rect GetHitTestBounds()
        {
            return GetContentBounds();
        }


        /// <summary>
        /// Return top most visual of a hit test.
        /// </summary>
        internal HitTestResult HitTest(Point point)
        {
            return HitTest(point, include2DOn3D: true);
        }

        internal HitTestResult HitTest(Point point, bool include2DOn3D)
        {
            TopMostHitResult result = new TopMostHitResult();
            HitTestFilterCallback filterCallback = include2DOn3D ? null : new HitTestFilterCallback(Skip2DOn3DFilterCallback);
            HitTest(filterCallback, new HitTestResultCallback(result.HitTestResult), new PointHitTestParameters(point));
            return result._hitResult;
        }

        private static HitTestFilterBehavior Skip2DOn3DFilterCallback(DependencyObject potentialHitTestTarget)
        {
            if (potentialHitTestTarget is Viewport2DVisual3D)
            {
                return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
            }

            return HitTestFilterBehavior.Continue;
        }


        /// <summary>
        /// Initiate a hit test using delegates.
        /// </summary>
        internal void HitTest(
            HitTestFilterCallback filterCallback,
            HitTestResultCallback resultCallback,
            HitTestParameters hitTestParameters)
        {
            ArgumentNullException.ThrowIfNull(resultCallback);

            ArgumentNullException.ThrowIfNull(hitTestParameters);

            VerifyAPIReadWrite();

            Precompute();

            PointHitTestParameters pointParams = hitTestParameters as PointHitTestParameters;

            if (pointParams != null)
            {
                // Because we call dynamic code during the hit testing walk we need to back up
                // the original hit point in case the user's delegate throws an exception so that
                // we can restore it.
                Point backupHitPoint = pointParams.HitPoint;

                try
                {
                    HitTestPoint(filterCallback, resultCallback, pointParams);
                }
                catch
                {
                    // If an exception occured, restore the user's hit point and rethrow.
                    pointParams.SetHitPoint(backupHitPoint);

                    throw;
                }
                finally
                {
                    Debug.Assert(Point.Equals(pointParams.HitPoint, backupHitPoint),
                        "Failed to restore user's hit point back to the original coordinate system.");
                }
            }
            else
            {
                GeometryHitTestParameters geometryParams = hitTestParameters as GeometryHitTestParameters;

                if (geometryParams != null)
                {
                    // Because we call dynamic code during the hit testing walk we need to ensure
                    // that if the user's delegate throws an exception we restore the original
                    // transform on the hit test geometry.
#if DEBUG
                    // Internally we replace the hit geometry with a copy which is guaranteed to have
                    // a MatrixTransform so we do not need to worry about null dereferences here.
                    Matrix originalMatrix = geometryParams.InternalHitGeometry.Transform.Value;
#endif // DEBUG
                    try
                    {
                        HitTestGeometry(filterCallback, resultCallback, geometryParams);
                    }
                    catch
                    {
                        geometryParams.EmergencyRestoreOriginalTransform();

                        throw;
                    }
#if DEBUG
                    finally
                    {
                        Debug.Assert(Matrix.Equals(geometryParams.InternalHitGeometry.Transform.Value, originalMatrix),
                            "Failed to restore user's hit geometry back to the original coordinate system.");
                    }
#endif // DEBUG
                }
                else
                {
                    // This should never happen, users can not extend the abstract HitTestParameters class.
                    Invariant.Assert(false,
                        String.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "'{0}' HitTestParameters are not supported on {1}.",
                            hitTestParameters.GetType().Name, this.GetType().Name));
                }
            }
        }

        internal HitTestResultBehavior HitTestPoint(
            HitTestFilterCallback filterCallback,
            HitTestResultCallback resultCallback,
            PointHitTestParameters pointParams)
        {
            // we do not need parameter checks because they are done in HitTest()

            Geometry clip = VisualClip;

            // Before we continue hit-testing we check against the hit-test bounds for the sub-graph.
            // If the point is not with-in the hit-test bounds, the sub-graph can be skipped.
            if (_bboxSubgraph.Contains(pointParams.HitPoint) &&
                ((null == clip) || clip.FillContains(pointParams.HitPoint))) // Check that the hit-point is with-in the clip.
            {
                //
                // Determine if there is a special filter behavior defined for this
                // Visual.
                //

                HitTestFilterBehavior filter = HitTestFilterBehavior.Continue;
                if (filterCallback != null)
                {
                    filter = filterCallback(this);

                    if (filter == HitTestFilterBehavior.ContinueSkipSelfAndChildren)
                    {
                        return HitTestResultBehavior.Continue;
                    }

                    if (filter == HitTestFilterBehavior.Stop)
                    {
                        return HitTestResultBehavior.Stop;
                    }
                }

                // if there is a bitmap effect transform the point
                // Backup the hit point so that we can restore it later on.
                Point originalHitPoint = pointParams.HitPoint;
                Point hitPoint = originalHitPoint;

                if (CheckFlagsAnd(VisualFlags.NodeHasEffect))
                {
                    Effect imageEffect = EffectField.GetValue(this);
                    if (imageEffect != null)
                    {
                        GeneralTransform effectHitTestInverse = imageEffect.EffectMapping.Inverse;

                        // only do work if the transform isn't the identity transform
                        if (effectHitTestInverse != Transform.Identity)
                        {
                            bool ok = false;

                            // Convert to unit space
                            Point? unitHitPoint = Effect.WorldToUnit(originalHitPoint, _bboxSubgraph);
                            if (unitHitPoint != null)
                            {
                                Point transformedPt = new Point();

                                // Do the transform
                                if (effectHitTestInverse.TryTransform(unitHitPoint.Value, out transformedPt))
                                {
                                    // Convert back to world space
                                    Point? worldSpace = Effect.UnitToWorld(transformedPt, _bboxSubgraph);
                                    if (worldSpace != null)
                                    {
                                        hitPoint = worldSpace.Value;
                                        ok = true;
                                    }
                                }
                            }

                            if (!ok)
                            {
                                return HitTestResultBehavior.Continue;
                            }
                        }
                    }
                    
                }



                //
                // Hit test against the children.
                //
                if (filter != HitTestFilterBehavior.ContinueSkipChildren)
                {
                    int childCount = VisualChildrenCount;
                    for (int i=childCount-1; i>=0; i--)
                    {
                        Visual child = GetVisualChild(i);
                        if (child != null)
                        {
                            // Hit the scollClip bounds first, which are in the child's outer-space.
                            Rect? scrollClip = ScrollableAreaClipField.GetValue(child);
                            if (scrollClip.HasValue && !scrollClip.Value.Contains(hitPoint))
                            {
                                // Skip child if the point is not within the ScrollableClip.
                                continue;
                            }

                            //
                            // Transform the hit-test point below offset and transform.
                            //

                            Point newHitPoint = hitPoint;

                            // Apply the offset.
                            newHitPoint -= child._offset;

                            // If we have a transform, apply the transform.
                            Transform childTransform = TransformField.GetValue(child);
                            if (childTransform != null)
                            {
                                Matrix inv = childTransform.Value;

                                // If we can't invert the transform, the child is not hitable. This makes sense since
                                // the node's rendered content is degenerate, i.e. does not really take up any space.
                                // Skip the child by continuing in the loop.
                                if (!inv.HasInverse)
                                {
                                    continue;
                                }

                                inv.Invert();

                                newHitPoint *= inv;
                            }

                            // Set the new hittesting point into the hittest params.
                            pointParams.SetHitPoint(newHitPoint);

                            // Perform the hit-test against the child.
                            HitTestResultBehavior result =
                                child.HitTestPoint(filterCallback, resultCallback, pointParams);

                            // Restore the hit-test point.
                            pointParams.SetHitPoint(originalHitPoint);


                            if (result == HitTestResultBehavior.Stop)
                            {
                                return HitTestResultBehavior.Stop;
                            }
                        }
                    }
                }

                //
                // Hit test against the content of this Visual.
                //

                if (filter != HitTestFilterBehavior.ContinueSkipSelf)
                {
                    // set the transformed hit point
                    pointParams.SetHitPoint(hitPoint);

                    HitTestResultBehavior result = HitTestPointInternal(filterCallback, resultCallback, pointParams);

                    // restore the hit point back to its original
                    pointParams.SetHitPoint(originalHitPoint);

                    if (result == HitTestResultBehavior.Stop)
                    {
                        return HitTestResultBehavior.Stop;
                    }
                }
            }

            return HitTestResultBehavior.Continue;
        }

        // provides a transform that goes between the Visual's coordinate space
        // and that after applying the transforms that bring it to outer space.
        internal GeneralTransform TransformToOuterSpace()
        {
            Matrix m = Matrix.Identity;
            GeneralTransformGroup group = null;
            GeneralTransform result = null;

            if (CheckFlagsAnd(VisualFlags.NodeHasEffect))
            {
                Effect effect = EffectField.GetValue(this);
                if (effect != null)
                {
                    GeneralTransform gt = effect.CoerceToUnitSpaceGeneralTransform(
                        effect.EffectMapping,
                        VisualDescendantBounds);

                    Transform affineTransform = gt.AffineTransform;
                    if (affineTransform != null)
                    {
                        Matrix cm = affineTransform.Value;
                        MatrixUtil.MultiplyMatrix(ref m, ref cm);
                    }
                    else
                    {
                        group = new GeneralTransformGroup();
                        group.Children.Add(gt);
                    }
                }
                
            }

            Transform transform = TransformField.GetValue(this);
            if (transform != null)
            {
                Matrix cm = transform.Value;
                MatrixUtil.MultiplyMatrix(ref m, ref cm);
            }
            m.Translate(_offset.X, _offset.Y); // Consider having a bit that indicates that we have a non-null offset.

            if (group == null)
            {
                result = new MatrixTransform(m);
            }
            else
            {
                group.Children.Add(new MatrixTransform(m));
                result = group;
            }

            result.Freeze();
            return result;
        }

        internal HitTestResultBehavior HitTestGeometry(
            HitTestFilterCallback filterCallback,
            HitTestResultCallback resultCallback,
            GeometryHitTestParameters geometryParams)
        {
            // we do not need parameter checks because they are done in HitTest()
            Geometry clip = VisualClip;
            if (clip != null)
            {
                // HitTest with a Geometry and a clip should hit test with
                // the intersection of the geometry and the clip, not the entire geometry
                IntersectionDetail intersectionDetail = clip.FillContainsWithDetail(geometryParams.InternalHitGeometry);

                Debug.Assert(intersectionDetail != IntersectionDetail.NotCalculated);
                if (intersectionDetail == IntersectionDetail.Empty)
                {
                    // bail out if there is a clip and this region is not inside
                    return HitTestResultBehavior.Continue;
                }
            }

            //
            // Check if the geometry intersects with our hittest bounds.
            // If not, the Visual is not hit-testable at all.

            if (_bboxSubgraph.IntersectsWith(geometryParams.Bounds))
            {
                //
                // Determine if there is a special filter behavior defined for this
                // Visual.
                //

                HitTestFilterBehavior filter = HitTestFilterBehavior.Continue;

                if (filterCallback != null)
                {
                    filter = filterCallback(this);

                    if (filter == HitTestFilterBehavior.ContinueSkipSelfAndChildren)
                    {
                        return HitTestResultBehavior.Continue;
                    }

                    if (filter == HitTestFilterBehavior.Stop)
                    {
                        return HitTestResultBehavior.Stop;
                    }
                }

                //
                // Hit-test against the children.
                //

                int childCount = VisualChildrenCount;

                if (filter != HitTestFilterBehavior.ContinueSkipChildren)
                {
                    for (int i=childCount-1; i>=0; i--)
                    {
                        Visual child = GetVisualChild(i);
                        if (child != null)
                        {
                            // Hit the scollClip bounds first, which are in the child's outer-space.
                            Rect? scrollClip = ScrollableAreaClipField.GetValue(child);
                            if (scrollClip.HasValue)
                            {
                                // Hit-testing with a Geometry and a clip should hit test with
                                // the intersection of the geometry and the clip, not the entire geometry
                                RectangleGeometry rectClip = new RectangleGeometry(scrollClip.Value);
                                IntersectionDetail intersectionDetail = rectClip.FillContainsWithDetail(geometryParams.InternalHitGeometry);

                                Debug.Assert(intersectionDetail != IntersectionDetail.NotCalculated);
                                if (intersectionDetail == IntersectionDetail.Empty)
                                {
                                    // Skip child if there is a scrollable clip and this region is not inside it.
                                    continue;
                                }
                            }

                            // Transform the geometry below offset and transform.
                            Matrix inv = Matrix.Identity;
                            inv.Translate(-child._offset.X, -child._offset.Y);

                            Transform childTransform = TransformField.GetValue(child);
                            if (childTransform != null)
                            {
                                Matrix m = childTransform.Value;

                                // If we can't invert the transform, the child is not hitable. This makes sense since
                                // the node's rendered content is degnerated, i.e. does not really take up any space.
                                // Skipping the child by continuing the loop.
                                if (!m.HasInverse)
                                {
                                   continue;
                                }

                                // Inverse the transform.
                                m.Invert();

                                // Multiply the inverse and the offset together.
                                // inv = inv * m;
                                MatrixUtil.MultiplyMatrix(ref inv, ref m);
                            }

                            // Push the transform on the geometry params.
                            geometryParams.PushMatrix(ref inv);

                            // Hit-Test against the children.

                            HitTestResultBehavior result =
                                child.HitTestGeometry(filterCallback, resultCallback, geometryParams);

                            // Pop the transform from the geometry params.

                            geometryParams.PopMatrix();

                            // Process the result.
                            if (result == HitTestResultBehavior.Stop)
                            {
                                return HitTestResultBehavior.Stop;
                            }
                        }
                    }
                }

                //
                // Hit-test against the content of the Visual.
                //

                if (filter != HitTestFilterBehavior.ContinueSkipSelf)
                {
                    GeometryHitTestResult hitResult = HitTestCore(geometryParams);

                    if (hitResult != null)
                    {
                        Debug.Assert(resultCallback != null);

                        return resultCallback(hitResult);
                    }
                }
            }

            return HitTestResultBehavior.Continue;
        }


        /// <summary>
        /// This method provides an internal extension point for Viewport3DVisual
        /// to grab the HitTestFilterCallback and ResultDelegate before it gets lost in the
        /// forward to HitTestCore.
        /// </summary>
        internal virtual HitTestResultBehavior HitTestPointInternal(
            HitTestFilterCallback filterCallback,
            HitTestResultCallback resultCallback,
            PointHitTestParameters hitTestParameters)
        {
            HitTestResult hitResult = HitTestCore(hitTestParameters);

            if (hitResult != null)
            {
                return resultCallback(hitResult);
            }

            return HitTestResultBehavior.Continue;
        }

        /// <summary>
        /// HitTestCore implements whether we have hit the bounds of this visual.
        /// </summary>
        protected virtual HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            ArgumentNullException.ThrowIfNull(hitTestParameters);

            // If we don't have a clip, or if the clip contains the point, keep going.
            if (GetHitTestBounds().Contains(hitTestParameters.HitPoint))
            {
                return new PointHitTestResult(this, hitTestParameters.HitPoint);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// HitTestCore implements whether we have hit the bounds of this visual.
        /// </summary>
        protected virtual GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            ArgumentNullException.ThrowIfNull(hitTestParameters);

            IntersectionDetail intersectionDetail;

            RectangleGeometry contentGeometry = new RectangleGeometry(GetHitTestBounds());

            intersectionDetail = contentGeometry.FillContainsWithDetail(hitTestParameters.InternalHitGeometry);
            Debug.Assert(intersectionDetail != IntersectionDetail.NotCalculated);

            if (intersectionDetail != IntersectionDetail.Empty)
            {
                return new GeometryHitTestResult(this, intersectionDetail);
            }


            return null;
        }

        #endregion Hit Testing



        // --------------------------------------------------------------------
        //
        //   Visual Operations API
        //
        // --------------------------------------------------------------------

        #region VisualChildren

        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: During this virtual method the Visual tree must not be modified.
        /// </summary>
        protected virtual int VisualChildrenCount
        {
            get { return 0; }
        }

        /// <summary>
        /// Returns the number of 2D children. This returns 0 for visuals
        /// whose children are Visual3Ds.
        /// </summary>
        internal int InternalVisualChildrenCount
        {
            get
            {
                // Call the right virtual method.
                return VisualChildrenCount;
            }
        }

        /// <summary>
        /// Returns the number of children of this object (in most cases this will be
        /// the number of Visuals, but it some cases, Viewport3DVisual for instance,
        /// this is the number of Visual3Ds).
        ///
        /// Used only by VisualTreeHelper.
        /// </summary>
        internal virtual int InternalVisual2DOr3DChildrenCount
        {
            get
            {
                // Call the right virtual method.
                return VisualChildrenCount;
            }
        }

        ///<Summary>
        ///Flag to check if this visual has any children
        ///</Summary>
        internal bool HasVisualChildren
        {
            get
            {
                return ((_flags & VisualFlags.HasChildren) != 0);
            }
        }

        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark:
        ///       Need to lock down Visual tree during the callbacks.
        ///       During this virtual call it is not valid to modify the Visual tree.
        ///
        ///       It is okay to type this protected API to the 2D Visual.  The only 2D Visual with
        ///       3D childern is the Viewport3DVisual which is sealed
        /// </summary>
        protected virtual Visual GetVisualChild(int index)
        {
           throw new ArgumentOutOfRangeException(nameof(index), index, SR.Visual_ArgumentOutOfRange);
        }

        /// <summary>
        /// Returns the 2D child at index "index". This will fail for Visuals
        /// whose children are Visual3Ds.
        /// </summary>
        internal Visual InternalGetVisualChild(int index)
        {
            // Call the right virtual method.
            return GetVisualChild(index);
        }

        /// <summary>
        /// Returns the child at index "index" (in most cases this will be
        /// a Visual, but it some cases, Viewport3DVisual for instance,
        /// this is a Visual3D).
        ///
        /// Used only by VisualTreeHelper.
        /// </summary>
        internal virtual DependencyObject InternalGet2DOr3DVisualChild(int index)
        {
            // Call the right virtual method.
            return GetVisualChild(index);
        }

        /// <summary>
        /// Helper method to provide access to AddVisualChild for the VisualCollection.
        /// </summary>
        internal void InternalAddVisualChild(Visual child)
        {
            this.AddVisualChild(child);
        }

        /// <summary>
        /// Helper method to provide access to RemoveVisualChild for the VisualCollection.
        /// </summary>
        internal void InternalRemoveVisualChild(Visual child)
        {
            this.RemoveVisualChild(child);
        }

        /// <summary>
        /// AttachChild
        ///
        ///    Derived classes must call this method to notify the Visual layer that a new
        ///    child appeard in the children collection. The Visual layer will then call the GetVisualChild
        ///    method to find out where the child was added.
        ///
        ///  Remark: To move a Visual child in a collection it must be first disconnected and then connected
        ///    again. (Moving forward we might want to add a special optimization there so that we do not
        ///    unmarshal our composition resources).
        ///
        ///    It is okay to type this protected API to the 2D Visual.  The only 2D Visual with
        ///    3D childern is the Viewport3DVisual which is sealed.
        /// </summary>
        protected void AddVisualChild(Visual child)
        {
            if (child == null)
            {
                return;
            }

            if (child._parent != null)
            {
                throw new ArgumentException(SR.Visual_HasParent);
            }

            // invalid during a VisualTreeChanged event
            VisualDiagnostics.VerifyVisualTreeChange(this);

            SetFlags(true, VisualFlags.HasChildren);

            // Set the parent pointer.

            child._parent = this;

            //
            // The child might be dirty. Hence we need to propagate dirty information
            // from the parent and from the child.
            //

            Visual.PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);

            Visual.PropagateFlags(
                child,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);

            //
            // Resume layout.
            //
            PropagateResumeLayoutHook?.Invoke(this, child);

            if (IsPerMonitorDpiAwareHook?.Invoke() == true && IsPerMonitorDpiScalingEnabledHook?.Invoke() == true)
            {
                bool flag1 = CheckFlagsAnd(VisualFlags.DpiScaleFlag1);
                bool flag2 = CheckFlagsAnd(VisualFlags.DpiScaleFlag2);
                int index = 0; // dummy value;
                if (flag1 && flag2)
                {
                    index = DpiIndex.GetValue(this);
                }

                child.RecursiveSetDpiScaleVisualFlags(new DpiRecursiveChangeArgs(new DpiFlags(flag1, flag2, index),
                    child.GetDpi(), this.GetDpi()));
            }

            // Fire notifications
            this.OnVisualChildrenChanged(child, visualRemoved: null);
            child.FireOnVisualParentChanged(null);
            VisualDiagnostics.OnVisualChildChanged(this, child, true);
        }

        /// <summary>
        /// DisconnectChild
        ///
        ///    Derived classes must call this method to notify the Visual layer that a
        ///    child was removed from the children collection. The Visual layer will then call
        ///    GetChildren to find out which child has been removed.
        ///
        /// </summary>
        protected void RemoveVisualChild(Visual child)
        {
            if (child == null || child._parent == null)
            {
                return;
            }

            if (child._parent != this)
            {
                throw new ArgumentException(SR.Visual_NotChild);
            }

            // invalid during a VisualTreeChanged event
            VisualDiagnostics.VerifyVisualTreeChange(this);

            VisualDiagnostics.OnVisualChildChanged(this, child, false);

            if (InternalVisual2DOr3DChildrenCount == 0)
            {
                SetFlags(false, VisualFlags.HasChildren);
            }

            // Clear connected flag on removed child.
            child.SetFlags(false, VisualProxyFlags.IsConnectedToParent);

            // Set the parent pointer to null.

            child._parent = null;

            Visual.PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);

            PropagateSuspendLayoutHook?.Invoke(child);

            // Fire notifications
            child.FireOnVisualParentChanged(this);
            OnVisualChildrenChanged(visualAdded: null, child);
        }

        /// <summary>
        /// InvalidateZOrder
        /// Note: must do invalidation without removing / adding
        /// to avoid loosing focused element by input system
        /// </summary>
        internal void InvalidateZOrder()
        {
            //  if we don't have any children, there is nothing to do
            if (VisualChildrenCount == 0)
                return;

            Visual.PropagateFlags(
                       this,
                        VisualFlags.IsSubtreeDirtyForPrecompute,
                        VisualProxyFlags.IsSubtreeDirtyForRender);

            this.SetFlagsOnAllChannels(true, VisualProxyFlags.IsChildrenZOrderDirty);

            //  This is a workaround
            //  Input system needs to be notified about the changes on screen to be able to re-hittest
            NotifyHitTestInvalidatedHook?.Invoke();
        }

        //This is used by LayoutManager as a perf optimization for layout updates.
        //During layout updates, LM needs to find which areas of the visual tree
        //are higher in the tree - they have to be processed first to avoid multiple
        //updates of lower descendants. The tree level counter is maintained by
        //UIElement.PropagateResume/SuspendLayout methods and uses 8 bits in VisualFlags to
        //keep the count.
        internal uint TreeLevel
        {
            get
            {
                return ((uint)_flags & 0xFFE00000) >> 21;
            }
            set
            {
                if(value > TreeLevelLimit)
                {
                    throw new InvalidOperationException(SR.Format(SR.LayoutManager_DeepRecursion, TreeLevelLimit));
                }

                _flags = (VisualFlags)(((uint)_flags & 0x001FFFFF) | (value << 21));
            }
        }


        #endregion VisualChildren


        #region VisualParent

        /// <summary>
        /// Returns the parent of this Visual.  Parent may be either a Visual or Visual3D.
        /// </summary>
        protected DependencyObject VisualParent
        {
            get
            {
                VerifyAPIReadOnly();

                return InternalVisualParent;
            }
        }

        /// <summary>
        /// Identical to VisualParent, except that skips verify access for perf.
        /// </summary>
        internal DependencyObject InternalVisualParent
        {
            get
            {
                return _parent;
            }
        }

        #endregion VisualParent

        // These 2 method will be REMOVED once Hamid is back and can
        // explain why Window needs to Bypass layout for setting Flow Direction.
        // These methods are only called from InternalSetLayoutTransform which is called only from Window
        internal void InternalSetOffsetWorkaround(Vector offset)
        {
            VisualOffset = offset;
        }
        internal void InternalSetTransformWorkaround(Transform transform)
        {
            VisualTransform = transform;
        }

        // --------------------------------------------------------------------
        //
        //   Visual Properties
        //
        // --------------------------------------------------------------------

        #region Visual Properties

        /// <summary>
        /// Gets or sets the transform of this Visual.
        /// </summary>
        protected internal Transform VisualTransform
        {
            get
            {
                VerifyAPIReadOnly();

                return TransformField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite(value);

                Transform transform = TransformField.GetValue(this);
                if (transform == value)
                {
                    return;
                }

                Transform newTransform = value;

                // Add changed notifications for the new transform if necessary.
                if (newTransform != null && !newTransform.IsFrozen)
                {
                    newTransform.Changed += TransformChangedHandler;
                }

                if (transform != null)
                {
                    //
                    // Remove changed notifications for the old transform if necessary.
                    //

                    if (!transform.IsFrozen)
                    {
                        transform.Changed -= TransformChangedHandler;
                    }

                }

                //
                // Set the new clip and mark it dirty
                //

                TransformField.SetValue(this, newTransform);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsTransformDirty);

                TransformChanged(sender: null, args: null);
            }
        }

        /// <summary>
        /// Gets or sets the Effect of this Visual.
        /// </summary>
        protected internal Effect VisualEffect

        {

            get

            {

                VerifyAPIReadOnly();

                return EffectField.GetValue(this);

            }

            protected set

            {

                VerifyAPIReadWrite(value);

                VisualEffectInternal = value;

            }

        }


        /// <summary>
        /// Internal accessor to image effect property that gets or sets the Effect of this Visual.
        /// The internal accessor is used by the VisualBitmapEffect emulation layer to avoid some of the
        /// compatibility checks in the protected VisualEffect property.
        /// </summary>
        internal Effect VisualEffectInternal
        {
            get
            {
                return EffectField.GetValue(this);
            }

            set
            {
                Effect imageEffect = EffectField.GetValue(this);
                if (imageEffect == value)
                {
                    return;
                }

                Effect newEffect = value;

                // Add changed notifications for the new Effect if necessary.
                if (newEffect != null && !newEffect.IsFrozen)
                {
                    newEffect.Changed += EffectChangedHandler;
                }

                if (imageEffect != null)
                {
                    //
                    // Remove changed notifications for the old Effect if necessary.
                    //

                    if (!imageEffect.IsFrozen)
                    {
                        imageEffect.Changed -= EffectChangedHandler;
                    }

                }

                //
                // Set the new effect and mark it dirty
                //

                SetFlags(newEffect != null, VisualFlags.NodeHasEffect);

                EffectField.SetValue(this, newEffect);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsEffectDirty);

                EffectChanged(sender: null, args: null);
            }
        }





        /// <summary>
        /// Gets or sets the caching behavior for the Visual.
        /// </summary>
        protected internal CacheMode VisualCacheMode
        {
            get
            {
                VerifyAPIReadOnly();

                return CacheModeField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite(value);

                CacheMode cacheMode = CacheModeField.GetValue(this);
                if (cacheMode == value)
                {
                    return;
                }

                CacheMode newCacheMode = value;

                // Add changed notifications for the new cache mode if necessary.
                if (newCacheMode != null && !newCacheMode.IsFrozen)
                {
                    newCacheMode.Changed += CacheModeChangedHandler;
                }

                if (cacheMode != null)
                {
                    //
                    // Remove changed notifications for the old cache mode if necessary.
                    //

                    if (!cacheMode.IsFrozen)
                    {
                        cacheMode.Changed -= CacheModeChangedHandler;
                    }

                }

                //
                // Set the new cache mode and mark it dirty
                //

                CacheModeField.SetValue(this, newCacheMode);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsCacheModeDirty);

                CacheModeChanged(sender: null, args: null);
            }
        }

        /// <summary>
        /// Gets or sets the scrollable area clip for the Visual.
        /// </summary>
        protected internal Rect? VisualScrollableAreaClip
        {
            get
            {
                VerifyAPIReadOnly();

                return ScrollableAreaClipField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite();

                Rect? currentValue = ScrollableAreaClipField.GetValue(this);

                if (currentValue != value)
                {
                    ScrollableAreaClipField.SetValue(this, value);

                    SetFlagsOnAllChannels(true, VisualProxyFlags.IsScrollableAreaClipDirty);

                    ScrollableAreaClipChanged(sender: null, args: null);
                }
            }
        }

        /// <summary>
        /// Gets or sets the clip of this Visual.
        /// </summary>
        protected internal Geometry VisualClip
        {
            get
            {
                VerifyAPIReadOnly();

                return ClipField.GetValue(this);
            }
            protected set
            {
                ChangeVisualClip(value, dontSetWhenClose: false);
            }
        }

        /// <summary>
        ///     Processes changing the clip from the old clip to the new clip.
        ///     Called from Visual.set_VisualClip and from places that want
        ///     to optimize setting a new clip (like UIElement.ensureClip).
        /// </summary>
        internal void ChangeVisualClip(Geometry newClip, bool dontSetWhenClose)
        {
            VerifyAPIReadWrite(newClip);

            Geometry oldClip = ClipField.GetValue(this);
            if ((oldClip == newClip) ||
                (dontSetWhenClose && (oldClip != null) && (newClip != null) && oldClip.AreClose(newClip)))
            {
                return;
            }

            // Add changed notifications for the new clip if necessary.
            if (newClip != null && !newClip.IsFrozen)
            {
                newClip.Changed += ClipChangedHandler;
            }

            if (oldClip != null)
            {
                //
                // Remove changed notifications for the old clip if necessary.
                //

                if (!oldClip.IsFrozen)
                {
                    oldClip.Changed -= ClipChangedHandler;
                }

            }

            //
            // Set the new clip and mark it dirty
            //

            ClipField.SetValue(this, newClip);

            SetFlagsOnAllChannels(true, VisualProxyFlags.IsClipDirty);

            ClipChanged(sender: null, args: null);
        }

        /// <summary>
        /// Gets and sets the offset.
        /// </summary>
        protected internal Vector VisualOffset
        {
            get
            {
                // VerifyAPIReadOnly(); // Intentionally removed for performance reasons.
                return _offset;
            }
            protected set
            {
                VerifyAPIReadWrite();

                if (value != _offset) // Fuzzy comparison might be better here.
                {
                    VisualFlags flags;

                    _offset = value;

                    SetFlagsOnAllChannels(true, VisualProxyFlags.IsOffsetDirty);

                    flags = VisualFlags.IsSubtreeDirtyForPrecompute;

                    PropagateFlags(
                        this,
                        flags,
                        VisualProxyFlags.IsSubtreeDirtyForRender);
                }
            }
        }

        /// <summary>
        /// Gets or sets the opacity of the Visual.
        /// </summary>
        protected internal double VisualOpacity
        {
            get
            {
                VerifyAPIReadOnly();

                return OpacityField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite();

                if (OpacityField.GetValue(this) == value)
                {
                    return;
                }

                OpacityField.SetValue(this, value);

                // Microsoft: We need to do more here for animated opacity.

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsOpacityDirty);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// Gets or sets the EdgeMode of the Visual.
        /// </summary>
        protected internal EdgeMode VisualEdgeMode
        {
            get
            {
                VerifyAPIReadOnly();

                return EdgeModeField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite();

                if (EdgeModeField.GetValue(this) == value)
                {
                    return;
                }

                EdgeModeField.SetValue(this, value);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsEdgeModeDirty);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// Gets or sets the ImageScalingMode of the Visual.
        /// </summary>
        protected internal BitmapScalingMode VisualBitmapScalingMode
        {
            get
            {
                VerifyAPIReadOnly();

                return BitmapScalingModeField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite();

                if (BitmapScalingModeField.GetValue(this) == value)
                {
                    return;
                }

                BitmapScalingModeField.SetValue(this, value);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsBitmapScalingModeDirty);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// Gets or sets the ClearTypeHint of the Visual.
        /// </summary>
        protected internal ClearTypeHint VisualClearTypeHint
        {
            get
            {
                VerifyAPIReadOnly();

                return ClearTypeHintField.GetValue(this);
            }
            set
            {
                VerifyAPIReadWrite();

                if (ClearTypeHintField.GetValue(this) == value)
                {
                    return;
                }

                ClearTypeHintField.SetValue(this, value);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsClearTypeHintDirty);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// Gets or sets the TextRenderingMode of the Visual.
        /// </summary>
        protected internal TextRenderingMode VisualTextRenderingMode
        {
            get
            {
                VerifyAPIReadOnly();

                return TextRenderingModeField.GetValue(this);
            }
            set
            {
                VerifyAPIReadWrite();

                if (TextRenderingModeField.GetValue(this) == value)
                {
                    return;
                }

                TextRenderingModeField.SetValue(this, value);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsTextRenderingModeDirty);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// Gets or sets the TextRenderingMode of the Visual.
        /// </summary>
        protected internal TextHintingMode VisualTextHintingMode
        {
            get
            {
                VerifyAPIReadOnly();

                return TextHintingModeField.GetValue(this);
            }
            set
            {
                VerifyAPIReadWrite();

                if (TextHintingModeField.GetValue(this) == value)
                {
                    return;
                }

                TextHintingModeField.SetValue(this, value);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsTextHintingModeDirty);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// OpacityMask Property -
        /// Gets or sets the optional OpacityMask.  If set, the Brush's opacity will
        /// be combined multiplicitively with the Visual's rendered content.
        /// </summary>
        protected internal Brush VisualOpacityMask
        {
            get
            {
                VerifyAPIReadOnly();

                return OpacityMaskField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite(value);

                Brush opacityMask = OpacityMaskField.GetValue(this);
                if (opacityMask == value)
                {
                    return;
                }

                Brush newOpacityMask = value;

                // Add changed notifications for the new opacity mask if necessary.
                if (newOpacityMask != null && !newOpacityMask.IsFrozen)
                {
                    newOpacityMask.Changed += OpacityMaskChangedHandler;
                }

                if (opacityMask != null)
                {
                    //
                    // Remove changed notifications for the old opacity mask if necessary.
                    //

                    if (!opacityMask.IsFrozen)
                    {
                        opacityMask.Changed -= OpacityMaskChangedHandler;
                    }

                }

                //
                // Set the new opacity mask and mark it dirty
                //

                OpacityMaskField.SetValue(this, newOpacityMask);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsOpacityMaskDirty);

                OpacityMaskChanged(sender: null, args: null);
            }
        }


        /// <summary>
        /// Gets or sets X- (vertical) guidelines on this Visual.
        /// </summary>
        protected internal DoubleCollection VisualXSnappingGuidelines
        {
            get
            {
                VerifyAPIReadOnly();

                return GuidelinesXField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite(value);

                DoubleCollection guidelines = GuidelinesXField.GetValue(this);
                if (guidelines == value)
                {
                    return;
                }

                DoubleCollection newGuidelines = value;

                // Add changed notifications for the new guidelines if necessary.
                if (newGuidelines != null && !newGuidelines.IsFrozen)
                {
                    newGuidelines.Changed += GuidelinesChangedHandler;
                }

                // Remove changed notifications for the old guidelines if necessary.
                if (guidelines != null && !guidelines.IsFrozen)
                {
                    guidelines.Changed -= GuidelinesChangedHandler;
                }

                GuidelinesXField.SetValue(this, newGuidelines);

                GuidelinesChanged(sender: null, args: null);
            }
        }


        /// <summary>
        /// Gets or sets Y- (horizontal) guidelines of this Visual.
        /// </summary>
        protected internal DoubleCollection VisualYSnappingGuidelines
        {
            get
            {
                VerifyAPIReadOnly();

                return GuidelinesYField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite(value);

                DoubleCollection guidelines = GuidelinesYField.GetValue(this);
                if (guidelines == value)
                {
                    return;
                }

                DoubleCollection newGuidelines = value;

                // Add changed notifications for the new guidelines if necessary.
                if (newGuidelines != null && !newGuidelines.IsFrozen)
                {
                    newGuidelines.Changed += GuidelinesChangedHandler;
                }

                // Remove changed notifications for the old guidelines if necessary.
                if (guidelines != null && !guidelines.IsFrozen)
                {
                    guidelines.Changed -= GuidelinesChangedHandler;
                }

                GuidelinesYField.SetValue(this, newGuidelines);

                GuidelinesChanged(sender: null, args: null);
            }
        }

        #endregion Visual Properties






        /// <summary>
        /// GetDrawing - Returns the Drawing content of this Visual
        /// </summary>
        internal virtual DrawingGroup GetDrawing()
        {
            VerifyAPIReadOnly();

            // Default implementation returns null for Visual's that
            // don't have drawings
            return null;
        }



        // --------------------------------------------------------------------
        //
        //   Visual Ancestry Relations
        //
        // --------------------------------------------------------------------

        #region Visual Ancestry Relations

        /// <summary>
        /// This is called when the parent link of the Visual is changed.
        /// This method executes important base functionality before calling the
        /// overridable virtual.
        /// </summary>
        /// <param name="oldParent">Old parent or null if the Visual did not have a parent before.</param>
        internal virtual void FireOnVisualParentChanged(DependencyObject oldParent)
        {
            // Call the ParentChanged virtual before firing the Ancestor Changed Event
            OnVisualParentChanged(oldParent);

            // Clean up bits when the tree is Cut or Pasted.

            // If we are attaching to a tree then
            // send the bit up if we need to.
            if(oldParent == null)
            {
                Debug.Assert(_parent != null, "If oldParent is null, current parent should != null.");

                if(CheckFlagsAnd(VisualFlags.SubTreeHoldsAncestorChanged))
                {
                    SetTreeBits(
                        _parent,
                        VisualFlags.SubTreeHoldsAncestorChanged,
                        VisualFlags.RegisteredForAncestorChanged);
                }
            }
            // If we are cutting a sub tree off then
            // clear the bit in the main tree above if we need to.
            else
            {
                if(CheckFlagsAnd(VisualFlags.SubTreeHoldsAncestorChanged))
                {
                    ClearTreeBits(
                        oldParent,
                        VisualFlags.SubTreeHoldsAncestorChanged,
                        VisualFlags.RegisteredForAncestorChanged);
                }
            }

            // Fire the Ancestor changed Event on the nodes.
            AncestorChangedEventArgs args = new AncestorChangedEventArgs(this, oldParent);
            ProcessAncestorChangedNotificationRecursive(this, args);
        }


        /// <summary>
        /// OnVisualParentChanged is called when the parent of the Visual is changed.
        /// </summary>
        /// <param name="oldParent">Old parent or null if the Visual did not have a parent before.</param>
        protected internal virtual void OnVisualParentChanged(DependencyObject oldParent)
        {
        }

        /// <summary>
        /// OnVisualChildrenChanged is called when the VisualCollection of the Visual is edited.
        /// </summary>
        protected internal virtual void OnVisualChildrenChanged(
            DependencyObject visualAdded,
            DependencyObject visualRemoved)
        {
        }

        /// <summary>
        /// OnDpiChanged is called when the DPI at which this visual is rendered, changes.
        /// </summary>
        protected virtual void OnDpiChanged(
            DpiScale oldDpi,
            DpiScale newDpi)
        {
        }


        /// <summary>
        ///   Add removed delegates to the VisualAncenstorChanged Event.
        /// </summary>
        /// <remarks>
        ///     This also sets/clears the tree-searching bit up the tree
        /// </remarks>
        internal event AncestorChangedEventHandler VisualAncestorChanged
        {
            add
            {
                AncestorChangedEventHandler newHandler = AncestorChangedEventField.GetValue(this);

                if (newHandler == null)
                {
                    newHandler = value;
                }
                else
                {
                    newHandler += value;
                }

                AncestorChangedEventField.SetValue(this, newHandler);

                SetTreeBits(
                    this,
                    VisualFlags.SubTreeHoldsAncestorChanged,
                    VisualFlags.RegisteredForAncestorChanged);
            }

            remove
            {
                // check that we are Disabling a node that was previously Enabled
                if(CheckFlagsAnd(VisualFlags.SubTreeHoldsAncestorChanged))
                {
                    ClearTreeBits(
                        this,
                        VisualFlags.SubTreeHoldsAncestorChanged,
                        VisualFlags.RegisteredForAncestorChanged);
                }

                // if we are Disabling a Visual that was not Enabled then this
                // search should fail.  But it is safe to check.
                AncestorChangedEventHandler newHandler = AncestorChangedEventField.GetValue(this);

                if (newHandler != null)
                {
                    newHandler -= value;

                    if(newHandler == null)
                    {
                        AncestorChangedEventField.ClearValue(this);
                    }
                    else
                    {
                        AncestorChangedEventField.SetValue(this, newHandler);
                    }
                }
            }
        }


        /// <summary>
        ///     Walks down in the tree for nodes that have AncestorChanged Handlers
        ///     registered and calls them.
        ///     It uses Flag bits that help it prune the walk.  This should go
        ///     straight to the relevent nodes.
        /// </summary>
        internal static void ProcessAncestorChangedNotificationRecursive(DependencyObject e, AncestorChangedEventArgs args)
        {
            if (e is Visual)
            {
                Visual eAsVisual = e as Visual;

                // If the flag is not set, then we are Done.
                if(!eAsVisual.CheckFlagsAnd(VisualFlags.SubTreeHoldsAncestorChanged))
                {
                    return;
                }

                // If there is a handler on this node, then fire it.
                AncestorChangedEventHandler handler = AncestorChangedEventField.GetValue(eAsVisual);

                if(handler != null)
                {
                    handler(eAsVisual, args);
                }

                // Decend into the children.
                int count = eAsVisual.InternalVisual2DOr3DChildrenCount;

                for (int i = 0; i < count; i++)
                {
                    DependencyObject childVisual = eAsVisual.InternalGet2DOr3DVisualChild(i);
                    if (childVisual != null)
                    {
                        ProcessAncestorChangedNotificationRecursive(childVisual, args);
                    }
                }
            }
            else if (e is Media3D.Visual3D eAsVisual3D)
            {
                Media3D.Visual3D.ProcessAncestorChangedNotificationRecursive(eAsVisual3D, args);
            }
        }


        /// <summary>
        /// Returns true if the specified ancestor (this) is really the ancestor of the
        /// given descendant (argument).
        /// </summary>
        public bool IsAncestorOf(DependencyObject descendant)
        {
            Visual visual;
            Media3D.Visual3D visual3D;

            VisualTreeUtils.AsNonNullVisual(descendant, out visual, out visual3D);

            if (visual != null)
            {
                return visual.IsDescendantOf(this);
            }

            return visual3D.IsDescendantOf(this);
        }

        /// <summary>
        /// Returns true if the refernece Visual (this) is a descendant of the argument Visual.
        /// </summary>
        public bool IsDescendantOf(DependencyObject ancestor)
        {
            ArgumentNullException.ThrowIfNull(ancestor);

            VisualTreeUtils.EnsureVisual(ancestor);

            // Walk up the parent chain of the descendant until we run out
            // of parents or we find the ancestor.
            DependencyObject current = this;

            while ((current != null) && (current != ancestor))
            {
                if (current is Visual currentAsVisual)
                {
                    current = currentAsVisual._parent;
                }
                else if (current is Media3D.Visual3D currentAsVisual3D)
                {
                    current = currentAsVisual3D.InternalVisualParent;
                }
                else
                {
                    current = null;
                }
            }

            return current == ancestor;
        }


        /// <summary>
        ///     Walks up the Visual tree setting or clearing the given flags.  Unlike
        ///     PropagateFlags this does not terminate when it reaches node with
        ///     the flags already set.  It always walks all the way to the root.
        /// </summary>
        internal void SetFlagsToRoot(bool value, VisualFlags flag)
        {
            DependencyObject current = this;

            do
            {
                if (current is Visual currentAsVisual)
                {
                    currentAsVisual.SetFlags(value, flag);
                    current = currentAsVisual._parent;
                }
                else if (current is Media3D.Visual3D currentAsVisual3D)
                {
                    currentAsVisual3D.SetFlagsToRoot(value, flag);
                    return;
                }
                else
                {
                    current = null;
                }
            }
            while (current != null);
        }


        /// <summary>
        ///     Finds the first ancestor of the given element which has the given
        ///     flags set.
        /// </summary>
        internal DependencyObject FindFirstAncestorWithFlagsAnd(VisualFlags flag)
        {
            DependencyObject current = this;

            do
            {
                if (current is Visual currentAsVisual)
                {
                    if (currentAsVisual.CheckFlagsAnd(flag))
                    {
                        // The other Visual crossed through this Visual's parent chain. Hence this is our
                        // common ancestor.
                        return currentAsVisual;
                    }

                    current = currentAsVisual._parent;
                }
                else if (current is Media3D.Visual3D currentAsVisual3D)
                {
                    return currentAsVisual3D.FindFirstAncestorWithFlagsAnd(flag);
                }
                else
                {
                    current = null;
                }
            }
            while (current != null);

            return null;
        }


        /// <summary>
        /// Finds the common ancestor of two Visuals.
        /// </summary>
        /// <returns>Returns the common ancestor if the Visuals have one or otherwise null.</returns>
        /// <exception cref="ArgumentNullException">If the argument is null.</exception>
        public DependencyObject FindCommonVisualAncestor(DependencyObject otherVisual)
        {
            VerifyAPIReadOnly(otherVisual);

            ArgumentNullException.ThrowIfNull(otherVisual);

            // Since we can't rely on code running in the CLR, we need to first make sure
            // that the FindCommonAncestor flag is not set. It is enought to ensure this
            // on one path to the root Visual.

            // Later, when we get from the CLR the "RunForSure" section support, we can replace
            // this algorithm with one that is linear in the distance of the two visuals to
            // their common ancestor.

            SetFlagsToRoot(false, VisualFlags.FindCommonAncestor);

            // Walk up the other visual's parent chain and set the FindCommonAncestor flag.
            VisualTreeUtils.SetFlagsToRoot(otherVisual, true, VisualFlags.FindCommonAncestor);

            // Now see if the other Visual's parent chain crosses our parent chain.
            return FindFirstAncestorWithFlagsAnd(VisualFlags.FindCommonAncestor);
        }

        #endregion Visual Ancestry Relations

        #region ForceInherit property support

        internal virtual void InvalidateForceInheritPropertyOnChildren(DependencyProperty property)
        {
            InvalidateForceInheritHook?.Invoke(this, property);
        }

        #endregion ForceInherit property support

        // --------------------------------------------------------------------
        //
        //   Visual-to-Visual Transforms
        //
        // --------------------------------------------------------------------

        #region Visual-to-Visual Transforms

        /// <summary>
        /// Returns a transform that can be used to transform coordinate from this
        /// node to the specified ancestor.  It allows 3D to be between the 2D nodes.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// If ancestor is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the ancestor Visual is not a ancestor of Visual.
        /// </exception>
        /// <exception cref="InvalidOperationException">If the Visuals are not connected.</exception>
        public GeneralTransform TransformToAncestor(
            Visual ancestor)
        {
            ArgumentNullException.ThrowIfNull(ancestor);

            VerifyAPIReadOnly(ancestor);

            return InternalTransformToAncestor(ancestor, false);
        }


        /// <summary>
        /// Returns a transform that can be used to transform coordinates from this
        /// node to the specified descendant, or null if the transform from descendant to "this"
        /// is non-invertible.  It allows 3D to be between the 2D nodes.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If the reference Visual is not a ancestor of the descendant Visual.
        /// </exception>
        /// <exception cref="ArgumentNullException">If the descendant argument is null.</exception>
        /// <exception cref="InvalidOperationException">If the Visuals are not connected.</exception>
        public GeneralTransform TransformToDescendant(Visual descendant)
        {
            ArgumentNullException.ThrowIfNull(descendant);

            VerifyAPIReadOnly(descendant);

            return descendant.InternalTransformToAncestor(this, true);
        }

        public GeneralTransform2DTo3D TransformToAncestor(Visual3D ancestor)
        {
            ArgumentNullException.ThrowIfNull(ancestor);

            VerifyAPIReadOnly(ancestor);

            return InternalTransformToAncestor(ancestor);
        }

        internal GeneralTransform2DTo3D InternalTransformToAncestor(Visual3D ancestor)
        {
            Debug.Assert(ancestor != null);

            Viewport2DVisual3D containingVisual3D = VisualTreeHelper.GetContainingVisual3D(this) as Viewport2DVisual3D;
            if (containingVisual3D == null || containingVisual3D.Visual == null)
            {
                throw new InvalidOperationException(SR.Visual_NotAnAncestor);
            }

            GeneralTransform transform2D = (this == containingVisual3D.Visual)
                ? Transform.Identity
                : this.TransformToAncestor(containingVisual3D.Visual);
            GeneralTransform3D transform3D = containingVisual3D.TransformToAncestor(ancestor);

            if (transform2D == null || transform3D == null)
            {
                return null;
            }

            GeneralTransform2DTo3D result = new GeneralTransform2DTo3D(transform2D, containingVisual3D, transform3D);
            result.Freeze();

            return result;
        }


        /// <summary>
        /// The returned matrix can be used to transform coordinates from this Visual to
        /// the specified Visual.
        /// Returns null if no such transform exists due to a non-invertible Transform.
        /// </summary>
        /// <exception cref="ArgumentNullException">If visual is null.</exception>
        /// <exception cref="InvalidOperationException">If the Visuals are not connected.</exception>
        public GeneralTransform TransformToVisual(Visual visual)
        {
            DependencyObject ancestor = FindCommonVisualAncestor(visual);
            Visual ancestorAsVisual = ancestor as Visual;

            if (ancestorAsVisual == null)
            {
                throw new System.InvalidOperationException(SR.Visual_NoCommonAncestor);
            }

            GeneralTransform g0;
            Matrix m0;

            bool isSimple0 = this.TrySimpleTransformToAncestor(ancestorAsVisual,
                                                               false,
                                                               out g0,
                                                               out m0);

            GeneralTransform g1;
            Matrix m1;

            bool isSimple1 = visual.TrySimpleTransformToAncestor(ancestorAsVisual,
                                                                 true,
                                                                 out g1,
                                                                 out m1);

            // combine the transforms
            // if both transforms are simple Matrix transforms, just multiply them and
            // return the result.
            if (isSimple0 && isSimple1)
            {
                MatrixUtil.MultiplyMatrix(ref m0, ref m1);
                MatrixTransform m = new MatrixTransform(m0);
                m.Freeze();
                return m;
            }

            // Handle the case where 0 is simple and 1 is complex.
            if (isSimple0)
            {
                g0 = new MatrixTransform(m0);
                g0.Freeze();
            }
            else if (isSimple1)
            {
                g1 = new MatrixTransform(m1);
                g1.Freeze();
            }

            // If inverse was requested, TrySimpleTransformToAncestor can return null
            // add the transform only if it is not null
            if (g1 != null)
            {
                GeneralTransformGroup group = new GeneralTransformGroup();
                group.Children.Add(g0);
                group.Children.Add(g1);
                group.Freeze();
                return group;
            }

            return g0;
        }

        /// <summary>
        /// Returns the transform or the inverse transform between this visual and the specified ancestor.
        /// If inverse is requested but does not exist (if the transform is not invertible), null is returned.
        /// </summary>
        /// <param name="ancestor">Ancestor visual.</param>
        /// <param name="inverse">Returns inverse if this argument is true.</param>
        private GeneralTransform InternalTransformToAncestor(Visual ancestor, bool inverse)
        {
            GeneralTransform generalTransform;
            Matrix simpleTransform;

            bool isSimple = TrySimpleTransformToAncestor(ancestor,
                                                         inverse,
                                                         out generalTransform,
                                                         out simpleTransform);

            if (isSimple)
            {
                MatrixTransform matrixTransform = new MatrixTransform(simpleTransform);
                matrixTransform.Freeze();
                return matrixTransform;
            }
            else
            {
                return generalTransform;
            }
        }

        /// <summary>
        /// Provides the transform or the inverse transform between this visual and the specified ancestor.
        /// Returns true if the transform is "simple" - in which case the GeneralTransform is null
        /// and the caller should use the Matrix.
        /// Otherwise, returns false - use the GeneralTransform and ignore the Matrix.
        /// If inverse is requested but not available (if the transform is not invertible), false is
        /// returned and the GeneralTransform is null.
        /// </summary>
        /// <param name="ancestor">Ancestor visual.</param>
        /// <param name="inverse">Returns inverse if this argument is true.</param>
        /// <param name="generalTransform">The GeneralTransform if this method returns false.</param>
        /// <param name="simpleTransform">The Matrix if this method returns true.</param>
        internal bool TrySimpleTransformToAncestor(Visual ancestor,
                                                   bool inverse,
                                                   out GeneralTransform generalTransform,
                                                   out Matrix simpleTransform)
        {
            Debug.Assert(ancestor != null);

            DependencyObject g = this;
            Matrix m = Matrix.Identity;

            // Keep this null until it's needed
            GeneralTransformGroup group = null;

            // This while loop will walk up the visual tree until we encounter the ancestor.
            // As it does so, it will accumulate the descendent->ancestor transform.
            // In most cases, this is simply a matrix, though if we encounter a bitmap effect we
            // will need to use a general transform group to store the transform.
            // We will accumulate the current transform in a matrix until we encounter a bitmap effect,
            // at which point we will add the matrix's current value and the bitmap effect's transforms
            // to the GeneralTransformGroup and continue to accumulate further transforms in the matrix again.
            // At the end of this loop, we will have 0 or more transforms in the GeneralTransformGroup
            // and the matrix which, if not identity, should be appended to the GeneralTransformGroup.
            // If, as is commonly the case, this loop terminates without encountering a bitmap effect
            // we will simply use the Matrix.

            while ((VisualTreeHelper.GetParent(g) != null) && (g != ancestor))
            {
                Visual gAsVisual = g as Visual;
                if (gAsVisual != null)
                {
                    if (gAsVisual._parent is Viewport2DVisual3D v2dv3d)
                    {
                        if (group == null)
                        {
                            group = new GeneralTransformGroup();
                        }

                        if (!m.IsIdentity)
                        {
                            group.Children.Add(new MatrixTransform(m));
                            m = Matrix.Identity;
                        }

                        group.Children.Add(new MS.Internal.Media3D.GeneralTransform2DTo3DTo2D(v2dv3d, gAsVisual));
                        g = VisualTreeHelper.GetContainingVisual2D(v2dv3d);
                        continue;
                    }

                    if (gAsVisual.CheckFlagsAnd(VisualFlags.NodeHasEffect))
                    {
                        // Only check for Effect, not legacy BitmapEffect.  Previous
                        // version had an incorrect BitmapEffect implementation
                        // here, and there's no need to improve on our
                        // BitmapEffect implementation if it didn't work
                        // before.

                        Effect imageEffect = EffectField.GetValue(gAsVisual);
                        if (imageEffect != null)
                        {
                            GeneralTransform gt = imageEffect.CoerceToUnitSpaceGeneralTransform(
                                imageEffect.EffectMapping,
                                gAsVisual.VisualDescendantBounds);

                            Transform affineTransform = gt.AffineTransform;
                            if (affineTransform != null)
                            {
                                Matrix cm = affineTransform.Value;
                                MatrixUtil.MultiplyMatrix(ref m, ref cm);
                            }
                            else
                            {
                                if (group == null)
                                {
                                    group = new GeneralTransformGroup();
                                }

                                group.Children.Add(new MatrixTransform(m));
                                m = Matrix.Identity;

                                group.Children.Add(gt);
                            }
                        }
                    }

                    Transform transform = TransformField.GetValue(gAsVisual);
                    if (transform != null)
                    {
                        Matrix cm = transform.Value;
                        MatrixUtil.MultiplyMatrix(ref m, ref cm);
                    }
                    m.Translate(gAsVisual._offset.X, gAsVisual._offset.Y); // Consider having a bit that indicates that we have a non-null offset.
                    g = gAsVisual._parent;
                }
                else
                {
                    g = null;
                }
            }

            if (g != ancestor)
            {
                throw new System.InvalidOperationException(inverse ? SR.Visual_NotADescendant : SR.Visual_NotAnAncestor);
            }

            // At this point, we will have 0 or more transforms in the GeneralTransformGroup
            // and the matrix which, if not identity, should be appended to the GeneralTransformGroup.
            // If, as is commonly the case, this loop terminates without encountering a bitmap effect
            // we will simply use the Matrix.

            // Assert that a non-null group implies at least one child
            Debug.Assert((group == null) || (group.Children.Count > 0));

            // Do we have a group?
            if (group != null)
            {
                if (!m.IsIdentity)
                {
                    group.Children.Add(new MatrixTransform(m));
                }

                if (inverse)
                {
                    group = (GeneralTransformGroup)group.Inverse;
                }

                // group can be null if it does not have an inverse
                group?.Freeze();

                // Initialize out params
                generalTransform = group;
                simpleTransform = new Matrix();
                return false; // simple transform failed
            }
            // If not, the entire transform is stored in the matrix
            else
            {
                // Initialize out params
                generalTransform = null;

                if (inverse)
                {
                    if (!m.HasInverse)
                    {
                        simpleTransform = new Matrix();
                        return false; // inversion failed, so simple transform failed.
                    }

                    m.Invert();
                }

                simpleTransform = m;
                return true; // simple transform succeeded
            }
        }



        /// <summary>
        /// Returns the DPI information at which this Visual is rendered.
        /// </summary>
        internal DpiScale GetDpi()

        {

            DpiScale dpi;

            lock (DpiUtil.DpiLock)

            {

                if (DpiUtil.DpiScaleXValues.Count == 0)

                {

                    return EnsureDpiScaleFromDpiUtil();

                }



                dpi = new DpiScale(DpiUtil.DpiScaleXValues[0], DpiUtil.DpiScaleYValues[0]);

                int index = 0;

                index = CheckFlagsAnd(VisualFlags.DpiScaleFlag1) ? index | 1 : index;

                index = CheckFlagsAnd(VisualFlags.DpiScaleFlag2) ? index | 2 : index;



                if (index < 3 && index < DpiUtil.DpiScaleXValues.Count && DpiUtil.DpiScaleXValues[index] != 0 && DpiUtil.DpiScaleYValues[index] != 0)

                {

                    dpi = new DpiScale(DpiUtil.DpiScaleXValues[index], DpiUtil.DpiScaleYValues[index]);

                }

                else if (index == 3)

                {

                    int actualIndex = DpiIndex.GetValue(this);

                    if (actualIndex >= 0 && actualIndex < DpiUtil.DpiScaleXValues.Count)

                    {

                        dpi = new DpiScale(DpiUtil.DpiScaleXValues[actualIndex], DpiUtil.DpiScaleYValues[actualIndex]);

                    }

                }

            }

            return dpi;

        }


        /// <summary>
        /// This method converts a point in the current Visual's coordinate
        /// system into a point in screen coordinates.
        /// </summary>
        public Point PointToScreen(Point point)
        {
            VerifyAPIReadOnly();

            PresentationSource source = PresentationSourceFromVisualHook?.Invoke(this) as PresentationSource;
            if (source == null || source.RootVisual == null || PointToScreenHook == null)
            {
                throw new InvalidOperationException(SR.Visual_NoPresentationSource);
            }

            // First, convert the point into the root visual's coordinate space.
            GeneralTransform gUp = this.TransformToAncestor(source.RootVisual);
            if (gUp == null || !gUp.TryTransform(point, out point))
            {
                throw new InvalidOperationException(SR.Visual_CannotTransformPoint);
            }

            // Now convert from the root visual's coordinate space to screen coordinates.
            return PointToScreenHook(point, source);
        }


        /// <summary>
        /// This method converts a point in screen coordinates into a point
        /// in the current Visual's coordinate system.
        /// </summary>
        public Point PointFromScreen(Point point)
        {
            VerifyAPIReadOnly();

            PresentationSource source = PresentationSourceFromVisualHook?.Invoke(this) as PresentationSource;
            if (source == null || source.RootVisual == null || PointFromScreenHook == null)
            {
                throw new InvalidOperationException(SR.Visual_NoPresentationSource);
            }

            // First, convert from screen coordinates to the root visual's coordinate space.
            point = PointFromScreenHook(point, source);

            // Now convert from the root visual's coordinate space to this visual's coordinate space.
            GeneralTransform gDown = source.RootVisual.TransformToDescendant(this);
            if (gDown == null || !gDown.TryTransform(point, out point))
            {
                throw new InvalidOperationException(SR.Visual_CannotTransformPoint);
            }

            return point;
        }


        #endregion Visual-to-Visual Transforms



        // --------------------------------------------------------------------
        //
        //   Internal Event Handlers
        //
        // --------------------------------------------------------------------

        #region Internal Event Handlers

        internal EventHandler ClipChangedHandler
        {
            get
            {
                return new EventHandler(ClipChanged);
            }
        }

        internal void ClipChanged(object sender, EventArgs args)
        {
            PropagateChangedFlags();
        }

        internal EventHandler ScrollableAreaClipChangedHandler
        {
            get
            {
                return new EventHandler(ScrollableAreaClipChanged);
            }
        }

        internal void ScrollableAreaClipChanged(object sender, EventArgs args)
        {
            PropagateChangedFlags();
        }

        internal EventHandler TransformChangedHandler
        {
            get
            {
                return new EventHandler(TransformChanged);
            }
        }

        internal void TransformChanged(object sender, EventArgs args)
        {
            PropagateChangedFlags();
        }


        internal EventHandler EffectChangedHandler
        {
            get
            {
                return new EventHandler(EffectChanged);
            }
        }

        internal void EffectChanged(object sender, EventArgs args)
        {
            PropagateChangedFlags();
        }

        internal EventHandler CacheModeChangedHandler
        {
            get
            {
                return new EventHandler(EffectChanged);
            }
        }

        internal void CacheModeChanged(object sender, EventArgs args)
        {
            PropagateChangedFlags();
        }

        internal EventHandler GuidelinesChangedHandler
        {
            get
            {
                return new EventHandler(GuidelinesChanged);
            }
        }

        internal void GuidelinesChanged(object sender, EventArgs args)
        {
            SetFlagsOnAllChannels(
                true,
                VisualProxyFlags.IsGuidelineCollectionDirty);

            PropagateChangedFlags();
        }

        internal EventHandler OpacityMaskChangedHandler
        {
            get
            {
                return new EventHandler(OpacityMaskChanged);
            }
        }

        internal void OpacityMaskChanged(object sender, EventArgs args)
        {
            PropagateChangedFlags();
        }

        internal EventHandler ContentsChangedHandler
        {
            get
            {
                return new EventHandler(ContentsChanged);
            }
        }

        internal virtual void ContentsChanged(object sender, EventArgs args)
        {
            PropagateChangedFlags();
        }

        #endregion Internal Event Handlers



        // --------------------------------------------------------------------
        //
        //   Visual flags manipulation
        //
        // --------------------------------------------------------------------

        #region Visual flags manipulation



        /// <summary>
        /// SetFlags is used to set or unset one or multiple node flags on the node.
        /// </summary>
        internal void SetFlags(bool value, VisualFlags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        /// <summary>
        /// Sets the DPI scale Visual flags on the current visual.
        /// </summary>
        internal void SetDpiScaleVisualFlags(DpiRecursiveChangeArgs args)
        {
            _flags = args.DpiScaleFlag1 ? (_flags | VisualFlags.DpiScaleFlag1) : (_flags & ~VisualFlags.DpiScaleFlag1);
            _flags = args.DpiScaleFlag2 ? (_flags | VisualFlags.DpiScaleFlag2) : (_flags & ~VisualFlags.DpiScaleFlag2);
            if (args.DpiScaleFlag1 && args.DpiScaleFlag2)
            {
                DpiIndex.SetValue(this, args.Index);
            }

            if (!args.OldDpiScale.Equals(args.NewDpiScale))
            {
                OnDpiChanged(args.OldDpiScale, args.NewDpiScale);
            }
        }

        /// <summary>
        /// Recursively sets the DPI scale visual flags.
        /// </summary>
        internal void RecursiveSetDpiScaleVisualFlags(DpiRecursiveChangeArgs args)
        {
            SetDpiScaleVisualFlags(args);
            int count = InternalVisualChildrenCount;
            for (int i = 0; i < count; i++)
            {
                Visual cv = InternalGetVisualChild(i);
                cv?.RecursiveSetDpiScaleVisualFlags(args);
            }
        }




        /// <summary>
        /// CheckFlagsAnd returns true if all flags in the bitmask flags are set on the node.
        /// </summary>
        /// <remarks>If there aren't any bits set on the specified flags the method
        /// returns true</remarks>
        internal bool CheckFlagsAnd(VisualFlags flags)
        {
            return (_flags & flags) == flags;
        }

        /// <summary>
        /// Checks if any of the specified flags is set on the node.
        /// </summary>
        /// <remarks>If there aren't any bits set on the specified flags the method
        /// returns true</remarks>
        internal bool CheckFlagsOr(VisualFlags flags)
        {
            return (flags == 0) || ((_flags & flags) > 0);
        }

        /// <summary>
        ///     Set a bit in a Visual node and in all its direct ancestors.
        /// </summary>
        /// <param name="e">The Visual Element</param>
        /// <param name="treeFlag">The Flag that marks a sub tree to search</param>
        /// <param name="nodeFlag">The Flag that marks the node to search for.</param>
        internal static void SetTreeBits(
            DependencyObject e,
            VisualFlags treeFlag,
            VisualFlags nodeFlag)
        {
            Visual eAsVisual = e as Visual;
            if (eAsVisual != null)
            {
                eAsVisual.SetFlags(true, nodeFlag);
            }

            while (e != null)
            {
                eAsVisual = e as Visual;
                if (eAsVisual != null)
                {
                    // if the bit is already set, then we're done.
                    if (eAsVisual.CheckFlagsAnd(treeFlag))
                        return;

                    eAsVisual.SetFlags(true, treeFlag);
                }

                e = VisualTreeHelper.GetParent(e);
            }
        }


        /// <summary>
        ///     Clean a bit in a Visual node and in all its direct ancestors;
        ///     unless the ancestor also has
        /// </summary>
        /// <param name="e">The Visual Element</param>
        /// <param name="treeFlag">The Flag that marks a sub tree to search</param>
        /// <param name="nodeFlag">The Flag that marks the node to search for.</param>
        internal static void ClearTreeBits(
            DependencyObject e,
            VisualFlags treeFlag,
            VisualFlags nodeFlag)
        {
            Visual eAsVisual = e as Visual;

            // This bit might not be set, but checking costs as much as setting
            // So it is faster to just clear it everytime.
            if (eAsVisual != null)
            {
                eAsVisual.SetFlags(false, nodeFlag);
            }

            while (e != null)
            {
                eAsVisual = e as Visual;
                if (eAsVisual != null)
                {
                    if (eAsVisual.CheckFlagsAnd(nodeFlag))
                    {
                        return;  // Done;   if a parent also has the Node bit set.
                    }

                    if (DoAnyChildrenHaveABitSet(eAsVisual, treeFlag))
                    {
                        return;  // Done;   if a other subtrees are set.
                    }

                    eAsVisual.SetFlags(false, treeFlag);
                }

                e = VisualTreeHelper.GetParent(e);
            }
        }


        /// <summary>
        ///     Check all the children for a bit.
        /// </summary>
        private static bool DoAnyChildrenHaveABitSet(
            Visual pe,
            VisualFlags flag)
        {
            int count = pe.InternalVisual2DOr3DChildrenCount;
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = pe.InternalGet2DOr3DVisualChild(i);
                Visual v;
                Media3D.Visual3D v3d;
                VisualTreeUtils.AsNonNullVisual(child, out v, out v3d);
                if (v != null && v.CheckFlagsAnd(flag))
                {
                    return true;
                }
                else if (v3d != null && v3d.CheckFlagsAnd(flag))
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Propagates the flags up to the root.
        /// </summary>
        /// <remarks>
        /// The walk stops on a node with all of the required flags set.
        /// </remarks>
        internal static void PropagateFlags(

            Visual e,

            VisualFlags flags,

            VisualProxyFlags proxyFlags)

        {

            while ((e != null) &&

                   (!e.CheckFlagsAnd(flags) || !e.CheckFlagsOnAllChannels(proxyFlags)))

            {

                if (e.CheckFlagsOr(VisualFlags.ShouldPostRender))

                {

                    MediaContext mctx = MediaContext.From(e.Dispatcher);

                    mctx?.PostRender();

                }

                else if (e.CheckFlagsAnd(VisualFlags.NodeIsCyclicBrushRoot))

                {

                    Dictionary<ICyclicBrush, int> cyclicBrushToChannelsMap =

                        CyclicBrushToChannelsMapField.GetValue(e);



                    if (cyclicBrushToChannelsMap != null)

                    {

                        foreach (ICyclicBrush cyclicBrush in cyclicBrushToChannelsMap.Keys)

                        {

                            cyclicBrush.FireOnChanged();

                        }

                    }

                }



                e.SetFlags(true, flags);

                e.SetFlagsOnAllChannels(true, proxyFlags);



                if (e._parent == null)

                {

                    return;

                }



                DependencyObject parent = e._parent;
                e = parent as Visual;
                if (e == null && parent is Media3D.Visual3D parent3D)
                {
                    Media3D.Visual3D.PropagateFlags(parent3D, flags, proxyFlags);
                    return;
                }

            }

        }


        /// <summary>
        /// Propagates the dirty flags up to the root.
        /// </summary>
        /// <remarks>
        /// The walk stops on a node with all of the required flags set.
        /// </remarks>
        internal void PropagateChangedFlags()
        {
            PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);
        }




        #endregion Visual flags manipulation

        // --------------------------------------------------------------------
        //
        //   Internal Fields
        //
        // --------------------------------------------------------------------

        #region Internal Fields


        internal delegate void AncestorChangedEventHandler(object sender, AncestorChangedEventArgs e);

        // index in parent child array. no meaning if parent is null.
        // note that we maintain in debug that the _parentIndex is -1 if the parent is null.
        // Exception: children added to TextBoxView and InkPresenter.
        internal int _parentIndex;

        // We may have to change the API so that we can save
        // here. For now that is good enough.
        internal DependencyObject _parent;


        #endregion Internal Fields



        // --------------------------------------------------------------------
        //
        //   Private Fields
        //
        // --------------------------------------------------------------------

        #region Private Fields

        // bbox in inner coordinate space of this node including its children.
        private Rect _bboxSubgraph = Rect.Empty;

        //
        // Store the cyclic brushes that hold on to this visual. Also store the corresponding
        // number of channel, on which that cyclic brush holds on to this visual.
        //
        private static readonly UncommonField<Dictionary<ICyclicBrush, int>> CyclicBrushToChannelsMapField
            = new UncommonField<Dictionary<ICyclicBrush, int>>();

        //
        // Store the channels on which cyclic brushes hold on to this visual. Also store the
        // corresponding number of cyclic brushes on that channel, holding on to this visual.
        //

        internal static readonly UncommonField<int> DpiIndex = new UncommonField<int>();
        private static readonly UncommonField<Geometry> ClipField = new UncommonField<Geometry>();
        private static readonly UncommonField<double> OpacityField = new UncommonField<double>(1.0);
        private static readonly UncommonField<Brush> OpacityMaskField = new UncommonField<Brush>();
        private static readonly UncommonField<EdgeMode> EdgeModeField = new UncommonField<EdgeMode>();
        private static readonly UncommonField<BitmapScalingMode> BitmapScalingModeField = new UncommonField<BitmapScalingMode>();
        private static readonly UncommonField<ClearTypeHint> ClearTypeHintField = new UncommonField<ClearTypeHint>();

        private static readonly UncommonField<Transform> TransformField = new UncommonField<Transform>();
        private static readonly UncommonField<Effect> EffectField = new UncommonField<Effect>();
        private static readonly UncommonField<CacheMode> CacheModeField = new UncommonField<CacheMode>();

        private static readonly UncommonField<DoubleCollection> GuidelinesXField = new UncommonField<DoubleCollection>();
        private static readonly UncommonField<DoubleCollection> GuidelinesYField = new UncommonField<DoubleCollection>();

        private static readonly UncommonField<AncestorChangedEventHandler> AncestorChangedEventField
            = new UncommonField<AncestorChangedEventHandler>();


        private static readonly UncommonField<Rect?> ScrollableAreaClipField = new UncommonField<Rect?>(null);

        private static readonly UncommonField<TextRenderingMode> TextRenderingModeField = new UncommonField<TextRenderingMode>();
        private static readonly UncommonField<TextHintingMode> TextHintingModeField = new UncommonField<TextHintingMode>();

        private Vector _offset;
        private VisualFlags _flags;

        private const uint TreeLevelLimit = 0x7FF;

        #endregion Private Fields
    }
}






