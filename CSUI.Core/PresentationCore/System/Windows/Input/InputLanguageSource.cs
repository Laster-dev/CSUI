// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;

using MS.Win32;

namespace System.Windows.Input
{
    /// <summary>
    /// This is an internal. The source for input languages (Win32 HKL-backed, NativeAOT zero-COM).
    /// </summary>
    internal sealed class InputLanguageSource : IInputLanguageSource, IDisposable
    {
        internal InputLanguageSource(InputLanguageManager inputlanguagemanager)
        {
            _inputlanguagemanager = inputlanguagemanager;

            // initialize the current input language.
            _langid = (short)NativeMethods.IntPtrToInt32(SafeNativeMethods.GetKeyboardLayout(0));

            // store the dispatcher thread id. This will be used to call GetKeyboardLayout() from
            // other thread.
            _dispatcherThreadId = SafeNativeMethods.GetCurrentThreadId();

            // Register source
            _inputlanguagemanager.RegisterInputLanguageSource(this);
        }

        public void Dispose()
        {
            Uninitialize();
        }

        public void Initialize()
        {
        }

        public void Uninitialize()
        {
        }

        public CultureInfo CurrentInputLanguage
        {
            get
            {
                return new CultureInfo(_CurrentInputLanguage);
            }
            set
            {
                _CurrentInputLanguage = (short)value.LCID;
            }
        }

        public IEnumerable InputLanguageList
        {
            get
            {
                int count = SafeNativeMethods.GetKeyboardLayoutList(0, null);
                if (count <= 0)
                {
                    return new CultureInfo[1] { CurrentInputLanguage };
                }

                IntPtr[] hkls = new IntPtr[count];
                count = SafeNativeMethods.GetKeyboardLayoutList(count, hkls);
                if (count <= 0)
                {
                    return new CultureInfo[1] { CurrentInputLanguage };
                }

                var result = new List<CultureInfo>(count);
                var seen = new HashSet<short>();
                for (int i = 0; i < count; i++)
                {
                    short langId = (short)NativeMethods.IntPtrToInt32(hkls[i]);
                    if (seen.Add(langId))
                    {
                        result.Add(new CultureInfo(langId));
                    }
                }
                return result.ToArray();
            }
        }

        internal bool OnLanguageChange(short langid)
        {
            if (_langid != langid)
            {
                if (InputLanguageManager.Current.Source == this)
                {
                    return InputLanguageManager.Current.ReportInputLanguageChanging(new CultureInfo(langid), new CultureInfo(_langid));
                }
            }

            return true;
        }

        internal void OnLanguageChanged()
        {
            short langid = _CurrentInputLanguage;
            if (_langid != langid)
            {
                short prevlangid = _langid;
                _langid = langid;

                if (InputLanguageManager.Current.Source == this)
                {
                    InputLanguageManager.Current.ReportInputLanguageChanged(new CultureInfo(langid), new CultureInfo(prevlangid));
                }
            }
        }

        private short _CurrentInputLanguage
        {
            get
            {
                return (short)NativeMethods.IntPtrToInt32(SafeNativeMethods.GetKeyboardLayout(_dispatcherThreadId));
            }
            set
            {
                int count = SafeNativeMethods.GetKeyboardLayoutList(0, null);
                IntPtr targetHkl = IntPtr.Zero;
                if (count > 0)
                {
                    IntPtr[] hkls = new IntPtr[count];
                    count = SafeNativeMethods.GetKeyboardLayoutList(count, hkls);
                    for (int i = 0; i < count; i++)
                    {
                        if ((short)NativeMethods.IntPtrToInt32(hkls[i]) == value)
                        {
                            targetHkl = hkls[i];
                            break;
                        }
                    }
                }

                if (targetHkl == IntPtr.Zero)
                {
                    targetHkl = new IntPtr((ushort)value);
                }

                if (targetHkl != IntPtr.Zero)
                {
                    SafeNativeMethods.ActivateKeyboardLayout(new HandleRef(this, targetHkl), 0);
                }
            }
        }

        private short _langid;
        private int _dispatcherThreadId;
        private InputLanguageManager _inputlanguagemanager;
    }
}
