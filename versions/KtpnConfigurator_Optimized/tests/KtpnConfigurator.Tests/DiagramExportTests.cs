using System.IO;
using System.Threading;
using ClosedXML.Excel;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Documents;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;
using Xunit;

namespace KtpnConfigurator.Tests;

public class DiagramExportTests
{
    private static string DataDir => Path.Combine(AppContext.BaseDirectory, "Data");

    [Fact]
    public void SingleLineDiagram_RendersNonEmptyPng()
    {
        RunSta(() =>
        {
            var (cfg, res, _, store) = BuildSampleProject();
            var png = SingleLineDiagramRenderer.RenderPng(cfg, res, store);

            Assert.True(png.Length > 20_000);
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png.Take(4).ToArray());
        });
    }

    [Fact]
    public void SingleLineDiagram_UsesCleanCanvasForSmallProjects()
    {
        var (cfg, _, _, _) = BuildSampleProject();
        cfg.OutgoingFeeders.RemoveRange(1, cfg.OutgoingFeeders.Count - 1);
        var size = SingleLineDiagramRenderer.Measure(cfg);

        Assert.True(size.Width > size.Height);
        Assert.Equal(1500, size.Width);
        Assert.Equal(620, size.Height);
    }

    [Fact]
    public void SingleLineDiagram_GrowsVerticallyForDenseProjects()
    {
        var (cfg, _, _, _) = BuildSampleProject();
        var baseSize = SingleLineDiagramRenderer.Measure(cfg);
        cfg.OutgoingFeeders.AddRange(new[]
        {
            new OutgoingFeederConfig { Number = 3, DeviceType = "АВ", Manufacturer = "IEK", Model = "ВА88", Nominal = 250 },
            new OutgoingFeederConfig { Number = 4, DeviceType = "АВ", Manufacturer = "IEK", Model = "ВА88", Nominal = 160 },
            new OutgoingFeederConfig { Number = 5, DeviceType = "АВ", Manufacturer = "IEK", Model = "ВА88", Nominal = 160 },
            new OutgoingFeederConfig { Number = 6, DeviceType = "АВ", Manufacturer = "IEK", Model = "ВА88", Nominal = 160 },
            new OutgoingFeederConfig { Number = 7, DeviceType = "АВ", Manufacturer = "IEK", Model = "ВА88", Nominal = 100 },
        });

        var size = SingleLineDiagramRenderer.Measure(cfg);

        Assert.Equal(1500, size.Width);
        Assert.True(size.Height > baseSize.Height);
    }

    [Fact]
    public void SingleLineDiagram_ChangesWhenInputDeviceChanges()
    {
        RunSta(() =>
        {
            var (pvrCfg, pvrRes, _, store) = BuildSampleProject();
            pvrCfg.PvrOn = true;
            pvrCfg.ReOn = false;
            pvrCfg.AvInOn = false;
            pvrRes = CalculationEngine.Calculate(pvrCfg, store);

            var avCfg = CloneForDiagram(pvrCfg);
            avCfg.PvrOn = false;
            avCfg.AvInOn = true;
            avCfg.AvInNominal = pvrCfg.PvrNominal;
            var avRes = CalculationEngine.Calculate(avCfg, store);

            var reCfg = CloneForDiagram(pvrCfg);
            reCfg.PvrOn = false;
            reCfg.ReOn = true;
            reCfg.ReNominal = pvrCfg.PvrNominal;
            var reRes = CalculationEngine.Calculate(reCfg, store);

            var pvrPng = SingleLineDiagramRenderer.RenderPng(pvrCfg, pvrRes, store);
            var avPng = SingleLineDiagramRenderer.RenderPng(avCfg, avRes, store);
            var rePng = SingleLineDiagramRenderer.RenderPng(reCfg, reRes, store);

            Assert.NotEqual(pvrPng, avPng);
            Assert.NotEqual(pvrPng, rePng);
            Assert.NotEqual(avPng, rePng);
        });
    }

    [Fact]
    public void SingleLineDiagram_DrawsOutgoingCurrentTransformersWhenFeederHasMeter()
    {
        RunSta(() =>
        {
            var (baseCfg, baseRes, _, store) = BuildSampleProject();
            foreach (var feeder in baseCfg.OutgoingFeeders)
            {
                feeder.HasMeter = false;
                feeder.TtRatio = "";
            }
            baseRes = CalculationEngine.Calculate(baseCfg, store);

            var meteredCfg = CloneForDiagram(baseCfg);
            meteredCfg.OutgoingFeeders[0].HasMeter = true;
            meteredCfg.OutgoingFeeders[0].TtRatio = "600/5";
            var meteredRes = CalculationEngine.Calculate(meteredCfg, store);

            var plainPng = SingleLineDiagramRenderer.RenderPng(baseCfg, baseRes, store);
            var meteredPng = SingleLineDiagramRenderer.RenderPng(meteredCfg, meteredRes, store);

            Assert.NotEqual(plainPng, meteredPng);
            Assert.True(meteredPng.Length > plainPng.Length);
        });
    }

    [Fact]
    public void SingleLineDiagram_RendersAuxiliaryBranchThroughDiagramModelPath()
    {
        RunSta(() =>
        {
            var (baseCfg, baseRes, _, store) = BuildSampleProject();
            baseCfg.AuxiliaryNeeds = new AuxiliaryNeedsConfig();
            var plainPng = SingleLineDiagramRenderer.RenderPng(baseCfg, baseRes, store);

            var auxCfg = CloneForDiagram(baseCfg);
            auxCfg.AuxiliaryNeeds = new AuxiliaryNeedsConfig
            {
                HasAuxiliaryCabinet = true,
                CabinetModel = "ЩСН-0,4",
                MainBreakerNominal = 25,
                HasLighting = true,
                LightingControlType = LightingControlType.PhotoRelay,
                SocketEnabled = true,
            };
            var auxRes = CalculationEngine.Calculate(auxCfg, store);
            var auxPng = SingleLineDiagramRenderer.RenderPng(auxCfg, auxRes, store);

            Assert.NotEqual(plainPng, auxPng);
            Assert.True(auxPng.Length > plainPng.Length);
        });
    }

    [Fact]
    public void SingleLineDiagram_RendersMeteringSheetWhenMeteringExists()
    {
        RunSta(() =>
        {
            var (cfg, res, _, store) = BuildSampleProject();

            var sheets = SingleLineDiagramRenderer.RenderPngSheets(cfg, res, store);

            Assert.Contains(sheets, s => s.Name == "Однолинейная схема" && s.Png.Length > 20_000);
            Assert.Contains(sheets, s => s.Name == "Цепи учета" && s.Png.Length > 15_000);
        });
    }

    [Fact]
    public void ExcelExport_DoesNotAddDiagramSheetWhileSchemesAreDisabled()
    {
        RunSta(() =>
        {
            var (cfg, res, docs, store) = BuildSampleProject();
            var path = TempFile(".xlsx");

            try
            {
                ExcelExporter.Export(path, cfg, res, docs, store);

                using var workbook = new XLWorkbook(path);
                Assert.False(workbook.TryGetWorksheet("Схема", out _));
                Assert.False(workbook.TryGetWorksheet("Цепи учета", out _));
            }
            finally
            {
                TryDelete(path);
            }
        });
    }

    [Fact]
    public void PdfExport_DoesNotAppendDiagramWhileSchemesAreDisabled()
    {
        RunSta(() =>
        {
            var (cfg, res, docs, store) = BuildSampleProject();
            var docsOnlyPath = TempFile(".pdf");
            var withDiagramPath = TempFile(".pdf");

            try
            {
                PdfExporter.Export(docsOnlyPath, docs);
                PdfExporter.Export(withDiagramPath, docs, cfg, res, store);

                var docsOnlySize = new FileInfo(docsOnlyPath).Length;
                var withDiagramSize = new FileInfo(withDiagramPath).Length;
                Assert.InRange(Math.Abs(withDiagramSize - docsOnlySize), 0, 1000);
            }
            finally
            {
                TryDelete(docsOnlyPath);
                TryDelete(withDiagramPath);
            }
        });
    }

    private static (ProjectConfig Config, CalculationResult Result, List<GeneratedDocument> Documents, CatalogStore Store) BuildSampleProject()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = new ProjectConfig
        {
            ProjectName = "Тест схемы КТПН",
            GridCompany = "АО МРСК",
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            SteelType = "ОЦ",
            Thickness = 2.0,
            Channel = "10П",
            BodyColor = "RAL 7035",
            DoorColor = "RAL 7035",
            Voltage = "6 кВ",
            RuvnType = "Тупиковая",
            RuvnSwitch = "РВЗ",
            RuvnSwitchNominal = 630,
            FuseType = "ПКТ",
            FuseNominal = "31.5А",
            RuvnExecution = "Воздушный",
            RuvnSurgeArrester = true,
            PvrOn = true,
            PvrNominal = 630,
            PvrManufacturer = "CHINT",
            RunnSurgeArrester = true,
            HasMeter = true,
            HasCt = true,
            CtRatio = "600/5",
            AvOn = true,
            AvQty = 2,
            AvBrand = "IEK",
            OutgoingExecution = "Воздушный",
            OutgoingFeeders =
            {
                new OutgoingFeederConfig
                {
                    Number = 1,
                    DeviceType = "АВ",
                    Manufacturer = "IEK",
                    Model = "ВА88",
                    Nominal = 630,
                    TtRatio = "600/5",
                    HasMeter = true,
                },
                new OutgoingFeederConfig
                {
                    Number = 2,
                    DeviceType = "АВ",
                    Manufacturer = "IEK",
                    Model = "ВА88",
                    Nominal = 400,
                },
            },
        };

        var res = CalculationEngine.Calculate(cfg, store);
        var docs = new List<GeneratedDocument>
        {
            DocumentBuilder.BuildProductionOrder(cfg, res, store, DocTemplates.Load(DataDir)),
            DocumentBuilder.BuildPassport(cfg, res, store),
            DocumentBuilder.BuildChecklist(cfg, res, store, DocTemplates.Load(DataDir)),
        };
        return (cfg, res, docs, store);
    }

    private static ProjectConfig CloneForDiagram(ProjectConfig cfg)
    {
        var clone = cfg.Clone();
        clone.OutgoingFeeders = cfg.OutgoingFeeders
            .Select(f => new OutgoingFeederConfig
            {
                Number = f.Number,
                DeviceType = f.DeviceType,
                Manufacturer = f.Manufacturer,
                Model = f.Model,
                Nominal = f.Nominal,
                TtRatio = f.TtRatio,
                HasMeter = f.HasMeter,
            })
            .ToList();
        return clone;
    }

    private static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error is not null)
            throw error;
    }

    private static string TempFile(string extension) =>
        Path.Combine(Path.GetTempPath(), $"ktpn_test_{Guid.NewGuid():N}{extension}");

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
