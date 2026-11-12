// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.Collections;
using MS.Utility;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;


#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    public sealed partial class DrawingGroup : Drawing
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Shadows inherited Clone() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new DrawingGroup Clone()
        {
            return (DrawingGroup)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new DrawingGroup CloneCurrentValue()
        {
            return (DrawingGroup)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ChildrenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            DrawingGroup target = ((DrawingGroup)d);
            target.PropertyChanged(ChildrenProperty);
        }
        private static void ClipGeometryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            DrawingGroup target = ((DrawingGroup)d);
            target.PropertyChanged(ClipGeometryProperty);
        }
        private static void OpacityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingGroup target = ((DrawingGroup) d);


            target.PropertyChanged(OpacityProperty);
        }
        private static void OpacityMaskPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            DrawingGroup target = ((DrawingGroup)d);
            target.PropertyChanged(OpacityMaskProperty);
        }
        private static void TransformPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            DrawingGroup target = ((DrawingGroup)d);
            target.PropertyChanged(TransformProperty);
        }
        private static void GuidelineSetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            DrawingGroup target = ((DrawingGroup)d);
            target.PropertyChanged(GuidelineSetProperty);
        }
        private static void EdgeModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingGroup target = ((DrawingGroup) d);


            target.PropertyChanged(RenderOptions.EdgeModeProperty);
        }
        private static void BitmapScalingModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingGroup target = ((DrawingGroup) d);


            target.PropertyChanged(RenderOptions.BitmapScalingModeProperty);
        }
        private static void ClearTypeHintPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingGroup target = ((DrawingGroup) d);


            target.PropertyChanged(RenderOptions.ClearTypeHintProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Children - DrawingCollection.  Default value is new FreezableDefaultValueFactory(DrawingCollection.Empty).
        /// </summary>
        public DrawingCollection Children
        {
            get
            {
                return (DrawingCollection)GetValue(ChildrenProperty);
            }
            set
            {
                SetValueInternal(ChildrenProperty, value);
            }
        }

        /// <summary>
        ///     ClipGeometry - Geometry.  Default value is null.
        /// </summary>
        public Geometry ClipGeometry
        {
            get
            {
                return (Geometry)GetValue(ClipGeometryProperty);
            }
            set
            {
                SetValueInternal(ClipGeometryProperty, value);
            }
        }

        /// <summary>
        ///     Opacity - double.  Default value is 1.0.
        /// </summary>
        public double Opacity
        {
            get
            {
                return (double)GetValue(OpacityProperty);
            }
            set
            {
                SetValueInternal(OpacityProperty, value);
            }
        }

        /// <summary>
        ///     OpacityMask - Brush.  Default value is null.
        /// </summary>
        public Brush OpacityMask
        {
            get
            {
                return (Brush)GetValue(OpacityMaskProperty);
            }
            set
            {
                SetValueInternal(OpacityMaskProperty, value);
            }
        }

        /// <summary>
        ///     Transform - Transform.  Default value is null.
        /// </summary>
        public Transform Transform
        {
            get
            {
                return (Transform)GetValue(TransformProperty);
            }
            set
            {
                SetValueInternal(TransformProperty, value);
            }
        }

        /// <summary>
        ///     GuidelineSet - GuidelineSet.  Default value is null.
        /// </summary>
        public GuidelineSet GuidelineSet
        {
            get
            {
                return (GuidelineSet)GetValue(GuidelineSetProperty);
            }
            set
            {
                SetValueInternal(GuidelineSetProperty, value);
            }
        }



        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DrawingGroup();
        }



        #endregion ProtectedMethods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods




        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties





        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties

        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.Children property.
        /// </summary>
        public static readonly DependencyProperty ChildrenProperty;
        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.ClipGeometry property.
        /// </summary>
        public static readonly DependencyProperty ClipGeometryProperty;
        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.Opacity property.
        /// </summary>
        public static readonly DependencyProperty OpacityProperty;
        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.OpacityMask property.
        /// </summary>
        public static readonly DependencyProperty OpacityMaskProperty;
        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.Transform property.
        /// </summary>
        public static readonly DependencyProperty TransformProperty;
        /// <summary>
        ///     The DependencyProperty for the DrawingGroup.GuidelineSet property.
        /// </summary>
        public static readonly DependencyProperty GuidelineSetProperty;
        /// <summary>
        /// </summary>
        /// <summary>
        /// </summary>

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal static DrawingCollection s_Children = DrawingCollection.Empty;
        internal const double c_Opacity = 1.0;
        internal const EdgeMode c_EdgeMode = EdgeMode.Unspecified;
        internal const BitmapScalingMode c_BitmapScalingMode = BitmapScalingMode.Unspecified;
        internal const ClearTypeHint c_ClearTypeHint = ClearTypeHint.Auto;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static DrawingGroup()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.
            Debug.Assert(s_Children == null || s_Children.IsFrozen,
                "Detected context bound default value DrawingGroup.s_Children (See OS Bug #947272).");


            RenderOptions.EdgeModeProperty.OverrideMetadata(
                typeof(DrawingGroup),
                new UIPropertyMetadata(EdgeMode.Unspecified,
                                       new PropertyChangedCallback(EdgeModePropertyChanged)));

            RenderOptions.BitmapScalingModeProperty.OverrideMetadata(
                typeof(DrawingGroup),
                new UIPropertyMetadata(BitmapScalingMode.Unspecified,
                                       new PropertyChangedCallback(BitmapScalingModePropertyChanged)));

            RenderOptions.ClearTypeHintProperty.OverrideMetadata(
                typeof(DrawingGroup),
                new UIPropertyMetadata(ClearTypeHint.Auto,
                                       new PropertyChangedCallback(ClearTypeHintPropertyChanged)));

            // Initializations
            Type typeofThis = typeof(DrawingGroup);
            ChildrenProperty =
                  RegisterProperty("Children",
                                   typeof(DrawingCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(DrawingCollection.Empty),
                                   new PropertyChangedCallback(ChildrenPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ClipGeometryProperty =
                  RegisterProperty("ClipGeometry",
                                   typeof(Geometry),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(ClipGeometryPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            OpacityProperty =
                  RegisterProperty("Opacity",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(OpacityPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            OpacityMaskProperty =
                  RegisterProperty("OpacityMask",
                                   typeof(Brush),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(OpacityMaskPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            TransformProperty =
                  RegisterProperty("Transform",
                                   typeof(Transform),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(TransformPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            GuidelineSetProperty =
                  RegisterProperty("GuidelineSet",
                                   typeof(GuidelineSet),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(GuidelineSetPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
