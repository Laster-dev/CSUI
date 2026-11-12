// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using SR = MS.Internal.PresentationFramework.SR;

namespace System.Windows.Markup
{
    internal static class XamlSerializerUtil
    {
        internal static void ThrowIfNonWhiteSpaceInAddText(string s, object parent)
        {
            if (s != null)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    if (!char.IsWhiteSpace(s[i]))
                    {
                        throw new ArgumentException(SR.Format(SR.NonWhiteSpaceInAddText, s));
                    }
                }
            }
        }
    }
}
