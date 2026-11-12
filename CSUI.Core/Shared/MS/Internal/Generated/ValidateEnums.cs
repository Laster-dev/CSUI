// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Media
{
    internal static partial class ValidateEnums
    {
        public static bool IsAlignmentXValid(object valueObject)
        {
            AlignmentX value = (AlignmentX)valueObject;
            return (value == AlignmentX.Left) ||
                   (value == AlignmentX.Center) ||
                   (value == AlignmentX.Right);
        }

        public static bool IsAlignmentYValid(object valueObject)
        {
            AlignmentY value = (AlignmentY)valueObject;
            return (value == AlignmentY.Top) ||
                   (value == AlignmentY.Center) ||
                   (value == AlignmentY.Bottom);
        }

        public static bool IsBitmapScalingModeValid(object valueObject)
        {
            BitmapScalingMode value = (BitmapScalingMode)valueObject;
            return (value == BitmapScalingMode.Unspecified) ||
                   (value == BitmapScalingMode.LowQuality) ||
                   (value == BitmapScalingMode.HighQuality) ||
                   (value == BitmapScalingMode.Linear) ||
                   (value == BitmapScalingMode.Fant) ||
                   (value == BitmapScalingMode.NearestNeighbor);
        }

        public static bool IsBrushMappingModeValid(object valueObject)
        {
            BrushMappingMode value = (BrushMappingMode)valueObject;
            return (value == BrushMappingMode.Absolute) ||
                   (value == BrushMappingMode.RelativeToBoundingBox);
        }

        public static bool IsCachingHintValid(object valueObject)
        {
            CachingHint value = (CachingHint)valueObject;
            return (value == CachingHint.Unspecified) ||
                   (value == CachingHint.Cache);
        }

        public static bool IsClearTypeHintValid(object valueObject)
        {
            ClearTypeHint value = (ClearTypeHint)valueObject;
            return (value == ClearTypeHint.Auto) ||
                   (value == ClearTypeHint.Enabled);
        }

        public static bool IsColorInterpolationModeValid(object valueObject)
        {
            ColorInterpolationMode value = (ColorInterpolationMode)valueObject;
            return (value == ColorInterpolationMode.ScRgbLinearInterpolation) ||
                   (value == ColorInterpolationMode.SRgbLinearInterpolation);
        }

        public static bool IsEdgeModeValid(object valueObject)
        {
            EdgeMode value = (EdgeMode)valueObject;
            return (value == EdgeMode.Unspecified) ||
                   (value == EdgeMode.Aliased);
        }

        public static bool IsFillRuleValid(object valueObject)
        {
            FillRule value = (FillRule)valueObject;
            return (value == FillRule.EvenOdd) ||
                   (value == FillRule.Nonzero);
        }

        public static bool IsGeometryCombineModeValid(object valueObject)
        {
            GeometryCombineMode value = (GeometryCombineMode)valueObject;
            return (value == GeometryCombineMode.Union) ||
                   (value == GeometryCombineMode.Intersect) ||
                   (value == GeometryCombineMode.Xor) ||
                   (value == GeometryCombineMode.Exclude);
        }

        public static bool IsGradientSpreadMethodValid(object valueObject)
        {
            GradientSpreadMethod value = (GradientSpreadMethod)valueObject;
            return (value == GradientSpreadMethod.Pad) ||
                   (value == GradientSpreadMethod.Reflect) ||
                   (value == GradientSpreadMethod.Repeat);
        }

        public static bool IsPenLineCapValid(object valueObject)
        {
            PenLineCap value = (PenLineCap)valueObject;
            return (value == PenLineCap.Flat) ||
                   (value == PenLineCap.Square) ||
                   (value == PenLineCap.Round) ||
                   (value == PenLineCap.Triangle);
        }

        public static bool IsPenLineJoinValid(object valueObject)
        {
            PenLineJoin value = (PenLineJoin)valueObject;
            return (value == PenLineJoin.Miter) ||
                   (value == PenLineJoin.Bevel) ||
                   (value == PenLineJoin.Round);
        }

        public static bool IsRotationValid(object valueObject)
        {
            System.Windows.Media.Imaging.Rotation value = (System.Windows.Media.Imaging.Rotation)valueObject;
            return (value == System.Windows.Media.Imaging.Rotation.Rotate0) ||
                   (value == System.Windows.Media.Imaging.Rotation.Rotate90) ||
                   (value == System.Windows.Media.Imaging.Rotation.Rotate180) ||
                   (value == System.Windows.Media.Imaging.Rotation.Rotate270);
        }

        public static bool IsStretchValid(object valueObject)
        {
            Stretch value = (Stretch)valueObject;
            return (value == Stretch.None) ||
                   (value == Stretch.Fill) ||
                   (value == Stretch.Uniform) ||
                   (value == Stretch.UniformToFill);
        }

        public static bool IsSweepDirectionValid(object valueObject)
        {
            SweepDirection value = (SweepDirection)valueObject;
            return (value == SweepDirection.Counterclockwise) ||
                   (value == SweepDirection.Clockwise);
        }

        public static bool IsTextHintingModeValid(object valueObject)
        {
            TextHintingMode value = (TextHintingMode)valueObject;
            return (value == TextHintingMode.Auto) ||
                   (value == TextHintingMode.Fixed) ||
                   (value == TextHintingMode.Animated);
        }

        public static bool IsTextRenderingModeValid(object valueObject)
        {
            TextRenderingMode value = (TextRenderingMode)valueObject;
            return (value == TextRenderingMode.Auto) ||
                   (value == TextRenderingMode.Aliased) ||
                   (value == TextRenderingMode.Grayscale) ||
                   (value == TextRenderingMode.ClearType);
        }

        public static bool IsTileModeValid(object valueObject)
        {
            TileMode value = (TileMode)valueObject;
            return (value == TileMode.None) ||
                   (value == TileMode.Tile) ||
                   (value == TileMode.FlipX) ||
                   (value == TileMode.FlipY) ||
                   (value == TileMode.FlipXY);
        }

        public static bool IsToleranceTypeValid(object valueObject)
        {
            ToleranceType value = (ToleranceType)valueObject;
            return (value == ToleranceType.Absolute) ||
                   (value == ToleranceType.Relative);
        }
    }
}
