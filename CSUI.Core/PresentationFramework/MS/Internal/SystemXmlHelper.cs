// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace MS.Internal
{
    /// <summary>
    /// NativeAOT trim-safe System.Xml bridge (no reflection / Assembly.Load).
    /// </summary>
    internal static class SystemXmlHelper
    {
        internal static bool IsXmlNode(object item) => false;

        internal static bool IsXmlNamespaceManager(object item) => false;

        internal static bool TryGetValueFromXmlNode(object item, string name, out object value)
        {
            value = null;
            return false;
        }

        internal static IComparer PrepareXmlComparer(IEnumerable collection, SortDescriptionCollection sort, CultureInfo culture) => null;

        internal static bool IsEmptyXmlDataCollection(object parent) => false;

        internal static string GetXmlTagName(object item, DependencyObject target) => null;

        internal static object FindXmlNodeWithInnerText(IEnumerable items, object innerText, out int index)
        {
            index = -1;
            return DependencyProperty.UnsetValue;
        }

        internal static object GetInnerText(object item) => null;
    }
}
