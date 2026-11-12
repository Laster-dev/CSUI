// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//  Contents:  Pure C# Unicode classification entry point (NativeAOT replacement
//             for PresentationNative MILGetClassificationTables).
//
//

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Media.TextFormatting;

namespace MS.Internal
{
    /// <summary>
    /// Provides character classification callbacks to MS.Internal.Text.TextInterface.
    /// </summary>
    internal class ClassificationUtility : MS.Internal.Text.TextInterface.IClassification
    {
        internal static readonly bool[] ScriptCaretInfo = new bool[]
        {
            /* Default              */    false,
            /* Arabic               */    false,
            /* Armenian             */    false,
            /* Bengali              */    true,
            /* Bopomofo             */    false,
            /* Braille              */    false,
            /* Buginese             */    true,
            /* Buhid                */    false,
            /* CanadianSyllabics    */    false,
            /* Cherokee             */    false,
            /* CJKIdeographic       */    false,
            /* Coptic               */    false,
            /* CypriotSyllabary     */    false,
            /* Cyrillic             */    false,
            /* Deseret              */    false,
            /* Devanagari           */    true,
            /* Ethiopic             */    false,
            /* Georgian             */    false,
            /* Glagolitic           */    false,
            /* Gothic               */    false,
            /* Greek                */    false,
            /* Gujarati             */    true,
            /* Gurmukhi             */    true,
            /* Hangul               */    true,
            /* Hanunoo              */    false,
            /* Hebrew               */    true,
            /* Kannada              */    true,
            /* Kana                 */    false,
            /* Kharoshthi           */    true,
            /* Khmer                */    true,
            /* Lao                  */    true,
            /* Latin                */    false,
            /* Limbu                */    true,
            /* LinearB              */    false,
            /* Malayalam            */    true,
            /* MathematicalAlphanumericSymbols */ false,
            /* Mongolian            */    true,
            /* MusicalSymbols       */    false,
            /* Myanmar              */    true,
            /* NewTaiLue            */    true,
            /* Ogham                */    false,
            /* OldItalic            */    false,
            /* OldPersianCuneiform  */    false,
            /* Oriya                */    true,
            /* Osmanya              */    false,
            /* Runic                */    false,
            /* Shavian              */    false,
            /* Sinhala              */    true,
            /* SylotiNagri          */    true,
            /* Syriac               */    false,
            /* Tagalog              */    false,
            /* Tagbanwa             */    false,
            /* TaiLe                */    false,
            /* Tamil                */    true,
            /* Telugu               */    true,
            /* Thaana               */    true,
            /* Thai                 */    true,
            /* Tibetan              */    true,
            /* Tifinagh             */    false,
            /* UgariticCuneiform    */    false,
            /* Yi                   */    false,
            /* Digit                */    false,
            /* Control              */    false,
            /* Mirror               */    false,
        };

        private static readonly ClassificationUtility _classificationUtilityInstance = new ClassificationUtility();

        internal static ClassificationUtility Instance
        {
            get { return _classificationUtilityInstance; }
        }

        public void GetCharAttribute(
            int unicodeScalar,
            out bool isCombining,
            out bool needsCaretInfo,
            out bool isIndic,
            out bool isDigit,
            out bool isLatin,
            out bool isStrong)
        {
            CharacterAttribute charAttribute = Classification.CharAttributeOf((int)Classification.GetUnicodeClass(unicodeScalar));

            byte itemClass = charAttribute.ItemClass;
            isCombining = (itemClass == (byte)ItemClass.SimpleMarkClass
                        || itemClass == (byte)ItemClass.ComplexMarkClass
                        || Classification.IsIVS(unicodeScalar));

            isStrong = (itemClass == (byte)ItemClass.StrongClass);

            int script = charAttribute.Script;
            needsCaretInfo = ScriptCaretInfo[script];

            ScriptID scriptId = (ScriptID)script;
            isDigit = scriptId == ScriptID.Digit;
            isLatin = scriptId == ScriptID.Latin;
            isIndic = !isLatin && IsScriptIndic(scriptId);
        }

        /// <summary>
        /// Returns true if specified script is Indic.
        /// </summary>
        private static bool IsScriptIndic(ScriptID scriptId)
        {
            return scriptId == ScriptID.Bengali
                || scriptId == ScriptID.Devanagari
                || scriptId == ScriptID.Gurmukhi
                || scriptId == ScriptID.Gujarati
                || scriptId == ScriptID.Kannada
                || scriptId == ScriptID.Malayalam
                || scriptId == ScriptID.Oriya
                || scriptId == ScriptID.Tamil
                || scriptId == ScriptID.Telugu;
        }
    }

    /// <summary>
    /// Pure C# Unicode character classification tables and helpers.
    /// </summary>
    internal static class Classification
    {
        private const int HashTableSize = 1024; // Power of 2, > 2 * UnicodeClass.Max (472)

        private static readonly CharacterAttribute[] _charAttributes = new CharacterAttribute[(int)UnicodeClass.Max];
        private static readonly short[] _latin1Classes = new short[256];
        private static readonly ulong[] _hashKeys = new ulong[HashTableSize];
        private static readonly short[] _hashValues = new short[HashTableSize];
        private static int _attributeCount;

        static Classification()
        {
            Array.Fill(_hashValues, (short)-1);

            // Intern every general script Letter and Mark attribute so every script < ScriptID.Max is guaranteed present.
            for (int s = 0; s < (int)ScriptID.Max; s++)
            {
                ScriptID script = (ScriptID)s;
                Intern(BuildLetterAttribute(script));
                Intern(BuildMarkAttribute(script, isSpacing: false));
                Intern(BuildMarkAttribute(script, isSpacing: true));
            }

            // Intern all special scalar values (format anchors, bidi controls, digits, breaks, punctuation, spaces).
            int[] specialSamples = new int[]
            {
                0x0085, 0x00A0, 0x034F, 0x060C, 0x061B, 0x061C, 0x061F, 0x0660, 0x066A, 0x066B, 0x066C, 0x06F0,
                0x0966, 0x180B, 0x200B, 0x200C, 0x200D, 0x200E, 0x200F, 0x2028, 0x2029,
                0x202A, 0x202B, 0x202C, 0x202D, 0x202E, 0x2066, 0x2067, 0x2068, 0x2069,
                0x20A0, 0x2160, 0x3000, 0x3001, 0x3008, 0xFE00, 0xFEFF, 0x10140, 0x10400, 0x1D7CE, 0xE0100
            };

            for (int i = 0; i < specialSamples.Length; i++)
            {
                Intern(ComputeAttribute(specialSamples[i]));
            }

            // Intern generic neutral/symbol/punct/space/control categories.
            Intern(BuildNeutralAttribute(ItemClass.WeakClass, DirectionClass.OtherNeutral, 0, CharBreakingType.NoBreak));
            Intern(BuildNeutralAttribute(ItemClass.WeakClass, DirectionClass.OtherNeutral, (ushort)CharacterAttributeFlags.CharacterExtended, CharBreakingType.NoBreak));
            Intern(BuildNeutralAttribute(ItemClass.WeakClass, DirectionClass.EuropeanTerminator, 0, CharBreakingType.NoBreak));
            Intern(BuildNeutralAttribute(ItemClass.CSClass, DirectionClass.CommonSeparator, 0, CharBreakingType.NoBreak));
            Intern(BuildNeutralAttribute(ItemClass.ESClass, DirectionClass.EuropeanSeparator, 0, CharBreakingType.NoBreak));
            Intern(BuildNeutralAttribute(ItemClass.WeakClass, DirectionClass.Left, (ushort)CharacterAttributeFlags.CharacterExtended, CharBreakingType.NoBreak));
            Intern(BuildNeutralAttribute(ItemClass.WeakClass, DirectionClass.Right, (ushort)CharacterAttributeFlags.CharacterComplex | (ushort)CharacterAttributeFlags.CharacterRTL, CharBreakingType.NoBreak));
            Intern(BuildNeutralAttribute(ItemClass.WeakClass, DirectionClass.ArabicLetter, (ushort)CharacterAttributeFlags.CharacterComplex | (ushort)CharacterAttributeFlags.CharacterRTL, CharBreakingType.NoBreak));
            Intern(BuildNeutralAttribute(ItemClass.WeakClass, DirectionClass.WhiteSpace, (ushort)CharacterAttributeFlags.CharacterSpace, CharBreakingType.NoBreak));
            Intern(BuildNeutralAttribute(ItemClass.WeakClass, DirectionClass.WhiteSpace, (ushort)(CharacterAttributeFlags.CharacterSpace | CharacterAttributeFlags.CharacterIdeo), CharBreakingType.NoBreak));
            Intern(BuildMirrorAttribute(DirectionClass.OtherNeutral, fastText: false));
            Intern(BuildControlAttribute(DirectionClass.BoundaryNeutral, 0));

            // Pre-populate O(1) lookup for U+0000..U+00FF (covers all ASCII & Latin-1).
            for (int ch = 0; ch < 256; ch++)
            {
                _latin1Classes[ch] = Intern(ComputeAttribute(ch));
            }

            Invariant.Assert(_attributeCount < (int)UnicodeClass.Max);
        }

        /// <summary>
        /// Lookup Unicode character class for a Unicode UTF16 value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short GetUnicodeClassUTF16(char codepoint)
        {
            if (codepoint < 256)
            {
                return _latin1Classes[codepoint];
            }

            return GetUnicodeClassSlow(codepoint);
        }

        /// <summary>
        /// Lookup Unicode character class for a Unicode scalar value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short GetUnicodeClass(int unicodeScalar)
        {
            Invariant.Assert(unicodeScalar >= 0 && unicodeScalar <= 0x10FFFF);

            if ((uint)unicodeScalar < 256u)
            {
                return _latin1Classes[unicodeScalar];
            }

            return GetUnicodeClassSlow(unicodeScalar);
        }

        private static short GetUnicodeClassSlow(int unicodeScalar)
        {
            CharacterAttribute attr = ComputeAttribute(unicodeScalar);
            ulong key = PackAttribute(in attr);
            int slot = (int)(HashKey(key) & (HashTableSize - 1));

            for (int probe = 0; probe < HashTableSize; probe++)
            {
                short value = _hashValues[slot];
                if (value < 0)
                {
                    break;
                }
                if (_hashKeys[slot] == key)
                {
                    return value;
                }
                slot = (slot + 1) & (HashTableSize - 1);
            }

            // Fallback to a valid neutral entry in range [0, _attributeCount).
            return 0;
        }

        /// <summary>
        /// Lookup script ID for a Unicode scalar value
        /// </summary>
        public static ScriptID GetScript(int unicodeScalar)
        {
            return (ScriptID)_charAttributes[GetUnicodeClass(unicodeScalar)].Script;
        }

        /// <summary>
        /// Compute Unicode scalar value from unicode codepoint stream
        /// </summary>
        internal static int UnicodeScalar(
            CharacterBufferRange unicodeString,
            out int sizeofChar)
        {
            Invariant.Assert(unicodeString.CharacterBuffer != null && unicodeString.Length > 0);

            int ch = unicodeString[0];
            sizeofChar = 1;

            if (unicodeString.Length >= 2
                && (ch & 0xFC00) == 0xD800
                && (unicodeString[1] & 0xFC00) == 0xDC00)
            {
                ch = (((ch & 0x03FF) << 10) | (unicodeString[1] & 0x3FF)) + 0x10000;
                sizeofChar++;
            }

            return ch;
        }

        /// <summary>
        /// Check whether the character is combining mark
        /// </summary>
        public static bool IsCombining(int unicodeScalar)
        {
            byte itemClass = _charAttributes[GetUnicodeClass(unicodeScalar)].ItemClass;

            return itemClass == (byte)ItemClass.SimpleMarkClass
                || itemClass == (byte)ItemClass.ComplexMarkClass
                || IsIVS(unicodeScalar);
        }

        /// <summary>
        /// Check whether the character is a joiner character
        /// </summary>
        public static bool IsJoiner(int unicodeScalar)
        {
            byte itemClass = _charAttributes[GetUnicodeClass(unicodeScalar)].ItemClass;
            return itemClass == (byte)ItemClass.JoinerClass;
        }

        /// <summary>
        /// Check whether the character is an IVS selector character
        /// </summary>
        public static bool IsIVS(int unicodeScalar)
        {
            // An Ideographic Variation Sequence (IVS) variation selector spans U+E0100..U+E01EF.
            return unicodeScalar >= 0xE0100 && unicodeScalar <= 0xE01EF;
        }

        /// <summary>
        /// Scan UTF16 character string until a character with specified attributes is found
        /// </summary>
        /// <returns>character index of first character matching the attribute.</returns>
        public static int AdvanceUntilUTF16(
            CharacterBuffer charBuffer,
            int offsetToFirstChar,
            int stringLength,
            ushort mask,
            out ushort charFlags)
        {
            int i = offsetToFirstChar;
            int limit = offsetToFirstChar + stringLength;
            charFlags = 0;

            while (i < limit)
            {
                ushort flags = _charAttributes[(int)GetUnicodeClassUTF16(charBuffer[i])].Flags;

                if ((flags & mask) != 0)
                    break;

                charFlags |= flags;
                i++;
            }

            return i - offsetToFirstChar;
        }

        /// <summary>
        /// Scan character string until a character that is not the specified ItemClass is found
        /// </summary>
        /// <returns>character index of first character that is not the specified ItemClass</returns>
        public static int AdvanceWhile(
            CharacterBufferRange unicodeString,
            ItemClass itemClass)
        {
            int i = 0;
            int limit = unicodeString.Length;

            while (i < limit)
            {
                int ch = UnicodeScalar(
                    new CharacterBufferRange(unicodeString, i, limit - i),
                    out int sizeofChar);

                byte currentClass = _charAttributes[(int)GetUnicodeClass(ch)].ItemClass;
                if (currentClass != (byte)itemClass)
                    break;

                i += sizeofChar;
            }

            return i;
        }

        internal static CharacterAttribute CharAttributeOf(int charClass)
        {
            Invariant.Assert(charClass >= 0 && charClass < (int)UnicodeClass.Max);
            return _charAttributes[charClass];
        }

        private static short Intern(in CharacterAttribute attr)
        {
            ulong key = PackAttribute(in attr);
            int slot = (int)(HashKey(key) & (HashTableSize - 1));

            while (true)
            {
                short existing = _hashValues[slot];
                if (existing < 0)
                {
                    Invariant.Assert(_attributeCount < (int)UnicodeClass.Max);
                    short idx = (short)_attributeCount++;
                    _charAttributes[idx] = attr;
                    _hashKeys[slot] = key;
                    _hashValues[slot] = idx;
                    return idx;
                }

                if (_hashKeys[slot] == key)
                {
                    return existing;
                }

                slot = (slot + 1) & (HashTableSize - 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong PackAttribute(in CharacterAttribute attr)
        {
            return (ulong)attr.Script
                | ((ulong)attr.ItemClass << 8)
                | ((ulong)attr.Flags << 16)
                | ((ulong)attr.BreakType << 32)
                | ((ulong)attr.BiDi << 40)
                | ((ulong)(ushort)attr.LineBreak << 48);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint HashKey(ulong key)
        {
            key ^= key >> 33;
            key *= 0xff51afd7ed558ccdUL;
            key ^= key >> 33;
            return (uint)key;
        }

        private static CharacterAttribute ComputeAttribute(int ch)
        {
            // Explicit control, line/paragraph break, bidi format, and joiner characters
            switch (ch)
            {
                case '\r':
                case '\n':
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Control,
                        ItemClass = (byte)ItemClass.ControlClass,
                        Flags = (ushort)(CharacterAttributeFlags.CharacterLineBreak | CharacterAttributeFlags.CharacterCRLF),
                        BreakType = (byte)CharBreakingType.ControlBreak,
                        BiDi = DirectionClass.ParagraphSeparator,
                    };
                case 0x0085: // NEL
                case 0x2028: // LS
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Control,
                        ItemClass = (byte)ItemClass.ControlClass,
                        Flags = (ushort)CharacterAttributeFlags.CharacterLineBreak,
                        BreakType = (byte)CharBreakingType.ControlBreak,
                        BiDi = ch == 0x0085 ? DirectionClass.ParagraphSeparator : DirectionClass.WhiteSpace,
                    };
                case 0x2029: // PS
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Control,
                        ItemClass = (byte)ItemClass.ControlClass,
                        Flags = (ushort)CharacterAttributeFlags.CharacterParaBreak,
                        BreakType = (byte)CharBreakingType.ControlBreak,
                        BiDi = DirectionClass.ParagraphSeparator,
                    };
                case '\t':
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Control,
                        ItemClass = (byte)ItemClass.ControlClass,
                        Flags = (ushort)CharacterAttributeFlags.CharacterFormatAnchor,
                        BreakType = (byte)CharBreakingType.ControlBreak,
                        BiDi = DirectionClass.SegmentSeparator,
                    };
                case '\u000B':
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Control,
                        ItemClass = (byte)ItemClass.ControlClass,
                        Flags = (ushort)CharacterAttributeFlags.CharacterLineBreak,
                        BreakType = (byte)CharBreakingType.ControlBreak,
                        BiDi = DirectionClass.SegmentSeparator,
                    };
                case '\f':
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Control,
                        ItemClass = (byte)ItemClass.ControlClass,
                        Flags = (ushort)CharacterAttributeFlags.CharacterLineBreak,
                        BreakType = (byte)CharBreakingType.ControlBreak,
                        BiDi = DirectionClass.WhiteSpace,
                    };
                case 0x200C: // ZWNJ
                case 0x200D: // ZWJ
                case 0x034F: // CGJ
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Control,
                        ItemClass = (byte)ItemClass.JoinerClass,
                        Flags = (ushort)(CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterFormatAnchor),
                        BreakType = (byte)CharBreakingType.NoBreak,
                        BiDi = DirectionClass.BoundaryNeutral,
                    };
                case 0x200B: // ZWSP
                case 0xFEFF: // BOM / ZWNBSP
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Control,
                        ItemClass = (byte)ItemClass.ControlClass,
                        Flags = (ushort)CharacterAttributeFlags.CharacterFormatAnchor,
                        BreakType = (byte)CharBreakingType.NoBreak,
                        BiDi = DirectionClass.BoundaryNeutral,
                    };
                case 0x200E: // LRM
                case 0x200F: // RLM
                case 0x061C: // ALM
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Control,
                        ItemClass = (byte)ItemClass.StrongClass,
                        Flags = (ushort)(CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterFormatAnchor |
                                         (ch == 0x200E ? (CharacterAttributeFlags)0 : CharacterAttributeFlags.CharacterRTL)),
                        BreakType = (byte)CharBreakingType.NoBreak,
                        BiDi = ch == 0x200E ? DirectionClass.Left : (ch == 0x061C ? DirectionClass.ArabicLetter : DirectionClass.Right),
                    };
                case 0x202A:
                    return BuildControlAttribute(DirectionClass.LeftToRightEmbedding, (ushort)CharacterAttributeFlags.CharacterFormatAnchor);
                case 0x202B:
                    return BuildControlAttribute(DirectionClass.RightToLeftEmbedding, (ushort)(CharacterAttributeFlags.CharacterFormatAnchor | CharacterAttributeFlags.CharacterRTL | CharacterAttributeFlags.CharacterComplex));
                case 0x202C:
                    return BuildControlAttribute(DirectionClass.PopDirectionalFormat, (ushort)(CharacterAttributeFlags.CharacterFormatAnchor | CharacterAttributeFlags.CharacterComplex));
                case 0x202D:
                    return BuildControlAttribute(DirectionClass.LeftToRightOverride, (ushort)(CharacterAttributeFlags.CharacterFormatAnchor | CharacterAttributeFlags.CharacterComplex));
                case 0x202E:
                    return BuildControlAttribute(DirectionClass.RightToLeftOverride, (ushort)(CharacterAttributeFlags.CharacterFormatAnchor | CharacterAttributeFlags.CharacterRTL | CharacterAttributeFlags.CharacterComplex));
                case 0x00A0: // NBSP
                    return BuildNeutralAttribute(ItemClass.CSClass, DirectionClass.CommonSeparator, (ushort)CharacterAttributeFlags.CharacterSpace, CharBreakingType.NoBreak);
                case 0x066A: // Arabic percent sign
                    return BuildNeutralAttribute(ItemClass.ETClass, DirectionClass.EuropeanTerminator, (ushort)(CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterRTL), CharBreakingType.NoBreak);
                case 0x066B: // Arabic decimal separator
                    return BuildNeutralAttribute(ItemClass.ANClass, DirectionClass.ArabicNumber, (ushort)(CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterRTL), CharBreakingType.NoBreak);
                case 0x066C: // Arabic thousands separator
                    return BuildNeutralAttribute(ItemClass.CSClass, DirectionClass.ArabicNumber, (ushort)(CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterRTL), CharBreakingType.NoBreak);
            }

            // Variation selectors and Mongolian free variation selectors
            if ((ch >= 0xFE00 && ch <= 0xFE0F)
                || (ch >= 0x180B && ch <= 0x180D)
                || ch == 0x180F
                || IsIVS(ch))
            {
                return new CharacterAttribute
                {
                    Script = (byte)ScriptID.Control,
                    ItemClass = (byte)ItemClass.JoinerClass,
                    Flags = (ushort)(CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterExtended),
                    BreakType = (byte)CharBreakingType.NoBreak,
                    BiDi = DirectionClass.NonSpacingMark,
                };
            }

            // ASCII fast path for printable chars and controls
            if (ch < 128)
            {
                if (ch < 0x20 || ch == 0x7F)
                {
                    return BuildControlAttribute(DirectionClass.BoundaryNeutral, 0);
                }
                if (ch == ' ')
                {
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Default,
                        ItemClass = (byte)ItemClass.WeakClass,
                        Flags = (ushort)(CharacterAttributeFlags.CharacterFastText | CharacterAttributeFlags.CharacterSpace),
                        BreakType = (byte)CharBreakingType.NoBreak,
                        BiDi = DirectionClass.WhiteSpace,
                    };
                }
                if (ch >= '0' && ch <= '9')
                {
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Digit,
                        ItemClass = (byte)ItemClass.DigitClass,
                        Flags = (ushort)(CharacterAttributeFlags.CharacterFastText | CharacterAttributeFlags.CharacterDigit),
                        BreakType = (byte)CharBreakingType.DigitBreak,
                        BiDi = DirectionClass.EuropeanNumber,
                    };
                }
                if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z'))
                {
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Latin,
                        ItemClass = (byte)ItemClass.StrongClass,
                        Flags = (ushort)(CharacterAttributeFlags.CharacterFastText | CharacterAttributeFlags.CharacterLetter),
                        BreakType = (byte)CharBreakingType.NoBreak,
                        BiDi = DirectionClass.Left,
                    };
                }
                if (ch == '+' || ch == '-')
                {
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Default,
                        ItemClass = (byte)ItemClass.ESClass,
                        Flags = (ushort)CharacterAttributeFlags.CharacterFastText,
                        BreakType = (byte)CharBreakingType.NoBreak,
                        BiDi = DirectionClass.EuropeanSeparator,
                    };
                }
                if (ch == ',' || ch == '.' || ch == '/' || ch == ':')
                {
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Default,
                        ItemClass = (byte)ItemClass.CSClass,
                        Flags = (ushort)CharacterAttributeFlags.CharacterFastText,
                        BreakType = (byte)CharBreakingType.NoBreak,
                        BiDi = DirectionClass.CommonSeparator,
                    };
                }
                if (ch == '#' || ch == '$' || ch == '%')
                {
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Default,
                        ItemClass = (byte)ItemClass.ETClass,
                        Flags = (ushort)CharacterAttributeFlags.CharacterFastText,
                        BreakType = (byte)CharBreakingType.NoBreak,
                        BiDi = DirectionClass.EuropeanTerminator,
                    };
                }
                if (IsMirrorAscii(ch))
                {
                    return BuildMirrorAttribute(DirectionClass.OtherNeutral, fastText: true);
                }
                return new CharacterAttribute
                {
                    Script = (byte)ScriptID.Default,
                    ItemClass = (byte)ItemClass.WeakClass,
                    Flags = (ushort)CharacterAttributeFlags.CharacterFastText,
                    BreakType = (byte)CharBreakingType.NoBreak,
                    BiDi = DirectionClass.OtherNeutral,
                };
            }

            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            ScriptID script = GetScriptForScalar(ch);
            bool extended = ch > 0xFFFF;

            switch (cat)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                    return BuildLetterAttribute(script);

                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.EnclosingMark:
                    return BuildMarkAttribute(script, isSpacing: false);

                case UnicodeCategory.SpacingCombiningMark:
                    return BuildMarkAttribute(script, isSpacing: true);

                case UnicodeCategory.DecimalDigitNumber:
                    if (ch >= 0x0660 && ch <= 0x0669)
                    {
                        return new CharacterAttribute
                        {
                            Script = (byte)ScriptID.Digit,
                            ItemClass = (byte)ItemClass.ANClass,
                            Flags = (ushort)(CharacterAttributeFlags.CharacterDigit | CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterRTL),
                            BreakType = (byte)CharBreakingType.DigitBreak,
                            BiDi = DirectionClass.ArabicNumber,
                        };
                    }
                    if (ch >= 0x06F0 && ch <= 0x06F9)
                    {
                        return new CharacterAttribute
                        {
                            Script = (byte)ScriptID.Digit,
                            ItemClass = (byte)ItemClass.DigitClass,
                            Flags = (ushort)(CharacterAttributeFlags.CharacterDigit | CharacterAttributeFlags.CharacterComplex),
                            BreakType = (byte)CharBreakingType.DigitBreak,
                            BiDi = DirectionClass.EuropeanNumber,
                        };
                    }
                    return new CharacterAttribute
                    {
                        Script = (byte)ScriptID.Digit,
                        ItemClass = (byte)ItemClass.DigitClass,
                        Flags = (ushort)(CharacterAttributeFlags.CharacterDigit | (extended ? CharacterAttributeFlags.CharacterExtended : (CharacterAttributeFlags)0)),
                        BreakType = (byte)CharBreakingType.DigitBreak,
                        BiDi = DirectionClass.EuropeanNumber,
                    };

                case UnicodeCategory.SpaceSeparator:
                    return BuildNeutralAttribute(
                        ItemClass.WeakClass,
                        DirectionClass.WhiteSpace,
                        (ushort)(CharacterAttributeFlags.CharacterSpace | (ch == 0x3000 ? CharacterAttributeFlags.CharacterIdeo : (CharacterAttributeFlags)0)),
                        CharBreakingType.NoBreak);

                case UnicodeCategory.LineSeparator:
                    return BuildControlAttribute(DirectionClass.WhiteSpace, (ushort)CharacterAttributeFlags.CharacterLineBreak);

                case UnicodeCategory.ParagraphSeparator:
                    return BuildControlAttribute(DirectionClass.ParagraphSeparator, (ushort)CharacterAttributeFlags.CharacterParaBreak);

                case UnicodeCategory.Control:
                    return BuildControlAttribute(DirectionClass.BoundaryNeutral, 0);

                case UnicodeCategory.Format:
                    return BuildControlAttribute(
                        DirectionClass.BoundaryNeutral,
                        (ushort)(CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterFormatAnchor));

                case UnicodeCategory.CurrencySymbol:
                    return BuildNeutralAttribute(ItemClass.ETClass, DirectionClass.EuropeanTerminator, 0, CharBreakingType.NoBreak);

                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.FinalQuotePunctuation:
                    return BuildMirrorAttribute(DirectionClass.OtherNeutral, fastText: false);

                default:
                    if (IsRtlBlock(ch))
                    {
                        return BuildNeutralAttribute(
                            ItemClass.WeakClass,
                            IsArabicBlock(ch) ? DirectionClass.ArabicLetter : DirectionClass.Right,
                            (ushort)(CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterRTL),
                            CharBreakingType.NoBreak);
                    }
                    if (ch >= 0x2190 && ch <= 0x2BFF)
                    {
                        return BuildNeutralAttribute(ItemClass.WeakClass, DirectionClass.OtherNeutral, 0, CharBreakingType.NoBreak);
                    }
                    return BuildNeutralAttribute(
                        ItemClass.WeakClass,
                        cat == UnicodeCategory.LetterNumber ? DirectionClass.Left : DirectionClass.OtherNeutral,
                        extended ? (ushort)CharacterAttributeFlags.CharacterExtended : (ushort)0,
                        CharBreakingType.NoBreak);
            }
        }

        private static CharacterAttribute BuildLetterAttribute(ScriptID script)
        {
            DirectionClass bidi = DirectionClass.Left;
            ushort flags = (ushort)CharacterAttributeFlags.CharacterLetter;

            if (script == ScriptID.Arabic || script == ScriptID.Syriac)
            {
                bidi = DirectionClass.ArabicLetter;
                flags |= (ushort)(CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterRTL);
            }
            else if (script == ScriptID.Hebrew || script == ScriptID.Thaana)
            {
                bidi = DirectionClass.Right;
                flags |= (ushort)(CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterRTL);
            }
            else if (IsComplexScript(script))
            {
                flags |= (ushort)CharacterAttributeFlags.CharacterComplex;
            }
            else if (IsIdeographicScript(script))
            {
                flags |= (ushort)CharacterAttributeFlags.CharacterIdeo;
            }
            else if (script == ScriptID.Latin || script == ScriptID.Greek || script == ScriptID.Cyrillic)
            {
                flags |= (ushort)CharacterAttributeFlags.CharacterFastText;
            }

            return new CharacterAttribute
            {
                Script = (byte)script,
                ItemClass = (byte)ItemClass.StrongClass,
                Flags = flags,
                BreakType = (byte)CharBreakingType.NoBreak,
                BiDi = bidi,
            };
        }

        private static CharacterAttribute BuildMarkAttribute(ScriptID script, bool isSpacing)
        {
            bool complex = IsComplexScript(script)
                || script == ScriptID.Arabic
                || script == ScriptID.Hebrew
                || script == ScriptID.Syriac
                || script == ScriptID.Thaana;

            ushort flags = (ushort)(CharacterAttributeFlags.CharacterComplex | CharacterAttributeFlags.CharacterExtended);
            if (script == ScriptID.Arabic || script == ScriptID.Hebrew || script == ScriptID.Syriac || script == ScriptID.Thaana)
            {
                flags |= (ushort)CharacterAttributeFlags.CharacterRTL;
            }

            return new CharacterAttribute
            {
                Script = (byte)script,
                ItemClass = (byte)(complex ? ItemClass.ComplexMarkClass : ItemClass.SimpleMarkClass),
                Flags = flags,
                BreakType = (byte)CharBreakingType.NoBreak,
                BiDi = isSpacing ? DirectionClass.Left : DirectionClass.NonSpacingMark,
            };
        }

        private static CharacterAttribute BuildNeutralAttribute(
            ItemClass itemClass,
            DirectionClass bidi,
            ushort flags,
            CharBreakingType breakType)
        {
            return new CharacterAttribute
            {
                Script = (byte)ScriptID.Default,
                ItemClass = (byte)itemClass,
                Flags = flags,
                BreakType = (byte)breakType,
                BiDi = bidi,
            };
        }

        private static CharacterAttribute BuildMirrorAttribute(DirectionClass bidi, bool fastText)
        {
            return new CharacterAttribute
            {
                Script = (byte)ScriptID.Mirror,
                ItemClass = (byte)ItemClass.WeakClass,
                Flags = fastText ? (ushort)CharacterAttributeFlags.CharacterFastText : (ushort)0,
                BreakType = (byte)CharBreakingType.PairMirrorBreak,
                BiDi = bidi,
            };
        }

        private static CharacterAttribute BuildControlAttribute(DirectionClass bidi, ushort flags)
        {
            return new CharacterAttribute
            {
                Script = (byte)ScriptID.Control,
                ItemClass = (byte)ItemClass.ControlClass,
                Flags = flags,
                BreakType = (byte)CharBreakingType.ControlBreak,
                BiDi = bidi,
            };
        }

        private static bool IsMirrorAscii(int ch)
        {
            return ch == '(' || ch == ')' || ch == '<' || ch == '>'
                || ch == '[' || ch == ']' || ch == '{' || ch == '}';
        }

        private static bool IsComplexScript(ScriptID script)
        {
            switch (script)
            {
                case ScriptID.Arabic:
                case ScriptID.Bengali:
                case ScriptID.Buginese:
                case ScriptID.Devanagari:
                case ScriptID.Gujarati:
                case ScriptID.Gurmukhi:
                case ScriptID.Hangul:
                case ScriptID.Hebrew:
                case ScriptID.Kannada:
                case ScriptID.Kharoshthi:
                case ScriptID.Khmer:
                case ScriptID.Lao:
                case ScriptID.Limbu:
                case ScriptID.Malayalam:
                case ScriptID.Mongolian:
                case ScriptID.Myanmar:
                case ScriptID.NewTaiLue:
                case ScriptID.Oriya:
                case ScriptID.Sinhala:
                case ScriptID.SylotiNagri:
                case ScriptID.Syriac:
                case ScriptID.Tamil:
                case ScriptID.Telugu:
                case ScriptID.Thaana:
                case ScriptID.Thai:
                case ScriptID.Tibetan:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsIdeographicScript(ScriptID script)
        {
            return script == ScriptID.CJKIdeographic
                || script == ScriptID.Kana
                || script == ScriptID.Bopomofo
                || script == ScriptID.Yi;
        }

        private static bool IsRtlBlock(int ch)
        {
            return IsArabicBlock(ch)
                || (ch >= 0x0590 && ch <= 0x05FF)
                || (ch >= 0x0780 && ch <= 0x085F)
                || (ch >= 0xFB1D && ch <= 0xFB4F);
        }

        private static bool IsArabicBlock(int ch)
        {
            return (ch >= 0x0600 && ch <= 0x077F)
                || (ch >= 0x08A0 && ch <= 0x08FF)
                || (ch >= 0xFB50 && ch <= 0xFDFF)
                || (ch >= 0xFE70 && ch <= 0xFEFF);
        }

        private static ScriptID GetScriptForScalar(int ch)
        {
            if (ch < 0x02B0)
            {
                if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch >= 0x00C0)
                    return ScriptID.Latin;
                return ScriptID.Default;
            }

            if (ch < 0x0370) return ScriptID.Latin;
            if (ch < 0x0400) return (ch >= 0x03E2 && ch <= 0x03EF) ? ScriptID.Coptic : ScriptID.Greek;
            if (ch < 0x0530) return ScriptID.Cyrillic;
            if (ch < 0x0590) return ScriptID.Armenian;
            if (ch < 0x0600) return ScriptID.Hebrew;
            if (ch < 0x0700) return ScriptID.Arabic;
            if (ch < 0x0750) return ScriptID.Syriac;
            if (ch < 0x0780) return ScriptID.Arabic;
            if (ch < 0x07C0) return ScriptID.Thaana;
            if (ch < 0x08A0) return ScriptID.Default;
            if (ch < 0x0900) return ScriptID.Arabic;
            if (ch < 0x0980) return ScriptID.Devanagari;
            if (ch < 0x0A00) return ScriptID.Bengali;
            if (ch < 0x0A80) return ScriptID.Gurmukhi;
            if (ch < 0x0B00) return ScriptID.Gujarati;
            if (ch < 0x0B80) return ScriptID.Oriya;
            if (ch < 0x0C00) return ScriptID.Tamil;
            if (ch < 0x0C80) return ScriptID.Telugu;
            if (ch < 0x0D00) return ScriptID.Kannada;
            if (ch < 0x0D80) return ScriptID.Malayalam;
            if (ch < 0x0E00) return ScriptID.Sinhala;
            if (ch < 0x0E80) return ScriptID.Thai;
            if (ch < 0x0F00) return ScriptID.Lao;
            if (ch < 0x1000) return ScriptID.Tibetan;
            if (ch < 0x10A0) return ScriptID.Myanmar;
            if (ch < 0x1100) return ScriptID.Georgian;
            if (ch < 0x1200) return ScriptID.Hangul;
            if (ch < 0x13A0) return ScriptID.Ethiopic;
            if (ch < 0x1400) return ScriptID.Cherokee;
            if (ch < 0x1680) return ScriptID.CanadianSyllabics;
            if (ch < 0x16A0) return ScriptID.Ogham;
            if (ch < 0x1700) return ScriptID.Runic;
            if (ch < 0x1720) return ScriptID.Tagalog;
            if (ch < 0x1740) return ScriptID.Hanunoo;
            if (ch < 0x1760) return ScriptID.Buhid;
            if (ch < 0x1780) return ScriptID.Tagbanwa;
            if (ch < 0x1800) return ScriptID.Khmer;
            if (ch < 0x18B0) return ScriptID.Mongolian;
            if (ch < 0x1900) return ScriptID.CanadianSyllabics;
            if (ch < 0x1950) return ScriptID.Limbu;
            if (ch < 0x1980) return ScriptID.TaiLe;
            if (ch < 0x19E0) return ScriptID.NewTaiLue;
            if (ch < 0x1A00) return ScriptID.Khmer;
            if (ch < 0x1A20) return ScriptID.Buginese;
            if (ch >= 0x1D00 && ch <= 0x1EFF) return ScriptID.Latin;
            if (ch >= 0x1F00 && ch <= 0x1FFF) return ScriptID.Greek;
            if (ch >= 0x2800 && ch <= 0x28FF) return ScriptID.Braille;
            if (ch >= 0x2C00 && ch <= 0x2C5F) return ScriptID.Glagolitic;
            if (ch >= 0x2C60 && ch <= 0x2C7F) return ScriptID.Latin;
            if (ch >= 0x2C80 && ch <= 0x2CFF) return ScriptID.Coptic;
            if (ch >= 0x2D00 && ch <= 0x2D2F) return ScriptID.Georgian;
            if (ch >= 0x2D30 && ch <= 0x2D7F) return ScriptID.Tifinagh;
            if (ch >= 0x2D80 && ch <= 0x2DDF) return ScriptID.Ethiopic;
            if (ch >= 0x2DE0 && ch <= 0x2DFF) return ScriptID.Cyrillic;
            if (ch >= 0x2E80 && ch <= 0x2FDF) return ScriptID.CJKIdeographic;
            if (ch >= 0x3040 && ch <= 0x30FF) return ScriptID.Kana;
            if (ch >= 0x3100 && ch <= 0x312F) return ScriptID.Bopomofo;
            if (ch >= 0x3130 && ch <= 0x318F) return ScriptID.Hangul;
            if (ch >= 0x31A0 && ch <= 0x31BF) return ScriptID.Bopomofo;
            if (ch >= 0x31F0 && ch <= 0x31FF) return ScriptID.Kana;
            if (ch >= 0x3400 && ch <= 0x9FFF) return ScriptID.CJKIdeographic;
            if (ch >= 0xA000 && ch <= 0xA4CF) return ScriptID.Yi;
            if (ch >= 0xA640 && ch <= 0xA69F) return ScriptID.Cyrillic;
            if (ch >= 0xA720 && ch <= 0xA7FF) return ScriptID.Latin;
            if (ch >= 0xA800 && ch <= 0xA82F) return ScriptID.SylotiNagri;
            if (ch >= 0xA8E0 && ch <= 0xA8FF) return ScriptID.Devanagari;
            if (ch >= 0xA960 && ch <= 0xA97F) return ScriptID.Hangul;
            if (ch >= 0xA9E0 && ch <= 0xAA7F) return ScriptID.Myanmar;
            if (ch >= 0xAB00 && ch <= 0xAB2F) return ScriptID.Ethiopic;
            if (ch >= 0xAB30 && ch <= 0xAB6F) return ScriptID.Latin;
            if (ch >= 0xAB70 && ch <= 0xABBF) return ScriptID.Cherokee;
            if (ch >= 0xAC00 && ch <= 0xD7FF) return ScriptID.Hangul;
            if (ch >= 0xF900 && ch <= 0xFAFF) return ScriptID.CJKIdeographic;
            if (ch >= 0xFB13 && ch <= 0xFB17) return ScriptID.Armenian;
            if (ch >= 0xFB1D && ch <= 0xFB4F) return ScriptID.Hebrew;
            if (ch >= 0xFB50 && ch <= 0xFDFF) return ScriptID.Arabic;
            if (ch >= 0xFE70 && ch <= 0xFEFF) return ScriptID.Arabic;
            if (ch >= 0xFF21 && ch <= 0xFF3A) return ScriptID.Latin;
            if (ch >= 0xFF41 && ch <= 0xFF5A) return ScriptID.Latin;
            if (ch >= 0xFF65 && ch <= 0xFF9F) return ScriptID.Kana;
            if (ch >= 0xFFA0 && ch <= 0xFFDC) return ScriptID.Hangul;

            if (ch >= 0x10000)
            {
                if (ch <= 0x100FF) return ScriptID.LinearB;
                if (ch >= 0x10300 && ch <= 0x1032F) return ScriptID.OldItalic;
                if (ch >= 0x10330 && ch <= 0x1034F) return ScriptID.Gothic;
                if (ch >= 0x10380 && ch <= 0x1039F) return ScriptID.UgariticCuneiform;
                if (ch >= 0x103A0 && ch <= 0x103DF) return ScriptID.OldPersianCuneiform;
                if (ch >= 0x10400 && ch <= 0x1044F) return ScriptID.Deseret;
                if (ch >= 0x10450 && ch <= 0x1047F) return ScriptID.Shavian;
                if (ch >= 0x10480 && ch <= 0x104AF) return ScriptID.Osmanya;
                if (ch >= 0x10800 && ch <= 0x1083F) return ScriptID.CypriotSyllabary;
                if (ch >= 0x10A00 && ch <= 0x10A5F) return ScriptID.Kharoshthi;
                if (ch >= 0x1D100 && ch <= 0x1D1FF) return ScriptID.MusicalSymbols;
                if (ch >= 0x1D400 && ch <= 0x1D7FF) return ScriptID.MathematicalAlphanumericSymbols;
                if (ch >= 0x20000 && ch <= 0x323AF) return ScriptID.CJKIdeographic;
            }

            return ScriptID.Default;
        }
    }
}
