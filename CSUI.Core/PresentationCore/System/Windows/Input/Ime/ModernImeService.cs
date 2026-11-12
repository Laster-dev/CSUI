// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using CSUI.Core.Interop;

namespace System.Windows.Input.Ime
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT_I32
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT_I32
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct COMPOSITIONFORM
    {
        public uint dwStyle;
        public POINT_I32 ptCurrentPos;
        public RECT_I32 rcArea;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CANDIDATEFORM
    {
        public uint dwIndex;
        public uint dwStyle;
        public POINT_I32 ptCurrentPos;
        public RECT_I32 rcArea;
    }

    /// <summary>
    /// Event arguments for high-precision modern IME composition events.
    /// </summary>
    public sealed class ModernImeCompositionEventArgs : EventArgs
    {
        public string CompositionText { get; }
        public string ResultText { get; }
        public bool IsCommitted { get; }

        public ModernImeCompositionEventArgs(string compText, string resultText, bool isCommitted)
        {
            CompositionText = compText ?? string.Empty;
            ResultText = resultText ?? string.Empty;
            IsCommitted = isCommitted;
        }
    }

    /// <summary>
    /// Helper for navigating multi-character Unicode grapheme clusters (family emojis, surrogate pairs, skin tones).
    /// Prevents string truncation and broken emoji rendering during caret navigation and backspacing.
    /// </summary>
    public static class GraphemeClusterHelper
    {
        public static int GetNextClusterIndex(string text, int currentIndex)
        {
            if (string.IsNullOrEmpty(text) || currentIndex >= text.Length)
                return text?.Length ?? 0;

            var enumerator = StringInfo.GetTextElementEnumerator(text);
            while (enumerator.MoveNext())
            {
                int elemIndex = enumerator.ElementIndex;
                if (elemIndex > currentIndex)
                    return elemIndex;
            }
            return text.Length;
        }

        public static int GetPreviousClusterIndex(string text, int currentIndex)
        {
            if (string.IsNullOrEmpty(text) || currentIndex <= 0)
                return 0;

            int prev = 0;
            var enumerator = StringInfo.GetTextElementEnumerator(text);
            while (enumerator.MoveNext())
            {
                int elemIndex = enumerator.ElementIndex;
                if (elemIndex >= currentIndex)
                    return prev;
                prev = elemIndex;
            }
            return prev;
        }

        public static int ClusterCount(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return new StringInfo(text).LengthInTextElements;
        }
    }

    /// <summary>
    /// Hardware-accelerated smooth physical caret glide and breathing animation engine.
    /// Decouples visual caret motion from discrete keystrokes for high-refresh-rate (120Hz/144Hz) displays.
    /// </summary>
    public sealed class SmoothCaretTracker
    {
        public Point TargetPosition { get; set; }
        public Point CurrentPosition { get; private set; }
        public double CaretHeight { get; set; } = 20.0;
        public double CaretWidth { get; set; } = 2.0;
        public double Opacity { get; private set; } = 1.0;
        public double GlideSpeed { get; set; } = 12.0; // Spring interpolation speed

        private double _elapsedTimeSeconds = 0;
        private bool _isBlinking = true;

        public SmoothCaretTracker(Point initialPosition)
        {
            TargetPosition = initialPosition;
            CurrentPosition = initialPosition;
        }

        public void JumpTo(Point position)
        {
            TargetPosition = position;
            CurrentPosition = position;
            _elapsedTimeSeconds = 0;
            Opacity = 1.0; // Reset blink cycle to visible on motion
        }

        public void Step(double deltaSeconds)
        {
            _elapsedTimeSeconds += deltaSeconds;

            // 1. Smooth Spring/Decay Glide towards target position
            double alpha = Math.Clamp(GlideSpeed * deltaSeconds, 0.0, 1.0);
            double newX = CurrentPosition.X + (TargetPosition.X - CurrentPosition.X) * alpha;
            double newY = CurrentPosition.Y + (TargetPosition.Y - CurrentPosition.Y) * alpha;
            CurrentPosition = new Point(newX, newY);

            // 2. Harmonic breathing blink animation (1.2 Hz)
            if (_isBlinking)
            {
                // Smooth sine wave between 0.0 and 1.0
                double cycle = Math.Sin(_elapsedTimeSeconds * Math.PI * 2.0 * 1.2);
                Opacity = cycle >= 0 ? 1.0 : 0.0; // Crisp half-period blink
            }
            else
            {
                Opacity = 1.0;
            }
        }
    }

    /// <summary>
    /// Modern IME and Text Services Framework interaction engine.
    /// Handles composition messages and dynamically pins candidate popup windows to subpixel caret positions.
    /// </summary>
    public sealed unsafe class ModernImeService
    {
        private const uint CFS_POINT = 0x0002;
        private const uint CFS_CANDIDATEPOS = 0x0040;
        private const uint GCS_COMPSTR = 0x0008;
        private const uint GCS_RESULTSTR = 0x0800;

        private static readonly nint Imm32 = NativeModule.Load("imm32.dll");

        private static readonly delegate* unmanaged[Stdcall]<nint, nint> _immGetContext =
            Imm32 != 0 ? (delegate* unmanaged[Stdcall]<nint, nint>)NativeModule.GetExport(Imm32, "ImmGetContext") : null;

        private static readonly delegate* unmanaged[Stdcall]<nint, nint, int> _immReleaseContext =
            Imm32 != 0 ? (delegate* unmanaged[Stdcall]<nint, nint, int>)NativeModule.GetExport(Imm32, "ImmReleaseContext") : null;

        private static readonly delegate* unmanaged[Stdcall]<nint, COMPOSITIONFORM*, int> _immSetCompositionWindow =
            Imm32 != 0 ? (delegate* unmanaged[Stdcall]<nint, COMPOSITIONFORM*, int>)NativeModule.GetExport(Imm32, "ImmSetCompositionWindow") : null;

        private static readonly delegate* unmanaged[Stdcall]<nint, CANDIDATEFORM*, int> _immSetCandidateWindow =
            Imm32 != 0 ? (delegate* unmanaged[Stdcall]<nint, CANDIDATEFORM*, int>)NativeModule.GetExport(Imm32, "ImmSetCandidateWindow") : null;

        private static readonly delegate* unmanaged[Stdcall]<nint, uint, void*, uint, int> _immGetCompositionString =
            Imm32 != 0 ? (delegate* unmanaged[Stdcall]<nint, uint, void*, uint, int>)NativeModule.GetExport(Imm32, "ImmGetCompositionStringW") : null;

        public event EventHandler<ModernImeCompositionEventArgs> CompositionStarted;
        public event EventHandler<ModernImeCompositionEventArgs> CompositionUpdated;
        public event EventHandler<ModernImeCompositionEventArgs> CompositionCompleted;

        public bool IsComposing { get; private set; }
        public string CurrentComposition { get; private set; } = string.Empty;

        public ModernImeService()
        {
        }

        public void UpdateCaretPosition(nint hwnd, Point screenPosition, double caretHeight, double dpiScale)
        {
            if (hwnd == 0 || _immGetContext == null) return;

            nint hImc = _immGetContext(hwnd);
            if (hImc == 0) return;

            try
            {
                int px = (int)Math.Round(screenPosition.X * dpiScale);
                int py = (int)Math.Round((screenPosition.Y + caretHeight) * dpiScale);

                // Pin composition window
                if (_immSetCompositionWindow != null)
                {
                    var form = new COMPOSITIONFORM
                    {
                        dwStyle = CFS_POINT,
                        ptCurrentPos = new POINT_I32 { x = px, y = py }
                    };
                    _immSetCompositionWindow(hImc, &form);
                }

                // Pin candidate popup window
                if (_immSetCandidateWindow != null)
                {
                    var cand = new CANDIDATEFORM
                    {
                        dwIndex = 0,
                        dwStyle = CFS_CANDIDATEPOS,
                        ptCurrentPos = new POINT_I32 { x = px, y = py }
                    };
                    _immSetCandidateWindow(hImc, &cand);
                }
            }
            finally
            {
                if (_immReleaseContext != null) _immReleaseContext(hwnd, hImc);
            }
        }

        public bool ProcessImeMessage(nint hwnd, int msg, nint wParam, nint lParam, out nint result)
        {
            result = 0;
            const int WM_IME_STARTCOMPOSITION = 0x010D;
            const int WM_IME_ENDCOMPOSITION = 0x010E;
            const int WM_IME_COMPOSITION = 0x010F;

            switch (msg)
            {
                case WM_IME_STARTCOMPOSITION:
                    IsComposing = true;
                    CurrentComposition = string.Empty;
                    CompositionStarted?.Invoke(this, new ModernImeCompositionEventArgs(string.Empty, string.Empty, false));
                    return false; // Allow default processing to activate IMM

                case WM_IME_COMPOSITION:
                    if (_immGetContext == null) return false;
                    nint hImc = _immGetContext(hwnd);
                    if (hImc != 0)
                    {
                        try
                        {
                            // 1. Result string committed (final characters)
                            if (((long)lParam & GCS_RESULTSTR) != 0)
                            {
                                string res = GetImeString(hImc, GCS_RESULTSTR);
                                CurrentComposition = string.Empty;
                                IsComposing = false;
                                CompositionCompleted?.Invoke(this, new ModernImeCompositionEventArgs(string.Empty, res, true));
                            }
                            // 2. Reading string (in-progress Pinyin / Hiragana)
                            else if (((long)lParam & GCS_COMPSTR) != 0)
                            {
                                string comp = GetImeString(hImc, GCS_COMPSTR);
                                CurrentComposition = comp;
                                IsComposing = true;
                                CompositionUpdated?.Invoke(this, new ModernImeCompositionEventArgs(comp, string.Empty, false));
                            }
                        }
                        finally
                        {
                            if (_immReleaseContext != null) _immReleaseContext(hwnd, hImc);
                        }
                    }
                    return false;

                case WM_IME_ENDCOMPOSITION:
                    IsComposing = false;
                    CurrentComposition = string.Empty;
                    return false;

                default:
                    return false;
            }
        }

        private string GetImeString(nint hImc, uint flag)
        {
            if (_immGetCompositionString == null) return string.Empty;

            int len = _immGetCompositionString(hImc, flag, null, 0);
            if (len <= 0) return string.Empty;

            Span<char> buffer = stackalloc char[len / sizeof(char) + 1];
            fixed (char* p = buffer)
            {
                int read = _immGetCompositionString(hImc, flag, p, (uint)len);
                if (read > 0)
                {
                    return new string(p, 0, read / sizeof(char));
                }
            }
            return string.Empty;
        }
    }
}
