// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    /// <summary>
    /// Represents a data format by wrapping a format name and numeric ID.
    /// </summary>
    public sealed class DataFormat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFormat"/> class.
        /// </summary>
        public DataFormat(string name, int id)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            _name = name;
            _id = id;
        }

        /// <summary>
        /// Gets the name of this format.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets the numeric ID number for this format.
        /// </summary>
        public int Id => _id;

        private readonly string _name;
        private readonly int _id;
    }
}
