using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.App;

/// <summary>Hex-строка (#RRGGBB) → кисть.</summary>
public sealed class StringToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s))
            return Brushes.Black;
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(s)); }
        catch { return Brushes.Black; }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Severity → цвет сообщения валидации.</summary>
public sealed class SeverityToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Severity s
            ? s switch
            {
                Severity.Error => new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
                Severity.Warning => new SolidColorBrush(Color.FromRgb(0xEF, 0x6C, 0x00)),
                _ => new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),
            }
            : Brushes.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Severity → символ-иконка.</summary>
public sealed class SeverityToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Severity s
            ? s switch { Severity.Error => "🛑", Severity.Warning => "⚠", _ => "✅" }
            : "•";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
