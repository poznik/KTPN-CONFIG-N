using System.Text.Json;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;
using Xunit;

namespace KtpnConfigurator.Tests;

public sealed class OracleConfig
{
    public string Mark { get; set; } = "";
    public string RuvnType { get; set; } = "";
    public double LenRuvn { get; set; }
    public double LenRunn { get; set; }
    public double Tol { get; set; }
    public double Buffer { get; set; }
    public string Channel { get; set; } = "";
    public double Thickness { get; set; }
    public bool AvOn { get; set; }
    public int AvQty { get; set; }
    public bool RpsOn { get; set; }
    public int RpsQty { get; set; }
    public bool PvrOn { get; set; }
    public int PvrNom { get; set; }
    public bool ReOn { get; set; }
    public int ReNom { get; set; }
    public bool AvInOn { get; set; }
    public int AvInNom { get; set; }
    public double? ManualLength { get; set; }
    public double? ManualWidth { get; set; }
    public double? ManualHeight { get; set; }
}

public sealed class OracleExpected
{
    public double RatedCurrentA { get; set; }
    public double LengthCalc { get; set; }
    public double LengthFinal { get; set; }
    public double WidthCalc { get; set; }
    public double WidthFinal { get; set; }
    public double HeightCalc { get; set; }
    public double HeightFinal { get; set; }
    public double BaseMass { get; set; }
    public double BodyMass { get; set; }
    public double GrossMass { get; set; }
    public double InputNominal { get; set; }
    public bool ValidationOk { get; set; }
}

public sealed class GoldenFile
{
    public Dictionary<string, OracleConfig> Configs { get; set; } = new();
    public Dictionary<string, OracleExpected> Expected { get; set; } = new();
}

public class GoldenTests
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static string DataDir => Path.Combine(AppContext.BaseDirectory, "Data");
    private static string GoldenPath => Path.Combine(AppContext.BaseDirectory, "golden.json");

    private static GoldenFile LoadGolden()
    {
        using var fs = File.OpenRead(GoldenPath);
        return JsonSerializer.Deserialize<GoldenFile>(fs, JsonOpts)!;
    }

    private static ProjectConfig ToConfig(OracleConfig o) => new()
    {
        Mark = o.Mark,
        RuvnType = o.RuvnType,
        LenRuvn = o.LenRuvn,
        LenRunn = o.LenRunn,
        TransformerTolerance = o.Tol,
        LengthBuffer = o.Buffer,
        Channel = o.Channel,
        Thickness = o.Thickness,
        AvOn = o.AvOn,
        AvQty = o.AvQty,
        RpsOn = o.RpsOn,
        RpsQty = o.RpsQty,
        PvrOn = o.PvrOn,
        PvrNominal = o.PvrNom,
        ReOn = o.ReOn,
        ReNominal = o.ReNom,
        AvInOn = o.AvInOn,
        AvInNominal = o.AvInNom,
        ManualLength = o.ManualLength,
        ManualWidth = o.ManualWidth,
        ManualHeight = o.ManualHeight,
    };

    public static IEnumerable<object[]> CaseNames()
    {
        foreach (var name in LoadGolden().Configs.Keys)
            yield return new object[] { name };
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Engine_matches_oracle(string caseName)
    {
        var store = CatalogStore.Load(DataDir);
        var golden = LoadGolden();
        var cfg = ToConfig(golden.Configs[caseName]);
        var exp = golden.Expected[caseName];

        var res = CalculationEngine.Calculate(cfg, store);

        Assert.Equal(exp.RatedCurrentA, res.RatedCurrentA, 3);
        Assert.Equal(exp.LengthCalc, res.LengthCalc, 3);
        Assert.Equal(exp.LengthFinal, res.LengthFinal, 3);
        Assert.Equal(exp.WidthCalc, res.WidthCalc, 3);
        Assert.Equal(exp.WidthFinal, res.WidthFinal, 3);
        Assert.Equal(exp.HeightCalc, res.HeightCalc, 3);
        Assert.Equal(exp.HeightFinal, res.HeightFinal, 3);
        Assert.Equal(exp.BaseMass, res.BaseMass, 3);
        Assert.Equal(exp.BodyMass, res.BodyMass, 3);
        Assert.Equal(exp.GrossMass, res.GrossMass, 3);
        Assert.Equal(exp.InputNominal, res.InputNominal, 3);
        Assert.Equal(exp.ValidationOk, res.ValidationOk);
    }

    [Fact]
    public void Catalogs_load_expected_counts()
    {
        var store = CatalogStore.Load(DataDir);
        Assert.Equal(73, store.Transformers.Count);
        Assert.Equal(18, store.Apparatus.Count);
        Assert.Equal(6, store.Manufacturers().Count);
        Assert.Equal(12, store.Options.Channels.Count);
        Assert.Equal(4, store.Options.Steels.Count);
    }

    [Fact]
    public void Cascade_filters_marks_by_manufacturer()
    {
        var store = CatalogStore.Load(DataDir);
        Assert.Equal(20, store.MarksFor("Алагеум").Count);
        Assert.Equal(12, store.MarksFor("Алттранс").Count);
        Assert.All(store.MarksFor("СВЭЛ"), mark =>
            Assert.Equal("СВЭЛ", store.GetTransformer(mark)!.Manufacturer));
    }

    [Fact]
    public void Manual_mass_overrides_apply_like_excel_columnC()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = new ProjectConfig
        {
            Manufacturer = "Алагеум", Mark = "ТМГ-400 (Алагеум)",
            Channel = "10П", Thickness = 2.0, RuvnType = "Тупиковая",
            PvrOn = true, PvrNominal = 630,
        };
        var baseline = CalculationEngine.Calculate(cfg, store);

        cfg.ManualBaseMass = 500;
        cfg.ManualBodyMass = 700;
        var res = CalculationEngine.Calculate(cfg, store);

        // calc-значения сохраняются, итоговые берут ручной ввод
        Assert.Equal(baseline.BaseMass, res.BaseMassCalc, 3);
        Assert.Equal(baseline.BodyMass, res.BodyMassCalc, 3);
        Assert.Equal(500, res.BaseMass, 3);
        Assert.Equal(700, res.BodyMass, 3);
        // брутто пересобирается из переопределённых масс (D58 = E7 + D56 + D57)
        var trMass = store.GetTransformer(cfg.Mark)!.MassKg;
        Assert.Equal(trMass + 500 + 700, res.GrossMass, 3);

        // явный ручной ввод брутто перекрывает всё
        cfg.ManualGrossMass = 9999;
        Assert.Equal(9999, CalculationEngine.Calculate(cfg, store).GrossMass, 3);
    }

    [Fact]
    public void Missing_transformer_yields_error_and_no_crash()
    {
        var store = CatalogStore.Load(DataDir);
        var res = CalculationEngine.Calculate(new ProjectConfig { Mark = "НЕСУЩЕСТВУЕТ" }, store);
        Assert.True(res.HasErrors);
        Assert.Equal(0, res.GrossMass);
    }
}
