// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.Globalization;
using System.IO;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Markup
{
    /// <summary>
    ///     An RFC3066 language tag for use in Xml markup.
    /// </summary>
    public class XmlLanguage
    {
        private static Hashtable _cache = new Hashtable(InitialDictionarySize);
        private const int InitialDictionarySize = 10;

        private const int MaxCultureDepth = 32;
        private static XmlLanguage _empty = null;

        private readonly string _lowerCaseTag;
        private CultureInfo _equivalentCulture;
        private CultureInfo _specificCulture;
        private CultureInfo _compatibleCulture;
        private int _specificity;
        private bool _equivalentCultureFailed;

        private XmlLanguage(string lowercase)
        {
            _lowerCaseTag = lowercase;
            _equivalentCulture = null;
            _specificCulture = null;
            _compatibleCulture = null;
            _specificity = -1;
            _equivalentCultureFailed = false;
        }

        public static XmlLanguage Empty
        {
            get
            {
                if (_empty == null)
                {
                    _empty = GetLanguage(string.Empty);
                }
                return _empty;
            }
        }

        public static XmlLanguage GetLanguage(string ietfLanguageTag)
        {
            XmlLanguage language;

            ArgumentNullException.ThrowIfNull(ietfLanguageTag);

            string lowercase = AsciiToLower(ietfLanguageTag);

            language = (XmlLanguage)_cache[lowercase];
            if (language == null)
            {
                ValidateLowerCaseTag(lowercase);
                lock (_cache.SyncRoot)
                {
                    language = (XmlLanguage)_cache[lowercase];
                    if (language == null)
                    {
                        _cache[lowercase] = language = new XmlLanguage(lowercase);
                    }
                }
            }

            return language;
        }

        public string IetfLanguageTag => _lowerCaseTag;

        public override string ToString() => IetfLanguageTag;

        public CultureInfo GetEquivalentCulture()
        {
            if (_equivalentCulture == null)
            {
                string lowerCaseTag = _lowerCaseTag;

                if (string.Equals(lowerCaseTag, "und", StringComparison.Ordinal))
                {
                    lowerCaseTag = string.Empty;
                }

                try
                {
                    _equivalentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag(lowerCaseTag);
                }
                catch (ArgumentException e)
                {
                    _equivalentCultureFailed = true;
                    throw new InvalidOperationException(SR.Format(SR.XmlLangGetCultureFailure, lowerCaseTag), e);
                }
            }

            return _equivalentCulture;
        }

        public CultureInfo GetSpecificCulture()
        {
            if (_specificCulture == null)
            {
                if (_lowerCaseTag.Length == 0 || string.Equals(_lowerCaseTag, "und", StringComparison.Ordinal))
                {
                    _specificCulture = GetEquivalentCulture();
                }
                else
                {
                    CultureInfo culture = GetCompatibleCulture();

                    if (culture.IetfLanguageTag.Length == 0)
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlLangGetSpecificCulture, _lowerCaseTag));
                    }

                    if (!culture.IsNeutralCulture)
                    {
                        _specificCulture = culture;
                    }
                    else
                    {
                        try
                        {
                            culture = CultureInfo.CreateSpecificCulture(culture.Name);
                            _specificCulture = CultureInfo.GetCultureInfoByIetfLanguageTag(culture.IetfLanguageTag);
                        }
                        catch (ArgumentException e)
                        {
                            throw new InvalidOperationException(SR.Format(SR.XmlLangGetSpecificCulture, _lowerCaseTag), e);
                        }
                    }
                }
            }

            return _specificCulture;
        }

        internal CultureInfo GetCompatibleCulture()
        {
            if (_compatibleCulture == null)
            {
                if (!TryGetEquivalentCulture(out CultureInfo culture))
                {
                    string languageTag = IetfLanguageTag;

                    do
                    {
                        languageTag = Shorten(languageTag);
                        if (languageTag == null)
                        {
                            culture = CultureInfo.InvariantCulture;
                        }
                        else
                        {
                            try
                            {
                                culture = CultureInfo.GetCultureInfoByIetfLanguageTag(languageTag);
                            }
                            catch (ArgumentException)
                            {
                            }
                        }
                    }
                    while (culture == null);
                }
                _compatibleCulture = culture;
            }
            return _compatibleCulture;
        }

        internal bool RangeIncludes(XmlLanguage language)
        {
            if (this.IsPrefixOf(language.IetfLanguageTag))
            {
                return true;
            }

            return RangeIncludes(language.GetCompatibleCulture());
        }

        internal bool RangeIncludes(CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(culture);

            for (int i = 0; i < MaxCultureDepth; ++i)
            {
                if (this.IsPrefixOf(culture.IetfLanguageTag))
                {
                    return true;
                }

                CultureInfo parentCulture = culture.Parent;

                if (parentCulture == null
                        || parentCulture.Equals(CultureInfo.InvariantCulture)
                        || parentCulture == culture)
                    break;

                culture = parentCulture;
            }

            return false;
        }

        internal int GetSpecificity()
        {
            if (_specificity < 0)
            {
                CultureInfo compatibleCulture = GetCompatibleCulture();

                int specificity = GetSpecificity(compatibleCulture, MaxCultureDepth);

                if (compatibleCulture != _equivalentCulture)
                {
                    specificity += GetSubtagCount(_lowerCaseTag) - GetSubtagCount(compatibleCulture.IetfLanguageTag);
                }

                _specificity = specificity;
            }

            return _specificity;
        }

        private static int GetSpecificity(CultureInfo culture, int maxDepth)
        {
            int specificity = 0;

            if (maxDepth != 0 && culture != null)
            {
                string languageTag = culture.IetfLanguageTag;

                if (languageTag.Length > 0)
                {
                    specificity = Math.Max(GetSubtagCount(languageTag), 1 + GetSpecificity(culture.Parent, maxDepth - 1));
                }
            }

            return specificity;
        }

        private static int GetSubtagCount(string languageTag)
        {
            int tagLength = languageTag.Length;
            int subtagCount = 0;

            if (tagLength > 0)
            {
                subtagCount = 1;

                for (int i = 0; i < tagLength; i++)
                {
                    if (languageTag[i] == '-')
                    {
                        subtagCount += 1;
                    }
                }
            }

            return subtagCount;
        }

        internal MatchingLanguageCollection MatchingLanguages => new MatchingLanguageCollection(this);

        internal struct MatchingLanguageCollection : IEnumerable<XmlLanguage>, IEnumerable
        {
            private XmlLanguage _start;
            public MatchingLanguageCollection(XmlLanguage start)
            {
                _start = start;
            }

            public MatchingLanguageEnumerator GetEnumerator() => new MatchingLanguageEnumerator(_start);
            IEnumerator<XmlLanguage> IEnumerable<XmlLanguage>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        internal struct MatchingLanguageEnumerator : IEnumerator<XmlLanguage>, IEnumerator
        {
            private readonly XmlLanguage _start;
            private XmlLanguage _current;
            private bool _atStart;
            private bool _pastEnd;
            private int _maxCultureDepth;

            public MatchingLanguageEnumerator(XmlLanguage start)
            {
                _start = start;
                _current = start;
                _pastEnd = false;
                _atStart = true;
                _maxCultureDepth = XmlLanguage.MaxCultureDepth;
            }

            public void Reset()
            {
                _current = _start;
                _pastEnd = false;
                _atStart = true;
                _maxCultureDepth = XmlLanguage.MaxCultureDepth;
            }

            public XmlLanguage Current
            {
                get
                {
                    if (_atStart)
                    {
                        throw new InvalidOperationException(SR.Enumerator_NotStarted);
                    }
                    if (_pastEnd)
                    {
                        throw new InvalidOperationException(SR.Enumerator_ReachedEnd);
                    }

                    return _current;
                }
            }

            public bool MoveNext()
            {
                if (_atStart)
                {
                    _atStart = false;
                    return true;
                }
                else if (_current.IetfLanguageTag.Length == 0)
                {
                    _atStart = false;
                    _pastEnd = true;
                    return false;
                }
                else
                {
                    XmlLanguage prefixLanguage = _current.PrefixLanguage;
                    CultureInfo culture = null;

                    if (_maxCultureDepth > 0)
                    {
                        if (_current.TryGetEquivalentCulture(out culture))
                        {
                            culture = culture.Parent;
                        }
                        else
                        {
                            culture = null;
                        }
                    }

                    if (culture == null)
                    {
                        _current = prefixLanguage;
                        _atStart = false;
                        return true;
                    }
                    else
                    {
                        XmlLanguage parentLanguage = XmlLanguage.GetLanguage(culture.IetfLanguageTag);

                        if (parentLanguage.IsPrefixOf(prefixLanguage.IetfLanguageTag))
                        {
                            _current = prefixLanguage;
                            _atStart = false;
                            return true;
                        }
                        else
                        {
                            _maxCultureDepth -= 1;
                            _current = parentLanguage;
                            _atStart = false;
                            return true;
                        }
                    }
                }
            }

            object IEnumerator.Current => Current;
            void IDisposable.Dispose() { }
        }

        private bool IsPrefixOf(string longTag)
        {
            string prefix = IetfLanguageTag;

            if (!longTag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return (prefix.Length == 0 || prefix.Length == longTag.Length || longTag[prefix.Length] == '-');
        }

        private bool TryGetEquivalentCulture(out CultureInfo culture)
        {
            culture = null;

            if (_equivalentCulture == null && !_equivalentCultureFailed)
            {
                try
                {
                    GetEquivalentCulture();
                }
                catch (InvalidOperationException)
                {
                }
            }

            culture = _equivalentCulture;

            return (culture != null);
        }

        private XmlLanguage PrefixLanguage
        {
            get
            {
                string prefix = Shorten(IetfLanguageTag);
                return XmlLanguage.GetLanguage(prefix);
            }
        }

        private static string Shorten(string languageTag)
        {
            if (languageTag.Length == 0)
            {
                return null;
            }

            int i = languageTag.Length - 1;

            while (languageTag[i] != '-' && i > 0)
            {
                i -= 1;
            }

            return languageTag.Substring(0, i);
        }

        private static void ValidateLowerCaseTag(string ietfLanguageTag)
        {
            ArgumentNullException.ThrowIfNull(ietfLanguageTag);

            if (ietfLanguageTag.Length > 0)
            {
                using (StringReader reader = new StringReader(ietfLanguageTag))
                {
                    int i = ParseSubtag(ietfLanguageTag, reader, isPrimary: true);
                    while (i != -1)
                    {
                        i = ParseSubtag(ietfLanguageTag, reader, isPrimary: false);
                    }
                }
            }
        }

        private static int ParseSubtag(string ietfLanguageTag, StringReader reader, bool isPrimary)
        {
            const int maxCharsPerSubtag = 8;

            int c = reader.Read();

            bool ok = IsLowerAlpha(c);
            if (!ok && !isPrimary)
                ok = IsDigit(c);

            if (!ok)
            {
                ThrowParseException(ietfLanguageTag);
            }

            int charsRead = 1;
            for (;;)
            {
                c = reader.Read();
                charsRead++;

                ok = IsLowerAlpha(c);
                if (!ok && !isPrimary)
                {
                    ok = IsDigit(c);
                }

                if (!ok)
                {
                    if (c == -1 || c == '-')
                    {
                        return c;
                    }
                    else
                    {
                        ThrowParseException(ietfLanguageTag);
                    }
                }
                else
                {
                    if (charsRead > maxCharsPerSubtag)
                    {
                        ThrowParseException(ietfLanguageTag);
                    }
                }
            }
        }

        private static bool IsLowerAlpha(int c) => c >= 'a' && c <= 'z';
        private static bool IsDigit(int c) => c >= '0' && c <= '9';

        private static void ThrowParseException(string ietfLanguageTag)
        {
            throw new ArgumentException(SR.Format(SR.XmlLangMalformed, ietfLanguageTag), nameof(ietfLanguageTag));
        }

        private static string AsciiToLower(string tag)
        {
            int length = tag.Length;

            for (int i = 0; i < length; i++)
            {
                if (tag[i] > 127)
                {
                    ThrowParseException(tag);
                }
            }

            return tag.ToLowerInvariant();
        }
    }
}
