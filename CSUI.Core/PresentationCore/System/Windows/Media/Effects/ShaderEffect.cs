// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Media.Media3D;

namespace System.Windows.Media.Effects
{
    public abstract class ShaderEffect : Effect
    {
        protected static readonly DependencyProperty PixelShaderProperty =
            DependencyProperty.Register(
                nameof(PixelShader),
                typeof(PixelShader),
                typeof(ShaderEffect),
                new PropertyMetadata(null));

        private readonly Dictionary<int, Vector4> _floatConstants = new();
        private readonly Dictionary<int, Brush> _samplerInputs = new();

        protected PixelShader PixelShader
        {
            get => (PixelShader)GetValue(PixelShaderProperty);
            set => SetValue(PixelShaderProperty, value);
        }

        protected double PaddingTop { get; set; }
        protected double PaddingBottom { get; set; }
        protected double PaddingLeft { get; set; }
        protected double PaddingRight { get; set; }
        protected int DdxUvDdyUvRegisterIndex { get; set; } = -1;

        protected static PropertyChangedCallback PixelShaderConstantCallback(int registerIndex)
        {
            return (d, e) =>
            {
                if (d is ShaderEffect se)
                {
                    se.SetFloatConstant(registerIndex, e.NewValue);
                }
            };
        }

        protected static DependencyProperty RegisterPixelShaderSamplerProperty(
            string dpName,
            Type ownerType,
            int samplerRegisterIndex,
            SamplingMode samplingMode = SamplingMode.Auto)
        {
            _ = samplingMode;
            return DependencyProperty.Register(
                dpName,
                typeof(Brush),
                ownerType,
                new PropertyMetadata(ImplicitInput, (d, e) =>
                {
                    if (d is ShaderEffect se)
                    {
                        se._samplerInputs[samplerRegisterIndex] = e.NewValue as Brush;
                    }
                }));
        }

        protected void UpdateShaderValue(DependencyProperty dp)
        {
            ArgumentNullException.ThrowIfNull(dp);
            object val = GetValue(dp);
            dp.DefaultMetadata?.PropertyChangedCallback?.Invoke(
                this,
                new DependencyPropertyChangedEventArgs(dp, val, val));
        }

        internal bool TryGetConstant(int registerIndex, out Vector4 value)
            => _floatConstants.TryGetValue(registerIndex, out value);

        internal bool TryGetSampler(int registerIndex, out Brush brush)
            => _samplerInputs.TryGetValue(registerIndex, out brush);

        private void SetFloatConstant(int registerIndex, object value)
        {
            _floatConstants[registerIndex] = value switch
            {
                double d => new Vector4((float)d, (float)d, (float)d, (float)d),
                float f => new Vector4(f, f, f, f),
                int i => new Vector4(i, i, i, i),
                Point p => new Vector4((float)p.X, (float)p.Y, 0f, 0f),
                Size s => new Vector4((float)s.Width, (float)s.Height, 0f, 0f),
                Vector v => new Vector4((float)v.X, (float)v.Y, 0f, 0f),
                Color c => new Vector4(c.ScR, c.ScG, c.ScB, c.ScA),
                Point3D p3 => new Vector4((float)p3.X, (float)p3.Y, (float)p3.Z, 1f),
                Vector3D v3 => new Vector4((float)v3.X, (float)v3.Y, (float)v3.Z, 0f),
                Point4D p4 => new Vector4((float)p4.X, (float)p4.Y, (float)p4.Z, (float)p4.W),
                _ => Vector4.Zero
            };
        }

        internal override Rect GetRenderBounds(Rect contentBounds)
        {
            if (contentBounds.IsEmpty)
            {
                return contentBounds;
            }
            return new Rect(
                contentBounds.X - PaddingLeft,
                contentBounds.Y - PaddingTop,
                contentBounds.Width + PaddingLeft + PaddingRight,
                contentBounds.Height + PaddingTop + PaddingBottom);
        }

        internal override unsafe void DrawD2DWithInput(void* d2dContext, void* inputImage)
        {
            base.DrawD2DWithInput(d2dContext, inputImage);
        }
    }
}
