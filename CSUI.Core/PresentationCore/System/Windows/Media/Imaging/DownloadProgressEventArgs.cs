// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Event args for the DownloadProgress event.
    /// </summary>
    public class DownloadProgressEventArgs : EventArgs
    {
        internal DownloadProgressEventArgs(int percentComplete)
        {
            _percentComplete = percentComplete;
        }

        /// <summary>
        /// Returns the progress between 1-100
        /// </summary>
        public int Progress => _percentComplete;

        private readonly int _percentComplete;
    }
}
