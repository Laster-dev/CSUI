// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This file follows WPF's Generated\BitmapImage.cs contract, adapted for NativeAOT without reflection attributes or DUCE channels.
//

#nullable disable

using System.IO;
using System.Net.Cache;

namespace System.Windows.Media.Imaging
{
    public sealed partial class BitmapImage : BitmapSource
    {
        #region Public Methods

        /// <summary>
        ///     Shadows inherited Clone() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new BitmapImage Clone()
        {
            return (BitmapImage)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new BitmapImage CloneCurrentValue()
        {
            return (BitmapImage)base.CloneCurrentValue();
        }

        #endregion Public Methods

        #region Private Methods

        private static void UriCachePolicyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage)d);
            target.UriCachePolicyPropertyChangedHook(e);
        }

        private static void UriSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage)d);
            target.UriSourcePropertyChangedHook(e);
        }

        private static void StreamSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage)d);
            target.StreamSourcePropertyChangedHook(e);
        }

        private static void DecodePixelWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage)d);
            target.DecodePixelWidthPropertyChangedHook(e);
        }

        private static void DecodePixelHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage)d);
            target.DecodePixelHeightPropertyChangedHook(e);
        }

        private static void RotationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage)d);
            target.RotationPropertyChangedHook(e);
        }

        private static void SourceRectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage)d);
            target.SourceRectPropertyChangedHook(e);
        }

        private static void CreateOptionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage)d);
            target.CreateOptionsPropertyChangedHook(e);
        }

        private static void CacheOptionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitmapImage target = ((BitmapImage)d);
            target.CacheOptionPropertyChangedHook(e);
        }

        #endregion Private Methods

        #region Public Properties

        public RequestCachePolicy UriCachePolicy
        {
            get => (RequestCachePolicy)GetValue(UriCachePolicyProperty);
            set => SetValueInternal(UriCachePolicyProperty, value);
        }

        public Uri UriSource
        {
            get => (Uri)GetValue(UriSourceProperty);
            set => SetValueInternal(UriSourceProperty, value);
        }

        public Stream StreamSource
        {
            get => (Stream)GetValue(StreamSourceProperty);
            set => SetValueInternal(StreamSourceProperty, value);
        }

        public int DecodePixelWidth
        {
            get => (int)GetValue(DecodePixelWidthProperty);
            set => SetValueInternal(DecodePixelWidthProperty, value);
        }

        public int DecodePixelHeight
        {
            get => (int)GetValue(DecodePixelHeightProperty);
            set => SetValueInternal(DecodePixelHeightProperty, value);
        }

        public Rotation Rotation
        {
            get => (Rotation)GetValue(RotationProperty);
            set => SetValueInternal(RotationProperty, value);
        }

        public Int32Rect SourceRect
        {
            get => (Int32Rect)GetValue(SourceRectProperty);
            set => SetValueInternal(SourceRectProperty, value);
        }

        public BitmapCreateOptions CreateOptions
        {
            get => (BitmapCreateOptions)GetValue(CreateOptionsProperty);
            set => SetValueInternal(CreateOptionsProperty, value);
        }

        public BitmapCacheOption CacheOption
        {
            get => (BitmapCacheOption)GetValue(CacheOptionProperty);
            set => SetValueInternal(CacheOptionProperty, value);
        }

        #endregion Public Properties

        #region Protected Methods

        protected override Freezable CreateInstanceCore()
        {
            return new BitmapImage();
        }

        protected override void CloneCore(Freezable source)
        {
            BitmapImage sourceBitmapImage = (BitmapImage)source;
            ClonePrequel(sourceBitmapImage);
            base.CloneCore(source);
            ClonePostscript(sourceBitmapImage);
        }

        protected override void CloneCurrentValueCore(Freezable source)
        {
            BitmapImage sourceBitmapImage = (BitmapImage)source;
            ClonePrequel(sourceBitmapImage);
            base.CloneCurrentValueCore(source);
            ClonePostscript(sourceBitmapImage);
        }

        protected override void GetAsFrozenCore(Freezable source)
        {
            BitmapImage sourceBitmapImage = (BitmapImage)source;
            ClonePrequel(sourceBitmapImage);
            base.GetAsFrozenCore(source);
            ClonePostscript(sourceBitmapImage);
        }

        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            BitmapImage sourceBitmapImage = (BitmapImage)source;
            ClonePrequel(sourceBitmapImage);
            base.GetCurrentValueAsFrozenCore(source);
            ClonePostscript(sourceBitmapImage);
        }

        #endregion Protected Methods

        #region Dependency Properties

        public static readonly DependencyProperty UriCachePolicyProperty;
        public static readonly DependencyProperty UriSourceProperty;
        public static readonly DependencyProperty StreamSourceProperty;
        public static readonly DependencyProperty DecodePixelWidthProperty;
        public static readonly DependencyProperty DecodePixelHeightProperty;
        public static readonly DependencyProperty RotationProperty;
        public static readonly DependencyProperty SourceRectProperty;
        public static readonly DependencyProperty CreateOptionsProperty;
        public static readonly DependencyProperty CacheOptionProperty;

        #endregion Dependency Properties

        #region Static Constructor

        internal static RequestCachePolicy s_UriCachePolicy = null;
        internal static Uri s_UriSource = null;
        internal static Stream s_StreamSource = null;
        internal const int c_DecodePixelWidth = 0;
        internal const int c_DecodePixelHeight = 0;
        internal const Rotation c_Rotation = Rotation.Rotate0;
        internal static Int32Rect s_SourceRect = Int32Rect.Empty;
        internal const BitmapCreateOptions c_CreateOptions = BitmapCreateOptions.None;
        internal const BitmapCacheOption c_CacheOption = BitmapCacheOption.Default;

        static BitmapImage()
        {
            Type typeofThis = typeof(BitmapImage);

            UriCachePolicyProperty = RegisterProperty(
                "UriCachePolicy",
                typeof(RequestCachePolicy),
                typeofThis,
                null,
                new PropertyChangedCallback(UriCachePolicyPropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceUriCachePolicy));

            UriSourceProperty = RegisterProperty(
                "UriSource",
                typeof(Uri),
                typeofThis,
                null,
                new PropertyChangedCallback(UriSourcePropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceUriSource));

            StreamSourceProperty = RegisterProperty(
                "StreamSource",
                typeof(Stream),
                typeofThis,
                null,
                new PropertyChangedCallback(StreamSourcePropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceStreamSource));

            DecodePixelWidthProperty = RegisterProperty(
                "DecodePixelWidth",
                typeof(int),
                typeofThis,
                0,
                new PropertyChangedCallback(DecodePixelWidthPropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceDecodePixelWidth));

            DecodePixelHeightProperty = RegisterProperty(
                "DecodePixelHeight",
                typeof(int),
                typeofThis,
                0,
                new PropertyChangedCallback(DecodePixelHeightPropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceDecodePixelHeight));

            RotationProperty = RegisterProperty(
                "Rotation",
                typeof(Rotation),
                typeofThis,
                Rotation.Rotate0,
                new PropertyChangedCallback(RotationPropertyChanged),
                new ValidateValueCallback(ValidateEnums.IsRotationValid),
                false,
                new CoerceValueCallback(CoerceRotation));

            SourceRectProperty = RegisterProperty(
                "SourceRect",
                typeof(Int32Rect),
                typeofThis,
                Int32Rect.Empty,
                new PropertyChangedCallback(SourceRectPropertyChanged),
                null,
                false,
                new CoerceValueCallback(CoerceSourceRect));

            CreateOptionsProperty = RegisterProperty(
                "CreateOptions",
                typeof(BitmapCreateOptions),
                typeofThis,
                BitmapCreateOptions.None,
                new PropertyChangedCallback(CreateOptionsPropertyChanged),
                new ValidateValueCallback(ValidateEnums.IsBitmapCreateOptionsValid),
                false,
                new CoerceValueCallback(CoerceCreateOptions));

            CacheOptionProperty = RegisterProperty(
                "CacheOption",
                typeof(BitmapCacheOption),
                typeofThis,
                BitmapCacheOption.Default,
                new PropertyChangedCallback(CacheOptionPropertyChanged),
                new ValidateValueCallback(ValidateEnums.IsBitmapCacheOptionValid),
                false,
                new CoerceValueCallback(CoerceCacheOption));
        }

        #endregion Static Constructor
    }
}
