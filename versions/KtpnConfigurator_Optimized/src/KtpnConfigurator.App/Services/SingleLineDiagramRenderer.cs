using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Diagrams;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.App.Services;

public static class SingleLineDiagramRenderer
{
    private const string CircuitBreakerSymbol = "circuitBreaker";
    private const string DisconnectorSymbol = "disconnector";
    private const string SwitchDisconnectFuseSymbol = "switchDisconnectFuse";
    private const string FuseSymbol = "fuse";
    private const string SurgeArresterSymbol = "surgeArrester";
    private const string CurrentTransformerSymbol = "currentTransformer";
    private const string MeterSymbol = "meter";
    private const string PowerTransformerSymbol = "powerTransformer";
    private const string AuxiliaryCabinetSymbol = "auxiliaryCabinet";
    private const string LightingSymbol = "lighting";
    private const string PhotoRelaySymbol = "photoRelay";
    private const string AstroTimerSymbol = "astroTimer";
    private const string TimeRelaySymbol = "timeRelay";
    private const string HeatingSymbol = "heating";
    private const string VentilationSymbol = "ventilation";
    private const string SocketSymbol = "socket";
    private const string BackupPowerSourceSymbol = "backupPowerSource";

    private const double CleanDiagramWidth = 1500;
    private const double CleanDiagramMinHeight = 620;
    private const double CleanDiagramTopBottom = 250;
    private const double CleanDiagramBranchStep = 126;
    private const double MeteringSheetWidth = 1500;
    private const double MeteringSheetMinHeight = 620;
    private const double MeteringSheetRowStep = 142;

    public static Size Measure(ProjectConfig cfg) =>
        Measure(cfg, DiagramSheetKind.OneLine);

    public static Size Measure(ProjectConfig cfg, DiagramSheetKind kind)
    {
        if (kind == DiagramSheetKind.Metering)
        {
            var rows = MeteringRowCount(cfg);
            return new Size(MeteringSheetWidth, Math.Max(MeteringSheetMinHeight, 180 + rows * MeteringSheetRowStep));
        }

        var branchCount = Math.Max(1, cfg.OutgoingFeeders?.Count ?? 0);
        if (cfg.AuxiliaryNeeds?.HasAuxiliaryCabinet == true)
            branchCount++;

        var height = Math.Max(CleanDiagramMinHeight, CleanDiagramTopBottom + branchCount * CleanDiagramBranchStep);
        return new Size(CleanDiagramWidth, height);
    }

    public static byte[] RenderPng(ProjectConfig cfg, CalculationResult res, CatalogStore catalog) =>
        RenderPng(cfg, res, catalog, DiagramSheetKind.OneLine);

    public static byte[] RenderPng(ProjectConfig cfg, CalculationResult res, CatalogStore catalog, DiagramSheetKind kind)
    {
        var size = Measure(cfg, kind);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
            Draw(dc, cfg, res, catalog, size, 1, kind);

        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(size.Width),
            (int)Math.Ceiling(size.Height),
            96,
            96,
            PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    public static IReadOnlyList<RenderedDiagramSheet> RenderPngSheets(ProjectConfig cfg, CalculationResult res, CatalogStore catalog)
    {
        var model = DiagramModelBuilder.Build(cfg, res, catalog);
        var result = new List<RenderedDiagramSheet>();

        foreach (var sheet in model.Sheets)
        {
            var size = Measure(cfg, sheet.Kind);
            var png = RenderPng(cfg, res, catalog, sheet.Kind);
            result.Add(new RenderedDiagramSheet(sheet.Name, sheet.Kind, size, png));
        }

        return result;
    }

    public static void Draw(DrawingContext dc, ProjectConfig cfg, CalculationResult res, CatalogStore catalog, Size size, double pixelsPerDip)
        => Draw(dc, cfg, res, catalog, size, pixelsPerDip, DiagramSheetKind.OneLine);

    public static void Draw(DrawingContext dc, ProjectConfig cfg, CalculationResult res, CatalogStore catalog, Size size, double pixelsPerDip, DiagramSheetKind kind)
    {
        var model = DiagramModelBuilder.Build(cfg, res, catalog);
        var scene = DiagramLayoutEngine.Build(model, kind, size);
        var renderer = new CadSceneRenderer(dc, pixelsPerDip);
        renderer.Draw(scene);
    }

    private static int MeteringRowCount(ProjectConfig cfg)
    {
        var count = cfg.HasMeter || cfg.HasCt ? 1 : 0;
        count += (cfg.OutgoingFeeders ?? new List<OutgoingFeederConfig>()).Count(f => f.HasMeter);
        return Math.Max(1, count);
    }

    public static IReadOnlyList<string> CollectDesignations(ProjectConfig cfg, CatalogStore catalog)
    {
        var notation = DiagramNotation.From(catalog);
        var result = new List<string>();

        if (HasDevice(cfg.RuvnSwitch))
            result.Add(notation.Designation(DisconnectorSymbol, 1, "QS"));
        if (HasDevice(cfg.FuseType))
            result.Add(notation.DesignationRange(FuseSymbol, 1, 3, "FU"));
        if (cfg.RuvnSurgeArrester)
            result.Add(notation.DesignationRange(SurgeArresterSymbol, 1, 3, "FV"));

        result.Add(notation.Designation(PowerTransformerSymbol, 1, "T"));

        AddRunnInputDesignations(cfg, notation, result);

        if (cfg.RunnSurgeArrester)
            result.Add(notation.DesignationRange(SurgeArresterSymbol, 4, 6, "FV"));

        if (cfg.HasCt || cfg.HasMeter)
            result.Add(notation.DesignationRange(CurrentTransformerSymbol, 1, 3, "TA"));
        if (cfg.HasMeter)
            result.Add(notation.Designation(MeterSymbol, 1, "PI"));

        AddAuxiliaryDesignations(cfg.AuxiliaryNeeds, notation, result);
        AddFeederDesignations(cfg, notation, result);

        return result;
    }

    private static void AddRunnInputDesignations(ProjectConfig cfg, DiagramNotation notation, List<string> result)
    {
        var hasInputDevice = false;
        if (cfg.PvrOn)
        {
            result.Add(notation.Designation(SwitchDisconnectFuseSymbol, 2, "QS"));
            hasInputDevice = true;
        }

        if (cfg.ReOn)
        {
            result.Add(notation.Designation(DisconnectorSymbol, 3, "QS"));
            hasInputDevice = true;
        }

        if (cfg.AvInOn)
        {
            result.Add(notation.Designation(CircuitBreakerSymbol, 1, "QF"));
            hasInputDevice = true;
        }

        if (!hasInputDevice)
            result.Add(notation.Designation(CircuitBreakerSymbol, 1, "QF"));
    }

    private static void AddAuxiliaryDesignations(AuxiliaryNeedsConfig? aux, DiagramNotation notation, List<string> result)
    {
        if (aux?.HasAuxiliaryCabinet != true)
            return;

        result.Add(notation.Designation(CircuitBreakerSymbol, 20, "QF"));
        result.Add(notation.Designation(AuxiliaryCabinetSymbol, 1, "ЩСН"));

        if (aux.HasLighting)
        {
            result.Add(notation.Designation(CircuitBreakerSymbol, 21, "QF"));
            result.Add(notation.Designation(LightingSymbol, 1, "EL"));
            if (aux.LightingControlMode.Contains("Фотореле", StringComparison.OrdinalIgnoreCase)
                || aux.LightingControlMode.Contains("Авто", StringComparison.OrdinalIgnoreCase))
                result.Add(notation.Designation(PhotoRelaySymbol, 1, "BL"));
            if (aux.LightingControlMode.Contains("Астро", StringComparison.OrdinalIgnoreCase))
                result.Add(notation.Designation(AstroTimerSymbol, 1, "KT"));
            if (aux.LightingControlMode.Contains("Реле времени", StringComparison.OrdinalIgnoreCase))
                result.Add(notation.Designation(TimeRelaySymbol, 1, "KT"));
        }

        if (aux.SocketEnabled)
        {
            result.Add(notation.Designation(CircuitBreakerSymbol, 22, "QF"));
            result.Add(notation.Designation(SocketSymbol, 1, "XS"));
        }

        if (aux.HeatingEnabled)
        {
            result.Add(notation.Designation(CircuitBreakerSymbol, 23, "QF"));
            result.Add(notation.Designation(HeatingSymbol, 1, "EK"));
        }

        if (aux.VentilationEnabled)
        {
            result.Add(notation.Designation(CircuitBreakerSymbol, 24, "QF"));
            result.Add(notation.Designation(VentilationSymbol, 1, "M"));
        }

        if (aux.HasRise)
        {
            result.Add(notation.Designation(CircuitBreakerSymbol, 25, "QF"));
            result.Add(notation.Designation(BackupPowerSourceSymbol, 1, "РИСЭ"));
        }
    }

    private static void AddFeederDesignations(ProjectConfig cfg, DiagramNotation notation, List<string> result)
    {
        var feeders = OrderedFeeders(cfg);
        var nextQf = 2;
        var nextQs = 4;

        for (var i = 0; i < feeders.Count; i++)
        {
            var feeder = feeders[i];
            var symbolKey = FeederSymbolKey(feeder.DeviceType);
            var fallback = FallbackCodeForDeviceType(feeder.DeviceType);
            var number = IsCircuitBreakerType(feeder.DeviceType) ? nextQf++ : nextQs++;
            result.Add(notation.Designation(symbolKey, number, fallback));

            if (feeder.HasMeter)
            {
                var taStart = 4 + i * 3;
                result.Add(notation.DesignationRange(CurrentTransformerSymbol, taStart, taStart + 2, "TA"));
                result.Add(notation.Designation(MeterSymbol, i + 2, "PI"));
            }
        }
    }

    private static List<OutgoingFeederConfig> OrderedFeeders(ProjectConfig cfg) =>
        (cfg.OutgoingFeeders ?? new List<OutgoingFeederConfig>())
        .OrderBy(f => f.Number)
        .ThenBy(f => f.DeviceType)
        .ToList();

    private static string FeederSymbolKey(string deviceType) =>
        IsCircuitBreakerType(deviceType)
            ? CircuitBreakerSymbol
            : deviceType.Equals("РПС", StringComparison.OrdinalIgnoreCase)
                || deviceType.Equals("ПВР/NH", StringComparison.OrdinalIgnoreCase)
                ? SwitchDisconnectFuseSymbol
                : DisconnectorSymbol;

    private static string FallbackCodeForDeviceType(string deviceType) =>
        IsCircuitBreakerType(deviceType) ? "QF" : "QS";

    private static bool IsCircuitBreakerType(string deviceType) =>
        deviceType.Equals("АВ", StringComparison.OrdinalIgnoreCase)
        || deviceType.Contains("автомат", StringComparison.OrdinalIgnoreCase)
        || deviceType.Equals("QF", StringComparison.OrdinalIgnoreCase);

    private static bool HasDevice(string value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Equals("Нет", StringComparison.OrdinalIgnoreCase);

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

        private string Code(string symbolKey, string fallbackCode)
        {
            return _symbols.TryGetValue(symbolKey, out var symbol) && !string.IsNullOrWhiteSpace(symbol.LetterCode)
                ? symbol.LetterCode.Trim()
                : fallbackCode;
        }

        private string Pattern(string symbolKey, string fallbackPattern)
        {
            return _symbols.TryGetValue(symbolKey, out var symbol) && !string.IsNullOrWhiteSpace(symbol.DesignationPattern)
                ? symbol.DesignationPattern.Trim()
                : fallbackPattern;
        }

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
