// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace System.Windows.Controls.Ribbon
{
    public class RibbonButton : Button
    {
        public static readonly DependencyProperty LargeImageSourceProperty =
            RibbonControlService.LargeImageSourceProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty SmallImageSourceProperty =
            RibbonControlService.SmallImageSourceProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty LabelProperty =
            RibbonControlService.LabelProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty ToolTipTitleProperty =
            RibbonControlService.ToolTipTitleProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty ToolTipDescriptionProperty =
            RibbonControlService.ToolTipDescriptionProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty ToolTipImageSourceProperty =
            RibbonControlService.ToolTipImageSourceProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty ToolTipFooterTitleProperty =
            RibbonControlService.ToolTipFooterTitleProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty ToolTipFooterDescriptionProperty =
            RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty ToolTipFooterImageSourceProperty =
            RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty ControlSizeDefinitionProperty =
            RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty IsInControlGroupProperty =
            RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty QuickAccessToolBarIdProperty =
            RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
            RibbonControlService.CanAddToQuickAccessToolBarProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty IsInQuickAccessToolBarProperty =
            RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty MouseOverBorderBrushProperty =
            RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty MouseOverBackgroundProperty =
            RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty PressedBorderBrushProperty =
            RibbonControlService.PressedBorderBrushProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty PressedBackgroundProperty =
            RibbonControlService.PressedBackgroundProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonButton));

        public static readonly DependencyProperty KeyTipProperty =
            KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonButton));

        public ImageSource LargeImageSource { get => (ImageSource)GetValue(LargeImageSourceProperty); set => SetValue(LargeImageSourceProperty, value); }
        public ImageSource SmallImageSource { get => (ImageSource)GetValue(SmallImageSourceProperty); set => SetValue(SmallImageSourceProperty, value); }
        public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
        public string ToolTipTitle { get => (string)GetValue(ToolTipTitleProperty); set => SetValue(ToolTipTitleProperty, value); }
        public string ToolTipDescription { get => (string)GetValue(ToolTipDescriptionProperty); set => SetValue(ToolTipDescriptionProperty, value); }
        public ImageSource ToolTipImageSource { get => (ImageSource)GetValue(ToolTipImageSourceProperty); set => SetValue(ToolTipImageSourceProperty, value); }
        public string ToolTipFooterTitle { get => (string)GetValue(ToolTipFooterTitleProperty); set => SetValue(ToolTipFooterTitleProperty, value); }
        public string ToolTipFooterDescription { get => (string)GetValue(ToolTipFooterDescriptionProperty); set => SetValue(ToolTipFooterDescriptionProperty, value); }
        public ImageSource ToolTipFooterImageSource { get => (ImageSource)GetValue(ToolTipFooterImageSourceProperty); set => SetValue(ToolTipFooterImageSourceProperty, value); }
        public RibbonControlSizeDefinition ControlSizeDefinition { get => (RibbonControlSizeDefinition)GetValue(ControlSizeDefinitionProperty); set => SetValue(ControlSizeDefinitionProperty, value); }
        public bool IsInControlGroup => (bool)GetValue(IsInControlGroupProperty);
        public object QuickAccessToolBarId { get => GetValue(QuickAccessToolBarIdProperty); set => SetValue(QuickAccessToolBarIdProperty, value); }
        public bool CanAddToQuickAccessToolBar { get => (bool)GetValue(CanAddToQuickAccessToolBarProperty); set => SetValue(CanAddToQuickAccessToolBarProperty, value); }
        public bool IsInQuickAccessToolBar => (bool)GetValue(IsInQuickAccessToolBarProperty);
        public Brush MouseOverBorderBrush { get => (Brush)GetValue(MouseOverBorderBrushProperty); set => SetValue(MouseOverBorderBrushProperty, value); }
        public Brush MouseOverBackground { get => (Brush)GetValue(MouseOverBackgroundProperty); set => SetValue(MouseOverBackgroundProperty, value); }
        public Brush PressedBorderBrush { get => (Brush)GetValue(PressedBorderBrushProperty); set => SetValue(PressedBorderBrushProperty, value); }
        public Brush PressedBackground { get => (Brush)GetValue(PressedBackgroundProperty); set => SetValue(PressedBackgroundProperty, value); }
        public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);
        public string KeyTip { get => (string)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

        protected override Size MeasureOverride(Size constraint)
        {
            if (VisualChildrenCount > 0)
            {
                return base.MeasureOverride(constraint);
            }

            string text = Label ?? Content?.ToString() ?? string.Empty;
            bool isLarge = ControlSizeDefinition?.ImageSize == RibbonImageSize.Large ||
                           (ControlSizeDefinition == null && LargeImageSource != null);
            double minW = Math.Max(26.0, text.Length * 7.2 + 16.0);
            double minH = isLarge ? 66.0 : 24.0;
            return new Size(Math.Min(minW, constraint.Width), Math.Min(minH, constraint.Height));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (VisualChildrenCount > 0)
            {
                base.OnRender(drawingContext);
                return;
            }
            RibbonVisualDefaults.DrawLabelAndChrome(drawingContext, this, RenderSize, Label ?? Content?.ToString(), IsMouseOver, IsPressed);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonButtonAutomationPeer(this);
    }

    public class RibbonToggleButton : ToggleButton
    {
        public static readonly DependencyProperty LargeImageSourceProperty = RibbonControlService.LargeImageSourceProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty SmallImageSourceProperty = RibbonControlService.SmallImageSourceProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty LabelProperty = RibbonControlService.LabelProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty ToolTipTitleProperty = RibbonControlService.ToolTipTitleProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty ToolTipDescriptionProperty = RibbonControlService.ToolTipDescriptionProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty ToolTipImageSourceProperty = RibbonControlService.ToolTipImageSourceProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty ToolTipFooterTitleProperty = RibbonControlService.ToolTipFooterTitleProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty ToolTipFooterDescriptionProperty = RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty ToolTipFooterImageSourceProperty = RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty ControlSizeDefinitionProperty = RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty IsInControlGroupProperty = RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty = RibbonControlService.CanAddToQuickAccessToolBarProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty IsInQuickAccessToolBarProperty = RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty MouseOverBorderBrushProperty = RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty MouseOverBackgroundProperty = RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty PressedBorderBrushProperty = RibbonControlService.PressedBorderBrushProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty PressedBackgroundProperty = RibbonControlService.PressedBackgroundProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty CheckedBorderBrushProperty = RibbonControlService.CheckedBorderBrushProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty CheckedBackgroundProperty = RibbonControlService.CheckedBackgroundProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty FocusedBorderBrushProperty = RibbonControlService.FocusedBorderBrushProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty FocusedBackgroundProperty = RibbonControlService.FocusedBackgroundProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonToggleButton));
        public static readonly DependencyProperty KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonToggleButton));

        public ImageSource LargeImageSource { get => (ImageSource)GetValue(LargeImageSourceProperty); set => SetValue(LargeImageSourceProperty, value); }
        public ImageSource SmallImageSource { get => (ImageSource)GetValue(SmallImageSourceProperty); set => SetValue(SmallImageSourceProperty, value); }
        public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
        public string ToolTipTitle { get => (string)GetValue(ToolTipTitleProperty); set => SetValue(ToolTipTitleProperty, value); }
        public string ToolTipDescription { get => (string)GetValue(ToolTipDescriptionProperty); set => SetValue(ToolTipDescriptionProperty, value); }
        public ImageSource ToolTipImageSource { get => (ImageSource)GetValue(ToolTipImageSourceProperty); set => SetValue(ToolTipImageSourceProperty, value); }
        public string ToolTipFooterTitle { get => (string)GetValue(ToolTipFooterTitleProperty); set => SetValue(ToolTipFooterTitleProperty, value); }
        public string ToolTipFooterDescription { get => (string)GetValue(ToolTipFooterDescriptionProperty); set => SetValue(ToolTipFooterDescriptionProperty, value); }
        public ImageSource ToolTipFooterImageSource { get => (ImageSource)GetValue(ToolTipFooterImageSourceProperty); set => SetValue(ToolTipFooterImageSourceProperty, value); }
        public RibbonControlSizeDefinition ControlSizeDefinition { get => (RibbonControlSizeDefinition)GetValue(ControlSizeDefinitionProperty); set => SetValue(ControlSizeDefinitionProperty, value); }
        public bool IsInControlGroup => (bool)GetValue(IsInControlGroupProperty);
        public object QuickAccessToolBarId { get => GetValue(QuickAccessToolBarIdProperty); set => SetValue(QuickAccessToolBarIdProperty, value); }
        public bool CanAddToQuickAccessToolBar { get => (bool)GetValue(CanAddToQuickAccessToolBarProperty); set => SetValue(CanAddToQuickAccessToolBarProperty, value); }
        public bool IsInQuickAccessToolBar => (bool)GetValue(IsInQuickAccessToolBarProperty);
        public Brush MouseOverBorderBrush { get => (Brush)GetValue(MouseOverBorderBrushProperty); set => SetValue(MouseOverBorderBrushProperty, value); }
        public Brush MouseOverBackground { get => (Brush)GetValue(MouseOverBackgroundProperty); set => SetValue(MouseOverBackgroundProperty, value); }
        public Brush PressedBorderBrush { get => (Brush)GetValue(PressedBorderBrushProperty); set => SetValue(PressedBorderBrushProperty, value); }
        public Brush PressedBackground { get => (Brush)GetValue(PressedBackgroundProperty); set => SetValue(PressedBackgroundProperty, value); }
        public Brush CheckedBorderBrush { get => (Brush)GetValue(CheckedBorderBrushProperty); set => SetValue(CheckedBorderBrushProperty, value); }
        public Brush CheckedBackground { get => (Brush)GetValue(CheckedBackgroundProperty); set => SetValue(CheckedBackgroundProperty, value); }
        public Brush FocusedBorderBrush { get => (Brush)GetValue(FocusedBorderBrushProperty); set => SetValue(FocusedBorderBrushProperty, value); }
        public Brush FocusedBackground { get => (Brush)GetValue(FocusedBackgroundProperty); set => SetValue(FocusedBackgroundProperty, value); }
        public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);
        public string KeyTip { get => (string)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

        protected override Size MeasureOverride(Size constraint)
        {
            if (VisualChildrenCount > 0) return base.MeasureOverride(constraint);
            string text = Label ?? Content?.ToString() ?? string.Empty;
            return new Size(Math.Min(Math.Max(26.0, text.Length * 7.2 + 16.0), constraint.Width), Math.Min(24.0, constraint.Height));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (VisualChildrenCount > 0) { base.OnRender(drawingContext); return; }
            RibbonVisualDefaults.DrawLabelAndChrome(drawingContext, this, RenderSize, Label ?? Content?.ToString(), IsMouseOver, IsPressed || IsChecked == true);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonToggleButtonAutomationPeer(this);
    }

    public class RibbonRadioButton : RadioButton
    {
        public static readonly DependencyProperty LargeImageSourceProperty = RibbonControlService.LargeImageSourceProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty SmallImageSourceProperty = RibbonControlService.SmallImageSourceProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty LabelProperty = RibbonControlService.LabelProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty ControlSizeDefinitionProperty = RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty IsInControlGroupProperty = RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty = RibbonControlService.CanAddToQuickAccessToolBarProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty IsInQuickAccessToolBarProperty = RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty MouseOverBorderBrushProperty = RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty MouseOverBackgroundProperty = RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty PressedBorderBrushProperty = RibbonControlService.PressedBorderBrushProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty PressedBackgroundProperty = RibbonControlService.PressedBackgroundProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty CheckedBorderBrushProperty = RibbonControlService.CheckedBorderBrushProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty CheckedBackgroundProperty = RibbonControlService.CheckedBackgroundProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonRadioButton));
        public static readonly DependencyProperty KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonRadioButton));

        public ImageSource LargeImageSource { get => (ImageSource)GetValue(LargeImageSourceProperty); set => SetValue(LargeImageSourceProperty, value); }
        public ImageSource SmallImageSource { get => (ImageSource)GetValue(SmallImageSourceProperty); set => SetValue(SmallImageSourceProperty, value); }
        public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
        public RibbonControlSizeDefinition ControlSizeDefinition { get => (RibbonControlSizeDefinition)GetValue(ControlSizeDefinitionProperty); set => SetValue(ControlSizeDefinitionProperty, value); }
        public bool IsInControlGroup => (bool)GetValue(IsInControlGroupProperty);
        public object QuickAccessToolBarId { get => GetValue(QuickAccessToolBarIdProperty); set => SetValue(QuickAccessToolBarIdProperty, value); }
        public bool CanAddToQuickAccessToolBar { get => (bool)GetValue(CanAddToQuickAccessToolBarProperty); set => SetValue(CanAddToQuickAccessToolBarProperty, value); }
        public bool IsInQuickAccessToolBar => (bool)GetValue(IsInQuickAccessToolBarProperty);
        public Brush MouseOverBorderBrush { get => (Brush)GetValue(MouseOverBorderBrushProperty); set => SetValue(MouseOverBorderBrushProperty, value); }
        public Brush MouseOverBackground { get => (Brush)GetValue(MouseOverBackgroundProperty); set => SetValue(MouseOverBackgroundProperty, value); }
        public Brush PressedBorderBrush { get => (Brush)GetValue(PressedBorderBrushProperty); set => SetValue(PressedBorderBrushProperty, value); }
        public Brush PressedBackground { get => (Brush)GetValue(PressedBackgroundProperty); set => SetValue(PressedBackgroundProperty, value); }
        public Brush CheckedBorderBrush { get => (Brush)GetValue(CheckedBorderBrushProperty); set => SetValue(CheckedBorderBrushProperty, value); }
        public Brush CheckedBackground { get => (Brush)GetValue(CheckedBackgroundProperty); set => SetValue(CheckedBackgroundProperty, value); }
        public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);
        public string KeyTip { get => (string)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonRadioButtonAutomationPeer(this);
    }

    public class RibbonCheckBox : CheckBox
    {
        public static readonly DependencyProperty LargeImageSourceProperty = RibbonControlService.LargeImageSourceProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty SmallImageSourceProperty = RibbonControlService.SmallImageSourceProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty LabelProperty = RibbonControlService.LabelProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty ToolTipTitleProperty = RibbonControlService.ToolTipTitleProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty ToolTipDescriptionProperty = RibbonControlService.ToolTipDescriptionProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty ToolTipImageSourceProperty = RibbonControlService.ToolTipImageSourceProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty ToolTipFooterTitleProperty = RibbonControlService.ToolTipFooterTitleProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty ToolTipFooterDescriptionProperty = RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty ToolTipFooterImageSourceProperty = RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty ControlSizeDefinitionProperty = RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty IsInControlGroupProperty = RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty = RibbonControlService.CanAddToQuickAccessToolBarProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty IsInQuickAccessToolBarProperty = RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty MouseOverBorderBrushProperty = RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty MouseOverBackgroundProperty = RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty PressedBorderBrushProperty = RibbonControlService.PressedBorderBrushProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty PressedBackgroundProperty = RibbonControlService.PressedBackgroundProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty CheckedBorderBrushProperty = RibbonControlService.CheckedBorderBrushProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty CheckedBackgroundProperty = RibbonControlService.CheckedBackgroundProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty FocusedBorderBrushProperty = RibbonControlService.FocusedBorderBrushProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty FocusedBackgroundProperty = RibbonControlService.FocusedBackgroundProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonCheckBox));
        public static readonly DependencyProperty KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonCheckBox));

        public ImageSource LargeImageSource { get => (ImageSource)GetValue(LargeImageSourceProperty); set => SetValue(LargeImageSourceProperty, value); }
        public ImageSource SmallImageSource { get => (ImageSource)GetValue(SmallImageSourceProperty); set => SetValue(SmallImageSourceProperty, value); }
        public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
        public string ToolTipTitle { get => (string)GetValue(ToolTipTitleProperty); set => SetValue(ToolTipTitleProperty, value); }
        public string ToolTipDescription { get => (string)GetValue(ToolTipDescriptionProperty); set => SetValue(ToolTipDescriptionProperty, value); }
        public ImageSource ToolTipImageSource { get => (ImageSource)GetValue(ToolTipImageSourceProperty); set => SetValue(ToolTipImageSourceProperty, value); }
        public string ToolTipFooterTitle { get => (string)GetValue(ToolTipFooterTitleProperty); set => SetValue(ToolTipFooterTitleProperty, value); }
        public string ToolTipFooterDescription { get => (string)GetValue(ToolTipFooterDescriptionProperty); set => SetValue(ToolTipFooterDescriptionProperty, value); }
        public ImageSource ToolTipFooterImageSource { get => (ImageSource)GetValue(ToolTipFooterImageSourceProperty); set => SetValue(ToolTipFooterImageSourceProperty, value); }
        public RibbonControlSizeDefinition ControlSizeDefinition { get => (RibbonControlSizeDefinition)GetValue(ControlSizeDefinitionProperty); set => SetValue(ControlSizeDefinitionProperty, value); }
        public bool IsInControlGroup => (bool)GetValue(IsInControlGroupProperty);
        public object QuickAccessToolBarId { get => GetValue(QuickAccessToolBarIdProperty); set => SetValue(QuickAccessToolBarIdProperty, value); }
        public bool CanAddToQuickAccessToolBar { get => (bool)GetValue(CanAddToQuickAccessToolBarProperty); set => SetValue(CanAddToQuickAccessToolBarProperty, value); }
        public bool IsInQuickAccessToolBar => (bool)GetValue(IsInQuickAccessToolBarProperty);
        public Brush MouseOverBorderBrush { get => (Brush)GetValue(MouseOverBorderBrushProperty); set => SetValue(MouseOverBorderBrushProperty, value); }
        public Brush MouseOverBackground { get => (Brush)GetValue(MouseOverBackgroundProperty); set => SetValue(MouseOverBackgroundProperty, value); }
        public Brush PressedBorderBrush { get => (Brush)GetValue(PressedBorderBrushProperty); set => SetValue(PressedBorderBrushProperty, value); }
        public Brush PressedBackground { get => (Brush)GetValue(PressedBackgroundProperty); set => SetValue(PressedBackgroundProperty, value); }
        public Brush CheckedBorderBrush { get => (Brush)GetValue(CheckedBorderBrushProperty); set => SetValue(CheckedBorderBrushProperty, value); }
        public Brush CheckedBackground { get => (Brush)GetValue(CheckedBackgroundProperty); set => SetValue(CheckedBackgroundProperty, value); }
        public Brush FocusedBorderBrush { get => (Brush)GetValue(FocusedBorderBrushProperty); set => SetValue(FocusedBorderBrushProperty, value); }
        public Brush FocusedBackground { get => (Brush)GetValue(FocusedBackgroundProperty); set => SetValue(FocusedBackgroundProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);
        public string KeyTip { get => (string)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonCheckBoxAutomationPeer(this);
    }

    public class RibbonMenuButton : ItemsControl
    {
        public static readonly DependencyProperty IsDropDownOpenProperty =
            ComboBox.IsDropDownOpenProperty.AddOwner(typeof(RibbonMenuButton),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

        public static readonly DependencyProperty MaxDropDownHeightProperty =
            ComboBox.MaxDropDownHeightProperty.AddOwner(typeof(RibbonMenuButton));

        public static readonly DependencyProperty CanUserResizeHorizontallyProperty =
            DependencyProperty.Register(nameof(CanUserResizeHorizontally), typeof(bool), typeof(RibbonMenuButton), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty CanUserResizeVerticallyProperty =
            DependencyProperty.Register(nameof(CanUserResizeVertically), typeof(bool), typeof(RibbonMenuButton), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty DropDownHeightProperty =
            DependencyProperty.Register(nameof(DropDownHeight), typeof(double), typeof(RibbonMenuButton), new FrameworkPropertyMetadata(double.NaN));

        private static readonly DependencyPropertyKey HasGalleryPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasGallery), typeof(bool), typeof(RibbonMenuButton), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty HasGalleryProperty = HasGalleryPropertyKey.DependencyProperty;

        public static readonly DependencyProperty LargeImageSourceProperty = RibbonControlService.LargeImageSourceProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty SmallImageSourceProperty = RibbonControlService.SmallImageSourceProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty LabelProperty = RibbonControlService.LabelProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty ControlSizeDefinitionProperty = RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty IsInControlGroupProperty = RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty = RibbonControlService.CanAddToQuickAccessToolBarProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty IsInQuickAccessToolBarProperty = RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty MouseOverBorderBrushProperty = RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty MouseOverBackgroundProperty = RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty PressedBorderBrushProperty = RibbonControlService.PressedBorderBrushProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty PressedBackgroundProperty = RibbonControlService.PressedBackgroundProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty FocusedBorderBrushProperty = RibbonControlService.FocusedBorderBrushProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty FocusedBackgroundProperty = RibbonControlService.FocusedBackgroundProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonMenuButton));
        public static readonly DependencyProperty KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonMenuButton));

        public bool IsDropDownOpen { get => (bool)GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }
        public double MaxDropDownHeight { get => (double)GetValue(MaxDropDownHeightProperty); set => SetValue(MaxDropDownHeightProperty, value); }
        public bool CanUserResizeHorizontally { get => (bool)GetValue(CanUserResizeHorizontallyProperty); set => SetValue(CanUserResizeHorizontallyProperty, value); }
        public bool CanUserResizeVertically { get => (bool)GetValue(CanUserResizeVerticallyProperty); set => SetValue(CanUserResizeVerticallyProperty, value); }
        public double DropDownHeight { get => (double)GetValue(DropDownHeightProperty); set => SetValue(DropDownHeightProperty, value); }
        public bool HasGallery { get => (bool)GetValue(HasGalleryProperty); internal set => SetValue(HasGalleryPropertyKey, value); }
        public ImageSource LargeImageSource { get => (ImageSource)GetValue(LargeImageSourceProperty); set => SetValue(LargeImageSourceProperty, value); }
        public ImageSource SmallImageSource { get => (ImageSource)GetValue(SmallImageSourceProperty); set => SetValue(SmallImageSourceProperty, value); }
        public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
        public RibbonControlSizeDefinition ControlSizeDefinition { get => (RibbonControlSizeDefinition)GetValue(ControlSizeDefinitionProperty); set => SetValue(ControlSizeDefinitionProperty, value); }
        public bool IsInControlGroup => (bool)GetValue(IsInControlGroupProperty);
        public object QuickAccessToolBarId { get => GetValue(QuickAccessToolBarIdProperty); set => SetValue(QuickAccessToolBarIdProperty, value); }
        public bool CanAddToQuickAccessToolBar { get => (bool)GetValue(CanAddToQuickAccessToolBarProperty); set => SetValue(CanAddToQuickAccessToolBarProperty, value); }
        public bool IsInQuickAccessToolBar => (bool)GetValue(IsInQuickAccessToolBarProperty);
        public Brush MouseOverBorderBrush { get => (Brush)GetValue(MouseOverBorderBrushProperty); set => SetValue(MouseOverBorderBrushProperty, value); }
        public Brush MouseOverBackground { get => (Brush)GetValue(MouseOverBackgroundProperty); set => SetValue(MouseOverBackgroundProperty, value); }
        public Brush PressedBorderBrush { get => (Brush)GetValue(PressedBorderBrushProperty); set => SetValue(PressedBorderBrushProperty, value); }
        public Brush PressedBackground { get => (Brush)GetValue(PressedBackgroundProperty); set => SetValue(PressedBackgroundProperty, value); }
        public Brush FocusedBorderBrush { get => (Brush)GetValue(FocusedBorderBrushProperty); set => SetValue(FocusedBorderBrushProperty, value); }
        public Brush FocusedBackground { get => (Brush)GetValue(FocusedBackgroundProperty); set => SetValue(FocusedBackgroundProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);
        public string KeyTip { get => (string)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

        public event EventHandler DropDownOpened;
        public event EventHandler DropDownClosed;

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (RibbonMenuButton)d;
            if ((bool)e.NewValue)
            {
                button.OnDropDownOpened(EventArgs.Empty);
            }
            else
            {
                button.OnDropDownClosed(EventArgs.Empty);
            }
        }

        protected virtual void OnDropDownOpened(EventArgs e) => DropDownOpened?.Invoke(this, e);
        protected virtual void OnDropDownClosed(EventArgs e)
        {
            // Cleanly replace WPF's legacy private reflection against MenuBase.HasPushedMenuMode / PopMenuMode()
            if (VisualTreeHelper.GetChildrenCount(this) > 0)
            {
                PopChildMenuMode(this);
            }
            DropDownClosed?.Invoke(this, e);
        }

        private static void PopChildMenuMode(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is MenuBase menu && menu.HasPushedMenuMode)
                {
                    menu.PopMenuMode();
                }
                else
                {
                    PopChildMenuMode(child);
                }
            }
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
            => item is RibbonMenuItem || item is RibbonSeparator || item is RibbonGallery;

        protected override DependencyObject GetContainerForItemOverride()
            => new RibbonMenuItem();

        protected override Size MeasureOverride(Size constraint)
        {
            if (VisualChildrenCount > 0) return base.MeasureOverride(constraint);
            string text = Label ?? string.Empty;
            return new Size(Math.Min(Math.Max(34.0, text.Length * 7.2 + 24.0), constraint.Width), Math.Min(24.0, constraint.Height));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (VisualChildrenCount > 0) { base.OnRender(drawingContext); return; }
            RibbonVisualDefaults.DrawLabelAndChrome(drawingContext, this, RenderSize, Label, IsMouseOver, IsDropDownOpen, showDropDownArrow: true);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonMenuButtonAutomationPeer(this);
    }

    public class RibbonSplitButton : RibbonMenuButton, ICommandSource
    {
        public static readonly RoutedEvent ClickEvent = ButtonBase.ClickEvent.AddOwner(typeof(RibbonSplitButton));
        public static readonly RoutedEvent CheckedEvent = ToggleButton.CheckedEvent.AddOwner(typeof(RibbonSplitButton));
        public static readonly RoutedEvent UncheckedEvent = ToggleButton.UncheckedEvent.AddOwner(typeof(RibbonSplitButton));

        public static readonly DependencyProperty CommandProperty = ButtonBase.CommandProperty.AddOwner(typeof(RibbonSplitButton));
        public static readonly DependencyProperty CommandParameterProperty = ButtonBase.CommandParameterProperty.AddOwner(typeof(RibbonSplitButton));
        public static readonly DependencyProperty CommandTargetProperty = ButtonBase.CommandTargetProperty.AddOwner(typeof(RibbonSplitButton));
        public static readonly DependencyProperty IsCheckedProperty = ToggleButton.IsCheckedProperty.AddOwner(typeof(RibbonSplitButton));
        public static readonly DependencyProperty IsCheckableProperty = MenuItem.IsCheckableProperty.AddOwner(typeof(RibbonSplitButton));
        public static readonly DependencyProperty CheckedBorderBrushProperty = RibbonControlService.CheckedBorderBrushProperty.AddOwner(typeof(RibbonSplitButton));
        public static readonly DependencyProperty CheckedBackgroundProperty = RibbonControlService.CheckedBackgroundProperty.AddOwner(typeof(RibbonSplitButton));
        public static readonly DependencyProperty DropDownToolTipTitleProperty = DependencyProperty.Register(nameof(DropDownToolTipTitle), typeof(string), typeof(RibbonSplitButton));
        public static readonly DependencyProperty DropDownToolTipDescriptionProperty = DependencyProperty.Register(nameof(DropDownToolTipDescription), typeof(string), typeof(RibbonSplitButton));
        public static readonly DependencyProperty DropDownToolTipImageSourceProperty = DependencyProperty.Register(nameof(DropDownToolTipImageSource), typeof(ImageSource), typeof(RibbonSplitButton));
        public static readonly DependencyProperty DropDownToolTipFooterTitleProperty = DependencyProperty.Register(nameof(DropDownToolTipFooterTitle), typeof(string), typeof(RibbonSplitButton));
        public static readonly DependencyProperty DropDownToolTipFooterDescriptionProperty = DependencyProperty.Register(nameof(DropDownToolTipFooterDescription), typeof(string), typeof(RibbonSplitButton));
        public static readonly DependencyProperty DropDownToolTipFooterImageSourceProperty = DependencyProperty.Register(nameof(DropDownToolTipFooterImageSource), typeof(ImageSource), typeof(RibbonSplitButton));
        public static readonly DependencyProperty HeaderQuickAccessToolBarIdProperty = DependencyProperty.Register(nameof(HeaderQuickAccessToolBarId), typeof(object), typeof(RibbonSplitButton));
        public static readonly DependencyProperty HeaderKeyTipProperty = DependencyProperty.Register(nameof(HeaderKeyTip), typeof(string), typeof(RibbonSplitButton));

        public ICommand Command { get => (ICommand)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
        public object CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
        public IInputElement CommandTarget { get => (IInputElement)GetValue(CommandTargetProperty); set => SetValue(CommandTargetProperty, value); }
        public bool IsChecked { get => (bool)GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
        public bool IsCheckable { get => (bool)GetValue(IsCheckableProperty); set => SetValue(IsCheckableProperty, value); }
        public Brush CheckedBorderBrush { get => (Brush)GetValue(CheckedBorderBrushProperty); set => SetValue(CheckedBorderBrushProperty, value); }
        public Brush CheckedBackground { get => (Brush)GetValue(CheckedBackgroundProperty); set => SetValue(CheckedBackgroundProperty, value); }
        public string DropDownToolTipTitle { get => (string)GetValue(DropDownToolTipTitleProperty); set => SetValue(DropDownToolTipTitleProperty, value); }
        public string DropDownToolTipDescription { get => (string)GetValue(DropDownToolTipDescriptionProperty); set => SetValue(DropDownToolTipDescriptionProperty, value); }
        public ImageSource DropDownToolTipImageSource { get => (ImageSource)GetValue(DropDownToolTipImageSourceProperty); set => SetValue(DropDownToolTipImageSourceProperty, value); }
        public string DropDownToolTipFooterTitle { get => (string)GetValue(DropDownToolTipFooterTitleProperty); set => SetValue(DropDownToolTipFooterTitleProperty, value); }
        public string DropDownToolTipFooterDescription { get => (string)GetValue(DropDownToolTipFooterDescriptionProperty); set => SetValue(DropDownToolTipFooterDescriptionProperty, value); }
        public ImageSource DropDownToolTipFooterImageSource { get => (ImageSource)GetValue(DropDownToolTipFooterImageSourceProperty); set => SetValue(DropDownToolTipFooterImageSourceProperty, value); }
        public object HeaderQuickAccessToolBarId { get => GetValue(HeaderQuickAccessToolBarIdProperty); set => SetValue(HeaderQuickAccessToolBarIdProperty, value); }
        public string HeaderKeyTip { get => (string)GetValue(HeaderKeyTipProperty); set => SetValue(HeaderKeyTipProperty, value); }

        public event RoutedEventHandler Click { add => AddHandler(ClickEvent, value); remove => RemoveHandler(ClickEvent, value); }
        public event RoutedEventHandler Checked { add => AddHandler(CheckedEvent, value); remove => RemoveHandler(CheckedEvent, value); }
        public event RoutedEventHandler Unchecked { add => AddHandler(UncheckedEvent, value); remove => RemoveHandler(UncheckedEvent, value); }

        internal void OnAutomationInvoke()
        {
            if (IsCheckable)
            {
                IsChecked = !IsChecked;
            }
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
            Command?.Execute(CommandParameter);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonSplitButtonAutomationPeer(this);
    }

    public class RibbonMenuItem : MenuItem
    {
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(RibbonMenuItem), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonMenuItem));
        public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty = RibbonControlService.CanAddToQuickAccessToolBarProperty.AddOwner(typeof(RibbonMenuItem));
        public static readonly DependencyProperty IsInQuickAccessToolBarProperty = RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonMenuItem));
        public static readonly DependencyProperty MouseOverBorderBrushProperty = RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonMenuItem));
        public static readonly DependencyProperty MouseOverBackgroundProperty = RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonMenuItem));
        public static readonly DependencyProperty CheckedBorderBrushProperty = RibbonControlService.CheckedBorderBrushProperty.AddOwner(typeof(RibbonMenuItem));
        public static readonly DependencyProperty CheckedBackgroundProperty = RibbonControlService.CheckedBackgroundProperty.AddOwner(typeof(RibbonMenuItem));
        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonMenuItem));
        public static readonly DependencyProperty KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonMenuItem));

        public ImageSource ImageSource { get => (ImageSource)GetValue(ImageSourceProperty); set => SetValue(ImageSourceProperty, value); }
        public object QuickAccessToolBarId { get => GetValue(QuickAccessToolBarIdProperty); set => SetValue(QuickAccessToolBarIdProperty, value); }
        public bool CanAddToQuickAccessToolBar { get => (bool)GetValue(CanAddToQuickAccessToolBarProperty); set => SetValue(CanAddToQuickAccessToolBarProperty, value); }
        public bool IsInQuickAccessToolBar => (bool)GetValue(IsInQuickAccessToolBarProperty);
        public Brush MouseOverBorderBrush { get => (Brush)GetValue(MouseOverBorderBrushProperty); set => SetValue(MouseOverBorderBrushProperty, value); }
        public Brush MouseOverBackground { get => (Brush)GetValue(MouseOverBackgroundProperty); set => SetValue(MouseOverBackgroundProperty, value); }
        public Brush CheckedBorderBrush { get => (Brush)GetValue(CheckedBorderBrushProperty); set => SetValue(CheckedBorderBrushProperty, value); }
        public Brush CheckedBackground { get => (Brush)GetValue(CheckedBackgroundProperty); set => SetValue(CheckedBackgroundProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);
        public string KeyTip { get => (string)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonMenuItemAutomationPeer(this);
    }

    public class RibbonSplitMenuItem : RibbonMenuItem
    {
        public static readonly DependencyProperty DropDownToolTipTitleProperty = RibbonSplitButton.DropDownToolTipTitleProperty.AddOwner(typeof(RibbonSplitMenuItem));
        public static readonly DependencyProperty DropDownToolTipDescriptionProperty = RibbonSplitButton.DropDownToolTipDescriptionProperty.AddOwner(typeof(RibbonSplitMenuItem));
        public static readonly DependencyProperty DropDownToolTipImageSourceProperty = RibbonSplitButton.DropDownToolTipImageSourceProperty.AddOwner(typeof(RibbonSplitMenuItem));
        public static readonly DependencyProperty HeaderQuickAccessToolBarIdProperty = RibbonSplitButton.HeaderQuickAccessToolBarIdProperty.AddOwner(typeof(RibbonSplitMenuItem));
        public static readonly DependencyProperty HeaderKeyTipProperty = RibbonSplitButton.HeaderKeyTipProperty.AddOwner(typeof(RibbonSplitMenuItem));

        public string DropDownToolTipTitle { get => (string)GetValue(DropDownToolTipTitleProperty); set => SetValue(DropDownToolTipTitleProperty, value); }
        public string DropDownToolTipDescription { get => (string)GetValue(DropDownToolTipDescriptionProperty); set => SetValue(DropDownToolTipDescriptionProperty, value); }
        public ImageSource DropDownToolTipImageSource { get => (ImageSource)GetValue(DropDownToolTipImageSourceProperty); set => SetValue(DropDownToolTipImageSourceProperty, value); }
        public object HeaderQuickAccessToolBarId { get => GetValue(HeaderQuickAccessToolBarIdProperty); set => SetValue(HeaderQuickAccessToolBarIdProperty, value); }
        public string HeaderKeyTip { get => (string)GetValue(HeaderKeyTipProperty); set => SetValue(HeaderKeyTipProperty, value); }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonSplitMenuItemAutomationPeer(this);
    }

    public class RibbonSeparator : Separator
    {
        public static readonly DependencyProperty LabelProperty = RibbonControlService.LabelProperty.AddOwner(typeof(RibbonSeparator));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonSeparatorAutomationPeer(this);
    }

    public class RibbonTextBox : TextBox
    {
        public static readonly DependencyProperty TextBoxWidthProperty =
            DependencyProperty.Register(nameof(TextBoxWidth), typeof(double), typeof(RibbonTextBox), new FrameworkPropertyMetadata(100.0));

        public static readonly DependencyProperty LargeImageSourceProperty = RibbonControlService.LargeImageSourceProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty SmallImageSourceProperty = RibbonControlService.SmallImageSourceProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty LabelProperty = RibbonControlService.LabelProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty ControlSizeDefinitionProperty = RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty IsInControlGroupProperty = RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty = RibbonControlService.CanAddToQuickAccessToolBarProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty IsInQuickAccessToolBarProperty = RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty MouseOverBorderBrushProperty = RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty MouseOverBackgroundProperty = RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty FocusedBorderBrushProperty = RibbonControlService.FocusedBorderBrushProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty FocusedBackgroundProperty = RibbonControlService.FocusedBackgroundProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty RibbonProperty = RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonTextBox));
        public static readonly DependencyProperty KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonTextBox));

        public double TextBoxWidth { get => (double)GetValue(TextBoxWidthProperty); set => SetValue(TextBoxWidthProperty, value); }
        public ImageSource LargeImageSource { get => (ImageSource)GetValue(LargeImageSourceProperty); set => SetValue(LargeImageSourceProperty, value); }
        public ImageSource SmallImageSource { get => (ImageSource)GetValue(SmallImageSourceProperty); set => SetValue(SmallImageSourceProperty, value); }
        public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
        public RibbonControlSizeDefinition ControlSizeDefinition { get => (RibbonControlSizeDefinition)GetValue(ControlSizeDefinitionProperty); set => SetValue(ControlSizeDefinitionProperty, value); }
        public bool IsInControlGroup => (bool)GetValue(IsInControlGroupProperty);
        public object QuickAccessToolBarId { get => GetValue(QuickAccessToolBarIdProperty); set => SetValue(QuickAccessToolBarIdProperty, value); }
        public bool CanAddToQuickAccessToolBar { get => (bool)GetValue(CanAddToQuickAccessToolBarProperty); set => SetValue(CanAddToQuickAccessToolBarProperty, value); }
        public bool IsInQuickAccessToolBar => (bool)GetValue(IsInQuickAccessToolBarProperty);
        public Brush MouseOverBorderBrush { get => (Brush)GetValue(MouseOverBorderBrushProperty); set => SetValue(MouseOverBorderBrushProperty, value); }
        public Brush MouseOverBackground { get => (Brush)GetValue(MouseOverBackgroundProperty); set => SetValue(MouseOverBackgroundProperty, value); }
        public Brush FocusedBorderBrush { get => (Brush)GetValue(FocusedBorderBrushProperty); set => SetValue(FocusedBorderBrushProperty, value); }
        public Brush FocusedBackground { get => (Brush)GetValue(FocusedBackgroundProperty); set => SetValue(FocusedBackgroundProperty, value); }
        public Ribbon Ribbon => (Ribbon)GetValue(RibbonProperty);
        public string KeyTip { get => (string)GetValue(KeyTipProperty); set => SetValue(KeyTipProperty, value); }

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTextBoxAutomationPeer(this);
    }

    public class RibbonComboBox : RibbonMenuButton
    {
        public static readonly DependencyProperty SelectionBoxItemProperty =
            DependencyProperty.Register(nameof(SelectionBoxItem), typeof(object), typeof(RibbonComboBox), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty SelectionBoxItemTemplateProperty =
            DependencyProperty.Register(nameof(SelectionBoxItemTemplate), typeof(DataTemplate), typeof(RibbonComboBox), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty SelectionBoxWidthProperty =
            DependencyProperty.Register(nameof(SelectionBoxWidth), typeof(double), typeof(RibbonComboBox), new FrameworkPropertyMetadata(double.NaN));

        public static readonly DependencyProperty IsEditableProperty =
            ComboBox.IsEditableProperty.AddOwner(typeof(RibbonComboBox));

        public static readonly DependencyProperty TextProperty =
            ComboBox.TextProperty.AddOwner(typeof(RibbonComboBox));

        public static readonly DependencyProperty StaysOpenOnEditProperty =
            ComboBox.StaysOpenOnEditProperty.AddOwner(typeof(RibbonComboBox));

        public static readonly DependencyProperty ShowKeyboardCuesProperty =
            RibbonControlService.ShowKeyboardCuesProperty.AddOwner(typeof(RibbonComboBox));

        public object SelectionBoxItem { get => GetValue(SelectionBoxItemProperty); set => SetValue(SelectionBoxItemProperty, value); }
        public DataTemplate SelectionBoxItemTemplate { get => (DataTemplate)GetValue(SelectionBoxItemTemplateProperty); set => SetValue(SelectionBoxItemTemplateProperty, value); }
        public double SelectionBoxWidth { get => (double)GetValue(SelectionBoxWidthProperty); set => SetValue(SelectionBoxWidthProperty, value); }
        public bool IsEditable { get => (bool)GetValue(IsEditableProperty); set => SetValue(IsEditableProperty, value); }
        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
        public bool StaysOpenOnEdit { get => (bool)GetValue(StaysOpenOnEditProperty); set => SetValue(StaysOpenOnEditProperty, value); }
        public bool ShowKeyboardCues => (bool)GetValue(ShowKeyboardCuesProperty);

        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonComboBoxAutomationPeer(this);
    }

    public class RibbonFilterMenuButton : RibbonMenuButton
    {
        public static readonly DependencyProperty CurrentFilterItemProperty =
            DependencyProperty.Register(nameof(CurrentFilterItem), typeof(object), typeof(RibbonFilterMenuButton), new FrameworkPropertyMetadata(null));

        public object CurrentFilterItem
        {
            get => GetValue(CurrentFilterItemProperty);
            set => SetValue(CurrentFilterItemProperty, value);
        }
    }
}
