// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using MS.Internal.PresentationCore;

namespace System.Windows.Media.Imaging
{
    #region ColorConvertedBitmap

    /// <summary>
    /// ColorConvertedBitmap provides ICC color profile transformation for a BitmapSource.
    /// </summary>
    public sealed partial class ColorConvertedBitmap : BitmapSource, ISupportInitialize
    {
        public ColorConvertedBitmap() : base(true)
        {
        }

        public ColorConvertedBitmap(
            BitmapSource source,
            ColorContext sourceColorContext,
            ColorContext destinationColorContext,
            PixelFormat format)
            : base(true)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(sourceColorContext);
            ArgumentNullException.ThrowIfNull(destinationColorContext);

            _bitmapInit.BeginInit();

            Source = source;
            SourceColorContext = sourceColorContext;
            DestinationColorContext = destinationColorContext;
            DestinationFormat = format;

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

        private void ClonePrequel(ColorConvertedBitmap otherColorConvertedBitmap)
        {
            BeginInit();
        }

        private void ClonePostscript(ColorConvertedBitmap otherColorConvertedBitmap)
        {
            EndInit();
        }

        internal override unsafe void FinalizeCreation()
        {
            _bitmapInit.EnsureInitializedComplete();
            BitmapSourceSafeMILHandle wicConverter = null;

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                try
                {
                    void* pTransformer = null;
                    HRESULT.Check(IWICImagingFactoryExVtbl.CreateColorTransformer(
                        (void*)factoryMaker.ImagingFactoryPtr,
                        &pTransformer));
                    wicConverter = new BitmapSourceSafeMILHandle((IntPtr)pTransformer);

                    lock (_syncObject)
                    {
                        Guid fmtDestFmt = DestinationFormat.Guid;
                        SafeMILHandle srcHandle = Source.WicSourceHandle;
                        SafeMILHandle srcCtxHandle = SourceColorContext.ColorContextHandle;
                        SafeMILHandle dstCtxHandle = DestinationColorContext.ColorContextHandle;

                        bool addRefConv = false;
                        bool addRefSrc = false;
                        bool addRefSrcCtx = false;
                        bool addRefDstCtx = false;

                        wicConverter.DangerousAddRef(ref addRefConv);
                        srcHandle.DangerousAddRef(ref addRefSrc);
                        srcCtxHandle.DangerousAddRef(ref addRefSrcCtx);
                        dstCtxHandle.DangerousAddRef(ref addRefDstCtx);

                        try
                        {
                            HRESULT.Check(IWICColorTransformVtbl.Initialize(
                                (void*)wicConverter.DangerousGetHandle(),
                                (void*)srcHandle.DangerousGetHandle(),
                                (void*)srcCtxHandle.DangerousGetHandle(),
                                (void*)dstCtxHandle.DangerousGetHandle(),
                                in fmtDestFmt));
                        }
                        finally
                        {
                            if (addRefDstCtx) dstCtxHandle.DangerousRelease();
                            if (addRefSrcCtx) srcCtxHandle.DangerousRelease();
                            if (addRefSrc) srcHandle.DangerousRelease();
                            if (addRefConv) wicConverter.DangerousRelease();
                        }
                    }

                    WicSourceHandle = wicConverter;
                    _isSourceCached = Source.IsSourceCached;
                    wicConverter = null;
                }
                catch
                {
                    _bitmapInit.Reset();
                    throw;
                }
                finally
                {
                    wicConverter?.Close();
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

            if (SourceColorContext == null)
            {
                if (throwIfInvalid)
                {
                    throw new InvalidOperationException(SR.Color_NullColorContext);
                }
                return false;
            }

            if (DestinationColorContext == null)
            {
                if (throwIfInvalid)
                {
                    throw new InvalidOperationException(SR.Format(SR.Image_NoArgument, "DestinationColorContext"));
                }
                return false;
            }

            return true;
        }

        private void SourceColorContextPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _sourceColorContext = e.NewValue as ColorContext;
            }
        }

        private void DestinationColorContextPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _destinationColorContext = e.NewValue as ColorContext;
            }
        }

        private void DestinationFormatPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _destinationFormat = (PixelFormat)e.NewValue;
            }
        }

        private static object CoerceSource(DependencyObject d, object value)
        {
            ColorConvertedBitmap bitmap = (ColorConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._source;
            }
            return value;
        }

        private static object CoerceSourceColorContext(DependencyObject d, object value)
        {
            ColorConvertedBitmap bitmap = (ColorConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._sourceColorContext;
            }
            return value;
        }

        private static object CoerceDestinationColorContext(DependencyObject d, object value)
        {
            ColorConvertedBitmap bitmap = (ColorConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._destinationColorContext;
            }
            return value;
        }

        private static object CoerceDestinationFormat(DependencyObject d, object value)
        {
            ColorConvertedBitmap bitmap = (ColorConvertedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._destinationFormat;
            }
            return value;
        }

        #region Data members

        private BitmapSource _source;
        private ColorContext _sourceColorContext;
        private ColorContext _destinationColorContext;
        private PixelFormat _destinationFormat;

        #endregion Data members
    }

    #endregion ColorConvertedBitmap
}
