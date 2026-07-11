using System.Globalization;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Documents;

public sealed class SpecificationItem
{
    public string Section { get; set; } = "";
    public string Position { get; set; } = "";
    public string Designation { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Nominal { get; set; } = "";
    public int Quantity { get; set; }
    public string Unit { get; set; } = "шт";
    public string Source { get; set; } = "";
    public string SourceConfidence { get; set; } = "";
    public string Notes { get; set; } = "";
}

public static class SpecificationBuilder
{
    private const string PowerSection = "Силовая часть и учет";
    private const string AuxiliarySection = "Собственные нужды, освещение и РИСЭ";

    public static IEnumerable<SpecificationItem> GenerateSpecification(ProjectConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var position = 1;
        yield return new SpecificationItem
        {
            Position = position.ToString(),
            Section = PowerSection,
            Designation = "T1",
            Name = "Силовой трансформатор",
            Type = config.Mark,
            Manufacturer = config.Manufacturer,
            Quantity = 1,
            Source = "project",
            SourceConfidence = "userInput",
        };

        if (config.AuxiliaryNeeds?.HasAuxiliaryCabinet == true)
        {
            position++;
            yield return new SpecificationItem
            {
                Position = position.ToString(),
                Section = AuxiliarySection,
                Designation = "ЩСН1",
                Name = "Шкаф собственных нужд",
                Type = string.IsNullOrWhiteSpace(config.AuxiliaryNeeds.CabinetModel)
                    ? "ЩСН"
                    : config.AuxiliaryNeeds.CabinetModel,
                Manufacturer = config.AuxiliaryNeeds.CabinetManufacturer,
                Quantity = 1,
                Source = "project",
                SourceConfidence = "userInput",
            };
        }
    }

    public static IReadOnlyList<SpecificationItem> GenerateSpecification(ProjectConfig config, CalculationResult result, CatalogStore store)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(store);

        var items = new List<SpecificationItem>();
        var pos = 1;
        var transformer = store.GetTransformer(config.Mark);
        Add(items, ref pos, PowerSection, "T1", "Силовой трансформатор", config.Mark, config.Manufacturer,
            transformer is null ? "" : $"{I(transformer.PowerKva)} кВА", 1, "шт", "transformers.json", "verified");

        AddRuvn(items, ref pos, config);

        AddLvInputDevices(items, ref pos, config, store);

        if (config.RunnSurgeArrester)
            Add(items, ref pos, PowerSection, "FV4-FV6", "ОПН РУНН", "ОПН", "", "0,4 кВ",
                3, "шт", "device_models.json", "needsVerification");
        if (config.HasCt)
            Add(items, ref pos, PowerSection, "TA1-TA3", "Трансформаторы тока учета", "ТТ", "", CtNominal(config.CtRatio, config.CtAccuracyClass),
                3, "шт", "project", "userInput");
        if (config.HasCtKip)
            Add(items, ref pos, PowerSection, "TAK1-TAK3", "Трансформаторы тока КИП", "ТТ КИП", "", CtNominal(config.CtKipRatio, config.CtKipAccuracyClass),
                3, "шт", "project", "userInput");
        if (config.HasMeter)
            Add(items, ref pos, PowerSection, "PI1", "Счетчик электрической энергии", "Счетчик", "", "",
                1, "шт", "device_models.json", "needsVerification");

        AddOutgoingFeeders(items, ref pos, config, store);

        if (!string.IsNullOrWhiteSpace(result.BusbarHv) && result.BusbarHv != "-")
            Add(items, ref pos, PowerSection, "ШВН", "Шины РУВН", result.BusbarHv, config.BusbarHvMaterial, ValueOrDash(config.Voltage),
                1, "компл.", "calculation", "calculated");
        if (!string.IsNullOrWhiteSpace(result.BusbarLv) && result.BusbarLv != "-")
            Add(items, ref pos, PowerSection, "ШНН", "Шины РУНН", result.BusbarLv, config.BusbarLvMaterial, "0,4 кВ",
                1, "компл.", "calculation", "calculated");
        if (!string.IsNullOrWhiteSpace(result.BusbarN) && result.BusbarN != "-")
            Add(items, ref pos, PowerSection, "N", "Шина N", result.BusbarN, config.BusbarNMaterial, "0,4 кВ",
                1, "компл.", "calculation", "calculated");
        if (!string.IsNullOrWhiteSpace(result.BusbarPe) && result.BusbarPe != "-")
            Add(items, ref pos, PowerSection, "PE/PEN", "Шина PE/PEN", result.BusbarPe, "", "",
                1, "компл.", "calculation", "calculated");

        AddAuxiliaryNeeds(items, ref pos, config.AuxiliaryNeeds, store);

        return items;
    }

    private static void AddLvInputDevices(List<SpecificationItem> items, ref int pos, ProjectConfig config, CatalogStore store)
    {
        var inputDevices = ProjectConfigNormalizer.GetLvInputDevices(config);
        if (inputDevices.Count == 0)
        {
            Add(items, ref pos, PowerSection, "QS2", "Вводное устройство РУНН", "Не установлено", "", "",
                1, "шт", "project", "userInput");
            return;
        }

        foreach (var input in inputDevices)
        {
            var source = SourceFor(store, input.DeviceType, input.Manufacturer, "");
            Add(items, ref pos, PowerSection, Designation(input), input.Caption, input.DeviceType, input.Manufacturer,
                input.Nominal > 0 ? CurrentA(input.Nominal) : "", 1, "шт", source.Source, source.Confidence);
        }
    }

    private static void AddRuvn(List<SpecificationItem> items, ref int pos, ProjectConfig config)
    {
        var branches = RuvnEngineering.Branches(config);
        for (var i = 0; i < branches.Count; i++)
        {
            var branch = branches[i];
            var qs = branches.Count == 1 ? "QS1" : $"QSВН{i + 1}";
            if (RuvnEngineering.IsVacuumBreaker(branch.SwitchType))
            {
                AddRuvnVacuumBranch(items, ref pos, branch, i, branches.Count);
                continue;
            }

            if (HasDevice(branch.SwitchType))
            {
                Add(items, ref pos, PowerSection, qs, $"Разъединитель РУВН - {branch.Title}",
                    branch.SwitchType, "", CurrentA(branch.SwitchNominal), 1, "шт", "project", "userInput");
            }

            if (!branch.FuseOn || !HasDevice(branch.FuseType))
                continue;

            var fuStart = i * 3 + 1;
            Add(items, ref pos, PowerSection, $"FU{fuStart}-FU{fuStart + 2}", $"ПКТ РУВН - {branch.Title}",
                branch.FuseType, "", CurrentTextOrDash(branch.FuseNominal), 3, "шт", "project", "userInput");
        }

        if (!config.RuvnSurgeArrester)
            return;

        var voltage = RuvnEngineering.RecommendedSurgeArresterOperatingVoltage(config.Voltage);
        var nominal = string.Join(", ", new[]
        {
            ValueOrDash(config.Voltage),
            string.IsNullOrWhiteSpace(voltage) ? "" : $"Uнр {voltage}",
            string.IsNullOrWhiteSpace(config.RuvnSurgeArresterThroughput) ? "" : config.RuvnSurgeArresterThroughput,
        }.Where(x => !string.IsNullOrWhiteSpace(x) && x != "-"));
        Add(items, ref pos, PowerSection, "FV1-FV3", "ОПН РУВН", "ОПН", "",
            nominal, 3, "шт", "device_models.json", "needsVerification", ValueOrDash(config.RuvnSurgeArresterLocation));
    }

    private static void AddRuvnVacuumBranch(List<SpecificationItem> items, ref int pos, RuvnBranchSelection branch, int index, int branchCount)
    {
        var equipment = branch.Equipment ?? new RuvnBranchEquipmentConfig();
        var suffix = branchCount == 1 ? "1" : $"ВН{index + 1}";
        var notes = string.Join("; ", new[]
        {
            equipment.VacuumBreakerInstallation,
            equipment.VacuumBreakerDrive,
            string.IsNullOrWhiteSpace(equipment.OperationalPower) ? "" : $"оперативное питание {equipment.OperationalPower}",
            string.IsNullOrWhiteSpace(equipment.Notes) ? "" : equipment.Notes.Trim(),
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

        if (RuvnEngineering.HasVisibleBreak(equipment.VisibleBreakBefore))
        {
            Add(items, ref pos, PowerSection, $"QS{suffix}A", $"Видимый разрыв перед вакуумным выключателем - {branch.Title}",
                equipment.VisibleBreakBefore, "", CurrentA(branch.SwitchNominal), 1, "шт", "project", "userInput");
        }

        var nominal = string.Join(", ", new[]
        {
            CurrentA(equipment.VacuumBreakerNominal),
            equipment.VacuumBreakerBreakingCurrentKa > 0 ? $"{F(equipment.VacuumBreakerBreakingCurrentKa)} кА" : "",
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
        Add(items, ref pos, PowerSection, $"Q{suffix}", $"Вакуумный выключатель РУВН - {branch.Title}",
            equipment.VacuumBreakerModel, "", nominal, 1, "шт", "project", "userInput", notes);

        if (RuvnEngineering.HasVisibleBreak(equipment.VisibleBreakAfter))
        {
            Add(items, ref pos, PowerSection, $"QS{suffix}B", $"Видимый разрыв после вакуумного выключателя - {branch.Title}",
                equipment.VisibleBreakAfter, "", CurrentA(branch.SwitchNominal), 1, "шт", "project", "userInput");
        }

        Add(items, ref pos, PowerSection, $"РЗА{suffix}", $"Терминал РЗА РУВН - {branch.Title}",
            equipment.RzaTerminal, "", RuvnEngineering.RzaFunctions(equipment), 1, "шт", "project", "userInput");

        var taQty = Math.Max(1, equipment.ProtectionCtQuantity);
        var taDesignation = branchCount == 1 ? $"TA1-TA{taQty}" : $"TA{suffix}.1-TA{suffix}.{taQty}";
        Add(items, ref pos, PowerSection, taDesignation, $"ТТ защиты РУВН - {branch.Title}",
            "ТТ", "", ValueOrDash(equipment.ProtectionCtRatio), taQty, "шт", "project", "userInput");

        if (equipment.HasTtnp)
        {
            Add(items, ref pos, PowerSection, $"TAN{suffix}", $"ТТНП РУВН - {branch.Title}",
                string.IsNullOrWhiteSpace(equipment.TtnpModel) ? "ТТНП" : equipment.TtnpModel, "", "", 1, "шт", "project", "userInput");
        }

        if (equipment.HasVoltageTransformer)
        {
            Add(items, ref pos, PowerSection, $"TV{suffix}", $"ТН РУВН - {branch.Title}",
                string.IsNullOrWhiteSpace(equipment.VoltageTransformerModel) ? "ТН" : equipment.VoltageTransformerModel, "", "", 1, "шт", "project", "userInput");
        }
    }

    private static void AddOutgoingFeeders(List<SpecificationItem> items, ref int pos, ProjectConfig config, CatalogStore store)
    {
        var feeders = (config.OutgoingFeeders ?? new List<OutgoingFeederConfig>())
            .OrderBy(f => f.Number)
            .ThenBy(f => f.DeviceType)
            .ToList();

        var nextQf = 2;
        var nextQs = 4;
        for (var i = 0; i < feeders.Count; i++)
        {
            var feeder = feeders[i];
            var isCircuitBreaker = IsCircuitBreakerType(feeder.DeviceType);
            var designator = isCircuitBreaker
                ? $"QF{nextQf++.ToString(CultureInfo.InvariantCulture)}"
                : $"QS{nextQs++.ToString(CultureInfo.InvariantCulture)}";
            var source = SourceFor(store, feeder.DeviceType, feeder.Manufacturer, feeder.Model);

            Add(items, ref pos, PowerSection, designator, FeederName(feeder),
                feeder.Model, feeder.Manufacturer, feeder.Nominal > 0 ? CurrentA(feeder.Nominal) : "",
                1, "шт", source.Source, source.Confidence, FeederNotes(feeder, source.Notes));

            if (!HasFeederMetering(feeder))
                continue;

            var taStart = 4 + i * 3;
            Add(items, ref pos, PowerSection, $"TA{taStart}-TA{taStart + 2}", $"ТТ отходящей линии {feeder.Number}",
                "ТТ", "", ValueOrDash(feeder.TtRatio), 3, "шт", "project", "userInput", MeteringNote(feeder));
            Add(items, ref pos, PowerSection, $"PI{i + 2}", $"Счетчик отходящей линии {feeder.Number}",
                "Счетчик", "", "", 1, "шт", "device_models.json", "needsVerification", MeteringNote(feeder));
        }
    }

    private static string FeederName(OutgoingFeederConfig feeder)
    {
        var name = $"Отходящая линия {feeder.DeviceType}-{feeder.Number}";
        if (!string.IsNullOrWhiteSpace(feeder.Purpose))
            name += $" - {feeder.Purpose.Trim()}";
        if (feeder.IsReserve)
            name += " (резерв)";
        return name;
    }

    private static string FeederNotes(OutgoingFeederConfig feeder, string sourceNotes)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(feeder.CableMark) || !string.IsNullOrWhiteSpace(feeder.CableSection))
            parts.Add($"кабель {CableDescription(feeder)}");
        parts.Add(MeteringNote(feeder));
        if (!string.IsNullOrWhiteSpace(feeder.Notes))
            parts.Add(feeder.Notes.Trim());
        if (!string.IsNullOrWhiteSpace(sourceNotes))
            parts.Add(sourceNotes.Trim());
        return string.Join("; ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string MeteringNote(OutgoingFeederConfig feeder)
    {
        var type = string.IsNullOrWhiteSpace(feeder.MeteringType)
            ? (feeder.HasMeter ? "Коммерческий" : "Нет")
            : feeder.MeteringType.Trim();
        return type.Equals("Нет", StringComparison.OrdinalIgnoreCase)
            ? "без учета"
            : $"учет: {type.ToLowerInvariant()}";
    }

    private static bool HasFeederMetering(OutgoingFeederConfig feeder)
    {
        var type = string.IsNullOrWhiteSpace(feeder.MeteringType)
            ? (feeder.HasMeter ? "Коммерческий" : "Нет")
            : feeder.MeteringType.Trim();
        return feeder.HasMeter || !type.Equals("Нет", StringComparison.OrdinalIgnoreCase);
    }

    private static string CableDescription(OutgoingFeederConfig feeder)
    {
        var mark = ValueOrDash(feeder.CableMark);
        var section = ValueOrDash(feeder.CableSection);
        if (mark == "-" && section == "-")
            return "-";
        if (mark == "-")
            return section;
        if (section == "-")
            return mark;
        return $"{mark} {section}";
    }

    private static void AddAuxiliaryNeeds(List<SpecificationItem> items, ref int pos, AuxiliaryNeedsConfig? aux, CatalogStore store)
    {
        if (aux is null || (!aux.HasAuxiliaryCabinet && !aux.HasRise))
            return;

        if (aux.HasAuxiliaryCabinet)
        {
            AddWithSource(items, ref pos, store, AuxiliarySection, "ЩСН1", "Шкаф собственных нужд",
                "Шкаф собственных нужд", aux.CabinetManufacturer, aux.CabinetModel, "", 1);
            AddWithSource(items, ref pos, store, AuxiliarySection, "QF20", "Вводной автомат ЩСН",
                "АВ", aux.MainBreakerManufacturer, aux.MainBreakerModel, aux.MainBreakerNominal > 0 ? CurrentA(aux.MainBreakerNominal) : "", 1);
        }

        if (aux.HasLighting)
        {
            AddWithSource(items, ref pos, store, AuxiliarySection, "QF21", "Автомат освещения",
                "АВ", aux.LightingBreakerManufacturer, aux.LightingBreakerModel, aux.LightingBreakerNominal > 0 ? CurrentA(aux.LightingBreakerNominal) : "",
                Math.Max(1, aux.LightingCircuits));
            AddWithSource(items, ref pos, store, AuxiliarySection, "EL1", "Светильники",
                "светильник", "", aux.LightingFixtureModel, ValueOrDash(aux.LightingControlMode), Math.Max(1, aux.LightingFixtureQuantity));

            if (aux.LightingControlMode.Contains("Фотореле", StringComparison.OrdinalIgnoreCase)
                || aux.LightingControlMode.Contains("Авто", StringComparison.OrdinalIgnoreCase))
            {
                AddWithSource(items, ref pos, store, AuxiliarySection, "BL1", "Фотореле освещения",
                    "фотореле", "", aux.PhotoRelayModel, "", 1);
            }

            if (aux.LightingControlMode.Contains("Астро", StringComparison.OrdinalIgnoreCase))
            {
                AddWithSource(items, ref pos, store, AuxiliarySection, "KT1", "Астрономический таймер",
                    "астротаймер", "", aux.AstroTimerModel, "", 1);
            }

            if (aux.LightingControlMode.Contains("Реле времени", StringComparison.OrdinalIgnoreCase))
            {
                AddWithSource(items, ref pos, store, AuxiliarySection, "KT1", "Реле времени",
                    "реле времени", "", aux.TimeRelayModel, "", 1);
            }
        }

        if (aux.SocketEnabled)
        {
            AddWithSource(items, ref pos, store, AuxiliarySection, "QF22", "Автомат сервисной розетки",
                "АВ", aux.SocketBreakerManufacturer, aux.SocketBreakerModel, aux.SocketBreakerNominal > 0 ? CurrentA(aux.SocketBreakerNominal) : "", 1);
            AddWithSource(items, ref pos, store, AuxiliarySection, "XS1", "Сервисная розетка",
                "розетка", "", aux.SocketModel, "", Math.Max(1, aux.SocketQuantity));
        }

        if (aux.HeatingEnabled)
        {
            AddWithSource(items, ref pos, store, AuxiliarySection, "QF23", "Автомат обогрева",
                "АВ", aux.HeatingBreakerManufacturer, aux.HeatingBreakerModel, aux.HeatingBreakerNominal > 0 ? CurrentA(aux.HeatingBreakerNominal) : "", 1);
            AddWithSource(items, ref pos, store, AuxiliarySection, "EK1", "Обогреватель шкафа",
                "обогреватель", "", aux.HeaterModel, "", Math.Max(1, aux.HeaterQuantity));
            AddWithSource(items, ref pos, store, AuxiliarySection, "SK1", "Термостат",
                "термостат", "", aux.ThermostatModel, "", 1);
        }

        if (aux.VentilationEnabled)
        {
            AddWithSource(items, ref pos, store, AuxiliarySection, "QF24", "Автомат вентиляции",
                "АВ", aux.VentilationBreakerManufacturer, aux.VentilationBreakerModel, aux.VentilationBreakerNominal > 0 ? CurrentA(aux.VentilationBreakerNominal) : "", 1);
            AddWithSource(items, ref pos, store, AuxiliarySection, "M1", "Вентилятор",
                "вентилятор", "", aux.FanModel, "", Math.Max(1, aux.FanQuantity));
        }

        if (aux.OpsEnabled)
        {
            Add(items, ref pos, AuxiliarySection, "ОПС1", "Охранно-пожарная сигнализация",
                string.IsNullOrWhiteSpace(aux.OpsModel) ? "ОПС" : aux.OpsModel,
                aux.OpsManufacturer,
                ValueOrDash(aux.OpsType),
                1,
                "компл.",
                "project",
                "userInput",
                aux.OpsLoops > 0 ? $"{aux.OpsLoops} шлейф." : "");
        }

        if (aux.HasRise)
        {
            AddWithSource(items, ref pos, store, AuxiliarySection, "QF25", "Защитный аппарат РИСЭ",
                "АВ", aux.RieseProtectionManufacturer, aux.RieseProtectionModel, "", 1);
            AddWithSource(items, ref pos, store, AuxiliarySection, "РИСЭ1", "Резервный источник снабжения электроэнергией",
                "РИСЭ", "", aux.RieseModuleModel, $"{ValueOrDash(aux.RieseType)}; {aux.RiesePowerVa} ВА; {aux.RieseAutonomyMinutes} мин",
                1);
        }
    }

    private static void AddWithSource(
        List<SpecificationItem> items,
        ref int pos,
        CatalogStore store,
        string section,
        string designation,
        string name,
        string deviceType,
        string manufacturer,
        string model,
        string nominal,
        int quantity)
    {
        var source = SourceFor(store, deviceType, manufacturer, model);
        Add(items, ref pos, section, designation, name, model, manufacturer, nominal, quantity, source.Unit, source.Source, source.Confidence, source.Notes);
    }

    private static void Add(
        List<SpecificationItem> items,
        ref int pos,
        string section,
        string designation,
        string name,
        string type,
        string manufacturer,
        string nominal,
        int quantity,
        string unit,
        string source,
        string sourceConfidence,
        string notes = "")
    {
        items.Add(new SpecificationItem
        {
            Section = section,
            Position = pos.ToString(CultureInfo.InvariantCulture),
            Designation = ValueOrDash(designation),
            Name = ValueOrDash(name),
            Type = ValueOrDash(type),
            Manufacturer = ValueOrDash(manufacturer),
            Nominal = ValueOrDash(nominal),
            Quantity = Math.Max(1, quantity),
            Unit = string.IsNullOrWhiteSpace(unit) ? "шт" : unit.Trim(),
            Source = ValueOrDash(source),
            SourceConfidence = ValueOrDash(sourceConfidence),
            Notes = notes.Trim(),
        });
        pos++;
    }

    private static SourceInfo SourceFor(CatalogStore store, string type, string manufacturer, string model)
    {
        var match = store.DeviceModels.FirstOrDefault(d =>
            d.Type.Equals(type, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(manufacturer) || d.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(model)
                || d.Model.Equals(model, StringComparison.OrdinalIgnoreCase)
                || d.Series.Equals(model, StringComparison.OrdinalIgnoreCase)));

        if (match is null)
            return new SourceInfo("project", "userInput", "шт", "");

        var source = string.IsNullOrWhiteSpace(match.DataSource) ? "device_models.json" : match.DataSource.Trim();
        var confidence = string.IsNullOrWhiteSpace(match.SourceConfidence) ? "needsVerification" : match.SourceConfidence.Trim();
        var unit = string.IsNullOrWhiteSpace(match.Unit) ? "шт" : match.Unit.Trim();
        return new SourceInfo(source, confidence, unit, match.Notes);
    }

    private static string Designation(LvInputDeviceSelection input) =>
        $"{input.FallbackCode}{input.DesignationNumber.ToString(CultureInfo.InvariantCulture)}";

    private static bool HasDevice(string value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Equals("Нет", StringComparison.OrdinalIgnoreCase);

    private static bool IsCircuitBreakerType(string? deviceType) =>
        deviceType is not null
        && (deviceType.Equals("АВ", StringComparison.OrdinalIgnoreCase)
            || deviceType.Contains("автомат", StringComparison.OrdinalIgnoreCase)
            || deviceType.Equals("QF", StringComparison.OrdinalIgnoreCase));

    private static string I(double v) => v.ToString("0", CultureInfo.InvariantCulture);

    private static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    private static string CurrentA(double v) => v > 0 ? $"{I(v)} А" : "";

    private static string CtNominal(string ratio, string accuracyClass)
    {
        var value = ValueOrDash(ratio);
        return string.IsNullOrWhiteSpace(accuracyClass)
            ? value
            : $"{value}; класс точности {accuracyClass.Trim()}";
    }

    private static string ValueOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private static string CurrentTextOrDash(string? value)
    {
        var text = ValueOrDash(value);
        if (text == "-")
            return text;

        var last = text[^1];
        if (last is 'А' or 'а' or 'A' or 'a')
        {
            var number = text[..^1].TrimEnd();
            return string.IsNullOrWhiteSpace(number) ? text : $"{number} А";
        }

        return text;
    }

    private sealed record SourceInfo(string Source, string Confidence, string Unit, string Notes);
}
