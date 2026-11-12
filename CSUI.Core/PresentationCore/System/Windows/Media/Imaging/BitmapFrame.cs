// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Cache;
using System.Windows.Markup;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// BitmapFrame abstract class
    /// </summary>
    public abstract class BitmapFrame : BitmapSource, IUriContext
    {
        protected BitmapFrame()
        {
        }

        internal BitmapFrame(bool useVirtuals) : base(useVirtuals)
        {
        }

        internal static BitmapFrame CreateFromUriOrStream(
            Uri baseUri,
            Uri uri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            RequestCachePolicy uriCachePolicy)
        {
            if (uri != null)
            {
                Debug.Assert(stream == null, "Both stream and uri are non-null");

                BitmapDecoder decoder = BitmapDecoder.CreateFromUriOrStream(
                    baseUri,
                    uri,
                    null,
                    createOptions,
                    cacheOption,
                    uriCachePolicy,
                    true);

                if (decoder.Frames.Count == 0)
                {
                    throw new ArgumentException(SR.Image_NoDecodeFrames, nameof(uri));
                }

                return decoder.Frames[0];
            }
            else
            {
                Debug.Assert(stream != null, "Both stream and uri are null");

                BitmapDecoder decoder = BitmapDecoder.Create(
                    stream,
                    createOptions,
                    cacheOption);

                if (decoder.Frames.Count == 0)
                {
                    throw new ArgumentException(SR.Image_NoDecodeFrames, nameof(stream));
                }

                return decoder.Frames[0];
            }
        }

        public static BitmapFrame Create(Uri bitmapUri)
        {
            return Create(bitmapUri, null);
        }

        public static BitmapFrame Create(Uri bitmapUri, RequestCachePolicy uriCachePolicy)
        {
            ArgumentNullException.ThrowIfNull(bitmapUri);

            return CreateFromUriOrStream(
                null,
                bitmapUri,
                null,
                BitmapCreateOptions.None,
                BitmapCacheOption.Default,
                uriCachePolicy);
        }

        public static BitmapFrame Create(
            Uri bitmapUri,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption)
        {
            return Create(bitmapUri, createOptions, cacheOption, null);
        }

        public static BitmapFrame Create(
            Uri bitmapUri,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            RequestCachePolicy uriCachePolicy)
        {
            ArgumentNullException.ThrowIfNull(bitmapUri);

            return CreateFromUriOrStream(
                null,
                bitmapUri,
                null,
                createOptions,
                cacheOption,
                uriCachePolicy);
        }

        public static BitmapFrame Create(Stream bitmapStream)
        {
            ArgumentNullException.ThrowIfNull(bitmapStream);

            return CreateFromUriOrStream(
                null,
                null,
                bitmapStream,
                BitmapCreateOptions.None,
                BitmapCacheOption.Default,
                null);
        }

        public static BitmapFrame Create(
            Stream bitmapStream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption)
        {
            ArgumentNullException.ThrowIfNull(bitmapStream);

            return CreateFromUriOrStream(
                null,
                null,
                bitmapStream,
                createOptions,
                cacheOption,
                null);
        }

        public static BitmapFrame Create(BitmapSource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            BitmapMetadata metadata = null;

            try
            {
                metadata = source.Metadata as BitmapMetadata;
            }
            catch (NotSupportedException)
            {
            }

            if (metadata != null)
            {
                metadata = metadata.Clone();
            }

            return new BitmapFrameEncode(source, null, metadata, null);
        }

        public static BitmapFrame Create(BitmapSource source, BitmapSource thumbnail)
        {
            ArgumentNullException.ThrowIfNull(source);

            BitmapMetadata metadata = null;

            try
            {
                metadata = source.Metadata as BitmapMetadata;
            }
            catch (NotSupportedException)
            {
            }

            if (metadata != null)
            {
                metadata = metadata.Clone();
            }

            return BitmapFrame.Create(source, thumbnail, metadata, null);
        }

        public static BitmapFrame Create(
            BitmapSource source,
            BitmapSource thumbnail,
            BitmapMetadata metadata,
            ReadOnlyCollection<ColorContext> colorContexts)
        {
            ArgumentNullException.ThrowIfNull(source);

            return new BitmapFrameEncode(source, thumbnail, metadata, colorContexts);
        }

        public abstract Uri BaseUri { get; set; }

        public abstract BitmapSource Thumbnail { get; }

        public abstract BitmapDecoder Decoder { get; }

        public abstract ReadOnlyCollection<ColorContext> ColorContexts { get; }

        internal override ReadOnlyCollection<ColorContext> InternalColorContexts => ColorContexts;

        public abstract InPlaceBitmapMetadataWriter CreateInPlaceBitmapMetadataWriter();

        internal virtual BitmapMetadata InternalMetadata
        {
            get { return null; }
            set { throw new NotImplementedException(); }
        }

        internal BitmapSource _thumbnail = null;

        internal BitmapMetadata _metadata;

        internal ReadOnlyCollection<ColorContext> _readOnlycolorContexts;
    }
}
