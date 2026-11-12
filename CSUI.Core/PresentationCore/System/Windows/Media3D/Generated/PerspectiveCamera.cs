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
    public sealed partial class PerspectiveCamera : ProjectionCamera
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
        public new PerspectiveCamera Clone()
        {
            return (PerspectiveCamera)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new PerspectiveCamera CloneCurrentValue()
        {
            return (PerspectiveCamera)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void FieldOfViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PerspectiveCamera target = ((PerspectiveCamera) d);


            target.PropertyChanged(FieldOfViewProperty);
        }


        #region Public Properties

        /// <summary>
        ///     FieldOfView - double.  Default value is (double)45.0.
        /// </summary>
        public double FieldOfView
        {
            get
            {
                return (double)GetValue(FieldOfViewProperty);
            }
            set
            {
                SetValueInternal(FieldOfViewProperty, value);
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
            return new PerspectiveCamera();
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
        ///     The DependencyProperty for the PerspectiveCamera.FieldOfView property.
        /// </summary>
        public static readonly DependencyProperty FieldOfViewProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal const double c_FieldOfView = (double)45.0;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static PerspectiveCamera()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(PerspectiveCamera);
            FieldOfViewProperty =
                  RegisterProperty("FieldOfView",
                                   typeof(double),
                                   typeofThis,
                                   (double)45.0,
                                   new PropertyChangedCallback(FieldOfViewPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
