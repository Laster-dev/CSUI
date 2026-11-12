// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;

namespace System.Windows.Media.Imaging
{
    public sealed partial class TransformedBitmap : BitmapSource
    {
        #region Public Methods

        public new TransformedBitmap Clone()
        {
            return (TransformedBitmap)base.Clone();
        }

        public new TransformedBitmap CloneCurrentValue()
        {
            return (TransformedBitmap)base.CloneCurrentValue();
        }

        #endregion Public Methods

        private static void SourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TransformedBitmap target = (TransformedBitmap)d;
            target.SourcePropertyChangedHook(e);

            if (e.IsASubPropertyChange &&
                (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            target.PropertyChanged(SourceProperty);
        }

        private static void TransformPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TransformedBitmap target = (TransformedBitmap)d;
            target.TransformPropertyChangedHook(e);

            if (e.IsASubPropertyChange &&
                (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            target.PropertyChanged(TransformProperty);
        }

        #region Public Properties

        public BitmapSource Source
        {
            get => (BitmapSource)GetValue(SourceProperty);
            set => SetValueInternal(SourceProperty, value);
        }

        public Transform Transform
        {
            get => (Transform)GetValue(TransformProperty);
            set => SetValueInternal(TransformProperty, value);
        }

        #endregion Public Properties

        #region Protected Methods

        protected override Freezable CreateInstanceCore()
        {
            return new TransformedBitmap();
        }

        protected override void CloneCore(Freezable source)
        {
            TransformedBitmap sourceTransformedBitmap = (TransformedBitmap)source;
            ClonePrequel(sourceTransformedBitmap);
            base.CloneCore(source);
            ClonePostscript(sourceTransformedBitmap);
        }

        protected override void CloneCurrentValueCore(Freezable source)
        {
            TransformedBitmap sourceTransformedBitmap = (TransformedBitmap)source;
            ClonePrequel(sourceTransformedBitmap);
            base.CloneCurrentValueCore(source);
            ClonePostscript(sourceTransformedBitmap);
        }

        protected override void GetAsFrozenCore(Freezable source)
        {
            TransformedBitmap sourceTransformedBitmap = (TransformedBitmap)source;
            ClonePrequel(sourceTransformedBitmap);
            base.GetAsFrozenCore(source);
            ClonePostscript(sourceTransformedBitmap);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            TransformedBitmap sourceTransformedBitmap = (TransformedBitmap)source;
            ClonePrequel(sourceTransformedBitmap);
            base.GetCurrentValueAsFrozenCore(source);
            ClonePostscript(sourceTransformedBitmap);
        }

        #endregion Protected Methods

        #region Dependency Properties

        public static readonly DependencyProperty SourceProperty;
        public static readonly DependencyProperty TransformProperty;

        #endregion Dependency Properties

        #region Internal Fields

        internal static BitmapSource s_Source = null;
        internal static Transform s_Transform = Transform.Identity;

        #endregion Internal Fields

        #region Constructors

        static TransformedBitmap()
        {
            Debug.Assert(s_Source == null || s_Source.IsFrozen,
                "Detected context bound default value TransformedBitmap.s_Source.");
            Debug.Assert(s_Transform == null || s_Transform.IsFrozen,
                "Detected context bound default value TransformedBitmap.s_Transform.");

            Type typeofThis = typeof(TransformedBitmap);
            SourceProperty = RegisterProperty(
                "Source",
                typeof(BitmapSource),
                typeofThis,
                null,
                new PropertyChangedCallback(SourcePropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceSource));

            TransformProperty = RegisterProperty(
                "Transform",
                typeof(Transform),
                typeofThis,
                Transform.Identity,
                new PropertyChangedCallback(TransformPropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceTransform));
        }

        #endregion Constructors
    }
}
