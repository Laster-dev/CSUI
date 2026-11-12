using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace CSUI.SampleApp
{
    [Bindable]
    internal sealed class SubsystemTelemetryItem
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string StatusText { get; set; }
        public bool IsGpuAccelerated { get; set; }
    }

    [ObservableObject]
    internal sealed partial class ShowcaseViewModel
    {
        [ObservableProperty]
        private int _surfaceClickCount;

        [ObservableProperty]
        private int _meshHitCount;

        [ObservableProperty]
        private string _lastHitSummary = "点击右侧 3D 晶体或 3D 曲面上的 2D 按钮进行交互";

        [ObservableProperty]
        private string _cameraModeName = "PerspectiveCamera (FOV 45°)";

        [ObservableProperty]
        private bool _isDarkTheme = true;

        [ObservableProperty]
        private bool _isAutoRotating = true;

        [ObservableProperty]
        private bool _onlyGpuSubsystems;

        private double _rotationSpeed = 48.0;
        private double _lightIntensity = 85.0;

        public ObservableCollection<SubsystemTelemetryItem> Subsystems { get; } = new ObservableCollection<SubsystemTelemetryItem>
        {
            new SubsystemTelemetryItem { Name = "Direct2D 1.1 Vector & Brush Engine", Category = "GPU 2D", StatusText = "Active (Hardware)", IsGpuAccelerated = true },
            new SubsystemTelemetryItem { Name = "DirectWrite Text Shaping & GlyphRun", Category = "GPU Text", StatusText = "Active (Hardware)", IsGpuAccelerated = true },
            new SubsystemTelemetryItem { Name = "WIC Imaging & WriteableBitmap", Category = "Imaging", StatusText = "Active (Zero-Copy)", IsGpuAccelerated = true },
            new SubsystemTelemetryItem { Name = "Direct3D 11 Viewport3D & Blinn-Phong", Category = "GPU 3D", StatusText = "Active (Z-Buffer)", IsGpuAccelerated = true },
            new SubsystemTelemetryItem { Name = "Viewport2DVisual3D UV Hit-Testing", Category = "Hybrid 2D/3D", StatusText = "Active (Interactive)", IsGpuAccelerated = true },
            new SubsystemTelemetryItem { Name = "DependencyProperty & Freezable Engine", Category = "Core", StatusText = "Active (AOT)", IsGpuAccelerated = false },
            new SubsystemTelemetryItem { Name = "Reflection-Free Binding.Create Engine", Category = "Data", StatusText = "Active (TwoWay)", IsGpuAccelerated = false },
        };

        public double RotationSpeed
        {
            get => _rotationSpeed;
            set => SetProperty(ref _rotationSpeed, Math.Clamp(value, 0.0, 180.0));
        }

        public double LightIntensity
        {
            get => _lightIntensity;
            set => SetProperty(ref _lightIntensity, Math.Clamp(value, 10.0, 100.0));
        }
    }

    internal sealed class SpeedFormatConverter : IValueConverter
    {
        public static readonly SpeedFormatConverter Instance = new SpeedFormatConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double speed = value is double d ? d : 0.0;
            return $"3D 角速度: {speed:F0} °/s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 48.0;
        }
    }

    internal sealed class LightIntensityFormatConverter : IValueConverter
    {
        public static readonly LightIntensityFormatConverter Instance = new LightIntensityFormatConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double pct = value is double d ? d : 0.0;
            return $"光源强度: {pct:F0}%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 85.0;
        }
    }

    internal sealed class SurfaceClickFormatConverter : IValueConverter
    {
        public static readonly SurfaceClickFormatConverter Instance = new SurfaceClickFormatConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int count = value is int c ? c : 0;
            return $"2D-on-3D 曲面按钮点击: {count} 次";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0;
        }
    }
}
