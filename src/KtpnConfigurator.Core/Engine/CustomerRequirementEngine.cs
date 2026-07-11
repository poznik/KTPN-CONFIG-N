using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Engine;

public static class CustomerRequirementEngine
{
    public static IReadOnlyList<ValidationMessage> Build(ProjectConfig config, CatalogStore store)
    {
        var profile = store.GetCustomerProfile(config.GridCompany);
        if (profile is null)
            return Array.Empty<ValidationMessage>();

        var settings = profile.Settings ?? new CustomerProfileSettings();
        var result = new List<ValidationMessage>();

        AddMatch(result, "цвет корпуса", settings.BodyColor, config.BodyColor);
        AddMatch(result, "цвет дверей", settings.DoorColor, config.DoorColor);
        AddMatch(result, "цвет крыши", settings.RoofColor, config.RoofColor);
        AddMatch(result, "цвет основания/цоколя", settings.BaseColor, config.BaseColor);
        AddMatch(result, "климат", settings.ClimateExecution, config.ClimateExecution);
        AddMatch(result, "степень защиты", settings.ProtectionDegree, config.ProtectionDegree);
        AddMatch(result, "исполнение РУВН", settings.RuvnExecution, config.RuvnExecution);
        AddMatch(result, "место установки ОПН РУВН", settings.RuvnSurgeArresterLocation, config.RuvnSurgeArresterLocation);
        AddMatch(result, "двери трансформаторного отсека", settings.TransformerDoorConfiguration, config.TransformerDoorConfiguration);
        AddMatch(result, "сетевой замок", settings.NetworkLockType, config.NetworkLockType);
        AddMatch(result, "тип вентиляции", settings.VentilationType, config.VentilationType);
        AddMatch(result, "размещение логотипа", settings.LogoPlacement, config.LogoPlacement);

        AddBoolMatch(result, "ригельный замок", settings.HasRigelLock, config.HasRigelLock);
        AddBoolMatch(result, "проушины под навесной замок", settings.HasPadlockProvision, config.HasPadlockProvision);
        AddBoolMatch(result, "козырьки над дверями", settings.HasDoorCanopies, config.HasDoorCanopies);
        AddBoolMatch(result, "уплотнения дверей", settings.HasDoorSeals, config.HasDoorSeals);
        AddBoolMatch(result, "сетчатые двери трансформаторного отсека", settings.HasTransformerMeshDoors, config.HasTransformerMeshDoors);
        AddBoolMatch(result, "сетка/защита жалюзи от животных и птиц", settings.HasLouverAnimalProtection, config.HasLouverAnimalProtection);
        AddBoolMatch(result, "пломбировка дверей", settings.HasDoorSealing, config.HasDoorSealing);
        AddBoolMatch(result, "дефлектор на крыше", settings.HasRoofDeflector, config.HasRoofDeflector);
        AddBoolMatch(result, "логотип", settings.HasLogo, config.HasLogo);
        AddBoolMatch(result, "предупреждающие надписи", settings.HasWarningLabels, config.HasWarningLabels);
        AddBoolMatch(result, "пофидерная маркировка", settings.HasFeederLabels, config.HasFeederLabels);
        AddMatch(result, "освещаемые отсеки", settings.LightingAreas, config.AuxiliaryNeeds?.LightingAreas ?? "");
        AddBoolMatch(result, "освещение собственных нужд", settings.LightingEnabled, config.AuxiliaryNeeds?.LightingEnabled);
        AddIntMatch(result, "ремонтное освещение", settings.RepairLightingVoltage, config.AuxiliaryNeeds?.RepairLightingVoltage, "В");
        AddBoolMatch(result, "наружное освещение", settings.OutdoorLightingEnabled, config.AuxiliaryNeeds?.OutdoorLightingEnabled);
        AddBoolMatch(result, "розетки собственных нужд", settings.SocketEnabled, config.AuxiliaryNeeds?.SocketEnabled);
        AddBoolMatch(result, "обогрев собственных нужд", settings.HeatingEnabled, config.AuxiliaryNeeds?.HeatingEnabled);
        AddBoolMatch(result, "обогрев счетчиков", settings.MeterHeatingEnabled, config.AuxiliaryNeeds?.MeterHeatingEnabled);
        AddBoolMatch(result, "ОПС", settings.OpsEnabled, config.AuxiliaryNeeds?.OpsEnabled);

        return result;
    }

    public static string Summary(ProjectConfig config, CatalogStore store)
    {
        var messages = Build(config, store);
        return messages.Count == 0
            ? "Несовпадений нет."
            : string.Join("; ", messages.Select(m => m.Text));
    }

    private static void AddMatch(List<ValidationMessage> result, string label, string expected, string actual)
    {
        if (string.IsNullOrWhiteSpace(expected))
            return;

        if (!expected.Equals(actual, StringComparison.OrdinalIgnoreCase))
            result.Add(new(Severity.Warning, $"Проверь: {label} должно быть \"{expected}\", сейчас \"{ValueOrDash(actual)}\"."));
    }

    private static void AddBoolMatch(List<ValidationMessage> result, string label, bool? expected, bool? actual)
    {
        if (!expected.HasValue)
            return;

        if (actual != expected)
            result.Add(new(Severity.Warning, $"Проверь: {label} - {BoolText(expected.Value)}, сейчас {BoolText(actual == true)}."));
    }

    private static void AddIntMatch(List<ValidationMessage> result, string label, int? expected, int? actual, string unit)
    {
        if (!expected.HasValue)
            return;

        var actualValue = actual ?? 0;
        if (actualValue != expected.Value)
            result.Add(new(Severity.Warning, $"Проверь: {label} должно быть {expected.Value} {unit}, сейчас {actualValue} {unit}."));
    }

    private static string BoolText(bool value) =>
        value ? "предусмотреть" : "не предусматривать";

    private static string ValueOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
}
