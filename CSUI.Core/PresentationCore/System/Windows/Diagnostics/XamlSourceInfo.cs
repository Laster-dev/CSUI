// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;

namespace System.Windows.Diagnostics
{
    [DebuggerDisplay("{{line={LineNumber}, offset={LinePosition}, uri={SourceUri}}}")]
    public class XamlSourceInfo
    {
        public Uri SourceUri { get; private set; }
        public int LineNumber { get; private set; }
        public int LinePosition { get; private set; }

        public XamlSourceInfo(Uri sourceUri, int lineNumber, int linePosition)
        {
            SourceUri = sourceUri;
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }
    }
}
