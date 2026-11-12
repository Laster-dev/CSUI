// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Threading;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Defines the color table for indexed pixel formats using WIC palettes.
    /// </summary>
    public sealed unsafe class BitmapPalette : DispatcherObject
    {
        private BitmapPalette()
        {
        }

        public BitmapPalette(IList<Color> colors)
        {
            ArgumentNullException.ThrowIfNull(colors);

            int count = colors.Count;
            if (count < 1 || count > 256)
            {
                throw new InvalidOperationException(SR.Image_PaletteZeroColors);
            }

            Color[] clonedColors = new Color[count];
            for (int i = 0; i < count; i++)
            {
                clonedColors[i] = colors[i];
            }

            _colors = new ReadOnlyCollection<Color>(clonedColors);
            _palette = CreateInternalPalette();
            UpdateUnmanaged();
        }

        public BitmapPalette(BitmapSource bitmapSource, int maxColorCount)
        {
            ArgumentNullException.ThrowIfNull(bitmapSource);

            SafeMILHandle wicBitmap = bitmapSource.WicSourceHandle;
            if (wicBitmap == null || wicBitmap.IsInvalid)
            {
                throw new ArgumentException(null, nameof(bitmapSource));
            }

            _palette = CreateInternalPalette();

            lock (bitmapSource.SyncObject)
            {
                HRESULT.Check(IWICPaletteVtbl.InitializeFromBitmap(
                    (void*)_palette.DangerousGetHandle(),
                    (void*)wicBitmap.DangerousGetHandle(),
                    (uint)maxColorCount,
                    1));
            }

            UpdateManaged();
        }

        internal BitmapPalette(WICPaletteType paletteType, bool addTransparentColor)
        {
            switch (paletteType)
            {
                case WICPaletteType.WICPaletteTypeFixedBW:
                case WICPaletteType.WICPaletteTypeFixedHalftone8:
                case WICPaletteType.WICPaletteTypeFixedHalftone27:
                case WICPaletteType.WICPaletteTypeFixedHalftone64:
                case WICPaletteType.WICPaletteTypeFixedHalftone125:
                case WICPaletteType.WICPaletteTypeFixedHalftone216:
                case WICPaletteType.WICPaletteTypeFixedHalftone252:
                case WICPaletteType.WICPaletteTypeFixedHalftone256:
                case WICPaletteType.WICPaletteTypeFixedGray4:
                case WICPaletteType.WICPaletteTypeFixedGray16:
                case WICPaletteType.WICPaletteTypeFixedGray256:
                    break;
                default:
                    throw new ArgumentException(SR.Format(SR.Image_PaletteFixedType, paletteType));
            }

            _palette = CreateInternalPalette();
            HRESULT.Check(IWICPaletteVtbl.InitializePredefined(
                (void*)_palette.DangerousGetHandle(),
                (uint)paletteType,
                addTransparentColor ? 1 : 0));

            UpdateManaged();
        }

        internal BitmapPalette(SafeMILHandle unmanagedPalette)
        {
            _palette = unmanagedPalette;
            UpdateManaged();
        }

        internal static BitmapPalette CreateFromBitmapSource(BitmapSource source)
        {
            Debug.Assert(source != null);

            SafeMILHandle bitmapSource = source.WicSourceHandle;
            Debug.Assert(bitmapSource != null && !bitmapSource.IsInvalid);

            SafeMILHandle unmanagedPalette = CreateInternalPalette();

            lock (source.SyncObject)
            {
                int hr = IWICBitmapSourceExVtbl.CopyPalette(
                    (void*)bitmapSource.DangerousGetHandle(),
                    (void*)unmanagedPalette.DangerousGetHandle());

                if (hr != HRESULT.S_OK)
                {
                    return null;
                }
            }

            void* pPalette = (void*)unmanagedPalette.DangerousGetHandle();
            HRESULT.Check(IWICPaletteVtbl.GetType(pPalette, out WICPaletteType paletteType));
            HRESULT.Check(IWICPaletteVtbl.HasAlpha(pPalette, out bool hasAlpha));

            if (paletteType == WICPaletteType.WICPaletteTypeCustom ||
                paletteType == WICPaletteType.WICPaletteTypeOptimal)
            {
                return new BitmapPalette(unmanagedPalette);
            }

            BitmapPalette palette = BitmapPalettes.FromMILPaletteType(paletteType, hasAlpha);
            Debug.Assert(palette != null);
            return palette;
        }

        public IList<Color> Colors => _colors;

        internal SafeMILHandle InternalPalette
        {
            get
            {
                if (_palette == null || _palette.IsInvalid)
                {
                    _palette = CreateInternalPalette();
                }

                return _palette;
            }
        }

        internal static bool DoesPaletteHaveAlpha(BitmapPalette palette)
        {
            if (palette != null)
            {
                foreach (Color color in palette.Colors)
                {
                    if (color.A != 255)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static SafeMILHandle CreateInternalPalette()
        {
            using var factoryMaker = new FactoryMaker();
            void* palettePtr = null;
            HRESULT.Check(IWICImagingFactoryExVtbl.CreatePalette((void*)factoryMaker.ImagingFactoryPtr, &palettePtr));
            return new SafeMILHandle((IntPtr)palettePtr);
        }

        private void UpdateUnmanaged()
        {
            int count = Math.Min(256, _colors.Count);
            uint* colors = stackalloc uint[count];
            for (int i = 0; i < count; i++)
            {
                Color c = _colors[i];
                colors[i] = ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
            }

            HRESULT.Check(IWICPaletteVtbl.InitializeCustom(
                (void*)_palette.DangerousGetHandle(),
                colors,
                (uint)count));
        }

        private void UpdateManaged()
        {
            void* pPalette = (void*)_palette.DangerousGetHandle();
            HRESULT.Check(IWICPaletteVtbl.GetColorCount(pPalette, out uint colorCount));

            List<Color> colors = new List<Color>();
            if (colorCount >= 1 && colorCount <= 256)
            {
                uint* pColors = stackalloc uint[(int)colorCount];
                HRESULT.Check(IWICPaletteVtbl.GetColors(pPalette, colorCount, pColors, out uint actualColors));
                for (int i = 0; i < actualColors; i++)
                {
                    uint argb = pColors[i];
                    byte a = (byte)((argb & 0xff000000) >> 24);
                    byte r = (byte)((argb & 0x00ff0000) >> 16);
                    byte g = (byte)((argb & 0x0000ff00) >> 8);
                    byte b = (byte)(argb & 0x000000ff);
                    colors.Add(Color.FromArgb(a, r, g, b));
                }
            }

            _colors = new ReadOnlyCollection<Color>(colors);
        }

        private SafeMILHandle _palette;
        private IList<Color> _colors = new ReadOnlyCollection<Color>(Array.Empty<Color>());
    }
}
