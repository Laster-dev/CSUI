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
    public sealed partial class ScaleTransform3D : AffineTransform3D
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
        public new ScaleTransform3D Clone()
        {
            return (ScaleTransform3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ScaleTransform3D CloneCurrentValue()
        {
            return (ScaleTransform3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ScaleXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform3D target = ((ScaleTransform3D) d);

            target._cachedScaleXValue = (double)e.NewValue;


            target.PropertyChanged(ScaleXProperty);
        }
        private static void ScaleYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform3D target = ((ScaleTransform3D) d);

            target._cachedScaleYValue = (double)e.NewValue;


            target.PropertyChanged(ScaleYProperty);
        }
        private static void ScaleZPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform3D target = ((ScaleTransform3D) d);

            target._cachedScaleZValue = (double)e.NewValue;


            target.PropertyChanged(ScaleZProperty);
        }
        private static void CenterXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform3D target = ((ScaleTransform3D) d);

            target._cachedCenterXValue = (double)e.NewValue;


            target.PropertyChanged(CenterXProperty);
        }
        private static void CenterYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform3D target = ((ScaleTransform3D) d);

            target._cachedCenterYValue = (double)e.NewValue;


            target.PropertyChanged(CenterYProperty);
        }
        private static void CenterZPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform3D target = ((ScaleTransform3D) d);

            target._cachedCenterZValue = (double)e.NewValue;


            target.PropertyChanged(CenterZProperty);
        }


        #region Public Properties

        /// <summary>
        ///     ScaleX - double.  Default value is 1.0.
        /// </summary>
        public double ScaleX
        {
            get
            {
                ReadPreamble();
                return _cachedScaleXValue;
            }
            set
            {
                SetValueInternal(ScaleXProperty, value);
            }
        }

        /// <summary>
        ///     ScaleY - double.  Default value is 1.0.
        /// </summary>
        public double ScaleY
        {
            get
            {
                ReadPreamble();
                return _cachedScaleYValue;
            }
            set
            {
                SetValueInternal(ScaleYProperty, value);
            }
        }

        /// <summary>
        ///     ScaleZ - double.  Default value is 1.0.
        /// </summary>
        public double ScaleZ
        {
            get
            {
                ReadPreamble();
                return _cachedScaleZValue;
            }
            set
            {
                SetValueInternal(ScaleZProperty, value);
            }
        }

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
            return new ScaleTransform3D();
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
        ///     The DependencyProperty for the ScaleTransform3D.ScaleX property.
        /// </summary>
        public static readonly DependencyProperty ScaleXProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform3D.ScaleY property.
        /// </summary>
        public static readonly DependencyProperty ScaleYProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform3D.ScaleZ property.
        /// </summary>
        public static readonly DependencyProperty ScaleZProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform3D.CenterX property.
        /// </summary>
        public static readonly DependencyProperty CenterXProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform3D.CenterY property.
        /// </summary>
        public static readonly DependencyProperty CenterYProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform3D.CenterZ property.
        /// </summary>
        public static readonly DependencyProperty CenterZProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        private double _cachedScaleXValue = 1.0;
        private double _cachedScaleYValue = 1.0;
        private double _cachedScaleZValue = 1.0;
        private double _cachedCenterXValue = 0.0;
        private double _cachedCenterYValue = 0.0;
        private double _cachedCenterZValue = 0.0;

        internal const double c_ScaleX = 1.0;
        internal const double c_ScaleY = 1.0;
        internal const double c_ScaleZ = 1.0;
        internal const double c_CenterX = 0.0;
        internal const double c_CenterY = 0.0;
        internal const double c_CenterZ = 0.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static ScaleTransform3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(ScaleTransform3D);
            ScaleXProperty =
                  RegisterProperty("ScaleX",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(ScaleXPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            ScaleYProperty =
                  RegisterProperty("ScaleY",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(ScaleYPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            ScaleZProperty =
                  RegisterProperty("ScaleZ",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(ScaleZPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
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
        }



        #endregion Constructors
    }
}
