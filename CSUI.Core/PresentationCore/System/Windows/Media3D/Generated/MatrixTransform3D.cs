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
    public sealed partial class MatrixTransform3D : Transform3D
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
        public new MatrixTransform3D Clone()
        {
            return (MatrixTransform3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new MatrixTransform3D CloneCurrentValue()
        {
            return (MatrixTransform3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void MatrixPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MatrixTransform3D target = ((MatrixTransform3D) d);


            target.PropertyChanged(MatrixProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Matrix - Matrix3D.  Default value is Matrix3D.Identity.
        /// </summary>
        public Matrix3D Matrix
        {
            get
            {
                return (Matrix3D)GetValue(MatrixProperty);
            }
            set
            {
                SetValueInternal(MatrixProperty, value);
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
            return new MatrixTransform3D();
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
        //    Matrix
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
        ///     The DependencyProperty for the MatrixTransform3D.Matrix property.
        /// </summary>
        public static readonly DependencyProperty MatrixProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal static Matrix3D s_Matrix = Matrix3D.Identity;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static MatrixTransform3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(MatrixTransform3D);
            MatrixProperty =
                  RegisterProperty("Matrix",
                                   typeof(Matrix3D),
                                   typeofThis,
                                   Matrix3D.Identity,
                                   new PropertyChangedCallback(MatrixPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
