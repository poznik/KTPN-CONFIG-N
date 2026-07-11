using System;
using System.IO;
using System.Linq;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Diagrams;
using KtpnConfigurator.Core.Models;
using Xunit;

namespace KtpnConfigurator.Tests;

public class ProjectAndDiagramModelTests
{
    private static string DataDir => Path.Combine(AppContext.BaseDirectory, "Data");

    [Fact]
    public void ProjectConfig_Clone_DoesNotShareNestedState()
    {
        var cfg = new ProjectConfig
        {
            ProjectName = "Исходный",
            AuxiliaryNeeds = new AuxiliaryNeedsConfig
            {
                HasAuxiliaryCabinet = true,
                CabinetModel = "ЩСН-01",
            },
            OutgoingFeeders =
            {
                new OutgoingFeederConfig
                {
                    Number = 1,
                    DeviceType = "АВ",
                    Manufacturer = "IEK",
                    Model = "ВА88",
                    Nominal = 400,
                    HasMeter = true,
                    TtRatio = "400/5",
                },
            },
        };

        var clone = cfg.Clone();
        clone.AuxiliaryNeeds.CabinetModel = "ЩСН-02";
        clone.OutgoingFeeders[0].Model = "Другая модель";
        clone.OutgoingFeeders.Add(new OutgoingFeederConfig { Number = 2, DeviceType = "РПС" });

        Assert.Equal("ЩСН-01", cfg.AuxiliaryNeeds.CabinetModel);
        Assert.Equal("ВА88", cfg.OutgoingFeeders[0].Model);
        Assert.Single(cfg.OutgoingFeeders);
    }

    [Fact]
    public void ProjectStorage_LoadsLegacyProjectWithCurrentVersion()
    {
        var path = Path.Combine(Path.GetTempPath(), $"legacy_version_{Guid.NewGuid():N}.ktpn");
        File.WriteAllText(path, """
        {
          "ProjectName": "Старый проект",
          "OutgoingFeeders": null,
          "AuxiliaryNeeds": null
        }
        """);

        try
        {
            var cfg = ProjectStorage.Load(path);

            Assert.Equal(ProjectConfig.CurrentVersion, cfg.ProjectVersion);
            Assert.NotNull(cfg.AuxiliaryNeeds);
            Assert.NotNull(cfg.OutgoingFeeders);
            Assert.Empty(cfg.OutgoingFeeders);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ProjectStorage_SaveWritesCurrentVersion()
    {
        var path = Path.Combine(Path.GetTempPath(), $"current_version_{Guid.NewGuid():N}.ktpn");

        try
        {
            ProjectStorage.Save(new ProjectConfig(), path);
            var json = File.ReadAllText(path);

            Assert.Contains("\"ProjectVersion\": 1", json);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void DiagramModelBuilder_ReflectsSelectedLvInputDevice()
    {
        var store = CatalogStore.Load(DataDir);
        var res = new CalculationResult { BusbarLv = "50x5" };

        var pvr = BuildBaseConfig();
        pvr.PvrOn = true;
        var pvrModel = DiagramModelBuilder.Build(pvr, res, store);
        Assert.Contains(pvrModel.MainSheet!.Nodes, n => n.Designation == "QS2" && n.Title.Contains("ПВР"));

        var re = BuildBaseConfig();
        re.ReOn = true;
        var reModel = DiagramModelBuilder.Build(re, res, store);
        Assert.Contains(reModel.MainSheet!.Nodes, n => n.Designation == "QS3" && n.Title.Contains("РЕ"));

        var av = BuildBaseConfig();
        av.AvInOn = true;
        var avModel = DiagramModelBuilder.Build(av, res, store);
        Assert.Contains(avModel.MainSheet!.Nodes, n => n.Designation == "QF1" && n.Title.Contains("АВ"));
    }

    [Fact]
    public void DiagramModelBuilder_AddsOutgoingMeteringNodes()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = BuildBaseConfig();
        cfg.PvrOn = true;
        cfg.OutgoingFeeders.Add(new OutgoingFeederConfig
        {
            Number = 1,
            DeviceType = "АВ",
            Manufacturer = "IEK",
            Model = "ВА88",
            Nominal = 630,
            HasMeter = true,
            TtRatio = "600/5",
        });

        var model = DiagramModelBuilder.Build(cfg, new CalculationResult { BusbarLv = "50x5" }, store);
        var main = model.MainSheet!;
        var metering = model.Sheets.Single(s => s.Kind == DiagramSheetKind.Metering);

        Assert.Contains(main.Nodes, n => n.Id == "feeder-1-ct" && n.Designation == "TA4-TA6");
        Assert.Contains(main.Nodes, n => n.Id == "feeder-1-meter" && n.Designation == "PI2");
        Assert.Contains(main.Connections, c => c.Kind == DiagramConnectionKind.Metering && c.Label.Contains("учета"));
        Assert.Contains(metering.Nodes, n => n.Id == "metering-feeder-1-meter" && n.Designation == "PI2");
    }

    [Fact]
    public void DiagramModelBuilder_AddsAuxiliaryBranchNodes()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = BuildBaseConfig();
        cfg.PvrOn = true;
        cfg.AuxiliaryNeeds = new AuxiliaryNeedsConfig
        {
            HasAuxiliaryCabinet = true,
            CabinetModel = "ЩСН-0,4",
            MainBreakerNominal = 25,
            HasLighting = true,
            LightingControlType = LightingControlType.PhotoRelay,
            SocketEnabled = true,
            HasRise = true,
            RiseType = RiseType.UPS,
        };

        var model = DiagramModelBuilder.Build(cfg, new CalculationResult { BusbarLv = "50x5" }, store);
        var main = model.MainSheet!;

        Assert.Contains(main.Nodes, n => n.Id == "aux-main-breaker" && n.Designation == "QF20");
        Assert.Contains(main.Nodes, n => n.Id == "aux-cabinet" && n.Designation == "ЩСН1");
        Assert.Contains(main.Nodes, n => n.Id == "aux-lighting" && n.Designation == "EL1");
        Assert.Contains(main.Nodes, n => n.Id == "aux-photo-relay" && n.Designation == "BL1");
        Assert.Contains(main.Nodes, n => n.Id == "aux-socket" && n.Designation == "XS1");
        Assert.Contains(main.Nodes, n => n.Id == "aux-rise" && n.Designation == "РИСЭ1");
    }

    private static ProjectConfig BuildBaseConfig() =>
        new()
        {
            Voltage = "10 кВ",
            RuvnSwitch = "РВЗ",
            FuseType = "ПКТ",
            RuvnSurgeArrester = true,
            RunnSurgeArrester = true,
            CtRatio = "600/5",
            OutgoingExecution = "Кабельный",
        };
}
