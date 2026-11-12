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
    public sealed partial class EllipseGeometry : Geometry
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
        public new EllipseGeometry Clone()
        {
            return (EllipseGeometry)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new EllipseGeometry CloneCurrentValue()
        {
            return (EllipseGeometry)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void RadiusXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EllipseGeometry target = ((EllipseGeometry) d);


            target.PropertyChanged(RadiusXProperty);
        }
        private static void RadiusYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EllipseGeometry target = ((EllipseGeometry) d);


            target.PropertyChanged(RadiusYProperty);
        }
        private static void CenterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EllipseGeometry target = ((EllipseGeometry) d);


            target.PropertyChanged(CenterProperty);
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
        ///     Center - Point.  Default value is new Point().
        /// </summary>
        public Point Center
        {
            get
            {
                return (Point)GetValue(CenterProperty);
            }
            set
            {
                SetValueInternal(CenterProperty, value);
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
            return new EllipseGeometry();
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
        //    RadiusX
        //    RadiusY
        //    Center
        //
        internal override int EffectiveValuesInitialSize
        {
            get
            {
                return 3;
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
        ///     The DependencyProperty for the EllipseGeometry.RadiusX property.
        /// </summary>
        public static readonly DependencyProperty RadiusXProperty;
        /// <summary>
        ///     The DependencyProperty for the EllipseGeometry.RadiusY property.
        /// </summary>
        public static readonly DependencyProperty RadiusYProperty;
        /// <summary>
        ///     The DependencyProperty for the EllipseGeometry.Center property.
        /// </summary>
        public static readonly DependencyProperty CenterProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal const double c_RadiusX = 0.0;
        internal const double c_RadiusY = 0.0;
        internal static Point s_Center = new Point();

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static EllipseGeometry()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(EllipseGeometry);
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
            CenterProperty =
                  RegisterProperty("Center",
                                   typeof(Point),
                                   typeofThis,
                                   new Point(),
                                   new PropertyChangedCallback(CenterPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
