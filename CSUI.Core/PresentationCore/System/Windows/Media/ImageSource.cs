// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//

using MS.Internal;
using System.Windows.Media.Animation;


#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    #region ImageSource

    /// <summary>
    /// Interface for Bitmap Sources, included decoders and effects
    /// </summary>
    public abstract partial class ImageSource : Animatable
    {
        #region Constructor

        private static bool s_typeConvertersRegistered;

        static ImageSource()
        {
            EnsureTypeConvertersRegistered();
        }

        internal static void EnsureTypeConvertersRegistered()
        {
            if (s_typeConvertersRegistered)
                return;
            s_typeConvertersRegistered = true;
            System.Windows.Data.TypeConverterRegistry.Register<ImageSource>(new ImageSourceConverter());
            System.Windows.Data.TypeConverterRegistry.Register<PixelFormat>(new PixelFormatConverter());
        }

        /// <summary>
        /// Don't allow 3rd party extensibility.
        /// </summary>
        internal ImageSource()
        {
        }


        #endregion Constructor

        /// <summary>
        /// Get the width of the image in measure units (96ths of an inch).
        /// </summary>
        public abstract double Width
        {
            get;
        }

        /// <summary>
        /// Get the height of the image in measure units (96ths of an inch).
        /// </summary>
        public abstract double Height
        {
            get;
        }

        /// <summary>
        /// Get the metadata associated with this image source
        /// </summary>
        public abstract ImageMetadata Metadata
        {
            get;
        }

        /// <summary>
        /// Get the Size associated with this image source
        /// </summary>
        internal virtual Size Size
        {
            get
            {
                return new Size(Width, Height);            
            }
        }

        #region ToInstanceDescriptor
        /// <summary>
        /// Can serialze "this" to a string
        /// </summary>
        internal virtual bool CanSerializeToString()
        {
            return false;
        }

        #endregion     

        /// <summary>
        /// Converts pixels to DIPs in a way consistent with MIL. Protected here is okay
        /// because ImageSource isn't extensible by 3rd parties.
        /// </summary>
        internal virtual unsafe void DrawD2D(void* d2dContext, Rect rectangle)
        {
        }

        internal virtual bool TryCopyIconPixelsBgra32(out int pixelWidth, out int pixelHeight, out byte[] bgra32Pixels)
        {
            pixelWidth = 0;
            pixelHeight = 0;
            bgra32Pixels = null;
            return false;
        }

        protected static double PixelsToDIPs(double dpi, int pixels)
        {
            // Obtain the natural size in MIL Device Independant Pixels (DIPs, or 1/96") of the bitmap.
            // This is: (Bitmap Pixels) / (Bitmap DotsPerInch) * (DIPs per inch)

            float dpif = (float)dpi;

            // To be consistent with BitmapBrush
            //
            // Floating-point precision is used to maintain consistent
            // logic with BitmapBrush DPI scaling, which is implemented in
            // unmanaged code using single-precision math.  Any changes to
            // this logic must also be updated in the UCE BitmapBrush
            // resource to maintain this consistency.

            if (dpif < 0.0F || FloatUtil.IsCloseToDivideByZero(96.0F, dpif))
            {
                return pixels;
            }

            return (double)(pixels * (96.0F / dpif));
        }
    }

    #endregion // ImageSource
}

