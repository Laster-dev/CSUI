// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Windows.Controls
{
    public delegate void TextChangedEventHandler(object sender, TextChangedEventArgs e);

    /// <summary>
    /// Provides data for the TextChanged event.
    /// </summary>
    public class TextChangedEventArgs : RoutedEventArgs
    {
        private readonly ICollection<TextChange> _changes;

        public TextChangedEventArgs(RoutedEvent id)
            : this(id, UndoAction.None, null)
        {
        }

        public TextChangedEventArgs(RoutedEvent id, UndoAction action)
            : this(id, action, null)
        {
        }

        public TextChangedEventArgs(RoutedEvent id, UndoAction action, ICollection<TextChange> changes)
        {
            RoutedEvent = id;
            UndoAction = action;
            _changes = changes;
        }

        public UndoAction UndoAction { get; }

        public ICollection<TextChange> Changes =>
            _changes == null
                ? Array.Empty<TextChange>()
                : new ReadOnlyCollection<TextChange>(new List<TextChange>(_changes));

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
