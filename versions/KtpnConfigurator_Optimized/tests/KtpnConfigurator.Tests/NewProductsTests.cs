using System.IO;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.App.ViewModels;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Documents;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;
using Xunit;

namespace KtpnConfigurator.Tests;

public class NewProductsTests
{
    private static string DataDir => Path.Combine(AppContext.BaseDirectory, "Data");
    private static CatalogStore Store => CatalogStore.Load(DataDir);

    [Fact]
    public void EveryRegisteredProduct_IsAvailableInFirstRelease()
    {
        Assert.Equal(7, ProductRegistry.All.Count);
        Assert.All(ProductRegistry.All, product => Assert.Equal(ProductAvailability.Available, product.Availability));
    }

    [Fact]
    public void DoubleKtpn_CalculatesTwoTransformersAndBuildsDedicatedDocuments()
    {
        var store = Store;
        var project = new ProjectConfig
        {
            ProductTypeId = ProductTypeIds.DoubleKtpn,
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            Channel = "10П",
            SteelType = "Оцинкованная",
            DoubleKtpn = new DoubleKtpnConfig
            {
                SecondTransformerManufacturer = "Алагеум",
                SecondTransformerMark = "ТМГ-400 (Алагеум)",
                Section1InputNominalA = 630,
                Section2InputNominalA = 630,
                SectionCouplerNominalA = 630,
                AutomaticTransferEnabled = true,
            },
        };

        var result = CalculationEngine.Calculate(project, store);
        var documents = DocumentPackageBuilder.BuildAll(project, result, store, DocTemplates.Load(DataDir));

        Assert.True(result.LengthFinal > 5000);
        Assert.True(result.TransformerMass > store.GetTransformer(project.Mark)!.MassKg);
        Assert.Equal(630, result.Section1InputNominalA);
        Assert.Equal(630, result.Section2InputNominalA);
        Assert.True(result.Section1ShortCircuitCurrentKa > 0);
        Assert.True(result.Section2EstimatedMass > result.Section2TransformerMass);
        Assert.DoesNotContain(result.Messages, message => message.Severity == Severity.Error);
        var order = documents.Single(document => document.Kind == GeneratedDocumentKind.ProductionOrder).ToPlainText();
        Assert.Contains("Т2", order);
        Assert.Contains("Секция 1", order);
        Assert.Contains("Ориентировочный КЗ", order);
        Assert.Contains("ШС1", documents.Single(document => document.Kind == GeneratedDocumentKind.Specification).ToPlainText());
        Assert.Contains("АВР", documents.Single(document => document.Kind == GeneratedDocumentKind.Checklist).ToPlainText());
    }

    [Fact]
    public void DoubleKtpn_CalculatesTransformerSectionsIndependently()
    {
        var project = new ProjectConfig
        {
            ProductTypeId = ProductTypeIds.DoubleKtpn,
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            DoubleKtpn = new DoubleKtpnConfig
            {
                SecondTransformerManufacturer = "Алагеум",
                SecondTransformerMark = "ТМГ-630 (Алагеум)",
                Section1InputNominalA = 630,
                Section2InputNominalA = 1000,
                SectionCouplerNominalA = 1000,
                AutomaticTransferEnabled = true,
            },
        };

        var result = CalculationEngine.Calculate(project, Store);

        Assert.True(result.Section2RatedCurrentA > result.Section1RatedCurrentA);
        Assert.True(result.Section2ShortCircuitCurrentKa > result.Section1ShortCircuitCurrentKa);
        Assert.NotEqual(result.Section1BusbarLv, result.Section2BusbarLv);
        Assert.Contains(result.Messages, message => message.Severity == Severity.Warning
            && message.Text.Contains("КЗ", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Messages, message => message.Severity == Severity.Error);
    }

    [Theory]
    [InlineData(ProductTypeIds.Nku, "НКУ")]
    [InlineData(ProductTypeIds.Shcho, "ЩО")]
    [InlineData(ProductTypeIds.Vru, "ВРУ")]
    public void LowVoltageProducts_CreatePanelsCalculateBusbarsAndDocuments(string productTypeId, string title)
    {
        var project = new ProjectConfig { ProductTypeId = productTypeId };
        ProductConfigurationDefaults.Normalize(project);

        var result = CalculationEngine.Calculate(project, Store);
        var documents = DocumentPackageBuilder.BuildAll(project, result, Store, DocTemplates.Load(DataDir));

        Assert.NotEmpty(project.LowVoltageAssembly.Panels);
        Assert.True(result.LengthFinal > 0);
        Assert.True(result.BusbarMassEstimate > 0);
        Assert.True(result.GrossMassEstimated > result.GrossMass);
        Assert.Contains("предварительно", result.BusbarLv, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.Messages, message => message.Text.Contains("ГОСТ IEC 61439", StringComparison.OrdinalIgnoreCase));
        Assert.All(documents, document => Assert.Contains(title, document.Subtitle));
        var specification = documents.Single(document => document.Kind == GeneratedDocumentKind.Specification).ToPlainText();
        Assert.Contains("Панели", specification);
        Assert.Contains("линий", specification);
    }

    [Theory]
    [InlineData(ProductTypeIds.Kso, "КСО")]
    [InlineData(ProductTypeIds.Kru, "КРУ")]
    public void MediumVoltageProducts_CreateCellLineupAndDedicatedChecks(string productTypeId, string title)
    {
        var project = new ProjectConfig { ProductTypeId = productTypeId };
        ProductConfigurationDefaults.Normalize(project);

        var result = CalculationEngine.Calculate(project, Store);
        var documents = DocumentPackageBuilder.BuildAll(project, result, Store, DocTemplates.Load(DataDir));

        Assert.NotEmpty(project.MediumVoltageSwitchgear.Cells);
        Assert.True(result.LengthFinal > 0);
        Assert.True(result.BusbarMassEstimate > 0);
        Assert.True(result.GrossMassEstimated > result.GrossMass);
        Assert.Contains(result.Messages, message => message.Text.Contains(title, StringComparison.OrdinalIgnoreCase));
        var specification = documents.Single(document => document.Kind == GeneratedDocumentKind.Specification).ToPlainText();
        Assert.Contains("Ячейки", specification);
        Assert.Contains("разрывы", specification);
        Assert.Contains("РЗА", documents.Single(document => document.Kind == GeneratedDocumentKind.Checklist).ToPlainText());
    }

    [Fact]
    public void ProjectClone_DoesNotShareNewProductLineups()
    {
        var project = new ProjectConfig { ProductTypeId = ProductTypeIds.Kru };
        ProductConfigurationDefaults.Normalize(project);

        var clone = project.Clone();
        clone.MediumVoltageSwitchgear.Cells[0].Purpose = "Изменено";
        clone.LowVoltageAssembly.Panels.Add(new LowVoltagePanelConfig());
        clone.DoubleKtpn.SectionCouplerNominalA = 1600;

        Assert.NotEqual("Изменено", project.MediumVoltageSwitchgear.Cells[0].Purpose);
        Assert.Empty(project.LowVoltageAssembly.Panels);
        Assert.Equal(630, project.DoubleKtpn.SectionCouplerNominalA);
    }

    [Fact]
    public void ProjectStorage_MigratesVersionFourAndPreservesProductConfiguration()
    {
        var path = Path.Combine(Path.GetTempPath(), $"new_product_{Guid.NewGuid():N}.ktpn");
        File.WriteAllText(path, """
        {
          "ProjectVersion": 4,
          "ProductTypeId": "nku.shcho",
          "LowVoltageAssembly": {
            "RatedBusCurrentA": 1600,
            "Panels": [ { "Number": 1, "PanelType": "Вводная", "RatedCurrentA": 1600 } ]
          }
        }
        """);

        try
        {
            var project = ProjectStorage.Load(path);
            Assert.Equal(ProjectConfig.CurrentVersion, project.ProjectVersion);
            Assert.Equal(ProductTypeIds.Shcho, project.ProductTypeId);
            Assert.Equal(1600, project.LowVoltageAssembly.RatedBusCurrentA);
            Assert.Single(project.LowVoltageAssembly.Panels);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MainViewModel_SwitchesVisibleWorkflowByProductFamily()
    {
        var viewModel = new MainViewModel(AppEnvironment.Load());
        viewModel.SelectedProduct = ProductRegistry.Find(ProductTypeIds.Kru)!;

        Assert.False(viewModel.ShowKtpnTabs);
        Assert.True(viewModel.ShowProductConfigurationTab);
        Assert.True(viewModel.ShowMediumVoltageConfiguration);
        Assert.Contains("КРУ", viewModel.ApplicationTitle);
        Assert.NotEmpty(viewModel.MvCells);
    }

    [Fact]
    public void DoubleKtpn_ExposesIndependentTransformerSelectionsAndUpdatesSecondInput()
    {
        var viewModel = new MainViewModel(AppEnvironment.Load());
        viewModel.SelectedProduct = ProductRegistry.Find(ProductTypeIds.DoubleKtpn)!;

        Assert.NotEmpty(viewModel.Manufacturers);
        Assert.NotEmpty(viewModel.Marks);
        Assert.NotEmpty(viewModel.SecondTransformerMarks);

        var secondMark = viewModel.SecondTransformerMarks.Last();
        viewModel.SecondTransformerMark = secondMark;
        Assert.Equal(secondMark, viewModel.CurrentConfig.DoubleKtpn.SecondTransformerMark);
        Assert.True(viewModel.Section2InputNominal > 0);
    }

    [Fact]
    public void LowVoltagePanelRows_AreDirectlyEditableDuplicatedAndResettable()
    {
        var viewModel = new MainViewModel(AppEnvironment.Load());
        viewModel.SelectedProduct = ProductRegistry.Find(ProductTypeIds.Nku)!;
        var first = viewModel.LvPanels[0];

        first.Purpose = "Новая нагрузка";
        first.RatedCurrent = 400;
        var before = viewModel.LvPanels.Count;
        first.DuplicateCommand.Execute(null);

        Assert.Equal("Новая нагрузка", viewModel.CurrentConfig.LowVoltageAssembly.Panels[0].Purpose);
        Assert.Equal(before + 1, viewModel.LvPanels.Count);

        viewModel.ResetLvPanelsCommand.Execute(null);
        Assert.Equal(3, viewModel.LvPanels.Count);
        Assert.Equal("Вводная", viewModel.LvPanels[0].PanelType);
    }

    [Fact]
    public void ProductLineupOptionSources_AreAvailableForEditableRows()
    {
        var viewModel = new MainViewModel(AppEnvironment.Load());

        Assert.Contains("ПВР/NH", viewModel.LvMainDevices);
        Assert.Contains("NH2", viewModel.LvDeviceModels);
        Assert.Contains("ВВ/TEL-10", viewModel.MvDeviceModels);
        Assert.Contains("50/5", viewModel.MvCtRatios);
        Assert.Contains("РВЗ", viewModel.MvVisibleBreakOptions);
    }

    [Fact]
    public void LowVoltagePanelType_AutofillsDeviceAndEquipmentStatus()
    {
        var viewModel = new MainViewModel(AppEnvironment.Load());
        viewModel.SelectedProduct = ProductRegistry.Find(ProductTypeIds.Nku)!;
        var panel = viewModel.LvPanels[0];

        panel.PanelType = "Учетная";

        Assert.Equal("Коммерческий учет", panel.Purpose);
        Assert.Equal("Счетчик, ТТ", panel.MainDevice);
        Assert.Equal("Узел учета с ТТ", panel.ModelName);
        Assert.True(panel.HasMetering);
        Assert.Equal("Задано проектом", panel.EquipmentStatus);

        panel.MainDevice = "ПВР/NH";

        Assert.Contains("NH", panel.ModelName);
        Assert.Equal("Требуется проверка", panel.EquipmentStatus);
    }

    [Fact]
    public void MediumVoltageCellPurpose_AutofillsProtectionAndVisibleBreaks()
    {
        var viewModel = new MainViewModel(AppEnvironment.Load());
        viewModel.SelectedProduct = ProductRegistry.Find(ProductTypeIds.Kso)!;
        var cell = viewModel.MvCells[0];

        cell.Purpose = "Отходящая линия";

        Assert.Equal("Вакуумный выключатель", cell.MainDevice);
        Assert.Equal("ВВ/TEL-10", cell.DeviceModel);
        Assert.Equal("600/5", cell.CtRatio);
        Assert.Contains("МТЗ", cell.RelayProtection);
        Assert.Equal("РВЗ", cell.VisibleBreakBefore);
        Assert.Equal("РВЗ", cell.VisibleBreakAfter);
        Assert.Equal("Требуется проверка", cell.EquipmentStatus);
    }

    [Fact]
    public void LowVoltagePanelRows_PreserveMeteringSurgeAndCircuitCountInDocuments()
    {
        var project = new ProjectConfig { ProductTypeId = ProductTypeIds.Vru };
        ProductConfigurationDefaults.Normalize(project);
        ProductConfigurationDefaults.ApplyLowVoltageTemplate(project, "ВРУ: два ввода + АВР");
        var panel = project.LowVoltageAssembly.Panels[0];
        panel.CircuitCount = 12;
        panel.HasMetering = true;
        panel.HasSurgeProtection = true;
        panel.Model = "Узел учета";

        var result = CalculationEngine.Calculate(project, Store);
        var documents = DocumentPackageBuilder.BuildAll(project, result, Store, DocTemplates.Load(DataDir));
        var order = documents.Single(document => document.Kind == GeneratedDocumentKind.ProductionOrder).ToPlainText();

        Assert.Contains("линий 12", order);
        Assert.Contains("учет да", order);
        Assert.Contains("ОПН да", order);
        Assert.Contains("статус", order);
        Assert.Contains("ВРУ: два ввода + АВР", order);
        Assert.DoesNotContain(result.Messages, message => message.Severity == Severity.Error);
    }

    [Fact]
    public void MediumVoltageCellRows_PreserveCtVtRelayAndVisibleBreaksInDocuments()
    {
        var project = new ProjectConfig { ProductTypeId = ProductTypeIds.Kru };
        ProductConfigurationDefaults.Normalize(project);
        ProductConfigurationDefaults.ApplyMediumVoltageTemplate(project, "КРУ: две секции с АВР");
        var cell = project.MediumVoltageSwitchgear.Cells[0];
        cell.CtAccuracyClass = "10P/0,5";
        cell.HasVoltageTransformer = true;
        cell.VoltageTransformerModel = "НАМИ-10";
        cell.RelayTerminal = "Сириус-2-Л";
        cell.VisibleBreakBefore = "РВЗ до выключателя";
        cell.VisibleBreakAfter = "РВЗ после выключателя";

        var result = CalculationEngine.Calculate(project, Store);
        var documents = DocumentPackageBuilder.BuildAll(project, result, Store, DocTemplates.Load(DataDir));
        var specification = documents.Single(document => document.Kind == GeneratedDocumentKind.Specification).ToPlainText();

        Assert.Contains("10P/0,5", specification);
        Assert.Contains("НАМИ-10", specification);
        Assert.Contains("Сириус-2-Л", specification);
        Assert.Contains("РВЗ до выключателя", specification);
        Assert.Contains("РВЗ после выключателя", specification);
        Assert.Contains("КРУ: две секции с АВР", documents.Single(document => document.Kind == GeneratedDocumentKind.Passport).ToPlainText());
    }

    [Fact]
    public void ProductTemplates_RebuildLineupsAndElectricalParameters()
    {
        var nku = new ProjectConfig { ProductTypeId = ProductTypeIds.Nku };
        ProductConfigurationDefaults.ApplyLowVoltageTemplate(nku, "НКУ: два ввода + секционный аппарат");

        Assert.Equal("НКУ: два ввода + секционный аппарат", nku.LowVoltageAssembly.LineupTemplate);
        Assert.Equal(2, nku.LowVoltageAssembly.SectionCount);
        Assert.Equal(1000, nku.LowVoltageAssembly.RatedBusCurrentA);
        Assert.Equal(6, nku.LowVoltageAssembly.Panels.Count);
        Assert.Contains(nku.LowVoltageAssembly.Panels, panel => panel.PanelType == "Секционная");

        var kru = new ProjectConfig { ProductTypeId = ProductTypeIds.Kru };
        ProductConfigurationDefaults.ApplyMediumVoltageTemplate(kru, "КРУ: две секции с АВР");

        Assert.Equal("КРУ: две секции с АВР", kru.MediumVoltageSwitchgear.LineupTemplate);
        Assert.Equal("Выдвижное", kru.MediumVoltageSwitchgear.CellExecution);
        Assert.Equal(31.5, kru.MediumVoltageSwitchgear.DesignShortCircuitCurrentKa);
        Assert.Equal(5, kru.MediumVoltageSwitchgear.Cells.Count);
        Assert.Contains(kru.MediumVoltageSwitchgear.Cells, cell => cell.RelayProtection.Contains("АВР", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProductValidation_FailsWhenDesignShortCircuitExceedsWithstandOrBreakingCurrent()
    {
        var shcho = new ProjectConfig { ProductTypeId = ProductTypeIds.Shcho };
        ProductConfigurationDefaults.ApplyLowVoltageTemplate(shcho, "ЩО: одна секция 400 А");
        shcho.LowVoltageAssembly.DesignShortCircuitCurrentKa = 40;

        var lvResult = CalculationEngine.Calculate(shcho, Store);
        Assert.Contains(lvResult.Messages, message => message.Severity == Severity.Error
            && message.Text.Contains("выше Icw", StringComparison.OrdinalIgnoreCase));

        var kso = new ProjectConfig { ProductTypeId = ProductTypeIds.Kso };
        ProductConfigurationDefaults.ApplyMediumVoltageTemplate(kso, "КСО: ввод + ТН + трансформатор");
        kso.MediumVoltageSwitchgear.DesignShortCircuitCurrentKa = 31.5;

        var mvResult = CalculationEngine.Calculate(kso, Store);
        Assert.Contains(mvResult.Messages, message => message.Severity == Severity.Error
            && message.Text.Contains("выше тока", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Shcho_DefaultsToTwoSectionsAndRejectsUnknownPanelSection()
    {
        var project = new ProjectConfig { ProductTypeId = ProductTypeIds.Shcho };
        ProductConfigurationDefaults.Normalize(project);
        project.LowVoltageAssembly.Panels[0].SectionNumber = 3;

        var result = CalculationEngine.Calculate(project, Store);

        Assert.Equal(2, project.LowVoltageAssembly.SectionCount);
        Assert.Contains(result.Messages, message => message.Severity == Severity.Error
            && message.Text.Contains("несуществующей секции", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProjectStorage_PreservesTransformerPairAndEditablePanelRows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"product_rows_{Guid.NewGuid():N}.ktpn");
        var project = new ProjectConfig { ProductTypeId = ProductTypeIds.Nku };
        ProductConfigurationDefaults.Normalize(project);
        project.DoubleKtpn.SecondTransformerMark = "ТМГ-630 (Алагеум)";
        project.LowVoltageAssembly.Series = "НКУ-Проверка";
        project.LowVoltageAssembly.LineupTemplate = "НКУ: распределительный шкаф";
        project.LowVoltageAssembly.Panels[0].Purpose = "Сохраненная нагрузка";
        project.LowVoltageAssembly.Panels[0].CircuitCount = 9;
        project.LowVoltageAssembly.Panels[0].HasMetering = true;
        project.MediumVoltageSwitchgear.Cells.Add(new MediumVoltageCellConfig
        {
            Number = 1,
            Purpose = "Сохраненная ячейка",
            RelayTerminal = "Сохраненный терминал",
            VisibleBreakBefore = "До",
            VisibleBreakAfter = "После",
        });

        try
        {
            ProjectStorage.Save(project, path);
            var loaded = ProjectStorage.Load(path);

            Assert.Equal("ТМГ-630 (Алагеум)", loaded.DoubleKtpn.SecondTransformerMark);
            Assert.Equal("НКУ-Проверка", loaded.LowVoltageAssembly.Series);
            Assert.Equal("НКУ: распределительный шкаф", loaded.LowVoltageAssembly.LineupTemplate);
            Assert.Equal("Сохраненная нагрузка", loaded.LowVoltageAssembly.Panels[0].Purpose);
            Assert.Equal(9, loaded.LowVoltageAssembly.Panels[0].CircuitCount);
            Assert.True(loaded.LowVoltageAssembly.Panels[0].HasMetering);
            Assert.Equal("Сохраненный терминал", loaded.MediumVoltageSwitchgear.Cells[0].RelayTerminal);
            Assert.Equal("До", loaded.MediumVoltageSwitchgear.Cells[0].VisibleBreakBefore);
            Assert.Equal("После", loaded.MediumVoltageSwitchgear.Cells[0].VisibleBreakAfter);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
