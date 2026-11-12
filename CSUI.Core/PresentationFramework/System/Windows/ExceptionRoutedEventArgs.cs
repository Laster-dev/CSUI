// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    public class ExceptionRoutedEventArgs : RoutedEventArgs
    {
        internal ExceptionRoutedEventArgs(RoutedEvent routedEvent, object sender, Exception errorException)
            : base(routedEvent, sender)
        {
            ArgumentNullException.ThrowIfNull(errorException);
            _errorException = errorException;
        }

        public Exception ErrorException
        {
            get { return _errorException; }
        }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler is EventHandler<ExceptionRoutedEventArgs> typedHandler)
            {
                typedHandler(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }

        private readonly Exception _errorException;
    }
}
