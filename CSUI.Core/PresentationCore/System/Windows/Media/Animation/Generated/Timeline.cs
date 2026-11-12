// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Animation
{
    public abstract partial class Timeline : Animatable
    {
        public new Timeline Clone() => (Timeline)base.Clone();

        public new Timeline CloneCurrentValue() => (Timeline)base.CloneCurrentValue();
    }
}
