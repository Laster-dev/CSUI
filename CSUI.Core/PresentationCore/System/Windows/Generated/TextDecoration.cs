// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Media;
using System.Windows.Media.Animation;

namespace System.Windows
{
    public sealed partial class TextDecoration : Animatable
    {
        #region Public Methods

        public new TextDecoration Clone()
        {
            return (TextDecoration)base.Clone();
        }

        public new TextDecoration CloneCurrentValue()
        {
            return (TextDecoration)base.CloneCurrentValue();
        }

        #endregion Public Methods

        #region Public Properties

        public Pen Pen
        {
            get
            {
                return (Pen)GetValue(PenProperty);
            }
            set
            {
                SetValueInternal(PenProperty, value);
            }
        }

        public double PenOffset
        {
            get
            {
                return (double)GetValue(PenOffsetProperty);
            }
            set
            {
                SetValueInternal(PenOffsetProperty, value);
            }
        }

        public TextDecorationUnit PenOffsetUnit
        {
            get
            {
                return (TextDecorationUnit)GetValue(PenOffsetUnitProperty);
            }
            set
            {
                SetValueInternal(PenOffsetUnitProperty, value);
            }
        }

        public TextDecorationUnit PenThicknessUnit
        {
            get
            {
                return (TextDecorationUnit)GetValue(PenThicknessUnitProperty);
            }
            set
            {
                SetValueInternal(PenThicknessUnitProperty, value);
            }
        }

        public TextDecorationLocation Location
        {
            get
            {
                return (TextDecorationLocation)GetValue(LocationProperty);
            }
            set
            {
                SetValueInternal(LocationProperty, value);
            }
        }

        #endregion Public Properties

        #region Protected Methods

        protected override Freezable CreateInstanceCore()
        {
            return new TextDecoration();
        }

        #endregion Protected Methods

        #region Dependency Properties

        public static readonly DependencyProperty PenProperty;
        public static readonly DependencyProperty PenOffsetProperty;
        public static readonly DependencyProperty PenOffsetUnitProperty;
        public static readonly DependencyProperty PenThicknessUnitProperty;
        public static readonly DependencyProperty LocationProperty;

        #endregion Dependency Properties

        #region Internal Fields

        internal const double c_PenOffset = 0.0;
        internal const TextDecorationUnit c_PenOffsetUnit = TextDecorationUnit.FontRecommended;
        internal const TextDecorationUnit c_PenThicknessUnit = TextDecorationUnit.FontRecommended;
        internal const TextDecorationLocation c_Location = TextDecorationLocation.Underline;

        #endregion Internal Fields

        #region Constructors

        static TextDecoration()
        {
            Type typeofThis = typeof(TextDecoration);
            PenProperty =
                  RegisterProperty("Pen",
                                   typeof(Pen),
                                   typeofThis,
                                   null,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated = */ false,
                                   /* coerceValueCallback */ null);
            PenOffsetProperty =
                  RegisterProperty("PenOffset",
                                   typeof(double),
                                   typeofThis,
                                   0.0,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated = */ false,
                                   /* coerceValueCallback */ null);
            PenOffsetUnitProperty =
                  RegisterProperty("PenOffsetUnit",
                                   typeof(TextDecorationUnit),
                                   typeofThis,
                                   TextDecorationUnit.FontRecommended,
                                   null,
                                   new ValidateValueCallback(System.Windows.ValidateEnums.IsTextDecorationUnitValid),
                                   /* isIndependentlyAnimated = */ false,
                                   /* coerceValueCallback */ null);
            PenThicknessUnitProperty =
                  RegisterProperty("PenThicknessUnit",
                                   typeof(TextDecorationUnit),
                                   typeofThis,
                                   TextDecorationUnit.FontRecommended,
                                   null,
                                   new ValidateValueCallback(System.Windows.ValidateEnums.IsTextDecorationUnitValid),
                                   /* isIndependentlyAnimated = */ false,
                                   /* coerceValueCallback */ null);
            LocationProperty =
                  RegisterProperty("Location",
                                   typeof(TextDecorationLocation),
                                   typeofThis,
                                   TextDecorationLocation.Underline,
                                   null,
                                   new ValidateValueCallback(System.Windows.ValidateEnums.IsTextDecorationLocationValid),
                                   /* isIndependentlyAnimated = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
