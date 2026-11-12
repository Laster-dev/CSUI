// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Event arguments communicating an element that is being prepared to be re-virtualized.
    /// </summary>
    public class CleanUpVirtualizedItemEventArgs : RoutedEventArgs
    {
        public CleanUpVirtualizedItemEventArgs(object value, UIElement element)
            : base(MS.Internal.Controls.ControlFoundationHooks.CleanUpVirtualizedItemEvent)
        {
            _value = value;
            _element = element;
        }

        /// <summary>
        ///     The original data value.
        ///     If the data value is a visual element, it will be the same as UIElement.
        /// </summary>
        public object Value
        {
            get
            {
                return _value;
            }
        }

        /// <summary>
        ///     The instance of the visual element that represented the data value.
        ///     If the data value is a visual element, it will be the same as UIElement.
        /// </summary>
        public UIElement UIElement
        {
            get
            {
                return _element;
            }
        }

        /// <summary>
        ///     Set by handlers of this event to true to indicate that the 
        ///     re-virtualizing of this item should not happen.
        /// </summary>
        public bool Cancel
        {
            get
            {
                return _cancel;
            }

            set
            {
                _cancel = value;
            }
        }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler is CleanUpVirtualizedItemEventHandler handler)
            {
                handler(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }

        private object _value;
        private UIElement _element;
        private bool _cancel;
    }

    /// <summary>
    ///     The delegate to use for handlers that receive CleanUpVirtualizedItemEventArgs.
    /// </summary>
    public delegate void CleanUpVirtualizedItemEventHandler(object sender, CleanUpVirtualizedItemEventArgs e);
}

