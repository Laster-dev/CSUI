// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// Internal contract implemented by 3D visual containers (Viewport3DVisual and Visual3D).
    /// </summary>
    internal interface IVisual3DContainer
    {
        void AddChild(Visual3D child);
        void RemoveChild(Visual3D child);
        int GetChildrenCount();
        Visual3D GetChild(int index);
        void VerifyAPIReadOnly();
        void VerifyAPIReadOnly(DependencyObject other);
        void VerifyAPIReadWrite();
        void VerifyAPIReadWrite(DependencyObject other);
    }
}
