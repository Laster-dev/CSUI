// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// Interface to be implemented by an object that can animate a property value.
    /// </summary>
    public interface IAnimation
    {
        object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock);
    }
}
