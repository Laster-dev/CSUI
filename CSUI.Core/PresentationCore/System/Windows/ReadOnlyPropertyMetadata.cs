// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    internal class ReadOnlyPropertyMetadata : PropertyMetadata
    {
        public ReadOnlyPropertyMetadata(
            object defaultValue,
            GetReadOnlyValueCallback getValueCallback,
            PropertyChangedCallback propertyChangedCallback)
            : base(defaultValue, propertyChangedCallback)
        {
            _getValueCallback = getValueCallback;
        }

        internal override GetReadOnlyValueCallback GetReadOnlyValueCallback => _getValueCallback;

        private GetReadOnlyValueCallback _getValueCallback;
    }
}
