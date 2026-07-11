using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Engine;

public static class ValidationMessageClassifier
{
    public static void Apply(IEnumerable<ValidationMessage> messages)
    {
        foreach (var message in messages)
        {
            (message.Section, message.TabIndex) = Classify(message.Text);
        }
    }

    public static (string Section, int TabIndex) Classify(string text)
    {
        if (ContainsAny(text, "2КТПН", "НКУ", "ЩО", "ВРУ", "КСО", "КРУ", "панел", "ячейк", "Icw", "Ipk", "IAC", "LSC", "секционн"))
            return ("Конфигурация изделия", 5);
        if (ContainsAny(text, "РУВН", "ПКТ", "вакуумн", "РЗА", "ОПН на воздуш", "ОПН на шинн"))
            return ("РУВН", 2);
        if (ContainsAny(text, "РУНН", "отходящ", "вводн", "счетчик", "ТТ учета", "ТТ отходящей", "кабель"))
            return ("РУНН", 3);
        if (ContainsAny(text, "ЩСН", "освещ", "розет", "обогрев", "вентиляц", "ОПС", "РИСЭ"))
            return ("Собственные нужды", 4);
        if (ContainsAny(text, "трансформатор", "ТМГ", "корпус", "двер", "RAL", "габарит", "масса", "заземлен"))
            return ("Трансформатор и корпус", 1);
        return ("Результат", 6);
    }

    private static bool ContainsAny(string text, params string[] values) =>
        values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
}
