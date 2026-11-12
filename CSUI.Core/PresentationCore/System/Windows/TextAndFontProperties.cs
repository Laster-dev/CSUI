// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    #region Text formatting properties

    /// <summary>
    /// Enum specifying where a box should be positioned Vertically
    /// </summary>
    public enum BaselineAlignment
    {
        /// <summary>Align top toward top of container</summary>
        Top,

        /// <summary>Center vertically</summary>
        Center,

        /// <summary>Align bottom toward bottom of container</summary>
        Bottom,

        /// <summary>Align at baseline</summary>
        Baseline,

        /// <summary>Align toward text's top of container</summary>
        TextTop,

        /// <summary>Align toward text's bottom of container</summary>
        TextBottom,

        /// <summary>Align baseline to subscript position of container</summary>
        Subscript,

        /// <summary>Align baseline to superscript position of container</summary>
        Superscript,
    }

    /// <summary>
    /// This property describes how content of a block is aligned.
    /// </summary>
    public enum TextAlignment
    {
        /// <summary>
        /// In horizontal inline progression, the text is aligned on the left.
        /// </summary>
        Left,

        /// <summary>
        /// In horizontal inline progression, the text is aligned on the right.
        /// </summary>
        Right,

        /// <summary>
        /// The text is center aligned.
        /// </summary>
        Center,

        /// <summary>
        /// The text is justified.
        /// </summary>
        Justify,
    }

    /// <summary>
    /// The 'flow-direction' property specifies whether the primary text advance
    /// direction shall be left-to-right or right-to-left.
    /// </summary>
    public enum FlowDirection
    {
        LeftToRight,
        RightToLeft,
    }

    /// <summary>
    /// Breaking condition around inline object
    /// </summary>
    public enum LineBreakCondition
    {
        /// <summary>
        /// Break if not prohibited by other
        /// </summary>
        BreakDesired,

        /// <summary>
        /// Break if allowed by other
        /// </summary>
        BreakPossible,

        /// <summary>
        /// Break prohibited always
        /// </summary>
        BreakRestrained,

        /// <summary>
        /// Break allowed always
        /// </summary>
        BreakAlways
    }

    /// <summary>
    /// This property determines the appearance of the list item's marker
    /// </summary>
    public enum TextMarkerStyle
    {
        None,
        Disc,
        Circle,
        Square,
        Box,
        LowerRoman,
        UpperRoman,
        LowerLatin,
        UpperLatin,
        Decimal,
    }

    /// <summary>
    /// This property controls whether or not text wraps when it reaches the edge
    /// of its containing box
    /// </summary>
    public enum TextWrapping
    {
        WrapWithOverflow,
        NoWrap,
        Wrap,
    }

    /// <summary>
    /// This property determines how text is trimmed when it overflows the edge of its
    /// containing box.
    /// </summary>
    public enum TextTrimming
    {
        None,
        CharacterEllipsis,
        WordEllipsis,
    }

    #endregion

    #region Typography properties

    public enum FontVariants
    {
        Normal,
        Superscript,
        Subscript,
        Ordinal,
        Inferior,
        Ruby
    }

    public enum FontCapitals
    {
        Normal,
        AllSmallCaps,
        SmallCaps,
        AllPetiteCaps,
        PetiteCaps,
        Unicase,
        Titling
    }

    public enum FontFraction
    {
        Normal,
        Slashed,
        Stacked
    }

    public enum FontNumeralStyle
    {
        Normal,
        Lining,
        OldStyle
    }

    public enum FontNumeralAlignment
    {
        Normal,
        Proportional,
        Tabular
    }

    public enum FontEastAsianWidths
    {
        Normal,
        Proportional,
        Full,
        Half,
        Third,
        Quarter
    }

    public enum FontEastAsianLanguage
    {
        Normal,
        Jis78,
        Jis83,
        Jis90,
        Jis04,
        HojoKanji,
        NlcKanji,
        Simplified,
        Traditional,
        TraditionalNames
    }

    #endregion
}
