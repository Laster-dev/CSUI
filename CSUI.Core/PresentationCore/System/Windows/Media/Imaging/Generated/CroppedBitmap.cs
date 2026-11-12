// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;

namespace System.Windows.Media.Imaging
{
    public sealed partial class CroppedBitmap : BitmapSource
    {
        #region Public Methods

        public new CroppedBitmap Clone()
        {
            return (CroppedBitmap)base.Clone();
        }

        public new CroppedBitmap CloneCurrentValue()
        {
            return (CroppedBitmap)base.CloneCurrentValue();
        }

        #endregion Public Methods

        private static void SourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CroppedBitmap target = (CroppedBitmap)d;
            target.SourcePropertyChangedHook(e);

            if (e.IsASubPropertyChange &&
                (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            target.PropertyChanged(SourceProperty);
        }

        private static void SourceRectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CroppedBitmap target = (CroppedBitmap)d;
            target.SourceRectPropertyChangedHook(e);
            target.PropertyChanged(SourceRectProperty);
        }

        #region Public Properties

        public BitmapSource Source
        {
            get => (BitmapSource)GetValue(SourceProperty);
            set => SetValueInternal(SourceProperty, value);
        }

        public Int32Rect SourceRect
        {
            get => (Int32Rect)GetValue(SourceRectProperty);
            set => SetValueInternal(SourceRectProperty, value);
        }

        #endregion Public Properties

        #region Protected Methods

        protected override Freezable CreateInstanceCore()
        {
            return new CroppedBitmap();
        }

        protected override void CloneCore(Freezable source)
        {
            CroppedBitmap sourceCroppedBitmap = (CroppedBitmap)source;
            ClonePrequel(sourceCroppedBitmap);
            base.CloneCore(source);
            ClonePostscript(sourceCroppedBitmap);
        }

        protected override void CloneCurrentValueCore(Freezable source)
        {
            CroppedBitmap sourceCroppedBitmap = (CroppedBitmap)source;
            ClonePrequel(sourceCroppedBitmap);
            base.CloneCurrentValueCore(source);
            ClonePostscript(sourceCroppedBitmap);
        }

        protected override void GetAsFrozenCore(Freezable source)
        {
            CroppedBitmap sourceCroppedBitmap = (CroppedBitmap)source;
            ClonePrequel(sourceCroppedBitmap);
            base.GetAsFrozenCore(source);
            ClonePostscript(sourceCroppedBitmap);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            CroppedBitmap sourceCroppedBitmap = (CroppedBitmap)source;
            ClonePrequel(sourceCroppedBitmap);
            base.GetCurrentValueAsFrozenCore(source);
            ClonePostscript(sourceCroppedBitmap);
        }

        #endregion Protected Methods

        #region Dependency Properties

        public static readonly DependencyProperty SourceProperty;
        public static readonly DependencyProperty SourceRectProperty;

        #endregion Dependency Properties

        #region Internal Fields

        internal static BitmapSource s_Source = null;
        internal static Int32Rect s_SourceRect = Int32Rect.Empty;

        #endregion Internal Fields

        #region Constructors

        static CroppedBitmap()
        {
            Debug.Assert(s_Source == null || s_Source.IsFrozen,
                "Detected context bound default value CroppedBitmap.s_Source.");

            Type typeofThis = typeof(CroppedBitmap);
            SourceProperty = RegisterProperty(
                "Source",
                typeof(BitmapSource),
                typeofThis,
                null,
                new PropertyChangedCallback(SourcePropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceSource));

            SourceRectProperty = RegisterProperty(
                "SourceRect",
                typeof(Int32Rect),
                typeofThis,
                Int32Rect.Empty,
                new PropertyChangedCallback(SourceRectPropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceSourceRect));
        }

        #endregion Constructors
    }
}
