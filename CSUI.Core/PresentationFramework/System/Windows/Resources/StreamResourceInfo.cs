// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.IO;

namespace System.Windows.Resources
{
    /// <summary>
    /// Stores information for a stream resource used in Windows Presentation Foundation (WPF).
    /// </summary>
    public class StreamResourceInfo
    {
        public StreamResourceInfo()
        {
        }

        public StreamResourceInfo(Stream stream, string contentType)
        {
            Stream = stream;
            ContentType = contentType;
        }

        public Stream Stream { get; }

        public string ContentType { get; }
    }
}
