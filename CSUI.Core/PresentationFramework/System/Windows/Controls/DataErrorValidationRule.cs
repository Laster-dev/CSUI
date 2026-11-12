// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a rule that checks for errors raised by an <see cref="IDataErrorInfo"/> object.
    /// </summary>
    public sealed class DataErrorValidationRule : ValidationRule
    {
        internal static readonly DataErrorValidationRule Instance = new DataErrorValidationRule();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataErrorValidationRule"/> class.
        /// </summary>
        public DataErrorValidationRule()
            : base(ValidationStep.UpdatedValue, true)
        {
        }

        /// <summary>
        /// Performs validation checks on a value using <see cref="IDataErrorInfo"/>.
        /// </summary>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is BindingGroup bindingGroup)
            {
                IList items = bindingGroup.Items;
                for (int i = items.Count - 1; i >= 0; --i)
                {
                    if (items[i] is IDataErrorInfo itemErrorInfo)
                    {
                        string error = itemErrorInfo.Error;
                        if (!string.IsNullOrEmpty(error))
                        {
                            return new ValidationResult(false, error);
                        }
                    }
                }
            }
            else if (value is BindingExpression bindingExpression)
            {
                if (bindingExpression.ResolvedSource is IDataErrorInfo itemErrorInfo)
                {
                    string propertyName = bindingExpression.ResolvedSourcePropertyName;
                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        string error = itemErrorInfo[propertyName];
                        if (!string.IsNullOrEmpty(error))
                        {
                            return new ValidationResult(false, error);
                        }
                    }
                }
            }
            else if (value is IDataErrorInfo errorInfo)
            {
                string error = errorInfo.Error;
                if (!string.IsNullOrEmpty(error))
                {
                    return new ValidationResult(false, error);
                }
            }

            return ValidationResult.ValidResult;
        }
    }
}
