using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace KtpnConfigurator.App.Services;

internal sealed class CadSymbolRenderer
{
    private readonly DrawingContext _dc;
    private readonly Pen _line;
    private readonly Pen _thin;
    private readonly Pen _bus;
    private readonly Pen _dash;
    private readonly Brush _surface;
    private readonly Brush _text;
    private readonly Typeface _font;
    private readonly double _pixelsPerDip;

    public CadSymbolRenderer(
        DrawingContext dc,
        Pen line,
        Pen thin,
        Pen bus,
        Pen dash,
        Brush surface,
        Brush text,
        Typeface font,
        double pixelsPerDip)
    {
        _dc = dc;
        _line = line;
        _thin = thin;
        _bus = bus;
        _dash = dash;
        _surface = surface;
        _text = text;
        _font = font;
        _pixelsPerDip = pixelsPerDip <= 0 ? 1 : pixelsPerDip;
    }

    public void Busbar(double x, double top, double bottom, string phases, string neutral, string material)
    {
        Line(x, top, x, bottom, _bus);
        Line(x + 18, top, x + 18, bottom, _bus);
        Text(phases, x + 24, top - 6, 13, 64);
        Text(neutral, x + 24, top + 24, 13, 64);
        Text(material, x + 44, top - 35, 12, 150);
        Dot(x, top + 84);
        Dot(x + 18, top + 84);
    }

    public void DashedCabinet(Rect rect, string? label = null)
    {
        _dc.DrawRectangle(null, _dash, rect);
        if (!string.IsNullOrWhiteSpace(label))
            RotatedText(label, rect.Left + 12, rect.Top + 76, 12, -90, Math.Max(40, rect.Height - 20));
    }

    public void TerminalArrowLeft(double x, double y)
    {
        Line(x, y, x - 20, y - 9, _line);
        Line(x, y, x - 20, y + 9, _line);
        Line(x - 20, y - 9, x - 20, y + 9, _line);
    }

    public void TerminalArrowRight(double x, double y)
    {
        Line(x, y, x + 20, y - 9, _line);
        Line(x, y, x + 20, y + 9, _line);
        Line(x + 20, y - 9, x + 20, y + 9, _line);
    }

    public void TerminalArrowUp(double x, double y)
    {
        Line(x, y, x - 9, y + 20, _line);
        Line(x, y, x + 9, y + 20, _line);
        Line(x - 9, y + 20, x + 9, y + 20, _line);
    }

    public void TerminalArrowDown(double x, double y)
    {
        Line(x, y, x - 9, y - 20, _line);
        Line(x, y, x + 9, y - 20, _line);
        Line(x - 9, y - 20, x + 9, y - 20, _line);
    }

    public void CableSlashes(double x, double y)
    {
        for (var i = 0; i < 3; i++)
        {
            var dx = i * 9;
            Line(x + dx - 5, y + 9, x + dx + 5, y - 9, _thin);
        }
    }

    public void CableSlashesVertical(double x, double y)
    {
        for (var i = 0; i < 3; i++)
        {
            var dy = i * 9;
            Line(x - 9, y + dy + 5, x + 9, y + dy - 5, _thin);
        }
    }

    public void TerminalStrip(double x, double y, int cells, string label)
    {
        cells = Math.Clamp(cells, 1, 12);
        const double cellWidth = 15;
        const double height = 32;
        var width = cells * cellWidth;

        _dc.DrawRectangle(_surface, _thin, new Rect(x, y, width, height));
        for (var i = 1; i < cells; i++)
            Line(x + i * cellWidth, y, x + i * cellWidth, y + height, _thin);

        for (var i = 0; i < cells; i++)
        {
            var cx = x + i * cellWidth + cellWidth / 2;
            DotSmall(cx, y + 8);
            DotSmall(cx, y + height - 8);
        }

        if (!string.IsNullOrWhiteSpace(label))
            RotatedText(label, x + width + 11, y + 25, 9, -90, 46);
    }

    public void ControlRelay(double x, double y, string label)
    {
        _dc.DrawRectangle(_surface, _thin, new Rect(x - 14, y - 13, 28, 26));
        Text(label, x - 8, y - 7, 10, 24);
    }

    public void InstrumentCircle(double x, double y, string label)
    {
        _dc.DrawEllipse(_surface, _thin, new Point(x, y), 16, 16);
        Text(label, x - 11, y - 8, 9.5, 28);
    }

    public void Disconnector(double x, double y, string designator, string label)
    {
        Line(x + 38, y, x + 14, y, _line);
        Line(x - 14, y, x - 38, y, _line);
        _dc.DrawEllipse(_surface, _line, new Point(x + 14, y), 3.5, 3.5);
        _dc.DrawEllipse(_surface, _line, new Point(x - 14, y), 3.5, 3.5);
        Line(x - 12, y - 1, x + 12, y - 22, _line);
        Line(x + 1, y - 10, x + 12, y - 10, _thin);
        Text(designator, x - 18, y - 48, 12, 52);
        if (!string.IsNullOrWhiteSpace(label) && label != "-")
            Text(label, x - 25, y + 10, 11, 72);
    }

    public void CircuitBreaker(double x, double y, string designator, string label)
    {
        Line(x + 42, y, x + 12, y, _line);
        Line(x - 12, y, x - 42, y, _line);
        _dc.DrawEllipse(_surface, _line, new Point(x + 12, y), 3, 3);
        _dc.DrawEllipse(_surface, _line, new Point(x - 12, y), 2.5, 2.5);
        Line(x - 12, y, x + 10, y - 24, _line);
        Line(x - 16, y + 4, x - 22, y - 4, _thin);
        Line(x - 19, y - 8, x - 25, y - 16, _thin);
        Text(designator, x - 13, y - 50, 12, 52);
        if (!string.IsNullOrWhiteSpace(label) && label != "-")
            Text(label, x - 18, y - 32, 11, 74);
    }

    public void Fuse(double x, double y, string designator, string label)
    {
        Line(x + 36, y, x + 14, y, _line);
        Line(x - 14, y, x - 36, y, _line);
        _dc.DrawRectangle(_surface, _line, new Rect(x - 14, y - 10, 28, 20));
        Line(x - 9, y + 8, x + 9, y - 8, _line);
        Text(designator, x - 18, y - 48, 12, 70);
        if (!string.IsNullOrWhiteSpace(label) && label != "-")
            Text(label, x - 20, y + 12, 11, 80);
    }

    public void DisconnectorVertical(double x, double y, string designator, string label)
    {
        Line(x, y - 42, x, y - 14, _line);
        Line(x, y + 14, x, y + 42, _line);
        _dc.DrawEllipse(_surface, _line, new Point(x, y - 14), 3.5, 3.5);
        _dc.DrawEllipse(_surface, _line, new Point(x, y + 14), 3.5, 3.5);
        Line(x + 1, y - 12, x + 23, y + 12, _line);
        Text(designator, x - 68, y - 20, 12, 58);
        if (!string.IsNullOrWhiteSpace(label) && label != "-")
            Text(label, x - 68, y - 3, 11, 64);
    }

    public void CircuitBreakerVertical(double x, double y, string designator, string label)
    {
        Line(x, y - 44, x, y - 17, _line);
        _dc.DrawRectangle(_surface, _line, new Rect(x - 13, y - 17, 26, 34));
        Line(x - 8, y + 12, x + 8, y - 12, _line);
        Line(x, y + 17, x, y + 44, _line);
        Text(designator, x - 70, y - 17, 12, 62);
        if (!string.IsNullOrWhiteSpace(label) && label != "-")
            Text(label, x - 70, y + 1, 11, 64);
    }

    public void FuseVertical(double x, double y, string designator, string label)
    {
        Line(x, y - 42, x, y - 16, _line);
        _dc.DrawRectangle(_surface, _line, new Rect(x - 11, y - 16, 22, 32));
        Line(x - 8, y + 12, x + 8, y - 12, _line);
        Line(x, y + 16, x, y + 42, _line);
        Text(designator, x - 74, y - 17, 12, 70);
        if (!string.IsNullOrWhiteSpace(label) && label != "-")
            Text(label, x - 74, y + 1, 11, 64);
    }

    public void PowerTransformerVertical(double x, double y, string designator, string voltage, string mark)
    {
        _dc.DrawEllipse(_surface, _line, new Point(x, y - 24), 26, 26);
        _dc.DrawEllipse(_surface, _line, new Point(x, y + 24), 26, 26);
        Text("Y", x - 8, y - 33, 10, 18);
        Text("Y", x - 8, y + 15, 10, 18);
        Text(designator, x + 34, y - 10, 12, 44);
        Text(voltage, x + 34, y + 8, 11, 96);
        if (!string.IsNullOrWhiteSpace(mark) && mark != "-")
            Text(mark, x + 34, y + 25, 9, 110);
    }

    public void PowerTransformer(double x, double y, string designator, string voltage, string mark)
    {
        _dc.DrawEllipse(_surface, _line, new Point(x + 18, y), 24, 24);
        _dc.DrawEllipse(_surface, _line, new Point(x - 18, y), 24, 24);
        Text("Y", x - 26, y - 9, 10, 18);
        DrawDelta(x + 18, y);
        Text(designator, x + 32, y - 44, 12, 42);
        Text(voltage, x - 40, y + 36, 11, 120);
        Text(mark, x - 52, y + 53, 10, 150);
    }

    public void CurrentTransformerSet(double x, double y, string designator, string ratio)
    {
        for (var i = 0; i < 3; i++)
            DrawCoil(x + i * 18, y, 10, 18);
        Text(designator, x - 8, y - 46, 11, 78);
        if (!string.IsNullOrWhiteSpace(ratio))
            Text(ratio, x - 4, y + 28, 10, 64);
    }

    public void CurrentTransformerPair(double x, double y, string label)
    {
        DrawCoil(x - 8, y, 7, 12);
        DrawCoil(x + 8, y, 7, 12);
        if (!string.IsNullOrWhiteSpace(label))
            Text(label, x - 28, y + 18, 9, 70);
    }

    public void Meter(double x, double y, string designator, string text)
    {
        _dc.DrawEllipse(_surface, _line, new Point(x, y), 19, 19);
        Text(designator, x - 10, y - 9, 11, 40);
        if (!string.IsNullOrWhiteSpace(text))
            Text(text, x + 27, y - 9, 12, 50);
    }

    public void SurgeArrester(double x, double y, string designator, string label)
    {
        Line(x, y, x, y + 30, _thin);
        _dc.DrawRectangle(_surface, _thin, new Rect(x - 10, y + 30, 20, 34));
        Line(x - 9, y + 60, x + 9, y + 34, _thin);
        Line(x, y + 64, x, y + 92, _thin);
        Ground(x, y + 92);
        Text(designator, x + 15, y + 18, 10, 70);
        Text(label, x + 15, y + 34, 10, 70);
    }

    public void Ground(double x, double y)
    {
        Line(x - 13, y, x + 13, y, _thin);
        Line(x - 9, y + 6, x + 9, y + 6, _thin);
        Line(x - 5, y + 12, x + 5, y + 12, _thin);
    }

    public void Dot(double x, double y) =>
        _dc.DrawEllipse(Brushes.Black, null, new Point(x, y), 4, 4);

    public void DotSmall(double x, double y) =>
        _dc.DrawEllipse(Brushes.Black, null, new Point(x, y), 2.8, 2.8);

    private void DrawDelta(double x, double y)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(x, y - 11), isFilled: false, isClosed: true);
            ctx.LineTo(new Point(x - 10, y + 8), isStroked: true, isSmoothJoin: true);
            ctx.LineTo(new Point(x + 10, y + 8), isStroked: true, isSmoothJoin: true);
        }
        geometry.Freeze();
        _dc.DrawGeometry(null, _thin, geometry);
    }

    private void DrawCoil(double x, double y, double rx, double ry)
    {
        _dc.DrawEllipse(_surface, _line, new Point(x, y), rx, ry);
    }

    private void Text(string text, double x, double y, double size, double maxWidth)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.GetCultureInfo("ru-RU"),
            FlowDirection.LeftToRight,
            _font,
            size,
            _text,
            _pixelsPerDip)
        {
            MaxTextWidth = Math.Max(20, maxWidth),
            Trimming = TextTrimming.CharacterEllipsis,
        };
        _dc.DrawText(formatted, new Point(x, y));
    }

    private void RotatedText(string text, double x, double y, double size, double angle, double maxWidth)
    {
        _dc.PushTransform(new RotateTransform(angle, x, y));
        Text(text, x, y, size, maxWidth);
        _dc.Pop();
    }

    private void Line(double x1, double y1, double x2, double y2, Pen pen) =>
        _dc.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
}
