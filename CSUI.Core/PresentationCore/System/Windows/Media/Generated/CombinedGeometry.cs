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
    public sealed partial class CombinedGeometry : Geometry
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
        public new CombinedGeometry Clone()
        {
            return (CombinedGeometry)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new CombinedGeometry CloneCurrentValue()
        {
            return (CombinedGeometry)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void GeometryCombineModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CombinedGeometry target = ((CombinedGeometry) d);


            target.PropertyChanged(GeometryCombineModeProperty);
        }
        private static void Geometry1PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            CombinedGeometry target = ((CombinedGeometry)d);
            target.PropertyChanged(Geometry1Property);
        }
        private static void Geometry2PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            CombinedGeometry target = ((CombinedGeometry)d);
            target.PropertyChanged(Geometry2Property);
        }


        #region Public Properties

        /// <summary>
        ///     GeometryCombineMode - GeometryCombineMode.  Default value is GeometryCombineMode.Union.
        /// </summary>
        public GeometryCombineMode GeometryCombineMode
        {
            get
            {
                return (GeometryCombineMode)GetValue(GeometryCombineModeProperty);
            }
            set
            {
                SetValueInternal(GeometryCombineModeProperty, value);
            }
        }

        /// <summary>
        ///     Geometry1 - Geometry.  Default value is Geometry.Empty.
        /// </summary>
        public Geometry Geometry1
        {
            get
            {
                return (Geometry)GetValue(Geometry1Property);
            }
            set
            {
                SetValueInternal(Geometry1Property, value);
            }
        }

        /// <summary>
        ///     Geometry2 - Geometry.  Default value is Geometry.Empty.
        /// </summary>
        public Geometry Geometry2
        {
            get
            {
                return (Geometry)GetValue(Geometry2Property);
            }
            set
            {
                SetValueInternal(Geometry2Property, value);
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
            return new CombinedGeometry();
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
        //    GeometryCombineMode
        //    Geometry1
        //    Geometry2
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
        ///     The DependencyProperty for the CombinedGeometry.GeometryCombineMode property.
        /// </summary>
        public static readonly DependencyProperty GeometryCombineModeProperty;
        /// <summary>
        ///     The DependencyProperty for the CombinedGeometry.Geometry1 property.
        /// </summary>
        public static readonly DependencyProperty Geometry1Property;
        /// <summary>
        ///     The DependencyProperty for the CombinedGeometry.Geometry2 property.
        /// </summary>
        public static readonly DependencyProperty Geometry2Property;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal const GeometryCombineMode c_GeometryCombineMode = GeometryCombineMode.Union;
        internal static Geometry s_Geometry1 = Geometry.Empty;
        internal static Geometry s_Geometry2 = Geometry.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static CombinedGeometry()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.
            Debug.Assert(s_Geometry1 == null || s_Geometry1.IsFrozen,
                "Detected context bound default value CombinedGeometry.s_Geometry1 (See OS Bug #947272).");


            Debug.Assert(s_Geometry2 == null || s_Geometry2.IsFrozen,
                "Detected context bound default value CombinedGeometry.s_Geometry2 (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(CombinedGeometry);
            GeometryCombineModeProperty =
                  RegisterProperty("GeometryCombineMode",
                                   typeof(GeometryCombineMode),
                                   typeofThis,
                                   GeometryCombineMode.Union,
                                   new PropertyChangedCallback(GeometryCombineModePropertyChanged),
                                   new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsGeometryCombineModeValid),
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            Geometry1Property =
                  RegisterProperty("Geometry1",
                                   typeof(Geometry),
                                   typeofThis,
                                   Geometry.Empty,
                                   new PropertyChangedCallback(Geometry1PropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            Geometry2Property =
                  RegisterProperty("Geometry2",
                                   typeof(Geometry),
                                   typeofThis,
                                   Geometry.Empty,
                                   new PropertyChangedCallback(Geometry2PropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
