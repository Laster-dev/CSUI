// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Diagnostics;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using MS.Internal.PresentationCore;

namespace System.Windows.Media.Imaging
{
    #region TransformedBitmap

    /// <summary>
    /// TransformedBitmap provides orthogonal transform (scale, flip, rotate) functionality for a BitmapSource.
    /// </summary>
    public sealed partial class TransformedBitmap : BitmapSource, ISupportInitialize
    {
        public TransformedBitmap()
            : base(true)
        {
        }

        public TransformedBitmap(BitmapSource source, Transform newTransform)
            : base(true)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (newTransform == null)
            {
                throw new InvalidOperationException(SR.Format(SR.Image_NoArgument, "Transform"));
            }

            if (!CheckTransform(newTransform))
            {
                throw new InvalidOperationException(SR.Image_OnlyOrthogonal);
            }

            _bitmapInit.BeginInit();

            Source = source;
            Transform = newTransform;

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

        private void ClonePrequel(TransformedBitmap otherTransformedBitmap)
        {
            BeginInit();
        }

        private void ClonePostscript(TransformedBitmap otherTransformedBitmap)
        {
            EndInit();
        }

        internal bool CheckTransform(Transform newTransform)
        {
            Matrix m = newTransform.Value;
            return (DoubleUtil.IsZero(m.M11) && DoubleUtil.IsZero(m.M22)) ||
                   (DoubleUtil.IsZero(m.M12) && DoubleUtil.IsZero(m.M21));
        }

        internal void GetParamsFromTransform(
            Transform newTransform,
            out double scaleX,
            out double scaleY,
            out WICBitmapTransformOptions options)
        {
            Matrix m = newTransform.Value;

            if (DoubleUtil.IsZero(m.M12) && DoubleUtil.IsZero(m.M21))
            {
                scaleX = Math.Abs(m.M11);
                scaleY = Math.Abs(m.M22);

                options = WICBitmapTransformOptions.WICBitmapTransformRotate0;

                if (m.M11 < 0)
                {
                    options |= WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal;
                }

                if (m.M22 < 0)
                {
                    options |= WICBitmapTransformOptions.WICBitmapTransformFlipVertical;
                }
            }
            else
            {
                Debug.Assert(DoubleUtil.IsZero(m.M11) && DoubleUtil.IsZero(m.M22));

                scaleX = Math.Abs(m.M12);
                scaleY = Math.Abs(m.M21);

                options = WICBitmapTransformOptions.WICBitmapTransformRotate90;

                if (m.M12 < 0)
                {
                    options |= WICBitmapTransformOptions.WICBitmapTransformFlipHorizontal;
                }

                if (m.M21 >= 0)
                {
                    options |= WICBitmapTransformOptions.WICBitmapTransformFlipVertical;
                }
            }
        }

        internal override unsafe void FinalizeCreation()
        {
            _bitmapInit.EnsureInitializedComplete();
            BitmapSourceSafeMILHandle scalerHandle = null;
            BitmapSourceSafeMILHandle cachedHandle = null;
            BitmapSourceSafeMILHandle rotatorHandle = null;

            GetParamsFromTransform(Transform, out double scaleX, out double scaleY, out WICBitmapTransformOptions options);

            using (FactoryMaker factoryMaker = new FactoryMaker())
            {
                try
                {
                    void* wicFactory = (void*)factoryMaker.ImagingFactoryPtr;
                    BitmapSourceSafeMILHandle wicTransformer = _source.WicSourceHandle;

                    if (!DoubleUtil.IsOne(scaleX) || !DoubleUtil.IsOne(scaleY))
                    {
                        uint width = Math.Max(1, (uint)(scaleX * _source.PixelWidth + 0.5));
                        uint height = Math.Max(1, (uint)(scaleY * _source.PixelHeight + 0.5));

                        void* pScaler = null;
                        HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapScaler(
                            wicFactory,
                            &pScaler));
                        scalerHandle = new BitmapSourceSafeMILHandle((IntPtr)pScaler);

                        lock (_syncObject)
                        {
                            SafeMILHandle srcHandle = _source.WicSourceHandle;
                            bool addRefScaler = false;
                            bool addRefSrc = false;
                            scalerHandle.DangerousAddRef(ref addRefScaler);
                            srcHandle.DangerousAddRef(ref addRefSrc);
                            try
                            {
                                HRESULT.Check(IWICBitmapScalerVtbl.Initialize(
                                    (void*)scalerHandle.DangerousGetHandle(),
                                    (void*)srcHandle.DangerousGetHandle(),
                                    width,
                                    height,
                                    (uint)WICInterpolationMode.Fant));
                            }
                            finally
                            {
                                if (addRefSrc) srcHandle.DangerousRelease();
                                if (addRefScaler) scalerHandle.DangerousRelease();
                            }
                        }

                        wicTransformer = scalerHandle;
                    }

                    if (options != WICBitmapTransformOptions.WICBitmapTransformRotate0)
                    {
                        cachedHandle = CreateCachedBitmap(
                            null,
                            wicTransformer,
                            BitmapCreateOptions.PreservePixelFormat,
                            BitmapCacheOption.Default,
                            _source.Palette);

                        void* pRotator = null;
                        HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapFlipRotator(
                            wicFactory,
                            &pRotator));
                        rotatorHandle = new BitmapSourceSafeMILHandle((IntPtr)pRotator);

                        lock (_syncObject)
                        {
                            bool addRefRot = false;
                            bool addRefCached = false;
                            rotatorHandle.DangerousAddRef(ref addRefRot);
                            cachedHandle.DangerousAddRef(ref addRefCached);
                            try
                            {
                                HRESULT.Check(IWICBitmapFlipRotatorVtbl.Initialize(
                                    (void*)rotatorHandle.DangerousGetHandle(),
                                    (void*)cachedHandle.DangerousGetHandle(),
                                    (uint)options));
                            }
                            finally
                            {
                                if (addRefCached) cachedHandle.DangerousRelease();
                                if (addRefRot) rotatorHandle.DangerousRelease();
                            }
                        }

                        wicTransformer = rotatorHandle;
                    }

                    if (options == WICBitmapTransformOptions.WICBitmapTransformRotate0 &&
                        DoubleUtil.IsOne(scaleX) && DoubleUtil.IsOne(scaleY))
                    {
                        void* pRotator = null;
                        HRESULT.Check(IWICImagingFactoryExVtbl.CreateBitmapFlipRotator(
                            wicFactory,
                            &pRotator));
                        rotatorHandle = new BitmapSourceSafeMILHandle((IntPtr)pRotator);

                        lock (_syncObject)
                        {
                            SafeMILHandle srcHandle = _source.WicSourceHandle;
                            bool addRefRot = false;
                            bool addRefSrc = false;
                            rotatorHandle.DangerousAddRef(ref addRefRot);
                            srcHandle.DangerousAddRef(ref addRefSrc);
                            try
                            {
                                HRESULT.Check(IWICBitmapFlipRotatorVtbl.Initialize(
                                    (void*)rotatorHandle.DangerousGetHandle(),
                                    (void*)srcHandle.DangerousGetHandle(),
                                    (uint)WICBitmapTransformOptions.WICBitmapTransformRotate0));
                            }
                            finally
                            {
                                if (addRefSrc) srcHandle.DangerousRelease();
                                if (addRefRot) rotatorHandle.DangerousRelease();
                            }
                        }

                        wicTransformer = rotatorHandle;
                    }

                    WicSourceHandle = wicTransformer;
                    _isSourceCached = _source.IsSourceCached;
                }
                catch
                {
                    _bitmapInit.Reset();
                    throw;
                }
                finally
                {
                    rotatorHandle?.Close();
                    cachedHandle?.Close();
                    scalerHandle?.Close();
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

            Transform transform = Transform;
            if (transform == null)
            {
                if (throwIfInvalid)
                {
                    throw new InvalidOperationException(SR.Format(SR.Image_NoArgument, "Transform"));
                }
                return false;
            }

            if (!CheckTransform(transform))
            {
                if (throwIfInvalid)
                {
                    throw new InvalidOperationException(SR.Image_OnlyOrthogonal);
                }
                return false;
            }

            return true;
        }

        private void TransformPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            if (!e.IsASubPropertyChange)
            {
                _transform = e.NewValue as Transform;
            }
        }

        private static object CoerceSource(DependencyObject d, object value)
        {
            TransformedBitmap bitmap = (TransformedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._source;
            }
            return value;
        }

        private static object CoerceTransform(DependencyObject d, object value)
        {
            TransformedBitmap bitmap = (TransformedBitmap)d;
            if (!bitmap._bitmapInit.IsInInit)
            {
                return bitmap._transform;
            }
            return value;
        }

        #region Data Members

        private BitmapSource _source;
        private Transform _transform;

        #endregion Data Members
    }

    #endregion TransformedBitmap
}
