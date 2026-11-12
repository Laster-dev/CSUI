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
    public sealed partial class RectangleGeometry : Geometry
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
        public new RectangleGeometry Clone()
        {
            return (RectangleGeometry)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new RectangleGeometry CloneCurrentValue()
        {
            return (RectangleGeometry)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void RadiusXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RectangleGeometry target = ((RectangleGeometry) d);


            target.PropertyChanged(RadiusXProperty);
        }
        private static void RadiusYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RectangleGeometry target = ((RectangleGeometry) d);


            target.PropertyChanged(RadiusYProperty);
        }
        private static void RectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RectangleGeometry target = ((RectangleGeometry) d);


            target.PropertyChanged(RectProperty);
        }


        #region Public Properties

        /// <summary>
        ///     RadiusX - double.  Default value is 0.0.
        /// </summary>
        public double RadiusX
        {
            get
            {
                return (double)GetValue(RadiusXProperty);
            }
            set
            {
                SetValueInternal(RadiusXProperty, value);
            }
        }

        /// <summary>
        ///     RadiusY - double.  Default value is 0.0.
        /// </summary>
        public double RadiusY
        {
            get
            {
                return (double)GetValue(RadiusYProperty);
            }
            set
            {
                SetValueInternal(RadiusYProperty, value);
            }
        }

        /// <summary>
        ///     Rect - Rect.  Default value is Rect.Empty.
        /// </summary>
        public Rect Rect
        {
            get
            {
                return (Rect)GetValue(RectProperty);
            }
            set
            {
                SetValueInternal(RectProperty, value);
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
            return new RectangleGeometry();
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

        //
        //  This property finds the correct initial size for the _effectiveValues store on the
        //  current DependencyObject as a performance optimization
        //
        //  This includes:
        //    Rect
        //
        internal override int EffectiveValuesInitialSize
        {
            get
            {
                return 1;
            }
        }



        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties

        /// <summary>
        ///     The DependencyProperty for the RectangleGeometry.RadiusX property.
        /// </summary>
        public static readonly DependencyProperty RadiusXProperty;
        /// <summary>
        ///     The DependencyProperty for the RectangleGeometry.RadiusY property.
        /// </summary>
        public static readonly DependencyProperty RadiusYProperty;
        /// <summary>
        ///     The DependencyProperty for the RectangleGeometry.Rect property.
        /// </summary>
        public static readonly DependencyProperty RectProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal const double c_RadiusX = 0.0;
        internal const double c_RadiusY = 0.0;
        internal static Rect s_Rect = Rect.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static RectangleGeometry()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(RectangleGeometry);
            RadiusXProperty =
                  RegisterProperty("RadiusX",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(RadiusXPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            RadiusYProperty =
                  RegisterProperty("RadiusY",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   new PropertyChangedCallback(RadiusYPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            RectProperty =
                  RegisterProperty("Rect",
                                   typeof(Rect),
                                   typeofThis,
                                   Rect.Empty,
                                   new PropertyChangedCallback(RectPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
