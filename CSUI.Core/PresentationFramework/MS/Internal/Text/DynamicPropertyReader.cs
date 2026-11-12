// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//
// Description: Helper methods to retrive dynami properties from
//              DependencyObjects.
//


using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup;

#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace MS.Internal.Text
{
    // ----------------------------------------------------------------------
    // Helper methods to retrive dynami properties from DependencyObjects.
    // ----------------------------------------------------------------------
    internal static class DynamicPropertyReader
    {
        // ------------------------------------------------------------------
        //
        // Property Groups
        //
        // ------------------------------------------------------------------

        #region Property Groups

        // ------------------------------------------------------------------
        // Retrieve typeface properties from specified element.
        // ------------------------------------------------------------------
        internal static Typeface GetTypeface(DependencyObject element)
        {
            Debug.Assert(element != null);

            FontFamily  fontFamily  = (FontFamily)  element.GetValue(TextElement.FontFamilyProperty);
            FontStyle   fontStyle   = (FontStyle)   element.GetValue(TextElement.FontStyleProperty);
            FontWeight  fontWeight  = (FontWeight)  element.GetValue(TextElement.FontWeightProperty);
            FontStretch fontStretch = (FontStretch) element.GetValue(TextElement.FontStretchProperty);

            return new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
        }

        internal static Typeface GetModifiedTypeface(DependencyObject element, FontFamily fontFamily)
        {
            Debug.Assert(element != null);

            FontStyle   fontStyle   = (FontStyle)   element.GetValue(TextElement.FontStyleProperty);
            FontWeight  fontWeight  = (FontWeight)  element.GetValue(TextElement.FontWeightProperty);
            FontStretch fontStretch = (FontStretch) element.GetValue(TextElement.FontStretchProperty);

            return new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
        }

        // ------------------------------------------------------------------
        // Retrieve text properties from specified inline object.
        //
        // WORKAROUND: see PS task #13486 & #3399.
        // For inline object go to its parent and retrieve text decoration
        // properties from there.
        // ------------------------------------------------------------------
        internal static TextDecorationCollection GetTextDecorationsForInlineObject(DependencyObject element, TextDecorationCollection textDecorations)
        {
            Debug.Assert(element != null);

            DependencyObject parent = LogicalTreeHelper.GetParent(element);
            TextDecorationCollection parentTextDecorations = null;

            if (parent != null)
            {
                // Get parent text decorations if it is non-null
                parentTextDecorations = GetTextDecorations(parent);
            }

            // see if the two text decorations are equal.
            bool textDecorationsEqual = (textDecorations == null) ?
                                         parentTextDecorations == null
                                       : textDecorations.ValueEquals(parentTextDecorations);

            if (!textDecorationsEqual)
            {
                if (parentTextDecorations == null)
                {
                    textDecorations = null;
                }
                else
                {
                    textDecorations = new TextDecorationCollection();
                    int count = parentTextDecorations.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        textDecorations.Add(parentTextDecorations[i]);
                    }
                }
            }
            return textDecorations;
        }

        /// <summary>
        /// Helper method to get a TextDecorations property value. It returns null (instead of empty collection)
        /// when the property is not set on the given DO. 
        /// </summary>
        internal static TextDecorationCollection GetTextDecorations(DependencyObject element)
        {
            DependencyProperty dp = MS.Internal.Controls.ControlFoundationHooks.InlineTextDecorationsProperty;
            return dp != null ? GetCollectionValue(element, dp) as TextDecorationCollection : null;
        }

        /// <summary>
        /// Helper method to get a TextEffects property value. It returns null (instead of empty collection)
        /// when the property is not set on the given DO. 
        /// </summary>
        internal static TextEffectCollection GetTextEffects(DependencyObject element)
        {
            return GetCollectionValue(element, TextElement.TextEffectsProperty) as TextEffectCollection;
        }

        /// <summary>
        /// Helper method to get a collection property value. It returns null (instead of empty collection)
        /// when the property is not set on the given DO. 
        /// </summary>
        /// <remarks>
        /// Property system's GetValue() call creates a mutable empty collection when the property is accessed for the first time. 
        /// To avoids workingset overhead of those empty collections, we return null instead.  
        /// </remarks>
        private static object GetCollectionValue(DependencyObject element, DependencyProperty property)
        {
            bool hasModifiers; 
            if (element.GetValueSource(property, null, out hasModifiers)
                != BaseValueSourceInternal.Default || hasModifiers)
            {
                return element.GetValue(property);
            }

            return null;              
        }        

        #endregion Property Groups

        // ------------------------------------------------------------------
        //
        // Block Properties
        //
        // ------------------------------------------------------------------

        #region Block Properties

        // ------------------------------------------------------------------
        // GetKeepTogether
        // ------------------------------------------------------------------
        internal static bool GetKeepTogether(DependencyObject element)
        {
            return MS.Internal.Controls.ControlFoundationHooks.GetParagraphKeepTogetherHook?.Invoke(element) ?? false;
        }

        // ------------------------------------------------------------------
        // GetKeepWithNext
        // ------------------------------------------------------------------
        internal static bool GetKeepWithNext(DependencyObject element)
        {
            return MS.Internal.Controls.ControlFoundationHooks.GetParagraphKeepWithNextHook?.Invoke(element) ?? false;
        }

        // ------------------------------------------------------------------
        // GetMinWidowLines
        // ------------------------------------------------------------------
        internal static int GetMinWidowLines(DependencyObject element)
        {
            return MS.Internal.Controls.ControlFoundationHooks.GetParagraphMinWidowLinesHook?.Invoke(element) ?? 0;
        }

        // ------------------------------------------------------------------
        // GetMinOrphanLines
        // ------------------------------------------------------------------
        internal static int GetMinOrphanLines(DependencyObject element)
        {
            return MS.Internal.Controls.ControlFoundationHooks.GetParagraphMinOrphanLinesHook?.Invoke(element) ?? 0;
        }

        #endregion Block Properties

        // ------------------------------------------------------------------
        //
        // Misc Properties
        //
        // ------------------------------------------------------------------

        #region Misc Properties


        /// <summary>
        /// Gets actual value of LineHeight property. If LineHeight is Double.Nan, returns FontSize*FontFamily.LineSpacing
        /// </summary>
        internal static double GetLineHeightValue(DependencyObject d)
        {
            double lineHeight = MS.Internal.Controls.ControlFoundationHooks.BlockLineHeightProperty != null
                ? (double)d.GetValue(MS.Internal.Controls.ControlFoundationHooks.BlockLineHeightProperty)
                : double.NaN;
            // If LineHeight value is 'Auto', treat it as LineSpacing * FontSize.
            if (double.IsNaN(lineHeight))
            {
                FontFamily fontFamily = (FontFamily)d.GetValue(TextElement.FontFamilyProperty);
                double fontSize = (double)d.GetValue(TextElement.FontSizeProperty);
                lineHeight = fontFamily.LineSpacing * fontSize;
            }
            return Math.Max(TextDpi.MinWidth, Math.Min(TextDpi.MaxWidth, lineHeight));
        }

        // ------------------------------------------------------------------
        // Retrieve background brush property from specified element.
        // If 'element' is the same object as paragraph owner, ignore background
        // brush, because it is handled outside as paragraph's background.
        // NOTE: This method is only used to read background of text content.
        //
        //      element - Element associated with content. Passed only for
        //              performance reasons; it can be extracted from 'position'.
        //      paragraphOwner - Owner of paragraph (usually the parent of 'element').
        //
        // ------------------------------------------------------------------
        internal static Brush GetBackgroundBrush(DependencyObject element)
        {
            Debug.Assert(element != null);
            Brush backgroundBrush = null;

            // If 'element' is FrameworkElement, it is the host of the text content.
            // If 'element' is Block, the content is directly hosted by a block paragraph.
            // In such cases ignore background brush, because it is handled outside as paragraph's background.
            while (backgroundBrush == null && CanApplyBackgroundBrush(element))
            {
                backgroundBrush = (Brush)element.GetValue(TextElement.BackgroundProperty);
                Invariant.Assert(element is FrameworkContentElement);
                element = ((FrameworkContentElement)element).Parent;
            }
            return backgroundBrush;
        }

        // ------------------------------------------------------------------
        // ------------------------------------------------------------------
        // GetBaselineAlignment
        // ------------------------------------------------------------------
        internal static BaselineAlignment GetBaselineAlignment(DependencyObject element)
        {
            MS.Internal.Controls.IInlineHost i = element as MS.Internal.Controls.IInlineHost;
            BaselineAlignment baselineAlignment = (i != null) ? i.BaselineAlignment : BaselineAlignment.Baseline;

            // Walk up the tree to check if it inherits BaselineAlignment from a parent
            while (i != null && BaselineAlignmentIsDefault((DependencyObject)i))
            {
                i = i.Parent as MS.Internal.Controls.IInlineHost;
            }

            if (i != null)
            {
                // Found an Inline with non-default baseline alignment
                baselineAlignment = i.BaselineAlignment;
            }
            return baselineAlignment;
        }

        // ------------------------------------------------------------------
        // GetBaselineAlignmentForInlineObject
        // ------------------------------------------------------------------
        internal static BaselineAlignment GetBaselineAlignmentForInlineObject(DependencyObject element)
        {
            return GetBaselineAlignment(LogicalTreeHelper.GetParent(element));
        }

        // ------------------------------------------------------------------
        // Retrieve CultureInfo property from specified element.
        // ------------------------------------------------------------------
        internal static CultureInfo GetCultureInfo(DependencyObject element)
        {
            XmlLanguage language = (XmlLanguage) element.GetValue(FrameworkElement.LanguageProperty);
            try
            {
                return language.GetSpecificCulture();
            }
            catch (InvalidOperationException)
            {
                // We default to en-US if no part of the language tag is recognized.
                return System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS;
            }
        }

        // ------------------------------------------------------------------
        // Retrieve Number substitution properties from given element
        // ------------------------------------------------------------------
        internal static NumberSubstitution GetNumberSubstitution(DependencyObject element)
        {
            NumberSubstitution numberSubstitution = new NumberSubstitution
            {
                CultureSource = (NumberCultureSource)element.GetValue(NumberSubstitution.CultureSourceProperty),
                CultureOverride = (CultureInfo)element.GetValue(NumberSubstitution.CultureOverrideProperty),
                Substitution = (NumberSubstitutionMethod)element.GetValue(NumberSubstitution.SubstitutionProperty)
            };

            return numberSubstitution;
        }

        private static bool CanApplyBackgroundBrush(DependencyObject element)
        {
            // If 'element' is FrameworkElement, it is the host of the text content.
            // If 'element' is Block, the content is directly hosted by a block paragraph.
            // In such cases ignore background brush, because it is handled outside as paragraph's background.
            // We will only apply background on Inline elements that are not AnchoredBlocks.
            // NOTE: We ideally do not need the AnchoredBlock check because when walking up the content tree we should hit a block before
            // an AnchoredBlock. Leaving it in in case this helper is used for other purposes.
            if (element is not MS.Internal.Controls.IInlineHost inline || inline.IsAnchoredBlock)
            {
                return false;
            }
            return true;
        }

        private static bool BaselineAlignmentIsDefault(DependencyObject element)
        {
            Invariant.Assert(element != null);

            DependencyProperty dp = MS.Internal.Controls.ControlFoundationHooks.InlineBaselineAlignmentProperty;
            if (dp == null)
            {
                return true;
            }

            bool hasModifiers;
            if (element.GetValueSource(dp, null, out hasModifiers)
                != BaseValueSourceInternal.Default || hasModifiers)
            {
                return false;
            }
            return true;
        }

        #endregion Misc Properties
    }
 }
