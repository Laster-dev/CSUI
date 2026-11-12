// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Linq;

namespace MS.Internal.Hashing.PresentationFramework;

internal static class HashHelper
{
    private static readonly Type[] s_unreliableTypes =
    [
        typeof(System.Windows.Media.CharacterMetrics),
        typeof(System.Windows.Media.FamilyTypeface),
        typeof(System.Windows.Media.NumberSubstitution),
        typeof(System.Windows.ComponentResourceKey)
    ];

    /// <summary>
    ///  Certain objects don't have reliable hashcodes, and cannot be used in a Hashtable, Dictionary, etc.
    /// </summary>
    internal static bool HasReliableHashCode(object item) => item is not null && !s_unreliableTypes.Contains(item.GetType());
}
