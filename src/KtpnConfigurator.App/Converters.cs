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

public sealed class CurrentTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return "";

        if (value is int i)
            return $"{i:0} А";

        if (value is double d)
            return $"{d:0} А";

        var text = value.ToString()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(text))
            return "";

        var last = text[^1];
        if (last is 'А' or 'а' or 'A' or 'a')
        {
            var number = text[..^1].TrimEnd();
            return string.IsNullOrWhiteSpace(number) ? text : $"{number} А";
        }

        return text;
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
                Severity.Error => new SolidColorBrush(Color.FromRgb(0x8A, 0x5F, 0x5F)),
                Severity.Warning => new SolidColorBrush(Color.FromRgb(0x8A, 0x74, 0x3F)),
                _ => new SolidColorBrush(Color.FromRgb(0x60, 0x7D, 0x68)),
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
