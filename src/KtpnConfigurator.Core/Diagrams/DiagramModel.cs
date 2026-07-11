namespace KtpnConfigurator.Core.Diagrams;

public enum DiagramSheetKind
{
    OneLine,
    Metering,
    Auxiliary,
}

public enum DiagramNodeKind
{
    Source,
    HvSwitch,
    Fuse,
    SurgeArrester,
    PowerTransformer,
    LvInputDevice,
    Busbar,
    CurrentTransformer,
    Meter,
    OutgoingFeederDevice,
    FeederOutput,
    AuxiliaryBreaker,
    AuxiliaryCabinet,
    Lighting,
    Socket,
    Heating,
    Ventilation,
    BackupPowerSource,
    TerminalBlock,
    Ground,
}

public enum DiagramConnectionKind
{
    Power,
    Neutral,
    ProtectiveEarth,
    Metering,
    Control,
    Reference,
}

public sealed class DiagramModel
{
    public List<DiagramSheet> Sheets { get; set; } = new();

    public DiagramSheet? MainSheet =>
        Sheets.FirstOrDefault(s => s.Kind == DiagramSheetKind.OneLine);
}

public sealed class DiagramSheet
{
    public DiagramSheetKind Kind { get; set; }
    public string Name { get; set; } = "";
    public List<DiagramNode> Nodes { get; set; } = new();
    public List<DiagramConnection> Connections { get; set; } = new();
}

public sealed class DiagramNode
{
    public string Id { get; set; } = "";
    public DiagramNodeKind Kind { get; set; }
    public string SymbolKey { get; set; } = "";
    public string Designation { get; set; } = "";
    public string Title { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Model { get; set; } = "";
    public string Nominal { get; set; } = "";
    public List<DiagramLabel> Labels { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class DiagramConnection
{
    public string Id { get; set; } = "";
    public DiagramConnectionKind Kind { get; set; }
    public DiagramPort From { get; set; } = new();
    public DiagramPort To { get; set; } = new();
    public string Label { get; set; } = "";
}

public sealed class DiagramPort
{
    public string NodeId { get; set; } = "";
    public string Port { get; set; } = "";
}

public sealed class DiagramLabel
{
    public string Text { get; set; } = "";
    public string Role { get; set; } = "";
}
