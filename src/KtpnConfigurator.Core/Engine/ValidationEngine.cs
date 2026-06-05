using System.Text.RegularExpressions;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Engine;

/// <summary>Проверки проектных решений (ошибки блокируют выпуск Приказа).</summary>
public static partial class ValidationEngine
{
    public static void Apply(ProjectConfig c, CalculationResult res, CatalogStore store, bool transformerFound)
    {
        var msgs = res.Messages;

        if (!transformerFound)
        {
            msgs.Add(new(Severity.Error, "Не выбран трансформатор — расчёт габаритов невозможен."));
            return;
        }

        // --- Критическая проверка номинала ввода (A45) ---
        if (res.ValidationOk)
        {
            msgs.Add(new(Severity.Info,
                $"Номинал вводного устройства ({res.InputNominal:0} А) достаточен для тока ТМГ ({res.RatedCurrentA:0} А)."));
        }
        else
        {
            string who = res.InputNominal <= 0
                ? "вводное устройство РУНН не задано"
                : $"номинал ввода {res.InputNominal:0} А";
            msgs.Add(new(Severity.Error,
                $"ОШИБКА ПРОЕКТИРОВАНИЯ: {who} меньше номинального тока трансформатора ({res.RatedCurrentA:0} А)."));
        }

        // --- Проверка номиналов аппаратов по диапазонам каталога ---
        CheckBrandRange(c, store, msgs, "Предохранитель-разъединитель (ПВР / NH)", c.PvrOn, c.PvrManufacturer, c.PvrNominal, "ПВР");
        CheckBrandRange(c, store, msgs, "Рубильник открытый (РЕ)", c.ReOn, c.ReManufacturer, c.ReNominal, "Рубильник РЕ");
        CheckBrandRange(c, store, msgs, "Автоматический выключатель (АВ)", c.AvInOn, c.AvInManufacturer, c.AvInNominal, "Вводной АВ");

        // --- Предупреждения по ручным габаритам ---
        if (res.LengthFinal == 0 || res.WidthFinal == 0)
            msgs.Add(new(Severity.Warning, "Габарит задан вручную как 0 — проверьте корректность."));

        // --- Полнота заполнения ---
        if (string.IsNullOrWhiteSpace(c.Channel))
            msgs.Add(new(Severity.Warning, "Не выбран швеллер основания — масса основания будет некорректной."));
        if (string.IsNullOrWhiteSpace(c.SteelType) || store.SteelWeight(c.Thickness) <= 0)
            msgs.Add(new(Severity.Warning, "Не задан тип/толщина стали корпуса — масса корпуса будет некорректной."));
        if (string.IsNullOrWhiteSpace(c.Voltage))
            msgs.Add(new(Severity.Warning, "Не выбрано напряжение РУВН."));
    }

    private static void CheckBrandRange(ProjectConfig c, CatalogStore store, List<ValidationMessage> msgs,
        string apparatusType, bool on, string manufacturer, int nominal, string label)
    {
        if (!on || string.IsNullOrWhiteSpace(manufacturer) || nominal <= 0)
            return;
        var spec = store.Apparatus.FirstOrDefault(a => a.Type == apparatusType && a.Manufacturer == manufacturer);
        if (spec is null)
            return;
        var (min, max) = ParseRange(spec.CurrentRange);
        if (max > 0 && (nominal < min || nominal > max))
            msgs.Add(new(Severity.Warning,
                $"{label} {nominal} А ({manufacturer}) вне диапазона каталога {spec.CurrentRange}."));
    }

    /// <summary>Парсит диапазон вида "16 - 4000 А".</summary>
    private static (double min, double max) ParseRange(string range)
    {
        var nums = NumberRegex().Matches(range);
        if (nums.Count >= 2
            && double.TryParse(nums[0].Value, out var a)
            && double.TryParse(nums[1].Value, out var b))
            return (a, b);
        return (0, 0);
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();
}
