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
    public sealed partial class BitmapCache : CacheMode
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
        public new BitmapCache Clone()
        {
            return (BitmapCache)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new BitmapCache CloneCurrentValue()
        {
            return (BitmapCache)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void RenderAtScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCache target = ((BitmapCache) d);


            target.PropertyChanged(RenderAtScaleProperty);
        }
        private static void SnapsToDevicePixelsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCache target = ((BitmapCache) d);


            target.PropertyChanged(SnapsToDevicePixelsProperty);
        }
        private static void EnableClearTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapCache target = ((BitmapCache) d);


            target.PropertyChanged(EnableClearTypeProperty);
        }


        #region Public Properties

        /// <summary>
        ///     RenderAtScale - double.  Default value is 1.0.
        /// </summary>
        public double RenderAtScale
        {
            get
            {
                return (double)GetValue(RenderAtScaleProperty);
            }
            set
            {
                SetValueInternal(RenderAtScaleProperty, value);
            }
        }

        /// <summary>
        ///     SnapsToDevicePixels - bool.  Default value is false.
        /// </summary>
        public bool SnapsToDevicePixels
        {
            get
            {
                return (bool)GetValue(SnapsToDevicePixelsProperty);
            }
            set
            {
                SetValueInternal(SnapsToDevicePixelsProperty, BooleanBoxes.Box(value));
            }
        }

        /// <summary>
        ///     EnableClearType - bool.  Default value is false.
        /// </summary>
        public bool EnableClearType
        {
            get
            {
                return (bool)GetValue(EnableClearTypeProperty);
            }
            set
            {
                SetValueInternal(EnableClearTypeProperty, BooleanBoxes.Box(value));
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
            return new BitmapCache();
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
        ///     The DependencyProperty for the BitmapCache.RenderAtScale property.
        /// </summary>
        public static readonly DependencyProperty RenderAtScaleProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapCache.SnapsToDevicePixels property.
        /// </summary>
        public static readonly DependencyProperty SnapsToDevicePixelsProperty;
        /// <summary>
        ///     The DependencyProperty for the BitmapCache.EnableClearType property.
        /// </summary>
        public static readonly DependencyProperty EnableClearTypeProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields




        internal const double c_RenderAtScale = 1.0;
        internal const bool c_SnapsToDevicePixels = false;
        internal const bool c_EnableClearType = false;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static BitmapCache()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.


            // Initializations
            Type typeofThis = typeof(BitmapCache);
            RenderAtScaleProperty =
                  RegisterProperty("RenderAtScale",
                                   typeof(double),
                                   typeofThis,
                                   1.0,
                                   new PropertyChangedCallback(RenderAtScalePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ true,
                                   /* coerceValueCallback */ null);
            SnapsToDevicePixelsProperty =
                  RegisterProperty("SnapsToDevicePixels",
                                   typeof(bool),
                                   typeofThis,
                                   false,
                                   new PropertyChangedCallback(SnapsToDevicePixelsPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
            EnableClearTypeProperty =
                  RegisterProperty("EnableClearType",
                                   typeof(bool),
                                   typeofThis,
                                   false,
                                   new PropertyChangedCallback(EnableClearTypePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ null);
        }



        #endregion Constructors
    }
}
