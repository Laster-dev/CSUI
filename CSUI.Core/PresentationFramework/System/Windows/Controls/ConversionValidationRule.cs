// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;

namespace System.Windows.Controls
{
    /// <summary>
    /// Internal validation rule representing type conversion failures in data binding.
    /// </summary>
    internal sealed class ConversionValidationRule : ValidationRule
    {
        internal static readonly ConversionValidationRule Instance = new ConversionValidationRule();

        internal ConversionValidationRule()
            : base(ValidationStep.ConvertedProposedValue, false)
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return ValidationResult.ValidResult;
        }
    }
}
