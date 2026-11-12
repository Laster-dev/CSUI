// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows.Media.Animation;
using CSUI.Core.Interop.DirectX;

namespace System.Windows.Media.Effects
{
    /// <summary>
    /// Modern dual-source elevation shadow effect (Ambient + Key Light) accelerated via Direct2D 1.1 hardware effect pipelines.
    /// Provides ultra-smooth, premium elevation depth matching Windows 11 Fluent Design and Material Design 3.
    /// </summary>
    public sealed class ModernElevationEffect : Effect
    {
        public static readonly DependencyProperty ElevationProperty =
            DependencyProperty.Register(
                nameof(Elevation),
                typeof(double),
                typeof(ModernElevationEffect),
                new PropertyMetadata(4.0, OnElevationChanged));

        public static readonly DependencyProperty BlurRadiusProperty =
            DependencyProperty.Register(
                nameof(BlurRadius),
                typeof(double),
                typeof(ModernElevationEffect),
                new PropertyMetadata(16.0));

        public static readonly DependencyProperty ShadowDepthProperty =
            DependencyProperty.Register(
                nameof(ShadowDepth),
                typeof(double),
                typeof(ModernElevationEffect),
                new PropertyMetadata(4.0));

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(ModernElevationEffect),
                new PropertyMetadata(Colors.Black));

        public static readonly DependencyProperty OpacityProperty =
            DependencyProperty.Register(
                nameof(Opacity),
                typeof(double),
                typeof(ModernElevationEffect),
                new PropertyMetadata(0.35));

        public double Elevation
        {
            get => (double)GetValue(ElevationProperty);
            set => SetValue(ElevationProperty, value);
        }

        public double BlurRadius
        {
            get => (double)GetValue(BlurRadiusProperty);
            set => SetValue(BlurRadiusProperty, value);
        }

        public double ShadowDepth
        {
            get => (double)GetValue(ShadowDepthProperty);
            set => SetValue(ShadowDepthProperty, value);
        }

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public double Opacity
        {
            get => (double)GetValue(OpacityProperty);
            set => SetValue(OpacityProperty, value);
        }

        public ModernElevationEffect()
        {
            UpdateFromElevation(4.0);
        }

        public ModernElevationEffect(double elevation)
        {
            Elevation = elevation;
            UpdateFromElevation(elevation);
        }

        private static void OnElevationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ModernElevationEffect effect && e.NewValue is double elev)
            {
                effect.UpdateFromElevation(elev);
            }
        }

        private void UpdateFromElevation(double elevation)
        {
            double elev = Math.Clamp(elevation, 0.0, 32.0);
            BlurRadius = Math.Max(2.0, elev * 3.5);
            ShadowDepth = Math.Max(1.0, elev * 1.25);
            Opacity = Math.Clamp(0.18 + elev * 0.015, 0.1, 0.6);
        }

        protected override Freezable CreateInstanceCore() => new ModernElevationEffect();

        public Rect ComputeRenderBounds(Rect contentBounds) => GetRenderBounds(contentBounds);

        internal override Rect GetRenderBounds(Rect contentBounds)
        {
            double r = BlurRadius * 1.5;
            double d = ShadowDepth;
            return new Rect(
                contentBounds.X - r,
                contentBounds.Y - r,
                contentBounds.Width + r * 2.0,
                contentBounds.Height + r * 2.0 + d);
        }

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

            void* ambientShadow = null;
            void* ambientOutput = null;
            void* keyShadow = null;
            void* keyOutput = null;

            Color c = Color;
            double radius = Math.Max(1.0, BlurRadius);
            double depth = Math.Max(0.0, ShadowDepth);

            try
            {
                // 1. Ambient Shadow: Wide, soft, omnidirectional ambient occlusion
                if (ID2D1DeviceContextVtbl.CreateEffect(d2dContext, D2D1EffectGuids.CLSID_D2D1Shadow, &ambientShadow) >= 0 && ambientShadow != null)
                {
                    ID2D1EffectVtbl.SetInput(ambientShadow, 0, inputImage);
                    float ambientSigma = (float)Math.Clamp(radius * 0.7 / 3.0, 0.0, 250.0);
                    ID2D1EffectVtbl.SetFloat(ambientShadow, D2D1EffectGuids.D2D1_SHADOW_PROP_BLUR_STANDARD_DEVIATION, ambientSigma);

                    var ambientColor = new D2D1_COLOR_F(c.ScR, c.ScG, c.ScB, (float)(c.ScA * opacity * 0.45));
                    ID2D1EffectVtbl.SetVector4(ambientShadow, D2D1EffectGuids.D2D1_SHADOW_PROP_COLOR, ambientColor);
                    ID2D1EffectVtbl.SetEnum(ambientShadow, D2D1EffectGuids.D2D1_SHADOW_PROP_OPTIMIZATION, 2u); // Quality
                    ID2D1EffectVtbl.GetOutput(ambientShadow, &ambientOutput);

                    if (ambientOutput != null)
                    {
                        var ambientOffset = new D2D1_POINT_2F(0.0f, (float)(depth * 0.25));
                        ID2D1DeviceContextVtbl.DrawImage(
                            d2dContext,
                            ambientOutput,
                            &ambientOffset,
                            null,
                            DxMediaConst.D2D1_INTERPOLATION_MODE_LINEAR,
                            D2D1EffectGuids.D2D1_COMPOSITE_MODE_SOURCE_OVER);
                    }
                }

                // 2. Key Shadow: Directional cast shadow with defined vertical drop
                if (ID2D1DeviceContextVtbl.CreateEffect(d2dContext, D2D1EffectGuids.CLSID_D2D1Shadow, &keyShadow) >= 0 && keyShadow != null)
                {
                    ID2D1EffectVtbl.SetInput(keyShadow, 0, inputImage);
                    float keySigma = (float)Math.Clamp(radius * 0.45 / 3.0, 0.0, 250.0);
                    ID2D1EffectVtbl.SetFloat(keyShadow, D2D1EffectGuids.D2D1_SHADOW_PROP_BLUR_STANDARD_DEVIATION, keySigma);

                    var keyColor = new D2D1_COLOR_F(c.ScR, c.ScG, c.ScB, (float)(c.ScA * opacity * 0.55));
                    ID2D1EffectVtbl.SetVector4(keyShadow, D2D1EffectGuids.D2D1_SHADOW_PROP_COLOR, keyColor);
                    ID2D1EffectVtbl.SetEnum(keyShadow, D2D1EffectGuids.D2D1_SHADOW_PROP_OPTIMIZATION, 2u); // Quality
                    ID2D1EffectVtbl.GetOutput(keyShadow, &keyOutput);

                    if (keyOutput != null)
                    {
                        var keyOffset = new D2D1_POINT_2F(0.0f, (float)depth);
                        ID2D1DeviceContextVtbl.DrawImage(
                            d2dContext,
                            keyOutput,
                            &keyOffset,
                            null,
                            DxMediaConst.D2D1_INTERPOLATION_MODE_LINEAR,
                            D2D1EffectGuids.D2D1_COMPOSITE_MODE_SOURCE_OVER);
                    }
                }
            }
            finally
            {
                if (ambientOutput != null) ComHelpers.Release(ambientOutput);
                if (ambientShadow != null) ComHelpers.Release(ambientShadow);
                if (keyOutput != null) ComHelpers.Release(keyOutput);
                if (keyShadow != null) ComHelpers.Release(keyShadow);
            }

            // 3. Top layer: Content itself
            base.DrawD2DWithInput(d2dContext, inputImage);
        }
    }
}
