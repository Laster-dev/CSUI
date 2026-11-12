// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.Windows.Media.Animation;

namespace System.Windows
{
    /// <summary>
    /// A collection of text decoration instances
    /// </summary>
    public sealed partial class TextDecorationCollection : Animatable, IList
    {
        internal bool ValueEquals(TextDecorationCollection textDecorations)
        {
            if (textDecorations == null)
                return false;

            if (this == textDecorations)
                return true;

            if (this.Count != textDecorations.Count)
                return false;

            for (int i = 0; i < this.Count; i++)
            {
                if (!this[i].ValueEquals(textDecorations[i]))
                    return false;
            }
            return true;
        }

        public void Add(IEnumerable<TextDecoration> textDecorations)
        {
            ArgumentNullException.ThrowIfNull(textDecorations);

            foreach (TextDecoration textDecoration in textDecorations)
            {
                Add(textDecoration);
            }
        }

        public bool TryRemove(IEnumerable<TextDecoration> textDecorations, out TextDecorationCollection result)
        {
            ArgumentNullException.ThrowIfNull(textDecorations);

            bool removed = false;
            result = this.Clone();

            foreach (TextDecoration textDecoration in textDecorations)
            {
                for (int i = result.Count - 1; i >= 0; --i)
                {
                    if (result[i].ValueEquals(textDecoration))
                    {
                        result.RemoveAt(i);
                        removed = true;
                    }
                }
            }

            return removed;
        }
    }
}
