// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace System.Windows.Controls
{
    [ContentProperty("Values")]
    public class AlternationConverter : IValueConverter
    {
        public AlternationConverter()
        {
        }

        public IList Values
        {
            get { return _values; }
        }

        public object Convert(object o, Type targetType, object parameter, CultureInfo culture)
        {
            if (_values.Count > 0 && o is int index)
            {
                index = index % _values.Count;
                if (index < 0)
                {
                    index += _values.Count;
                }
                return _values[index];
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object o, Type targetType, object parameter, CultureInfo culture)
        {
            return _values.IndexOf(o);
        }

        private readonly List<object> _values = new List<object>();
    }
}
