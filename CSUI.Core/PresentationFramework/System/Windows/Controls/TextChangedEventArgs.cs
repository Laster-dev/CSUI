// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Controls
{
    public delegate void TextChangedEventHandler(object sender, TextChangedEventArgs e);

    /// <summary>
    /// Provides data for the TextChanged event.
    /// </summary>
    public class TextChangedEventArgs : RoutedEventArgs
    {
        public TextChangedEventArgs(RoutedEvent id)
        {
            RoutedEvent = id;
        }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler is TextChangedEventHandler handler)
            {
                handler(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }
    }
}
