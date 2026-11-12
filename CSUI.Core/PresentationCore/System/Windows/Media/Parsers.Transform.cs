// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Media;

#nullable disable

namespace MS.Internal
{
    internal static partial class Parsers
    {
        /// <summary>
        /// ParseTransform - parse a Transform from a string
        /// </summary>
        internal static Transform ParseTransform(
            string transformString,
            IFormatProvider formatProvider)
        {
            Matrix matrix = Matrix.Parse(transformString);

            return new MatrixTransform(matrix);
        }
    }
}
