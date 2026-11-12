// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Windows;
using MS.Internal.Media;

namespace System.Windows.Media
{
    /// <summary>
    /// Provides attached properties that affect how text is formatted, rendered, and animated on an element.
    /// </summary>
    public static class TextOptions
    {
        internal static bool IsTextFormattingModeValid(object valueObject)
        {
            TextFormattingMode value = (TextFormattingMode)valueObject;
            return (value == TextFormattingMode.Ideal) ||
                   (value == TextFormattingMode.Display);
        }

        /// <summary>
        /// Identifies the TextFormattingMode attached dependency property.
        /// </summary>
        public static readonly DependencyProperty TextFormattingModeProperty =
            DependencyProperty.RegisterAttached(
                "TextFormattingMode",
                typeof(TextFormattingMode),
                typeof(TextOptions),
                new UIPropertyMetadata(TextFormattingMode.Ideal),
                new ValidateValueCallback(IsTextFormattingModeValid));

        /// <summary>
        /// Identifies the TextRenderingMode attached dependency property.
        /// </summary>
        public static readonly DependencyProperty TextRenderingModeProperty =
            DependencyProperty.RegisterAttached(
                "TextRenderingMode",
                typeof(TextRenderingMode),
                typeof(TextOptions),
                new UIPropertyMetadata(TextRenderingMode.Auto),
                new ValidateValueCallback(ValidateEnums.IsTextRenderingModeValid));

        /// <summary>
        /// Identifies the TextHintingMode attached dependency property.
        /// </summary>
        public static readonly DependencyProperty TextHintingModeProperty =
            TextOptionsInternal.TextHintingModeProperty.AddOwner(typeof(TextOptions));

        /// <summary>
        /// Sets the TextFormattingMode attached property for the specified DependencyObject.
        /// </summary>
        public static void SetTextFormattingMode(DependencyObject element, TextFormattingMode value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(TextFormattingModeProperty, value);
        }

        /// <summary>
        /// Gets the TextFormattingMode attached property for the specified DependencyObject.
        /// </summary>
        public static TextFormattingMode GetTextFormattingMode(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (TextFormattingMode)element.GetValue(TextFormattingModeProperty);
        }

        /// <summary>
        /// Sets the TextRenderingMode attached property for the specified DependencyObject.
        /// </summary>
        public static void SetTextRenderingMode(DependencyObject element, TextRenderingMode value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(TextRenderingModeProperty, value);
        }

        /// <summary>
        /// Gets the TextRenderingMode attached property for the specified DependencyObject.
        /// </summary>
        public static TextRenderingMode GetTextRenderingMode(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (TextRenderingMode)element.GetValue(TextRenderingModeProperty);
        }

        /// <summary>
        /// Sets the TextHintingMode attached property for the specified DependencyObject.
        /// </summary>
        public static void SetTextHintingMode(DependencyObject element, TextHintingMode value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(TextHintingModeProperty, value);
        }

        /// <summary>
        /// Gets the TextHintingMode attached property for the specified DependencyObject.
        /// </summary>
        public static TextHintingMode GetTextHintingMode(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (TextHintingMode)element.GetValue(TextHintingModeProperty);
        }
    }
}
