// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Media;
using System.Windows.Media.Animation;

namespace System.Windows
{
    /// <summary>
    /// A text decoration
    /// </summary>
    public sealed partial class TextDecoration : Animatable
    {
        public TextDecoration()
        {
        }

        public TextDecoration(
            TextDecorationLocation location,
            Pen                    pen,
            double                 penOffset,
            TextDecorationUnit     penOffsetUnit,
            TextDecorationUnit     penThicknessUnit
            )
        {
            Location         = location;
            Pen              = pen;
            PenOffset        = penOffset;
            PenOffsetUnit    = penOffsetUnit;
            PenThicknessUnit = penThicknessUnit;
        }

        internal bool ValueEquals(TextDecoration textDecoration)
        {
            if (textDecoration == null)
                return false;

            if (this == textDecoration)
                return true;

            return (
               Location         == textDecoration.Location
            && PenOffset        == textDecoration.PenOffset
            && PenOffsetUnit    == textDecoration.PenOffsetUnit
            && PenThicknessUnit == textDecoration.PenThicknessUnit
            && (Pen == null ? textDecoration.Pen == null : Pen.Equals(textDecoration.Pen))
            );
        }
    }
}
