// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//
// Description: This file contains the implementation of VisualBrush.
//              The VisualBrush is a TileBrush which defines its tile content
//              by use of a Visual.
//
//

using System.Windows.Threading;


#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// VisualBrush - This TileBrush defines its content as a Visual
    /// </summary>
    public sealed partial class VisualBrush : TileBrush, ICyclicBrush
    {
        #region Constructors

        /// <summary>
        /// Default constructor for VisualBrush.  The resulting Brush has no content.
        /// </summary>
        public VisualBrush()
        {
        }

        /// <summary>
        /// VisualBrush Constructor where the image is set to the parameter's value
        /// </summary>
        /// <param name="visual"> The Visual representing the contents of this Brush. </param>
        public VisualBrush(Visual visual)
        {
            if (this.Dispatcher != null)
            {
                MediaSystem.AssertSameContext(this, visual);
                Visual = visual;
            }
        }
        #endregion Constructors
        
        void ICyclicBrush.FireOnChanged()
        {
            // Simple loop detection to avoid stack overflow in cyclic VisualBrush
            // scenarios. This fix is only aimed at mitigating a very common 
            // VisualBrush scenario. 

            bool canEnter = Enter();

            if (canEnter)
            {
                try
                {
                    _isCacheDirty = true;
                    FireChanged();

                    // Register brush's visual tree for Render().
                    RegisterForAsyncRenderForCyclicBrush();
                }
                finally
                {
                    Exit();
                }
            }
        } 

        /// <summary> 
        /// Calling this will make sure that the render request
        /// is registered with the MediaContext.
        /// </summary>
        private void RegisterForAsyncRenderForCyclicBrush()
        {
            if ((Dispatcher != null) && !_isAsyncRenderRegistered)
            {
                MediaContext mediaContext = MediaContext.From(Dispatcher);
                ICyclicBrush cyclicBrush = this;
                mediaContext.ResourcesUpdated += new MediaContext.ResourcesUpdatedHandler(cyclicBrush.RenderForCyclicBrush);
                _isAsyncRenderRegistered = true;
            }
        }

        void ICyclicBrush.RenderForCyclicBrush()
        {
            _isCacheDirty = true;
            _isAsyncRenderRegistered = false;
        }


        // Implement functions used to addref and release resources in codegen that need
        // to be specialized for Visual which doesn't implement DUCE.IResource


        /// <summary>
        /// Implementation of <see cref="System.Windows.DependencyObject.OnPropertyChanged">DependencyObject.OnPropertyInvalidated</see>.
        /// If the property is the Visual or the AutoLayoutContent property, we re-layout the Visual if 
        /// possible.
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.IsAValueChange || e.IsASubPropertyChange)
            {
                if ((e.Property == VisualProperty) || (e.Property == AutoLayoutContentProperty))
                {
                    // Should we initiate a layout on this Visual?
                    // Yes, if AutoLayoutContent is true and if the Visual is a UIElement not already in
                    // another tree (i.e. the parent is null) or its not the hwnd root.
                    if (AutoLayoutContent)
                    {
                        Debug.Assert(!_pendingLayout);                    
                        Visual element = Visual;
                    if ((element != null) && (Visual.ShouldAutoLayoutVisualHook?.Invoke(element, null, null) == true))
                    {
                        Visual.SubscribeLayoutUpdatedHook?.Invoke(element, OnLayoutUpdated);
                        _DispatcherLayoutResult = Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(LayoutCallback), element);
                        _pendingLayout = true;
                    }
                    }
                }
            }
        }

        /// <summary>
        /// We initiate the layout on the tree rooted at the Visual to which VisualBrush points.
        /// </summary>
        private void DoLayout(Visual element)
        {
            Debug.Assert(element != null);
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            if (!(element.IsUIElementRoot) && (parent == null || parent is System.Windows.Media.Media3D.Viewport2DVisual3D))
            {
                Visual.PerformAutoLayoutHook?.Invoke(element, null);
            }
        }

        /// <summary>
        /// LayoutUpdate event handler.
        /// </summary>
        /// <param name="sender">event sender (not used)</param>
        /// <param name="args">event arguments (not used)</param>
        private void OnLayoutUpdated(object sender, EventArgs args)
        {
            Debug.Assert(_pendingLayout);
            Visual element = Visual;
            Debug.Assert(element != null);
            Visual.UnsubscribeLayoutUpdatedHook?.Invoke(element, OnLayoutUpdated);
            _pendingLayout = false;
            bool abortStatus = _DispatcherLayoutResult.Abort();
            Debug.Assert(abortStatus);
            DoLayout(element);
        }


        /// <summary>
        /// DispatcherOperation callback to initiate layout.
        /// </summary>
        /// <param name="arg">The Visual root</param>
        private object LayoutCallback(object arg)
        {
            Debug.Assert(_pendingLayout);
            Visual element = arg as Visual;
            Debug.Assert(element != null);
            Visual.UnsubscribeLayoutUpdatedHook?.Invoke(element, OnLayoutUpdated);
            _pendingLayout = false;
            DoLayout(element);
            return null;
        }

        /// <summary>
        /// Enter is used for simple cycle detection in VisualBrush. If the method returns false
        /// the brush has already been entered and cannot be entered again. Matching invocation of Exit
        /// must be skipped if Enter returns false.
        /// </summary>
        internal bool Enter()
        {
            if (_reentrancyFlag)
            {
                return false;
            }
            else
            {
                _reentrancyFlag = true;
                return true;
            }
        }

        /// <summary>
        /// Exits the VisualBrush. For more details see Enter method.
        /// </summary>
        internal void Exit()
        {
            Debug.Assert(_reentrancyFlag); // Exit must be matched with Enter. See Enter comments.
            _reentrancyFlag = false;
        }       

        /// <summary>
        /// Obtains the current bounds of the brush's content
        /// </summary>
        /// <param name="contentBounds"> Output bounds of content</param>  
        internal void EnsureAutoLayoutForRender()
        {
            Visual element = Visual;
            if (element != null && AutoLayoutContent &&
                (_pendingLayout ||
                 (element is UIElement uie && (!uie.IsMeasureValid || !uie.IsArrangeValid) &&
                  Visual.ShouldAutoLayoutVisualHook?.Invoke(element, null, null) == true)))
            {
                if (_pendingLayout)
                {
                    Visual.UnsubscribeLayoutUpdatedHook?.Invoke(element, OnLayoutUpdated);
                    _pendingLayout = false;
                    _DispatcherLayoutResult?.Abort();
                }

                DoLayout(element);
                _isCacheDirty = true;
            }
        }

        protected override void GetContentBounds(out Rect contentBounds)
        {
            EnsureAutoLayoutForRender();

            // Obtain the current visual's outer space bounding box. We return the outer space
            // bounding box because we want to have the bounding box of the Visual tree including 
            // transform/offset at the root.
            if (_isCacheDirty)
            {
                _bbox = Visual.CalculateSubgraphBoundsOuterSpace();
                _isCacheDirty = false;
            }

            contentBounds = _bbox;
        }  

        private DispatcherOperation _DispatcherLayoutResult;
        private bool _pendingLayout;        
        private bool _reentrancyFlag;

        private bool _isAsyncRenderRegistered = false;
        
        // Whether we need to re-calculate our content bounds.
        private bool _isCacheDirty = true;

        // Keep our content bounds cached.
        private Rect _bbox = Rect.Empty;
    }
}

