// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows.Media;

namespace MS.Internal.WindowsRuntime
{
    namespace Windows.UI.ViewManagement
    {
        internal class UISettings : IDisposable
        {
            private readonly bool _isSupported;
            private IntPtr _uisettings;

            private static readonly Color _fallbackAccentColor = Color.FromArgb(0xff, 0x00, 0x78, 0xd4);

            private Color _accentColor, _accentLight1, _accentLight2, _accentLight3;
            private Color _accentDark1, _accentDark2, _accentDark3;

            private bool _useFallbackColor = false;

            internal UISettings()
            {
                _isSupported = false;

                try
                {
                    _uisettings = UISettingsRCW.GetUISettings3Pointer();
                }
                catch
                {
                    _uisettings = IntPtr.Zero;
                }

                if (_uisettings != IntPtr.Zero)
                {
                    _isSupported = true;
                    TryUpdateAccentColors();
                }
            }

            internal bool TryGetColorValue(UISettingsRCW.UIColorType desiredColor, out Color color)
            {
                if (_isSupported && _uisettings != IntPtr.Zero)
                {
                    int hr = UISettingsRCW.TryGetColorValue(_uisettings, desiredColor, out UISettingsRCW.UIColor uiColor);
                    if (hr >= 0)
                    {
                        color = Color.FromArgb(uiColor.A, uiColor.R, uiColor.G, uiColor.B);
                        return true;
                    }
                }

                color = _fallbackAccentColor;
                return false;
            }

            internal void TryUpdateAccentColors()
            {
                _useFallbackColor = true;

                if (_isSupported)
                {
                    try
                    {
                        if (TryGetColorValue(UISettingsRCW.UIColorType.Accent, out Color systemAccent))
                        {
                            bool result = true;
                            if (_accentColor != systemAccent)
                            {
                                result &= TryGetColorValue(UISettingsRCW.UIColorType.AccentLight1, out _accentLight1);
                                result &= TryGetColorValue(UISettingsRCW.UIColorType.AccentLight2, out _accentLight2);
                                result &= TryGetColorValue(UISettingsRCW.UIColorType.AccentLight3, out _accentLight3);
                                result &= TryGetColorValue(UISettingsRCW.UIColorType.AccentDark1, out _accentDark1);
                                result &= TryGetColorValue(UISettingsRCW.UIColorType.AccentDark2, out _accentDark2);
                                result &= TryGetColorValue(UISettingsRCW.UIColorType.AccentDark3, out _accentDark3);
                                _accentColor = systemAccent;
                            }

                            _useFallbackColor = !result;
                        }
                    }
                    catch
                    {
                        // Keep fallback colors cleanly
                    }
                }
            }

            #region Color Properties

            internal Color AccentColor => _useFallbackColor ? _fallbackAccentColor : _accentColor;
            internal Color AccentLight1 => _useFallbackColor ? _fallbackAccentColor : _accentLight1;
            internal Color AccentLight2 => _useFallbackColor ? _fallbackAccentColor : _accentLight2;
            internal Color AccentLight3 => _useFallbackColor ? _fallbackAccentColor : _accentLight3;
            internal Color AccentDark1 => _useFallbackColor ? _fallbackAccentColor : _accentDark1;
            internal Color AccentDark2 => _useFallbackColor ? _fallbackAccentColor : _accentDark2;
            internal Color AccentDark3 => _useFallbackColor ? _fallbackAccentColor : _accentDark3;

            #endregion

            #region IDisposable

            private bool _disposed = false;

            ~UISettings()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (_uisettings != IntPtr.Zero)
                    {
                        try
                        {
                            UISettingsRCW.Release(_uisettings);
                        }
                        catch
                        {
                            // Do not raise exceptions from finalizer
                        }

                        _uisettings = IntPtr.Zero;
                    }

                    _disposed = true;
                }
            }

            #endregion
        }
    }
}
