// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;

namespace System.Windows.Controls.Primitives
{
    public partial class ResizeGrip : Control
    {
        static partial void RegisterDefaultTheme();
        static ResizeGrip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizeGrip), new FrameworkPropertyMetadata(typeof(ResizeGrip)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ResizeGrip));

            Window.IWindowServiceProperty.OverrideMetadata(
                typeof(ResizeGrip),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(_OnWindowServiceChanged)));
            RegisterDefaultTheme();
        }

        public ResizeGrip() : base()
        {
        }

        private static void _OnWindowServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ResizeGrip rg = d as ResizeGrip;
            Debug.Assert(rg != null, "DependencyObject must be of type ResizeGrip.");

            rg.OnWindowServiceChanged(e.OldValue as Window, e.NewValue as Window);
        }

        private void OnWindowServiceChanged(Window oldWindow, Window newWindow)
        {
            if ((oldWindow != null) && (oldWindow != newWindow))
            {
                oldWindow.ClearResizeGripControl(this);
            }

            newWindow?.SetResizeGripControl(this);
        }

        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;
    }
}
