// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//


#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// This is the base class for packing together parameters for a hit test pass.
    /// </summary>
    public abstract class HitTestParameters
    {
        // Prevent 3rd parties from extending this abstract base class.
        internal HitTestParameters() {}
    }
}

