// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using MS.Internal.FontCache;
using MS.Internal.PresentationCore;

namespace System.Windows.Media
{
    /// <summary>
    /// The NumberSubstitution class specifies how numbers in text
    /// are to be displayed.
    /// </summary>
    public class NumberSubstitution
    {
        /// <summary>
        /// Initializes a NumberSubstitution object with default values.
        /// </summary>
        public NumberSubstitution()
        {
            _source = NumberCultureSource.Text;
            _cultureOverride = null;
            _substitution = NumberSubstitutionMethod.AsCulture;
        }

        /// <summary>
        /// Initializes a NumberSubstitution object with explicit values.
        /// </summary>
        /// <param name="source">Specifies how the number culture is determined.</param>
        /// <param name="cultureOverride">Number culture if NumberCultureSource.Override is specified.</param>
        /// <param name="substitution">Type of number substitution to perform.</param>
        public NumberSubstitution(
            NumberCultureSource source,
            CultureInfo? cultureOverride,
            NumberSubstitutionMethod substitution)
        {
            _source = source;
            _cultureOverride = ThrowIfInvalidCultureOverride(cultureOverride);
            _substitution = substitution;
        }

        /// <summary>
        /// The CultureSource property specifies how the culture for numbers
        /// is determined. The default value is NumberCultureSource.Text,
        /// which means the number culture is the culture of the text run.
        /// </summary>
        public NumberCultureSource CultureSource
        {
            get { return _source; }
            set
            {
                if ((uint)value > (uint)NumberCultureSource.Override)
                    throw new InvalidEnumArgumentException(nameof(CultureSource), (int)value, typeof(NumberCultureSource));

                _source = value;
            }
        }

        /// <summary>
        /// If the CultureSource == NumberCultureSource.Override, this
        /// property specifies the number culture. A value of null is interpreted
        /// as US-English. The default value is null. If CultureSource !=
        /// NumberCultureSource.Override, this property is ignored.
        /// </summary>
        public CultureInfo? CultureOverride
        {
            get { return _cultureOverride; }
            set { _cultureOverride = ThrowIfInvalidCultureOverride(value); }
        }

        /// <summary>
        /// Helper function to throw an exception if invalid value is specified for
        /// CultureOverride property.
        /// </summary>
        private static CultureInfo? ThrowIfInvalidCultureOverride(CultureInfo? culture)
        {
            if (!IsValidCultureOverride(culture))
            {
                throw new ArgumentException(SR.SpecificNumberCultureRequired);
            }
            return culture;
        }

        /// <summary>
        /// Determines whether the specific culture is a valid value for the
        /// CultureOverride property.
        /// </summary>
        private static bool IsValidCultureOverride(CultureInfo? culture)
        {
            // Null culture override is OK, but otherwise it must be a specific culture.
            return
                (culture == null) ||
                !(culture.IsNeutralCulture || culture.Equals(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Weakly typed validation callback for CultureOverride dependency property.
        /// </summary>
        private static bool IsValidCultureOverrideValue(object? value)
        {
            return IsValidCultureOverride(value as CultureInfo) && (value == null || value is CultureInfo);
        }

        /// <summary>
        /// Specifies the type of number substitution to perform, if any.
        /// </summary>
        public NumberSubstitutionMethod Substitution
        {
            get { return _substitution; }
            set
            {
                if ((uint)value > (uint)NumberSubstitutionMethod.Traditional)
                    throw new InvalidEnumArgumentException(nameof(Substitution), (int)value, typeof(NumberSubstitutionMethod));

                _substitution = value;
            }
        }

        /// <summary>
        /// DP For CultureSource
        /// </summary>
        public static readonly DependencyProperty CultureSourceProperty =
            DependencyProperty.RegisterAttached(
                "CultureSource",
                typeof(NumberCultureSource),
                typeof(NumberSubstitution),
                new PropertyMetadata(NumberCultureSource.Text));

        /// <summary>
        /// Setter for NumberSubstitution DependencyProperty
        /// </summary>
        public static void SetCultureSource(DependencyObject target, NumberCultureSource value)
        {
            ArgumentNullException.ThrowIfNull(target);

            target.SetValue(CultureSourceProperty, value);
        }

        /// <summary>
        /// Getter for NumberSubstitution DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static NumberCultureSource GetCultureSource(DependencyObject target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return (NumberCultureSource)(target.GetValue(CultureSourceProperty)!);
        }

        /// <summary>
        /// DP For CultureOverride
        /// </summary>
        public static readonly DependencyProperty CultureOverrideProperty =
            DependencyProperty.RegisterAttached(
                "CultureOverride",
                typeof(CultureInfo),
                typeof(NumberSubstitution),
                null,
                new ValidateValueCallback(IsValidCultureOverrideValue));

        /// <summary>
        /// Setter for NumberSubstitution DependencyProperty
        /// </summary>
        public static void SetCultureOverride(DependencyObject target, CultureInfo? value)
        {
            ArgumentNullException.ThrowIfNull(target);

            target.SetValue(CultureOverrideProperty, value);
        }

        /// <summary>
        /// Getter for NumberSubstitution DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static CultureInfo? GetCultureOverride(DependencyObject target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return (CultureInfo?)(target.GetValue(CultureOverrideProperty));
        }

        /// <summary>
        /// DP For Substitution
        /// </summary>
        public static readonly DependencyProperty SubstitutionProperty =
            DependencyProperty.RegisterAttached(
                "Substitution",
                typeof(NumberSubstitutionMethod),
                typeof(NumberSubstitution),
                new PropertyMetadata(NumberSubstitutionMethod.AsCulture));

        /// <summary>
        /// Setter for NumberSubstitution DependencyProperty
        /// </summary>
        public static void SetSubstitution(DependencyObject target, NumberSubstitutionMethod value)
        {
            ArgumentNullException.ThrowIfNull(target);

            target.SetValue(SubstitutionProperty, value);
        }

        /// <summary>
        /// Getter for NumberSubstitution DependencyProperty
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static NumberSubstitutionMethod GetSubstitution(DependencyObject target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return (NumberSubstitutionMethod)(target.GetValue(SubstitutionProperty)!);
        }

        /// <summary>
        /// Computes hash code for this object.
        /// </summary>
        public override int GetHashCode()
        {
            int hash = HashFn.HashMultiply((int)_source) + (int)_substitution;

            if (_cultureOverride != null)
                hash = HashFn.HashMultiply(hash) + _cultureOverride.GetHashCode();

            return HashFn.HashScramble(hash);
        }

        /// <summary>
        /// Checks whether this object is equal to another NumberSubstitution object.
        /// </summary>
        public override bool Equals(object? obj)
        {
            NumberSubstitution? sub = obj as NumberSubstitution;

            return sub != null &&
                _source == sub._source &&
                _substitution == sub._substitution &&
                (_cultureOverride == null ? (sub._cultureOverride == null) : _cultureOverride.Equals(sub._cultureOverride));
        }

        private NumberCultureSource _source;
        private CultureInfo? _cultureOverride;
        private NumberSubstitutionMethod _substitution;
    }

    /// <summary>
    /// Used with the NumberSubstitution class, specifies how the culture for
    /// numbers in a text run is determined.
    /// </summary>
    public enum NumberCultureSource
    {
        /// <summary>
        /// Number culture is TextRunProperties.CultureInfo, i.e., the culture
        /// of the text run.
        /// </summary>
        Text = 0,

        /// <summary>
        /// Number culture is the culture of the current thread, which by default
        /// is the user default culture.
        /// </summary>
        User = 1,

        /// <summary>
        /// Number culture is NumberSubstitution.CultureOverride.
        /// </summary>
        Override = 2
    }

    /// <summary>
    /// Value of the NumberSubstitution.Substitution property, specifies
    /// the type of number substitution to perform, if any.
    /// </summary>
    public enum NumberSubstitutionMethod
    {
        /// <summary>
        /// Specifies that the substitution method should be determined based
        /// on the number culture's NumberFormat.DigitSubstitution property.
        /// This is the default value.
        /// </summary>
        AsCulture = 0,

        /// <summary>
        /// If the number culture is an Arabic or Farsi culture, specifies that
        /// the digits depend on the context.
        /// </summary>
        Context = 1,

        /// <summary>
        /// Specifies that code points 0x30-0x39 are always rendered as European
        /// digits, i.e., no substitution is performed.
        /// </summary>
        European = 2,

        /// <summary>
        /// Specifies that numbers are rendered using the national digits for
        /// the number culture, as specified by the culture's NumberFormat
        /// property.
        /// </summary>
        NativeNational = 3,

        /// <summary>
        /// Specifies that numbers are rendered using the traditional digits
        /// for the number culture.
        /// </summary>
        Traditional = 4
    }
}
