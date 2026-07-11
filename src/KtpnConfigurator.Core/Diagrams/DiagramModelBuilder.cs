using System.Globalization;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Diagrams;

public static class DiagramModelBuilder
{
    public static DiagramModel Build(ProjectConfig config, CalculationResult result, CatalogStore catalog)
    {
        var notation = DiagramNotation.From(catalog);
        var model = new DiagramModel();
        var oneLine = new DiagramSheet
        {
            Kind = DiagramSheetKind.OneLine,
            Name = "Однолинейная схема",
        };

        BuildOneLine(oneLine, config, result, notation);
        model.Sheets.Add(oneLine);

        var metering = BuildMeteringSheet(config, notation);
        if (metering.Nodes.Count > 0)
            model.Sheets.Add(metering);

        return model;
    }

    private static void BuildOneLine(DiagramSheet sheet, ProjectConfig config, CalculationResult result, DiagramNotation notation)
    {
        var source = AddNode(sheet, "source-hv", DiagramNodeKind.Source, "", "", "Ввод", config.Voltage);
        var previous = source.Id;

        if (HasDevice(config.RuvnSwitch))
        {
            var node = AddNode(
                sheet,
                "hv-switch",
                DiagramNodeKind.HvSwitch,
                "disconnector",
                notation.Designation("disconnector", 1, "QS"),
                "Разъединитель РУВН",
                config.RuvnSwitch,
                nominal: CurrentA(config.RuvnSwitchNominal));
            Connect(sheet, previous, node.Id, DiagramConnectionKind.Power);
            previous = node.Id;
        }

        if (HasDevice(config.FuseType))
        {
            var node = AddNode(
                sheet,
                "hv-fuses",
                DiagramNodeKind.Fuse,
                "fuse",
                notation.DesignationRange("fuse", 1, 3, "FU"),
                "Предохранители РУВН",
                config.FuseType,
                nominal: CurrentText(config.FuseNominal));
            Connect(sheet, previous, node.Id, DiagramConnectionKind.Power);
            previous = node.Id;
        }

        if (config.RuvnSurgeArrester)
        {
            var node = AddNode(
                sheet,
                "hv-surge",
                DiagramNodeKind.SurgeArrester,
                "surgeArrester",
                notation.DesignationRange("surgeArrester", 1, 3, "FV"),
                "ОПН РУВН",
                "ОПН");
            Connect(sheet, previous, node.Id, DiagramConnectionKind.ProtectiveEarth);
        }

        var transformer = AddNode(
            sheet,
            "transformer",
            DiagramNodeKind.PowerTransformer,
            "powerTransformer",
            notation.Designation("powerTransformer", 1, "T"),
            "Силовой трансформатор",
            config.Mark,
            manufacturer: config.Manufacturer);
        transformer.Labels.Add(new DiagramLabel { Role = "Voltage", Text = $"{Safe(config.Voltage)}/0,4 кВ" });
        Connect(sheet, previous, transformer.Id, DiagramConnectionKind.Power);
        previous = transformer.Id;

        var inputDevices = ProjectConfigNormalizer.GetLvInputDevices(config);
        if (inputDevices.Count == 0)
        {
            inputDevices =
            [
                new LvInputDeviceSelection(
                    LvInputDeviceKind.CircuitBreaker,
                    "circuitBreaker",
                    1,
                    "QF",
                    "АВ",
                    "Резерв ввода РУНН",
                    "",
                    result.InputNominal > 0 ? (int)result.InputNominal : 250),
            ];
        }

        foreach (var input in inputDevices)
        {
            var node = AddNode(
                sheet,
                $"lv-input-{input.DesignationNumber.ToString(CultureInfo.InvariantCulture)}",
                DiagramNodeKind.LvInputDevice,
                input.SymbolKey,
                notation.Designation(input.SymbolKey, input.DesignationNumber, input.FallbackCode),
                input.Caption,
                input.DeviceType,
                input.Manufacturer,
                nominal: CurrentA(input.Nominal));
            Connect(sheet, previous, node.Id, DiagramConnectionKind.Power);
            previous = node.Id;
        }

        if (config.HasCt || config.HasMeter)
        {
            var ct = AddNode(
                sheet,
                "input-ct",
                DiagramNodeKind.CurrentTransformer,
                "currentTransformer",
                notation.DesignationRange("currentTransformer", 1, 3, "TA"),
                "ТТ учета на вводе",
                "ТТ",
                nominal: Safe(config.CtRatio));
            Connect(sheet, previous, ct.Id, DiagramConnectionKind.Power);
            previous = ct.Id;

            if (config.HasMeter)
            {
                var meter = AddNode(
                    sheet,
                    "input-meter",
                    DiagramNodeKind.Meter,
                    "meter",
                    notation.Designation("meter", 1, "PI"),
                    "Счетчик ввода",
                    "счетчик");
                Connect(sheet, ct.Id, meter.Id, DiagramConnectionKind.Metering, "цепи ТТ");
            }
        }

        var busbar = AddNode(
            sheet,
            "busbar-runn",
            DiagramNodeKind.Busbar,
            "busbar",
            "",
            "Шины РУНН",
            Safe(result.BusbarLv));
        busbar.Labels.Add(new DiagramLabel { Role = "Phase", Text = "A/B/C/N/PE" });
        Connect(sheet, previous, busbar.Id, DiagramConnectionKind.Power);

        if (config.RunnSurgeArrester)
        {
            var node = AddNode(
                sheet,
                "lv-surge",
                DiagramNodeKind.SurgeArrester,
                "surgeArrester",
                notation.DesignationRange("surgeArrester", 4, 6, "FV"),
                "ОПН РУНН",
                "ОПН");
            Connect(sheet, busbar.Id, node.Id, DiagramConnectionKind.ProtectiveEarth);
        }

        AddOutgoingFeeders(sheet, config, notation, busbar.Id);
        AddAuxiliaryNeeds(sheet, config.AuxiliaryNeeds, notation, busbar.Id);
    }

    private static void AddOutgoingFeeders(DiagramSheet sheet, ProjectConfig config, DiagramNotation notation, string busbarId)
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
            var symbolKey = FeederSymbolKey(feeder.DeviceType);
            var fallback = IsCircuitBreakerType(feeder.DeviceType) ? "QF" : "QS";
            var number = IsCircuitBreakerType(feeder.DeviceType) ? nextQf++ : nextQs++;
            var deviceId = $"feeder-{feeder.Number.ToString(CultureInfo.InvariantCulture)}-device";
            var device = AddNode(
                sheet,
                deviceId,
                DiagramNodeKind.OutgoingFeederDevice,
                symbolKey,
                notation.Designation(symbolKey, number, fallback),
                $"Отходящая линия {feeder.Number}",
                feeder.DeviceType,
                feeder.Manufacturer,
                feeder.Model,
                CurrentA(feeder.Nominal));
            device.Metadata["FeederNumber"] = feeder.Number.ToString(CultureInfo.InvariantCulture);
            Connect(sheet, busbarId, device.Id, DiagramConnectionKind.Power);

            var previous = device.Id;
            if (feeder.HasMeter)
            {
                var taStart = 4 + i * 3;
                var ct = AddNode(
                    sheet,
                    $"feeder-{feeder.Number.ToString(CultureInfo.InvariantCulture)}-ct",
                    DiagramNodeKind.CurrentTransformer,
                    "currentTransformer",
                    notation.DesignationRange("currentTransformer", taStart, taStart + 2, "TA"),
                    $"ТТ отходящей линии {feeder.Number}",
                    "ТТ",
                    nominal: Safe(feeder.TtRatio));
                ct.Metadata["FeederNumber"] = feeder.Number.ToString(CultureInfo.InvariantCulture);
                Connect(sheet, previous, ct.Id, DiagramConnectionKind.Power);
                previous = ct.Id;

                var meter = AddNode(
                    sheet,
                    $"feeder-{feeder.Number.ToString(CultureInfo.InvariantCulture)}-meter",
                    DiagramNodeKind.Meter,
                    "meter",
                    notation.Designation("meter", i + 2, "PI"),
                    $"Счетчик отходящей линии {feeder.Number}",
                    "счетчик");
                meter.Metadata["FeederNumber"] = feeder.Number.ToString(CultureInfo.InvariantCulture);
                Connect(sheet, ct.Id, meter.Id, DiagramConnectionKind.Metering, "к цепям учета");
            }

            var output = AddNode(
                sheet,
                $"feeder-{feeder.Number.ToString(CultureInfo.InvariantCulture)}-output",
                DiagramNodeKind.FeederOutput,
                "",
                "",
                $"Выход фидера {feeder.Number}",
                config.OutgoingExecution);
            output.Metadata["FeederNumber"] = feeder.Number.ToString(CultureInfo.InvariantCulture);
            Connect(sheet, previous, output.Id, DiagramConnectionKind.Power);
            Connect(sheet, busbarId, output.Id, DiagramConnectionKind.Neutral);
        }
    }

    private static void AddAuxiliaryNeeds(DiagramSheet sheet, AuxiliaryNeedsConfig? auxiliary, DiagramNotation notation, string busbarId)
    {
        if (auxiliary?.HasAuxiliaryCabinet != true)
            return;

        var breaker = AddNode(
            sheet,
            "aux-main-breaker",
            DiagramNodeKind.AuxiliaryBreaker,
            "circuitBreaker",
            notation.Designation("circuitBreaker", 20, "QF"),
            "Вводной автомат ЩСН",
            "АВ",
            auxiliary.MainBreakerManufacturer,
            auxiliary.MainBreakerModel,
            CurrentA(auxiliary.MainBreakerNominal));
        Connect(sheet, busbarId, breaker.Id, DiagramConnectionKind.Power);

        var cabinet = AddNode(
            sheet,
            "aux-cabinet",
            DiagramNodeKind.AuxiliaryCabinet,
            "auxiliaryCabinet",
            notation.Designation("auxiliaryCabinet", 1, "ЩСН"),
            "Шкаф собственных нужд",
            "ЩСН",
            auxiliary.CabinetManufacturer,
            auxiliary.CabinetModel);
        Connect(sheet, breaker.Id, cabinet.Id, DiagramConnectionKind.Power);
        Connect(sheet, busbarId, cabinet.Id, DiagramConnectionKind.Neutral);

        if (auxiliary.HasLighting)
        {
            var lighting = AddNode(sheet, "aux-lighting", DiagramNodeKind.Lighting, "lighting",
                notation.Designation("lighting", 1, "EL"), "Освещение КТПН", "освещение", model: auxiliary.LightingFixtureModel);
            lighting.Labels.Add(new DiagramLabel { Role = "Control", Text = Safe(auxiliary.LightingControlMode) });
            Connect(sheet, cabinet.Id, lighting.Id, DiagramConnectionKind.Power);

            AddLightingControl(sheet, auxiliary, notation, cabinet.Id);
        }

        if (auxiliary.SocketEnabled)
        {
            var socket = AddNode(sheet, "aux-socket", DiagramNodeKind.Socket, "socket",
                notation.Designation("socket", 1, "XS"), "Сервисная розетка", "розетка", model: auxiliary.SocketModel);
            Connect(sheet, cabinet.Id, socket.Id, DiagramConnectionKind.Power);
        }

        if (auxiliary.HeatingEnabled)
        {
            var heating = AddNode(sheet, "aux-heating", DiagramNodeKind.Heating, "heating",
                notation.Designation("heating", 1, "EK"), "Обогрев", "обогреватель", model: auxiliary.HeaterModel);
            Connect(sheet, cabinet.Id, heating.Id, DiagramConnectionKind.Power);
        }

        if (auxiliary.VentilationEnabled)
        {
            var ventilation = AddNode(sheet, "aux-ventilation", DiagramNodeKind.Ventilation, "ventilation",
                notation.Designation("ventilation", 1, "M"), "Вентиляция", "вентилятор", model: auxiliary.FanModel);
            Connect(sheet, cabinet.Id, ventilation.Id, DiagramConnectionKind.Power);
        }

        if (auxiliary.HasRise)
        {
            var backup = AddNode(sheet, "aux-rise", DiagramNodeKind.BackupPowerSource, "backupPowerSource",
                notation.Designation("backupPowerSource", 1, "РИСЭ"), "РИСЭ", "РИСЭ", model: auxiliary.RieseModuleModel);
            backup.Labels.Add(new DiagramLabel { Role = "Type", Text = Safe(auxiliary.RieseType) });
            Connect(sheet, cabinet.Id, backup.Id, DiagramConnectionKind.Power);
        }
    }

    private static void AddLightingControl(DiagramSheet sheet, AuxiliaryNeedsConfig auxiliary, DiagramNotation notation, string cabinetId)
    {
        if (auxiliary.LightingControlMode.Contains("Фотореле", StringComparison.OrdinalIgnoreCase)
            || auxiliary.LightingControlMode.Contains("Авто", StringComparison.OrdinalIgnoreCase))
        {
            var node = AddNode(sheet, "aux-photo-relay", DiagramNodeKind.Lighting, "photoRelay",
                notation.Designation("photoRelay", 1, "BL"), "Фотореле", "фотореле", model: auxiliary.PhotoRelayModel);
            Connect(sheet, cabinetId, node.Id, DiagramConnectionKind.Control);
        }

        if (auxiliary.LightingControlMode.Contains("Астро", StringComparison.OrdinalIgnoreCase))
        {
            var node = AddNode(sheet, "aux-astro-timer", DiagramNodeKind.Lighting, "astroTimer",
                notation.Designation("astroTimer", 1, "KT"), "Астротаймер", "астротаймер", model: auxiliary.AstroTimerModel);
            Connect(sheet, cabinetId, node.Id, DiagramConnectionKind.Control);
        }

        if (auxiliary.LightingControlMode.Contains("Реле времени", StringComparison.OrdinalIgnoreCase))
        {
            var node = AddNode(sheet, "aux-time-relay", DiagramNodeKind.Lighting, "timeRelay",
                notation.Designation("timeRelay", 1, "KT"), "Реле времени", "реле времени", model: auxiliary.TimeRelayModel);
            Connect(sheet, cabinetId, node.Id, DiagramConnectionKind.Control);
        }
    }

    private static DiagramSheet BuildMeteringSheet(ProjectConfig config, DiagramNotation notation)
    {
        var sheet = new DiagramSheet
        {
            Kind = DiagramSheetKind.Metering,
            Name = "Цепи учета",
        };

        if (config.HasMeter || config.HasCt)
        {
            var inputCt = AddNode(sheet, "metering-input-ct", DiagramNodeKind.CurrentTransformer, "currentTransformer",
                notation.DesignationRange("currentTransformer", 1, 3, "TA"), "ТТ учета на вводе", "ТТ", nominal: Safe(config.CtRatio));
            var terminal = AddNode(sheet, "metering-input-terminal", DiagramNodeKind.TerminalBlock, "", "XT1", "Клеммник цепей учета", "XT");
            Connect(sheet, inputCt.Id, terminal.Id, DiagramConnectionKind.Metering);

            if (config.HasMeter)
            {
                var meter = AddNode(sheet, "metering-input-meter", DiagramNodeKind.Meter, "meter",
                    notation.Designation("meter", 1, "PI"), "Счетчик ввода", "счетчик");
                Connect(sheet, terminal.Id, meter.Id, DiagramConnectionKind.Metering);
            }
        }

        var feeders = (config.OutgoingFeeders ?? new List<OutgoingFeederConfig>())
            .OrderBy(f => f.Number)
            .ThenBy(f => f.DeviceType)
            .ToList();

        for (var i = 0; i < feeders.Count; i++)
        {
            var feeder = feeders[i];
            if (!feeder.HasMeter)
                continue;

            var taStart = 4 + i * 3;
            var prefix = $"metering-feeder-{feeder.Number.ToString(CultureInfo.InvariantCulture)}";
            var ct = AddNode(sheet, $"{prefix}-ct", DiagramNodeKind.CurrentTransformer, "currentTransformer",
                notation.DesignationRange("currentTransformer", taStart, taStart + 2, "TA"),
                $"ТТ отходящей линии {feeder.Number}", "ТТ", nominal: Safe(feeder.TtRatio));
            var terminal = AddNode(sheet, $"{prefix}-terminal", DiagramNodeKind.TerminalBlock, "", $"XT{feeder.Number + 1}",
                $"Клеммник учета фидера {feeder.Number}", "XT");
            var meter = AddNode(sheet, $"{prefix}-meter", DiagramNodeKind.Meter, "meter",
                notation.Designation("meter", i + 2, "PI"), $"Счетчик фидера {feeder.Number}", "счетчик");

            Connect(sheet, ct.Id, terminal.Id, DiagramConnectionKind.Metering);
            Connect(sheet, terminal.Id, meter.Id, DiagramConnectionKind.Metering);
        }

        return sheet;
    }

    private static DiagramNode AddNode(
        DiagramSheet sheet,
        string id,
        DiagramNodeKind kind,
        string symbolKey,
        string designation,
        string title,
        string deviceType,
        string manufacturer = "",
        string model = "",
        string nominal = "")
    {
        var node = new DiagramNode
        {
            Id = id,
            Kind = kind,
            SymbolKey = symbolKey,
            Designation = designation,
            Title = title,
            DeviceType = deviceType,
            Manufacturer = manufacturer,
            Model = model,
            Nominal = nominal,
        };
        sheet.Nodes.Add(node);
        return node;
    }

    private static void Connect(DiagramSheet sheet, string fromNodeId, string toNodeId, DiagramConnectionKind kind, string label = "")
    {
        sheet.Connections.Add(new DiagramConnection
        {
            Id = $"{fromNodeId}->{toNodeId}:{kind}",
            Kind = kind,
            From = new DiagramPort { NodeId = fromNodeId, Port = "out" },
            To = new DiagramPort { NodeId = toNodeId, Port = "in" },
            Label = label,
        });
    }

    private static string FeederSymbolKey(string deviceType) =>
        IsCircuitBreakerType(deviceType)
            ? "circuitBreaker"
            : deviceType.Equals("РПС", StringComparison.OrdinalIgnoreCase)
                || deviceType.Equals("ПВР/NH", StringComparison.OrdinalIgnoreCase)
                ? "switchDisconnectFuse"
                : "disconnector";

    private static bool IsCircuitBreakerType(string deviceType) =>
        deviceType.Equals("АВ", StringComparison.OrdinalIgnoreCase)
        || deviceType.Contains("автомат", StringComparison.OrdinalIgnoreCase)
        || deviceType.Equals("QF", StringComparison.OrdinalIgnoreCase);

    private static bool HasDevice(string value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Equals("Нет", StringComparison.OrdinalIgnoreCase);

    private static string CurrentA(double value) => value > 0 ? $"{value:0} А" : "";

    private static string CurrentText(string? value)
    {
        var text = Safe(value);
        if (text == "-")
            return "";

        var last = text[^1];
        if (last is 'А' or 'а' or 'A' or 'a')
        {
            var number = text[..^1].TrimEnd();
            return string.IsNullOrWhiteSpace(number) ? text : $"{number} А";
        }

        return text;
    }

    private static string Safe(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private sealed class DiagramNotation
    {
        private readonly Dictionary<string, DiagramSymbol> _symbols;

        private DiagramNotation(Dictionary<string, DiagramSymbol> symbols)
        {
            _symbols = symbols;
        }

        public static DiagramNotation From(CatalogStore catalog)
        {
            var symbols = catalog.DiagramSymbols
                .Where(s => !string.IsNullOrWhiteSpace(s.SymbolKey))
                .GroupBy(s => s.SymbolKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            return new DiagramNotation(symbols);
        }

        public string Designation(string symbolKey, int number, string fallbackCode)
        {
            var pattern = Pattern(symbolKey, $"{Code(symbolKey, fallbackCode)}{{n}}");
            return FormatSingle(pattern, number);
        }

        public string DesignationRange(string symbolKey, int start, int end, string fallbackCode)
        {
            var pattern = Pattern(symbolKey, $"{Code(symbolKey, fallbackCode)}{{start}}-{Code(symbolKey, fallbackCode)}{{end}}");
            if (pattern.Contains("{start}", StringComparison.OrdinalIgnoreCase)
                || pattern.Contains("{end}", StringComparison.OrdinalIgnoreCase))
            {
                return pattern
                    .Replace("{start}", start.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                    .Replace("{end}", end.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
            }

            if (pattern.Contains("{n}", StringComparison.OrdinalIgnoreCase))
                return $"{FormatSingle(pattern, start)}-{Code(symbolKey, fallbackCode)}{end.ToString(CultureInfo.InvariantCulture)}";

            var code = Code(symbolKey, fallbackCode);
            return $"{code}{start.ToString(CultureInfo.InvariantCulture)}-{code}{end.ToString(CultureInfo.InvariantCulture)}";
        }

        private string Code(string symbolKey, string fallbackCode) =>
            _symbols.TryGetValue(symbolKey, out var symbol) && !string.IsNullOrWhiteSpace(symbol.LetterCode)
                ? symbol.LetterCode.Trim()
                : fallbackCode;

        private string Pattern(string symbolKey, string fallbackPattern) =>
            _symbols.TryGetValue(symbolKey, out var symbol) && !string.IsNullOrWhiteSpace(symbol.DesignationPattern)
                ? symbol.DesignationPattern.Trim()
                : fallbackPattern;

        private static string FormatSingle(string pattern, int number)
        {
            var value = number.ToString(CultureInfo.InvariantCulture);
            if (pattern.Contains("{n}", StringComparison.OrdinalIgnoreCase))
                return pattern.Replace("{n}", value, StringComparison.OrdinalIgnoreCase);
            if (pattern.Contains("{start}", StringComparison.OrdinalIgnoreCase)
                || pattern.Contains("{end}", StringComparison.OrdinalIgnoreCase))
            {
                return pattern
                    .Replace("{start}", value, StringComparison.OrdinalIgnoreCase)
                    .Replace("{end}", value, StringComparison.OrdinalIgnoreCase);
            }

            return $"{pattern}{value}";
        }
    }
}
