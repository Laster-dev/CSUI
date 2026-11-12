// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace System.Windows.Controls
{
    public class BorderGapMaskConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Type doubleType = typeof(double);

            if (parameter == null ||
                values == null ||
                values.Length != 3 ||
                values[0] == null ||
                values[1] == null ||
                values[2] == null ||
                !doubleType.IsAssignableFrom(values[0].GetType()) ||
                !doubleType.IsAssignableFrom(values[1].GetType()) ||
                !doubleType.IsAssignableFrom(values[2].GetType()))
            {
                return DependencyProperty.UnsetValue;
            }

            Type paramType = parameter.GetType();
            if (!(doubleType.IsAssignableFrom(paramType) || typeof(string).IsAssignableFrom(paramType)))
            {
                return DependencyProperty.UnsetValue;
            }

            double headerWidth = (double)values[0];
            double borderWidth = (double)values[1];
            double borderHeight = (double)values[2];

            if (borderWidth == 0 || borderHeight == 0)
            {
                return null;
            }

            double linePosition;
            if (parameter is string paramStr)
            {
                linePosition = double.Parse(paramStr, NumberFormatInfo.InvariantInfo);
            }
            else
            {
                linePosition = (double)parameter;
            }

            Grid myGrid = new Grid();
            myGrid.Width = borderWidth;
            myGrid.Height = borderHeight;
            ColumnDefinition colDef1 = new ColumnDefinition();
            ColumnDefinition colDef2 = new ColumnDefinition();
            ColumnDefinition colDef3 = new ColumnDefinition();
            colDef1.Width = new GridLength(linePosition);
            colDef2.Width = new GridLength(headerWidth);
            colDef3.Width = new GridLength(1, GridUnitType.Star);
            myGrid.ColumnDefinitions.Add(colDef1);
            myGrid.ColumnDefinitions.Add(colDef2);
            myGrid.ColumnDefinitions.Add(colDef3);
            RowDefinition rowDef1 = new RowDefinition();
            RowDefinition rowDef2 = new RowDefinition();
            rowDef1.Height = new GridLength(borderHeight / 2);
            rowDef2.Height = new GridLength(1, GridUnitType.Star);
            myGrid.RowDefinitions.Add(rowDef1);
            myGrid.RowDefinitions.Add(rowDef2);

            Rectangle rect1 = new Rectangle();
            Rectangle rect2 = new Rectangle();
            Rectangle rect3 = new Rectangle();
            rect1.Fill = Brushes.Black;
            rect2.Fill = Brushes.Black;
            rect3.Fill = Brushes.Black;

            Grid.SetRowSpan(rect1, 2);
            Grid.SetRow(rect1, 0);
            Grid.SetColumn(rect1, 0);

            Grid.SetRow(rect2, 1);
            Grid.SetColumn(rect2, 1);

            Grid.SetRowSpan(rect3, 2);
            Grid.SetRow(rect3, 0);
            Grid.SetColumn(rect3, 2);

            myGrid.Children.Add(rect1);
            myGrid.Children.Add(rect2);
            myGrid.Children.Add(rect3);

            return new VisualBrush(myGrid);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing };
        }
    }
}
