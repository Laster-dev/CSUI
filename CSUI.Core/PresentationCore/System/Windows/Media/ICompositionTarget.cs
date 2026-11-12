// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media
{
    /// <summary>
    /// With this interface we register CompositionTargets with the
    /// MediaContext.
    /// </summary>
    internal interface ICompositionTarget : IDisposable
    {
        void Render(bool inResize);
    }
}
