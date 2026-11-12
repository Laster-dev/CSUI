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
    public sealed partial class MatrixCamera : Camera
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
        public new MatrixCamera Clone()
        {
            return (MatrixCamera)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new MatrixCamera CloneCurrentValue()
        {
            return (MatrixCamera)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ViewMatrixPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MatrixCamera target = ((MatrixCamera) d);


            target.PropertyChanged(ViewMatrixProperty);
        }
        private static void ProjectionMatrixPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MatrixCamera target = ((MatrixCamera) d);


            target.PropertyChanged(ProjectionMatrixProperty);
        }


        #region Public Properties

        /// <summary>
        ///     ViewMatrix - Matrix3D.  Default value is Matrix3D.Identity.
        /// </summary>
        public Matrix3D ViewMatrix
        {
            get
            {
                return (Matrix3D)GetValue(ViewMatrixProperty);
            }
            set
            {
                SetValueInternal(ViewMatrixProperty, value);
            }
        }

        /// <summary>
        ///     ProjectionMatrix - Matrix3D.  Default value is Matrix3D.Identity.
        /// </summary>
        public Matrix3D ProjectionMatrix
        {
            get
            {
                return (Matrix3D)GetValue(ProjectionMatrixProperty);
            }
            set
            {
                SetValueInternal(ProjectionMatrixProperty, value);
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
            return new MatrixCamera();
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
        ///     The DependencyProperty for the MatrixCamera.ViewMatrix property.
        /// </summary>
        public static readonly DependencyProperty ViewMatrixProperty;
        /// <summary>
        ///     The DependencyProperty for the MatrixCamera.ProjectionMatrix property.
        /// </summary>
        public static readonly DependencyProperty ProjectionMatrixProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal static Matrix3D s_ViewMatrix = Matrix3D.Identity;
        internal static Matrix3D s_ProjectionMatrix = Matrix3D.Identity;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static MatrixCamera()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(MatrixCamera);
            ViewMatrixProperty =
                  RegisterProperty("ViewMatrix",
                                   typeof(Matrix3D),
                                   typeofThis,
                                   Matrix3D.Identity,
                                   new PropertyChangedCallback(ViewMatrixPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ProjectionMatrixProperty =
                  RegisterProperty("ProjectionMatrix",
                                   typeof(Matrix3D),
                                   typeofThis,
                                   Matrix3D.Identity,
                                   new PropertyChangedCallback(ProjectionMatrixPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
