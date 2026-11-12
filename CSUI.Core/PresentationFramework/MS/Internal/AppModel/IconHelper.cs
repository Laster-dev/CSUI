// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Zero-DllImport NativeLibrary + delegate* icon helper implementation.

#nullable disable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CSUI.Core.Interop;
using MS.Win32;

namespace MS.Internal
{
    internal static unsafe class IconHelper
    {
        private const int IMAGE_ICON = 1;
        private const int LR_SHARED = 0x00008000;
        private const int SM_CXICON = 11;
        private const int SM_CYICON = 12;
        private const int SM_CXSMICON = 49;
        private const int SM_CYSMICON = 50;
        private const uint BI_RGB = 0;
        private const uint DIB_RGB_COLORS = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public int fIcon;
            public int xHotspot;
            public int yHotspot;
            public nint hbmMask;
            public nint hbmColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        private static readonly nint s_user32 = NativeModule.Load(ExternDll.User32);
        private static readonly nint s_gdi32 = NativeModule.Load(ExternDll.Gdi32);
        private static readonly nint s_kernel32 = NativeModule.Load(ExternDll.Kernel32);
        private static readonly nint s_shell32 = NativeLibrary.TryLoad(ExternDll.Shell32, out nint sh) ? sh : 0;

        private static readonly delegate* unmanaged[Stdcall]<int, int> s_getSystemMetrics =
            (delegate* unmanaged[Stdcall]<int, int>)NativeModule.GetExport(s_user32, "GetSystemMetrics");

        private static readonly delegate* unmanaged[Stdcall]<nint, nint, int, int, int, uint, nint> s_loadImageW =
            (delegate* unmanaged[Stdcall]<nint, nint, int, int, int, uint, nint>)NativeModule.GetExport(s_user32, "LoadImageW");

        private static readonly delegate* unmanaged[Stdcall]<nint, nint> s_copyIcon =
            (delegate* unmanaged[Stdcall]<nint, nint>)NativeModule.GetExport(s_user32, "CopyIcon");

        private static readonly delegate* unmanaged[Stdcall]<ICONINFO*, nint> s_createIconIndirect =
            (delegate* unmanaged[Stdcall]<ICONINFO*, nint>)NativeModule.GetExport(s_user32, "CreateIconIndirect");

        private static readonly delegate* unmanaged[Stdcall]<char*, int, nint*, nint*, uint, uint> s_extractIconExW =
            s_shell32 != 0 && NativeLibrary.TryGetExport(s_shell32, "ExtractIconExW", out nint ex)
                ? (delegate* unmanaged[Stdcall]<char*, int, nint*, nint*, uint, uint>)ex
                : null;

        private static readonly delegate* unmanaged[Stdcall]<nint, char*, uint, uint> s_getModuleFileNameW =
            (delegate* unmanaged[Stdcall]<nint, char*, uint, uint>)NativeModule.GetExport(s_kernel32, "GetModuleFileNameW");

        private static readonly delegate* unmanaged[Stdcall]<nint, BITMAPINFOHEADER*, uint, void**, nint, uint, nint> s_createDIBSection =
            (delegate* unmanaged[Stdcall]<nint, BITMAPINFOHEADER*, uint, void**, nint, uint, nint>)NativeModule.GetExport(s_gdi32, "CreateDIBSection");

        private static readonly delegate* unmanaged[Stdcall]<int, int, uint, uint, void*, nint> s_createBitmap =
            (delegate* unmanaged[Stdcall]<int, int, uint, uint, void*, nint>)NativeModule.GetExport(s_gdi32, "CreateBitmap");

        private static readonly delegate* unmanaged[Stdcall]<nint, int> s_deleteObject =
            (delegate* unmanaged[Stdcall]<nint, int>)NativeModule.GetExport(s_gdi32, "DeleteObject");

        internal static void GetDefaultIconHandles(out NativeMethods.IconHandle largeIconHandle, out NativeMethods.IconHandle smallIconHandle)
        {
            largeIconHandle = NativeMethods.IconHandle.GetInvalidIcon();
            smallIconHandle = NativeMethods.IconHandle.GetInvalidIcon();

            if (s_extractIconExW != null)
            {
                char* exePath = stackalloc char[520];
                uint len = s_getModuleFileNameW(0, exePath, 520);
                if (len > 0 && len < 520)
                {
                    nint hLarge = 0;
                    nint hSmall = 0;
                    if (s_extractIconExW(exePath, 0, &hLarge, &hSmall, 1) > 0)
                    {
                        if (hLarge != 0) largeIconHandle = NativeMethods.IconHandle.CreateFromHandle(hLarge);
                        if (hSmall != 0) smallIconHandle = NativeMethods.IconHandle.CreateFromHandle(hSmall);
                        if (!largeIconHandle.IsInvalid || !smallIconHandle.IsInvalid)
                        {
                            return;
                        }
                    }
                }
            }

            nint sharedLarge = s_loadImageW(0, (nint)32512 /* IDI_APPLICATION */, IMAGE_ICON, s_getSystemMetrics(SM_CXICON), s_getSystemMetrics(SM_CYICON), LR_SHARED);
            nint sharedSmall = s_loadImageW(0, (nint)32512 /* IDI_APPLICATION */, IMAGE_ICON, s_getSystemMetrics(SM_CXSMICON), s_getSystemMetrics(SM_CYSMICON), LR_SHARED);

            if (sharedLarge != 0)
            {
                largeIconHandle = NativeMethods.IconHandle.CreateFromHandle(s_copyIcon(sharedLarge));
            }
            if (sharedSmall != 0)
            {
                smallIconHandle = NativeMethods.IconHandle.CreateFromHandle(s_copyIcon(sharedSmall));
            }
        }

        internal static void GetIconHandlesFromImageSource(ImageSource image, out NativeMethods.IconHandle largeIconHandle, out NativeMethods.IconHandle smallIconHandle)
        {
            int largeW = Math.Max(1, s_getSystemMetrics(SM_CXICON));
            int largeH = Math.Max(1, s_getSystemMetrics(SM_CYICON));
            int smallW = Math.Max(1, s_getSystemMetrics(SM_CXSMICON));
            int smallH = Math.Max(1, s_getSystemMetrics(SM_CYSMICON));

            largeIconHandle = CreateIconHandleFromImageSource(image, largeW, largeH);
            smallIconHandle = CreateIconHandleFromImageSource(image, smallW, smallH);
        }

        private static NativeMethods.IconHandle CreateIconHandleFromImageSource(ImageSource image, int targetW, int targetH)
        {
            if (image is not BitmapSource bitmap || bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
            {
                return NativeMethods.IconHandle.GetInvalidIcon();
            }

            BitmapSource source = bitmap;
            if (source.Format != PixelFormats.Bgra32 && source.Format != PixelFormats.Pbgra32)
            {
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            }

            int srcW = source.PixelWidth;
            int srcH = source.PixelHeight;
            int srcStride = srcW * 4;
            byte[] srcPixels = new byte[srcH * srcStride];
            source.CopyPixels(srcPixels, srcStride, 0);

            BITMAPINFOHEADER bih = new BITMAPINFOHEADER
            {
                biSize = (uint)sizeof(BITMAPINFOHEADER),
                biWidth = targetW,
                biHeight = -targetH, // top-down DIB
                biPlanes = 1,
                biBitCount = 32,
                biCompression = BI_RGB,
            };

            void* pBits = null;
            nint hColor = s_createDIBSection(0, &bih, DIB_RGB_COLORS, &pBits, 0, 0);
            nint hMask = s_createBitmap(targetW, targetH, 1, 1, null);
            if (hColor == 0 || pBits == null || hMask == 0)
            {
                if (hColor != 0) s_deleteObject(hColor);
                if (hMask != 0) s_deleteObject(hMask);
                return NativeMethods.IconHandle.GetInvalidIcon();
            }

            try
            {
                byte* dst = (byte*)pBits;
                for (int y = 0; y < targetH; y++)
                {
                    int sy = (int)((long)y * srcH / targetH);
                    for (int x = 0; x < targetW; x++)
                    {
                        int sx = (int)((long)x * srcW / targetW);
                        int srcIdx = (sy * srcW + sx) * 4;
                        int dstIdx = (y * targetW + x) * 4;
                        dst[dstIdx + 0] = srcPixels[srcIdx + 0];
                        dst[dstIdx + 1] = srcPixels[srcIdx + 1];
                        dst[dstIdx + 2] = srcPixels[srcIdx + 2];
                        dst[dstIdx + 3] = srcPixels[srcIdx + 3];
                    }
                }

                ICONINFO info = new ICONINFO
                {
                    fIcon = 1,
                    hbmMask = hMask,
                    hbmColor = hColor,
                };
                nint hIcon = s_createIconIndirect(&info);
                return hIcon != 0
                    ? NativeMethods.IconHandle.CreateFromHandle(hIcon)
                    : NativeMethods.IconHandle.GetInvalidIcon();
            }
            finally
            {
                s_deleteObject(hColor);
                s_deleteObject(hMask);
            }
        }
    }
}
