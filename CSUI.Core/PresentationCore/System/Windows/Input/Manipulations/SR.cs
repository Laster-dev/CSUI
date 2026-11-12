// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Windows.Input.Manipulations
{
    internal static class SR
    {
        internal const string ArgumentOutOfRange = "Value of {0} is outside of the allowed range.";
        internal const string CannotChangeParameterDuringInertia = "The value of {0} cannot be changed while the inertia processor is running.";
        internal const string IllegalPivotRadius = "Value of {0} must be either NaN, or a finite value greater than or equal to 1.";
        internal const string InertiaParametersUnspecified1and2 = "Either {0}, or {1} and {2}, needs to be specified.";
        internal const string InertiaParametersUnspecified2 = "Either {0} or {1} needs to be specified.";
        internal const string InertiaProcessorInvalidRadius = "InitialRadius should be a finite number, greater than or equal to 1.";
        internal const string InvalidTimestamp = "Timestamp values must not decrease.";
        internal const string NoInertiaVelocitiesSpecified = "At least one of the following must be specified: linear velocity ({0} or {1}), angular velocity ({2}), or expansion velocity ({3} or {4}).";
        internal const string OnlyProportionalExpansionSupported = "Currently, only proportional expansion is supported. {0} must equal {1}.";
        internal const string ValueMustBeFinite = "Value of {0} must be finite.";
        internal const string ValueMustBeFiniteNonNegative = "Value of {0} must be finite and non-negative.";
        internal const string ValueMustBeFiniteOrNaN = "Value of {0} must be finite or NaN.";
    }
}
