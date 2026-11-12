// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;
using MS.Internal;

namespace System.Windows.Media.Imaging
{
    internal unsafe class BitmapSourceSafeMILHandle : SafeMILHandle
    {
        internal BitmapSourceSafeMILHandle() : base()
        {
        }

        internal BitmapSourceSafeMILHandle(IntPtr handle) : base()
        {
            SetHandle(handle);
        }

        internal BitmapSourceSafeMILHandle(IntPtr handle, SafeMILHandle copyMemoryPressureFrom)
            : this(handle)
        {
            CopyMemoryPressure(copyMemoryPressureFrom);
        }

        internal void CalculateSize()
        {
            UpdateEstimatedSize(ComputeEstimatedSize(handle));
        }

        private static long ComputeEstimatedSize(IntPtr bitmapObject)
        {
            long estimatedSize = 0;

            if (bitmapObject != IntPtr.Zero)
            {
                void* wicBitmap = null;
                int hr = ComPtr<byte>.QueryInterface((void*)bitmapObject, WicGuids.IID_IWICBitmapSource, &wicBitmap);
                if (hr == HRESULT.S_OK && wicBitmap != null)
                {
                    try
                    {
                        uint pixelWidth = 0;
                        uint pixelHeight = 0;
                        hr = IWICBitmapSourceVtbl.GetSize(wicBitmap, out pixelWidth, out pixelHeight);
                        if (hr == HRESULT.S_OK)
                        {
                            Guid pixelFormatGuid;
                            hr = IWICBitmapSourceVtbl.GetPixelFormat(wicBitmap, out pixelFormatGuid);
                            if (hr == HRESULT.S_OK)
                            {
                                PixelFormat pf = new PixelFormat(pixelFormatGuid);
                                long bitsPerPixel = pf.InternalBitsPerPixel;
                                estimatedSize = ((long)pixelWidth * bitsPerPixel * (long)pixelHeight) / 8;
                            }
                        }
                    }
                    finally
                    {
                        ComHelpers.Release(wicBitmap);
                    }
                }
            }

            return estimatedSize;
        }
    }
}
