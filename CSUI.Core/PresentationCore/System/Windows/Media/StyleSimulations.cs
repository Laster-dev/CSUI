// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media
{
    /// <summary>
    /// Font style simulation
    /// </summary>
    [Flags]
    public enum StyleSimulations
    {
        None                 = 0,
        BoldSimulation       = 1,
        ItalicSimulation     = 2,
        BoldItalicSimulation = BoldSimulation | ItalicSimulation
    }
}
