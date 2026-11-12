// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// A class registers this type of Metadata for a property if the property
    /// can be independently animated on that class.
    /// </summary>
    internal class IndependentlyAnimatedPropertyMetadata : UIPropertyMetadata
    {
        internal IndependentlyAnimatedPropertyMetadata(object defaultValue) : base(defaultValue) { }

        internal IndependentlyAnimatedPropertyMetadata(
            object defaultValue,
            PropertyChangedCallback propertyChangedCallback,
            CoerceValueCallback coerceValueCallback)
            : base(defaultValue, propertyChangedCallback, coerceValueCallback) { }

        internal override PropertyMetadata CreateInstance()
        {
            return new IndependentlyAnimatedPropertyMetadata(DefaultValue);
        }
    }
}
