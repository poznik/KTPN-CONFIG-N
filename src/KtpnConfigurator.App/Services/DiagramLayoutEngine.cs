using System.Windows;
using KtpnConfigurator.Core.Diagrams;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.App.Services;

internal static class DiagramLayoutEngine
{
    public static CadScene Build(DiagramModel model, ProjectConfig cfg, CalculationResult res, Size size)
        => Build(model, DiagramSheetKind.OneLine, size);

    public static CadScene Build(DiagramModel model, DiagramSheetKind kind, Size size)
    {
        var scene = new CadScene(size);
        var sheet = model.Sheets.FirstOrDefault(s => s.Kind == kind);
        if (sheet is null)
            return scene;

        if (kind == DiagramSheetKind.Metering)
            new MeteringLayout(scene, sheet).Draw();
        else
            new OneLineLayout(scene, sheet).Draw();

        return scene;
    }

    private sealed class OneLineLayout
    {
        private readonly CadScene _scene;
        private readonly DiagramSheet _sheet;
        private readonly DiagramSymbolLibrary _symbols;
        private readonly Dictionary<string, DiagramNode> _nodes;

        public OneLineLayout(CadScene scene, DiagramSheet sheet)
        {
            _scene = scene;
            _sheet = sheet;
            _symbols = new DiagramSymbolLibrary(scene);
            _nodes = sheet.Nodes
                .Where(n => !string.IsNullOrWhiteSpace(n.Id))
                .GroupBy(n => n.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }

        public void Draw()
        {
            var feederDevices = FeederDevices();
            var hasAux = Node("aux-cabinet") is not null;
            var branchCount = Math.Max(1, feederDevices.Count) + (hasAux ? 1 : 0);

            var left = 70.0;
            var inputY = 165.0;
            var busX = 900.0;
            var busTop = 86.0;
            var firstBranchY = 292.0;
            var branchStep = 126.0;
            var lastBranchY = firstBranchY + (branchCount - 1) * branchStep;
            var busBottom = Math.Min(_scene.Size.Height - 82, Math.Max(lastBranchY + 74, inputY + 280));

            _scene.Text("Однолинейная схема КТПН", left, 28, 20, 360, bold: true);
            _scene.Text(Safe(Node("transformer")?.DeviceType), left, 55, 12, 380);

            DrawIncomingPath(left, inputY, busX);
            DrawBus(busX, busTop, busBottom);

            if (Node("input-ct") is not null || Node("input-meter") is not null)
                DrawMeteringBox(busX - 198, inputY + 58);

            var row = 0;
            if (feederDevices.Count == 0)
            {
                DrawReserveFeeder(busX, firstBranchY);
                row++;
            }
            else
            {
                foreach (var feeder in feederDevices)
                {
                    DrawFeeder(busX, firstBranchY + row * branchStep, feeder);
                    row++;
                }
            }

            if (hasAux)
                DrawAuxiliaryBranch(busX, firstBranchY + row * branchStep);

            if (Node("lv-surge") is { } lvSurge)
                DrawLvSurge(busX, busBottom - 74, lvSurge);
        }

        private void DrawIncomingPath(double left, double y, double busX)
        {
            var qsX = left + 120;
            var fuX = left + 240;
            var transformerX = left + 386;
            var phaseTapX = busX + 28;

            _symbols.ArrowLeft(left - 28, y);
            _scene.Text($"Ввод {Safe(Node("source-hv")?.DeviceType)}", left, y - 44, 12, 120);
            _scene.Line(left - 8, y, qsX - 38, y, CadStroke.Normal);

            if (Node("hv-switch") is { } hvSwitch)
                _symbols.Disconnector(qsX, y, hvSwitch.Designation, hvSwitch.Nominal);
            else
                _scene.Line(qsX - 38, y, qsX + 38, y, CadStroke.Normal);

            _scene.Line(qsX + 38, y, fuX - 36, y, CadStroke.Normal);
            if (Node("hv-fuses") is { } hvFuses)
                _symbols.Fuse(fuX, y, hvFuses.Designation, hvFuses.Nominal);
            else
                _scene.Line(fuX - 36, y, fuX + 36, y, CadStroke.Normal);

            _scene.Line(fuX + 36, y, transformerX - 58, y, CadStroke.Normal);

            if (Node("transformer") is { } transformer)
            {
                _symbols.PowerTransformerHorizontal(
                    transformerX,
                    y,
                    transformer.Designation,
                    Label(transformer, "Voltage"),
                    transformer.DeviceType);
            }
            else
            {
                _scene.Line(transformerX - 58, y, transformerX + 58, y, CadStroke.Normal);
            }

            var wireX = transformerX + 58;
            var deviceX = transformerX + 92;
            foreach (var device in LvInputDevices())
            {
                _scene.Line(wireX, y, deviceX - 38, y, CadStroke.Normal);
                DrawRunnInputDevice(deviceX, y, device);
                wireX = deviceX + 42;
                deviceX += 96;
            }

            var ctX = deviceX + 8;
            _scene.Line(wireX, y, ctX - 28, y, CadStroke.Normal);

            if (Node("input-ct") is { } inputCt)
            {
                _symbols.CurrentTransformerSet(ctX, y, inputCt.Designation, inputCt.Nominal);
                if (Node("input-meter") is not null)
                    DrawInputMeteringLink(ctX, y);
                _scene.Line(ctX + 48, y, phaseTapX, y, CadStroke.Normal);
            }
            else
            {
                _scene.Line(ctX - 28, y, phaseTapX, y, CadStroke.Normal);
            }

            _scene.Dot(phaseTapX, y);

            if (Node("hv-surge") is { } hvSurge)
            {
                var surgeX = qsX + 54;
                _scene.Dot(surgeX, y);
                _scene.Line(surgeX, y, surgeX, y + 24, CadStroke.Thin);
                _symbols.SurgeArrester(surgeX, y + 24, hvSurge.Designation, "");
            }
        }

        private void DrawRunnInputDevice(double x, double y, DiagramNode device)
        {
            _symbols.DrawDevice(device, x, y);
            _scene.Text(device.Title, x - 30, y + 31, 10, 84);
        }

        private void DrawBus(double x, double top, double bottom)
        {
            var bus = Node("busbar-runn");
            var label = bus is null ? "" : $"Шины РУНН: {Safe(bus.DeviceType)}";
            _symbols.Busbar(x, top, bottom, label);
        }

        private void DrawMeteringBox(double x, double y)
        {
            _scene.Rect(x - 52, y - 14, 168, 104, CadStroke.Thin, dashed: true);
            _scene.Text("Учет", x - 42, y - 8, 11, 70);

            if (Node("input-meter") is { } meter)
            {
                _symbols.Meter(x + 32, y + 42, meter.Designation, "");
                _scene.Text("1Wh", x + 18, y + 72, 10, 44);
                _scene.Line(x + 54, y + 42, x + 116, y + 42, CadStroke.Dashed);
                _scene.Text("цепи учета", x + 66, y + 56, 9.5, 86);
            }
            else
            {
                _scene.Text("ТТ без счетчика", x - 34, y + 38, 11, 120);
            }
        }

        private void DrawInputMeteringLink(double ctX, double ctY)
        {
            var dropX = ctX + 26;
            var bendY = ctY + 74;
            _scene.Line(dropX, ctY + 22, dropX, bendY, CadStroke.Dashed);
            _scene.Line(dropX, bendY, ctX + 82, ctY + 100, CadStroke.Dashed);
            _scene.Text("цепи ТТ", dropX + 8, bendY - 12, 9.5, 56);
        }

        private void DrawReserveFeeder(double busX, double y)
        {
            var phaseX = busX + 28;
            _scene.Dot(phaseX, y);
            _scene.Line(phaseX, y, phaseX + 86, y, CadStroke.Thin);
            _scene.Text("Резерв отходящих линий", phaseX + 104, y - 8, 12, 200);
        }

        private void DrawFeeder(double busX, double y, DiagramNode feeder)
        {
            var number = FeederNumber(feeder);
            var phaseX = busX + 28;
            var neutralX = busX + 58;
            var breakerX = busX + 138;
            var ct = Node($"feeder-{number}-ct");
            var output = Node($"feeder-{number}-output");
            var hasMetering = ct is not null;
            var ctX = busX + 242;
            var cableX = hasMetering ? busX + 318 : busX + 268;
            var arrowX = hasMetering ? busX + 456 : busX + 406;

            _scene.Dot(phaseX, y);
            _scene.Line(phaseX, y, breakerX - 42, y, CadStroke.Normal);
            _symbols.DrawDevice(feeder, breakerX, y);

            if (ct is not null)
            {
                _scene.Line(breakerX + 42, y, ctX - 23, y, CadStroke.Normal);
                _symbols.CurrentTransformerSingleLine(ctX, y, ct.Designation, ct.Nominal);
                _scene.Line(ctX + 23, y, arrowX - 20, y, CadStroke.Normal);
            }
            else
            {
                _scene.Line(breakerX + 42, y, arrowX - 20, y, CadStroke.Normal);
            }

            _symbols.CableSlashes(cableX, y);
            _symbols.ArrowRight(arrowX, y);

            _scene.Dot(neutralX, y + 26, 3);
            _scene.Line(neutralX, y + 26, arrowX - 18, y + 26, CadStroke.Dashed);

            _scene.Text($"Фидер №{number}", cableX + 34, y - 48, 12, 116, bold: true);
            if (ct is not null && !string.IsNullOrWhiteSpace(ct.Nominal) && ct.Nominal != "-")
                _scene.Text($"ТТ {ct.Nominal}", cableX + 34, y - 30, 10, 116);
            if (output is not null && !string.IsNullOrWhiteSpace(output.DeviceType) && output.DeviceType != "-")
                _scene.Text(output.DeviceType, cableX + 34, y + 14, 9.5, 116);
        }

        private void DrawAuxiliaryBranch(double busX, double y)
        {
            var cabinet = Node("aux-cabinet");
            if (cabinet is null)
                return;

            var phaseX = busX + 28;
            var neutralX = busX + 58;
            var breakerX = busX + 138;
            var cabinetX = busX + 240;
            var cabinetY = y - 54;
            var cabinetW = 180.0;
            var cabinetH = 116.0;
            var breaker = Node("aux-main-breaker");

            _scene.Dot(phaseX, y);
            _scene.Line(phaseX, y, breakerX - 42, y, CadStroke.Normal);
            if (breaker is not null)
                _symbols.CircuitBreaker(breakerX, y, breaker.Designation, breaker.Nominal);
            else
                _scene.Line(breakerX - 42, y, breakerX + 42, y, CadStroke.Normal);

            _scene.Line(breakerX + 42, y, cabinetX, y, CadStroke.Normal);
            _symbols.CableSlashes(breakerX + 58, y);
            _symbols.AuxiliaryCabinet(cabinetX, cabinetY, cabinetW, cabinetH, cabinet, AuxiliaryLabels());

            _scene.Dot(neutralX, y + 28, 3);
            _scene.Line(neutralX, y + 28, cabinetX, y + 28, CadStroke.Dashed);
        }

        private void DrawLvSurge(double busX, double y, DiagramNode surge)
        {
            var phaseX = busX + 28;
            var surgeX = busX - 76;
            _scene.Dot(phaseX, y);
            _scene.Line(phaseX, y, surgeX, y, CadStroke.Thin);
            _symbols.SurgeArrester(surgeX, y, surge.Designation, "0,4 кВ");
        }

        private IReadOnlyList<DiagramNode> LvInputDevices() =>
            _sheet.Nodes
                .Where(n => n.Kind == DiagramNodeKind.LvInputDevice)
                .ToList();

        private IReadOnlyList<DiagramNode> FeederDevices() =>
            _sheet.Nodes
                .Where(n => n.Kind == DiagramNodeKind.OutgoingFeederDevice)
                .OrderBy(FeederNumber)
                .ToList();

        private IReadOnlyList<string> AuxiliaryLabels()
        {
            var labels = new List<string>();

            if (Node("aux-lighting") is { } lighting)
            {
                var control = Label(lighting, "Control");
                labels.Add(string.IsNullOrWhiteSpace(control)
                    ? $"{lighting.Designation} освещение"
                    : $"{lighting.Designation} {control}");
            }

            if (Node("aux-socket") is { } socket)
                labels.Add($"{socket.Designation} розетка");
            if (Node("aux-heating") is { } heating)
                labels.Add($"{heating.Designation} обогрев");
            if (Node("aux-ventilation") is { } ventilation)
                labels.Add($"{ventilation.Designation} вентиляция");
            if (Node("aux-rise") is { } rise)
                labels.Add($"{rise.Designation} {Label(rise, "Type")}".Trim());

            return labels.Take(4).ToList();
        }

        private DiagramNode? Node(string id) =>
            _nodes.TryGetValue(id, out var node) ? node : null;

        private static int FeederNumber(DiagramNode node) =>
            node.Metadata.TryGetValue("FeederNumber", out var value) && int.TryParse(value, out var parsed)
                ? parsed
                : 0;

        private static string Label(DiagramNode node, string role) =>
            node.Labels.FirstOrDefault(l => l.Role.Equals(role, StringComparison.OrdinalIgnoreCase))?.Text ?? "";

        private static string Safe(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private sealed class MeteringLayout
    {
        private readonly CadScene _scene;
        private readonly DiagramSheet _sheet;
        private readonly DiagramSymbolLibrary _symbols;
        private readonly Dictionary<string, DiagramNode> _nodes;

        public MeteringLayout(CadScene scene, DiagramSheet sheet)
        {
            _scene = scene;
            _sheet = sheet;
            _symbols = new DiagramSymbolLibrary(scene);
            _nodes = sheet.Nodes
                .Where(n => !string.IsNullOrWhiteSpace(n.Id))
                .GroupBy(n => n.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }

        public void Draw()
        {
            const double left = 70;
            const double top = 34;
            const double firstRowY = 178;
            const double rowStep = 142;

            _scene.Text("Цепи учета", left, top, 20, 360, bold: true);
            _scene.Text("Схема электрическая принципиальная", left, top + 28, 12, 360);

            DrawColumnHeaders(firstRowY - 72);

            var rows = MeteringRows().ToList();
            if (rows.Count == 0)
            {
                _scene.Text("Цепи учета не заданы", left, firstRowY, 14, 360);
                return;
            }

            for (var i = 0; i < rows.Count; i++)
                DrawMeteringRow(rows[i], firstRowY + i * rowStep);
        }

        private void DrawColumnHeaders(double y)
        {
            _scene.Text("Присоединение", 72, y, 11, 180, bold: true);
            _scene.Text("Трансформаторы тока", 280, y, 11, 200, bold: true);
            _scene.Text("Клеммник", 575, y, 11, 120, bold: true);
            _scene.Text("Счетчик", 785, y, 11, 140, bold: true);
            _scene.Text("Примечание", 1040, y, 11, 220, bold: true);
            _scene.Line(70, y + 26, 1320, y + 26, CadStroke.Thin);
        }

        private void DrawMeteringRow(MeteringRow row, double y)
        {
            _scene.Text(row.Title, 72, y - 22, 12, 180, bold: true);
            _scene.Text(row.Ct.Nominal, 72, y - 4, 10, 180);

            _scene.Line(228, y, 320, y, CadStroke.Normal);
            _symbols.CurrentTransformerSet(320, y, row.Ct.Designation, row.Ct.Nominal);
            _scene.Line(374, y, 548, y, CadStroke.Dashed);

            DrawTerminalBlock(548, y - 24, row.Terminal.Designation);
            _scene.Line(668, y, 772, y, CadStroke.Dashed);

            if (row.Meter is not null)
            {
                _symbols.Meter(810, y, row.Meter.Designation, "");
                _scene.Line(829, y, 930, y, CadStroke.Dashed);
                _scene.Text("1Wh", 794, y + 30, 10, 52);
            }
            else
            {
                _scene.Text("резерв под счетчик", 785, y - 7, 11, 150);
            }

            _scene.Text("вторичные цепи ТТ", 1040, y - 9, 10, 180);
            _scene.Line(70, y + 62, 1320, y + 62, CadStroke.Hair);
        }

        private void DrawTerminalBlock(double x, double y, string designation)
        {
            const int cells = 6;
            const double cellWidth = 18;
            const double height = 48;
            var width = cells * cellWidth;

            _scene.Rect(x, y, width, height, CadStroke.Thin);
            for (var i = 1; i < cells; i++)
                _scene.Line(x + i * cellWidth, y, x + i * cellWidth, y + height, CadStroke.Thin);
            for (var i = 0; i < cells; i++)
            {
                var cx = x + i * cellWidth + cellWidth / 2;
                _scene.Dot(cx, y + 13, 2.7);
                _scene.Dot(cx, y + height - 13, 2.7);
            }

            _scene.Text(designation, x + 4, y - 28, 11, 80, bold: true);
        }

        private IEnumerable<MeteringRow> MeteringRows()
        {
            if (Node("metering-input-ct") is { } inputCt && Node("metering-input-terminal") is { } inputTerminal)
            {
                yield return new MeteringRow(
                    "Ввод РУНН",
                    inputCt,
                    inputTerminal,
                    Node("metering-input-meter"));
            }

            foreach (var ct in _sheet.Nodes
                         .Where(n => n.Id.StartsWith("metering-feeder-", StringComparison.OrdinalIgnoreCase)
                                     && n.Id.EndsWith("-ct", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(FeederNumber))
            {
                var number = FeederNumber(ct);
                var terminal = Node($"metering-feeder-{number}-terminal");
                if (terminal is null)
                    continue;

                yield return new MeteringRow(
                    $"Фидер №{number}",
                    ct,
                    terminal,
                    Node($"metering-feeder-{number}-meter"));
            }
        }

        private DiagramNode? Node(string id) =>
            _nodes.TryGetValue(id, out var node) ? node : null;

        private static int FeederNumber(DiagramNode node)
        {
            var parts = node.Id.Split('-', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Equals("feeder", StringComparison.OrdinalIgnoreCase)
                    && i + 1 < parts.Length
                    && int.TryParse(parts[i + 1], out var number))
                {
                    return number;
                }
            }

            return 0;
        }

        private sealed record MeteringRow(
            string Title,
            DiagramNode Ct,
            DiagramNode Terminal,
            DiagramNode? Meter);
    }
}
