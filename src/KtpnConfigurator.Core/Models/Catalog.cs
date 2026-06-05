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
    public List<string> PvrManufacturers { get; set; } = new();
    public List<string> ReManufacturers { get; set; } = new();
    public List<string> RpsManufacturers { get; set; } = new();
    public List<string> AvManufacturers { get; set; } = new();
    public List<string> YesNo { get; set; } = new();
    public MethodologyParams Methodology { get; set; } = new();
}
