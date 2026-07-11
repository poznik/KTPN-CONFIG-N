using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Engine;

public sealed record ElectricalSelectionChange(string Field, string PreviousValue, string NewValue);

/// <summary>Synchronizes input-device and CT ratings from the selected transformer and device catalog.</summary>
public static class ElectricalSelectionService
{
    public static IReadOnlyList<ElectricalSelectionChange> Apply(ProjectConfig config, CatalogStore store)
    {
        var changes = new List<ElectricalSelectionChange>();
        if (!config.AutoElectricalSelection)
            return changes;

        var transformer = store.GetTransformer(config.Mark);
        if (transformer is null)
            return changes;

        if (config.PvrOn)
            ApplyInputNominal("Вводной ПВР", "ПВР/NH", config.PvrManufacturer, config.PvrNominal,
                transformer.RatedCurrentA, value => config.PvrNominal = value, store, changes);
        if (config.ReOn)
            ApplyInputNominal("Вводной рубильник", "РЕ", config.ReManufacturer, config.ReNominal,
                transformer.RatedCurrentA, value => config.ReNominal = value, store, changes);
        if (config.AvInOn)
            ApplyInputNominal("Вводной АВ", "АВ", config.AvInManufacturer, config.AvInNominal,
                transformer.RatedCurrentA, value => config.AvInNominal = value, store, changes);

        var limitingInput = LimitingInputNominal(config);
        var ctReference = CurrentTransformerReferenceNominal(config);
        var ctRatio = RecommendedCtRatio(store.Options.TtRatios, ctReference);
        if (config.HasCt)
            SetText("ТТ учета", config.CtRatio, ctRatio, value => config.CtRatio = value, changes);
        if (config.HasCtKip)
            SetText("ТТ КИП", config.CtKipRatio, ctRatio, value => config.CtKipRatio = value, changes);

        foreach (var feeder in config.OutgoingFeeders ?? new List<OutgoingFeederConfig>())
        {
            if (feeder.DeviceType.Equals("АВ", StringComparison.OrdinalIgnoreCase)
                && limitingInput > 0
                && feeder.Nominal > limitingInput)
            {
                var allowed = FeederNominals(feeder, store)
                    .Where(value => value <= limitingInput)
                    .DefaultIfEmpty(0)
                    .Max();
                SetNominal($"{feeder.DeviceType}-{feeder.Number}", feeder.Nominal, allowed,
                    value => feeder.Nominal = value, changes);
            }

            if (HasMetering(feeder))
            {
                var feederRatio = RecommendedCtRatio(store.Options.TtRatios, feeder.Nominal);
                SetText($"ТТ {feeder.DeviceType}-{feeder.Number}", feeder.TtRatio, feederRatio,
                    value => feeder.TtRatio = value, changes);
            }
        }

        return changes;
    }

    public static IReadOnlyList<int> InputNominals(
        string deviceType,
        string manufacturer,
        CatalogStore store)
    {
        var matching = store.DeviceModels
            .Where(model => model.Type.Equals(deviceType, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(manufacturer)
                    || model.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase))
                && IsRunnModel(model))
            .ToList();

        var intendedForInput = matching
            .Where(model => model.ApplicationRange.Contains("ввод", StringComparison.OrdinalIgnoreCase)
                || model.Purpose.Contains("ввод", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var nominals = intendedForInput.SelectMany(model => model.Nominals)
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToList();

        return nominals;
    }

    public static IReadOnlyList<string> InputManufacturers(string deviceType, CatalogStore store) =>
        store.DeviceModels
            .Where(model => model.Type.Equals(deviceType, StringComparison.OrdinalIgnoreCase)
                && IsRunnModel(model)
                && (model.ApplicationRange.Contains("ввод", StringComparison.OrdinalIgnoreCase)
                    || model.Purpose.Contains("ввод", StringComparison.OrdinalIgnoreCase)))
            .Select(model => model.Manufacturer)
            .Where(manufacturer => !string.IsNullOrWhiteSpace(manufacturer))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(manufacturer => manufacturer)
            .ToList();

    public static int RecommendedInputNominal(
        string deviceType,
        string manufacturer,
        double transformerCurrent,
        CatalogStore store) =>
        InputNominals(deviceType, manufacturer, store)
            .Where(value => value >= transformerCurrent)
            .DefaultIfEmpty(0)
            .Min();

    public static int LimitingInputNominal(ProjectConfig config)
    {
        var active = ProjectConfigNormalizer.GetLvInputDevices(config)
            .Select(device => device.Nominal)
            .Where(nominal => nominal > 0)
            .ToList();
        return active.Count == 0 ? 0 : active.Min();
    }

    public static int CurrentTransformerReferenceNominal(ProjectConfig config) =>
        config.AvInOn && config.AvInNominal > 0
            ? config.AvInNominal
            : LimitingInputNominal(config);

    public static string RecommendedCtRatio(IEnumerable<string> ratios, int nominal)
    {
        if (nominal <= 0)
            return "";

        return ratios.Select(ratio => new { Ratio = ratio, Primary = ParsePrimary(ratio) })
            .Where(item => item.Primary > 0)
            .OrderBy(item => Math.Abs(item.Primary - nominal))
            .ThenBy(item => item.Primary < nominal ? 1 : 0)
            .Select(item => item.Ratio)
            .FirstOrDefault() ?? "";
    }

    private static void ApplyInputNominal(
        string field,
        string deviceType,
        string manufacturer,
        int previous,
        double transformerCurrent,
        Action<int> setter,
        CatalogStore store,
        ICollection<ElectricalSelectionChange> changes)
    {
        var recommended = RecommendedInputNominal(deviceType, manufacturer, transformerCurrent, store);
        SetNominal(field, previous, recommended, setter, changes);
    }

    private static IReadOnlyList<int> FeederNominals(OutgoingFeederConfig feeder, CatalogStore store)
    {
        var values = store.DeviceModels
            .Where(model => model.Type.Equals(feeder.DeviceType, StringComparison.OrdinalIgnoreCase)
                && IsRunnModel(model)
                && IsOutgoingModel(model)
                && (string.IsNullOrWhiteSpace(feeder.Manufacturer)
                    || model.Manufacturer.Equals(feeder.Manufacturer, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(feeder.Model)
                    || model.Model.Equals(feeder.Model, StringComparison.OrdinalIgnoreCase)
                    || model.Series.Equals(feeder.Model, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(model => model.Nominals)
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToList();
        return values;
    }

    private static bool IsRunnModel(DeviceModel model) =>
        model.InstallationArea.Contains("РУНН", StringComparison.OrdinalIgnoreCase)
        || model.ApplicationRange.Contains("РУНН", StringComparison.OrdinalIgnoreCase);

    private static bool IsOutgoingModel(DeviceModel model) =>
        model.Purpose.Contains("отход", StringComparison.OrdinalIgnoreCase)
        || model.ApplicationRange.Contains("отход", StringComparison.OrdinalIgnoreCase);

    private static int ParsePrimary(string ratio) =>
        int.TryParse(ratio.Split('/', StringSplitOptions.TrimEntries).FirstOrDefault(), out var value) ? value : 0;

    private static bool HasMetering(OutgoingFeederConfig feeder) =>
        feeder.HasMeter || (!string.IsNullOrWhiteSpace(feeder.MeteringType)
            && !feeder.MeteringType.Equals("Нет", StringComparison.OrdinalIgnoreCase));

    private static void SetNominal(string field, int previous, int next, Action<int> setter, ICollection<ElectricalSelectionChange> changes)
    {
        if (next <= 0 || previous == next)
            return;
        setter(next);
        changes.Add(new(field, $"{previous} А", $"{next} А"));
    }

    private static void SetText(string field, string previous, string next, Action<string> setter, ICollection<ElectricalSelectionChange> changes)
    {
        if (string.IsNullOrWhiteSpace(next) || string.Equals(previous, next, StringComparison.OrdinalIgnoreCase))
            return;
        setter(next);
        changes.Add(new(field, previous, next));
    }
}
