// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

#nullable disable

namespace System.Windows.Media.Effects
{
    internal static partial class ValidateEnums
    {
        /// <summary>
        ///     Returns whether or not an enumeration instance is a valid value.
        /// </summary>
        public static bool IsKernelTypeValid(object valueObject)
        {
            KernelType value = (KernelType)valueObject;

            return (value == KernelType.Gaussian) ||
                   (value == KernelType.Box);
        }

        /// <summary>
        ///     Returns whether or not an enumeration instance is a valid value.
        /// </summary>
        public static bool IsRenderingBiasValid(object valueObject)
        {
            RenderingBias value = (RenderingBias)valueObject;

            return (value == RenderingBias.Performance) ||
                   (value == RenderingBias.Quality);
        }
    }
}
