// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    /// <summary>
    ///     This is the base class for all attached property browsable attributes.
    /// </summary>
    public abstract class AttachedPropertyBrowsableAttribute : Attribute
    {
        internal virtual bool UnionResults { get { return false; } }

        internal abstract bool IsBrowsable(DependencyObject d, DependencyProperty dp);
    }
}
