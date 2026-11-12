// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Globalization;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// Provides a type converter to convert from <see cref="PixelFormat"/> to <see cref="string"/> and vice versa.
    /// Zero-reflection NativeAOT implementation.
    /// </summary>
    public sealed class PixelFormatConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            return t == typeof(string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public new object ConvertFromString(string value)
        {
            return value is not null ? new PixelFormat(value) : null;
        }

        public override object ConvertFrom(ITypeDescriptorContext td, CultureInfo ci, object o)
        {
            return o is not null ? new PixelFormat(o as string) : null;
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);
            ArgumentNullException.ThrowIfNull(value);

            if (value is not PixelFormat pixelFormat)
            {
                throw new ArgumentException(SR.Format(SR.General_Expected_Type, nameof(PixelFormat)));
            }

            if (destinationType == typeof(string))
            {
                return pixelFormat.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
