namespace KtpnConfigurator.Core.Models;

/// <summary>Силовой трансформатор (строка сводной БД).</summary>
public sealed class TransformerSpec
{
    public string Mark { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public double PowerKva { get; set; }
    public double LengthMm { get; set; }
    public double WidthMm { get; set; }
    public double HeightMm { get; set; }
    public double MassKg { get; set; }
    public double RatedCurrentA { get; set; }
    public string BusbarHv { get; set; } = "";
    public string BusbarLv { get; set; } = "";
    public string BusbarPe { get; set; } = "";
}

/// <summary>Аппарат РУНН (каталог).</summary>
public sealed class ApparatusSpec
{
    public string Type { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Country { get; set; } = "";
    public string Series { get; set; } = "";
    public string CurrentRange { get; set; } = "";
}

public sealed class ChannelSpec
{
    public string Size { get; set; } = "";
    public double WeightPerM { get; set; }
}

public sealed class SteelSpec
{
    public double ThicknessMm { get; set; }
    public double WeightPerM2 { get; set; }
}

/// <summary>Параметры методики расчёта (коэффициенты, типовые ряды, пороги).</summary>
public sealed class MethodologyParams
{
    public double FloorSheetKgPerM2 { get; set; } = 35.0;
    public double FrameCoef { get; set; } = 1.15;
    public double BodyWasteCoef { get; set; } = 1.25;
    public List<double> StandardLengths { get; set; } = new() { 2000, 3000, 3500 };
    public double MinDimensionMm { get; set; } = 1800;
    public double HeightThresholdMm { get; set; } = 1800;
    public double TallHeightMm { get; set; } = 2400;
    public double ShortHeightMm { get; set; } = 2250;
    public double PassageWidthMm { get; set; } = 2420;
    public double MultiBayWidthMm { get; set; } = 2420;
    public double StandardWidthMm { get; set; } = 2000;
    public int MultiBayAvQty { get; set; } = 8;
    public int MultiBayRpsQty { get; set; } = 4;
}

/// <summary>Все справочные списки и таблицы пересчёта (options.json).</summary>
public sealed class CatalogOptions
{
    public List<string> SteelTypes { get; set; } = new();
    public List<double> SteelThicknesses { get; set; } = new();
    public List<ChannelSpec> Channels { get; set; } = new();
    public List<SteelSpec> Steels { get; set; } = new();
    public List<string> RalColors { get; set; } = new();
    public List<string> GridCompanies { get; set; } = new();
    public List<string> Voltages { get; set; } = new();
    public List<string> RuvnTypes { get; set; } = new();
    public List<string> RuvnSwitches { get; set; } = new();
    public List<int> RuvnNominals { get; set; } = new();
    public List<string> FuseTypes { get; set; } = new();
    public List<string> FuseNominals { get; set; } = new();
    public List<string> CableExecutions { get; set; } = new();
    public List<int> LvNominals { get; set; } = new();
    public List<string> TtRatios { get; set; } = new();
    public List<string> TtAccuracyClasses { get; set; } = new();
    public List<string> PvrManufacturers { get; set; } = new();
    public List<string> ReManufacturers { get; set; } = new();
    public List<string> RpsManufacturers { get; set; } = new();
    public List<string> AvManufacturers { get; set; } = new();
    public List<string> YesNo { get; set; } = new();
    public MethodologyParams Methodology { get; set; } = new();
}

public sealed class CustomerProfileSpec
{
    public string Name { get; set; } = "";
    public List<string> Aliases { get; set; } = new();
    public string Source { get; set; } = "";
    public CustomerProfileSettings Settings { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

public sealed class CustomerProfileSettings
{
    public string SteelType { get; set; } = "";
    public double? Thickness { get; set; }
    public string BodyColor { get; set; } = "";
    public string DoorColor { get; set; } = "";
    public string RoofColor { get; set; } = "";
    public string BaseColor { get; set; } = "";
    public string InternalPanelColor { get; set; } = "";
    public string LogoColor { get; set; } = "";
    public string ServicePlatformColor { get; set; } = "";
    public string ClimateExecution { get; set; } = "";
    public string ProtectionDegree { get; set; } = "";
    public string RuvnExecution { get; set; } = "";
    public string RuvnSurgeArresterLocation { get; set; } = "";
    public string RuvnSurgeArresterThroughput { get; set; } = "";
    public string RuvnDoorConfiguration { get; set; } = "";
    public string RunnDoorConfiguration { get; set; } = "";
    public string TransformerDoorConfiguration { get; set; } = "";
    public bool? HasRigelLock { get; set; }
    public string NetworkLockType { get; set; } = "";
    public bool? HasPadlockProvision { get; set; }
    public string GroundingType { get; set; } = "";
    public string VentilationType { get; set; } = "";
    public bool? HasRoofDeflector { get; set; }
    public bool? HasNameplate { get; set; }
    public bool? HasDoorCanopies { get; set; }
    public bool? HasDoorSeals { get; set; }
    public bool? HasTransformerMeshDoors { get; set; }
    public bool? HasLouverAnimalProtection { get; set; }
    public bool? HasAntiVandalHinges { get; set; }
    public bool? HasDoorSealing { get; set; }
    public bool? HasServicePlatform { get; set; }
    public bool? HasLogo { get; set; }
    public string LogoPlacement { get; set; } = "";
    public bool? HasWarningLabels { get; set; }
    public bool? HasDispatcherNameplate { get; set; }
    public bool? HasFeederLabels { get; set; }
    public string MarkingNotes { get; set; } = "";
    public string BusbarHvMaterial { get; set; } = "";
    public string BusbarLvMaterial { get; set; } = "";
    public string BusbarNMaterial { get; set; } = "";
    public bool? AuxiliaryEnabled { get; set; }
    public bool? LightingEnabled { get; set; }
    public int? LightingFixtureQuantity { get; set; }
    public string LightingAreas { get; set; } = "";
    public int? RepairLightingVoltage { get; set; }
    public bool? OutdoorLightingEnabled { get; set; }
    public bool? SocketEnabled { get; set; }
    public int? SocketQuantity { get; set; }
    public bool? HeatingEnabled { get; set; }
    public bool? MeterHeatingEnabled { get; set; }
    public bool? VentilationEnabled { get; set; }
    public bool? OpsEnabled { get; set; }
    public string OpsType { get; set; } = "";
    public string OpsManufacturer { get; set; } = "";
    public string OpsModel { get; set; } = "";
    public int? OpsLoops { get; set; }
    public string EnclosureNote { get; set; } = "";
    public string AuxiliaryNote { get; set; } = "";
}

/// <summary>Модель аппарата для выбора в РУНН.</summary>
public sealed class DeviceModel
{
    public string Type { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Series { get; set; } = "";
    public string Model { get; set; } = "";
    public List<int> Nominals { get; set; } = new();
    public string RatedCurrentA { get; set; } = "";
    public string Voltage { get; set; } = "";
    public int Poles { get; set; }
    public string CurrentRange { get; set; } = "";
    public string ApplicationRange { get; set; } = "";
    public string InstallationArea { get; set; } = "";
    public bool CompatibleWithMeter { get; set; }
    public List<string> CompatibleWith { get; set; } = new();
    public string RecommendedTtRatio { get; set; } = "";
    public string ProtectionKind { get; set; } = "";
    public string ReleaseType { get; set; } = "";
    public string TripCurve { get; set; } = "";
    public double? BreakingCapacityIcuKa { get; set; }
    public double? ServiceBreakingCapacityIcsKa { get; set; }
    public string SelectivityCategory { get; set; } = "";
    public string ProtectionNotes { get; set; } = "";
    public string SymbolKey { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Country { get; set; } = "";
    public string DataSource { get; set; } = "";
    public string SourceConfidence { get; set; } = "";
    public string VerificationDate { get; set; } = "";
    public string Unit { get; set; } = "шт";
}

/// <summary>УГО и буквенное обозначение для схемы.</summary>
public sealed class DiagramSymbol
{
    public string SymbolKey { get; set; } = "";
    public List<string> DeviceTypes { get; set; } = new();
    public string LetterCode { get; set; } = "";
    public string DisplayNameRu { get; set; } = "";
    public string DesignationPattern { get; set; } = "";
    public string LabelTemplate { get; set; } = "";
    public string SymbolGeometry { get; set; } = "";
    public List<string> Terminals { get; set; } = new();
    public string DefaultOrientation { get; set; } = "";
    public string LegendText { get; set; } = "";
    public List<string> GostRefs { get; set; } = new();
    public string SourceConfidence { get; set; } = "";
    public string Notes { get; set; } = "";
}

public sealed class FrameMarginsMm
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
}

public sealed class TitleBlockRule
{
    public double WidthMm { get; set; }
    public double HeightMm { get; set; }
    public string Placement { get; set; } = "";
    public string GostRef { get; set; } = "";
    public string SourceConfidence { get; set; } = "";
}

public sealed class DiagramLineWeightRule
{
    public double Frame { get; set; }
    public double MainCircuit { get; set; }
    public double AuxiliaryCircuit { get; set; }
    public double Thin { get; set; }
}

public sealed class DiagramSheetSetRule
{
    public string Name { get; set; } = "";
    public string ReferenceExample { get; set; } = "";
    public List<string> Sheets { get; set; } = new();
}

public sealed class DiagramStyleRule
{
    public string ReferenceStyle { get; set; } = "";
    public List<string> ReferenceExamples { get; set; } = new();
    public List<string> SheetFormats { get; set; } = new();
    public string FontFamily { get; set; } = "";
    public string TextStyle { get; set; } = "";
    public string FrameStyle { get; set; } = "";
    public string TitleBlockStyle { get; set; } = "";
    public List<string> VisibleFeatures { get; set; } = new();
    public List<DiagramSheetSetRule> SheetSets { get; set; } = new();
    public DiagramLineWeightRule LineWeights { get; set; } = new();
}

/// <summary>Правила построения схемы.</summary>
public sealed class DiagramRule
{
    public string PageFormat { get; set; } = "";
    public string PageOrientation { get; set; } = "";
    public FrameMarginsMm FrameMarginsMm { get; set; } = new();
    public TitleBlockRule TitleBlock { get; set; } = new();
    public List<string> ConstructionOrder { get; set; } = new();
    public Dictionary<string, string> NumberingRules { get; set; } = new();
    public string TextTruncationRule { get; set; } = "";
    public Dictionary<string, string> ExportRules { get; set; } = new();
    public DiagramStyleRule Style { get; set; } = new();
}
