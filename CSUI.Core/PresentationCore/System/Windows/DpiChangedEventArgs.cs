// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using MS.Internal;
using MS.Win32;

namespace System.Windows
{
    public sealed class HwndDpiChangedEventArgs : HandledEventArgs
    {
        [Obsolete]
        internal unsafe HwndDpiChangedEventArgs(double oldDpiX, double oldDpiY, double newDpiX, double newDpiY, IntPtr lParam)
            : base(false)
        {
            OldDpi = new DpiScale(oldDpiX / DpiUtil.DefaultPixelsPerInch, oldDpiY / DpiUtil.DefaultPixelsPerInch);
            NewDpi = new DpiScale(newDpiX / DpiUtil.DefaultPixelsPerInch, newDpiY / DpiUtil.DefaultPixelsPerInch);
            NativeMethods.RECT suggestedRect = *(NativeMethods.RECT*)lParam;
            this.SuggestedRect = new Rect(suggestedRect.left, suggestedRect.top, suggestedRect.Width, suggestedRect.Height);
        }

        public HwndDpiChangedEventArgs(DpiScale oldDpi, DpiScale newDpi, Rect suggestedRect)
            : base(false)
        {
            OldDpi = oldDpi;
            NewDpi = newDpi;
            SuggestedRect = suggestedRect;
        }

        public DpiScale OldDpi { get; private set; }
        public DpiScale NewDpi { get; private set; }
        public Rect SuggestedRect { get; private set; }
    }

    internal class HwndDpiChangedAfterParentEventArgs : HandledEventArgs
    {
        internal HwndDpiChangedAfterParentEventArgs(DpiScale oldDpi, DpiScale newDpi, Rect suggestedRect)
            : base(false)
        {
            OldDpi = oldDpi;
            NewDpi = newDpi;
            SuggestedRect = suggestedRect;
        }

        internal HwndDpiChangedAfterParentEventArgs(HwndDpiChangedEventArgs e)
            : this(e.OldDpi, e.NewDpi, e.SuggestedRect)
        {
        }

        internal DpiScale OldDpi { get; }
        internal DpiScale NewDpi { get; }
        internal Rect SuggestedRect { get; }

        public static explicit operator HwndDpiChangedEventArgs(HwndDpiChangedAfterParentEventArgs e)
        {
            return new HwndDpiChangedEventArgs(e.OldDpi, e.NewDpi, e.SuggestedRect);
        }
    }

    public delegate void DpiChangedEventHandler(object sender, DpiChangedEventArgs e);

    public delegate void HwndDpiChangedEventHandler(object sender, HwndDpiChangedEventArgs e);

    public sealed class DpiChangedEventArgs : RoutedEventArgs
    {
        public DpiChangedEventArgs(DpiScale oldDpi, DpiScale newDpi, RoutedEvent routedEvent, object source)
            : base(routedEvent, source)
        {
            OldDpi = oldDpi;
            NewDpi = newDpi;
        }

        public DpiScale OldDpi { get; private set; }
        public DpiScale NewDpi { get; private set; }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            var handler = (DpiChangedEventHandler)genericHandler;
            handler(genericTarget, this);
        }
    }

    internal class DpiRecursiveChangeArgs
    {
        internal DpiRecursiveChangeArgs(DpiFlags dpiFlags, DpiScale oldDpiScale, DpiScale newDpiScale)
        {
            DpiScaleFlag1 = dpiFlags.DpiScaleFlag1;
            DpiScaleFlag2 = dpiFlags.DpiScaleFlag2;
            Index = dpiFlags.Index;
            OldDpiScale = oldDpiScale;
            NewDpiScale = newDpiScale;
        }

        internal bool DpiScaleFlag1 { get; set; }
        internal bool DpiScaleFlag2 { get; set; }
        internal int Index { get; set; }
        internal DpiScale OldDpiScale { get; set; }
        internal DpiScale NewDpiScale { get; set; }
    }

    internal class DpiFlags
    {
        internal DpiFlags(bool dpiScaleFlag1, bool dpiScaleFlag2, int index)
        {
            DpiScaleFlag1 = dpiScaleFlag1;
            DpiScaleFlag2 = dpiScaleFlag2;
            Index = index;
        }

        internal bool DpiScaleFlag1 { get; set; }
        internal bool DpiScaleFlag2 { get; set; }
        internal int Index { get; set; }
    }
}
