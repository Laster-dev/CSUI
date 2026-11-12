// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Globalization;

namespace System.Windows.Controls
{
    public class RowDefinitionCollectionConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object value)
        {
            if (value is string stringValue)
            {
                string[] parts = stringValue.Split(',');
                var definitions = new RowDefinitionCollection();
                foreach (string part in parts)
                {
                    definitions.Add(new RowDefinition(GridLength.Parse(part.Trim())));
                }
                return definitions;
            }

            throw GetConvertFromException(value);
        }

        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (destinationType == typeof(string) && value is RowDefinitionCollection definitions)
            {
                if (definitions.Count == 0)
                {
                    return string.Empty;
                }

                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < definitions.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(GridLengthConverter.ToString(definitions[i].Height, cultureInfo));
                }
                return sb.ToString();
            }

            throw GetConvertToException(value, destinationType);
        }
    }
}
