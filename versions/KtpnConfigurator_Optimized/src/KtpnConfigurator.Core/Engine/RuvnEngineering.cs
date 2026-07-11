using System.Globalization;
using System.Text.RegularExpressions;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Engine;

public sealed record RuvnBranchSelection(
    string Key,
    string Title,
    string SwitchType,
    int SwitchNominal,
    bool FuseOn,
    string FuseType,
    string FuseNominal,
    RuvnBranchEquipmentConfig Equipment);

public static partial class RuvnEngineering
{
    public const string NoRuvn = "Нет";
    public const string TerminalRuvn = "Тупиковая";
    public const string PassThroughRuvn = "Проходная";
    public const string VacuumBreaker = "Вакуумный выключатель";

    public const string NoSurgeArrester = "Не устанавливать";
    public const string SurgeArresterAtAirPortal = "На воздушном портале";
    public const string SurgeArresterAtBusBridge = "На шинном мосту";
    public const string SurgeArresterAtTransformer = "Для защиты ТМГ";

    public static readonly IReadOnlyList<string> SurgeArresterLocations = new[]
    {
        NoSurgeArrester,
        SurgeArresterAtAirPortal,
        SurgeArresterAtBusBridge,
        SurgeArresterAtTransformer,
    };

    public static readonly IReadOnlyList<string> SurgeArresterThroughputs = new[]
    {
        "Класс пропускной способности 1",
        "Класс пропускной способности 2",
        "Класс пропускной способности 3",
        "Класс пропускной способности 4",
        "Класс пропускной способности 5",
        "По ТЗ",
    };

    public static bool HasRuvn(ProjectConfig config) =>
        !string.Equals(config.RuvnType, NoRuvn, StringComparison.OrdinalIgnoreCase);

    public static bool IsPassThrough(ProjectConfig config) =>
        string.Equals(config.RuvnType, PassThroughRuvn, StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<RuvnBranchSelection> Branches(ProjectConfig config)
    {
        if (!HasRuvn(config))
            return Array.Empty<RuvnBranchSelection>();

        var transformer = TransformerBranch(config);
        if (!IsPassThrough(config))
            return new[] { transformer };

        return new[]
        {
            new RuvnBranchSelection(
                "incoming",
                "Входящая линия",
                config.RuvnIncomingSwitch,
                config.RuvnIncomingSwitchNominal,
                config.RuvnIncomingFuseOn,
                config.RuvnIncomingFuseType,
                config.RuvnIncomingFuseNominal,
                EquipmentFor(config, "incoming")),
            new RuvnBranchSelection(
                "outgoing",
                "Отходящая линия",
                config.RuvnOutgoingSwitch,
                config.RuvnOutgoingSwitchNominal,
                config.RuvnOutgoingFuseOn,
                config.RuvnOutgoingFuseType,
                config.RuvnOutgoingFuseNominal,
                EquipmentFor(config, "outgoing")),
            transformer,
        };
    }

    public static string RecommendedFuseNominal(ProjectConfig config, CatalogStore store)
    {
        var transformer = store.GetTransformer(config.Mark);
        return transformer is null
            ? ""
            : RecommendedFuseNominal(transformer.PowerKva, config.Voltage);
    }

    public static string RecommendedFuseNominal(double powerKva, string voltage)
    {
        var voltageClass = VoltageClassKv(voltage);
        if (voltageClass is not 6 and not 10)
            return "";

        var kva = (int)Math.Round(powerKva, MidpointRounding.AwayFromZero);
        return FuseTable.TryGetValue((kva, voltageClass), out var value) ? value : "";
    }

    public static string RecommendedSurgeArresterOperatingVoltage(string voltage) =>
        VoltageClassKv(voltage) switch
        {
            6 => "7,2 кВ",
            10 => "12 кВ",
            _ => "",
        };

    public static bool HasDevice(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Equals(NoRuvn, StringComparison.OrdinalIgnoreCase);

    public static bool IsVacuumBreaker(string? value) =>
        value?.Equals(VacuumBreaker, StringComparison.OrdinalIgnoreCase) == true;

    public static bool HasVisibleBreak(string? value) =>
        HasDevice(value) && !value!.Equals("По схеме ячейки", StringComparison.OrdinalIgnoreCase);

    public static RuvnBranchEquipmentConfig EquipmentFor(ProjectConfig config, string branchKey) =>
        branchKey switch
        {
            "incoming" => config.RuvnIncomingEquipment,
            "outgoing" => config.RuvnOutgoingEquipment,
            _ => config.RuvnTransformerEquipment,
        } ?? new RuvnBranchEquipmentConfig();

    public static string RzaFunctions(RuvnBranchEquipmentConfig equipment)
    {
        var functions = new List<string>();
        if (equipment.RzaMtz) functions.Add("МТЗ");
        if (equipment.RzaCurrentCutoff) functions.Add("ТО");
        if (equipment.RzaGroundFault) functions.Add("ОЗЗ");
        if (equipment.RzaOverload) functions.Add("перегрузка");
        if (equipment.RzaUrov) functions.Add("УРОВ");
        if (equipment.RzaLzsh) functions.Add("ЛЗШ");
        if (equipment.RzaApv) functions.Add("АПВ");
        if (equipment.RzaAvr) functions.Add("АВР");
        if (equipment.RzaArcProtection) functions.Add("дуговая защита");
        if (equipment.RzaTransformerGas) functions.Add("газовая защита ТМГ");
        return string.Join(", ", functions);
    }

    private static RuvnBranchSelection TransformerBranch(ProjectConfig config)
    {
        var switchType = HasDevice(config.RuvnTransformerSwitch) ? config.RuvnTransformerSwitch : config.RuvnSwitch;
        var switchNominal = config.RuvnTransformerSwitchNominal > 0
            ? config.RuvnTransformerSwitchNominal
            : config.RuvnSwitchNominal;
        var fuseType = HasDevice(config.RuvnTransformerFuseType) ? config.RuvnTransformerFuseType : config.FuseType;
        var fuseNominal = HasDevice(config.RuvnTransformerFuseNominal) ? config.RuvnTransformerFuseNominal : config.FuseNominal;
        var fuseOn = config.RuvnTransformerFuseOn;

        return new RuvnBranchSelection(
            "transformer",
            "Ответвление на ТМГ",
            switchType,
            switchNominal,
            fuseOn,
            fuseType,
            fuseNominal,
            EquipmentFor(config, "transformer"));
    }

    private static int VoltageClassKv(string voltage)
    {
        var match = NumberRegex().Match(voltage ?? "");
        if (!match.Success)
            return 0;

        var text = match.Value.Replace(',', '.');
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? (int)Math.Round(value, MidpointRounding.AwayFromZero)
            : 0;
    }

    private static readonly Dictionary<(int PowerKva, int VoltageKv), string> FuseTable = new()
    {
        [(25, 6)] = "5А",
        [(25, 10)] = "3.2А",
        [(40, 6)] = "8А",
        [(40, 10)] = "5А",
        [(63, 6)] = "16А",
        [(63, 10)] = "8А",
        [(100, 6)] = "20А",
        [(100, 10)] = "10А",
        [(160, 6)] = "31.5А",
        [(160, 10)] = "20А",
        [(250, 6)] = "40А",
        [(250, 10)] = "31.5А",
        [(400, 6)] = "80А",
        [(400, 10)] = "50А",
        [(630, 6)] = "100А",
        [(630, 10)] = "80А",
        [(1000, 6)] = "160А",
        [(1000, 10)] = "100А",
        [(1250, 6)] = "200А",
        [(1250, 10)] = "160А",
        [(1600, 6)] = "315А",
        [(1600, 10)] = "160А",
        [(2000, 6)] = "315А",
        [(2000, 10)] = "200А",
        [(2500, 6)] = "315А",
        [(2500, 10)] = "315А",
    };

    [GeneratedRegex(@"[\d]+(?:[,.][\d]+)?")]
    private static partial Regex NumberRegex();
}
