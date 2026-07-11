using System.Windows;
using System.Windows.Media;

namespace KtpnConfigurator.App.Services;

internal sealed class CadScene
{
    public CadScene(Size size)
    {
        Size = size;
    }

    public Size Size { get; }
    public List<CadEntity> Entities { get; } = new();

    public void Line(double x1, double y1, double x2, double y2, CadStroke stroke = CadStroke.Thin) =>
        Entities.Add(new CadLine(new Point(x1, y1), new Point(x2, y2), stroke));

    public void Rect(double x, double y, double width, double height, CadStroke stroke = CadStroke.Thin, bool dashed = false) =>
        Entities.Add(new CadRect(new Rect(x, y, width, height), stroke, dashed));

    public void FilledRect(double x, double y, double width, double height, CadStroke stroke = CadStroke.Thin) =>
        Entities.Add(new CadRect(new Rect(x, y, width, height), stroke, false, Brushes.White));

    public void Ellipse(double x, double y, double rx, double ry, CadStroke stroke = CadStroke.Thin, Brush? fill = null) =>
        Entities.Add(new CadEllipse(new Point(x, y), rx, ry, stroke, fill));

    public void Dot(double x, double y, double r = 4) =>
        Entities.Add(new CadEllipse(new Point(x, y), r, r, CadStroke.None, Brushes.Black));

    public void Text(string text, double x, double y, double size, double maxWidth, double angle = 0, bool bold = false, TextAlignment alignment = TextAlignment.Left) =>
        Entities.Add(new CadText(text, new Point(x, y), size, maxWidth, angle, bold, alignment));

    public void Polyline(IReadOnlyList<Point> points, CadStroke stroke = CadStroke.Thin, bool closed = false) =>
        Entities.Add(new CadPolyline(points, stroke, closed));
}

internal enum CadStroke
{
    None,
    Hair,
    Thin,
    Normal,
    Bus,
    Frame,
    Dashed,
}

internal abstract record CadEntity;

internal sealed record CadLine(Point Start, Point End, CadStroke Stroke) : CadEntity;

internal sealed record CadRect(Rect Rect, CadStroke Stroke, bool Dashed, Brush? Fill = null) : CadEntity;

internal sealed record CadEllipse(Point Center, double RadiusX, double RadiusY, CadStroke Stroke, Brush? Fill = null) : CadEntity;

internal sealed record CadText(
    string Text,
    Point Origin,
    double Size,
    double MaxWidth,
    double Angle,
    bool Bold,
    TextAlignment Alignment) : CadEntity;

internal sealed record CadPolyline(IReadOnlyList<Point> Points, CadStroke Stroke, bool Closed) : CadEntity;
