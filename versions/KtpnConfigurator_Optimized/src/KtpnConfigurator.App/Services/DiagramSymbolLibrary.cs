using System.Windows;
using System.Windows.Media;
using KtpnConfigurator.Core.Diagrams;

namespace KtpnConfigurator.App.Services;

internal sealed class DiagramSymbolLibrary
{
    private readonly CadScene _scene;

    public DiagramSymbolLibrary(CadScene scene)
    {
        _scene = scene;
    }

    public void DrawDevice(DiagramNode node, double x, double y)
    {
        switch (node.SymbolKey)
        {
            case "disconnector":
                Disconnector(x, y, node.Designation, node.Nominal);
                break;
            case "switchDisconnectFuse":
                Fuse(x, y, node.Designation, node.Nominal);
                break;
            case "fuse":
                Fuse(x, y, node.Designation, node.Nominal);
                break;
            case "currentTransformer":
                CurrentTransformerSet(x, y, node.Designation, node.Nominal);
                break;
            case "meter":
                Meter(x, y, node.Designation, "");
                break;
            case "powerTransformer":
                PowerTransformerHorizontal(x, y, node.Designation, Label(node, "Voltage"), node.Title);
                break;
            case "surgeArrester":
                SurgeArrester(x, y, node.Designation, node.Nominal);
                break;
            default:
                CircuitBreaker(x, y, node.Designation, node.Nominal);
                break;
        }
    }

    public void ArrowLeft(double x, double y)
    {
        _scene.Line(x, y, x + 20, y - 9, CadStroke.Normal);
        _scene.Line(x, y, x + 20, y + 9, CadStroke.Normal);
        _scene.Line(x + 20, y - 9, x + 20, y + 9, CadStroke.Normal);
    }

    public void ArrowRight(double x, double y)
    {
        _scene.Line(x, y, x - 20, y - 9, CadStroke.Normal);
        _scene.Line(x, y, x - 20, y + 9, CadStroke.Normal);
        _scene.Line(x - 20, y - 9, x - 20, y + 9, CadStroke.Normal);
    }

    public void CableSlashes(double x, double y)
    {
        for (var i = 0; i < 3; i++)
        {
            var dx = i * 9;
            _scene.Line(x + dx - 5, y + 9, x + dx + 5, y - 9);
        }
    }

    public void Disconnector(double x, double y, string designator, string label)
    {
        _scene.Line(x + 38, y, x + 14, y, CadStroke.Normal);
        _scene.Line(x - 14, y, x - 38, y, CadStroke.Normal);
        _scene.Ellipse(x + 14, y, 3.5, 3.5, CadStroke.Normal, Brushes.White);
        _scene.Ellipse(x - 14, y, 3.5, 3.5, CadStroke.Normal, Brushes.White);
        _scene.Line(x - 12, y - 1, x + 12, y - 22, CadStroke.Normal);
        _scene.Text(designator, x - 18, y - 48, 12, 52);
        TextIfPresent(label, x - 25, y + 10, 11, 72);
    }

    public void CircuitBreaker(double x, double y, string designator, string label)
    {
        _scene.Line(x + 42, y, x + 12, y, CadStroke.Normal);
        _scene.Line(x - 12, y, x - 42, y, CadStroke.Normal);
        _scene.Ellipse(x + 12, y, 3, 3, CadStroke.Normal, Brushes.White);
        _scene.Ellipse(x - 12, y, 2.5, 2.5, CadStroke.Normal, Brushes.White);
        _scene.Line(x - 12, y, x + 10, y - 24, CadStroke.Normal);
        _scene.Text(designator, x - 13, y - 50, 12, 52);
        TextIfPresent(label, x - 18, y - 32, 11, 74);
    }

    public void Fuse(double x, double y, string designator, string label)
    {
        _scene.Line(x + 36, y, x + 14, y, CadStroke.Normal);
        _scene.Line(x - 14, y, x - 36, y, CadStroke.Normal);
        _scene.FilledRect(x - 14, y - 10, 28, 20, CadStroke.Normal);
        _scene.Line(x - 9, y + 8, x + 9, y - 8, CadStroke.Normal);
        _scene.Text(designator, x - 18, y - 48, 12, 70);
        TextIfPresent(label, x - 20, y + 12, 11, 80);
    }

    public void PowerTransformerHorizontal(double x, double y, string designator, string voltage, string mark)
    {
        _scene.Ellipse(x + 18, y, 24, 24, CadStroke.Normal, Brushes.White);
        _scene.Ellipse(x - 18, y, 24, 24, CadStroke.Normal, Brushes.White);
        _scene.Text("Y", x - 26, y - 9, 10, 18);
        _scene.Polyline(new[]
        {
            new Point(x + 18, y - 11),
            new Point(x + 8, y + 8),
            new Point(x + 28, y + 8),
        }, CadStroke.Thin, closed: true);
        _scene.Text(designator, x + 32, y - 44, 12, 42);
        TextIfPresent(voltage, x - 40, y + 36, 11, 120);
        TextIfPresent(mark, x - 52, y + 53, 10, 150);
    }

    public void Busbar(double x, double top, double bottom, string label)
    {
        var phase = new[] { x, x + 14, x + 28 };
        foreach (var px in phase)
            _scene.Line(px, top, px, bottom, CadStroke.Bus);

        var neutralX = x + 58;
        var peX = x + 80;
        _scene.Line(neutralX, top, neutralX, bottom, CadStroke.Normal);
        _scene.Line(peX, top, peX, bottom - 22, CadStroke.Thin);
        Ground(peX, bottom - 22);

        _scene.Text("A", x - 3, top - 27, 12, 18, bold: true);
        _scene.Text("B", x + 11, top - 27, 12, 18, bold: true);
        _scene.Text("C", x + 25, top - 27, 12, 18, bold: true);
        _scene.Text("N", neutralX - 5, top - 27, 12, 20, bold: true);
        _scene.Text("PE", peX - 9, top - 27, 12, 28, bold: true);
        TextIfPresent(label, x - 56, top - 52, 11, 230);
    }

    public void CurrentTransformerSet(double x, double y, string designator, string ratio)
    {
        _scene.Line(x - 18, y, x + 54, y, CadStroke.Thin);
        for (var i = 0; i < 3; i++)
            _scene.Ellipse(x + i * 18, y, 10, 18, CadStroke.Normal, Brushes.White);
        _scene.Line(x - 18, y, x + 54, y, CadStroke.Thin);
        _scene.Text(designator, x - 8, y - 46, 11, 78);
        TextIfPresent(ratio, x - 4, y + 28, 10, 64);
    }

    public void CurrentTransformerSingleLine(double x, double y, string designator, string ratio)
    {
        _scene.Line(x - 23, y, x + 23, y, CadStroke.Thin);
        _scene.Ellipse(x, y, 18, 11, CadStroke.Normal, Brushes.White);
        _scene.Ellipse(x, y, 10, 5.5, CadStroke.Thin);
        _scene.Line(x - 23, y, x + 23, y, CadStroke.Thin);
        _scene.Text(designator, x - 30, y - 34, 10.5, 90, bold: true);
        TextIfPresent(ratio, x - 22, y + 17, 9.5, 72);
    }

    public void Meter(double x, double y, string designator, string text)
    {
        _scene.Ellipse(x, y, 19, 19, CadStroke.Normal, Brushes.White);
        _scene.Text(designator, x - 10, y - 9, 11, 40);
        TextIfPresent(text, x + 27, y - 9, 12, 50);
    }

    public void SurgeArrester(double x, double y, string designator, string label)
    {
        _scene.Line(x, y, x, y + 30);
        _scene.FilledRect(x - 10, y + 30, 20, 34);
        _scene.Line(x - 9, y + 60, x + 9, y + 34);
        _scene.Line(x, y + 64, x, y + 92);
        Ground(x, y + 92);
        _scene.Text(designator, x + 15, y + 18, 10, 70);
        TextIfPresent(label, x + 15, y + 34, 10, 70);
    }

    public void AuxiliaryCabinet(double x, double y, double width, double height, DiagramNode cabinet, IReadOnlyList<string> labels)
    {
        _scene.Rect(x, y, width, height, CadStroke.Thin, dashed: true);
        _scene.Text("ЩСН", x + 16, y + 15, 18, 72, bold: true);
        TextIfPresent(cabinet.Model, x + 74, y + 20, 10, 92);

        var innerY = y + 56;
        if (labels.Count == 0)
            _scene.Text("Собственные нужды", x + 18, innerY, 10, width - 32);
        else
            for (var i = 0; i < labels.Count; i++)
                _scene.Text(labels[i], x + 18, innerY + i * 16, 10, width - 32);
    }

    public void Ground(double x, double y)
    {
        _scene.Line(x - 13, y, x + 13, y);
        _scene.Line(x - 9, y + 6, x + 9, y + 6);
        _scene.Line(x - 5, y + 12, x + 5, y + 12);
    }

    private static string Label(DiagramNode node, string role) =>
        node.Labels.FirstOrDefault(l => l.Role.Equals(role, StringComparison.OrdinalIgnoreCase))?.Text ?? "";

    private void TextIfPresent(string? text, double x, double y, double size, double width)
    {
        if (!string.IsNullOrWhiteSpace(text) && text != "-")
            _scene.Text(text, x, y, size, width);
    }
}
