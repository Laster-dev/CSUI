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
    public sealed partial class AxisAngleRotation3D : Rotation3D
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
        public new AxisAngleRotation3D Clone()
        {
            return (AxisAngleRotation3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new AxisAngleRotation3D CloneCurrentValue()
        {
            return (AxisAngleRotation3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void AxisPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AxisAngleRotation3D target = ((AxisAngleRotation3D) d);


            target.AxisPropertyChangedHook(e);

            target.PropertyChanged(AxisProperty);
        }
        private static void AnglePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AxisAngleRotation3D target = ((AxisAngleRotation3D) d);


            target.AnglePropertyChangedHook(e);

            target.PropertyChanged(AngleProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Axis - Vector3D.  Default value is new Vector3D(0,1,0).
        /// </summary>
        public Vector3D Axis
        {
            get
            {
                return (Vector3D)GetValue(AxisProperty);
            }
            set
            {
                SetValueInternal(AxisProperty, value);
            }
        }

        /// <summary>
        ///     Angle - double.  Default value is (double)0.0.
        /// </summary>
        public double Angle
        {
            get
            {
                return (double)GetValue(AngleProperty);
            }
            set
            {
                SetValueInternal(AngleProperty, value);
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
            return new AxisAngleRotation3D();
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
        ///     The DependencyProperty for the AxisAngleRotation3D.Axis property.
        /// </summary>
        public static readonly DependencyProperty AxisProperty;
        /// <summary>
        ///     The DependencyProperty for the AxisAngleRotation3D.Angle property.
        /// </summary>
        public static readonly DependencyProperty AngleProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal static Vector3D s_Axis = new Vector3D(0,1,0);
        internal const double c_Angle = (double)0.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static AxisAngleRotation3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(AxisAngleRotation3D);
            AxisProperty =
                  RegisterProperty("Axis",
                                   typeof(Vector3D),
                                   typeofThis,
                                   new Vector3D(0,1,0),
                                   new PropertyChangedCallback(AxisPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            AngleProperty =
                  RegisterProperty("Angle",
                                   typeof(double),
                                   typeofThis,
                                   (double)0.0,
                                   new PropertyChangedCallback(AnglePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
