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
    public sealed partial class ImageDrawing : Drawing
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
        public new ImageDrawing Clone()
        {
            return (ImageDrawing)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ImageDrawing CloneCurrentValue()
        {
            return (ImageDrawing)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void ImageSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange && (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            ImageDrawing target = ((ImageDrawing)d);
            target.PropertyChanged(ImageSourceProperty);
        }
        private static void RectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ImageDrawing target = ((ImageDrawing) d);


            target.PropertyChanged(RectProperty);
        }


        #region Public Properties

        /// <summary>
        ///     ImageSource - ImageSource.  Default value is null.
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return (ImageSource)GetValue(ImageSourceProperty);
            }
            set
            {
                SetValueInternal(ImageSourceProperty, value);
            }
        }

        /// <summary>
        ///     Rect - Rect.  Default value is Rect.Empty.
        /// </summary>
        public Rect Rect
        {
            get
            {
                return (Rect)GetValue(RectProperty);
            }
            set
            {
                SetValueInternal(RectProperty, value);
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
            return new ImageDrawing();
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
        ///     The DependencyProperty for the ImageDrawing.ImageSource property.
        /// </summary>
        public static readonly DependencyProperty ImageSourceProperty;
        /// <summary>
        ///     The DependencyProperty for the ImageDrawing.Rect property.
        /// </summary>
        public static readonly DependencyProperty RectProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal static Rect s_Rect = Rect.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static ImageDrawing()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(ImageDrawing);
            ImageSourceProperty =
                  RegisterProperty("ImageSource",
                                   typeof(ImageSource),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(ImageSourcePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            RectProperty =
                  RegisterProperty("Rect",
                                   typeof(Rect),
                                   typeofThis,
                                   Rect.Empty,
                                   new PropertyChangedCallback(RectPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
