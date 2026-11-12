// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//  Description: The CultureMapper class implements static methods for mapping
//               CultureInfo objects from WPF clients to CultureInfo objects
//               that can be used internally by text formatting code.
//
//

using System.Globalization;
using System.Windows.Markup;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Implements static methods for mapping CultureInfo objects.
    /// </summary>
    internal static class CultureMapper
    {
        /// <summary>
        /// Returns a specific culture given an arbitrary CultureInfo, which may be null, the invariant
        /// culture, or a neutral culture.
        /// </summary>
        public static CultureInfo GetSpecificCulture(CultureInfo? runCulture)
        {
            // Assume default culture unless we can do better.
            CultureInfo specificCulture = TypeConverterHelper.InvariantEnglishUS;

            if (runCulture != null)
            {
                // Assign _cachedCultureMap to a local variable for thread safety. The reference assignment
                // is atomic and the CachedCultureMap class is immutable.
                CachedCultureMap? cachedCultureMap = _cachedCultureMap;
                if (cachedCultureMap != null && object.ReferenceEquals(cachedCultureMap.OriginalCulture, runCulture))
                    return cachedCultureMap.SpecificCulture;

                if (runCulture != CultureInfo.InvariantCulture)
                {
                    if (!runCulture.IsNeutralCulture)
                    {
                        // It's already a specific culture (neither neutral nor InvariantCulture)
                        specificCulture = runCulture;
                    }
                    else
                    {
                        string cultureName = runCulture.Name;
                        if (!string.IsNullOrEmpty(cultureName))
                        {
                            try
                            {
                                CultureInfo culture = CultureInfo.CreateSpecificCulture(cultureName);
                                specificCulture = CultureInfo.GetCultureInfoByIetfLanguageTag(culture.IetfLanguageTag);
                            }
                            catch (ArgumentException)
                            {
                                specificCulture = TypeConverterHelper.InvariantEnglishUS;
                            }
                        }
                    }
                }

                // Save the mapping so the next call will be fast if we're given the same runCulture.
                _cachedCultureMap = new CachedCultureMap(runCulture, specificCulture);
            }

            return specificCulture;
        }

        private class CachedCultureMap
        {
            public CachedCultureMap(CultureInfo originalCulture, CultureInfo specificCulture)
            {
                _originalCulture = originalCulture;
                _specificCulture = specificCulture;
            }

            public CultureInfo OriginalCulture
            {
                get { return _originalCulture; }
            }

            public CultureInfo SpecificCulture
            {
                get { return _specificCulture; }
            }

            private readonly CultureInfo _originalCulture;
            private readonly CultureInfo _specificCulture;
        }

        private static CachedCultureMap? _cachedCultureMap = null;
    }
}
