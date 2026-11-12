using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace CSUI.Tests;

// ZERO CSUI attributes: no [XamlType], no [Bindable], no [ObservableObject]
public sealed class ZeroModBadge : UserControl
{
    public string BadgeCaption { get; set; } = string.Empty;
}

public sealed class ZeroModItem
{
    public string Label { get; set; } = string.Empty;
}

public sealed class ZeroModBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "YES" : "NO";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ZeroModViewModel : INotifyPropertyChanged
{
    private string _title = "PureWpfTitle";
    private bool _active = true;
    private string _slot = "SlotA";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }
    }

    public bool Active
    {
        get => _active;
        set
        {
            if (_active != value)
            {
                _active = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Active)));
            }
        }
    }

    public string this[string key] => key == "slot" ? _slot : string.Empty;

    public ZeroModItem FeaturedItem { get; } = new ZeroModItem { Label = "ZeroAttrItem" };
}

public partial class ZeroModWpfView : UserControl
{
    public ZeroModWpfView()
    {
        InitializeComponent();
    }
}
