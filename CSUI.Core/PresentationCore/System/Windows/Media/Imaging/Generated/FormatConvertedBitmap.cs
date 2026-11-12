// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;

namespace System.Windows.Media.Imaging
{
    public sealed partial class FormatConvertedBitmap : BitmapSource
    {
        #region Public Methods

        public new FormatConvertedBitmap Clone()
        {
            return (FormatConvertedBitmap)base.Clone();
        }

        public new FormatConvertedBitmap CloneCurrentValue()
        {
            return (FormatConvertedBitmap)base.CloneCurrentValue();
        }

        #endregion Public Methods

        private static void SourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FormatConvertedBitmap target = (FormatConvertedBitmap)d;
            target.SourcePropertyChangedHook(e);

            if (e.IsASubPropertyChange &&
                (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            target.PropertyChanged(SourceProperty);
        }

        private static void DestinationFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FormatConvertedBitmap target = (FormatConvertedBitmap)d;
            target.DestinationFormatPropertyChangedHook(e);
            target.PropertyChanged(DestinationFormatProperty);
        }

        private static void DestinationPalettePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FormatConvertedBitmap target = (FormatConvertedBitmap)d;
            target.DestinationPalettePropertyChangedHook(e);
            target.PropertyChanged(DestinationPaletteProperty);
        }

        private static void AlphaThresholdPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FormatConvertedBitmap target = (FormatConvertedBitmap)d;
            target.AlphaThresholdPropertyChangedHook(e);
            target.PropertyChanged(AlphaThresholdProperty);
        }

        #region Public Properties

        public BitmapSource Source
        {
            get => (BitmapSource)GetValue(SourceProperty);
            set => SetValueInternal(SourceProperty, value);
        }

        public PixelFormat DestinationFormat
        {
            get => (PixelFormat)GetValue(DestinationFormatProperty);
            set => SetValueInternal(DestinationFormatProperty, value);
        }

        public BitmapPalette DestinationPalette
        {
            get => (BitmapPalette)GetValue(DestinationPaletteProperty);
            set => SetValueInternal(DestinationPaletteProperty, value);
        }

        public double AlphaThreshold
        {
            get => (double)GetValue(AlphaThresholdProperty);
            set => SetValueInternal(AlphaThresholdProperty, value);
        }

        #endregion Public Properties

        #region Protected Methods

        protected override Freezable CreateInstanceCore()
        {
            return new FormatConvertedBitmap();
        }

        protected override void CloneCore(Freezable source)
        {
            FormatConvertedBitmap sourceFormatConvertedBitmap = (FormatConvertedBitmap)source;
            ClonePrequel(sourceFormatConvertedBitmap);
            base.CloneCore(source);
            ClonePostscript(sourceFormatConvertedBitmap);
        }

        protected override void CloneCurrentValueCore(Freezable source)
        {
            FormatConvertedBitmap sourceFormatConvertedBitmap = (FormatConvertedBitmap)source;
            ClonePrequel(sourceFormatConvertedBitmap);
            base.CloneCurrentValueCore(source);
            ClonePostscript(sourceFormatConvertedBitmap);
        }

        protected override void GetAsFrozenCore(Freezable source)
        {
            FormatConvertedBitmap sourceFormatConvertedBitmap = (FormatConvertedBitmap)source;
            ClonePrequel(sourceFormatConvertedBitmap);
            base.GetAsFrozenCore(source);
            ClonePostscript(sourceFormatConvertedBitmap);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            FormatConvertedBitmap sourceFormatConvertedBitmap = (FormatConvertedBitmap)source;
            ClonePrequel(sourceFormatConvertedBitmap);
            base.GetCurrentValueAsFrozenCore(source);
            ClonePostscript(sourceFormatConvertedBitmap);
        }

        #endregion Protected Methods

        #region Dependency Properties

        public static readonly DependencyProperty SourceProperty;
        public static readonly DependencyProperty DestinationFormatProperty;
        public static readonly DependencyProperty DestinationPaletteProperty;
        public static readonly DependencyProperty AlphaThresholdProperty;

        #endregion Dependency Properties

        #region Internal Fields

        internal static BitmapSource s_Source = null;
        internal static PixelFormat s_DestinationFormat = PixelFormats.Pbgra32;
        internal static BitmapPalette s_DestinationPalette = null;
        internal const double c_AlphaThreshold = 0.0;

        #endregion Internal Fields

        #region Constructors

        static FormatConvertedBitmap()
        {
            Debug.Assert(s_Source == null || s_Source.IsFrozen,
                "Detected context bound default value FormatConvertedBitmap.s_Source.");

            Type typeofThis = typeof(FormatConvertedBitmap);
            SourceProperty = RegisterProperty(
                "Source",
                typeof(BitmapSource),
                typeofThis,
                null,
                new PropertyChangedCallback(SourcePropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceSource));

            DestinationFormatProperty = RegisterProperty(
                "DestinationFormat",
                typeof(PixelFormat),
                typeofThis,
                PixelFormats.Pbgra32,
                new PropertyChangedCallback(DestinationFormatPropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceDestinationFormat));

            DestinationPaletteProperty = RegisterProperty(
                "DestinationPalette",
                typeof(BitmapPalette),
                typeofThis,
                null,
                new PropertyChangedCallback(DestinationPalettePropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceDestinationPalette));

            AlphaThresholdProperty = RegisterProperty(
                "AlphaThreshold",
                typeof(double),
                typeofThis,
                0.0,
                new PropertyChangedCallback(AlphaThresholdPropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceAlphaThreshold));
        }

        #endregion Constructors
    }
}
