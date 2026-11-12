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
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;


#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    public sealed partial class GeometryDrawing : Drawing
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
        public new GeometryDrawing Clone()
        {
            return (GeometryDrawing)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new GeometryDrawing CloneCurrentValue()
        {
            return (GeometryDrawing)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void BrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            GeometryDrawing target = ((GeometryDrawing)d);
            target.PropertyChanged(BrushProperty);
        }
        private static void PenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            GeometryDrawing target = ((GeometryDrawing)d);
            target.PropertyChanged(PenProperty);
        }
        private static void GeometryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            GeometryDrawing target = ((GeometryDrawing)d);
            target.PropertyChanged(GeometryProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Brush - Brush.  Default value is null.
        /// </summary>
        public Brush Brush
        {
            get
            {
                return (Brush)GetValue(BrushProperty);
            }
            set
            {
                SetValueInternal(BrushProperty, value);
            }
        }

        /// <summary>
        ///     Pen - Pen.  Default value is null.
        /// </summary>
        public Pen Pen
        {
            get
            {
                return (Pen)GetValue(PenProperty);
            }
            set
            {
                SetValueInternal(PenProperty, value);
            }
        }

        /// <summary>
        ///     Geometry - Geometry.  Default value is null.
        /// </summary>
        public Geometry Geometry
        {
            get
            {
                return (Geometry)GetValue(GeometryProperty);
            }
            set
            {
                SetValueInternal(GeometryProperty, value);
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
            return new GeometryDrawing();
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
        ///     The DependencyProperty for the GeometryDrawing.Brush property.
        /// </summary>
        public static readonly DependencyProperty BrushProperty;
        /// <summary>
        ///     The DependencyProperty for the GeometryDrawing.Pen property.
        /// </summary>
        public static readonly DependencyProperty PenProperty;
        /// <summary>
        ///     The DependencyProperty for the GeometryDrawing.Geometry property.
        /// </summary>
        public static readonly DependencyProperty GeometryProperty;

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

        static GeometryDrawing()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(GeometryDrawing);
            BrushProperty =
                  RegisterProperty("Brush",
                                   typeof(Brush),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(BrushPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            PenProperty =
                  RegisterProperty("Pen",
                                   typeof(Pen),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(PenPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            GeometryProperty =
                  RegisterProperty("Geometry",
                                   typeof(Geometry),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(GeometryPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
