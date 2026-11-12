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
using System.Windows.Media.Animation;
using System.Windows.Markup;


#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    public sealed partial class SkewTransform : Transform
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
        public new SkewTransform Clone()
        {
            return (SkewTransform)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new SkewTransform CloneCurrentValue()
        {
            return (SkewTransform)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void AngleXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SkewTransform target = ((SkewTransform) d);


            target.PropertyChanged(AngleXProperty);
        }
        private static void AngleYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SkewTransform target = ((SkewTransform) d);


            target.PropertyChanged(AngleYProperty);
        }
        private static void CenterXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SkewTransform target = ((SkewTransform) d);


            target.PropertyChanged(CenterXProperty);
        }
        private static void CenterYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SkewTransform target = ((SkewTransform) d);


            target.PropertyChanged(CenterYProperty);
        }


        #region Public Properties

        /// <summary>
        ///     AngleX - double.  Default value is 0.0.
        /// </summary>
        public double AngleX
        {
            get
            {
                return (double)GetValue(AngleXProperty);
            }
            set
            {
                SetValueInternal(AngleXProperty, value);
            }
        }

        /// <summary>
        ///     AngleY - double.  Default value is 0.0 .
        /// </summary>
        public double AngleY
        {
            get
            {
                return (double)GetValue(AngleYProperty);
            }
            set
            {
                SetValueInternal(AngleYProperty, value);
            }
        }

        /// <summary>
        ///     CenterX - double.  Default value is 0.0.
        /// </summary>
        public double CenterX
        {
            get
            {
                return (double)GetValue(CenterXProperty);
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
                return (double)GetValue(CenterYProperty);
            }
            set
            {
                SetValueInternal(CenterYProperty, value);
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
            return new SkewTransform();
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
        ///     The DependencyProperty for the SkewTransform.AngleX property.
        /// </summary>
        public static readonly DependencyProperty AngleXProperty;
        /// <summary>
        ///     The DependencyProperty for the SkewTransform.AngleY property.
        /// </summary>
        public static readonly DependencyProperty AngleYProperty;
        /// <summary>
        ///     The DependencyProperty for the SkewTransform.CenterX property.
        /// </summary>
        public static readonly DependencyProperty CenterXProperty;
        /// <summary>
        ///     The DependencyProperty for the SkewTransform.CenterY property.
        /// </summary>
        public static readonly DependencyProperty CenterYProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal const double c_AngleX = 0.0;
        internal const double c_AngleY = 0.0 ;
        internal const double c_CenterX = 0.0;
        internal const double c_CenterY = 0.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static SkewTransform()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(SkewTransform);
            AngleXProperty =
                  RegisterProperty("AngleX",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(AngleXPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            AngleYProperty =
                  RegisterProperty("AngleY",
                                   typeof(double),
                                   typeofThis,
                                   0.0 ,
                                   new PropertyChangedCallback(AngleYPropertyChanged),
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
        }



        #endregion Constructors
    }
}
