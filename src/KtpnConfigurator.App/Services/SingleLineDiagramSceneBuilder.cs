using System.Windows;
using System.Windows.Media;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.App.Services;

internal static class SingleLineDiagramSceneBuilder
{
    public static CadScene Build(ProjectConfig cfg, CalculationResult res, Size size)
    {
        var scene = new CadScene(size);
        var builder = new Builder(scene, cfg, res);
        builder.Draw();
        return scene;
    }

    private sealed class Builder
    {
        private readonly CadScene _s;
        private readonly ProjectConfig _cfg;
        private readonly CalculationResult _res;

        public Builder(CadScene scene, ProjectConfig cfg, CalculationResult res)
        {
            _s = scene;
            _cfg = cfg;
            _res = res;
        }

        public void Draw()
        {
            DrawCleanSchematic();
        }

        private void DrawCleanSchematic()
        {
            var feeders = (_cfg.OutgoingFeeders ?? new List<OutgoingFeederConfig>())
                .OrderBy(f => f.Number)
                .ThenBy(f => f.DeviceType)
                .ToList();

            var hasAux = _cfg.AuxiliaryNeeds?.HasAuxiliaryCabinet == true;
            var branchCount = Math.Max(1, feeders.Count) + (hasAux ? 1 : 0);

            var left = 70.0;
            var inputY = 165.0;
            var busX = 900.0;
            var busTop = 86.0;
            var firstBranchY = 292.0;
            var branchStep = 126.0;
            var lastBranchY = firstBranchY + (branchCount - 1) * branchStep;
            var busBottom = Math.Min(_s.Size.Height - 82, Math.Max(lastBranchY + 74, inputY + 280));

            _s.Text("Однолинейная схема КТПН", left, 28, 20, 360, bold: true);
            _s.Text(Safe(_cfg.Mark), left, 55, 12, 380);

            DrawCleanIncomingPath(left, inputY, busX);
            DrawCleanBus(busX, busTop, busBottom);

            if (_cfg.HasCt || _cfg.HasMeter)
                DrawCleanMetering(busX - 198, inputY + 58);

            var row = 0;
            if (feeders.Count == 0)
            {
                DrawCleanReserveFeeder(busX, firstBranchY);
                row++;
            }
            else
            {
                var nextQf = 2;
                var nextQs = 4;
                for (var i = 0; i < feeders.Count; i++)
                {
                    var designator = NextFeederDesignator(feeders[i], ref nextQf, ref nextQs);
                    DrawCleanFeeder(busX, firstBranchY + row * branchStep, feeders[i], i, designator);
                    row++;
                }
            }

            if (hasAux)
                DrawCleanAuxiliaryBranch(busX, firstBranchY + row * branchStep);

            if (_cfg.RunnSurgeArrester)
                DrawCleanLvSurge(busX, busBottom - 74);
        }

        private void DrawCleanIncomingPath(double left, double y, double busX)
        {
            var qsX = left + 120;
            var fuX = left + 240;
            var transformerX = left + 386;
            var phaseTapX = busX + 28;

            ArrowRight(left - 28, y);
            _s.Text($"Ввод {Safe(_cfg.Voltage)}", left, y - 44, 12, 120);
            _s.Line(left - 8, y, qsX - 38, y, CadStroke.Normal);

            if (HasDevice(_cfg.RuvnSwitch))
                Disconnector(qsX, y, "QS1", CurrentA(_cfg.RuvnSwitchNominal));
            else
                _s.Line(qsX - 38, y, qsX + 38, y, CadStroke.Normal);

            _s.Line(qsX + 38, y, fuX - 36, y, CadStroke.Normal);
            if (HasDevice(_cfg.FuseType))
                Fuse(fuX, y, "FU1-FU3", CurrentText(_cfg.FuseNominal));
            else
                _s.Line(fuX - 36, y, fuX + 36, y, CadStroke.Normal);

            _s.Line(fuX + 36, y, transformerX - 58, y, CadStroke.Normal);
            TransformerHorizontal(transformerX, y);

            var wireX = transformerX + 58;
            var deviceX = transformerX + 92;
            var inputDevices = RunnInputDevices().ToList();
            if (inputDevices.Count == 0)
                inputDevices.Add(new InputDevice("QF1", "Резерв ввода", "АВ", InputNominalOrFallback()));

            foreach (var device in inputDevices)
            {
                _s.Line(wireX, y, deviceX - 38, y, CadStroke.Normal);
                DrawRunnInputDevice(deviceX, y, device);
                wireX = deviceX + 42;
                deviceX += 96;
            }

            var ctX = deviceX + 8;
            _s.Line(wireX, y, ctX - 28, y, CadStroke.Normal);

            if (_cfg.HasCt)
            {
                CurrentTransformers(ctX, y, "TA1-TA3", CtRatioOrFallback());
                if (_cfg.HasMeter)
                    DrawInputMeteringLink(ctX, y);
                _s.Line(ctX + 48, y, phaseTapX, y, CadStroke.Normal);
            }
            else
            {
                _s.Line(ctX - 28, y, phaseTapX, y, CadStroke.Normal);
            }

            _s.Dot(phaseTapX, y);

            if (_cfg.RuvnSurgeArrester)
            {
                var surgeX = qsX + 54;
                _s.Dot(surgeX, y);
                _s.Line(surgeX, y, surgeX, y + 24, CadStroke.Thin);
                SurgeArrester(surgeX, y + 24, "FV1-FV3", "");
            }
        }

        private IEnumerable<InputDevice> RunnInputDevices()
        {
            if (_cfg.PvrOn)
                yield return new InputDevice("QS2", "ПВР/NH", "ПВР", _cfg.PvrNominal);
            if (_cfg.ReOn)
                yield return new InputDevice("QS3", "РЕ", "РЕ", _cfg.ReNominal);
            if (_cfg.AvInOn)
                yield return new InputDevice("QF1", "Вводной АВ", "АВ", _cfg.AvInNominal);
        }

        private void DrawRunnInputDevice(double x, double y, InputDevice device)
        {
            switch (device.Kind)
            {
                case "ПВР":
                    Fuse(x, y, device.Designation, CurrentA(device.Nominal));
                    break;
                case "РЕ":
                    Disconnector(x, y, device.Designation, CurrentA(device.Nominal));
                    break;
                default:
                    CircuitBreaker(x, y, device.Designation, CurrentA(device.Nominal));
                    break;
            }

            _s.Text(device.Caption, x - 30, y + 31, 10, 84);
        }

        private void DrawCleanBus(double x, double top, double bottom)
        {
            var phase = new[] { x, x + 14, x + 28 };
            foreach (var px in phase)
                _s.Line(px, top, px, bottom, CadStroke.Bus);

            var neutralX = x + 58;
            var peX = x + 80;
            _s.Line(neutralX, top, neutralX, bottom, CadStroke.Normal);
            _s.Line(peX, top, peX, bottom - 22, CadStroke.Thin);
            Ground(peX, bottom - 22);

            _s.Text("A", x - 3, top - 27, 12, 18, bold: true);
            _s.Text("B", x + 11, top - 27, 12, 18, bold: true);
            _s.Text("C", x + 25, top - 27, 12, 18, bold: true);
            _s.Text("N", neutralX - 5, top - 27, 12, 20, bold: true);
            _s.Text("PE", peX - 9, top - 27, 12, 28, bold: true);
            _s.Text($"Шины РУНН: {Safe(_res.BusbarLv)}", x - 56, top - 52, 11, 230);
        }

        private void DrawCleanMetering(double x, double y)
        {
            _s.Rect(x - 52, y - 14, 168, 104, CadStroke.Thin, dashed: true);
            _s.Text("Учет", x - 42, y - 8, 11, 70);

            if (_cfg.HasMeter)
            {
                Meter(x + 32, y + 42, "PI1", "");
                _s.Text("1Wh", x + 18, y + 72, 10, 44);
                _s.Line(x + 54, y + 42, x + 116, y + 42, CadStroke.Dashed);
                _s.Text("цепи учета", x + 66, y + 56, 9.5, 86);
            }
            else
            {
                _s.Text("ТТ без счетчика", x - 34, y + 38, 11, 120);
            }
        }

        private void DrawInputMeteringLink(double ctX, double ctY)
        {
            var dropX = ctX + 26;
            var bendY = ctY + 74;
            _s.Line(dropX, ctY + 22, dropX, bendY, CadStroke.Dashed);
            _s.Line(dropX, bendY, ctX + 82, ctY + 100, CadStroke.Dashed);
            _s.Text("цепи ТТ", dropX + 8, bendY - 12, 9.5, 56);
        }

        private void DrawCleanReserveFeeder(double busX, double y)
        {
            var phaseX = busX + 28;
            _s.Dot(phaseX, y);
            _s.Line(phaseX, y, phaseX + 86, y, CadStroke.Thin);
            _s.Text("Резерв отходящих линий", phaseX + 104, y - 8, 12, 200);
        }

        private void DrawCleanFeeder(double busX, double y, OutgoingFeederConfig feeder, int index, string designator)
        {
            var phaseX = busX + 28;
            var neutralX = busX + 58;
            var breakerX = busX + 138;
            var hasMetering = feeder.HasMeter;
            var ctX = busX + 242;
            var cableX = hasMetering ? busX + 318 : busX + 268;
            var arrowX = hasMetering ? busX + 456 : busX + 406;
            var current = feeder.Nominal > 0 ? CurrentA(feeder.Nominal) : "";

            _s.Dot(phaseX, y);
            _s.Line(phaseX, y, breakerX - 42, y, CadStroke.Normal);
            DrawFeederDevice(breakerX, y, feeder, designator, current);
            if (hasMetering)
            {
                var taStart = 4 + index * 3;
                var ratio = string.IsNullOrWhiteSpace(feeder.TtRatio) ? "ТТ" : Safe(feeder.TtRatio);
                _s.Line(breakerX + 42, y, ctX - 23, y, CadStroke.Normal);
                CurrentTransformerSingleLine(ctX, y, $"TA{taStart}-TA{taStart + 2}", ratio);
                _s.Line(ctX + 23, y, arrowX - 20, y, CadStroke.Normal);
            }
            else
            {
                _s.Line(breakerX + 42, y, arrowX - 20, y, CadStroke.Normal);
            }

            CableSlashes(cableX, y);
            ArrowRight(arrowX, y);

            _s.Dot(neutralX, y + 26, 3);
            _s.Line(neutralX, y + 26, arrowX - 18, y + 26, CadStroke.Dashed);

            _s.Text($"Фидер №{feeder.Number}", cableX + 34, y - 48, 12, 116, bold: true);
            if (feeder.HasMeter && !string.IsNullOrWhiteSpace(feeder.TtRatio))
                _s.Text($"ТТ {feeder.TtRatio}", cableX + 34, y - 30, 10, 116);
        }

        private void DrawFeederDevice(double x, double y, OutgoingFeederConfig feeder, string designator, string current)
        {
            var deviceType = Safe(feeder.DeviceType);
            if (deviceType.Contains("РПС", StringComparison.OrdinalIgnoreCase)
                || deviceType.Contains("ПВР", StringComparison.OrdinalIgnoreCase)
                || deviceType.Contains("QS", StringComparison.OrdinalIgnoreCase))
            {
                Disconnector(x, y, designator, current);
                return;
            }

            CircuitBreaker(x, y, designator, current);
        }

        private static string NextFeederDesignator(OutgoingFeederConfig feeder, ref int nextQf, ref int nextQs)
        {
            if (IsCircuitBreakerType(feeder.DeviceType))
                return $"QF{nextQf++}";

            return $"QS{nextQs++}";
        }

        private static bool IsCircuitBreakerType(string deviceType) =>
            deviceType.Equals("АВ", StringComparison.OrdinalIgnoreCase)
            || deviceType.Contains("автомат", StringComparison.OrdinalIgnoreCase)
            || deviceType.Equals("QF", StringComparison.OrdinalIgnoreCase);

        private void DrawCleanAuxiliaryBranch(double busX, double y)
        {
            var aux = _cfg.AuxiliaryNeeds ?? new AuxiliaryNeedsConfig();
            var phaseX = busX + 28;
            var neutralX = busX + 58;
            var breakerX = busX + 138;
            var cabinetX = busX + 240;
            var cabinetY = y - 54;
            var cabinetW = 180.0;
            var cabinetH = 116.0;

            _s.Dot(phaseX, y);
            _s.Line(phaseX, y, breakerX - 42, y, CadStroke.Normal);
            CircuitBreaker(breakerX, y, "QF20", aux.MainBreakerNominal > 0 ? CurrentA(aux.MainBreakerNominal) : "");
            _s.Line(breakerX + 42, y, cabinetX, y, CadStroke.Normal);
            CableSlashes(breakerX + 58, y);

            _s.Rect(cabinetX, cabinetY, cabinetW, cabinetH, CadStroke.Thin, dashed: true);
            _s.Text("ЩСН", cabinetX + 16, cabinetY + 15, 18, 72, bold: true);
            _s.Text(Safe(aux.CabinetModel), cabinetX + 74, cabinetY + 20, 10, 92);

            var innerY = cabinetY + 56;
            var labels = AuxiliaryLabels(aux).Take(4).ToList();
            if (labels.Count == 0)
                labels.Add("Собственные нужды");

            for (var i = 0; i < labels.Count; i++)
                _s.Text(labels[i], cabinetX + 18, innerY + i * 16, 10, cabinetW - 32);

            _s.Dot(neutralX, y + 28, 3);
            _s.Line(neutralX, y + 28, cabinetX, y + 28, CadStroke.Dashed);
        }

        private void DrawCleanLvSurge(double busX, double y)
        {
            var phaseX = busX + 28;
            var surgeX = busX - 76;
            _s.Dot(phaseX, y);
            _s.Line(phaseX, y, surgeX, y, CadStroke.Thin);
            SurgeArrester(surgeX, y, "FV4-FV6", "0,4 кВ");
        }

        private static IEnumerable<string> AuxiliaryLabels(AuxiliaryNeedsConfig aux)
        {
            if (aux.HasLighting)
                yield return $"EL1 {Safe(aux.LightingControlMode)}";
            if (aux.SocketEnabled)
                yield return "XS1 розетка";
            if (aux.HeatingEnabled)
                yield return "EH1 обогрев";
            if (aux.VentilationEnabled)
                yield return "M1 вентиляция";
            if (aux.HasRise)
                yield return $"РИСЭ {Safe(aux.RieseType)}";
        }

        private void ArrowRight(double x, double y)
        {
            _s.Line(x, y, x + 20, y - 9, CadStroke.Normal);
            _s.Line(x, y, x + 20, y + 9, CadStroke.Normal);
            _s.Line(x + 20, y - 9, x + 20, y + 9, CadStroke.Normal);
        }

        private void CableSlashes(double x, double y)
        {
            for (var i = 0; i < 3; i++)
            {
                var dx = i * 9;
                _s.Line(x + dx - 5, y + 9, x + dx + 5, y - 9);
            }
        }

        private void Disconnector(double x, double y, string designator, string label)
        {
            _s.Line(x + 38, y, x + 14, y, CadStroke.Normal);
            _s.Line(x - 14, y, x - 38, y, CadStroke.Normal);
            _s.Ellipse(x + 14, y, 3.5, 3.5, CadStroke.Normal, Brushes.White);
            _s.Ellipse(x - 14, y, 3.5, 3.5, CadStroke.Normal, Brushes.White);
            _s.Line(x - 12, y - 1, x + 12, y - 22, CadStroke.Normal);
            _s.Text(designator, x - 18, y - 48, 12, 52);
            if (!string.IsNullOrWhiteSpace(label) && label != "-")
                _s.Text(label, x - 25, y + 10, 11, 72);
        }

        private void CircuitBreaker(double x, double y, string designator, string label)
        {
            _s.Line(x + 42, y, x + 12, y, CadStroke.Normal);
            _s.Line(x - 12, y, x - 42, y, CadStroke.Normal);
            _s.Ellipse(x + 12, y, 3, 3, CadStroke.Normal, Brushes.White);
            _s.Ellipse(x - 12, y, 2.5, 2.5, CadStroke.Normal, Brushes.White);
            _s.Line(x - 12, y, x + 10, y - 24, CadStroke.Normal);
            _s.Text(designator, x - 13, y - 50, 12, 52);
            if (!string.IsNullOrWhiteSpace(label) && label != "-")
                _s.Text(label, x - 18, y - 32, 11, 74);
        }

        private void Fuse(double x, double y, string designator, string label)
        {
            _s.Line(x + 36, y, x + 14, y, CadStroke.Normal);
            _s.Line(x - 14, y, x - 36, y, CadStroke.Normal);
            _s.FilledRect(x - 14, y - 10, 28, 20, CadStroke.Normal);
            _s.Line(x - 9, y + 8, x + 9, y - 8, CadStroke.Normal);
            _s.Text(designator, x - 18, y - 48, 12, 70);
            _s.Text(label, x - 20, y + 12, 11, 80);
        }

        private void TransformerHorizontal(double x, double y)
        {
            _s.Ellipse(x + 18, y, 24, 24, CadStroke.Normal, Brushes.White);
            _s.Ellipse(x - 18, y, 24, 24, CadStroke.Normal, Brushes.White);
            _s.Text("Y", x - 26, y - 9, 10, 18);
            _s.Polyline(new[] { new Point(x + 18, y - 11), new Point(x + 8, y + 8), new Point(x + 28, y + 8) }, CadStroke.Thin, closed: true);
            _s.Text("T1", x + 32, y - 44, 12, 42);
            _s.Text($"{Safe(_cfg.Voltage)}/0,4 кВ", x - 40, y + 36, 11, 120);
            _s.Text(Safe(_cfg.Mark), x - 52, y + 53, 10, 150);
        }

        private void CurrentTransformers(double x, double y, string designator, string ratio)
        {
            _s.Line(x - 18, y, x + 54, y, CadStroke.Thin);
            for (var i = 0; i < 3; i++)
                _s.Ellipse(x + i * 18, y, 10, 18, CadStroke.Normal, Brushes.White);
            _s.Line(x - 18, y, x + 54, y, CadStroke.Thin);
            _s.Text(designator, x - 8, y - 46, 11, 78);
            if (!string.IsNullOrWhiteSpace(ratio) && ratio != "-")
                _s.Text(ratio, x - 4, y + 28, 10, 64);
        }

        private void CurrentTransformerSingleLine(double x, double y, string designator, string ratio)
        {
            _s.Line(x - 23, y, x + 23, y, CadStroke.Thin);
            _s.Ellipse(x, y, 18, 11, CadStroke.Normal, Brushes.White);
            _s.Ellipse(x, y, 10, 5.5, CadStroke.Thin);
            _s.Line(x - 23, y, x + 23, y, CadStroke.Thin);
            _s.Text(designator, x - 30, y - 34, 10.5, 90, bold: true);
            if (!string.IsNullOrWhiteSpace(ratio) && ratio != "-")
                _s.Text(ratio, x - 22, y + 17, 9.5, 72);
        }

        private void Meter(double x, double y, string designator, string text)
        {
            _s.Ellipse(x, y, 19, 19, CadStroke.Normal, Brushes.White);
            _s.Text(designator, x - 10, y - 9, 11, 40);
            _s.Text(text, x + 27, y - 9, 12, 50);
        }

        private void SurgeArrester(double x, double y, string designator, string label)
        {
            _s.Line(x, y, x, y + 30);
            _s.FilledRect(x - 10, y + 30, 20, 34);
            _s.Line(x - 9, y + 60, x + 9, y + 34);
            _s.Line(x, y + 64, x, y + 92);
            Ground(x, y + 92);
            _s.Text(designator, x + 15, y + 18, 10, 70);
            _s.Text(label, x + 15, y + 34, 10, 70);
        }

        private void Ground(double x, double y)
        {
            _s.Line(x - 13, y, x + 13, y);
            _s.Line(x - 9, y + 6, x + 9, y + 6);
            _s.Line(x - 5, y + 12, x + 5, y + 12);
        }

        private double InputNominalOrFallback() =>
            _res.InputNominal > 0
                ? _res.InputNominal
                : Math.Max(
                    _cfg.PvrOn ? _cfg.PvrNominal : 0,
                    Math.Max(_cfg.ReOn ? _cfg.ReNominal : 0, Math.Max(_cfg.AvInOn ? _cfg.AvInNominal : 0, 250)));

        private string CtRatioOrFallback() =>
            string.IsNullOrWhiteSpace(_cfg.CtRatio) ? "300/5" : _cfg.CtRatio.Trim();

        private static bool HasDevice(string value) =>
            !string.IsNullOrWhiteSpace(value) && !value.Equals("Нет", StringComparison.OrdinalIgnoreCase);

        private static string CurrentA(double value) => value > 0 ? $"{value:0} А" : "-";

        private static string CurrentText(string? value)
        {
            var text = Safe(value);
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

        private static string Safe(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

        private sealed record InputDevice(string Designation, string Caption, string Kind, double Nominal);
    }
}
