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
    public sealed partial class DirectionalLight : Light
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
        public new DirectionalLight Clone()
        {
            return (DirectionalLight)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new DirectionalLight CloneCurrentValue()
        {
            return (DirectionalLight)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void DirectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DirectionalLight target = ((DirectionalLight) d);


            target.PropertyChanged(DirectionProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Direction - Vector3D.  Default value is new Vector3D(0,0,-1).
        /// </summary>
        public Vector3D Direction
        {
            get
            {
                return (Vector3D)GetValue(DirectionProperty);
            }
            set
            {
                SetValueInternal(DirectionProperty, value);
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
            return new DirectionalLight();
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
        ///     The DependencyProperty for the DirectionalLight.Direction property.
        /// </summary>
        public static readonly DependencyProperty DirectionProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal static Vector3D s_Direction = new Vector3D(0,0,-1);

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static DirectionalLight()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(DirectionalLight);
            DirectionProperty =
                  RegisterProperty("Direction",
                                   typeof(Vector3D),
                                   typeofThis,
                                   new Vector3D(0,0,-1),
                                   new PropertyChangedCallback(DirectionPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
