// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Globalization;
using System.Net.Cache;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    /// <summary>
    /// RequestCachePolicyConverter parses a RequestCachePolicy.
    /// Zero-reflection NativeAOT implementation (omits InstanceDescriptor reflection).
    /// </summary>
    public sealed class RequestCachePolicyConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            return t == typeof(string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext td, CultureInfo ci, object value)
        {
            if (value == null)
            {
                throw GetConvertFromException(value);
            }

            if (value is not string s)
            {
                throw new ArgumentException(SR.Format(SR.General_BadType, "ConvertFrom"), nameof(value));
            }

            HttpRequestCacheLevel level = Enum.Parse<HttpRequestCacheLevel>(s, true);
            return new HttpRequestCachePolicy(level);
        }

        public override object ConvertTo(
            ITypeDescriptorContext typeDescriptorContext,
            CultureInfo cultureInfo,
            object value,
            Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (value is HttpRequestCachePolicy httpPolicy && destinationType == typeof(string))
            {
                return httpPolicy.Level.ToString();
            }

            if (value is RequestCachePolicy policy && destinationType == typeof(string))
            {
                return policy.Level.ToString();
            }

            throw GetConvertToException(value, destinationType);
        }
    }
}
