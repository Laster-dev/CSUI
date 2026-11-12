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
    public sealed partial class TranslateTransform3D : AffineTransform3D
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
        public new TranslateTransform3D Clone()
        {
            return (TranslateTransform3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new TranslateTransform3D CloneCurrentValue()
        {
            return (TranslateTransform3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void OffsetXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TranslateTransform3D target = ((TranslateTransform3D) d);

            target._cachedOffsetXValue = (double)e.NewValue;


            target.PropertyChanged(OffsetXProperty);
        }
        private static void OffsetYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TranslateTransform3D target = ((TranslateTransform3D) d);

            target._cachedOffsetYValue = (double)e.NewValue;


            target.PropertyChanged(OffsetYProperty);
        }
        private static void OffsetZPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TranslateTransform3D target = ((TranslateTransform3D) d);

            target._cachedOffsetZValue = (double)e.NewValue;


            target.PropertyChanged(OffsetZProperty);
        }


        #region Public Properties

        /// <summary>
        ///     OffsetX - double.  Default value is 0.0.
        /// </summary>
        public double OffsetX
        {
            get
            {
                ReadPreamble();
                return _cachedOffsetXValue;
            }
            set
            {
                SetValueInternal(OffsetXProperty, value);
            }
        }

        /// <summary>
        ///     OffsetY - double.  Default value is 0.0.
        /// </summary>
        public double OffsetY
        {
            get
            {
                ReadPreamble();
                return _cachedOffsetYValue;
            }
            set
            {
                SetValueInternal(OffsetYProperty, value);
            }
        }

        /// <summary>
        ///     OffsetZ - double.  Default value is 0.0.
        /// </summary>
        public double OffsetZ
        {
            get
            {
                ReadPreamble();
                return _cachedOffsetZValue;
            }
            set
            {
                SetValueInternal(OffsetZProperty, value);
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
            return new TranslateTransform3D();
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
        ///     The DependencyProperty for the TranslateTransform3D.OffsetX property.
        /// </summary>
        public static readonly DependencyProperty OffsetXProperty;
        /// <summary>
        ///     The DependencyProperty for the TranslateTransform3D.OffsetY property.
        /// </summary>
        public static readonly DependencyProperty OffsetYProperty;
        /// <summary>
        ///     The DependencyProperty for the TranslateTransform3D.OffsetZ property.
        /// </summary>
        public static readonly DependencyProperty OffsetZProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        private double _cachedOffsetXValue = 0.0;
        private double _cachedOffsetYValue = 0.0;
        private double _cachedOffsetZValue = 0.0;

        internal const double c_OffsetX = 0.0;
        internal const double c_OffsetY = 0.0;
        internal const double c_OffsetZ = 0.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static TranslateTransform3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(TranslateTransform3D);
            OffsetXProperty =
                  RegisterProperty("OffsetX",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(OffsetXPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            OffsetYProperty =
                  RegisterProperty("OffsetY",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(OffsetYPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            OffsetZProperty =
                  RegisterProperty("OffsetZ",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(OffsetZPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
