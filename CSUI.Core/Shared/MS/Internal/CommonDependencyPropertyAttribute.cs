// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Diagnostics;

namespace MS.Internal.PresentationCore
{
    /// <summary>
    ///     An attribute that indicates that a DependencyProperty declaration is common
    ///     enough to be included in KnownTypes.cs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [Conditional("COMMONDPS")]
    internal sealed class CommonDependencyPropertyAttribute : Attribute
    {
    }
}

namespace MS.Internal.PresentationFramework
{
    [AttributeUsage(AttributeTargets.Field)]
    [Conditional("COMMONDPS")]
    internal sealed class CommonDependencyPropertyAttribute : Attribute
    {
    }
}
