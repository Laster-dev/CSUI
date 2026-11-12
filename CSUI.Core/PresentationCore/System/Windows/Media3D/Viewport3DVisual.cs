// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MS.Internal;
using MS.Internal.Media3D;
using System.ComponentModel;
using System.Windows.Diagnostics;
using System.Windows.Markup;
using System.Windows.Media.Effects;

#nullable disable

using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     The Viewport3DVisual provides the Camera and viewport Rect
    ///     required to project the Visual3Ds into 2D.  The Viewport3DVisual
    ///     is the bridge between 2D visuals and 3D.
    /// </summary>
    [ContentProperty("Children")]
    public sealed class Viewport3DVisual : Visual, IVisual3DContainer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        ///     Default constructor
        /// </summary>
        public Viewport3DVisual() : base(false)
        {
            SetFlags(true, VisualFlags.IsViewport3DVisual);
            _children = new Visual3DCollection(this);
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        // ------------------------------------------------------------------------------------------
        // Publicly re-exposed VisualTreeHelper interfaces.
        //
        //     Note that we do not want to expose the Children property on Viewport3DVisual
        //     since the Viewport3DVisual provides its own set of Visual3D children.
        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public DependencyObject Parent
        {
            get { return base.VisualParent; }
        }

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public Geometry Clip
        {
            get { return base.VisualClip; }
            set { base.VisualClip = value; }
        }

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public double Opacity
        {
            get { return base.VisualOpacity; }
            set { base.VisualOpacity = value; }
        }

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public Brush OpacityMask
        {
            get { return base.VisualOpacityMask;  }
            set { base.VisualOpacityMask = value; }
        }

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public new HitTestResult HitTest(Point point)
        {
            return base.HitTest(point);
        }

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public new void HitTest(HitTestFilterCallback filterCallback, HitTestResultCallback resultCallback, HitTestParameters hitTestParameters)
        {
            base.HitTest(filterCallback, resultCallback, hitTestParameters);
        }

        /// <summary>
        /// VisualContentBounds returns the bounding box for the contents of this Visual.
        /// </summary>
        public Rect ContentBounds
        {
            get
            {
                return base.VisualContentBounds;
            }
        }

        /// <summary>
        /// Gets or sets the Transform property.
        /// </summary>
        public Transform Transform
        {
            get
            {
                return base.VisualTransform;
            }
            set
            {
                base.VisualTransform = value;
            }
        }

        /// <summary>
        /// Gets or sets the Offset property.
        /// </summary>
        public Vector Offset
        {
            get
            {
                return base.VisualOffset;
            }
            set
            {
                base.VisualOffset = value;
            }
        }

        /// <summary>
        /// DescendantBounds returns the union of all of the content bounding
        /// boxes for all of the descendants of the current visual, but not including
        /// the contents of the current visual.
        /// </summary>
        public Rect DescendantBounds
        {
            get
            {
                return base.VisualDescendantBounds;
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///    DependencyProperty which backs the ModelVisual3D.Camera property.
        /// </summary>
        public static readonly DependencyProperty CameraProperty =
            DependencyProperty.Register(
                    "Camera",
                    propertyType: typeof(Camera),
                    ownerType: typeof(Viewport3DVisual),
                    new PropertyMetadata(
                        (Camera)new PerspectiveCamera().GetAsFrozen(),
                        CameraPropertyChanged),
                    (ValidateValueCallback) delegate { return MediaContext.CurrentMediaContext.WriteAccessEnabled; });

        private static void CameraPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Viewport3DVisual owner = ((Viewport3DVisual) d);

            if (!e.IsASubPropertyChange)
            {
                owner.SetFlagsOnAllChannels(true, VisualProxyFlags.Viewport3DVisual_IsCameraDirty | VisualProxyFlags.IsContentDirty);
            }

            owner.ContentsChanged(sender: owner, EventArgs.Empty);
        }

        /// <summary>
        ///     Camera for this Visual3D.
        /// </summary>
        public Camera Camera
        {
            get
            {
                return (Camera) GetValue(CameraProperty);
            }

            set
            {
                SetValue(CameraProperty, value);
            }
        }

        /// <summary>
        ///    DependencyProperty which backs the ModelVisual3D.Viewport property.
        /// </summary>
        public static readonly DependencyProperty ViewportProperty =
            DependencyProperty.Register(
                    "Viewport",
                    propertyType: typeof(Rect),
                    ownerType: typeof(Viewport3DVisual),
                    new PropertyMetadata(Rect.Empty, ViewportPropertyChanged),
                    (ValidateValueCallback) delegate { return MediaContext.CurrentMediaContext.WriteAccessEnabled; });

        private static void ViewportPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Viewport3DVisual owner = ((Viewport3DVisual) d);

            Debug.Assert(!e.IsASubPropertyChange,
                "How are we receiving sub property changes from a struct?");

            owner.SetFlagsOnAllChannels(true, VisualProxyFlags.Viewport3DVisual_IsViewportDirty | VisualProxyFlags.IsContentDirty);
            owner.ContentsChanged(sender: owner, EventArgs.Empty);
        }

        /// <summary>
        ///     Viewport for this Visual3D.
        /// </summary>
        public Rect Viewport
        {
            get
            {
                return (Rect) GetValue(ViewportProperty);
            }

            set
            {
                SetValue(ViewportProperty, value);
            }
        }

        /// <summary>
        ///     The 3D children to be projected by this Viewport3DVisual.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Visual3DCollection Children
        {
            get
            {
                return _children;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // CS0536: Intreface implementation must be public or explicit so we re-expose
        // the internal methods as an explicit implementation of an internal interface.
        void IVisual3DContainer.VerifyAPIReadOnly() { this.VerifyAPIReadOnly(); }
        void IVisual3DContainer.VerifyAPIReadOnly(DependencyObject other) { this.VerifyAPIReadOnly(other); }
        void IVisual3DContainer.VerifyAPIReadWrite() { this.VerifyAPIReadWrite(); }
        void IVisual3DContainer.VerifyAPIReadWrite(DependencyObject other) { this.VerifyAPIReadWrite(other); }

        // NOTE:  The code here is highly similar to AddChildCore in ModelVisual3D,
        //        but slightly different because the parent is 2D here.
        void IVisual3DContainer.AddChild(Visual3D child)
        {
            // It is invalid to modify the children collection that we
            // might be iterating during a property invalidation tree walk.
            if (IsVisualChildrenIterationInProgress)
            {
                throw new InvalidOperationException(SR.CannotModifyVisualChildrenDuringTreeWalk);
            }

            // invalid during a VisualTreeChanged event
            VisualDiagnostics.VerifyVisualTreeChange(this);

            Debug.Assert(child != null);
            Debug.Assert(child.InternalVisualParent == null);

            child.SetParent(this);

            // set the inheritance context so databinding, etc... work
            _inheritanceContextForChildren?.ProvideSelfAsInheritanceContext(child, null);

            SetFlagsOnAllChannels(true, VisualProxyFlags.IsContentDirty);

            // The child already might be dirty. Hence we need to propagate dirty information
            // from the parent and from the child.
            Visual.PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);

            Visual3D.PropagateFlags(
                child,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);

            // We do not currently support layout in 3D
            // UIElement.PropagateResumeLayout(child);

            // Fire notifications
            OnVisualChildrenChanged(child, visualRemoved: null);

            child.FireOnVisualParentChanged(null);
            VisualDiagnostics.OnVisualChildChanged(this, child, true);
        }

        // NOTE:  The code here is highly similar to RemoveChildCore in ModelVisual3D,
        //        but slightly different because the parent is 2D here.
        void IVisual3DContainer.RemoveChild(Visual3D child)
        {
            int index = child.ParentIndex;

            // It is invalid to modify the children collection that we
            // might be iterating during a property invalidation tree walk.
            if (IsVisualChildrenIterationInProgress)
            {
                throw new InvalidOperationException(SR.CannotModifyVisualChildrenDuringTreeWalk);
            }

            // invalid during a VisualTreeChanged event
            VisualDiagnostics.VerifyVisualTreeChange(this);

            Debug.Assert(child != null);
            Debug.Assert(child.InternalVisualParent == this);

            VisualDiagnostics.OnVisualChildChanged(this, child, false);

            child.SetParent(newParent: (Visual) null);  // CS0121: Call is ambigious without casting null to Visual.

            // remove the inheritance context
            _inheritanceContextForChildren?.RemoveSelfAsInheritanceContext(child, null);

            child.SetFlagsOnAllChannels(false, VisualProxyFlags.IsConnectedToParent);

            SetFlagsOnAllChannels(true, VisualProxyFlags.IsContentDirty);

            //
            // Force a full precompute and render pass for this visual.
            //

            Visual.PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);

            // We do not currently support layout in 3D
            // UIElement.PropagateSuspendLayout(child);

            child.FireOnVisualParentChanged(this);

            OnVisualChildrenChanged(visualAdded: null , child);
        }

        /// <summary>
        ///     Gets the number of Visual3D children that the IVisual3DContainer
        ///     contains.
        /// </summary>
        int IVisual3DContainer.GetChildrenCount()
        {
            return InternalVisual2DOr3DChildrenCount;
        }

        /// <summary>
        ///     Gets the index children of the IVisual3DContainer
        /// </summary>
        Visual3D IVisual3DContainer.GetChild(int index)
        {
            return (Visual3D)InternalGet2DOr3DVisualChild(index);
        }

        /// <summary>
        /// Returns the number of children of this object (in most cases this will be
        /// the number of Visuals, but it some cases, Viewport3DVisual for instance,
        /// this is the number of Visual3Ds).
        ///
        /// Used only by VisualTreeHelper.
        /// </summary>
        internal override int InternalVisual2DOr3DChildrenCount
        {
            get
            {
                return Children.Count;
            }
        }

        /// <summary>
        /// Used only by VisualTreeHelper.
        ///
        /// Returns the child at index "index" (in most cases this will be
        /// a Visual, but it some cases, Viewport3DVisual, for instance,
        /// this is a Visual3D).
        /// </summary>
        internal override DependencyObject InternalGet2DOr3DVisualChild(int index)
        {
            return Children[index];
        }

        internal override HitTestResultBehavior HitTestPointInternal(
            HitTestFilterCallback filterCallback,
            HitTestResultCallback resultCallback,
            PointHitTestParameters hitTestParameters)
        {
            if (_children.Count != 0)
            {
                double distanceAdjustment;

                RayHitTestParameters rayParams =
                    Camera.RayFromViewportPoint(
                        hitTestParameters.HitPoint,
                        Viewport.Size,
                        BBoxSubgraph,
                        out distanceAdjustment);

                HitTestResultBehavior result = Visual3D.HitTestChildren(filterCallback, rayParams, this);

                return rayParams.RaiseCallback(resultCallback, filterCallback, result, distanceAdjustment);
            }

            return HitTestResultBehavior.Continue;
        }

        /// <summary>
        ///     Viewport3DVisual does not yet support Geometry hit testing.
        /// </summary>
        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            throw new NotSupportedException(SR.Format(SR.HitTest_Invalid, nameof(GeometryHitTestParameters), this.GetType().Name));
        }

        internal Point WorldToViewport(Point4D point)
        {
            double aspectRatio = M3DUtil.GetAspectRatio(Viewport.Size);
            Camera camera = Camera;

            if (camera != null)
            {
                Matrix3D viewProjMatrix = camera.GetViewMatrix() * camera.GetProjectionMatrix(aspectRatio);
                point *= viewProjMatrix;

                Point point2D = new Point(point.X/point.W, point.Y/point.W);
                point2D *= M3DUtil.GetHomogeneousToViewportTransform(Viewport);

                return point2D;
            }
            else
            {
                return new Point(0,0);
            }
        }

        /// <summary>
        /// Derived classes return the hit-test bounding box from the
        /// GetHitTestBounds virtual. Visual uses the bounds to optimize
        /// hit-testing.
        /// </summary>
        internal override Rect GetHitTestBounds()
        {
            return CalculateSubgraphBoundsInnerSpace();
        }

        internal override Rect CalculateSubgraphBoundsInnerSpace(bool renderBounds)
        {
            Camera camera = Camera;

            if (camera == null)
            {
                return Rect.Empty;
            }

            //
            // Cache the 3D bounding box for future use. Here we are relying on
            // the fact that this method is called by PrecomputeContent(),
            // which is called prior to all usages of _bboxChildrenSubgraph3D.
            //
            _bboxChildrenSubgraph3D = ComputeSubgraphBounds3D();

            if (_bboxChildrenSubgraph3D.IsEmpty)
            {
                // Attempting to project empty bounds will result in NaNs which
                // which will ruin descendant bounds for the 2D tree.  We handle
                // this explicitly and early exit with the correct answer.

                return Rect.Empty;
            }

            Rect viewport = Viewport;

            // Common Case: Viewport3DVisual in a collasped UIElement.
            if (viewport.IsEmpty)
            {
                // Creating a 3D homogenous space to 2D viewport space transform
                // with an empty rectangle will result in NaNs which ruin the
                // descendant bounds for the 2D tree.  We handle this explicitly
                // and early exit with the correct answer.
                //
                // (Usage of Visibility="Collapsed" attribute in avalon Viewport3D element triggers assertion failed: IsWellOrdered())

                return Rect.Empty;
            }

            double aspectRatio = M3DUtil.GetAspectRatio(viewport.Size);
            Matrix3D viewProjMatrix = camera.GetViewMatrix() * camera.GetProjectionMatrix(aspectRatio);
            Rect projectedBounds2D = MILUtilities.ProjectBounds(ref viewProjMatrix, ref _bboxChildrenSubgraph3D);
            Matrix homoToLocal = M3DUtil.GetHomogeneousToViewportTransform(viewport);
            MatrixUtil.TransformRect(ref projectedBounds2D, ref homoToLocal);

            return projectedBounds2D;
        }

        //
        // NOTE: Must only be called after PrecomputeContent().
        //
        private Rect3D BBoxSubgraph
        {
            get
            {
                Debug_VerifyCachedSubgraphBounds();

                return _bboxChildrenSubgraph3D;
            }
        }

        internal Rect3D ComputeSubgraphBounds3D()
        {
            Rect3D bboxChildrenSubgraph3D = Rect3D.Empty;

            for (int i = 0, count = _children.InternalCount; i < count; i++)
            {
                Visual3D child = _children.InternalGetItem(i);

                bboxChildrenSubgraph3D.Union(child.CalculateSubgraphBoundsOuterSpace());
            }

            return bboxChildrenSubgraph3D;
        }

        [Conditional("DEBUG")]
        private void Debug_VerifyCachedSubgraphBounds()
        {
            Rect3D currentBounds = Rect3D.Empty;
            currentBounds = ComputeSubgraphBounds3D();

            Rect3D cachedBounds = _bboxChildrenSubgraph3D;

            // The funny boolean logic below avoids asserts when the cached
            // bounds contain NaNs.  (NaN != NaN)
            bool boundsAreEqual =
                !(cachedBounds.X < currentBounds.X || cachedBounds.X > currentBounds.X) &&
                !(cachedBounds.Y < currentBounds.Y || cachedBounds.Y > currentBounds.Y) &&
                !(cachedBounds.Z < currentBounds.Z || cachedBounds.Z > currentBounds.Z) &&
                !(cachedBounds.SizeX < currentBounds.SizeX || cachedBounds.SizeX > currentBounds.SizeX) &&
                !(cachedBounds.SizeY < currentBounds.SizeY || cachedBounds.SizeY > currentBounds.SizeY) &&
                !(cachedBounds.SizeZ < currentBounds.SizeZ || cachedBounds.SizeZ > currentBounds.SizeZ);

            if (!boundsAreEqual)
            {
                Debug.Fail("Cached bbox subgraph is incorrect!");
            }
        }

        /// <summary>
        /// Precompute pass.
        /// </summary>
        internal override void PrecomputeContent()
        {
            base.PrecomputeContent();

            if (_children != null)
            {
                for (int i = 0, count = _children.InternalCount; i < count; i++)
                {
                    Visual3D child = _children.InternalGetItem(i);
                    if (child != null)
                    {
                        Rect3D bboxSubgraphChildIgnored;
                        child.PrecomputeRecursive(out bboxSubgraphChildIgnored);
                    }
                }
            }
        }

        internal static Action<Viewport3DVisual, IntPtr> RenderContentD2DHook { get; set; }

        internal override unsafe void RenderContentD2D(void* d2dContext)
        {
            if (RenderContentD2DHook != null)
            {
                RenderContentD2DHook(this, (IntPtr)d2dContext);
            }
            else
            {
                CSUI.Core.Interop.DirectX.DxViewport3DRenderer.RenderViewport(this, d2dContext);
            }

            SetFlagsOnAllChannels(
                false,
                VisualProxyFlags.Viewport3DVisual_IsCameraDirty |
                VisualProxyFlags.Viewport3DVisual_IsViewportDirty);

            if (_children != null)
            {
                for (int i = 0, count = _children.InternalCount; i < count; i++)
                {
                    _children.InternalGetItem(i)?.ClearDirtyFlagsSubtree();
                }
            }
        }

        // Notify the Viewport3DVisual that the Visual3D subtree it hosts
        // has been modified.
        internal void Visual3DTreeChanged()
        {
            // The Visual3D tree is plugged into the 2D Visual tree via the
            // same extensibility point we use for "content".
            SetFlagsOnAllChannels(true, VisualProxyFlags.IsContentDirty);

            ContentsChanged(sender: this, EventArgs.Empty);
        }

        /// <summary>
        /// Returns the handle this visual has on the given channel.
        /// Note: The 3D handle is obtained from _proxy3D.
        /// </summary>


        // Because 2D Visuals and FEs do not participate in inheritance context
        // we allow this backdoor for a Viewport3D to set itself as the inheritance
        // context of the Visual3DCollection it exposes as Children.
        internal void SetInheritanceContextForChildren(DependencyObject inheritanceContextForChildren)
        {
            _inheritanceContextForChildren = inheritanceContextForChildren;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The 3D content root.
        /// </summary>
        /// <remarks>
        /// Important! - Not readonly because CS will silently copy
        /// for self-modifying methods. (C# Spec 14.5.4)
        /// </remarks>
        private Rect3D _bboxChildrenSubgraph3D;

        private readonly Visual3DCollection _children;

        private DependencyObject _inheritanceContextForChildren;

        #endregion Private Fields
    }
}
