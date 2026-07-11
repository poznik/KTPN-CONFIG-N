namespace KtpnConfigurator.Core.Models;

public enum LvInputDeviceKind
{
    SwitchDisconnectFuse,
    Disconnector,
    CircuitBreaker,
}

public sealed record LvInputDeviceSelection(
    LvInputDeviceKind Kind,
    string SymbolKey,
    int DesignationNumber,
    string FallbackCode,
    string DeviceType,
    string Caption,
    string Manufacturer,
    int Nominal);

public static class ProjectConfigNormalizer
{
    public static IReadOnlyList<LvInputDeviceSelection> GetLvInputDevices(ProjectConfig config)
    {
        var result = new List<LvInputDeviceSelection>();

        if (config.PvrOn)
        {
            result.Add(new(
                LvInputDeviceKind.SwitchDisconnectFuse,
                "switchDisconnectFuse",
                2,
                "QS",
                "ПВР/NH",
                "ПВР/NH на вводе РУНН",
                config.PvrManufacturer,
                config.PvrNominal));
        }

        if (config.ReOn)
        {
            result.Add(new(
                LvInputDeviceKind.Disconnector,
                "disconnector",
                3,
                "QS",
                "РЕ",
                "РЕ на вводе РУНН",
                config.ReManufacturer,
                config.ReNominal));
        }

        if (config.AvInOn)
        {
            result.Add(new(
                LvInputDeviceKind.CircuitBreaker,
                "circuitBreaker",
                1,
                "QF",
                "АВ",
                "АВ на вводе РУНН",
                config.AvInManufacturer,
                config.AvInNominal));
        }

        return result;
    }
}
