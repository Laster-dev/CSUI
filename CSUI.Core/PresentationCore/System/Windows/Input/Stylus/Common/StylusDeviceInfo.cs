#nullable disable
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The Stylus struct used to store Stylus cursor information.
    /// </summary>
#pragma warning disable CS0649
    internal struct StylusDeviceInfo
    {
        public string   CursorName;
        public int      CursorId;
        public bool     CursorInverted;
        public StylusButtonCollection ButtonCollection;
    }
#pragma warning restore CS0649
}

