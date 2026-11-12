// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.ComponentModel;
using System.Globalization;

#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace System.Windows
{

    /// <summary>
    /// NullableBoolConverter - Converter class for converting instances of other types to and from bool to Nullable bool.
    /// </summary> 
    public class NullableBoolConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || sourceType == typeof(bool) || base.CanConvertFrom(context, sourceType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null)
            {
                return (bool?)null;
            }

            if (value is bool b)
            {
                return (bool?)b;
            }

            if (value is string s)
            {
                string trimmed = s.Trim();
                if (trimmed.Length == 0 || trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    return (bool?)null;
                }

                if (bool.TryParse(trimmed, out bool parsed))
                {
                    return (bool?)parsed;
                }

                throw new FormatException($"Cannot convert '{s}' to Nullable<bool>.");
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || destinationType == typeof(bool) || destinationType == typeof(bool?) || base.CanConvertTo(context, destinationType);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return value == null ? string.Empty : ((bool)value).ToString();
            }

            if (destinationType == typeof(bool) && value is bool b)
            {
                return b;
            }

            if (destinationType == typeof(bool?))
            {
                return value as bool?;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }


        /// <summary>
        /// Returns whether this object supports a standard set of values that can be
        /// picked from a list, using the specified context.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <returns>
        ///     true if GetStandardValues should be called to find a common set
        ///     of values the object supports; otherwise, false.
        /// </returns>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>
        ///     As implemented in this class, this method always returns true.
        /// If the list is exclusive, such as in an enumeration data type, then no other values are valid. If the list is not exclusive, then other valid values might exist in addition to the list of standard values that GetStandardValues provides.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Returns whether the collection of standard values returned from GetStandardValues is an exclusive list of possible values, using the specified context.
        /// </returns>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>
        /// StandardValuesCollection method override
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <returns>TypeConverter.StandardValuesCollection</returns>
        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return _standardValues ??= new TypeConverter.StandardValuesCollection(new Nullable<bool>[]
            {
                true, false, null
            });
        }

        /// <summary>
        /// Cached value for GetStandardValues
        /// </summary>
        [ThreadStatic]
        private static TypeConverter.StandardValuesCollection _standardValues;
    }
}
