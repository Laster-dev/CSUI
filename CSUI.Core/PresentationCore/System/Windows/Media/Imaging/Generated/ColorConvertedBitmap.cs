// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;

namespace System.Windows.Media.Imaging
{
    public sealed partial class ColorConvertedBitmap : BitmapSource
    {
        #region Public Methods

        public new ColorConvertedBitmap Clone()
        {
            return (ColorConvertedBitmap)base.Clone();
        }

        public new ColorConvertedBitmap CloneCurrentValue()
        {
            return (ColorConvertedBitmap)base.CloneCurrentValue();
        }

        #endregion Public Methods

        private static void SourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorConvertedBitmap target = (ColorConvertedBitmap)d;
            target.SourcePropertyChangedHook(e);

            if (e.IsASubPropertyChange &&
                (e.OldValueSource == e.NewValueSource))
            {
                return;
            }

            target.PropertyChanged(SourceProperty);
        }

        private static void SourceColorContextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorConvertedBitmap target = (ColorConvertedBitmap)d;
            target.SourceColorContextPropertyChangedHook(e);
            target.PropertyChanged(SourceColorContextProperty);
        }

        private static void DestinationColorContextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorConvertedBitmap target = (ColorConvertedBitmap)d;
            target.DestinationColorContextPropertyChangedHook(e);
            target.PropertyChanged(DestinationColorContextProperty);
        }

        private static void DestinationFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorConvertedBitmap target = (ColorConvertedBitmap)d;
            target.DestinationFormatPropertyChangedHook(e);
            target.PropertyChanged(DestinationFormatProperty);
        }

        #region Public Properties

        public BitmapSource Source
        {
            get => (BitmapSource)GetValue(SourceProperty);
            set => SetValueInternal(SourceProperty, value);
        }

        public ColorContext SourceColorContext
        {
            get => (ColorContext)GetValue(SourceColorContextProperty);
            set => SetValueInternal(SourceColorContextProperty, value);
        }

        public ColorContext DestinationColorContext
        {
            get => (ColorContext)GetValue(DestinationColorContextProperty);
            set => SetValueInternal(DestinationColorContextProperty, value);
        }

        public PixelFormat DestinationFormat
        {
            get => (PixelFormat)GetValue(DestinationFormatProperty);
            set => SetValueInternal(DestinationFormatProperty, value);
        }

        #endregion Public Properties

        #region Protected Methods

        protected override Freezable CreateInstanceCore()
        {
            return new ColorConvertedBitmap();
        }

        protected override void CloneCore(Freezable source)
        {
            ColorConvertedBitmap sourceColorConvertedBitmap = (ColorConvertedBitmap)source;
            ClonePrequel(sourceColorConvertedBitmap);
            base.CloneCore(source);
            ClonePostscript(sourceColorConvertedBitmap);
        }

        protected override void CloneCurrentValueCore(Freezable source)
        {
            ColorConvertedBitmap sourceColorConvertedBitmap = (ColorConvertedBitmap)source;
            ClonePrequel(sourceColorConvertedBitmap);
            base.CloneCurrentValueCore(source);
            ClonePostscript(sourceColorConvertedBitmap);
        }

        protected override void GetAsFrozenCore(Freezable source)
        {
            ColorConvertedBitmap sourceColorConvertedBitmap = (ColorConvertedBitmap)source;
            ClonePrequel(sourceColorConvertedBitmap);
            base.GetAsFrozenCore(source);
            ClonePostscript(sourceColorConvertedBitmap);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            ColorConvertedBitmap sourceColorConvertedBitmap = (ColorConvertedBitmap)source;
            ClonePrequel(sourceColorConvertedBitmap);
            base.GetCurrentValueAsFrozenCore(source);
            ClonePostscript(sourceColorConvertedBitmap);
        }

        #endregion Protected Methods

        #region Dependency Properties

        public static readonly DependencyProperty SourceProperty;
        public static readonly DependencyProperty SourceColorContextProperty;
        public static readonly DependencyProperty DestinationColorContextProperty;
        public static readonly DependencyProperty DestinationFormatProperty;

        #endregion Dependency Properties

        #region Internal Fields

        internal static BitmapSource s_Source = null;
        internal static ColorContext s_SourceColorContext = null;
        internal static ColorContext s_DestinationColorContext = null;
        internal static PixelFormat s_DestinationFormat = PixelFormats.Pbgra32;

        #endregion Internal Fields

        #region Constructors

        static ColorConvertedBitmap()
        {
            Debug.Assert(s_Source == null || s_Source.IsFrozen,
                "Detected context bound default value ColorConvertedBitmap.s_Source.");

            Type typeofThis = typeof(ColorConvertedBitmap);
            SourceProperty = RegisterProperty(
                "Source",
                typeof(BitmapSource),
                typeofThis,
                null,
                new PropertyChangedCallback(SourcePropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceSource));

            SourceColorContextProperty = RegisterProperty(
                "SourceColorContext",
                typeof(ColorContext),
                typeofThis,
                null,
                new PropertyChangedCallback(SourceColorContextPropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceSourceColorContext));

            DestinationColorContextProperty = RegisterProperty(
                "DestinationColorContext",
                typeof(ColorContext),
                typeofThis,
                null,
                new PropertyChangedCallback(DestinationColorContextPropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceDestinationColorContext));

            DestinationFormatProperty = RegisterProperty(
                "DestinationFormat",
                typeof(PixelFormat),
                typeofThis,
                PixelFormats.Pbgra32,
                new PropertyChangedCallback(DestinationFormatPropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceDestinationFormat));
        }

        #endregion Constructors
    }
}
