// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows.Media;

namespace System.Windows.Appearance;

/// <summary>
/// Strongly-typed resource keys for Windows 11 Fluent Design tokens.
/// </summary>
public static class FluentThemeKeys
{
    public static ComponentResourceKey WindowBackgroundBrushKey { get; } = new(typeof(FluentThemeKeys), "WindowBackgroundBrush");
    public static ComponentResourceKey WindowForegroundBrushKey { get; } = new(typeof(FluentThemeKeys), "WindowForegroundBrush");
    public static ComponentResourceKey CardBackgroundBrushKey { get; } = new(typeof(FluentThemeKeys), "CardBackgroundBrush");
    public static ComponentResourceKey CardBorderBrushKey { get; } = new(typeof(FluentThemeKeys), "CardBorderBrush");
    public static ComponentResourceKey ControlBackgroundBrushKey { get; } = new(typeof(FluentThemeKeys), "ControlBackgroundBrush");
    public static ComponentResourceKey ControlForegroundBrushKey { get; } = new(typeof(FluentThemeKeys), "ControlForegroundBrush");
    public static ComponentResourceKey ControlBorderBrushKey { get; } = new(typeof(FluentThemeKeys), "ControlBorderBrush");
    public static ComponentResourceKey AccentBrushKey { get; } = new(typeof(FluentThemeKeys), "AccentBrush");
    public static ComponentResourceKey AccentForegroundBrushKey { get; } = new(typeof(FluentThemeKeys), "AccentForegroundBrush");
    public static ComponentResourceKey SurfaceStrokeBrushKey { get; } = new(typeof(FluentThemeKeys), "SurfaceStrokeBrush");
    public static ComponentResourceKey MicaTintBrushKey { get; } = new(typeof(FluentThemeKeys), "MicaTintBrush");
    public static ComponentResourceKey SubtleFillBrushKey { get; } = new(typeof(FluentThemeKeys), "SubtleFillBrush");
    public static ComponentResourceKey DividerStrokeBrushKey { get; } = new(typeof(FluentThemeKeys), "DividerStrokeBrush");
}

/// <summary>
/// High-performance built-in Windows 11 Fluent Theme dictionary provider.
/// Completely reflection-free and NativeAOT optimized with pre-frozen brushes.
/// </summary>
public static class FluentThemeResources
{
    private static readonly Lazy<ResourceDictionary> s_lightDictionary = new(() => BuildDictionary(isDark: false));
    private static readonly Lazy<ResourceDictionary> s_darkDictionary = new(() => BuildDictionary(isDark: true));

    public static ResourceDictionary GetDictionary(bool isDark) => isDark ? s_darkDictionary.Value : s_lightDictionary.Value;

    private static ResourceDictionary BuildDictionary(bool isDark)
    {
        var rd = new ResourceDictionary();

        if (isDark)
        {
            Register(rd, FluentThemeKeys.WindowBackgroundBrushKey, "WindowBackgroundBrush", Color.FromRgb(0x20, 0x20, 0x20));
            Register(rd, FluentThemeKeys.WindowForegroundBrushKey, "WindowForegroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
            Register(rd, FluentThemeKeys.CardBackgroundBrushKey, "CardBackgroundBrush", Color.FromRgb(0x2B, 0x2B, 0x2B));
            Register(rd, FluentThemeKeys.CardBorderBrushKey, "CardBorderBrush", Color.FromRgb(0x38, 0x38, 0x38));
            Register(rd, FluentThemeKeys.ControlBackgroundBrushKey, "ControlBackgroundBrush", Color.FromRgb(0x2D, 0x2D, 0x2D));
            Register(rd, FluentThemeKeys.ControlForegroundBrushKey, "ControlForegroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
            Register(rd, FluentThemeKeys.ControlBorderBrushKey, "ControlBorderBrush", Color.FromRgb(0x3C, 0x3C, 0x3C));
            Register(rd, FluentThemeKeys.AccentBrushKey, "AccentBrush", Color.FromRgb(0x4C, 0xC2, 0xFF)); // Windows 11 Dark Neon Blue
            Register(rd, FluentThemeKeys.AccentForegroundBrushKey, "AccentForegroundBrush", Color.FromRgb(0x00, 0x00, 0x00));
            Register(rd, FluentThemeKeys.SurfaceStrokeBrushKey, "SurfaceStrokeBrush", Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF));
            Register(rd, FluentThemeKeys.MicaTintBrushKey, "MicaTintBrush", Color.FromArgb(0x0A, 0xFF, 0xFF, 0xFF));
            Register(rd, FluentThemeKeys.SubtleFillBrushKey, "SubtleFillBrush", Color.FromRgb(0x32, 0x32, 0x32));
            Register(rd, FluentThemeKeys.DividerStrokeBrushKey, "DividerStrokeBrush", Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF));
        }
        else
        {
            Register(rd, FluentThemeKeys.WindowBackgroundBrushKey, "WindowBackgroundBrush", Color.FromRgb(0xF3, 0xF3, 0xF3));
            Register(rd, FluentThemeKeys.WindowForegroundBrushKey, "WindowForegroundBrush", Color.FromArgb(0xE4, 0x00, 0x00, 0x00));
            Register(rd, FluentThemeKeys.CardBackgroundBrushKey, "CardBackgroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
            Register(rd, FluentThemeKeys.CardBorderBrushKey, "CardBorderBrush", Color.FromRgb(0xE5, 0xE5, 0xE5));
            Register(rd, FluentThemeKeys.ControlBackgroundBrushKey, "ControlBackgroundBrush", Color.FromRgb(0xFB, 0xFB, 0xFB));
            Register(rd, FluentThemeKeys.ControlForegroundBrushKey, "ControlForegroundBrush", Color.FromArgb(0xE4, 0x00, 0x00, 0x00));
            Register(rd, FluentThemeKeys.ControlBorderBrushKey, "ControlBorderBrush", Color.FromRgb(0xD1, 0xD1, 0xD1));
            Register(rd, FluentThemeKeys.AccentBrushKey, "AccentBrush", Color.FromRgb(0x00, 0x67, 0xC0)); // Windows 11 Light Royal Blue
            Register(rd, FluentThemeKeys.AccentForegroundBrushKey, "AccentForegroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
            Register(rd, FluentThemeKeys.SurfaceStrokeBrushKey, "SurfaceStrokeBrush", Color.FromArgb(0x0F, 0x00, 0x00, 0x00));
            Register(rd, FluentThemeKeys.MicaTintBrushKey, "MicaTintBrush", Color.FromArgb(0x05, 0x00, 0x00, 0x00));
            Register(rd, FluentThemeKeys.SubtleFillBrushKey, "SubtleFillBrush", Color.FromRgb(0xF5, 0xF5, 0xF5));
            Register(rd, FluentThemeKeys.DividerStrokeBrushKey, "DividerStrokeBrush", Color.FromArgb(0x14, 0x00, 0x00, 0x00));
        }

        return rd;
    }

    private static void Register(ResourceDictionary rd, ComponentResourceKey compKey, string stringKey, Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();

        rd[compKey] = brush;
        rd[stringKey] = brush;
    }
}
