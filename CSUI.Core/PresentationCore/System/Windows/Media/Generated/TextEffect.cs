// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Windows.Media.Animation;

namespace System.Windows.Media
{
    public sealed partial class TextEffect : Animatable
    {
        #region Public Methods

        public new TextEffect Clone()
        {
            return (TextEffect)base.Clone();
        }

        public new TextEffect CloneCurrentValue()
        {
            return (TextEffect)base.CloneCurrentValue();
        }

        #endregion Public Methods

        private static bool ValidatePositionStartValue(object value)
        {
            if (!OnPositionStartChanging((int)value))
            {
                return false;
            }
            return true;
        }

        private static bool ValidatePositionCountValue(object value)
        {
            if (!OnPositionCountChanging((int)value))
            {
                return false;
            }
            return true;
        }

        #region Public Properties

        public Transform Transform
        {
            get
            {
                return (Transform)GetValue(TransformProperty);
            }
            set
            {
                SetValueInternal(TransformProperty, value);
            }
        }

        public Geometry Clip
        {
            get
            {
                return (Geometry)GetValue(ClipProperty);
            }
            set
            {
                SetValueInternal(ClipProperty, value);
            }
        }

        public Brush Foreground
        {
            get
            {
                return (Brush)GetValue(ForegroundProperty);
            }
            set
            {
                SetValueInternal(ForegroundProperty, value);
            }
        }

        public int PositionStart
        {
            get
            {
                return (int)GetValue(PositionStartProperty);
            }
            set
            {
                SetValueInternal(PositionStartProperty, value);
            }
        }

        public int PositionCount
        {
            get
            {
                return (int)GetValue(PositionCountProperty);
            }
            set
            {
                SetValueInternal(PositionCountProperty, value);
            }
        }

        #endregion Public Properties

        #region Protected Methods

        protected override Freezable CreateInstanceCore()
        {
            return new TextEffect();
        }

        #endregion Protected Methods

        #region Dependency Properties

        public static readonly DependencyProperty TransformProperty;
        public static readonly DependencyProperty ClipProperty;
        public static readonly DependencyProperty ForegroundProperty;
        public static readonly DependencyProperty PositionStartProperty;
        public static readonly DependencyProperty PositionCountProperty;

        #endregion Dependency Properties

        #region Internal Fields

        internal const int c_PositionStart = 0;
        internal const int c_PositionCount = 0;

        #endregion Internal Fields

        #region Constructors

        static TextEffect()
        {
            Type typeofThis = typeof(TextEffect);
            TransformProperty =
                  RegisterProperty("Transform",
                                   typeof(Transform),
                                   typeofThis,
                                   null,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated = */ false,
                                   /* coerceValueCallback */ null);
            ClipProperty =
                  RegisterProperty("Clip",
                                   typeof(Geometry),
                                   typeofThis,
                                   null,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated = */ false,
                                   /* coerceValueCallback */ null);
            ForegroundProperty =
                  RegisterProperty("Foreground",
                                   typeof(Brush),
                                   typeofThis,
                                   null,
                                   null,
                                   null,
                                   /* isIndependentlyAnimated = */ false,
                                   /* coerceValueCallback */ null);
            PositionStartProperty =
                  RegisterProperty("PositionStart",
                                   typeof(int),
                                   typeofThis,
                                   0,
                                   null,
                                   new ValidateValueCallback(ValidatePositionStartValue),
                                   /* isIndependentlyAnimated = */ false,
                                   /* coerceValueCallback */ null);
            PositionCountProperty =
                  RegisterProperty("PositionCount",
                                   typeof(int),
                                   typeofThis,
                                   0,
                                   null,
                                   new ValidateValueCallback(ValidatePositionCountValue),
                                   /* isIndependentlyAnimated = */ false,
                                   /* coerceValueCallback */ null);
        }

        #endregion Constructors
    }
}
