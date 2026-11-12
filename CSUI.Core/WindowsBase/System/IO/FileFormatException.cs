// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.IO
{
    /// <summary>
    /// The exception that is thrown when an input file or a data stream that is supposed to conform to a certain file format specification is malformed.
    /// </summary>
    public class FileFormatException : FormatException
    {
        private readonly Uri _sourceUri;

        public FileFormatException()
            : base("Input file or data stream does not conform to the expected file format specification.")
        {
        }

        public FileFormatException(string message)
            : base(message)
        {
        }

        public FileFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public FileFormatException(Uri sourceUri)
            : base(sourceUri == null
                ? "Input file or data stream does not conform to the expected file format specification."
                : $"Input file '{sourceUri}' does not conform to the expected file format specification.")
        {
            _sourceUri = sourceUri;
        }

        public FileFormatException(Uri sourceUri, string message)
            : base(message)
        {
            _sourceUri = sourceUri;
        }

        public FileFormatException(Uri sourceUri, Exception innerException)
            : base(sourceUri == null
                ? "Input file or data stream does not conform to the expected file format specification."
                : $"Input file '{sourceUri}' does not conform to the expected file format specification.", innerException)
        {
            _sourceUri = sourceUri;
        }

        public FileFormatException(Uri sourceUri, string message, Exception innerException)
            : base(message, innerException)
        {
            _sourceUri = sourceUri;
        }

        public Uri SourceUri => _sourceUri;
    }
}
