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
    public sealed partial class GeometryModel3D : Model3D
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
        public new GeometryModel3D Clone()
        {
            return (GeometryModel3D)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new GeometryModel3D CloneCurrentValue()
        {
            return (GeometryModel3D)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void GeometryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {




            // The first change to the default value of a mutable collection property (e.g. GeometryGroup.Children) 
            // will promote the property value from a default value to a local value. This is technically a sub-property 
            // change because the collection was changed and not a new collection set (GeometryGroup.Children.
            // Add versus GeometryGroup.Children = myNewChildrenCollection). However, we never marshalled 
            // the default value to the compositor. If the property changes from a default value, the new local value 
            // needs to be marshalled to the compositor. We detect this scenario with the second condition 
            // e.OldValueSource != e.NewValueSource. Specifically in this scenario the OldValueSource will be 
            // Default and the NewValueSource will be Local.
            if (e.IsASubPropertyChange && 
               (e.OldValueSource == e.NewValueSource))
            {
                return;
            }


            GeometryModel3D target = ((GeometryModel3D) d);


            target.PropertyChanged(GeometryProperty);
        }
        private static void MaterialPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            GeometryModel3D target = ((GeometryModel3D) d);


            target.MaterialPropertyChangedHook(e);



            // The first change to the default value of a mutable collection property (e.g. GeometryGroup.Children) 
            // will promote the property value from a default value to a local value. This is technically a sub-property 
            // change because the collection was changed and not a new collection set (GeometryGroup.Children.
            // Add versus GeometryGroup.Children = myNewChildrenCollection). However, we never marshalled 
            // the default value to the compositor. If the property changes from a default value, the new local value 
            // needs to be marshalled to the compositor. We detect this scenario with the second condition 
            // e.OldValueSource != e.NewValueSource. Specifically in this scenario the OldValueSource will be 
            // Default and the NewValueSource will be Local.
            if (e.IsASubPropertyChange && 
               (e.OldValueSource == e.NewValueSource))
            {
                return;
            }



            target.PropertyChanged(MaterialProperty);
        }
        private static void BackMaterialPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            GeometryModel3D target = ((GeometryModel3D) d);


            target.BackMaterialPropertyChangedHook(e);



            // The first change to the default value of a mutable collection property (e.g. GeometryGroup.Children) 
            // will promote the property value from a default value to a local value. This is technically a sub-property 
            // change because the collection was changed and not a new collection set (GeometryGroup.Children.
            // Add versus GeometryGroup.Children = myNewChildrenCollection). However, we never marshalled 
            // the default value to the compositor. If the property changes from a default value, the new local value 
            // needs to be marshalled to the compositor. We detect this scenario with the second condition 
            // e.OldValueSource != e.NewValueSource. Specifically in this scenario the OldValueSource will be 
            // Default and the NewValueSource will be Local.
            if (e.IsASubPropertyChange && 
               (e.OldValueSource == e.NewValueSource))
            {
                return;
            }



            target.PropertyChanged(BackMaterialProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Geometry - Geometry3D.  Default value is null.
        /// </summary>
        public Geometry3D Geometry
        {
            get
            {
                return (Geometry3D)GetValue(GeometryProperty);
            }
            set
            {
                SetValueInternal(GeometryProperty, value);
            }
        }

        /// <summary>
        ///     Material - Material.  Default value is null.
        /// </summary>
        public Material Material
        {
            get
            {
                return (Material)GetValue(MaterialProperty);
            }
            set
            {
                SetValueInternal(MaterialProperty, value);
            }
        }

        /// <summary>
        ///     BackMaterial - Material.  Default value is null.
        /// </summary>
        public Material BackMaterial
        {
            get
            {
                return (Material)GetValue(BackMaterialProperty);
            }
            set
            {
                SetValueInternal(BackMaterialProperty, value);
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
            return new GeometryModel3D();
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
        ///     The DependencyProperty for the GeometryModel3D.Geometry property.
        /// </summary>
        public static readonly DependencyProperty GeometryProperty;
        /// <summary>
        ///     The DependencyProperty for the GeometryModel3D.Material property.
        /// </summary>
        public static readonly DependencyProperty MaterialProperty;
        /// <summary>
        ///     The DependencyProperty for the GeometryModel3D.BackMaterial property.
        /// </summary>
        public static readonly DependencyProperty BackMaterialProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields



        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static GeometryModel3D()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(GeometryModel3D);
            GeometryProperty =
                  RegisterProperty("Geometry",
                                   typeof(Geometry3D),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(GeometryPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            MaterialProperty =
                  RegisterProperty("Material",
                                   typeof(Material),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(MaterialPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            BackMaterialProperty =
                  RegisterProperty("BackMaterial",
                                   typeof(Material),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(BackMaterialPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
