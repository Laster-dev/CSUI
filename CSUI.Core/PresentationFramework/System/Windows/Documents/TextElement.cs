// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: Base class for content in text based FrameworkElement.
//

using System.Collections;
using System.ComponentModel;
using System.Windows.Markup;
using System.Windows.Media;
using MS.Internal;
using MS.Internal.PresentationFramework;
using MS.Internal.Text;

#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace System.Windows.Documents
{
    /// <summary>
    /// TextElement is an  base class for content in text based FrameworkElement
    /// controls such as Text, FlowDocument, or RichTextBox.  TextElements span
    /// other content, applying property values or providing structural information.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public abstract class TextElement : FrameworkContentElement, IAddChild
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        static TextElement()
        {
            // For attached properties metadata specific to the type needs to be set using OverrideMetadata
            // instead of passing it during property registration. Otherwise all types will get it.
            PropertyChangedCallback typographyChanged = new PropertyChangedCallback(OnTypographyChanged);

            // Registering typography properties metadata
            DependencyProperty[] typographyProperties = Typography.TypographyPropertiesList;
            for (int i = 0; i < typographyProperties.Length; i++)
            {
                typographyProperties[i].OverrideMetadata(typeof(TextElement), new FrameworkPropertyMetadata(typographyChanged));
            }
        }

        /// <summary>
        /// Internal constructor to prevent publicly derived classes.
        /// </summary>
        internal TextElement() : base()
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Dependency Properties

        #region DependencyProperties

        /// <summary>
        /// DependencyProperty for <see cref="FontFamily" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontFamilyProperty =
                DependencyProperty.RegisterAttached(
                        "FontFamily",
                        typeof(FontFamily),
                        typeof(TextElement),
                        new FrameworkPropertyMetadata(
                                SystemFonts.MessageFontFamily,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits),
                        new ValidateValueCallback(IsValidFontFamily));

        /// <summary>
        /// The FontFamily property specifies the name of font family.
        /// </summary>
        [Localizability(
            LocalizationCategory.Font,
            Modifiability = Modifiability.Unmodifiable
        )]
        public FontFamily FontFamily
        {
            get { return (FontFamily) GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="FontFamily" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontFamily(DependencyObject element, FontFamily value)
        {
            ArgumentNullException.ThrowIfNull(element);

            element.SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="FontFamily" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static FontFamily GetFontFamily(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);

            return (FontFamily)element.GetValue(FontFamilyProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontStyle" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontStyleProperty =
                DependencyProperty.RegisterAttached(
                        "FontStyle",
                        typeof(FontStyle),
                        typeof(TextElement),
                        new FrameworkPropertyMetadata(
                                SystemFonts.MessageFontStyle,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// The FontStyle property requests normal, italic, and oblique faces within a font family.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return (FontStyle) GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="FontStyle" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontStyle(DependencyObject element, FontStyle value)
        {
            ArgumentNullException.ThrowIfNull(element);

            element.SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="FontStyle" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static FontStyle GetFontStyle(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);

            return (FontStyle)element.GetValue(FontStyleProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontWeight" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontWeightProperty =
                DependencyProperty.RegisterAttached(
                        "FontWeight",
                        typeof(FontWeight),
                        typeof(TextElement),
                        new FrameworkPropertyMetadata(
                                SystemFonts.MessageFontWeight,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// The FontWeight property specifies the weight of the font.
        /// </summary>
        public FontWeight FontWeight
        {
            get { return (FontWeight) GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="FontWeight" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontWeight(DependencyObject element, FontWeight value)
        {
            ArgumentNullException.ThrowIfNull(element);

            element.SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="FontWeight" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static FontWeight GetFontWeight(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);

            return (FontWeight)element.GetValue(FontWeightProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontStretch" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontStretchProperty =
                DependencyProperty.RegisterAttached(
                        "FontStretch",
                        typeof(FontStretch),
                        typeof(TextElement),
                        new FrameworkPropertyMetadata(
                                FontStretches.Normal,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// The FontStretch property selects a normal, condensed, or extended face from a font family.
        /// </summary>
        public FontStretch FontStretch
        {
            get { return (FontStretch) GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="FontStretch" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontStretch(DependencyObject element, FontStretch value)
        {
            ArgumentNullException.ThrowIfNull(element);

            element.SetValue(FontStretchProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="FontStretch" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static FontStretch GetFontStretch(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);

            return (FontStretch)element.GetValue(FontStretchProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontSize" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontSizeProperty =
                DependencyProperty.RegisterAttached(
                        "FontSize",
                        typeof(double),
                        typeof(TextElement),
                        new FrameworkPropertyMetadata(
                                SystemFonts.ThemeMessageFontSize,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits),
                        new ValidateValueCallback(IsValidFontSize));

        /// <summary>
        /// The FontSize property specifies the size of the font.
        /// </summary>
        [Localizability(LocalizationCategory.None)]
        public double FontSize
        {
            get { return (double) GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="FontSize" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetFontSize(DependencyObject element, double value)
        {
            ArgumentNullException.ThrowIfNull(element);

            element.SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="FontSize" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static double GetFontSize(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);

            return (double)element.GetValue(FontSizeProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="Foreground" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ForegroundProperty =
                DependencyProperty.RegisterAttached(
                        "Foreground",
                        typeof(Brush),
                        typeof(TextElement),
                        new FrameworkPropertyMetadata(
                                Brushes.Black,
                                FrameworkPropertyMetadataOptions.AffectsRender |
                                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                                FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// The Foreground property specifies the foreground brush of an element's text content.
        /// </summary>
        public Brush Foreground
        {
            get { return (Brush) GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="Foreground" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetForeground(DependencyObject element, Brush value)
        {
            ArgumentNullException.ThrowIfNull(element);

            element.SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="Foreground" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static Brush GetForeground(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);

            return (Brush)element.GetValue(ForegroundProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="Background" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty BackgroundProperty =
                DependencyProperty.Register("Background",
                        typeof(Brush),
                        typeof(TextElement),
                        new FrameworkPropertyMetadata((Brush)null,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The Background property defines the brush used to fill the content area.
        /// </summary>
        public Brush Background
        {
            get { return (Brush) GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextEffectCollection" /> property.
        /// It doesn't affect layout
        /// </summary>
        public static readonly DependencyProperty TextEffectsProperty =
                DependencyProperty.Register(
                        "TextEffects",
                        typeof(TextEffectCollection),
                        typeof(TextElement),
                        new FrameworkPropertyMetadata(
                                new FreezableDefaultValueFactory(TextEffectCollection.Empty),
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The TextEffects property specifies effects that are added to the text of an element.
        /// </summary>
        public TextEffectCollection TextEffects
        {
            get { return (TextEffectCollection) GetValue(TextEffectsProperty); }
            set { SetValue(TextEffectsProperty, value); }
        }

        /// <summary>
        /// Class providing access to all text typography properties
        /// </summary>
        public Typography Typography
        {
            get
            {
                return new Typography(this);
            }
        }

        #endregion DependencyProperties

        #region IAddChild

        void IAddChild.AddChild(object value)
        {
            throw new NotSupportedException();
        }

        void IAddChild.AddText(string text)
        {
            throw new NotSupportedException();
        }

        #endregion IAddChild

        #region LogicalTree

        /// <summary>
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                return MS.Internal.Controls.EmptyEnumerator.Instance;
            }
        }

        #endregion LogicalTree

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Notification that a specified property has been invalidated
        /// </summary>
        /// <param name="e">EventArgs that contains the property, metadata, old value, and new value for this change</param>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            // Always call base.OnPropertyChanged, otherwise Property Engine will not work.
            base.OnPropertyChanged(e);
        }

        // Notify listeners that a typographic property has changed value on this TextElement.
        internal void NotifyTypographicPropertyChanged(bool affectsMeasureOrArrange, bool localValueChanged, DependencyProperty property)
        {
        }

        #endregion Protected Events

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static TypographyProperties GetTypographyProperties(DependencyObject element)
        {
            TypographyProperties group = new TypographyProperties();

            group.SetStandardLigatures((bool) element.GetValue(Typography.StandardLigaturesProperty));
            group.SetContextualLigatures((bool) element.GetValue(Typography.ContextualLigaturesProperty));
            group.SetDiscretionaryLigatures((bool) element.GetValue(Typography.DiscretionaryLigaturesProperty));
            group.SetHistoricalLigatures((bool) element.GetValue(Typography.HistoricalLigaturesProperty));
            group.SetAnnotationAlternates((int) element.GetValue(Typography.AnnotationAlternatesProperty));
            group.SetContextualAlternates((bool) element.GetValue(Typography.ContextualAlternatesProperty));
            group.SetHistoricalForms((bool) element.GetValue(Typography.HistoricalFormsProperty));
            group.SetKerning((bool) element.GetValue(Typography.KerningProperty));
            group.SetCapitalSpacing((bool) element.GetValue(Typography.CapitalSpacingProperty));
            group.SetCaseSensitiveForms((bool) element.GetValue(Typography.CaseSensitiveFormsProperty));
            group.SetStylisticSet1((bool) element.GetValue(Typography.StylisticSet1Property));
            group.SetStylisticSet2((bool) element.GetValue(Typography.StylisticSet2Property));
            group.SetStylisticSet3((bool) element.GetValue(Typography.StylisticSet3Property));
            group.SetStylisticSet4((bool) element.GetValue(Typography.StylisticSet4Property));
            group.SetStylisticSet5((bool) element.GetValue(Typography.StylisticSet5Property));
            group.SetStylisticSet6((bool) element.GetValue(Typography.StylisticSet6Property));
            group.SetStylisticSet7((bool) element.GetValue(Typography.StylisticSet7Property));
            group.SetStylisticSet8((bool) element.GetValue(Typography.StylisticSet8Property));
            group.SetStylisticSet9((bool) element.GetValue(Typography.StylisticSet9Property));
            group.SetStylisticSet10((bool) element.GetValue(Typography.StylisticSet10Property));
            group.SetStylisticSet11((bool) element.GetValue(Typography.StylisticSet11Property));
            group.SetStylisticSet12((bool) element.GetValue(Typography.StylisticSet12Property));
            group.SetStylisticSet13((bool) element.GetValue(Typography.StylisticSet13Property));
            group.SetStylisticSet14((bool) element.GetValue(Typography.StylisticSet14Property));
            group.SetStylisticSet15((bool) element.GetValue(Typography.StylisticSet15Property));
            group.SetStylisticSet16((bool) element.GetValue(Typography.StylisticSet16Property));
            group.SetStylisticSet17((bool) element.GetValue(Typography.StylisticSet17Property));
            group.SetStylisticSet18((bool) element.GetValue(Typography.StylisticSet18Property));
            group.SetStylisticSet19((bool) element.GetValue(Typography.StylisticSet19Property));
            group.SetStylisticSet20((bool) element.GetValue(Typography.StylisticSet20Property));
            group.SetFraction((FontFraction) element.GetValue(Typography.FractionProperty));
            group.SetSlashedZero((bool) element.GetValue(Typography.SlashedZeroProperty));
            group.SetMathematicalGreek((bool) element.GetValue(Typography.MathematicalGreekProperty));
            group.SetEastAsianExpertForms((bool) element.GetValue(Typography.EastAsianExpertFormsProperty));
            group.SetVariants((FontVariants) element.GetValue(Typography.VariantsProperty));
            group.SetCapitals((FontCapitals) element.GetValue(Typography.CapitalsProperty));
            group.SetNumeralStyle((FontNumeralStyle) element.GetValue(Typography.NumeralStyleProperty));
            group.SetNumeralAlignment((FontNumeralAlignment) element.GetValue(Typography.NumeralAlignmentProperty));
            group.SetEastAsianWidths((FontEastAsianWidths) element.GetValue(Typography.EastAsianWidthsProperty));
            group.SetEastAsianLanguage((FontEastAsianLanguage) element.GetValue(Typography.EastAsianLanguageProperty));
            group.SetStandardSwashes((int) element.GetValue(Typography.StandardSwashesProperty));
            group.SetContextualSwashes((int) element.GetValue(Typography.ContextualSwashesProperty));
            group.SetStylisticAlternates((int) element.GetValue(Typography.StylisticAlternatesProperty));

            return group;
        }

        // ........................................................................
        //
        // Helpers for Text Flow Initialization
        //
        // ........................................................................

        // Returns the common TextElement ancestor of two TextElements.
        internal static TextElement GetCommonAncestor(TextElement element1, TextElement element2)
        {
            if (element1 != element2)
            {
                int depth1 = 0;
                int depth2 = 0;
                TextElement element;

                // Calculate the depths of each TextElement within the tree.
                for (element = element1; element.Parent is TextElement; element = (TextElement)element.Parent)
                {
                    depth1++;
                }
                for (element = element2; element.Parent is TextElement; element = (TextElement)element.Parent)
                {
                    depth2++;
                }

                // Then walk up until we reach an equal depth.

                while (depth1 > depth2 && element1 != element2)
                {
                    element1 = (TextElement)element1.Parent;
                    depth1--;
                }

                while (depth2 > depth1 && element1 != element2)
                {
                    element2 = (TextElement)element2.Parent;
                    depth2--;
                }

                // Then, if necessary, keep going up to the root looking for a match.
                while (element1 != element2)
                {
                    element1 = element1.Parent as TextElement;
                    element2 = element2.Parent as TextElement;
                }
            }

            Invariant.Assert(element1 == element2);
            return element1;
        }


        /// <summary>
        /// Derived classes override this method to get notified when a TextContainer
        /// change affects the text parented by this element.
        /// </summary>
        /// <remarks>
        /// If this TextElement is a Run, this function will be called whenever the text content 
        /// under this Run changes. If this TextElement is not a Run, this function will be called 
        /// most of the time its content changes, but not necessarily always.
        /// </remarks>
        internal virtual void OnTextUpdated()
        {
        }

        /// <summary>
        /// Derived classes override this method to get notified before TextContainer
        /// causes a logical tree change that affects this element.
        /// </summary>
        internal virtual void BeforeLogicalTreeChange()
        {
        }

        /// <summary>
        /// Derived classes override this method to get notified after TextContainer
        /// causes a logical tree change that affects this element.
        /// </summary>
        internal virtual void AfterLogicalTreeChange()
        {
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        //-------------------------------------------------------------------
        // Typography properties group
        //-------------------------------------------------------------------
        internal TypographyProperties TypographyPropertiesGroup
        {
            get
            {
                if (_typographyPropertiesGroup == null)
                {
                    _typographyPropertiesGroup = GetTypographyProperties(this);
                }
                return _typographyPropertiesGroup;
            }
        }

        private static void OnTypographyChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            ((TextElement) element)._typographyPropertiesGroup = null;
        }

        internal virtual bool IsIMEStructuralElement
        {
            get
            {
                return false;
            }
        }

        internal virtual bool IsFirstIMEVisibleSibling
        {
            get
            {
                return this.IsIMEStructuralElement;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private static bool IsValidFontFamily(object o)
        {
            FontFamily value = o as FontFamily;
            return (value != null);
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        private static bool IsValidFontSize(object value)
        {
            double fontSize = (double) value;
            double minFontSize = TextDpi.MinWidth;
            double maxFontSize = Math.Min(1000000, TextDpi.MaxWidth);

            if (Double.IsNaN(fontSize))
            {
                return false;
            }
            if (fontSize < minFontSize)
            {
                return false;
            }
            if (fontSize > maxFontSize)
            {
                return false;
            }
            return true;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        //-------------------------------------------------------------------
        // Typography Group Property
        //-------------------------------------------------------------------
        private TypographyProperties _typographyPropertiesGroup = Typography.Default;

        #endregion Private Fields
    }
}
