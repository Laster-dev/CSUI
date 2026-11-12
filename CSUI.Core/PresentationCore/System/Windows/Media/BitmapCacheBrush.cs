// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Threading;
using System.Windows.Media.Animation;


#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    public partial class BitmapCacheBrush : Brush, ICyclicBrush
    {
        #region Constructors        
        public BitmapCacheBrush()
        {
        }

        /// <summary>
        /// VisualBrush Constructor where the image is set to the parameter's value
        /// </summary>
        /// <param name="visual"> The Visual representing the contents of this Brush. </param>
        public BitmapCacheBrush(Visual visual)
        {
            if (this.Dispatcher != null)
            {
                MediaSystem.AssertSameContext(this, visual);
                Target = visual;
            }
        }
        #endregion Constructors

        private ContainerVisual AutoWrapVisual
        {
            get 
            { 
                // Lazily create the dummy visual instance.
                if (_dummyVisual == null)
                {
                    _dummyVisual = new ContainerVisual();
                }
                
                return _dummyVisual;
            }
        }

        // NOTE:  This class is basically identical to VisualBrush, it should be refactored to
        //        a common place to prevent code duplication (maybe Brush.cs?)
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
                if ((e.Property == TargetProperty) || (e.Property == AutoLayoutContentProperty))
                {   
                    // Should we wrap the visual in a dummy visual node for rendering?
                    if (e.Property == TargetProperty && e.IsAValueChange)
                    {
                        if (AutoWrapTarget)
                        {
                            Debug.Assert(InternalTarget == AutoWrapVisual, 
                                "InternalTarget should point to our dummy visual AutoWrapVisual when AutoWrapTarget is true.");

                            // Change the value being wrapped by AutoWrapVisual.
                            AutoWrapVisual.Children.Remove((Visual)e.OldValue);
                            AutoWrapVisual.Children.Add((Visual)e.NewValue);
                        }
                        else
                        {
                            // Target just passes through to InternalTarget.
                            InternalTarget = Target;
                        }
                    }
                    
                    // Should we initiate a layout on this Visual?
                    // Yes, if AutoLayoutContent is true and if the Visual is a UIElement not already in
                    // another tree (i.e. the parent is null) or its not the hwnd root.
                    if (AutoLayoutContent)
                    {
                        Debug.Assert(!_pendingLayout);
                        Visual element = Target;
                        if ((element != null) && (Visual.ShouldAutoLayoutVisualHook?.Invoke(element, InternalTarget, null) == true))
                        {
                            Visual.SubscribeLayoutUpdatedHook?.Invoke(element, OnLayoutUpdated);
                            _DispatcherLayoutResult = Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(LayoutCallback), element);
                            _pendingLayout = true;
                        }
                    }
                }
                else if (e.Property == AutoWrapTargetProperty)
                {
                    // If our AutoWrap behavior changed, wrap/unwrap the target here.
                    if (AutoWrapTarget)
                    {
                        InternalTarget = AutoWrapVisual;
                        AutoWrapVisual.Children.Add(Target);
                    }
                    else
                    {
                        AutoWrapVisual.Children.Remove(Target);
                        InternalTarget = Target;
                    }
                }
            }
        }

        /// <summary>
        /// We initiate the layout on the tree rooted at the Visual to which BitmapCacheBrush points.
        /// </summary>
        private void DoLayout(Visual element)
        {
            Debug.Assert(element != null);
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            if (!(element.IsUIElementRoot) && (parent == null || parent == InternalTarget || parent is System.Windows.Media.Media3D.Viewport2DVisual3D))
            {
                Visual.PerformAutoLayoutHook?.Invoke(element, null);
            }
        }

        /// <summary>
        /// LayoutUpdate event handler.
        /// </summary>
        private void OnLayoutUpdated(object sender, EventArgs args)
        {
            Debug.Assert(_pendingLayout);
            Visual element = Target;
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
        /// Enter is used for simple cycle detection in BitmapCacheBrush. If the method returns false
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
        /// Exits the BitmapCacheBrush. For more details see Enter method.
        /// </summary>
        internal void Exit()
        {
            Debug.Assert(_reentrancyFlag); // Exit must be matched with Enter. See Enter comments.
            _reentrancyFlag = false;
        }       

        private static object CoerceOpacity(DependencyObject d, object value)
        {
            if ((double)value != (double)OpacityProperty.GetDefaultValue(typeof(BitmapCacheBrush)))
            {
                throw new InvalidOperationException(SR.BitmapCacheBrush_OpacityChanged);
            }
            return 1.0;
        }

        private static object CoerceTransform(DependencyObject d, object value)
        {
            if ((Transform)value != (Transform)TransformProperty.GetDefaultValue(typeof(BitmapCacheBrush)))
            {
                throw new InvalidOperationException(SR.BitmapCacheBrush_TransformChanged);
            }
            return null;
        }

        private static object CoerceRelativeTransform(DependencyObject d, object value)
        {
            if ((Transform)value != (Transform)RelativeTransformProperty.GetDefaultValue(typeof(BitmapCacheBrush)))
            {
                throw new InvalidOperationException(SR.BitmapCacheBrush_RelativeTransformChanged);
            }
            return null;
        }

        private static void StaticInitialize(Type typeofThis)
        {             
            OpacityProperty.OverrideMetadata(typeofThis, new IndependentlyAnimatedPropertyMetadata(1.0, propertyChangedCallback: null, CoerceOpacity));
            TransformProperty.OverrideMetadata(typeofThis, new UIPropertyMetadata(null, propertyChangedCallback: null, CoerceTransform));
            RelativeTransformProperty.OverrideMetadata(typeofThis, new UIPropertyMetadata(null, propertyChangedCallback: null, CoerceRelativeTransform));
        }

        private ContainerVisual _dummyVisual;
        
        private DispatcherOperation _DispatcherLayoutResult;
        private bool _pendingLayout;        
        private bool _reentrancyFlag;

        private bool _isAsyncRenderRegistered = false;
    }
}
