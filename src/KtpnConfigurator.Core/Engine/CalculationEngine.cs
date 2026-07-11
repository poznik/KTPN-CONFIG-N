using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Engine;

/// <summary>
/// Расчётное ядро. Перенос формул листа «Расчет КТПН» 1:1.
/// Все числовые правила задокументированы в ПОСТАНОВКА_КТПН.md §7.
/// </summary>
public static class CalculationEngine
{
    /// <summary>Округление как Excel ROUND — половина «от нуля».</summary>
    private static double RoundExcel(double value) =>
        Math.Round(value, MidpointRounding.AwayFromZero);

    public static CalculationResult Calculate(ProjectConfig c, CatalogStore store)
    {
        var m = store.Options.Methodology;
        var t = store.GetTransformer(c.Mark);
        var res = new CalculationResult();

        if (t is null)
        {
            // Нет трансформатора — расчёт габаритов невозможен; вернём нули,
            // валидация добавит сообщение об ошибке.
            res.RatedCurrentA = 0;
            ValidationEngine.Apply(c, res, store, transformerFound: false);
            return res;
        }

        res.RatedCurrentA = t.RatedCurrentA;
        // --- Длина (B53) ---
        double ruvnLen = c.RuvnType == "Нет" ? 0 : c.LenRuvn;
        double baseLen = ruvnLen + t.LengthMm + c.LenRunn + c.LengthBuffer;
        res.LengthCalc = SnapLength(baseLen, m.StandardLengths);
        res.LengthFinal = WithMinOverride(res.LengthCalc, c.ManualLength, m.MinDimensionMm);

        // --- Ширина (B54) ---
        res.WidthCalc = CalcWidth(c, t.WidthMm, m);
        res.WidthFinal = WithMinOverride(res.WidthCalc, c.ManualWidth, m.MinDimensionMm);

        // --- Высота (B55) — ручной ввод без контроля минимума ---
        res.HeightCalc = t.HeightMm >= m.HeightThresholdMm ? m.TallHeightMm : m.ShortHeightMm;
        res.HeightFinal = c.ManualHeight ?? res.HeightCalc;

        // --- Массы (ручной ввод C56/C57/C58 имеет приоритет, как в исходной книге) ---
        res.BaseMassCalc = BaseMass(res.LengthFinal, res.WidthFinal, store.ChannelWeight(c.Channel), m);
        res.BaseMass = c.ManualBaseMass ?? res.BaseMassCalc;
        res.BodyMassCalc = BodyMass(res.LengthFinal, res.WidthFinal, res.HeightFinal, store.SteelWeight(c.Thickness), m);
        res.BodyMass = c.ManualBodyMass ?? res.BodyMassCalc;
        res.GrossMassCalc = t.MassKg + res.BaseMass + res.BodyMass;
        res.GrossMass = c.ManualGrossMass ?? res.GrossMassCalc;
        res.EquipmentMassEstimate = EquipmentMass(c);
        res.BusbarMassEstimate = BusbarMass(t.RatedCurrentA, c);
        res.DoorMassEstimate = DoorMass(c);
        res.AuxiliaryMassEstimate = AuxiliaryMass(c.AuxiliaryNeeds);
        res.EnclosureOptionMassEstimate = EnclosureOptionMass(c);
        res.AdditionalMassEstimate = RoundExcel(res.EquipmentMassEstimate
            + res.BusbarMassEstimate
            + res.DoorMassEstimate
            + res.AuxiliaryMassEstimate
            + res.EnclosureOptionMassEstimate);
        res.GrossMassEstimated = res.GrossMass + res.AdditionalMassEstimate;

        // --- Проверка номинала ввода ---
        res.InputNominal = Math.Max(c.PvrOn ? c.PvrNominal : 0,
                            Math.Max(c.ReOn ? c.ReNominal : 0,
                                     c.AvInOn ? c.AvInNominal : 0));
        res.ValidationOk = res.InputNominal >= t.RatedCurrentA;
        res.BusbarHv = SelectTransformerBusbar(t.BusbarHv, c.BusbarHvMaterial);
        res.BusbarLv = SelectTransformerBusbar(t.BusbarLv, c.BusbarLvMaterial);
        res.BusbarN = SelectTransformerBusbar(t.BusbarLv, c.BusbarNMaterial);
        res.BusbarPe = NormalizeBusbarSection(t.BusbarPe);

        ValidationEngine.Apply(c, res, store, transformerFound: true);
        return res;
    }

    private static double SnapLength(double baseLen, IReadOnlyList<double> ladder)
    {
        foreach (var step in ladder)
            if (baseLen <= step)
                return step;
        return baseLen;
    }

    private static double CalcWidth(ProjectConfig c, double trWidth, MethodologyParams m)
    {
        if (c.RuvnType == "Проходная")
            return m.PassageWidthMm;
        bool multi = (c.AvOn ? c.AvQty : 0) > m.MultiBayAvQty
                  || (c.RpsOn ? c.RpsQty : 0) > m.MultiBayRpsQty;
        double baseMin = multi ? m.MultiBayWidthMm : m.StandardWidthMm;
        return Math.Max(baseMin, trWidth + c.TransformerTolerance);
    }

    /// <summary>D53/D54: ручной ввод имеет приоритет; 0 → 0; иначе не менее минимума.</summary>
    private static double WithMinOverride(double calc, double? manual, double minDim)
    {
        if (!manual.HasValue)
            return calc;
        if (manual.Value == 0)
            return 0;
        return Math.Max(manual.Value, minDim);
    }

    private static double BaseMass(double len, double wid, double channelWeightPerM, MethodologyParams m)
    {
        double frame = ((len * 2 + wid * 4) / 1000.0) * channelWeightPerM * m.FrameCoef;
        double floor = (len * wid / 1_000_000.0) * m.FloorSheetKgPerM2;
        return RoundExcel(frame + floor);
    }

    private static double BodyMass(double len, double wid, double height, double steelWeightPerM2, MethodologyParams m)
    {
        double walls = (len + wid) * 2 * height / 1_000_000.0;
        double roofArea = len * wid / 1_000_000.0;
        return RoundExcel((walls + roofArea) * steelWeightPerM2 * m.BodyWasteCoef);
    }

    private static double EquipmentMass(ProjectConfig c)
    {
        double mass = 0;
        if (RuvnEngineering.HasRuvn(c))
        {
            mass += RuvnEngineering.IsPassThrough(c) ? 420 : 260;
            mass += RuvnEngineering.Branches(c).Count(b => RuvnEngineering.IsVacuumBreaker(b.SwitchType)) * 160;
        }

        if (c.PvrOn) mass += 35;
        if (c.ReOn) mass += 28;
        if (c.AvInOn) mass += 45;
        if (c.RunnSurgeArrester) mass += 6;
        if (c.HasCt) mass += 9;
        if (c.HasCtKip) mass += 9;
        if (c.HasMeter) mass += 2;

        mass += (c.OutgoingFeeders ?? new List<OutgoingFeederConfig>()).Sum(f =>
            f.DeviceType.Equals("РПС", StringComparison.OrdinalIgnoreCase) ? 18 : 12);

        return RoundExcel(mass);
    }

    private static double BusbarMass(double ratedCurrentA, ProjectConfig c)
    {
        var baseMass = ratedCurrentA switch
        {
            <= 400 => 24,
            <= 630 => 38,
            <= 1000 => 62,
            <= 1600 => 105,
            <= 2500 => 160,
            _ => 220,
        };

        var copperFactor = new[]
        {
            c.BusbarHvMaterial,
            c.BusbarLvMaterial,
            c.BusbarNMaterial,
        }.Count(IsCopper) * 0.12;

        return RoundExcel(baseMass * (1 + copperFactor));
    }

    private static double DoorMass(ProjectConfig c)
    {
        var mass = DoorConfigMass(c.RuvnDoorConfiguration)
            + DoorConfigMass(c.RunnDoorConfiguration)
            + TransformerDoorMass(c.TransformerDoorConfiguration);

        if (c.HasTransformerMeshDoors) mass += 55;
        if (c.HasAntiVandalHinges) mass += 12;
        if (c.HasDoorSealing) mass += 8;
        return RoundExcel(mass);
    }

    private static double DoorConfigMass(string value)
    {
        if (value.Contains("двух", StringComparison.OrdinalIgnoreCase))
            return 65;
        if (value.Contains("съем", StringComparison.OrdinalIgnoreCase)
            || value.Contains("панель", StringComparison.OrdinalIgnoreCase))
            return 35;
        return 42;
    }

    private static double TransformerDoorMass(string value)
    {
        if (value.Contains("двух сторон", StringComparison.OrdinalIgnoreCase))
            return 130;
        if (value.Contains("панель", StringComparison.OrdinalIgnoreCase))
            return 88;
        return 70;
    }

    private static double AuxiliaryMass(AuxiliaryNeedsConfig? aux)
    {
        if (aux is null || !aux.HasAuxiliaryCabinet)
            return 0;

        double mass = 75;
        if (aux.LightingEnabled) mass += 8 + aux.LightingFixtureQuantity * 1.2 + (aux.OutdoorLightingEnabled ? 4 : 0);
        if (aux.SocketEnabled) mass += 4 + aux.SocketQuantity * 0.8;
        if (aux.HeatingEnabled) mass += 9 + aux.HeaterQuantity * 4 + (aux.MeterHeatingEnabled ? 3 : 0);
        if (aux.VentilationEnabled) mass += 7 + aux.FanQuantity * 3;
        if (aux.OpsEnabled) mass += 5;
        if (aux.RieseEnabled) mass += aux.RieseType.Contains("ИБП", StringComparison.OrdinalIgnoreCase) ? 35 : 18;
        return RoundExcel(mass);
    }

    private static double EnclosureOptionMass(ProjectConfig c)
    {
        double mass = 0;
        if (c.HasRoofDeflector) mass += 18;
        if (c.HasDoorCanopies) mass += 30;
        if (c.HasDoorSeals) mass += 6;
        if (c.HasLouverAnimalProtection) mass += 7;
        if (c.HasPadlockProvision) mass += 2;
        if (c.HasServicePlatform) mass += 120;
        return RoundExcel(mass);
    }

    private static string SelectTransformerBusbar(string tableSection, string material)
    {
        var section = NormalizeBusbarSection(tableSection);
        if (section == "-" || !IsCopper(material))
            return section;

        return CopperEquivalentBusbars.TryGetValue(NormalizeBusbarKey(section), out var copperSection)
            ? copperSection
            : section;
    }

    private static string NormalizeBusbarSection(string section)
    {
        if (string.IsNullOrWhiteSpace(section))
            return "-";

        return section
            .Trim()
            .Replace('x', 'х')
            .Replace('X', 'х')
            .Replace('Х', 'х');
    }

    private static string NormalizeBusbarKey(string section)
    {
        return NormalizeBusbarSection(section)
            .Replace(" ", "", StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private static bool IsCopper(string material) =>
        material.Contains("мед", StringComparison.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> CopperEquivalentBusbars = new(StringComparer.OrdinalIgnoreCase)
    {
        ["40х4"] = "40х4",
        ["40х5"] = "30х4",
        ["50х5"] = "40х4",
        ["80х6"] = "60х6",
        ["100х8"] = "80х8",
        ["2х(80х8)"] = "100х10",
        ["2х(100х10)"] = "2х(80х8)",
        ["3х(100х10)"] = "2х(100х10)",
    };
}
