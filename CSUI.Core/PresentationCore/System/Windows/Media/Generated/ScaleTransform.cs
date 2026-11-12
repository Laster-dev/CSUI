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
    public sealed partial class ScaleTransform : Transform
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
        public new ScaleTransform Clone()
        {
            return (ScaleTransform)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ScaleTransform CloneCurrentValue()
        {
            return (ScaleTransform)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ScaleXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform target = ((ScaleTransform) d);


            target.PropertyChanged(ScaleXProperty);
        }
        private static void ScaleYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform target = ((ScaleTransform) d);


            target.PropertyChanged(ScaleYProperty);
        }
        private static void CenterXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform target = ((ScaleTransform) d);


            target.PropertyChanged(CenterXProperty);
        }
        private static void CenterYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScaleTransform target = ((ScaleTransform) d);


            target.PropertyChanged(CenterYProperty);
        }


        #region Public Properties

        /// <summary>
        ///     ScaleX - double.  Default value is 1.0.
        /// </summary>
        public double ScaleX
        {
            get
            {
                return (double)GetValue(ScaleXProperty);
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
                return (double)GetValue(ScaleYProperty);
            }
            set
            {
                SetValueInternal(ScaleYProperty, value);
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
            return new ScaleTransform();
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
        ///     The DependencyProperty for the ScaleTransform.ScaleX property.
        /// </summary>
        public static readonly DependencyProperty ScaleXProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform.ScaleY property.
        /// </summary>
        public static readonly DependencyProperty ScaleYProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform.CenterX property.
        /// </summary>
        public static readonly DependencyProperty CenterXProperty;
        /// <summary>
        ///     The DependencyProperty for the ScaleTransform.CenterY property.
        /// </summary>
        public static readonly DependencyProperty CenterYProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal const double c_ScaleX = 1.0;
        internal const double c_ScaleY = 1.0;
        internal const double c_CenterX = 0.0;
        internal const double c_CenterY = 0.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static ScaleTransform()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(ScaleTransform);
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
