// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows
{
    public class UIPropertyMetadata : PropertyMetadata
    {
        public UIPropertyMetadata()
            : base()
        {
        }

        public UIPropertyMetadata(object defaultValue)
            : base(defaultValue)
        {
        }

        public UIPropertyMetadata(PropertyChangedCallback propertyChangedCallback)
            : base(propertyChangedCallback)
        {
        }

        public UIPropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback)
            : base(defaultValue, propertyChangedCallback)
        {
        }

        public UIPropertyMetadata(
            object defaultValue,
            PropertyChangedCallback propertyChangedCallback,
            CoerceValueCallback coerceValueCallback)
            : base(defaultValue, propertyChangedCallback, coerceValueCallback)
        {
        }

        public UIPropertyMetadata(
            object defaultValue,
            PropertyChangedCallback propertyChangedCallback,
            CoerceValueCallback coerceValueCallback,
            bool isAnimationProhibited)
            : base(defaultValue, propertyChangedCallback, coerceValueCallback)
        {
            WriteFlag(MetadataFlags.UI_IsAnimationProhibitedID, isAnimationProhibited);
        }

        internal override PropertyMetadata CreateInstance() => new UIPropertyMetadata();

        public bool IsAnimationProhibited
        {
            get { return ReadFlag(MetadataFlags.UI_IsAnimationProhibitedID); }
            set
            {
                if (Sealed)
                {
                    throw new InvalidOperationException(SR.TypeMetadataCannotChangeAfterUse);
                }

                WriteFlag(MetadataFlags.UI_IsAnimationProhibitedID, value);
            }
        }
    }
}
