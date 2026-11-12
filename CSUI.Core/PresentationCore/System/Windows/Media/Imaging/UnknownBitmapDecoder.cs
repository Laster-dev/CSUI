// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.IO;
using MS.Internal;

namespace System.Windows.Media.Imaging
{
    #region UnknownBitmapDecoder

    /// <summary>
    /// UnknownBitmapDecoder represents a decoder for an unrecognized third-party or extension WIC codec.
    /// </summary>
    internal sealed class UnknownBitmapDecoder : BitmapDecoder
    {
        private UnknownBitmapDecoder()
        {
        }

        internal UnknownBitmapDecoder(
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

    #endregion UnknownBitmapDecoder
}
