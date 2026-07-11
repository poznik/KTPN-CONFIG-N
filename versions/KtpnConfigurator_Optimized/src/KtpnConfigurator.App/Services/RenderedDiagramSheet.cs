using System.Windows;
using KtpnConfigurator.Core.Diagrams;

namespace KtpnConfigurator.App.Services;

public sealed record RenderedDiagramSheet(
    string Name,
    DiagramSheetKind Kind,
    Size Size,
    byte[] Png);
