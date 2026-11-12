// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows
{
    public enum LineStackingStrategy
    {
        BlockLineHeight = 0,
        MaxHeight = 1
    }
}

namespace System.Windows.Controls.Ribbon
{
    public enum RibbonImageSize
    {
        Collapsed = 0,
        Small = 1,
        Large = 2
    }

    public enum RibbonControlLengthUnitType
    {
        Auto = 0,
        Pixel = 1,
        Item = 2,
        Star = 3
    }

    public enum RibbonDismissPopupMode
    {
        Always = 0,
        MousePhysicallyNotOver = 1
    }

    [TypeConverter(typeof(RibbonControlLengthConverter))]
    public struct RibbonControlLength : IEquatable<RibbonControlLength>
    {
        private readonly double _unitValue;
        private readonly RibbonControlLengthUnitType _unitType;
        private static readonly RibbonControlLength s_auto = new RibbonControlLength(1.0, RibbonControlLengthUnitType.Auto);

        public RibbonControlLength(double pixels)
            : this(pixels, RibbonControlLengthUnitType.Pixel)
        {
        }

        public RibbonControlLength(double value, RibbonControlLengthUnitType type)
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException("Value cannot be NaN.", nameof(value));
            }
            if (double.IsInfinity(value))
            {
                throw new ArgumentException("Value cannot be infinite.", nameof(value));
            }
            if (type != RibbonControlLengthUnitType.Auto &&
                type != RibbonControlLengthUnitType.Pixel &&
                type != RibbonControlLengthUnitType.Item &&
                type != RibbonControlLengthUnitType.Star)
            {
                throw new ArgumentException("Invalid RibbonControlLengthUnitType.", nameof(type));
            }

            _unitValue = (type == RibbonControlLengthUnitType.Auto) ? 0.0 : value;
            _unitType = type;
        }

        public static RibbonControlLength Auto => s_auto;

        public bool IsAbsolute => _unitType == RibbonControlLengthUnitType.Pixel || _unitType == RibbonControlLengthUnitType.Item;

        public bool IsAuto => _unitType == RibbonControlLengthUnitType.Auto;

        public bool IsStar => _unitType == RibbonControlLengthUnitType.Star;

        public RibbonControlLengthUnitType RibbonControlLengthUnitType => _unitType;

        public double Value => _unitType == RibbonControlLengthUnitType.Auto ? 1.0 : _unitValue;

        public static bool operator ==(RibbonControlLength length1, RibbonControlLength length2) => length1.Equals(length2);

        public static bool operator !=(RibbonControlLength length1, RibbonControlLength length2) => !length1.Equals(length2);

        public override bool Equals(object obj) => obj is RibbonControlLength other && Equals(other);

        public bool Equals(RibbonControlLength other) =>
            _unitType == other._unitType && (_unitType == RibbonControlLengthUnitType.Auto || _unitValue.Equals(other._unitValue));

        public override int GetHashCode() => (int)_unitValue + (int)_unitType;

        public override string ToString()
        {
            return _unitType switch
            {
                RibbonControlLengthUnitType.Auto => "Auto",
                RibbonControlLengthUnitType.Star => Math.Abs(_unitValue - 1.0) < 1e-10 ? "*" : _unitValue.ToString(CultureInfo.InvariantCulture) + "*",
                RibbonControlLengthUnitType.Item => _unitValue.ToString(CultureInfo.InvariantCulture) + "items",
                _ => _unitValue.ToString(CultureInfo.InvariantCulture)
            };
        }
    }

    public class RibbonControlLengthConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
            return sourceType == typeof(string) ||
                   sourceType == typeof(double) ||
                   sourceType == typeof(int) ||
                   sourceType == typeof(float) ||
                   sourceType == typeof(decimal);
        }

        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object source)
        {
            if (source == null)
            {
                throw GetConvertFromException(source);
            }

            if (source is string str)
            {
                str = str.Trim();
                if (string.Equals(str, "Auto", StringComparison.OrdinalIgnoreCase))
                {
                    return RibbonControlLength.Auto;
                }
                if (str.EndsWith("*", StringComparison.Ordinal))
                {
                    string num = str.Substring(0, str.Length - 1).Trim();
                    double val = string.IsNullOrEmpty(num) ? 1.0 : double.Parse(num, NumberStyles.Float, cultureInfo ?? CultureInfo.InvariantCulture);
                    return new RibbonControlLength(val, RibbonControlLengthUnitType.Star);
                }
                if (str.EndsWith("items", StringComparison.OrdinalIgnoreCase))
                {
                    string num = str.Substring(0, str.Length - 5).Trim();
                    double val = double.Parse(num, NumberStyles.Float, cultureInfo ?? CultureInfo.InvariantCulture);
                    return new RibbonControlLength(val, RibbonControlLengthUnitType.Item);
                }
                if (str.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                {
                    str = str.Substring(0, str.Length - 2).Trim();
                }

                double pixels = double.Parse(str, NumberStyles.Float, cultureInfo ?? CultureInfo.InvariantCulture);
                return new RibbonControlLength(pixels, RibbonControlLengthUnitType.Pixel);
            }

            double d = Convert.ToDouble(source, cultureInfo ?? CultureInfo.InvariantCulture);
            return new RibbonControlLength(d, RibbonControlLengthUnitType.Pixel);
        }

        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object value, Type destinationType)
        {
            if (value is RibbonControlLength length && destinationType == typeof(string))
            {
                return length.ToString();
            }

            throw GetConvertToException(value, destinationType);
        }
    }

    public class RibbonControlSizeDefinition : Freezable
    {
        public static readonly DependencyProperty ImageSizeProperty =
            DependencyProperty.Register(nameof(ImageSize), typeof(RibbonImageSize), typeof(RibbonControlSizeDefinition), new FrameworkPropertyMetadata(RibbonImageSize.Large));

        public static readonly DependencyProperty IsLabelVisibleProperty =
            DependencyProperty.Register(nameof(IsLabelVisible), typeof(bool), typeof(RibbonControlSizeDefinition), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty MinWidthProperty =
            DependencyProperty.Register(nameof(MinWidth), typeof(RibbonControlLength), typeof(RibbonControlSizeDefinition), new FrameworkPropertyMetadata(new RibbonControlLength(0.0)));

        public static readonly DependencyProperty MaxWidthProperty =
            DependencyProperty.Register(nameof(MaxWidth), typeof(RibbonControlLength), typeof(RibbonControlSizeDefinition), new FrameworkPropertyMetadata(RibbonControlLength.Auto));

        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(nameof(Width), typeof(RibbonControlLength), typeof(RibbonControlSizeDefinition), new FrameworkPropertyMetadata(RibbonControlLength.Auto));

        public RibbonImageSize ImageSize
        {
            get => (RibbonImageSize)GetValue(ImageSizeProperty);
            set => SetValue(ImageSizeProperty, value);
        }

        public bool IsLabelVisible
        {
            get => (bool)GetValue(IsLabelVisibleProperty);
            set => SetValue(IsLabelVisibleProperty, value);
        }

        public RibbonControlLength MinWidth
        {
            get => (RibbonControlLength)GetValue(MinWidthProperty);
            set => SetValue(MinWidthProperty, value);
        }

        public RibbonControlLength MaxWidth
        {
            get => (RibbonControlLength)GetValue(MaxWidthProperty);
            set => SetValue(MaxWidthProperty, value);
        }

        public RibbonControlLength Width
        {
            get => (RibbonControlLength)GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }

        protected override Freezable CreateInstanceCore() => new RibbonControlSizeDefinition();
    }

    public class RibbonControlSizeDefinitionCollection : FreezableCollection<RibbonControlSizeDefinition>
    {
    }

    public abstract class RibbonGroupSizeDefinitionBase : Freezable
    {
        public static readonly DependencyProperty IsCollapsedProperty =
            DependencyProperty.Register(nameof(IsCollapsed), typeof(bool), typeof(RibbonGroupSizeDefinitionBase), new FrameworkPropertyMetadata(false));

        public bool IsCollapsed
        {
            get => (bool)GetValue(IsCollapsedProperty);
            set => SetValue(IsCollapsedProperty, value);
        }
    }

    public class RibbonGroupSizeDefinitionBaseCollection : FreezableCollection<RibbonGroupSizeDefinitionBase>
    {
    }

    [ContentProperty("ControlDefinitions")]
    public class RibbonGroupSizeDefinition : RibbonGroupSizeDefinitionBase
    {
        private static readonly DependencyPropertyKey ControlDefinitionsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ControlDefinitions), typeof(RibbonControlSizeDefinitionCollection), typeof(RibbonGroupSizeDefinition), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ControlDefinitionsProperty = ControlDefinitionsPropertyKey.DependencyProperty;

        public RibbonGroupSizeDefinition()
        {
            SetValue(ControlDefinitionsPropertyKey, new RibbonControlSizeDefinitionCollection());
        }

        public RibbonControlSizeDefinitionCollection ControlDefinitions =>
            (RibbonControlSizeDefinitionCollection)GetValue(ControlDefinitionsProperty);

        protected override Freezable CreateInstanceCore() => new RibbonGroupSizeDefinition();
    }

    public class RibbonGroupSizeDefinitionCollection : FreezableCollection<RibbonGroupSizeDefinition>
    {
    }

    public class RibbonControlDataTemplate : DataTemplate
    {
    }

    public delegate void RibbonDismissPopupEventHandler(object sender, RibbonDismissPopupEventArgs e);

    public class RibbonDismissPopupEventArgs : RoutedEventArgs
    {
        public RibbonDismissPopupEventArgs()
            : this(RibbonDismissPopupMode.Always)
        {
        }

        public RibbonDismissPopupEventArgs(RibbonDismissPopupMode dismissMode)
        {
            RoutedEvent = RibbonControlService.DismissPopupEvent;
            DismissMode = dismissMode;
        }

        public RibbonDismissPopupMode DismissMode { get; }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler is RibbonDismissPopupEventHandler handler)
            {
                handler(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }
    }

    public delegate void RibbonQuickAccessToolBarCloneEventHandler(object sender, RibbonQuickAccessToolBarCloneEventArgs e);

    public class RibbonQuickAccessToolBarCloneEventArgs : RoutedEventArgs
    {
        public RibbonQuickAccessToolBarCloneEventArgs(UIElement targetElement)
        {
            RoutedEvent = RibbonControlService.CloneInstanceEvent;
            TargetElement = targetElement;
        }

        public UIElement TargetElement { get; }

        public UIElement CloneInstance { get; set; }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler is RibbonQuickAccessToolBarCloneEventHandler handler)
            {
                handler(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }
    }

    public static class RibbonCommands
    {
        public static RoutedUICommand AddToQuickAccessToolBarCommand { get; } =
            new RoutedUICommand("Add to Quick Access Toolbar", nameof(AddToQuickAccessToolBarCommand), typeof(RibbonCommands));

        public static RoutedUICommand RemoveFromQuickAccessToolBarCommand { get; } =
            new RoutedUICommand("Remove from Quick Access Toolbar", nameof(RemoveFromQuickAccessToolBarCommand), typeof(RibbonCommands));

        public static RoutedUICommand MaximizeRibbonCommand { get; } =
            new RoutedUICommand("Maximize the Ribbon", nameof(MaximizeRibbonCommand), typeof(RibbonCommands));

        public static RoutedUICommand MinimizeRibbonCommand { get; } =
            new RoutedUICommand("Minimize the Ribbon", nameof(MinimizeRibbonCommand), typeof(RibbonCommands));

        public static RoutedUICommand ShowQuickAccessToolBarAboveRibbonCommand { get; } =
            new RoutedUICommand("Show Quick Access Toolbar Above the Ribbon", nameof(ShowQuickAccessToolBarAboveRibbonCommand), typeof(RibbonCommands));

        public static RoutedUICommand ShowQuickAccessToolBarBelowRibbonCommand { get; } =
            new RoutedUICommand("Show Quick Access Toolbar Below the Ribbon", nameof(ShowQuickAccessToolBarBelowRibbonCommand), typeof(RibbonCommands));
    }

    public static class RibbonControlService
    {
        public static readonly DependencyProperty LargeImageSourceProperty =
            DependencyProperty.RegisterAttached("LargeImageSource", typeof(ImageSource), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty SmallImageSourceProperty =
            DependencyProperty.RegisterAttached("SmallImageSource", typeof(ImageSource), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.RegisterAttached("Label", typeof(string), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ToolTipTitleProperty =
            DependencyProperty.RegisterAttached("ToolTipTitle", typeof(string), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ToolTipDescriptionProperty =
            DependencyProperty.RegisterAttached("ToolTipDescription", typeof(string), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ToolTipImageSourceProperty =
            DependencyProperty.RegisterAttached("ToolTipImageSource", typeof(ImageSource), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ToolTipFooterTitleProperty =
            DependencyProperty.RegisterAttached("ToolTipFooterTitle", typeof(string), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ToolTipFooterDescriptionProperty =
            DependencyProperty.RegisterAttached("ToolTipFooterDescription", typeof(string), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ToolTipFooterImageSourceProperty =
            DependencyProperty.RegisterAttached("ToolTipFooterImageSource", typeof(ImageSource), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ControlSizeDefinitionProperty =
            DependencyProperty.RegisterAttached("ControlSizeDefinition", typeof(RibbonControlSizeDefinition), typeof(RibbonControlService),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

        internal static readonly DependencyPropertyKey IsInControlGroupPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("IsInControlGroup", typeof(bool), typeof(RibbonControlService), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsInControlGroupProperty = IsInControlGroupPropertyKey.DependencyProperty;

        public static readonly DependencyProperty QuickAccessToolBarIdProperty =
            DependencyProperty.RegisterAttached("QuickAccessToolBarId", typeof(object), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
            DependencyProperty.RegisterAttached("CanAddToQuickAccessToolBar", typeof(bool), typeof(RibbonControlService), new FrameworkPropertyMetadata(false));

        internal static readonly DependencyPropertyKey IsInQuickAccessToolBarPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("IsInQuickAccessToolBar", typeof(bool), typeof(RibbonControlService), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsInQuickAccessToolBarProperty = IsInQuickAccessToolBarPropertyKey.DependencyProperty;

        public static readonly DependencyProperty MouseOverBackgroundProperty =
            DependencyProperty.RegisterAttached("MouseOverBackground", typeof(Brush), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty MouseOverBorderBrushProperty =
            DependencyProperty.RegisterAttached("MouseOverBorderBrush", typeof(Brush), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty PressedBackgroundProperty =
            DependencyProperty.RegisterAttached("PressedBackground", typeof(Brush), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty PressedBorderBrushProperty =
            DependencyProperty.RegisterAttached("PressedBorderBrush", typeof(Brush), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty CheckedBackgroundProperty =
            DependencyProperty.RegisterAttached("CheckedBackground", typeof(Brush), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty CheckedBorderBrushProperty =
            DependencyProperty.RegisterAttached("CheckedBorderBrush", typeof(Brush), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty FocusedBackgroundProperty =
            DependencyProperty.RegisterAttached("FocusedBackground", typeof(Brush), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty FocusedBorderBrushProperty =
            DependencyProperty.RegisterAttached("FocusedBorderBrush", typeof(Brush), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ShowKeyboardCuesProperty =
            DependencyProperty.RegisterAttached("ShowKeyboardCues", typeof(bool), typeof(RibbonControlService),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        internal static readonly DependencyPropertyKey RibbonPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("Ribbon", typeof(Ribbon), typeof(RibbonControlService),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty RibbonProperty = RibbonPropertyKey.DependencyProperty;

        public static readonly DependencyProperty DefaultStyleKeyProperty =
            DependencyProperty.RegisterAttached("DefaultStyleKey", typeof(object), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty QuickAccessToolBarControlSizeDefinitionProperty =
            DependencyProperty.RegisterAttached("QuickAccessToolBarControlSizeDefinition", typeof(RibbonControlSizeDefinition), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        public static readonly RoutedEvent DismissPopupEvent =
            EventManager.RegisterRoutedEvent("DismissPopup", RoutingStrategy.Bubble, typeof(RibbonDismissPopupEventHandler), typeof(RibbonControlService));

        public static readonly RoutedEvent CloneInstanceEvent =
            EventManager.RegisterRoutedEvent("CloneInstance", RoutingStrategy.Bubble, typeof(RibbonQuickAccessToolBarCloneEventHandler), typeof(RibbonControlService));

        public static ImageSource GetLargeImageSource(DependencyObject element) => (ImageSource)element.GetValue(LargeImageSourceProperty);
        public static void SetLargeImageSource(DependencyObject element, ImageSource value) => element.SetValue(LargeImageSourceProperty, value);

        public static ImageSource GetSmallImageSource(DependencyObject element) => (ImageSource)element.GetValue(SmallImageSourceProperty);
        public static void SetSmallImageSource(DependencyObject element, ImageSource value) => element.SetValue(SmallImageSourceProperty, value);

        public static string GetLabel(DependencyObject element) => (string)element.GetValue(LabelProperty);
        public static void SetLabel(DependencyObject element, string value) => element.SetValue(LabelProperty, value);

        public static string GetToolTipTitle(DependencyObject element) => (string)element.GetValue(ToolTipTitleProperty);
        public static void SetToolTipTitle(DependencyObject element, string value) => element.SetValue(ToolTipTitleProperty, value);

        public static string GetToolTipDescription(DependencyObject element) => (string)element.GetValue(ToolTipDescriptionProperty);
        public static void SetToolTipDescription(DependencyObject element, string value) => element.SetValue(ToolTipDescriptionProperty, value);

        public static ImageSource GetToolTipImageSource(DependencyObject element) => (ImageSource)element.GetValue(ToolTipImageSourceProperty);
        public static void SetToolTipImageSource(DependencyObject element, ImageSource value) => element.SetValue(ToolTipImageSourceProperty, value);

        public static string GetToolTipFooterTitle(DependencyObject element) => (string)element.GetValue(ToolTipFooterTitleProperty);
        public static void SetToolTipFooterTitle(DependencyObject element, string value) => element.SetValue(ToolTipFooterTitleProperty, value);

        public static string GetToolTipFooterDescription(DependencyObject element) => (string)element.GetValue(ToolTipFooterDescriptionProperty);
        public static void SetToolTipFooterDescription(DependencyObject element, string value) => element.SetValue(ToolTipFooterDescriptionProperty, value);

        public static ImageSource GetToolTipFooterImageSource(DependencyObject element) => (ImageSource)element.GetValue(ToolTipFooterImageSourceProperty);
        public static void SetToolTipFooterImageSource(DependencyObject element, ImageSource value) => element.SetValue(ToolTipFooterImageSourceProperty, value);

        public static RibbonControlSizeDefinition GetControlSizeDefinition(DependencyObject element) => (RibbonControlSizeDefinition)element.GetValue(ControlSizeDefinitionProperty);
        public static void SetControlSizeDefinition(DependencyObject element, RibbonControlSizeDefinition value) => element.SetValue(ControlSizeDefinitionProperty, value);

        public static bool GetIsInControlGroup(DependencyObject element) => (bool)element.GetValue(IsInControlGroupProperty);
        internal static void SetIsInControlGroup(DependencyObject element, bool value) => element.SetValue(IsInControlGroupPropertyKey, value);

        public static object GetQuickAccessToolBarId(DependencyObject element) => element.GetValue(QuickAccessToolBarIdProperty);
        public static void SetQuickAccessToolBarId(DependencyObject element, object value) => element.SetValue(QuickAccessToolBarIdProperty, value);

        public static bool GetCanAddToQuickAccessToolBar(DependencyObject element) => (bool)element.GetValue(CanAddToQuickAccessToolBarProperty);
        public static void SetCanAddToQuickAccessToolBar(DependencyObject element, bool value) => element.SetValue(CanAddToQuickAccessToolBarProperty, value);

        public static bool GetIsInQuickAccessToolBar(DependencyObject element) => (bool)element.GetValue(IsInQuickAccessToolBarProperty);
        internal static void SetIsInQuickAccessToolBar(DependencyObject element, bool value) => element.SetValue(IsInQuickAccessToolBarPropertyKey, value);

        public static Brush GetMouseOverBackground(DependencyObject element) => (Brush)element.GetValue(MouseOverBackgroundProperty);
        public static void SetMouseOverBackground(DependencyObject element, Brush value) => element.SetValue(MouseOverBackgroundProperty, value);

        public static Brush GetMouseOverBorderBrush(DependencyObject element) => (Brush)element.GetValue(MouseOverBorderBrushProperty);
        public static void SetMouseOverBorderBrush(DependencyObject element, Brush value) => element.SetValue(MouseOverBorderBrushProperty, value);

        public static Brush GetPressedBackground(DependencyObject element) => (Brush)element.GetValue(PressedBackgroundProperty);
        public static void SetPressedBackground(DependencyObject element, Brush value) => element.SetValue(PressedBackgroundProperty, value);

        public static Brush GetPressedBorderBrush(DependencyObject element) => (Brush)element.GetValue(PressedBorderBrushProperty);
        public static void SetPressedBorderBrush(DependencyObject element, Brush value) => element.SetValue(PressedBorderBrushProperty, value);

        public static Brush GetCheckedBackground(DependencyObject element) => (Brush)element.GetValue(CheckedBackgroundProperty);
        public static void SetCheckedBackground(DependencyObject element, Brush value) => element.SetValue(CheckedBackgroundProperty, value);

        public static Brush GetCheckedBorderBrush(DependencyObject element) => (Brush)element.GetValue(CheckedBorderBrushProperty);
        public static void SetCheckedBorderBrush(DependencyObject element, Brush value) => element.SetValue(CheckedBorderBrushProperty, value);

        public static Brush GetFocusedBackground(DependencyObject element) => (Brush)element.GetValue(FocusedBackgroundProperty);
        public static void SetFocusedBackground(DependencyObject element, Brush value) => element.SetValue(FocusedBackgroundProperty, value);

        public static Brush GetFocusedBorderBrush(DependencyObject element) => (Brush)element.GetValue(FocusedBorderBrushProperty);
        public static void SetFocusedBorderBrush(DependencyObject element, Brush value) => element.SetValue(FocusedBorderBrushProperty, value);

        public static bool GetShowKeyboardCues(DependencyObject element) => (bool)element.GetValue(ShowKeyboardCuesProperty);
        public static void SetShowKeyboardCues(DependencyObject element, bool value) => element.SetValue(ShowKeyboardCuesProperty, value);

        public static Ribbon GetRibbon(DependencyObject element) => (Ribbon)element.GetValue(RibbonProperty);
        internal static void SetRibbon(DependencyObject element, Ribbon value) => element.SetValue(RibbonPropertyKey, value);

        public static object GetDefaultStyleKey(DependencyObject element) => element.GetValue(DefaultStyleKeyProperty);
        public static void SetDefaultStyleKey(DependencyObject element, object value) => element.SetValue(DefaultStyleKeyProperty, value);

        public static RibbonControlSizeDefinition GetQuickAccessToolBarControlSizeDefinition(DependencyObject element) =>
            (RibbonControlSizeDefinition)element.GetValue(QuickAccessToolBarControlSizeDefinitionProperty);

        public static void SetQuickAccessToolBarControlSizeDefinition(DependencyObject element, RibbonControlSizeDefinition value) =>
            element.SetValue(QuickAccessToolBarControlSizeDefinitionProperty, value);

        public static void AddDismissPopupHandler(DependencyObject element, RibbonDismissPopupEventHandler handler)
        {
            if (element is UIElement uie) uie.AddHandler(DismissPopupEvent, handler);
        }

        public static void RemoveDismissPopupHandler(DependencyObject element, RibbonDismissPopupEventHandler handler)
        {
            if (element is UIElement uie) uie.RemoveHandler(DismissPopupEvent, handler);
        }

        public static void AddCloneInstanceHandler(DependencyObject element, RibbonQuickAccessToolBarCloneEventHandler handler)
        {
            if (element is UIElement uie) uie.AddHandler(CloneInstanceEvent, handler);
        }

        public static void RemoveCloneInstanceHandler(DependencyObject element, RibbonQuickAccessToolBarCloneEventHandler handler)
        {
            if (element is UIElement uie) uie.RemoveHandler(CloneInstanceEvent, handler);
        }
    }

    public class RibbonControl : ContentControl
    {
        public static readonly DependencyProperty ControlSizeDefinitionProperty =
            RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonControl));

        public static readonly DependencyProperty IsInControlGroupProperty =
            RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonControl));

        public static readonly DependencyProperty IsInQuickAccessToolBarProperty =
            RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonControl));

        public RibbonControlSizeDefinition ControlSizeDefinition
        {
            get => (RibbonControlSizeDefinition)GetValue(ControlSizeDefinitionProperty);
            set => SetValue(ControlSizeDefinitionProperty, value);
        }

        public bool IsInControlGroup => (bool)GetValue(IsInControlGroupProperty);

        public bool IsInQuickAccessToolBar => (bool)GetValue(IsInQuickAccessToolBarProperty);

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonControlAutomationPeer(this);
    }

    public class RibbonContentPresenter : ContentPresenter
    {
        public static readonly DependencyProperty ControlSizeDefinitionProperty =
            RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonContentPresenter));

        public static readonly DependencyProperty IsInControlGroupProperty =
            RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonContentPresenter));

        public static readonly DependencyProperty IsInQuickAccessToolBarProperty =
            RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonContentPresenter));

        public RibbonControlSizeDefinition ControlSizeDefinition
        {
            get => (RibbonControlSizeDefinition)GetValue(ControlSizeDefinitionProperty);
            set => SetValue(ControlSizeDefinitionProperty, value);
        }

        public bool IsInControlGroup => (bool)GetValue(IsInControlGroupProperty);

        public bool IsInQuickAccessToolBar => (bool)GetValue(IsInQuickAccessToolBarProperty);
    }

    public class RibbonControlGroup : ItemsControl
    {
        public static readonly DependencyProperty ControlSizeDefinitionProperty =
            RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonControlGroup));

        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonControlGroup));

        public RibbonControlSizeDefinition ControlSizeDefinition
        {
            get => (RibbonControlSizeDefinition)GetValue(ControlSizeDefinitionProperty);
            set => SetValue(ControlSizeDefinitionProperty, value);
        }

        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            RibbonControlService.SetIsInControlGroup(element, true);
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);
            RibbonControlService.SetIsInControlGroup(element, false);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (VisualChildrenCount > 0)
            {
                return base.MeasureOverride(constraint);
            }

            double w = 0.0;
            double h = 24.0;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is UIElement child)
                {
                    child.Measure(constraint);
                    w += child.DesiredSize.Width;
                    h = Math.Max(h, child.DesiredSize.Height);
                }
            }
            return new Size(w, h);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonControlGroupAutomationPeer(this);
    }

    public class RibbonTwoLineText : Control
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(RibbonTwoLineText),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HasTwoLinesProperty =
            DependencyProperty.Register(nameof(HasTwoLines), typeof(bool), typeof(RibbonTwoLineText),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LineStackingStrategyProperty =
            DependencyProperty.Register(nameof(LineStackingStrategy), typeof(LineStackingStrategy), typeof(RibbonTwoLineText), new FrameworkPropertyMetadata(LineStackingStrategy.MaxHeight));

        public static readonly DependencyProperty BaselineOffsetProperty =
            DependencyProperty.Register(nameof(BaselineOffset), typeof(double), typeof(RibbonTwoLineText), new FrameworkPropertyMetadata(double.NaN));

        public static readonly DependencyProperty PathDataProperty =
            DependencyProperty.Register(nameof(PathData), typeof(Geometry), typeof(RibbonTwoLineText), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PathFillProperty =
            DependencyProperty.Register(nameof(PathFill), typeof(Brush), typeof(RibbonTwoLineText), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PathStrokeProperty =
            DependencyProperty.Register(nameof(PathStroke), typeof(Brush), typeof(RibbonTwoLineText), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner(typeof(RibbonTwoLineText));

        public static readonly DependencyProperty TextDecorationsProperty =
            DependencyProperty.Register(nameof(TextDecorations), typeof(TextDecorationCollection), typeof(RibbonTwoLineText), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TextEffectsProperty =
            DependencyProperty.Register(nameof(TextEffects), typeof(TextEffectCollection), typeof(RibbonTwoLineText), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TextTrimmingProperty =
            TextBlock.TextTrimmingProperty.AddOwner(typeof(RibbonTwoLineText));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public bool HasTwoLines
        {
            get => (bool)GetValue(HasTwoLinesProperty);
            set => SetValue(HasTwoLinesProperty, value);
        }

        public LineStackingStrategy LineStackingStrategy
        {
            get => (LineStackingStrategy)GetValue(LineStackingStrategyProperty);
            set => SetValue(LineStackingStrategyProperty, value);
        }

        public double BaselineOffset
        {
            get => (double)GetValue(BaselineOffsetProperty);
            set => SetValue(BaselineOffsetProperty, value);
        }

        public Geometry PathData
        {
            get => (Geometry)GetValue(PathDataProperty);
            set => SetValue(PathDataProperty, value);
        }

        public Brush PathFill
        {
            get => (Brush)GetValue(PathFillProperty);
            set => SetValue(PathFillProperty, value);
        }

        public Brush PathStroke
        {
            get => (Brush)GetValue(PathStrokeProperty);
            set => SetValue(PathStrokeProperty, value);
        }

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        public TextDecorationCollection TextDecorations
        {
            get => (TextDecorationCollection)GetValue(TextDecorationsProperty);
            set => SetValue(TextDecorationsProperty, value);
        }

        public TextEffectCollection TextEffects
        {
            get => (TextEffectCollection)GetValue(TextEffectsProperty);
            set => SetValue(TextEffectsProperty, value);
        }

        public TextTrimming TextTrimming
        {
            get => (TextTrimming)GetValue(TextTrimmingProperty);
            set => SetValue(TextTrimmingProperty, value);
        }

        private FormattedText CreateFormattedText(string text)
        {
            var typeface = new Typeface(FontFamily ?? SystemFonts.MessageFontFamily, FontStyle, FontWeight, FontStretch);
            var ft = new FormattedText(
                text ?? string.Empty,
                CultureInfo.CurrentUICulture,
                FlowDirection,
                typeface,
                FontSize > 0 ? FontSize : SystemFonts.MessageFontSize,
                Foreground ?? Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            ft.TextAlignment = TextAlignment;
            return ft;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (VisualChildrenCount > 0)
            {
                return base.MeasureOverride(constraint);
            }

            string text = Text;
            if (string.IsNullOrEmpty(text))
            {
                return new Size(0.0, HasTwoLines ? FontSize * 2.4 : 0.0);
            }

            FormattedText ft = CreateFormattedText(text);
            double width = ft.Width;
            double height = HasTwoLines ? ft.Height * 2.0 : ft.Height;
            if (!double.IsInfinity(constraint.Width))
            {
                width = Math.Min(width, constraint.Width);
            }
            return new Size(width, height);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (drawingContext == null || VisualChildrenCount > 0)
            {
                return;
            }

            string text = Text;
            if (!string.IsNullOrEmpty(text))
            {
                FormattedText ft = CreateFormattedText(text);
                if (!double.IsInfinity(RenderSize.Width) && RenderSize.Width > 0)
                {
                    ft.MaxTextWidth = RenderSize.Width;
                }
                drawingContext.DrawText(ft, new Point(0.0, 0.0));
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTwoLineTextAutomationPeer(this);
    }

    public class RibbonToolTip : ToolTip
    {
        public static readonly DependencyProperty TitleProperty =
            RibbonControlService.ToolTipTitleProperty.AddOwner(typeof(RibbonToolTip),
                new FrameworkPropertyMetadata(null, OnHeaderChanged));

        public static readonly DependencyProperty DescriptionProperty =
            RibbonControlService.ToolTipDescriptionProperty.AddOwner(typeof(RibbonToolTip),
                new FrameworkPropertyMetadata(null, OnHeaderChanged));

        public static readonly DependencyProperty ImageSourceProperty =
            RibbonControlService.ToolTipImageSourceProperty.AddOwner(typeof(RibbonToolTip),
                new FrameworkPropertyMetadata(null, OnHeaderChanged));

        public static readonly DependencyProperty FooterTitleProperty =
            RibbonControlService.ToolTipFooterTitleProperty.AddOwner(typeof(RibbonToolTip),
                new FrameworkPropertyMetadata(null, OnFooterChanged));

        public static readonly DependencyProperty FooterDescriptionProperty =
            RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner(typeof(RibbonToolTip),
                new FrameworkPropertyMetadata(null, OnFooterChanged));

        public static readonly DependencyProperty FooterImageSourceProperty =
            RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner(typeof(RibbonToolTip),
                new FrameworkPropertyMetadata(null, OnFooterChanged));

        private static readonly DependencyPropertyKey HasHeaderPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasHeader), typeof(bool), typeof(RibbonToolTip), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty HasHeaderProperty = HasHeaderPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey HasFooterPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasFooter), typeof(bool), typeof(RibbonToolTip), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty HasFooterProperty = HasFooterPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey IsPlacementTargetInRibbonPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsPlacementTargetInRibbon), typeof(bool), typeof(RibbonToolTip), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsPlacementTargetInRibbonProperty = IsPlacementTargetInRibbonPropertyKey.DependencyProperty;

        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonToolTip));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public ImageSource ImageSource
        {
            get => (ImageSource)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        public string FooterTitle
        {
            get => (string)GetValue(FooterTitleProperty);
            set => SetValue(FooterTitleProperty, value);
        }

        public string FooterDescription
        {
            get => (string)GetValue(FooterDescriptionProperty);
            set => SetValue(FooterDescriptionProperty, value);
        }

        public ImageSource FooterImageSource
        {
            get => (ImageSource)GetValue(FooterImageSourceProperty);
            set => SetValue(FooterImageSourceProperty, value);
        }

        public bool HasHeader => (bool)GetValue(HasHeaderProperty);

        public bool HasFooter => (bool)GetValue(HasFooterProperty);

        public bool IsPlacementTargetInRibbon => (bool)GetValue(IsPlacementTargetInRibbonProperty);

        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);

        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tip = (RibbonToolTip)d;
            bool hasHeader = !string.IsNullOrEmpty(tip.Title) || !string.IsNullOrEmpty(tip.Description) || tip.ImageSource != null;
            tip.SetValue(HasHeaderPropertyKey, hasHeader);
        }

        private static void OnFooterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tip = (RibbonToolTip)d;
            bool hasFooter = !string.IsNullOrEmpty(tip.FooterTitle) || !string.IsNullOrEmpty(tip.FooterDescription) || tip.FooterImageSource != null;
            tip.SetValue(HasFooterPropertyKey, hasFooter);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonToolTipAutomationPeer(this);
    }

    internal static class RibbonVisualDefaults
    {
        public static readonly Brush ChromeBackground = CreateFrozenBrush(Color.FromRgb(0xF5, 0xF6, 0xF7));
        public static readonly Brush ChromeBorder = CreateFrozenBrush(Color.FromRgb(0xDA, 0xDD, 0xE1));
        public static readonly Brush HoverBackground = CreateFrozenBrush(Color.FromRgb(0xE0, 0xEB, 0xF5));
        public static readonly Brush HoverBorder = CreateFrozenBrush(Color.FromRgb(0x98, 0xC1, 0xE6));
        public static readonly Brush PressedBackground = CreateFrozenBrush(Color.FromRgb(0xC6, 0xDC, 0xF0));
        public static readonly Brush PressedBorder = CreateFrozenBrush(Color.FromRgb(0x6C, 0xA2, 0xD6));
        public static readonly Brush AccentBlue = CreateFrozenBrush(Color.FromRgb(0x18, 0x5A, 0xBD));
        public static readonly Brush SubText = CreateFrozenBrush(Color.FromRgb(0x5A, 0x5A, 0x5A));
        public static readonly Pen SeparatorPen = CreateFrozenPen(ChromeBorder, 1.0);
        public static readonly Pen BorderPen = CreateFrozenPen(HoverBorder, 1.0);

        private static SolidColorBrush CreateFrozenBrush(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private static Pen CreateFrozenPen(Brush b, double thickness)
        {
            var p = new Pen(b, thickness);
            p.Freeze();
            return p;
        }

        public static void DrawLabelAndChrome(
            DrawingContext dc,
            Control owner,
            Size renderSize,
            string label,
            bool isHighlighted,
            bool isPressedOrChecked,
            bool showDropDownArrow = false)
        {
            if (dc == null || renderSize.Width <= 0 || renderSize.Height <= 0)
            {
                return;
            }

            Brush bg = owner.Background;
            Pen pen = null;
            if (isPressedOrChecked)
            {
                bg = PressedBackground;
                pen = BorderPen;
            }
            else if (isHighlighted)
            {
                bg = HoverBackground;
                pen = BorderPen;
            }

            if (bg != null || pen != null)
            {
                dc.DrawRoundedRectangle(bg, pen, new Rect(0.5, 0.5, Math.Max(0, renderSize.Width - 1.0), Math.Max(0, renderSize.Height - 1.0)), 2.0, 2.0);
            }

            if (!string.IsNullOrEmpty(label))
            {
                var typeface = new Typeface(owner.FontFamily ?? SystemFonts.MessageFontFamily, owner.FontStyle, owner.FontWeight, owner.FontStretch);
                var ft = new FormattedText(
                    label,
                    CultureInfo.CurrentUICulture,
                    owner.FlowDirection,
                    typeface,
                    owner.FontSize > 0 ? owner.FontSize : 12.0,
                    owner.Foreground ?? Brushes.Black,
                    VisualTreeHelper.GetDpi(owner).PixelsPerDip);

                double arrowReserve = showDropDownArrow ? 14.0 : 0.0;
                double x = Math.Max(4.0, (renderSize.Width - arrowReserve - ft.Width) * 0.5);
                double y = Math.Max(2.0, (renderSize.Height - ft.Height) * 0.5);
                dc.DrawText(ft, new Point(x, y));
            }

            if (showDropDownArrow && renderSize.Width >= 12.0 && renderSize.Height >= 8.0)
            {
                double cx = renderSize.Width - 8.0;
                double cy = renderSize.Height * 0.5;
                var geo = new StreamGeometry();
                using (StreamGeometryContext ctx = geo.Open())
                {
                    ctx.BeginFigure(new Point(cx - 3.0, cy - 1.5), true, true);
                    ctx.LineTo(new Point(cx + 3.0, cy - 1.5), true, false);
                    ctx.LineTo(new Point(cx, cy + 2.0), true, false);
                }
                geo.Freeze();
                dc.DrawGeometry(owner.Foreground ?? Brushes.Black, null, geo);
            }
        }
    }
}
