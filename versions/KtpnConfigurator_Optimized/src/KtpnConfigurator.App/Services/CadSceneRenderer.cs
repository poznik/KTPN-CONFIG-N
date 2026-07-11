using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace KtpnConfigurator.App.Services;

internal sealed class CadSceneRenderer
{
    private readonly DrawingContext _dc;
    private readonly double _pixelsPerDip;
    private readonly Typeface _regular = new(new FontFamily("Arial Narrow"), FontStyles.Italic, FontWeights.Normal, FontStretches.Normal);
    private readonly Typeface _bold = new(new FontFamily("Arial Narrow"), FontStyles.Italic, FontWeights.SemiBold, FontStretches.Normal);

    private readonly Pen _hair = new(Brushes.Black, 0.65);
    private readonly Pen _thin = new(Brushes.Black, 0.9);
    private readonly Pen _normal = new(Brushes.Black, 1.35);
    private readonly Pen _bus = new(Brushes.Black, 2.4);
    private readonly Pen _frame = new(Brushes.Black, 2.0);
    private readonly Pen _dash = new(Brushes.Black, 0.9) { DashStyle = new DashStyle(new[] { 12.0, 10.0 }, 0) };

    public CadSceneRenderer(DrawingContext dc, double pixelsPerDip)
    {
        _dc = dc;
        _pixelsPerDip = pixelsPerDip <= 0 ? 1 : pixelsPerDip;
    }

    public void Draw(CadScene scene)
    {
        _dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, scene.Size.Width, scene.Size.Height));

        foreach (var entity in scene.Entities)
        {
            switch (entity)
            {
                case CadLine line:
                    _dc.DrawLine(Pen(line.Stroke), line.Start, line.End);
                    break;
                case CadRect rect:
                    _dc.DrawRectangle(rect.Fill, rect.Dashed ? _dash : Pen(rect.Stroke), rect.Rect);
                    break;
                case CadEllipse ellipse:
                    _dc.DrawEllipse(ellipse.Fill, ellipse.Stroke == CadStroke.None ? null : Pen(ellipse.Stroke), ellipse.Center, ellipse.RadiusX, ellipse.RadiusY);
                    break;
                case CadText text:
                    DrawText(text);
                    break;
                case CadPolyline polyline:
                    DrawPolyline(polyline);
                    break;
            }
        }
    }

    private Pen Pen(CadStroke stroke) =>
        stroke switch
        {
            CadStroke.Hair => _hair,
            CadStroke.Thin => _thin,
            CadStroke.Normal => _normal,
            CadStroke.Bus => _bus,
            CadStroke.Frame => _frame,
            CadStroke.Dashed => _dash,
            _ => _thin,
        };

    private void DrawText(CadText text)
    {
        if (string.IsNullOrWhiteSpace(text.Text))
            return;

        var formatted = new FormattedText(
            text.Text,
            CultureInfo.GetCultureInfo("ru-RU"),
            FlowDirection.LeftToRight,
            text.Bold ? _bold : _regular,
            text.Size,
            Brushes.Black,
            _pixelsPerDip)
        {
            MaxTextWidth = Math.Max(12, text.MaxWidth),
            TextAlignment = text.Alignment,
            Trimming = TextTrimming.CharacterEllipsis,
        };

        if (Math.Abs(text.Angle) > 0.001)
            _dc.PushTransform(new RotateTransform(text.Angle, text.Origin.X, text.Origin.Y));

        _dc.DrawText(formatted, text.Origin);

        if (Math.Abs(text.Angle) > 0.001)
            _dc.Pop();
    }

    private void DrawPolyline(CadPolyline polyline)
    {
        if (polyline.Points.Count < 2)
            return;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(polyline.Points[0], isFilled: false, isClosed: polyline.Closed);
            for (var i = 1; i < polyline.Points.Count; i++)
                ctx.LineTo(polyline.Points[i], isStroked: true, isSmoothJoin: false);
        }
        geometry.Freeze();
        _dc.DrawGeometry(null, Pen(polyline.Stroke), geometry);
    }
}
