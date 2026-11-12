// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    internal static partial class ValidateEnums
    {
        public static bool IsTextDecorationUnitValid(object valueObject)
        {
            TextDecorationUnit value = (TextDecorationUnit)valueObject;

            return (value == TextDecorationUnit.FontRecommended) ||
                   (value == TextDecorationUnit.FontRenderingEmSize) ||
                   (value == TextDecorationUnit.Pixel);
        }

        public static bool IsTextDecorationLocationValid(object valueObject)
        {
            TextDecorationLocation value = (TextDecorationLocation)valueObject;

            return (value == TextDecorationLocation.Underline) ||
                   (value == TextDecorationLocation.OverLine) ||
                   (value == TextDecorationLocation.Strikethrough) ||
                   (value == TextDecorationLocation.Baseline);
        }
    }
}
