// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Media
{
    /// <summary>
    /// arguments for media failure event handlers
    /// </summary>
    public sealed class ExceptionEventArgs : EventArgs
    {
        internal ExceptionEventArgs(Exception errorException)
            : base()
        {
            ArgumentNullException.ThrowIfNull(errorException);
            _errorException = errorException;
        }

        /// <summary>
        /// Error Exception
        /// </summary>
        public Exception ErrorException => _errorException;

        private readonly Exception _errorException;
    }

    /// <summary>
    /// Arguments for any scripting commands associated with the media.
    /// </summary>
    public sealed class MediaScriptCommandEventArgs : EventArgs
    {
        internal MediaScriptCommandEventArgs(string parameterType, string parameterValue)
            : base()
        {
            ArgumentNullException.ThrowIfNull(parameterType);
            ArgumentNullException.ThrowIfNull(parameterValue);

            _parameterType = parameterType;
            _parameterValue = parameterValue;
        }

        public string ParameterType => _parameterType;

        public string ParameterValue => _parameterValue;

        private readonly string _parameterType;
        private readonly string _parameterValue;
    }
}
