// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.IO;
using MS.Internal;

namespace System.Windows.Media.Imaging
{
    #region TiffBitmapDecoder

    /// <summary>
    /// TiffBitmapDecoder represents a decoder for the TIFF image format.
    /// </summary>
    public sealed class TiffBitmapDecoder : BitmapDecoder
    {
        private TiffBitmapDecoder()
        {
        }

        public TiffBitmapDecoder(
            Uri bitmapUri,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption)
            : base(bitmapUri, createOptions, cacheOption, MILGuidData.GUID_ContainerFormatTiff)
        {
        }

        public TiffBitmapDecoder(
            Stream bitmapStream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption)
            : base(bitmapStream, createOptions, cacheOption, MILGuidData.GUID_ContainerFormatTiff)
        {
        }

        internal TiffBitmapDecoder(
            SafeMILHandle decoderHandle,
            BitmapDecoder decoder,
            Uri baseUri,
            Uri uri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            bool insertInDecoderCache,
            bool closeStream)
            : base(decoderHandle, decoder, baseUri, uri, stream, createOptions, cacheOption, insertInDecoderCache, closeStream)
        {
        }

        internal override void SealObject()
        {
            throw new NotImplementedException();
        }
    }

    #endregion TiffBitmapDecoder
}
