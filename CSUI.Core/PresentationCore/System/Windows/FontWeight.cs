// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows
{
    /// <summary>
    /// FontWeight structure describes the degree of blackness or thickness of strokes of characters in a font.
    /// </summary>
    public struct FontWeight : IFormattable
    {
        internal FontWeight(int weight)
        {
            Debug.Assert(1 <= weight && weight <= 999);

            // We want the default zero value of new FontWeight() to correspond to FontWeights.Normal.
            // Therefore, the _weight value is shifted by 400 relative to the OpenType weight value.
            _weight = weight - 400;
        }

        /// <summary>
        /// Creates a new FontWeight object that corresponds to the OpenType usWeightClass value.
        /// </summary>
        /// <param name="weightValue">An integer value between 1 and 999 that corresponds
        /// to the usWeightClass definition in the OpenType specification.</param>
        /// <returns>A new FontWeight object that corresponds to the weightValue parameter.</returns>
        public static FontWeight FromOpenTypeWeight(int weightValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(weightValue, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(weightValue, 999);
            return new FontWeight(weightValue);
        }

        /// <summary>
        /// Obtains OpenType usWeightClass value that corresponds to the FontWeight object.
        /// </summary>
        /// <returns>An integer value between 1 and 999 that corresponds
        /// to the usWeightClass definition in the OpenType specification.</returns>
        public int ToOpenTypeWeight()
        {
            return RealWeight;
        }

        /// <summary>
        /// Compares two font weight values and returns an indication of their relative values.
        /// </summary>
        public static int Compare(FontWeight left, FontWeight right)
        {
            return left._weight - right._weight;
        }

        public static bool operator <(FontWeight left, FontWeight right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator <=(FontWeight left, FontWeight right)
        {
            return Compare(left, right) <= 0;
        }

        public static bool operator >(FontWeight left, FontWeight right)
        {
            return Compare(left, right) > 0;
        }

        public static bool operator >=(FontWeight left, FontWeight right)
        {
            return Compare(left, right) >= 0;
        }

        public static bool operator ==(FontWeight left, FontWeight right)
        {
            return Compare(left, right) == 0;
        }

        public static bool operator !=(FontWeight left, FontWeight right)
        {
            return !(left == right);
        }

        public bool Equals(FontWeight obj)
        {
            return this == obj;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FontWeight))
                return false;
            return this == (FontWeight)obj;
        }

        public override int GetHashCode()
        {
            return RealWeight;
        }

        public override string ToString()
        {
            return ConvertToString(null, null);
        }

        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            return ConvertToString(format, provider);
        }

        private string ConvertToString(string format, IFormatProvider provider)
        {
            string convertedValue;
            if (!FontWeights.FontWeightToString(RealWeight, out convertedValue))
            {
                // This can happen if _weight value is not a multiple of 100.
                return RealWeight.ToString(provider);
            }
            return convertedValue;
        }

        private int RealWeight
        {
            get
            {
                return _weight + 400;
            }
        }

        private int _weight;
    }
}
