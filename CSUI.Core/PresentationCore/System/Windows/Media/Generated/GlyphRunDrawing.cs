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
    public sealed partial class GlyphRunDrawing : Drawing
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
        public new GlyphRunDrawing Clone()
        {
            return (GlyphRunDrawing)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new GlyphRunDrawing CloneCurrentValue()
        {
            return (GlyphRunDrawing)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void GlyphRunPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GlyphRunDrawing target = ((GlyphRunDrawing)d);
            target.PropertyChanged(GlyphRunProperty);
        }
        private static void ForegroundBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            GlyphRunDrawing target = ((GlyphRunDrawing)d);
            target.PropertyChanged(ForegroundBrushProperty);
        }


        #region Public Properties

        /// <summary>
        ///     GlyphRun - GlyphRun.  Default value is null.
        /// </summary>
        public GlyphRun GlyphRun
        {
            get
            {
                return (GlyphRun)GetValue(GlyphRunProperty);
            }
            set
            {
                SetValueInternal(GlyphRunProperty, value);
            }
        }

        /// <summary>
        ///     ForegroundBrush - Brush.  Default value is null.
        /// </summary>
        public Brush ForegroundBrush
        {
            get
            {
                return (Brush)GetValue(ForegroundBrushProperty);
            }
            set
            {
                SetValueInternal(ForegroundBrushProperty, value);
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
            return new GlyphRunDrawing();
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
        ///     The DependencyProperty for the GlyphRunDrawing.GlyphRun property.
        /// </summary>
        public static readonly DependencyProperty GlyphRunProperty;
        /// <summary>
        ///     The DependencyProperty for the GlyphRunDrawing.ForegroundBrush property.
        /// </summary>
        public static readonly DependencyProperty ForegroundBrushProperty;

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

        static GlyphRunDrawing()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(GlyphRunDrawing);
            GlyphRunProperty =
                  RegisterProperty("GlyphRun",
                                   typeof(GlyphRun),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(GlyphRunPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            ForegroundBrushProperty =
                  RegisterProperty("ForegroundBrush",
                                   typeof(Brush),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(ForegroundBrushPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
