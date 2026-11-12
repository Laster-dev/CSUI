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
    public sealed partial class MeshGeometry3D : Geometry3D
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
        public new MeshGeometry3D Clone()
        {
            return (MeshGeometry3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new MeshGeometry3D CloneCurrentValue()
        {
            return (MeshGeometry3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void PositionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {




            MeshGeometry3D target = ((MeshGeometry3D) d);


            target.PropertyChanged(PositionsProperty);
        }
        private static void NormalsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {




            MeshGeometry3D target = ((MeshGeometry3D) d);


            target.PropertyChanged(NormalsProperty);
        }
        private static void TextureCoordinatesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {




            MeshGeometry3D target = ((MeshGeometry3D) d);


            target.PropertyChanged(TextureCoordinatesProperty);
        }
        private static void TriangleIndicesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {




            MeshGeometry3D target = ((MeshGeometry3D) d);


            target.PropertyChanged(TriangleIndicesProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Positions - Point3DCollection.  Default value is new FreezableDefaultValueFactory(Point3DCollection.Empty).
        /// </summary>
        public Point3DCollection Positions
        {
            get
            {
                return (Point3DCollection)GetValue(PositionsProperty);
            }
            set
            {
                SetValueInternal(PositionsProperty, value);
            }
        }

        /// <summary>
        ///     Normals - Vector3DCollection.  Default value is new FreezableDefaultValueFactory(Vector3DCollection.Empty).
        /// </summary>
        public Vector3DCollection Normals
        {
            get
            {
                return (Vector3DCollection)GetValue(NormalsProperty);
            }
            set
            {
                SetValueInternal(NormalsProperty, value);
            }
        }

        /// <summary>
        ///     TextureCoordinates - PointCollection.  Default value is new FreezableDefaultValueFactory(PointCollection.Empty).
        /// </summary>
        public PointCollection TextureCoordinates
        {
            get
            {
                return (PointCollection)GetValue(TextureCoordinatesProperty);
            }
            set
            {
                SetValueInternal(TextureCoordinatesProperty, value);
            }
        }

        /// <summary>
        ///     TriangleIndices - Int32Collection.  Default value is new FreezableDefaultValueFactory(Int32Collection.Empty).
        /// </summary>
        public Int32Collection TriangleIndices
        {
            get
            {
                return (Int32Collection)GetValue(TriangleIndicesProperty);
            }
            set
            {
                SetValueInternal(TriangleIndicesProperty, value);
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
            return new MeshGeometry3D();
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
        //    Positions
        //    Normals
        //    TextureCoordinates
        //    TriangleIndices
        //
        internal override int EffectiveValuesInitialSize
        {
            get
            {
                return 4;
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
        ///     The DependencyProperty for the MeshGeometry3D.Positions property.
        /// </summary>
        public static readonly DependencyProperty PositionsProperty;
        /// <summary>
        ///     The DependencyProperty for the MeshGeometry3D.Normals property.
        /// </summary>
        public static readonly DependencyProperty NormalsProperty;
        /// <summary>
        ///     The DependencyProperty for the MeshGeometry3D.TextureCoordinates property.
        /// </summary>
        public static readonly DependencyProperty TextureCoordinatesProperty;
        /// <summary>
        ///     The DependencyProperty for the MeshGeometry3D.TriangleIndices property.
        /// </summary>
        public static readonly DependencyProperty TriangleIndicesProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        internal static Point3DCollection s_Positions = Point3DCollection.Empty;
        internal static Vector3DCollection s_Normals = Vector3DCollection.Empty;
        internal static PointCollection s_TextureCoordinates = PointCollection.Empty;
        internal static Int32Collection s_TriangleIndices = Int32Collection.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static MeshGeometry3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.
            Debug.Assert(s_Positions == null || s_Positions.IsFrozen,
                "Detected context bound default value MeshGeometry3D.s_Positions (See OS Bug #947272).");


            Debug.Assert(s_Normals == null || s_Normals.IsFrozen,
                "Detected context bound default value MeshGeometry3D.s_Normals (See OS Bug #947272).");


            Debug.Assert(s_TextureCoordinates == null || s_TextureCoordinates.IsFrozen,
                "Detected context bound default value MeshGeometry3D.s_TextureCoordinates (See OS Bug #947272).");


            Debug.Assert(s_TriangleIndices == null || s_TriangleIndices.IsFrozen,
                "Detected context bound default value MeshGeometry3D.s_TriangleIndices (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(MeshGeometry3D);
            PositionsProperty =
                  RegisterProperty("Positions",
                                   typeof(Point3DCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(Point3DCollection.Empty),
                                   new PropertyChangedCallback(PositionsPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            NormalsProperty =
                  RegisterProperty("Normals",
                                   typeof(Vector3DCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(Vector3DCollection.Empty),
                                   new PropertyChangedCallback(NormalsPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            TextureCoordinatesProperty =
                  RegisterProperty("TextureCoordinates",
                                   typeof(PointCollection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(PointCollection.Empty),
                                   new PropertyChangedCallback(TextureCoordinatesPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            TriangleIndicesProperty =
                  RegisterProperty("TriangleIndices",
                                   typeof(Int32Collection),
                                   typeofThis,
                                   new FreezableDefaultValueFactory(Int32Collection.Empty),
                                   new PropertyChangedCallback(TriangleIndicesPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
