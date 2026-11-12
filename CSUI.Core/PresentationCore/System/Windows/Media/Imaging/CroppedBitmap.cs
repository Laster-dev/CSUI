// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using MS.Internal.PresentationCore;

namespace System.Windows.Media.Imaging
{
    #region CroppedBitmap

    /// <summary>
    /// CroppedBitmap provides cropping functionality for a BitmapSource.
    /// </summary>
    public sealed partial class CroppedBitmap : BitmapSource, ISupportInitialize
    {
        public CroppedBitmap() : base(true)
        {
        }

        public CroppedBitmap(BitmapSource source, Int32Rect sourceRect)
            : base(true)
        {
            ArgumentNullException.ThrowIfNull(source);

            _bitmapInit.BeginInit();

            Source = source;
            SourceRect = sourceRect;

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

        private void ClonePrequel(CroppedBitmap otherCroppedBitmap)
        {
            BeginInit();
        }

        private void ClonePostscript(CroppedBitmap otherCroppedBitmap)
        {
            EndInit();
        }

        internal override unsafe void FinalizeCreation()
        {
            _bitmapInit.EnsureInitializedComplete();
            BitmapSourceSafeMILHandle wicClipper = null;

            Int32Rect rect = SourceRect;
            BitmapSource source = Source;

            if (rect.IsEmpty)
            {
                rect.Width = source.PixelWidth;
                rect.Height = source.PixelHeight;
            }

            WICRect wicRect = new WICRect(rect.X, rect.Y, rect.Width, rect.Height);

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                try
                {
                    void* wicFactory = (void*)factoryMaker.ImagingFactoryPtr;
                    void* pClipper = null;
                    HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapClipper(
                        wicFactory,
                        &pClipper));
                    wicClipper = new BitmapSourceSafeMILHandle((IntPtr)pClipper);

                    lock (_syncObject)
                    {
                        SafeMILHandle srcHandle = source.WicSourceHandle;
                        bool addRefClip = false;
                        bool addRefSrc = false;
                        wicClipper.DangerousAddRef(ref addRefClip);
                        srcHandle.DangerousAddRef(ref addRefSrc);
                        try
                        {
                            HRESULT.Check(IWICBitmapClipperVtbl.Initialize(
                                (void*)wicClipper.DangerousGetHandle(),
                                (void*)srcHandle.DangerousGetHandle(),
                                &wicRect));
                        }
                        finally
                        {
                            if (addRefSrc) srcHandle.DangerousRelease();
                            if (addRefClip) wicClipper.DangerousRelease();
                        }
                    }

                    WicSourceHandle = wicClipper;
                    _isSourceCached = source.IsSourceCached;
                    wicClipper = null;
                }
                catch
                {
                    _bitmapInit.Reset();
                    throw;
                }
                finally
                {
                    wicClipper?.Close();
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

            return true;
        }

        private void SourceRectPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _sourceRect = (Int32Rect)e.NewValue;
            }
        }

        private static object CoerceSource(DependencyObject d, object value)
        {
            CroppedBitmap bitmap = (CroppedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._source;
            }
            return value;
        }

        private static object CoerceSourceRect(DependencyObject d, object value)
        {
            CroppedBitmap bitmap = (CroppedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._sourceRect;
            }
            return value;
        }

        #region Data members

        private BitmapSource _source;
        private Int32Rect _sourceRect;

        #endregion Data members
    }

    #endregion CroppedBitmap
}
