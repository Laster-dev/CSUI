// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a rule that checks for errors raised asynchronously by an <see cref="System.ComponentModel.INotifyDataErrorInfo"/> object.
    /// </summary>
    public sealed class NotifyDataErrorValidationRule : ValidationRule
    {
        internal static readonly NotifyDataErrorValidationRule Instance = new NotifyDataErrorValidationRule();

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyDataErrorValidationRule"/> class.
        /// </summary>
        public NotifyDataErrorValidationRule()
            : base(ValidationStep.UpdatedValue, true)
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
