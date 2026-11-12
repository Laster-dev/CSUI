// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;
using System.Windows.Data;
using MS.Internal;

namespace System.Windows.Controls
{
    public sealed class MenuScrollingVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Type doubleType = typeof(double);

            if (parameter == null ||
                values == null ||
                values.Length != 4 ||
                values[0] == null ||
                values[1] == null ||
                values[2] == null ||
                values[3] == null ||
                !typeof(Visibility).IsAssignableFrom(values[0].GetType()) ||
                !doubleType.IsAssignableFrom(values[1].GetType()) ||
                !doubleType.IsAssignableFrom(values[2].GetType()) ||
                !doubleType.IsAssignableFrom(values[3].GetType()))
            {
                return DependencyProperty.UnsetValue;
            }

            Type paramType = parameter.GetType();
            if (!(doubleType.IsAssignableFrom(paramType) || typeof(string).IsAssignableFrom(paramType)))
            {
                return DependencyProperty.UnsetValue;
            }

            Visibility computedVerticalScrollBarVisibility = (Visibility)values[0];
            if (computedVerticalScrollBarVisibility == Visibility.Visible)
            {
                double target;
                if (parameter is string paramStr)
                {
                    target = double.Parse(paramStr, NumberFormatInfo.InvariantInfo);
                }
                else
                {
                    target = (double)parameter;
                }

                double verticalOffset = (double)values[1];
                double extentHeight = (double)values[2];
                double viewportHeight = (double)values[3];

                if (extentHeight != viewportHeight)
                {
                    double percent = Math.Min(100.0, Math.Max(0.0, (verticalOffset * 100.0 / (extentHeight - viewportHeight))));
                    if (DoubleUtil.AreClose(percent, target))
                    {
                        return Visibility.Collapsed;
                    }
                }

                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing };
        }
    }
}
