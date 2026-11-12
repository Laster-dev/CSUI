// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Markup
{
    /// <summary>
    /// This is an interface that should be implemented by classes that wish to provide
    /// fallback values for one or more of their properties.
    /// </summary>
    internal interface IProvidePropertyFallback
    {
        /// <summary>
        /// Says if the type can provide fallback value for the given property
        /// </summary>
        bool CanProvidePropertyFallback(string property);

        /// <summary>
        /// Returns the fallback value for the given property.
        /// </summary>
        object ProvidePropertyFallback(string property, Exception cause);
    }
}
