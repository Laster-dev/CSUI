// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This interface should be implemented by all key frames to provide
    /// untyped access to the key time and value.
    /// </summary>
    public interface IKeyFrame
    {
        KeyTime KeyTime { get; set; }

        object Value { get; set; }
    }
}
