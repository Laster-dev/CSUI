// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// CSUI native AccessText control backed by CSUI's DirectWrite TextBlock.

#nullable disable

using System;
using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using MS.Internal.Controls;
using MS.Internal.PresentationFramework;

namespace System.Windows.Controls
{
    [ContentProperty("Text")]
    public class AccessText : FrameworkElement, IAddChild
    {
        static AccessText()
        {
            FrameworkElementFactory.RegisterType<AccessText>();
            ControlFoundationHooks.RegisterAccessTextDisplay(
                TextProperty,
                static () => FrameworkElementFactory.Create<AccessText>());
        }

        public AccessText()
        {
            _textBlock = new TextBlock();
            ((ITextDisplayElement)_textBlock).IsContentPresenterContainer = true;
            AddVisualChild(_textBlock);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(AccessText),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                    new PropertyChangedCallback(OnTextChanged)));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner(
                typeof(AccessText),
                new FrameworkPropertyMetadata(OnTextPropertyChanged));

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly DependencyProperty FontStyleProperty =
            TextBlock.FontStyleProperty.AddOwner(
                typeof(AccessText),
                new FrameworkPropertyMetadata(OnTextPropertyChanged));

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public static readonly DependencyProperty FontWeightProperty =
            TextBlock.FontWeightProperty.AddOwner(
                typeof(AccessText),
                new FrameworkPropertyMetadata(OnTextPropertyChanged));

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public static readonly DependencyProperty FontSizeProperty =
            TextBlock.FontSizeProperty.AddOwner(
                typeof(AccessText),
                new FrameworkPropertyMetadata(OnTextPropertyChanged));

        [TypeConverter(typeof(FontSizeConverter))]
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly DependencyProperty ForegroundProperty =
            TextBlock.ForegroundProperty.AddOwner(
                typeof(AccessText),
                new FrameworkPropertyMetadata(OnTextPropertyChanged));

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly DependencyProperty BackgroundProperty =
            TextBlock.BackgroundProperty.AddOwner(
                typeof(AccessText),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnTextPropertyChanged));

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly DependencyProperty LineHeightProperty =
            TextBlock.LineHeightProperty.AddOwner(
                typeof(AccessText),
                new FrameworkPropertyMetadata(OnTextPropertyChanged));

        public double LineHeight
        {
            get => (double)GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        public static readonly DependencyProperty TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner(
                typeof(AccessText),
                new FrameworkPropertyMetadata(OnTextPropertyChanged));

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        public static readonly DependencyProperty TextTrimmingProperty =
            TextBlock.TextTrimmingProperty.AddOwner(
                typeof(AccessText),
                new FrameworkPropertyMetadata(OnTextPropertyChanged));

        public TextTrimming TextTrimming
        {
            get => (TextTrimming)GetValue(TextTrimmingProperty);
            set => SetValue(TextTrimmingProperty, value);
        }

        public static readonly DependencyProperty TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner(
                typeof(AccessText),
                new FrameworkPropertyMetadata(OnTextPropertyChanged));

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public static readonly DependencyProperty TextDecorationsProperty =
            DependencyProperty.Register(
                nameof(TextDecorations),
                typeof(TextDecorationCollection),
                typeof(AccessText),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public TextDecorationCollection TextDecorations
        {
            get => (TextDecorationCollection)GetValue(TextDecorationsProperty);
            set => SetValue(TextDecorationsProperty, value);
        }

        public char AccessKey => _accessKey;

        internal static char AccessKeyMarker => ControlFoundationHooks.AccessKeyMarker;

        internal static string RemoveAccessKeyMarker(string text)
        {
            return ControlFoundationHooks.RemoveAccessKeyMarker(text);
        }

        void IAddChild.AddChild(object value)
        {
            if (value == null)
            {
                return;
            }
            ((IAddChild)this).AddText(value.ToString());
        }

        void IAddChild.AddText(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            Text = string.IsNullOrEmpty(Text) ? text : Text + text;
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AccessText)d).SyncTextBlockProperties();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AccessText)d).UpdateAccessKey((string)e.NewValue);
        }

        private void SyncTextBlockProperties()
        {
            _textBlock.FontFamily = FontFamily;
            _textBlock.FontStyle = FontStyle;
            _textBlock.FontWeight = FontWeight;
            _textBlock.FontSize = FontSize;
            _textBlock.Foreground = Foreground;
            _textBlock.Background = Background;
            _textBlock.LineHeight = LineHeight;
            _textBlock.TextAlignment = TextAlignment;
            _textBlock.TextTrimming = TextTrimming;
            _textBlock.TextWrapping = TextWrapping;
            InvalidateMeasure();
        }

        private void UpdateAccessKey(string newText)
        {
            if (_currentlyRegistered != null)
            {
                AccessKeyManager.Unregister(_currentlyRegistered, this);
                _currentlyRegistered = null;
            }

            _accessKey = '\0';
            if (string.IsNullOrEmpty(newText))
            {
                _textBlock.Text = string.Empty;
                return;
            }

            var sb = new StringBuilder(newText.Length);
            bool foundAccessKey = false;
            for (int i = 0; i < newText.Length; i++)
            {
                char ch = newText[i];
                if (ch == AccessKeyMarker)
                {
                    if (i + 1 < newText.Length)
                    {
                        char next = newText[i + 1];
                        if (next == AccessKeyMarker)
                        {
                            sb.Append(AccessKeyMarker);
                            i++;
                            continue;
                        }
                        if (!foundAccessKey)
                        {
                            foundAccessKey = true;
                            _accessKey = next;
                            _currentlyRegistered = next.ToString();
                            AccessKeyManager.Register(_currentlyRegistered, this);
                            sb.Append(next);
                            i++;
                            continue;
                        }
                    }
                    continue;
                }
                sb.Append(ch);
            }

            _textBlock.Text = sb.ToString();
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.Visual_ArgumentOutOfRange);
            }
            return _textBlock;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            SyncTextBlockProperties();
            _textBlock.Measure(constraint);
            return _textBlock.DesiredSize;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _textBlock.Arrange(new Rect(arrangeBounds));
            return _textBlock.RenderSize;
        }

        private readonly TextBlock _textBlock;
        private char _accessKey;
        private string _currentlyRegistered;
    }
}
