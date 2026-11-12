// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System.Windows.Threading;

namespace System.Windows.Input
{
    /// <summary>
    /// Tracks keyboard focus for input method enabling/disabling.
    /// </summary>
    internal class TextServicesManager : DispatcherObject
    {
        internal TextServicesManager(InputManager inputManager)
        {
            _inputManager = inputManager;
        }

        // Track the focus of KeyboardDevice. KeyboardDevice.ChangeFocus() calls this.
        internal void Focus(DependencyObject focus)
        {
            if (focus == null)
            {
                this.Dispatcher.IsTSFMessagePumpEnabled = false;
                return;
            }

            this.Dispatcher.IsTSFMessagePumpEnabled = true;

            if ((bool)focus.GetValue(InputMethod.IsInputMethodSuspendedProperty))
            {
                return;
            }

            InputMethod.Current.EnableOrDisableInputMethod((bool)focus.GetValue(InputMethod.IsInputMethodEnabledProperty));
        }

        private readonly InputManager _inputManager;
    }
}
