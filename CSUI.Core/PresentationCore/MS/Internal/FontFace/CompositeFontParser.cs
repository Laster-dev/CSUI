// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

// Contents:  The XML Composite font parsing

using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Markup;
using System.Globalization;
using System.Collections;
using MS.Internal.TextFormatting;

using MS.Internal.PresentationCore;

namespace MS.Internal.FontFace
{
    internal class CompositeFontParser
    {
        internal static void VerifyMultiplierOfEm(string propertyName, ref double value)
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException(SR.Format(SR.PropertyValueCannotBeNaN, propertyName));
            }
            else if (value > Constants.GreatestMutiplierOfEm)
            {
                value = Constants.GreatestMutiplierOfEm;
            }
            else if (value < -Constants.GreatestMutiplierOfEm)
            {
                value = -Constants.GreatestMutiplierOfEm;
            }
        }

        internal static void VerifyPositiveMultiplierOfEm(string propertyName, ref double value)
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException(SR.Format(SR.PropertyValueCannotBeNaN, propertyName));
            }
            else if (value > Constants.GreatestMutiplierOfEm)
            {
                value = Constants.GreatestMutiplierOfEm;
            }
            else if (value <= 0)
            {
                throw new ArgumentException(SR.Format(SR.PropertyMustBeGreaterThanZero, propertyName));
            }
        }

        internal static void VerifyNonNegativeMultiplierOfEm(string propertyName, ref double value)
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException(SR.Format(SR.PropertyValueCannotBeNaN, propertyName));
            }
            else if (value > Constants.GreatestMutiplierOfEm)
            {
                value = Constants.GreatestMutiplierOfEm;
            }
            else if (value < 0)
            {
                throw new ArgumentException(SR.Format(SR.PropertyCannotBeNegative, propertyName));
            }
        }

        private double GetAttributeAsDouble()
        {
            string attrValue = GetAttributeValue();
            if (!double.TryParse(attrValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double value))
            {
                FailAttributeValue();
            }

            return value;
        }

        private XmlLanguage GetAttributeAsXmlLanguage()
        {
            string attrValue = GetAttributeValue();
            try
            {
                return XmlLanguage.GetLanguage(attrValue);
            }
            catch (ArgumentException)
            {
                FailAttributeValue();
                return null;
            }
        }

        /// <summary>
        /// Gets the value of the value of the current node which is assumed to be an
        /// attribute. Checks for markup expressions or escaped braces.
        /// </summary>
        private string GetAttributeValue()
        {
            string s = _reader.Value;
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            if (s[0] == '{')
            {
                if (s.Length > 1 && s[1] == '}')
                {
                    s = s.Substring(2);
                }
                else
                {
                    FailAttributeValue();
                }
            }

            return s;
        }

        private const NumberStyles UnsignedDecimalPointStyle =
            NumberStyles.AllowTrailingWhite | 
            NumberStyles.AllowLeadingWhite  | 
            NumberStyles.AllowDecimalPoint;
 
        private const NumberStyles SignedDecimalPointStyle = UnsignedDecimalPointStyle | NumberStyles.AllowLeadingSign;

        /// <summary>
        /// Reads the specified composite font file.
        /// </summary>
        internal static CompositeFontInfo LoadXml(Stream fileStream)
        {
            CompositeFontParser parser = new CompositeFontParser(fileStream);
            return parser._compositeFontInfo;
        }

        /// <summary>
        /// Constructs the composite font parser and parses the file.
        /// </summary>
        /// <param name="fileStream">File stream to parse.</param>
        private CompositeFontParser(Stream fileStream)
        {
            _compositeFontInfo = new CompositeFontInfo();

            _namespaceMap = new Hashtable();

            _reader = CreateXmlReader(fileStream);

            try
            {
                if (IsStartElement(FontFamilyElement, CompositeFontNamespace))
                {
                    ParseFontFamilyElement();
                }
                // DevDiv:1158540
                // CompositeFont files have been modified to allow a family collection in order to select
                // a family based on OS versions.
                else if (IsStartElement(FontFamilyCollectionElement, CompositeFontNamespace))
                {
                    ParseFontFamilyCollectionElement();
                }
                else
                {
                    FailUnknownElement();
                }
            }
            catch (LiteXmlException x)
            {
                FailNotWellFormed(x);
            }
            catch (FormatException x)
            {
                if (_reader.NodeType == LiteXmlNodeType.Attribute)
                    FailAttributeValue(x);
                else
                    Fail(x.Message, x);
            }
            catch (ArgumentException x)
            {
                if (_reader.NodeType == LiteXmlNodeType.Attribute)
                    FailAttributeValue(x);
                else
                    Fail(x.Message, x);
            }
            finally
            {
                _reader.Close();
                _reader = null;
            }
        }


        /// <summary>
        /// Creates the XML reader for the specified file.
        /// </summary>
        private LiteXmlReader CreateXmlReader(Stream fileStream)
        {
            return new LiteXmlReader(fileStream);
        }

        
        /// <summary>
        /// Determines whether a given XML namespace is "known" (i.e., should always be processed)
        /// or "unknown" (i.e., should be skipped if declared ignorable).
        /// </summary>
        /// <param name="xmlNamespace">XML namespace to look up.</param>
        /// <param name="newXmlNamespace">Other namespace to map to, if any. Used for versioning.
        /// This implementation always returns null in this parameter.</param>
        /// <returns>
        /// Returns true ("known") for the XAML namespace or the composite font namespace.
        /// for which a Mapping PI exists.
        /// </returns>
        /// <remarks>
        /// System.String is the only object in a mapped namespace that we can instantiate. However, 
        /// we don't want to ignore any mapped namespaces for compatibility reasons. In general, it's 
        /// better for us to reject valid XAML than to accept invalid XAML. We therefore don't want
        /// to ignore an element which the XAML parser would not ignore -- the ignored element might
        /// be invalid. If mapped elements other than System.String are needed in future versions, a 
        /// composite font author can achieve backwards compatibility by conditionalizing mapped 
        /// elements using c:AlternateContent markup.
        /// </remarks>
        private bool IsXmlNamespaceSupported(string xmlNamespace, out string newXmlNamespace)
        {
            newXmlNamespace = null;
            return xmlNamespace == CompositeFontNamespace || 
                xmlNamespace == XamlNamespace || 
                IsMappedNamespace(xmlNamespace);
        }

        /// <summary>
        /// Calls MoveToContent and checks whether the reader is positioned on the specified element.
        /// </summary>
        /// <remarks>
        /// We should always call this method instead of calling _reader.IsStartElement directly because
        /// the latter calls MoveToContent on the underlying reader which means we could fail to parse
        /// Mapping processing instructions.
        /// </remarks>
        private bool IsStartElement(string localName, string namespaceURI)
        {
            MoveToContent();
            return _reader.IsStartElement(localName, namespaceURI);
        }

        /// <summary>
        /// Same semantics as XmlReader.MoveToContent, but this method processes Mapping processing
        /// instructions as it advances past them.
        /// </summary>
        private LiteXmlNodeType MoveToContent()
        {
            bool contentNode = false;

            do
            {
                switch (_reader.NodeType)
                {
                    case LiteXmlNodeType.CDATA:
                    case LiteXmlNodeType.Element:
                    case LiteXmlNodeType.EndElement:
                    case LiteXmlNodeType.EntityReference:
                    case LiteXmlNodeType.EndEntity:
                        contentNode = true;
                        break;
                }
            } while (!contentNode && _reader.Read());

            return _reader.NodeType;
        }

        #region ProcessingInstructions

       private bool IsMappedNamespace(string xmlNamespace)
        {
            return _namespaceMap.ContainsKey(xmlNamespace);
        }

        private bool IsSystemNamespace(string xmlNamespace)
        {
            return (xmlNamespace == "clr-namespace:System;assembly=mscorlib");
        }

        #endregion ProcessingInstructions

        /// <summary>
        /// Find the OS specific font family from a collection in the CompositeFont
        /// 
        /// We must compare the OS in the font family against the current version.  The CompositeFont file
        /// is in descending OS order.
        /// </summary>
        private void ParseFontFamilyCollectionElement()
        {
            bool foundOsSection = false;

            OperatingSystemVersion fontFamilyOsVersion;

            while (_reader.Read())
            {
                // Once we find a FontFamilyElement with the proper OS attribute, parse it
                if (Enum.TryParse(_reader.GetAttribute("OS"), out fontFamilyOsVersion)
                    && OSVersionHelper.IsOsVersionOrGreater(fontFamilyOsVersion))
                {
                    foundOsSection = true;
                    ParseFontFamilyElement();
                    return;
                }
            }

            if (!foundOsSection)
            {
                Fail(string.Format("No FontFamily element found in FontFamilyCollection that matches current OS or greater: {0}", OSVersionHelper.GetOsVersion().ToString()));
            }
        }

        /// <summary>
        /// Parses the FontFamily element, including its attributes and children,
        /// and advances to the next sibling element.
        /// </summary>
        private void ParseFontFamilyElement()
        {
            // Iterate over the attributes.
            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    // Process attributes in the composite font namespace
                    if (IsCompositeFontAttribute())
                    {
                        string name = _reader.LocalName;

                        if (name == BaselineAttribute)
                        {
                            _compositeFontInfo.Baseline = GetAttributeAsDouble();
                        }
                        else if (name == LineSpacingAttribute)
                        {
                            _compositeFontInfo.LineSpacing = GetAttributeAsDouble();
                        }
                        // DevDiv:1158540
                        // We have to ignore the newly added OS attribute since it is just
                        // used in FontFamilyElement selection, not for anything else.
                        else if (name != OsAttribute)
                        {
                            FailUnknownAttribute();
                        }
                    }
                    else if (!IsIgnorableAttribute())
                    {
                        FailUnknownAttribute();
                    }
} while (_reader.MoveToNextAttribute());

                _reader.MoveToElement();
            }

            // Empty element?
            if (_reader.IsEmptyElement)
            {
                VerifyCompositeFontInfo();
                _reader.Read();
                return;
            }

            // Advance past the start tag.
            _reader.Read();

            // Iterate over children.
            while (MoveToContent() != LiteXmlNodeType.EndElement)
            {
                if (_reader.NodeType == LiteXmlNodeType.Element && _reader.NamespaceURI == CompositeFontNamespace)
                {
                    bool isEmpty = _reader.IsEmptyElement;

                    // It's an element in the composite font namespace; branch depending on the name.
                    switch (_reader.LocalName)
                    {
                        case FamilyNamesPropertyElement:
                            VerifyNoAttributes();
                            _reader.Read();
                            if (!isEmpty)
                            {
                                // Process all child elements.
                                while (MoveToContent() == LiteXmlNodeType.Element)
                                {
                                    if (_reader.LocalName == StringElement && IsSystemNamespace(_reader.NamespaceURI))
                                    {
                                        // It's a System.String.
                                        ParseFamilyNameElement();
                                    }
                                    else
                                    {
                                        // Only System.String is valid in this context.
                                        FailUnknownElement();
                                    }
                                }

                                // Advance past the </FontFamily.FamilyNames> end element, or throw
                                // an exception if we're not at an end element.
                                _reader.ReadEndElement();
                            }
                            break;

                        case FamilyTypefacesPropertyElement:
                            VerifyNoAttributes();
                            _reader.Read();
                            if (!isEmpty)
                            {
                                // Process child elements, of which the only one we recognize in this
                                // context is FamilyTypeface.
                                while (IsStartElement(FamilyTypefaceElement, CompositeFontNamespace))
                                {
                                    ParseFamilyTypefaceElement();
                                }

                                // Advance past the </FontFamily.FamilyTypefaces> end element, or throw
                                // an exception if we're not at an end element.
                                _reader.ReadEndElement();
                            }
                            break;

                        case FamilyMapsPropertyElement:
                            VerifyNoAttributes();
                            _reader.Read();
                            if (!isEmpty)
                            {
                                // Process child elements, of which the only one we recognize in this
                                // context is FontFamilyMap.
                                while (IsStartElement(FamilyMapElement, CompositeFontNamespace))
                                {
                                    ParseFamilyMapElement();
                                }

                                // Advance past the </FontFamily.FamilyTypefaces> end element, or throw
                                // an exception if we're not at an end element.
                                _reader.ReadEndElement();
                            }
                            break;

                        default:
                            // It's some other element.
                            FailUnknownElement();
                            break;
                    }
                }
                else
                {
                    // It's some other content besides an element in the composite font namespace; skip it.
                    _reader.Skip();
                }
            }

            // We should now have read right up to the </FontFamily> end tag.
            VerifyCompositeFontInfo();
            _reader.ReadEndElement();
        }

        /// <summary>
        /// Makes sure the current element has no attributes (except ignorable ones).
        /// </summary>
        private void VerifyNoAttributes()
        {
            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    if (!IsIgnorableAttribute())
                        FailUnknownAttribute();
} while (_reader.MoveToNextAttribute());

                _reader.MoveToElement();
            }
        }

        /// <summary>
        /// Parses the FamilyName element (actually String), including its attributes 
        /// and children, and advances to the next sibling element.
        /// </summary>
        private void ParseFamilyNameElement()
        {
            XmlLanguage language = null;

            // Iterate over the attributes.
            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    if (_reader.NamespaceURI == XamlNamespace && _reader.LocalName == KeyAttribute)
                    {
                        language = GetAttributeAsXmlLanguage();
                    }
                    else if (!IsIgnorableAttribute())
                    {
                        FailUnknownAttribute();
                    }
} while (_reader.MoveToNextAttribute());

                _reader.MoveToElement();
            }

            // XAML requires x:Key so we should, too.
            if (language == null)
            {
                FailMissingAttribute(LanguageAttribute);
            }

            // The family name is the element content.
            string familyName = _reader.ReadElementString();
            if (string.IsNullOrEmpty(familyName))
            {
                FailMissingAttribute(NameAttribute);
            }

            _compositeFontInfo.FamilyNames.Add(language, familyName);
        }

        /// <summary>
        /// Parses the FamilyTypeface element, including its attributes and children,
        /// and advances to the next sibling element.
        /// </summary>
        private void ParseFamilyTypefaceElement()
        {
            FamilyTypeface face = new FamilyTypeface();

            ParseFamilyTypefaceAttributes(face);

            if (_reader.IsEmptyElement)
            {
                _reader.Read();
            }
            else
            {
                _reader.Read();

                while (MoveToContent() != LiteXmlNodeType.EndElement)
                {
                    if (_reader.NodeType == LiteXmlNodeType.Element && _reader.NamespaceURI == CompositeFontNamespace)
                    {
                        if (_reader.LocalName == DeviceFontCharacterMetricsPropertyElement)
                        {
                            VerifyNoAttributes();

                            if (_reader.IsEmptyElement)
                            {
                                _reader.Read();
                            }
                            else
                            {
                                _reader.Read();

                                // Process all child elements.
                                while (MoveToContent() == LiteXmlNodeType.Element)
                                {
                                    if (_reader.LocalName == CharacterMetricsElement)
                                    {
                                        ParseCharacterMetricsElement(face);
                                    }
                                    else
                                    {
                                        // Only CharacterMetricsElement is valid in this context.
                                        FailUnknownElement();
                                    }
                                }
                                // Process the end element for the collection.
                                _reader.ReadEndElement();
                            }
                        }
                        else
                        {
                            FailUnknownElement();
                        }
                    }
                    else
                    {
                        _reader.Skip();
                    }
                }

                _reader.ReadEndElement();
            }

            // Add the typeface.
            _compositeFontInfo.GetFamilyTypefaceList().Add(face);
        }

        /// <summary>
        /// Parses the attributes of the FamilyTypeface element and sets the corresponding
        /// properties on the specified FamilyTypeface object. On return, the reader remains
        /// positioned on the element.
        /// </summary>
        private void ParseFamilyTypefaceAttributes(FamilyTypeface face)
        {
            // Iterate over the attributes.
            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    // Process attributes in the composite font namespace; ignore any others.
                    if (IsCompositeFontAttribute())
                    {
                        string name = _reader.LocalName;

                        if (name == StyleAttribute)
                        {
                            FontStyle fontStyle = new FontStyle();
                            if (!FontStyles.FontStyleStringToKnownStyle(GetAttributeValue(), CultureInfo.InvariantCulture, ref fontStyle))
                                FailAttributeValue();

                            face.Style = fontStyle;
                        }
                        else if (name == WeightAttribute)
                        {
                            FontWeight fontWeight = new FontWeight();
                            if (!FontWeights.FontWeightStringToKnownWeight(GetAttributeValue(), CultureInfo.InvariantCulture, ref fontWeight))
                                FailAttributeValue();

                            face.Weight = fontWeight;
                        }
                        else if (name == StretchAttribute)
                        {
                            FontStretch fontStretch = new FontStretch();
                            if (!FontStretches.FontStretchStringToKnownStretch(GetAttributeValue(), CultureInfo.InvariantCulture, ref fontStretch))
                                FailAttributeValue();

                            face.Stretch = fontStretch;
                        }
                        else if (name == UnderlinePositionAttribute)
                        {
                            face.UnderlinePosition = GetAttributeAsDouble();
                        }
                        else if (name == UnderlineThicknessAttribute)
                        {
                            face.UnderlineThickness = GetAttributeAsDouble();
                        }
                        else if (name == StrikethroughPositionAttribute)
                        {
                            face.StrikethroughPosition = GetAttributeAsDouble();
                        }
                        else if (name == StrikethroughThicknessAttribute)
                        {
                            face.StrikethroughThickness = GetAttributeAsDouble();
                        }
                        else if (name == CapsHeightAttribute)
                        {
                            face.CapsHeight = GetAttributeAsDouble();
                        }
                        else if (name == XHeightAttribute)
                        {
                            face.XHeight = GetAttributeAsDouble();
                        }
                        else if (name == DeviceFontNameAttribute)
                        {
                            face.DeviceFontName = GetAttributeValue();
                        }
                        else
                        {
                            FailUnknownAttribute();
                        }
                    }
                    else if (!IsIgnorableAttribute())
                    {
                        FailUnknownAttribute();
                    }
} while (_reader.MoveToNextAttribute());

                _reader.MoveToElement();
            }
        }

        /// <summary>
        /// Parses a CharacterMetrics element, and advances the current position beyond the
        /// element. Adds a CharacterMetrics object to the given FamilyTypface.
        /// </summary>
        private void ParseCharacterMetricsElement(FamilyTypeface face)
        {
            string key = null;
            string metrics = null;

            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    if (_reader.NamespaceURI == XamlNamespace && _reader.LocalName == KeyAttribute)
                    {
                        key = GetAttributeValue();
                    }
                    else if (IsCompositeFontAttribute() && _reader.LocalName == MetricsAttribute)
                    {
                        metrics = GetAttributeValue();
                    }
                    else if (!IsIgnorableAttribute())
                    {
                        FailUnknownAttribute();
                    }
                } while (_reader.MoveToNextAttribute());

                _reader.MoveToElement();
            }

            if (key == null)
                FailMissingAttribute(KeyAttribute);

            if (metrics == null)
                FailMissingAttribute(MetricsAttribute);

            face.DeviceFontCharacterMetrics.Add(
                CharacterMetricsDictionary.ConvertKey(key),
                new CharacterMetrics(metrics)
                );

            // There should be no child elements.
            ParseEmptyElement();
        }

        /// <summary>
        /// Parses the FontFamilyMap element, including its attributes and children,
        /// and advances to the next sibling element.
        /// </summary>
        private void ParseFamilyMapElement()
        {
            FontFamilyMap fmap = new FontFamilyMap();

            // Parse the family map attributes.
            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    // Process attributes in the composite font namespace; ignore any others.
                    if (IsCompositeFontAttribute())
                    {
                        string name = _reader.LocalName;

                        if (name == UnicodeAttribute)
                        {
                            fmap.Unicode = GetAttributeValue();
                        }
                        else if (name == TargetAttribute)
                        {
                            fmap.Target = GetAttributeValue();
                        }
                        else if (name == ScaleAttribute)
                        {
                            fmap.Scale = GetAttributeAsDouble();
                        }
                        else if (name == LanguageAttribute)
                        {
                            fmap.Language = GetAttributeAsXmlLanguage();
                        }
                        else
                        {
                            FailUnknownAttribute();
                        }
                    }
                    else if (!IsIgnorableAttribute())
                    {
                        FailUnknownAttribute();
                    }
} while (_reader.MoveToNextAttribute());

                _reader.MoveToElement();
            }

            _compositeFontInfo.FamilyMaps.Add(fmap);

            // There should be no child elements.
            ParseEmptyElement();
        }

        /// <summary>
        /// Advances past the current element and its children, throwing and exception 
        /// if there are any child elements in the composite font namespace.
        /// </summary>
        private void ParseEmptyElement()
        {
            if (_reader.IsEmptyElement)
            {
                _reader.Read();
                return;
            }

            _reader.Read();

            while (MoveToContent() != LiteXmlNodeType.EndElement)
            {
                if (_reader.NodeType == LiteXmlNodeType.Element && _reader.NamespaceURI == CompositeFontNamespace)
                {
                    FailUnknownElement();
                }
                else
                {
                    _reader.Skip();
                }
            }

            _reader.ReadEndElement();
        }

        /// <summary>
        /// Determines whether the reader is positioned on an composite font attribute, 
        /// which we define to me either (a) it has no namespace at all, or (b) it's in 
        /// the composite font namespace.
        /// </summary>
        private bool IsCompositeFontAttribute()
        {
            string ns = _reader.NamespaceURI;
            return string.IsNullOrEmpty(ns) || ns == CompositeFontNamespace;
        }

        /// <summary>
        /// Determines whether the attribute can be safely ignored, even if it
        /// has not been explicitly declared as ignorable via compatibility markup.
        /// Currently, we ignore attributes defined by the XML and XML namespaces
        /// standards.
        /// </summary>
        private bool IsIgnorableAttribute()
        {
            string ns = _reader.NamespaceURI;
            return ns == XmlNamespace || ns == XmlnsNamespace;
        }

        #region error reporting

        /// <summary>
        /// Make sure the minimum required information is specified.
        /// </summary>
        private void VerifyCompositeFontInfo()
        {
            if (_compositeFontInfo.FamilyMaps.Count == 0)
                Fail(SR.Format(SR.CompositeFontMissingElement, FamilyMapElement));

            if (_compositeFontInfo.FamilyNames.Count == 0)
                Fail(SR.Format(SR.CompositeFontMissingElement, StringElement));
        }

        /// <summary>
        /// Fail because of an XML exception.
        /// </summary>
        private void FailNotWellFormed(Exception x)
        {
            throw new FileFormatException(new Uri(_reader.BaseURI, UriKind.RelativeOrAbsolute), x);
        }

        /// <summary>
        /// Fail because of an incorrect attribute value.
        /// </summary>
        private void FailAttributeValue()
        {
            Fail(SR.Format(
                SR.CompositeFontAttributeValue1,
                _reader.LocalName));
        }

        /// <summary>
        /// Fail because of an incorrect attribute value with an inner exception.
        /// </summary>
        private void FailAttributeValue(Exception x)
        {
            Fail(SR.Format(
                SR.CompositeFontAttributeValue2,
                _reader.LocalName,
                x.Message),
                x);
        }

        /// <summary>
        /// Fail because of an unknown element.
        /// </summary>
        private void FailUnknownElement()
        {
            Fail(SR.Format(
                SR.CompositeFontUnknownElement,
                _reader.LocalName,
                _reader.NamespaceURI));
        }

        /// <summary>
        /// Fail because of an unknown attribute.
        /// </summary>
        private void FailUnknownAttribute()
        {
            Fail(SR.Format(
                SR.CompositeFontUnknownAttribute,
                _reader.LocalName,
                _reader.NamespaceURI));
        }
        
        /// <summary>
        /// Fail because a required attribute is not present.
        /// </summary>
        /// <param name="name"></param>
        private void FailMissingAttribute(string name)
        {
            Fail(SR.Format(SR.CompositeFontMissingAttribute, name));
        }

        /// <summary>
        ///  Fail with a specified error message.
        /// </summary>
        private void Fail(string message)
        {
            Fail(message, null);
        }

        /// <summary>
        /// Fail with a specified error message and inner exception.
        /// </summary>
        private void Fail(string message, Exception innerException)
        {
            string fileName = _reader.BaseURI;
            throw new FileFormatException(new Uri(fileName, UriKind.RelativeOrAbsolute), message, innerException);
        }
        #endregion

        private CompositeFontInfo _compositeFontInfo;

        private LiteXmlReader _reader;

        // XML namespaces for which Mapping processing instructions have been read. For each entry,
        // the key is the XML namespace, and the value is either SystemClrNamespace (if the the 
        // Mapping PI specifies "System" and "MSCORLIB") or String.Empty (any other Mapping).
        private Hashtable _namespaceMap;

        private const string SystemClrNamespace = "System";

        private const string CompositeFontNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/composite-font";
        private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
        private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
        private const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

        // DevDiv:1158540
        // Adding new collection element to hold multiple FontFamilyElements
        private const string FontFamilyCollectionElement = "FontFamilyCollection";
        private const string FontFamilyElement = "FontFamily";
        private const string BaselineAttribute = "Baseline";
        private const string LineSpacingAttribute = "LineSpacing";
        private const string FamilyNamesPropertyElement = "FontFamily.FamilyNames";
        private const string StringElement = "String";
        private const string FamilyTypefacesPropertyElement = "FontFamily.FamilyTypefaces";
        private const string FamilyTypefaceElement = "FamilyTypeface";
        private const string FamilyMapsPropertyElement = "FontFamily.FamilyMaps";
        private const string FamilyMapElement = "FontFamilyMap";
        private const string KeyAttribute = "Key";
        private const string LanguageAttribute = "Language";
        private const string NameAttribute = "Name";
        private const string StyleAttribute = "Style";
        private const string WeightAttribute = "Weight";
        private const string StretchAttribute = "Stretch";
        private const string UnderlinePositionAttribute = "UnderlinePosition";
        private const string UnderlineThicknessAttribute = "UnderlineThickness";
        private const string StrikethroughPositionAttribute = "StrikethroughPosition";
        private const string StrikethroughThicknessAttribute = "StrikethroughThickness";
        private const string CapsHeightAttribute = "CapsHeight";
        private const string XHeightAttribute = "XHeight";
        private const string UnicodeAttribute = "Unicode";
        private const string TargetAttribute = "Target";
        private const string ScaleAttribute = "Scale";
        private const string DeviceFontNameAttribute = "DeviceFontName";
        private const string DeviceFontCharacterMetricsPropertyElement = "FamilyTypeface.DeviceFontCharacterMetrics";
        private const string CharacterMetricsElement = "CharacterMetrics";
        private const string MetricsAttribute = "Metrics";

        // DevDiv:1158540
        // FontFamilyElements can now have OS specific attributes
        private const string OsAttribute = "OS";
    }

    internal enum LiteXmlNodeType
    {
        None = 0,
        Element = 1,
        Attribute = 2,
        Text = 3,
        CDATA = 4,
        EntityReference = 5,
        ProcessingInstruction = 7,
        Comment = 8,
        Whitespace = 13,
        EndElement = 15,
        EndEntity = 16,
    }

    internal sealed class LiteXmlException : Exception
    {
        public LiteXmlException(string message) : base(message) { }
    }

    /// <summary>
    /// Lightweight standalone XML pull-reader for CompositeFont files.
    /// Avoids pulling System.Private.Xml (550 KB+) into every NativeAOT UI app.
    /// </summary>
    internal sealed class LiteXmlReader
    {
        private struct AttrEntry
        {
            public string RawName;
            public string LocalName;
            public string Prefix;
            public string NamespaceURI;
            public string Value;
        }

        private readonly Stream _stream;
        private readonly string _text;
        private int _pos;

        private readonly List<Dictionary<string, string>> _nsScopes = new List<Dictionary<string, string>>();
        private readonly List<AttrEntry> _attrs = new List<AttrEntry>();
        private int _attrIndex = -1;

        private string _elemLocalName = string.Empty;
        private string _elemNamespaceURI = string.Empty;
        private bool _elemIsEmpty;

        public LiteXmlNodeType NodeType { get; private set; } = LiteXmlNodeType.None;
        public string LocalName { get; private set; } = string.Empty;
        public string NamespaceURI { get; private set; } = string.Empty;
        public string Value { get; private set; } = string.Empty;
        public bool IsEmptyElement => NodeType == LiteXmlNodeType.Element && _elemIsEmpty;
        public string BaseURI => string.Empty;

        public LiteXmlReader(Stream stream)
        {
            _stream = stream;
            using var sr = new StreamReader(stream, leaveOpen: true);
            _text = sr.ReadToEnd();
            _nsScopes.Add(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["xml"] = "http://www.w3.org/XML/1998/namespace",
                ["xmlns"] = "http://www.w3.org/2000/xmlns/",
                [""] = string.Empty,
            });
        }

        public void Close()
        {
            _stream?.Dispose();
        }

        public bool IsStartElement(string localName, string namespaceURI)
        {
            MoveToContent();
            return NodeType == LiteXmlNodeType.Element && LocalName == localName && NamespaceURI == namespaceURI;
        }

        private LiteXmlNodeType MoveToContent()
        {
            if (NodeType == LiteXmlNodeType.Attribute) MoveToElement();
            while (true)
            {
                if (NodeType == LiteXmlNodeType.Element ||
                    NodeType == LiteXmlNodeType.EndElement ||
                    NodeType == LiteXmlNodeType.CDATA ||
                    NodeType == LiteXmlNodeType.EntityReference ||
                    NodeType == LiteXmlNodeType.EndEntity)
                {
                    return NodeType;
                }
                if (NodeType == LiteXmlNodeType.Text && !string.IsNullOrWhiteSpace(Value))
                {
                    return NodeType;
                }
                if (!Read()) return LiteXmlNodeType.None;
            }
        }

        public string GetAttribute(string name)
        {
            if (NodeType != LiteXmlNodeType.Element && NodeType != LiteXmlNodeType.Attribute)
            {
                return null;
            }
            for (int i = 0; i < _attrs.Count; i++)
            {
                if (_attrs[i].RawName == name || _attrs[i].LocalName == name)
                {
                    return _attrs[i].Value;
                }
            }
            return null;
        }

        public bool MoveToFirstAttribute()
        {
            if (_attrs.Count == 0 || (NodeType != LiteXmlNodeType.Element && NodeType != LiteXmlNodeType.Attribute))
            {
                return false;
            }
            _attrIndex = 0;
            LoadCurrentAttr();
            return true;
        }

        public bool MoveToNextAttribute()
        {
            if (_attrIndex < 0 || _attrIndex + 1 >= _attrs.Count)
            {
                return false;
            }
            _attrIndex++;
            LoadCurrentAttr();
            return true;
        }

        public void MoveToElement()
        {
            if (NodeType == LiteXmlNodeType.Attribute)
            {
                _attrIndex = -1;
                NodeType = LiteXmlNodeType.Element;
                LocalName = _elemLocalName;
                NamespaceURI = _elemNamespaceURI;
                Value = string.Empty;
            }
        }

        private void LoadCurrentAttr()
        {
            AttrEntry a = _attrs[_attrIndex];
            NodeType = LiteXmlNodeType.Attribute;
            LocalName = a.LocalName;
            NamespaceURI = a.NamespaceURI;
            Value = a.Value;
        }

        public void Skip()
        {
            if (NodeType == LiteXmlNodeType.Attribute) MoveToElement();
            if (NodeType == LiteXmlNodeType.Element)
            {
                if (_elemIsEmpty)
                {
                    Read();
                    return;
                }
                int depth = 1;
                while (Read())
                {
                    if (NodeType == LiteXmlNodeType.Element && !_elemIsEmpty) depth++;
                    else if (NodeType == LiteXmlNodeType.EndElement)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            Read();
                            return;
                        }
                    }
                }
            }
            else
            {
                Read();
            }
        }

        public void ReadEndElement()
        {
            if (MoveToContent() != LiteXmlNodeType.EndElement)
            {
                throw new LiteXmlException($"Expected end element, found {NodeType}.");
            }
            Read();
        }

        public string ReadElementString()
        {
            if (NodeType == LiteXmlNodeType.Attribute) MoveToElement();
            if (NodeType != LiteXmlNodeType.Element)
            {
                throw new LiteXmlException("Expected element.");
            }
            if (_elemIsEmpty)
            {
                Read();
                return string.Empty;
            }
            Read();
            var sb = new System.Text.StringBuilder();
            while (NodeType == LiteXmlNodeType.Text || NodeType == LiteXmlNodeType.Whitespace || NodeType == LiteXmlNodeType.CDATA)
            {
                sb.Append(Value);
                if (!Read()) break;
            }
            if (NodeType == LiteXmlNodeType.EndElement)
            {
                Read();
            }
            return sb.ToString();
        }

        public bool Read()
        {
            // Pop namespace scope when stepping off EndElement or an empty Element
            if ((NodeType == LiteXmlNodeType.EndElement || (NodeType == LiteXmlNodeType.Element && _elemIsEmpty)) && _nsScopes.Count > 1)
            {
                _nsScopes.RemoveAt(_nsScopes.Count - 1);
            }

            _attrs.Clear();
            _attrIndex = -1;
            _elemIsEmpty = false;

            while (_pos < _text.Length)
            {
                if (_text[_pos] == '<')
                {
                    if (StartsWith("<!--"))
                    {
                        int end = _text.IndexOf("-->", _pos + 4, StringComparison.Ordinal);
                        if (end < 0) throw new LiteXmlException("Unterminated XML comment.");
                        _pos = end + 3;
                        continue;
                    }
                    if (StartsWith("<?"))
                    {
                        int end = _text.IndexOf("?>", _pos + 2, StringComparison.Ordinal);
                        if (end < 0) throw new LiteXmlException("Unterminated processing instruction.");
                        _pos = end + 2;
                        continue;
                    }
                    if (StartsWith("<![CDATA["))
                    {
                        int end = _text.IndexOf("]]>", _pos + 9, StringComparison.Ordinal);
                        if (end < 0) throw new LiteXmlException("Unterminated CDATA section.");
                        Value = _text.Substring(_pos + 9, end - (_pos + 9));
                        LocalName = string.Empty;
                        NamespaceURI = string.Empty;
                        NodeType = LiteXmlNodeType.CDATA;
                        _pos = end + 3;
                        return true;
                    }
                    if (_pos + 1 < _text.Length && _text[_pos + 1] == '/')
                    {
                        _pos += 2;
                        SkipWs();
                        string rawEnd = ReadName();
                        SkipWs();
                        if (_pos >= _text.Length || _text[_pos] != '>') throw new LiteXmlException("Malformed end tag.");
                        _pos++;
                        SplitQName(rawEnd, out string pfx, out string local);
                        NodeType = LiteXmlNodeType.EndElement;
                        LocalName = local;
                        NamespaceURI = ResolveNamespace(pfx, isElement: true);
                        Value = string.Empty;
                        return true;
                    }

                    // Start tag
                    _pos++;
                    string rawTag = ReadName();
                    var scope = new Dictionary<string, string>(StringComparer.Ordinal);
                    _nsScopes.Add(scope);

                    while (true)
                    {
                        SkipWs();
                        if (_pos >= _text.Length) throw new LiteXmlException("Unexpected EOF in start tag.");
                        if (_text[_pos] == '/' && _pos + 1 < _text.Length && _text[_pos + 1] == '>')
                        {
                            _elemIsEmpty = true;
                            _pos += 2;
                            break;
                        }
                        if (_text[_pos] == '>')
                        {
                            _elemIsEmpty = false;
                            _pos++;
                            break;
                        }

                        string attrRaw = ReadName();
                        SkipWs();
                        if (_pos >= _text.Length || _text[_pos] != '=') throw new LiteXmlException("Expected '=' after attribute name.");
                        _pos++;
                        SkipWs();
                        if (_pos >= _text.Length || (_text[_pos] != '"' && _text[_pos] != '\'')) throw new LiteXmlException("Expected quoted attribute value.");
                        char quote = _text[_pos++];
                        int valStart = _pos;
                        while (_pos < _text.Length && _text[_pos] != quote) _pos++;
                        if (_pos >= _text.Length) throw new LiteXmlException("Unterminated attribute value.");
                        string val = DecodeEntities(_text.Substring(valStart, _pos - valStart));
                        _pos++; // skip closing quote

                        SplitQName(attrRaw, out string aPfx, out string aLocal);
                        if (attrRaw == "xmlns")
                        {
                            scope[""] = val;
                        }
                        else if (aPfx == "xmlns")
                        {
                            scope[aLocal] = val;
                        }

                        _attrs.Add(new AttrEntry
                        {
                            RawName = attrRaw,
                            Prefix = aPfx,
                            LocalName = aLocal,
                            NamespaceURI = string.Empty,
                            Value = val,
                        });
                    }

                    // Resolve namespaces after reading all xmlns declarations on this element
                    for (int i = 0; i < _attrs.Count; i++)
                    {
                        AttrEntry a = _attrs[i];
                        if (a.RawName == "xmlns" || a.Prefix == "xmlns")
                        {
                            a.NamespaceURI = "http://www.w3.org/2000/xmlns/";
                        }
                        else if (!string.IsNullOrEmpty(a.Prefix))
                        {
                            a.NamespaceURI = ResolveNamespace(a.Prefix, isElement: false);
                        }
                        _attrs[i] = a;
                    }

                    SplitQName(rawTag, out string ePfx, out string eLocal);
                    NodeType = LiteXmlNodeType.Element;
                    _elemLocalName = eLocal;
                    _elemNamespaceURI = ResolveNamespace(ePfx, isElement: true);
                    LocalName = _elemLocalName;
                    NamespaceURI = _elemNamespaceURI;
                    Value = string.Empty;
                    return true;
                }
                else
                {
                    int start = _pos;
                    while (_pos < _text.Length && _text[_pos] != '<') _pos++;
                    string rawText = _text.Substring(start, _pos - start);
                    bool allWs = string.IsNullOrWhiteSpace(rawText);
                    NodeType = allWs ? LiteXmlNodeType.Whitespace : LiteXmlNodeType.Text;
                    LocalName = string.Empty;
                    NamespaceURI = string.Empty;
                    Value = allWs ? rawText : DecodeEntities(rawText);
                    return true;
                }
            }

            NodeType = LiteXmlNodeType.None;
            LocalName = string.Empty;
            NamespaceURI = string.Empty;
            Value = string.Empty;
            return false;
        }

        private bool StartsWith(string prefix)
            => _pos + prefix.Length <= _text.Length && string.Compare(_text, _pos, prefix, 0, prefix.Length, StringComparison.Ordinal) == 0;

        private void SkipWs()
        {
            while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos])) _pos++;
        }

        private string ReadName()
        {
            int start = _pos;
            while (_pos < _text.Length)
            {
                char c = _text[_pos];
                if (char.IsWhiteSpace(c) || c == '=' || c == '>' || c == '/' || c == '<') break;
                _pos++;
            }
            if (_pos == start) throw new LiteXmlException("Expected XML name.");
            return _text.Substring(start, _pos - start);
        }

        private static void SplitQName(string qname, out string prefix, out string localName)
        {
            int colon = qname.IndexOf(':');
            if (colon >= 0)
            {
                prefix = qname.Substring(0, colon);
                localName = qname.Substring(colon + 1);
            }
            else
            {
                prefix = string.Empty;
                localName = qname;
            }
        }

        private string ResolveNamespace(string prefix, bool isElement)
        {
            if (string.IsNullOrEmpty(prefix) && !isElement) return string.Empty;
            for (int i = _nsScopes.Count - 1; i >= 0; i--)
            {
                if (_nsScopes[i].TryGetValue(prefix, out string ns))
                {
                    return ns;
                }
            }
            return string.Empty;
        }

        private static string DecodeEntities(string s)
        {
            if (s.IndexOf('&') < 0) return s;
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '&')
                {
                    int semi = s.IndexOf(';', i + 1);
                    if (semi > i + 1)
                    {
                        string ent = s.Substring(i + 1, semi - i - 1);
                        char? decoded = ent switch
                        {
                            "amp" => '&',
                            "lt" => '<',
                            "gt" => '>',
                            "quot" => '"',
                            "apos" => '\'',
                            _ when ent.StartsWith("#x", StringComparison.OrdinalIgnoreCase) &&
                                   int.TryParse(ent.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hx) => (char)hx,
                            _ when ent.StartsWith("#", StringComparison.Ordinal) &&
                                   int.TryParse(ent.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int dec) => (char)dec,
                            _ => null
                        };
                        if (decoded.HasValue)
                        {
                            sb.Append(decoded.Value);
                            i = semi;
                            continue;
                        }
                    }
                }
                sb.Append(s[i]);
            }
            return sb.ToString();
        }
    }
}


namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Partial class splitted from the original one in LineServices.cs.
    /// We do this to avoid bringing in TextFormatting namespace when 
    /// building FontCacheServices.exe
    /// </summary>
    internal static partial class Constants
    {
        /// <summary>
        /// Greatest multiple of em allowed in composite font file
        /// </summary>
        public const double GreatestMutiplierOfEm = 100;
    }
}
