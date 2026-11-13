// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;

namespace System.Windows.Media.Effects
{
    public enum ShaderRenderMode
    {
        Auto = 0,
        SoftwareOnly = 1,
        HardwareOnly = 2
    }

    public enum SamplingMode
    {
        NearestNeighbor = 0,
        Bilinear = 1,
        Auto = 2
    }

    public sealed class PixelShader : Animatable
    {
        public static readonly DependencyProperty UriSourceProperty =
            DependencyProperty.Register(
                nameof(UriSource),
                typeof(Uri),
                typeof(PixelShader),
                new PropertyMetadata(null, OnUriSourceChanged));

        public static readonly DependencyProperty ShaderRenderModeProperty =
            DependencyProperty.Register(
                nameof(ShaderRenderMode),
                typeof(ShaderRenderMode),
                typeof(PixelShader),
                new PropertyMetadata(ShaderRenderMode.Auto));

        public static event EventHandler InvalidPixelShaderEncountered;

        private byte[] _bytecode;

        public Uri UriSource
        {
            get => (Uri)GetValue(UriSourceProperty);
            set => SetValue(UriSourceProperty, value);
        }

        public ShaderRenderMode ShaderRenderMode
        {
            get => (ShaderRenderMode)GetValue(ShaderRenderModeProperty);
            set => SetValue(ShaderRenderModeProperty, value);
        }

        internal byte[] Bytecode => _bytecode;

        public void SetStreamSource(Stream source)
        {
            ArgumentNullException.ThrowIfNull(source);
            using var ms = new MemoryStream();
            source.CopyTo(ms);
            _bytecode = ms.ToArray();
        }

        private static void OnUriSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ps = (PixelShader)d;
            if (e.NewValue is Uri uri)
            {
                ps.LoadFromUri(uri);
            }
            else
            {
                ps._bytecode = null;
            }
        }

        private void LoadFromUri(Uri uri)
        {
            try
            {
                if (uri.IsFile && File.Exists(uri.LocalPath))
                {
                    _bytecode = File.ReadAllBytes(uri.LocalPath);
                    return;
                }

                string rel = uri.OriginalString.TrimStart('/');
                string diskPath = Path.Combine(AppContext.BaseDirectory, rel.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(diskPath))
                {
                    _bytecode = File.ReadAllBytes(diskPath);
                    return;
                }
            }
            catch
            {
                InvalidPixelShaderEncountered?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override Freezable CreateInstanceCore() => new PixelShader();
    }
}
