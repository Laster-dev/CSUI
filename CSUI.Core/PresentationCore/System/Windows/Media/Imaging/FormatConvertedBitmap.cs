// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using MS.Internal.PresentationCore;

namespace System.Windows.Media.Imaging
{
    #region FormatConvertedBitmap

    /// <summary>
    /// FormatConvertedBitmap provides format conversion functionality for a BitmapSource.
    /// </summary>
    public sealed partial class FormatConvertedBitmap : BitmapSource, ISupportInitialize
    {
        public FormatConvertedBitmap() : base(true)
        {
        }

        public FormatConvertedBitmap(
            BitmapSource source,
            PixelFormat destinationFormat,
            BitmapPalette destinationPalette,
            double alphaThreshold)
            : base(true)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (alphaThreshold < 0.0 || alphaThreshold > 100.0)
            {
                throw new ArgumentException(SR.Image_AlphaThresholdOutOfRange);
            }

            _bitmapInit.BeginInit();

            Source = source;
            DestinationFormat = destinationFormat;
            DestinationPalette = destinationPalette;
            AlphaThreshold = alphaThreshold;

            _bitmapInit.EndInit();
            FinalizeCreation();
        }

        public void BeginInit()
        {
            WritePreamble();
            _bitmapInit.BeginInit();
        }

        public void EndInit()
        {
            WritePreamble();
            _bitmapInit.EndInit();

            IsValidForFinalizeCreation(throwIfInvalid: true);
            FinalizeCreation();
        }

        private void ClonePrequel(FormatConvertedBitmap otherFormatConvertedBitmap)
        {
            BeginInit();
        }

        private void ClonePostscript(FormatConvertedBitmap otherFormatConvertedBitmap)
        {
            EndInit();
        }

        internal override unsafe void FinalizeCreation()
        {
            _bitmapInit.EnsureInitializedComplete();
            BitmapSourceSafeMILHandle wicFormatter = null;

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                try
                {
                    void* wicFactory = (void*)factoryMaker.ImagingFactoryPtr;
                    void* pFormatter = null;
                    HRESULT.Check(IWICImagingFactoryVtbl.CreateFormatConverter(
                        wicFactory,
                        &pFormatter));
                    wicFormatter = new BitmapSourceSafeMILHandle((IntPtr)pFormatter);

                    SafeMILHandle internalPalette = DestinationPalette?.InternalPalette;
                    Guid format = DestinationFormat.Guid;

                    lock (_syncObject)
                    {
                        SafeMILHandle srcHandle = Source.WicSourceHandle;
                        bool addRefFmt = false;
                        bool addRefSrc = false;
                        bool addRefPal = false;
                        wicFormatter.DangerousAddRef(ref addRefFmt);
                        srcHandle.DangerousAddRef(ref addRefSrc);
                        if (internalPalette != null && !internalPalette.IsInvalid)
                        {
                            internalPalette.DangerousAddRef(ref addRefPal);
                        }

                        try
                        {
                            void* pPalette = (internalPalette != null && !internalPalette.IsInvalid)
                                ? (void*)internalPalette.DangerousGetHandle()
                                : null;

                            HRESULT.Check(IWICFormatConverterVtbl.Initialize(
                                (void*)wicFormatter.DangerousGetHandle(),
                                (void*)srcHandle.DangerousGetHandle(),
                                in format,
                                (uint)DitherType.DitherTypeErrorDiffusion,
                                pPalette,
                                AlphaThreshold,
                                (uint)WICPaletteType.WICPaletteTypeOptimal));
                        }
                        finally
                        {
                            if (addRefPal) internalPalette.DangerousRelease();
                            if (addRefSrc) srcHandle.DangerousRelease();
                            if (addRefFmt) wicFormatter.DangerousRelease();
                        }
                    }

                    WicSourceHandle = wicFormatter;
                    _isSourceCached = false;
                    wicFormatter = null;
                }
                catch
                {
                    _bitmapInit.Reset();
                    throw;
                }
                finally
                {
                    wicFormatter?.Close();
                }
            }

            CreationCompleted = true;
            UpdateCachedSettings();
        }

        private void SourcePropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                BitmapSource newSource = e.NewValue as BitmapSource;
                _source = newSource;
                RegisterDownloadEventSource(_source);
                _syncObject = (newSource != null) ? newSource.SyncObject : _bitmapInit;
            }
        }

        internal override bool IsValidForFinalizeCreation(bool throwIfInvalid)
        {
            if (Source == null)
            {
                if (throwIfInvalid)
                {
                    throw new InvalidOperationException(SR.Format(SR.Image_NoArgument, "Source"));
                }
                return false;
            }

            if (DestinationFormat.Palettized)
            {
                if (DestinationPalette == null)
                {
                    if (throwIfInvalid)
                    {
                        throw new InvalidOperationException(SR.Image_IndexedPixelFormatRequiresPalette);
                    }
                    return false;
                }
                else if ((1 << DestinationFormat.BitsPerPixel) < DestinationPalette.Colors.Count)
                {
                    if (throwIfInvalid)
                    {
                        throw new InvalidOperationException(SR.Image_PaletteColorsDoNotMatchFormat);
                    }
                    return false;
                }
            }

            return true;
        }

        private void DestinationFormatPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _destinationFormat = (PixelFormat)e.NewValue;
            }
        }

        private void DestinationPalettePropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _destinationPalette = e.NewValue as BitmapPalette;
            }
        }

        private void AlphaThresholdPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _alphaThreshold = (double)e.NewValue;
            }
        }

        private static object CoerceSource(DependencyObject d, object value)
        {
            FormatConvertedBitmap bitmap = (FormatConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._source;
            }
            return value;
        }

        private static object CoerceDestinationFormat(DependencyObject d, object value)
        {
            FormatConvertedBitmap bitmap = (FormatConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._destinationFormat;
            }

            if (((PixelFormat)value).Format == PixelFormatEnum.Default)
            {
                if (bitmap.Source != null)
                {
                    return bitmap.Source.Format;
                }
                return bitmap._destinationFormat;
            }

            return value;
        }

        private static object CoerceDestinationPalette(DependencyObject d, object value)
        {
            FormatConvertedBitmap bitmap = (FormatConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._destinationPalette;
            }
            return value;
        }

        private static object CoerceAlphaThreshold(DependencyObject d, object value)
        {
            FormatConvertedBitmap bitmap = (FormatConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._alphaThreshold;
            }
            return value;
        }

        #region Data Members

        private BitmapSource _source;
        private PixelFormat _destinationFormat;
        private BitmapPalette _destinationPalette;
        private double _alphaThreshold;

        #endregion Data Members
    }

    #endregion FormatConvertedBitmap
}
