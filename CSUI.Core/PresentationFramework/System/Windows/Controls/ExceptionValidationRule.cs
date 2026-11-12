// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a rule used to check for exceptions thrown during updates of the binding source property.
    /// </summary>
    public sealed class ExceptionValidationRule : ValidationRule
    {
        internal static readonly ExceptionValidationRule Instance = new ExceptionValidationRule();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionValidationRule"/> class.
        /// </summary>
        public ExceptionValidationRule()
            : base(ValidationStep.CommittedValue, false)
        {
        }

        /// <summary>
        /// Validates the specified value.
        /// </summary>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return ValidationResult.ValidResult;
        }
    }
}
