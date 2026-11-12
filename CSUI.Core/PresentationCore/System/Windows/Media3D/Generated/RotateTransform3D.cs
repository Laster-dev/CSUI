// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;
using MS.Internal.Collections;
using MS.Utility;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media.Animation;

#nullable disable

using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Media3D
{
    public sealed partial class RotateTransform3D : AffineTransform3D
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
        public new RotateTransform3D Clone()
        {
            return (RotateTransform3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new RotateTransform3D CloneCurrentValue()
        {
            return (RotateTransform3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void CenterXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RotateTransform3D target = ((RotateTransform3D) d);

            target._cachedCenterXValue = (double)e.NewValue;


            target.PropertyChanged(CenterXProperty);
        }
        private static void CenterYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RotateTransform3D target = ((RotateTransform3D) d);

            target._cachedCenterYValue = (double)e.NewValue;


            target.PropertyChanged(CenterYProperty);
        }
        private static void CenterZPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RotateTransform3D target = ((RotateTransform3D) d);

            target._cachedCenterZValue = (double)e.NewValue;


            target.PropertyChanged(CenterZProperty);
        }
        private static void RotationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {




            // The first change to the default value of a mutable collection property (e.g. GeometryGroup.Children) 
            // will promote the property value from a default value to a local value. This is technically a sub-property 
            // change because the collection was changed and not a new collection set (GeometryGroup.Children.
            // Add versus GeometryGroup.Children = myNewChildrenCollection). However, we never marshalled 
            // the default value to the compositor. If the property changes from a default value, the new local value 
            // needs to be marshalled to the compositor. We detect this scenario with the second condition 
            // e.OldValueSource != e.NewValueSource. Specifically in this scenario the OldValueSource will be 
            // Default and the NewValueSource will be Local.
            if (e.IsASubPropertyChange && 
               (e.OldValueSource == e.NewValueSource))
            {
                return;
            }


            RotateTransform3D target = ((RotateTransform3D) d);


            target._cachedRotationValue = (Rotation3D)e.NewValue;

            target.PropertyChanged(RotationProperty);
        }


        #region Public Properties

        /// <summary>
        ///     CenterX - double.  Default value is 0.0.
        /// </summary>
        public double CenterX
        {
            get
            {
                ReadPreamble();
                return _cachedCenterXValue;
            }
            set
            {
                SetValueInternal(CenterXProperty, value);
            }
        }

        /// <summary>
        ///     CenterY - double.  Default value is 0.0.
        /// </summary>
        public double CenterY
        {
            get
            {
                ReadPreamble();
                return _cachedCenterYValue;
            }
            set
            {
                SetValueInternal(CenterYProperty, value);
            }
        }

        /// <summary>
        ///     CenterZ - double.  Default value is 0.0.
        /// </summary>
        public double CenterZ
        {
            get
            {
                ReadPreamble();
                return _cachedCenterZValue;
            }
            set
            {
                SetValueInternal(CenterZProperty, value);
            }
        }

        /// <summary>
        ///     Rotation - Rotation3D.  Default value is Rotation3D.Identity.
        /// </summary>
        public Rotation3D Rotation
        {
            get
            {
                ReadPreamble();
                return _cachedRotationValue;
            }
            set
            {
                SetValueInternal(RotationProperty, value);
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
            return new RotateTransform3D();
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
        ///     The DependencyProperty for the RotateTransform3D.CenterX property.
        /// </summary>
        public static readonly DependencyProperty CenterXProperty;
        /// <summary>
        ///     The DependencyProperty for the RotateTransform3D.CenterY property.
        /// </summary>
        public static readonly DependencyProperty CenterYProperty;
        /// <summary>
        ///     The DependencyProperty for the RotateTransform3D.CenterZ property.
        /// </summary>
        public static readonly DependencyProperty CenterZProperty;
        /// <summary>
        ///     The DependencyProperty for the RotateTransform3D.Rotation property.
        /// </summary>
        public static readonly DependencyProperty RotationProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        private double _cachedCenterXValue = 0.0;
        private double _cachedCenterYValue = 0.0;
        private double _cachedCenterZValue = 0.0;
        private Rotation3D _cachedRotationValue = Rotation3D.Identity;

        internal const double c_CenterX = 0.0;
        internal const double c_CenterY = 0.0;
        internal const double c_CenterZ = 0.0;
        internal static Rotation3D s_Rotation = Rotation3D.Identity;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static RotateTransform3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.
            Debug.Assert(s_Rotation == null || s_Rotation.IsFrozen,
                "Detected context bound default value RotateTransform3D.s_Rotation (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(RotateTransform3D);
            CenterXProperty =
                  RegisterProperty("CenterX",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(CenterXPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            CenterYProperty =
                  RegisterProperty("CenterY",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(CenterYPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            CenterZProperty =
                  RegisterProperty("CenterZ",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(CenterZPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            RotationProperty =
                  RegisterProperty("Rotation",
                                   typeof(Rotation3D),
                                   typeofThis,
                                   Rotation3D.Identity,
                                   new PropertyChangedCallback(RotationPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
