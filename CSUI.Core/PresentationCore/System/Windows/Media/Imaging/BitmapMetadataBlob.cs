// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Represents a binary metadata blob (PROPVARIANT VT_BLOB).
    /// </summary>
    public class BitmapMetadataBlob
    {
        public BitmapMetadataBlob(byte[] blob)
        {
            _blob = blob;
        }

        public byte[] GetBlobValue()
        {
            return (byte[])_blob?.Clone();
        }

        internal byte[] InternalGetBlobValue()
        {
            return _blob;
        }

        private readonly byte[] _blob;
    }
}
