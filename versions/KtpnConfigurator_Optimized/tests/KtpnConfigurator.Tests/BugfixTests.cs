using System;
using System.IO;
using System.Linq;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.App.ViewModels;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Documents;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;
using Xunit;

namespace KtpnConfigurator.Tests;

public class BugfixTests
{
    private static string DataDir => Path.Combine(AppContext.BaseDirectory, "Data");

    [Fact]
    public void Corrupted_DocTemplates_DoesNotCrash()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "doc_templates.json"), "{ invalid_json: ");
            var tpl = DocTemplates.Load(tempDir);
            Assert.NotNull(tpl);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Apparatus_WithNullRange_DoesNotCrash()
    {
        var store = CatalogStore.Load(DataDir);
        var spec = new ApparatusSpec { Type = "Вводной АВ", Manufacturer = "TestBrand", CurrentRange = null! };
        var list = store.Apparatus as System.Collections.Generic.IList<ApparatusSpec>;
        list?.Add(spec);
        
        var cfg = new ProjectConfig
        {
            Manufacturer = "Алагеум", Mark = "ТМГ-400 (Алагеум)",
            AvInOn = true, AvInManufacturer = "TestBrand", AvInNominal = 630
        };
        var res = CalculationEngine.Calculate(cfg, store);
        Assert.NotNull(res);
    }

    [Fact]
    public void UnknownChannel_YieldsWarning()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = new ProjectConfig
        {
            Manufacturer = "Алагеум", Mark = "ТМГ-400 (Алагеум)",
            Channel = "НеизвестныйШвеллер99"
        };
        var res = CalculationEngine.Calculate(cfg, store);
        
        Assert.Contains(res.Messages, m => m.Severity == Severity.Warning && m.Text.Contains("масса неизвестна"));
    }

    [Fact]
    public void OutgoingFeederMeterChange_NotifiesDiagramRefresh()
    {
        var vm = new MainViewModel(AppEnvironment.Load());
        var changed = new System.Collections.Generic.HashSet<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.OutgoingFeeders.First().HasMeter = !vm.OutgoingFeeders.First().HasMeter;

        Assert.Contains(nameof(MainViewModel.CurrentConfig), changed);
        Assert.Contains(nameof(MainViewModel.CurrentResult), changed);
    }

    [Fact]
    public void NullAuxiliaryNeeds_DoesNotCrashCalculationOrSpecification()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = new ProjectConfig
        {
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            Channel = "10П",
            SteelType = "Оцинкованная",
            Thickness = 2.0,
            PvrOn = true,
            PvrManufacturer = "CHINT",
            PvrNominal = 630,
            AuxiliaryNeeds = null!,
        };

        var res = CalculationEngine.Calculate(cfg, store);
        var spec = DocumentBuilder.BuildSpecification(cfg, res, store);

        Assert.NotNull(res);
        Assert.DoesNotContain(spec.Sections, s => s.Name.Contains("Собственные нужды"));
    }
}
