// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using CSUI.Core.Interop.DirectX;

namespace System.Windows.Media.Effects
{
    public abstract partial class Effect
    {
        internal virtual unsafe void DrawD2DWithInput(void* d2dContext, void* inputImage)
        {
            if (d2dContext != null && inputImage != null)
            {
                ID2D1DeviceContextVtbl.DrawImage(
                    d2dContext,
                    inputImage,
                    null,
                    null,
                    DxMediaConst.D2D1_INTERPOLATION_MODE_LINEAR,
                    D2D1EffectGuids.D2D1_COMPOSITE_MODE_SOURCE_OVER);
            }
        }
    }

    public sealed partial class BlurEffect
    {
        internal override unsafe void DrawD2DWithInput(void* d2dContext, void* inputImage)
        {
            if (d2dContext == null || inputImage == null)
            {
                return;
            }

            double radius = Radius;
            if (radius <= 0.0 || double.IsNaN(radius))
            {
                base.DrawD2DWithInput(d2dContext, inputImage);
                return;
            }

            void* blurEffect = null;
            void* outputImage = null;
            try
            {
                if (ID2D1DeviceContextVtbl.CreateEffect(d2dContext, D2D1EffectGuids.CLSID_D2D1GaussianBlur, &blurEffect) < 0 || blurEffect == null)
                {
                    base.DrawD2DWithInput(d2dContext, inputImage);
                    return;
                }

                ID2D1EffectVtbl.SetInput(blurEffect, 0, inputImage);
                float sigma = (float)Math.Clamp(radius / 3.0, 0.0, 250.0);
                ID2D1EffectVtbl.SetFloat(blurEffect, D2D1EffectGuids.D2D1_GAUSSIANBLUR_PROP_STANDARD_DEVIATION, sigma);
                ID2D1EffectVtbl.SetEnum(blurEffect, D2D1EffectGuids.D2D1_GAUSSIANBLUR_PROP_OPTIMIZATION, RenderingBias == RenderingBias.Quality ? 2u : 0u);
                ID2D1EffectVtbl.GetOutput(blurEffect, &outputImage);
                if (outputImage != null)
                {
                    ID2D1DeviceContextVtbl.DrawImage(
                        d2dContext,
                        outputImage,
                        null,
                        null,
                        DxMediaConst.D2D1_INTERPOLATION_MODE_LINEAR,
                        D2D1EffectGuids.D2D1_COMPOSITE_MODE_SOURCE_OVER);
                }
            }
            finally
            {
                if (outputImage != null)
                {
                    ComHelpers.Release(outputImage);
                }

                if (blurEffect != null)
                {
                    ComHelpers.Release(blurEffect);
                }
            }
        }
    }

    public sealed partial class DropShadowEffect
    {
        internal override unsafe void DrawD2DWithInput(void* d2dContext, void* inputImage)
        {
            if (d2dContext == null || inputImage == null)
            {
                return;
            }

            double opacity = Math.Clamp(Opacity, 0.0, 1.0);
            if (opacity <= 0.0 || Color.A == 0)
            {
                base.DrawD2DWithInput(d2dContext, inputImage);
                return;
            }

            void* shadowEffect = null;
            void* outputImage = null;
            try
            {
                if (ID2D1DeviceContextVtbl.CreateEffect(d2dContext, D2D1EffectGuids.CLSID_D2D1Shadow, &shadowEffect) >= 0 && shadowEffect != null)
                {
                    ID2D1EffectVtbl.SetInput(shadowEffect, 0, inputImage);
                    float sigma = (float)Math.Clamp(BlurRadius / 3.0, 0.0, 250.0);
                    ID2D1EffectVtbl.SetFloat(shadowEffect, D2D1EffectGuids.D2D1_SHADOW_PROP_BLUR_STANDARD_DEVIATION, sigma);

                    Color c = Color;
                    var colorVec = new D2D1_COLOR_F(c.ScR, c.ScG, c.ScB, (float)(c.ScA * opacity));
                    ID2D1EffectVtbl.SetVector4(shadowEffect, D2D1EffectGuids.D2D1_SHADOW_PROP_COLOR, colorVec);
                    ID2D1EffectVtbl.SetEnum(shadowEffect, D2D1EffectGuids.D2D1_SHADOW_PROP_OPTIMIZATION, RenderingBias == RenderingBias.Quality ? 2u : 0u);
                    ID2D1EffectVtbl.GetOutput(shadowEffect, &outputImage);

                    if (outputImage != null)
                    {
                        double angleRad = Direction * (Math.PI / 180.0);
                        double depth = ShadowDepth;
                        var offset = new D2D1_POINT_2F((float)(Math.Cos(angleRad) * depth), (float)(-Math.Sin(angleRad) * depth));
                        ID2D1DeviceContextVtbl.DrawImage(
                            d2dContext,
                            outputImage,
                            &offset,
                            null,
                            DxMediaConst.D2D1_INTERPOLATION_MODE_LINEAR,
                            D2D1EffectGuids.D2D1_COMPOSITE_MODE_SOURCE_OVER);
                    }
                }
            }
            finally
            {
                if (outputImage != null)
                {
                    ComHelpers.Release(outputImage);
                }

                if (shadowEffect != null)
                {
                    ComHelpers.Release(shadowEffect);
                }
            }

            base.DrawD2DWithInput(d2dContext, inputImage);
        }
    }
}
