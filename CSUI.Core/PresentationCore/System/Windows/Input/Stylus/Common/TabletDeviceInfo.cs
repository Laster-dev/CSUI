#nullable disable
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The struct is used to store the tablet device size information.
    /// </summary>
    internal struct TabletDeviceSizeInfo
    {
        public Size TabletSize;
        public Size ScreenSize;

        // Constructor
        internal TabletDeviceSizeInfo(Size tabletSize, Size screenSize)
        {
            TabletSize = tabletSize;
            ScreenSize = screenSize;
        }
    }


    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The class is used to store tablet device information.
    /// </summary>
    internal class TabletDeviceInfo
    {
        public int Id;
        public string Name;
        public string PlugAndPlayId;
        public TabletDeviceSizeInfo SizeInfo;
        public TabletHardwareCapabilities HardwareCapabilities;
        public TabletDeviceType DeviceType;
        public ReadOnlyCollection<StylusPointProperty> StylusPointProperties;
        public int PressureIndex;
#pragma warning disable CS0649
        public StylusDeviceInfo[] StylusDevicesInfo;
#pragma warning restore CS0649
    }
}

