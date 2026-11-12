// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using MS.Win32;
using System.Runtime.InteropServices;

namespace MS.Utility
{
    internal partial class DpiAwarenessContextHandle : SafeHandle, IEquatable<IntPtr>, IEquatable<DpiAwarenessContextHandle>, IEquatable<DpiAwarenessContextValue>
    {
        static DpiAwarenessContextHandle()
        {
            WellKnownContextValues = new Dictionary<DpiAwarenessContextValue, IntPtr>
            {
                { DpiAwarenessContextValue.Unaware, new IntPtr((int)DpiAwarenessContextValue.Unaware) },
                { DpiAwarenessContextValue.SystemAware, new IntPtr((int)DpiAwarenessContextValue.SystemAware) },
                { DpiAwarenessContextValue.PerMonitorAware, new IntPtr((int)DpiAwarenessContextValue.PerMonitorAware) },
                { DpiAwarenessContextValue.PerMonitorAwareVersion2, new IntPtr((int)DpiAwarenessContextValue.PerMonitorAwareVersion2) },
            };

            DPI_AWARENESS_CONTEXT_UNAWARE =
                new DpiAwarenessContextHandle(WellKnownContextValues[DpiAwarenessContextValue.Unaware]);

            DPI_AWARENESS_CONTEXT_SYSTEM_AWARE =
                new DpiAwarenessContextHandle(WellKnownContextValues[DpiAwarenessContextValue.SystemAware]);

            DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE =
                new DpiAwarenessContextHandle(WellKnownContextValues[DpiAwarenessContextValue.PerMonitorAware]);

            DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 =
                new DpiAwarenessContextHandle(WellKnownContextValues[DpiAwarenessContextValue.PerMonitorAwareVersion2]);
        }

        internal DpiAwarenessContextHandle()
            : base(IntPtr.Zero, false)
        {
        }

        internal DpiAwarenessContextHandle(DpiAwarenessContextValue dpiAwarenessContextValue)
            : base(WellKnownContextValues[dpiAwarenessContextValue], false)
        {
        }

        internal DpiAwarenessContextHandle(IntPtr dpiContext)
            : base(dpiContext, false)
        {
        }

        protected DpiAwarenessContextHandle(IntPtr invalidHandleValue, bool ownsHandle)
            : base(invalidHandleValue, false)
        {
        }

        public override bool IsInvalid => true;

        internal static DpiAwarenessContextHandle DPI_AWARENESS_CONTEXT_UNAWARE { get; }
        internal static DpiAwarenessContextHandle DPI_AWARENESS_CONTEXT_SYSTEM_AWARE { get; }
        internal static DpiAwarenessContextHandle DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE { get; }
        internal static DpiAwarenessContextHandle DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 { get; }

        private static Dictionary<DpiAwarenessContextValue, IntPtr> WellKnownContextValues { get; }

        public static explicit operator DpiAwarenessContextValue(DpiAwarenessContextHandle dpiAwarenessContextHandle)
        {
            foreach (DpiAwarenessContextValue dpiContextValue in WellKnownContextValues.Keys)
            {
                if (dpiAwarenessContextHandle.Equals(dpiContextValue))
                {
                    return dpiContextValue;
                }
            }

            return DpiAwarenessContextValue.Invalid;
        }

        public bool Equals(DpiAwarenessContextHandle dpiContextHandle)
        {
            return dpiContextHandle != null &&
                DpiUtil.AreDpiAwarenessContextsEqual(this.DangerousGetHandle(), dpiContextHandle.DangerousGetHandle());
        }

        public bool Equals(IntPtr dpiContext)
        {
            return DpiUtil.AreDpiAwarenessContextsEqual(this.DangerousGetHandle(), dpiContext);
        }

        public bool Equals(DpiAwarenessContextValue dpiContextEnumValue)
        {
            return this.Equals(WellKnownContextValues[dpiContextEnumValue]);
        }

        public override bool Equals(object obj)
        {
            if (obj is IntPtr ptr)
            {
                return this.Equals(ptr);
            }
            else if (obj is DpiAwarenessContextHandle handle)
            {
                return this.Equals(handle);
            }
            else if (obj is DpiAwarenessContextValue val)
            {
                return this.Equals(val);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return ((DpiAwarenessContextValue)this).GetHashCode();
        }

        protected override bool ReleaseHandle() => true;
    }
}
