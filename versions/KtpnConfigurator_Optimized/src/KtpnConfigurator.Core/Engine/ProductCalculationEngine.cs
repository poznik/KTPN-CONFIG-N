using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Engine;

public static class ProductCalculationEngine
{
    public static CalculationResult Calculate(ProjectConfig project, CatalogStore store) =>
        project.ProductTypeId switch
        {
            ProductTypeIds.DoubleKtpn => CalculateDoubleKtpn(project, store),
            ProductTypeIds.Nku or ProductTypeIds.Shcho or ProductTypeIds.Vru => CalculateLowVoltageAssembly(project),
            ProductTypeIds.Kso or ProductTypeIds.Kru => CalculateMediumVoltageSwitchgear(project),
            _ => new CalculationResult(),
        };

    private static CalculationResult CalculateDoubleKtpn(ProjectConfig project, CatalogStore store)
    {
        var first = store.GetTransformer(project.Mark);
        var second = store.GetTransformer(project.DoubleKtpn.SecondTransformerMark);
        var result = new CalculationResult();
        if (first is null || second is null)
        {
            ProductValidationEngine.Apply(project, result, store);
            return result;
        }

        var baseLength = (project.RuvnType == "Нет" ? 0 : project.LenRuvn)
            + first.LengthMm + second.LengthMm + project.LenRunn * 2 + project.LengthBuffer * 2;
        result.LengthCalc = RoundUp(baseLength, 500);
        result.LengthFinal = project.ManualLength ?? result.LengthCalc;
        result.WidthCalc = Math.Max(2500, Math.Max(first.WidthMm, second.WidthMm) + project.TransformerTolerance);
        result.WidthFinal = project.ManualWidth ?? result.WidthCalc;
        result.HeightCalc = Math.Max(2500, Math.Max(first.HeightMm, second.HeightMm) + 350);
        result.HeightFinal = project.ManualHeight ?? result.HeightCalc;

        var area = result.LengthFinal * result.WidthFinal / 1_000_000d;
        var perimeter = (result.LengthFinal * 2 + result.WidthFinal * 4) / 1000d;
        result.BaseMassCalc = Math.Round(perimeter * store.ChannelWeight(project.Channel) * 1.15 + area * 24);
        result.BaseMass = project.ManualBaseMass ?? result.BaseMassCalc;
        var enclosureArea = 2 * (result.LengthFinal * result.WidthFinal
            + result.LengthFinal * result.HeightFinal
            + result.WidthFinal * result.HeightFinal) / 1_000_000d;
        result.BodyMassCalc = Math.Round(enclosureArea * store.SteelWeight(project.Thickness) * 0.82);
        result.BodyMass = project.ManualBodyMass ?? result.BodyMassCalc;
        result.TransformerMass = first.MassKg + second.MassKg;
        result.GrossMassCalc = result.BaseMass + result.BodyMass + result.TransformerMass;
        result.GrossMass = project.ManualGrossMass ?? result.GrossMassCalc;
        result.UsesManualMass = project.ManualBaseMass.HasValue || project.ManualBodyMass.HasValue || project.ManualGrossMass.HasValue;

        result.RatedCurrentA = Math.Max(first.RatedCurrentA, second.RatedCurrentA);
        result.InputNominal = Math.Min(project.DoubleKtpn.Section1InputNominalA, project.DoubleKtpn.Section2InputNominalA);
        result.ValidationOk = project.DoubleKtpn.Section1InputNominalA >= first.RatedCurrentA
            && project.DoubleKtpn.Section2InputNominalA >= second.RatedCurrentA;
        var limiting = first.RatedCurrentA >= second.RatedCurrentA ? first : second;
        result.BusbarHv = CalculationEngine.SelectTransformerBusbar(limiting.BusbarHv, project.BusbarHvMaterial);
        result.BusbarLv = CalculationEngine.SelectTransformerBusbar(limiting.BusbarLv, project.BusbarLvMaterial);
        result.BusbarN = CalculationEngine.SelectTransformerBusbar(limiting.BusbarLv, project.BusbarNMaterial);
        result.BusbarPe = CalculationEngine.NormalizeBusbarSection(limiting.BusbarPe);
        result.EquipmentMassEstimate = EstimateDoubleKtpnEquipmentMass(project);
        result.BusbarMassEstimate = Math.Round(result.LengthFinal / 1000d * 16);
        result.DoorMassEstimate = Math.Round(result.LengthFinal / 1000d * 30);
        result.AuxiliaryMassEstimate = project.AuxiliaryNeeds?.HasAuxiliaryCabinet == true ? 90 : 0;
        result.AdditionalMassEstimate = result.EquipmentMassEstimate + result.BusbarMassEstimate
            + result.DoorMassEstimate + result.AuxiliaryMassEstimate;
        result.GrossMassEstimated = result.GrossMass + result.AdditionalMassEstimate;
        ApplyDoubleKtpnSectionResults(project, result, first, second);
        ProductValidationEngine.Apply(project, result, store);
        result.ValidationOk = !result.HasErrors;
        return result;
    }

    private static CalculationResult CalculateLowVoltageAssembly(ProjectConfig project)
    {
        var config = project.LowVoltageAssembly;
        var busbar = SelectLowVoltageBusbarProfile(config.RatedBusCurrentA, config.BusbarMaterial);
        var length = config.Panels.Sum(panel => Math.Max(0, panel.WidthMm));
        var result = new CalculationResult
        {
            LengthCalc = length,
            WidthCalc = config.DepthMm,
            HeightCalc = config.HeightMm,
            InputNominal = config.RatedBusCurrentA,
            RatedCurrentA = config.RatedBusCurrentA,
            ValidationOk = config.RatedBusCurrentA > 0,
        };
        result.LengthFinal = project.ManualLength ?? result.LengthCalc;
        result.WidthFinal = project.ManualWidth ?? result.WidthCalc;
        result.HeightFinal = project.ManualHeight ?? result.HeightCalc;
        result.BaseMassCalc = Math.Round(Math.Max(0, result.LengthFinal) / 1000d * 18);
        result.BaseMass = project.ManualBaseMass ?? result.BaseMassCalc;
        result.BodyMassCalc = Math.Round(config.Panels.Sum(panel => Math.Max(0, panel.EstimatedMassKg)));
        result.BodyMass = project.ManualBodyMass ?? result.BodyMassCalc;
        result.GrossMassCalc = result.BodyMass + result.BaseMass;
        result.GrossMass = project.ManualGrossMass ?? result.GrossMassCalc;
        result.EquipmentMassEstimate = EstimateLowVoltageAccessoryMass(config);
        result.BusbarMassEstimate = EstimateLowVoltageBusbarMass(busbar, result.LengthFinal, config.SectionCount, config.EarthingSystem);
        result.AdditionalMassEstimate = result.EquipmentMassEstimate + result.BusbarMassEstimate;
        result.GrossMassEstimated = result.GrossMass + result.AdditionalMassEstimate;
        result.BusbarLv = FormatLowVoltageBusbar(busbar, config.BusbarMaterial);
        result.BusbarN = FormatNeutralBusbar(busbar, config);
        result.BusbarPe = FormatPeBusbar(busbar, config);
        ProductValidationEngine.Apply(project, result, null);
        result.ValidationOk = !result.HasErrors;
        return result;
    }

    private static CalculationResult CalculateMediumVoltageSwitchgear(ProjectConfig project)
    {
        var config = project.MediumVoltageSwitchgear;
        var result = new CalculationResult
        {
            LengthCalc = config.Cells.Sum(cell => Math.Max(0, cell.WidthMm)),
            WidthCalc = config.DepthMm,
            HeightCalc = config.HeightMm,
            InputNominal = config.RatedBusCurrentA,
            RatedCurrentA = config.RatedBusCurrentA,
            ValidationOk = config.RatedBusCurrentA > 0,
        };
        result.LengthFinal = project.ManualLength ?? result.LengthCalc;
        result.WidthFinal = project.ManualWidth ?? result.WidthCalc;
        result.HeightFinal = project.ManualHeight ?? result.HeightCalc;
        result.BodyMassCalc = Math.Round(config.Cells.Sum(cell => Math.Max(0, cell.EstimatedMassKg)));
        result.BodyMass = project.ManualBodyMass ?? result.BodyMassCalc;
        result.EquipmentMassEstimate = EstimateMediumVoltageSecondaryMass(config);
        result.BusbarMassEstimate = EstimateMediumVoltageBusbarMass(config, result.LengthFinal);
        result.AdditionalMassEstimate = result.EquipmentMassEstimate + result.BusbarMassEstimate;
        result.GrossMassCalc = result.BodyMass;
        result.GrossMass = project.ManualGrossMass ?? result.GrossMassCalc;
        result.GrossMassEstimated = result.GrossMass + result.AdditionalMassEstimate;
        result.BusbarHv = $"{config.RatedBusCurrentA} А; {config.ShortTimeWithstandCurrentKa:0.#} кА {config.ShortTimeDurationSeconds:0.#} с; сечение по испытанной серии";
        ProductValidationEngine.Apply(project, result, null);
        result.ValidationOk = !result.HasErrors;
        return result;
    }

    private static double EstimateDoubleKtpnEquipmentMass(ProjectConfig project)
    {
        var feederMass = (project.OutgoingFeeders ?? new List<OutgoingFeederConfig>()).Sum(feeder =>
            feeder.DeviceType.Equals("РПС", StringComparison.OrdinalIgnoreCase) ? 18 : 12);
        var ruvnMass = project.RuvnType == "Проходная" ? 520d : project.RuvnType == "Нет" ? 0d : 320d;
        return Math.Round(ruvnMass + 2 * 75 + 110 + feederMass);
    }

    private static void ApplyDoubleKtpnSectionResults(ProjectConfig project, CalculationResult result, TransformerSpec first, TransformerSpec second)
    {
        result.Section1RatedCurrentA = first.RatedCurrentA;
        result.Section2RatedCurrentA = second.RatedCurrentA;
        result.Section1InputNominalA = project.DoubleKtpn.Section1InputNominalA;
        result.Section2InputNominalA = project.DoubleKtpn.Section2InputNominalA;
        result.Section1TransformerMass = first.MassKg;
        result.Section2TransformerMass = second.MassKg;
        result.Section1ShortCircuitCurrentKa = EstimateTransformerShortCircuitKa(first);
        result.Section2ShortCircuitCurrentKa = EstimateTransformerShortCircuitKa(second);
        result.Section1BusbarLv = CalculationEngine.SelectTransformerBusbar(first.BusbarLv, project.BusbarLvMaterial);
        result.Section2BusbarLv = CalculationEngine.SelectTransformerBusbar(second.BusbarLv, project.BusbarLvMaterial);
        result.Section1BusbarN = CalculationEngine.SelectTransformerBusbar(first.BusbarLv, project.BusbarNMaterial);
        result.Section2BusbarN = CalculationEngine.SelectTransformerBusbar(second.BusbarLv, project.BusbarNMaterial);
        result.Section1BusbarPe = CalculationEngine.NormalizeBusbarSection(first.BusbarPe);
        result.Section2BusbarPe = CalculationEngine.NormalizeBusbarSection(second.BusbarPe);

        var sharedSectionMass = (result.BaseMass + result.BodyMass + result.EquipmentMassEstimate
            + result.BusbarMassEstimate + result.DoorMassEstimate + result.AuxiliaryMassEstimate) / 2d;
        result.Section1EstimatedMass = Math.Round(first.MassKg + sharedSectionMass);
        result.Section2EstimatedMass = Math.Round(second.MassKg + sharedSectionMass);
    }

    private static double EstimateTransformerShortCircuitKa(TransformerSpec transformer)
    {
        var ukPercent = transformer.PowerKva switch
        {
            <= 250 => 4.0,
            <= 630 => 4.5,
            <= 1000 => 5.5,
            _ => 6.0,
        };
        return Math.Round(transformer.RatedCurrentA * 100d / ukPercent / 1000d, 1);
    }

    private static double EstimateLowVoltageAccessoryMass(LowVoltageAssemblyConfig config)
    {
        return Math.Round(config.Panels.Sum(panel =>
            Math.Max(0, panel.CircuitCount) * 2.2
            + (panel.HasMetering ? 12 : 0)
            + (panel.HasSurgeProtection ? 6 : 0)));
    }

    private static double EstimateMediumVoltageSecondaryMass(MediumVoltageSwitchgearConfig config)
    {
        return Math.Round(config.Cells.Sum(cell =>
            (cell.MainDevice.Contains("выключатель", StringComparison.OrdinalIgnoreCase) ? 35d : 0d)
            + (string.IsNullOrWhiteSpace(cell.CtRatio) ? 0d : 12d)
            + (cell.HasVoltageTransformer ? 45d : 0d)
            + (string.IsNullOrWhiteSpace(cell.RelayProtection) ? 0d : 18d)
            + (cell.HasEarthingSwitch ? 16d : 0d)));
    }

    private static double EstimateMediumVoltageBusbarMass(MediumVoltageSwitchgearConfig config, double lengthMm)
    {
        var currentFactor = config.RatedBusCurrentA switch
        {
            <= 630 => 22,
            <= 1000 => 32,
            <= 1600 => 46,
            <= 2500 => 64,
            _ => 82,
        };
        return Math.Round(Math.Max(1, lengthMm / 1000d) * currentFactor);
    }

    private static LowVoltageBusbarProfile SelectLowVoltageBusbarProfile(int current, string material)
    {
        var copper = material.Contains("мед", StringComparison.OrdinalIgnoreCase);
        var values = copper
            ? new (int Current, string Section, double Area)[] { (400, "30x5", 150), (630, "40x5", 200), (1000, "50x8", 400), (1600, "2x60x10", 1200), (2500, "2x100x10", 2000), (3200, "3x100x10", 3000), (4000, "4x100x10", 4000), (5000, "5x100x10", 5000), (6300, "6x100x10", 6000) }
            : new (int Current, string Section, double Area)[] { (400, "40x5", 200), (630, "50x6", 300), (1000, "80x8", 640), (1600, "2x80x10", 1600), (2500, "2x100x10", 2000), (3200, "3x100x10", 3000), (4000, "4x100x10", 4000), (5000, "5x100x10", 5000), (6300, "6x100x10", 6000) };
        var selected = values.FirstOrDefault(item => current <= item.Current);
        return selected == default
            ? new LowVoltageBusbarProfile(current, ">6300 А", 0, copper, true)
            : new LowVoltageBusbarProfile(selected.Current, selected.Section, selected.Area, copper, false);
    }

    private static string FormatLowVoltageBusbar(LowVoltageBusbarProfile profile, string material) =>
        profile.RequiresSeparateCalculation
            ? $">6300 А; требуется отдельный расчет ({material})"
            : $"{profile.Section} мм ({material}); предварительно, проверить нагрев и КЗ";

    private static string FormatNeutralBusbar(LowVoltageBusbarProfile profile, LowVoltageAssemblyConfig config)
    {
        if (profile.RequiresSeparateCalculation)
            return $"N/PEN по расчету для {config.EarthingSystem}";
        var name = config.EarthingSystem.Equals("TN-C", StringComparison.OrdinalIgnoreCase) ? "PEN" : "N";
        return $"{name} {profile.Section} мм ({config.BusbarMaterial}); предварительно";
    }

    private static string FormatPeBusbar(LowVoltageBusbarProfile profile, LowVoltageAssemblyConfig config)
    {
        if (profile.RequiresSeparateCalculation)
            return "PE по расчету КЗ";
        return $"PE не менее 50% фазной шины ({config.BusbarMaterial}); проверить по КЗ";
    }

    private static double EstimateLowVoltageBusbarMass(LowVoltageBusbarProfile profile, double lengthMm, int sectionCount, string earthingSystem)
    {
        if (profile.RequiresSeparateCalculation || profile.AreaMm2 <= 0)
            return 0;
        var densityKgPerMm2M = profile.Copper ? 0.00896 : 0.00270;
        var conductorCount = earthingSystem.Equals("TN-C", StringComparison.OrdinalIgnoreCase) ? 4.0 : 4.5;
        var sectionFactor = Math.Max(1, sectionCount * 0.85);
        return Math.Round(profile.AreaMm2 * densityKgPerMm2M * Math.Max(0.6, lengthMm / 1000d) * conductorCount * sectionFactor);
    }

    private static double RoundUp(double value, double step) => Math.Ceiling(value / step) * step;

    private sealed record LowVoltageBusbarProfile(int CurrentA, string Section, double AreaMm2, bool Copper, bool RequiresSeparateCalculation);
}

public static class ProductValidationEngine
{
    public static void Apply(ProjectConfig project, CalculationResult result, CatalogStore? store)
    {
        switch (project.ProductTypeId)
        {
            case ProductTypeIds.DoubleKtpn:
                ValidateDoubleKtpn(project, result, store!);
                break;
            case ProductTypeIds.Nku:
            case ProductTypeIds.Shcho:
            case ProductTypeIds.Vru:
                ValidateLowVoltage(project, result);
                break;
            case ProductTypeIds.Kso:
            case ProductTypeIds.Kru:
                ValidateMediumVoltage(project, result);
                break;
        }
    }

    private static void ValidateDoubleKtpn(ProjectConfig project, CalculationResult result, CatalogStore store)
    {
        var first = store.GetTransformer(project.Mark);
        var second = store.GetTransformer(project.DoubleKtpn.SecondTransformerMark);
        if (first is null) result.Messages.Add(new(Severity.Error, "2КТПН: не выбран трансформатор Т1."));
        if (second is null) result.Messages.Add(new(Severity.Error, "2КТПН: не выбран трансформатор Т2."));
        if (first is null || second is null) return;

        if (project.DoubleKtpn.Section1InputNominalA < first.RatedCurrentA)
            result.Messages.Add(new(Severity.Error, $"2КТПН: ввод секции 1 меньше тока Т1 ({first.RatedCurrentA:0} А)."));
        if (project.DoubleKtpn.Section2InputNominalA < second.RatedCurrentA)
            result.Messages.Add(new(Severity.Error, $"2КТПН: ввод секции 2 меньше тока Т2 ({second.RatedCurrentA:0} А)."));
        var reserveCurrent = Math.Max(first.RatedCurrentA, second.RatedCurrentA);
        if (project.DoubleKtpn.SectionCouplerNominalA < reserveCurrent)
            result.Messages.Add(new(Severity.Error, $"2КТПН: секционный аппарат меньше расчетного тока резервного режима ({reserveCurrent:0} А)."));
        if (Math.Abs(first.PowerKva - second.PowerKva) > 0.1)
            result.Messages.Add(new(Severity.Warning, "2КТПН: мощности Т1 и Т2 различаются; проверьте распределение нагрузок, АВР и допустимую работу одной секции от второго трансформатора."));
        if (!result.Section1BusbarLv.Equals(result.Section2BusbarLv, StringComparison.OrdinalIgnoreCase))
            result.Messages.Add(new(Severity.Warning, "2КТПН: сечения шин секций отличаются из-за разных ТМГ; проверьте унификацию шин и перемычек в РУНН."));
        if (result.Section1ShortCircuitCurrentKa > 0 || result.Section2ShortCircuitCurrentKa > 0)
            result.Messages.Add(new(Severity.Warning, $"2КТПН: ориентировочный КЗ на шинах секций {result.Section1ShortCircuitCurrentKa:0.#}/{result.Section2ShortCircuitCurrentKa:0.#} кА; проверьте отключающую способность вводных, секционного и отходящих аппаратов."));
        if (project.DoubleKtpn.AutomaticTransferEnabled && project.DoubleKtpn.ParallelOperationAllowed)
            result.Messages.Add(new(Severity.Warning, "2КТПН: параллельная работа при АВР требует отдельной проверки трансформаторов, токов КЗ и блокировок."));
        if (project.DoubleKtpn.AutomaticTransferEnabled)
            result.Messages.Add(new(Severity.Warning, "2КТПН: проверьте нагрузку каждой секции и допустимую аварийную загрузку одного трансформатора при АВР."));
        if (!project.DoubleKtpn.ParallelOperationAllowed && !project.DoubleKtpn.NormalCouplerPosition.Equals("Отключен", StringComparison.OrdinalIgnoreCase))
            result.Messages.Add(new(Severity.Error, "2КТПН: при запрещенной параллельной работе секционный аппарат в нормальном режиме должен быть отключен."));
        foreach (var feeder in project.OutgoingFeeders ?? new List<OutgoingFeederConfig>())
        {
            if (feeder.SectionNumber is not (1 or 2))
                result.Messages.Add(new(Severity.Error, $"2КТПН: для отходящей линии {feeder.DeviceType}-{feeder.Number} не выбрана секция 1 или 2."));
            var inputNominal = feeder.SectionNumber == 2 ? project.DoubleKtpn.Section2InputNominalA : project.DoubleKtpn.Section1InputNominalA;
            if (feeder.Nominal > inputNominal)
                result.Messages.Add(new(Severity.Error, $"2КТПН: номинал линии {feeder.DeviceType}-{feeder.Number} выше ввода секции {feeder.SectionNumber}."));
        }
        result.Messages.Add(new(Severity.Info, "2КТПН: проверены два ввода, секционный аппарат и базовая логика АВР."));
    }

    private static void ValidateLowVoltage(ProjectConfig project, CalculationResult result)
    {
        var config = project.LowVoltageAssembly;
        var name = ProductRegistry.ResolveOrDefault(project.ProductTypeId).DisplayName;
        if (config.RatedVoltageV <= 0) result.Messages.Add(new(Severity.Error, $"{name}: не задано номинальное напряжение."));
        if (config.RatedBusCurrentA <= 0) result.Messages.Add(new(Severity.Error, $"{name}: не задан номинальный ток шин."));
        if (config.ShortTimeWithstandCurrentKa <= 0 || config.PeakWithstandCurrentKa <= 0)
            result.Messages.Add(new(Severity.Error, $"{name}: не заданы Icw/Ipk."));
        if (config.DesignShortCircuitCurrentKa > config.ShortTimeWithstandCurrentKa)
            result.Messages.Add(new(Severity.Error, $"{name}: расчетный ток КЗ выше Icw выбранной компоновки."));
        if (config.PeakWithstandCurrentKa < config.DesignShortCircuitCurrentKa * 2)
            result.Messages.Add(new(Severity.Warning, $"{name}: Ipk выглядит низким относительно расчетного тока КЗ; проверьте электродинамическую стойкость."));
        if (config.Panels.Count == 0) result.Messages.Add(new(Severity.Error, $"{name}: не добавлены панели."));
        if (config.SectionCount is < 1 or > 4)
            result.Messages.Add(new(Severity.Error, $"{name}: количество секций должно быть от 1 до 4."));
        if (!config.Panels.Any(panel => panel.PanelType.Contains("ввод", StringComparison.OrdinalIgnoreCase)))
            result.Messages.Add(new(Severity.Warning, $"{name}: в линейке нет явно указанной вводной панели."));
        if (config.SectionCount > 1 && !config.Panels.Any(panel => panel.PanelType.Contains("секцион", StringComparison.OrdinalIgnoreCase)))
            result.Messages.Add(new(Severity.Warning, $"{name}: для нескольких секций не добавлена секционная панель."));
        var unverifiedPanels = config.Panels.Count(panel => panel.EquipmentSourceConfidence.Equals("needsVerification", StringComparison.OrdinalIgnoreCase));
        if (unverifiedPanels > 0)
            result.Messages.Add(new(Severity.Warning, $"{name}: {unverifiedPanels} поз. линейки требуют проверки по паспорту/каталогу оборудования."));
        if (project.ProductTypeId == ProductTypeIds.Vru && config.RatedVoltageV != 400)
            result.Messages.Add(new(Severity.Warning, "ВРУ: профиль рассчитан на сеть 230/400 В; проверьте выбранное напряжение."));
        if (project.ProductTypeId == ProductTypeIds.Vru && config.EarthingSystem == "IT")
            result.Messages.Add(new(Severity.Warning, "ВРУ: система IT не соответствует типовому профилю жилых и общественных зданий с глухозаземленной нейтралью."));
        foreach (var panel in config.Panels)
        {
            if (panel.SectionNumber < 1 || panel.SectionNumber > config.SectionCount)
                result.Messages.Add(new(Severity.Error, $"{name}: панель {panel.Number} назначена несуществующей секции {panel.SectionNumber}."));
            if (panel.RatedCurrentA > config.RatedBusCurrentA)
                result.Messages.Add(new(Severity.Error, $"{name}: ток панели {panel.Number} выше тока сборных шин."));
            if (panel.BreakingCapacityKa < config.ShortTimeWithstandCurrentKa)
                result.Messages.Add(new(Severity.Warning, $"{name}: для панели {panel.Number} проверьте отключающую способность аппарата относительно Icw."));
            if (panel.CircuitCount < 0)
                result.Messages.Add(new(Severity.Error, $"{name}: у панели {panel.Number} количество линий не может быть отрицательным."));
            if (panel.HasMetering && string.IsNullOrWhiteSpace(panel.Model))
                result.Messages.Add(new(Severity.Warning, $"{name}: у панели учета {panel.Number} не указана модель счетчика/узла учета."));
            if (panel.WidthMm <= 0 || panel.EstimatedMassKg <= 0)
                result.Messages.Add(new(Severity.Warning, $"{name}: для панели {panel.Number} не заданы корректные ширина и масса."));
        }
        result.Messages.Add(new(Severity.Warning, $"{name}: сечение шин предварительное; требуется проверка нагрева, КЗ и подтвержденной конструкции по ГОСТ IEC 61439."));
    }

    private static void ValidateMediumVoltage(ProjectConfig project, CalculationResult result)
    {
        var config = project.MediumVoltageSwitchgear;
        var name = ProductRegistry.ResolveOrDefault(project.ProductTypeId).DisplayName;
        if (config.RatedVoltageKv <= 0 || config.HighestOperatingVoltageKv < config.RatedVoltageKv)
            result.Messages.Add(new(Severity.Error, $"{name}: некорректно заданы номинальное и наибольшее рабочее напряжения."));
        if (config.RatedBusCurrentA <= 0) result.Messages.Add(new(Severity.Error, $"{name}: не задан номинальный ток шин."));
        if (config.ShortTimeWithstandCurrentKa <= 0 || config.PeakWithstandCurrentKa <= 0)
            result.Messages.Add(new(Severity.Error, $"{name}: не заданы токи термической и электродинамической стойкости."));
        if (config.DesignShortCircuitCurrentKa > config.ShortTimeWithstandCurrentKa)
            result.Messages.Add(new(Severity.Error, $"{name}: расчетный ток КЗ выше тока термической стойкости линейки."));
        if (config.DesignShortCircuitCurrentKa > config.BreakerBreakingCurrentKa)
            result.Messages.Add(new(Severity.Error, $"{name}: расчетный ток КЗ выше принятого тока отключения выключателей."));
        if (config.PeakWithstandCurrentKa < config.DesignShortCircuitCurrentKa * 2)
            result.Messages.Add(new(Severity.Warning, $"{name}: Ipk выглядит низким относительно расчетного тока КЗ; проверьте электродинамическую стойкость."));
        if (config.Cells.Count == 0) result.Messages.Add(new(Severity.Error, $"{name}: не добавлены ячейки."));
        var unverifiedCells = config.Cells.Count(cell => cell.EquipmentSourceConfidence.Equals("needsVerification", StringComparison.OrdinalIgnoreCase));
        if (unverifiedCells > 0)
            result.Messages.Add(new(Severity.Warning, $"{name}: {unverifiedCells} поз. линейки требуют проверки по серии ячеек, паспортам аппаратов и протоколам испытаний."));
        foreach (var cell in config.Cells)
        {
            if (cell.RatedCurrentA > config.RatedBusCurrentA)
                result.Messages.Add(new(Severity.Error, $"{name}: ток ячейки {cell.Number} выше тока сборных шин."));
            if (cell.MainDevice.Contains("выключатель", StringComparison.OrdinalIgnoreCase)
                && cell.BreakingCurrentKa < config.BreakerBreakingCurrentKa)
                result.Messages.Add(new(Severity.Warning, $"{name}: ток отключения выключателя ячейки {cell.Number} ниже принятого для РУ."));
            if (cell.WidthMm <= 0 || cell.EstimatedMassKg <= 0)
                result.Messages.Add(new(Severity.Warning, $"{name}: для ячейки {cell.Number} не заданы корректные ширина и масса."));
            if (cell.MainDevice.Contains("вакуум", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(cell.RelayProtection))
                result.Messages.Add(new(Severity.Warning, $"{name}: для вакуумного выключателя ячейки {cell.Number} не задана РЗА."));
            if (cell.MainDevice.Contains("вакуум", StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(cell.VisibleBreakBefore) || string.IsNullOrWhiteSpace(cell.VisibleBreakAfter)))
                result.Messages.Add(new(Severity.Warning, $"{name}: для вакуумного выключателя ячейки {cell.Number} проверьте видимые разрывы до и после выключателя."));
            if (cell.MainDevice.Contains("выключатель", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(cell.CtRatio))
                result.Messages.Add(new(Severity.Warning, $"{name}: для ячейки {cell.Number} с выключателем не указан ТТ."));
            if (cell.HasVoltageTransformer && string.IsNullOrWhiteSpace(cell.VoltageTransformerModel))
                result.Messages.Add(new(Severity.Warning, $"{name}: для ячейки {cell.Number} включен ТН, но не указана модель."));
        }
        if (project.ProductTypeId == ProductTypeIds.Kso)
            result.Messages.Add(new(Severity.Warning, "КСО: характеристики должны подтверждаться ТУ конкретной серии камер."));
        if (project.ProductTypeId == ProductTypeIds.Kru && string.IsNullOrWhiteSpace(config.IacClassification))
            result.Messages.Add(new(Severity.Error, "КРУ: не задан класс дуговой стойкости IAC."));
        if (project.ProductTypeId == ProductTypeIds.Kru)
            result.Messages.Add(new(Severity.Warning, "КРУ: IAC, LSC и класс перегородок должны подтверждаться протоколами испытаний выбранной серии."));
    }
}
