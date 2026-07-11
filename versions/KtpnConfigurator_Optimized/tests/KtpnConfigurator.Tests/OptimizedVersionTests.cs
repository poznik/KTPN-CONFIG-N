using System.IO;
using ClosedXML.Excel;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.App.ViewModels;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Documents;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;
using Xunit;

namespace KtpnConfigurator.Tests;

public class OptimizedVersionTests
{
    private static string DataDir => Path.Combine(AppContext.BaseDirectory, "Data");

    [Fact]
    public void ElectricalSelection_SynchronizesInputCtAndOutgoingBreaker()
    {
        var store = CatalogStore.Load(DataDir);
        var config = new ProjectConfig
        {
            Mark = "ТМГ-400 (Алагеум)",
            AutoElectricalSelection = true,
            PvrOn = true,
            PvrNominal = 250,
            HasCt = true,
            CtRatio = "100/5",
            OutgoingFeeders =
            {
                new OutgoingFeederConfig
                {
                    Number = 1,
                    DeviceType = "АВ",
                    Manufacturer = "IEK",
                    Model = "ВА88",
                    Nominal = 1000,
                    MeteringType = "Коммерческий",
                    HasMeter = true,
                },
            },
        };

        var changes = ElectricalSelectionService.Apply(config, store);

        Assert.NotEmpty(changes);
        Assert.True(config.PvrNominal >= store.GetTransformer(config.Mark)!.RatedCurrentA);
        Assert.True(config.OutgoingFeeders[0].Nominal <= config.PvrNominal);
        Assert.False(string.IsNullOrWhiteSpace(config.CtRatio));
        Assert.False(string.IsNullOrWhiteSpace(config.OutgoingFeeders[0].TtRatio));
    }

    [Fact]
    public void ElectricalSelection_RespectsManualMode()
    {
        var store = CatalogStore.Load(DataDir);
        var config = new ProjectConfig
        {
            Mark = "ТМГ-400 (Алагеум)",
            AutoElectricalSelection = false,
            PvrOn = true,
            PvrNominal = 250,
        };

        Assert.Empty(ElectricalSelectionService.Apply(config, store));
        Assert.Equal(250, config.PvrNominal);
    }

    [Theory]
    [InlineData("ТМГ-25 (Алагеум)", 160, "150/5")]
    [InlineData("ТМГ-160 (Алагеум)", 250, "250/5")]
    [InlineData("ТМГ-400 (Алагеум)", 630, "600/5")]
    public void ElectricalSelection_UsesSelectedManufacturerCatalog(
        string transformerMark,
        int expectedInput,
        string expectedCt)
    {
        var store = CatalogStore.Load(DataDir);
        var config = new ProjectConfig
        {
            Mark = transformerMark,
            AutoElectricalSelection = true,
            PvrOn = true,
            PvrManufacturer = "CHINT",
            PvrNominal = 630,
            HasCt = true,
            CtRatio = "100/5",
        };

        ElectricalSelectionService.Apply(config, store);

        Assert.Equal(expectedInput, config.PvrNominal);
        Assert.Equal(expectedCt, config.CtRatio);
    }

    [Fact]
    public void ElectricalSelection_DoesNotInventUnavailableInputNominal()
    {
        var store = CatalogStore.Load(DataDir);
        var config = new ProjectConfig
        {
            Mark = "ТМГ-1000 (Алагеум)",
            AutoElectricalSelection = true,
            PvrOn = true,
            PvrManufacturer = "CHINT",
            PvrNominal = 630,
        };

        ElectricalSelectionService.Apply(config, store);
        var result = CalculationEngine.Calculate(config, store);

        Assert.Equal(630, config.PvrNominal);
        Assert.Contains(result.Messages, message => message.Severity == Severity.Error
            && message.Text.Contains("меньше номинального тока", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Messages, message => message.Severity == Severity.Error
            && message.Text.Contains("нет номинала", StringComparison.OrdinalIgnoreCase)
            && message.Text.Contains("CHINT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Calculation_UsesSmallestActiveInputDeviceAsLimit()
    {
        var store = CatalogStore.Load(DataDir);
        var config = new ProjectConfig
        {
            Mark = "ТМГ-400 (Алагеум)",
            PvrOn = true,
            PvrNominal = 630,
            AvInOn = true,
            AvInNominal = 1000,
        };

        var result = CalculationEngine.Calculate(config, store);

        Assert.Equal(630, result.InputNominal);
        Assert.Equal(630, ElectricalSelectionService.LimitingInputNominal(config));
    }

    [Fact]
    public void ManualInputNominal_DisablesAutoSelectionAndIsNotOverwritten()
    {
        var vm = new MainViewModel(AppEnvironment.Load());

        vm.PvrNominal = 400;
        vm.Recalculate();

        Assert.False(vm.AutoElectricalSelection);
        Assert.Equal(400, vm.PvrNominal);
        Assert.Contains("без перезаписи", vm.AutoSelectionSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AutoSelectionSummary_ExplainsInputCtAndBusbarBasis()
    {
        var vm = new MainViewModel(AppEnvironment.Load());

        Assert.Contains("ток НН", vm.AutoSelectionSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ТТ по номиналу ввода", vm.AutoSelectionSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Шины по таблице ТМГ", vm.AutoSelectionSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RuvnTransformerFuse_RemainsOffWhenTypeAndNominalStaySelected()
    {
        var config = new ProjectConfig
        {
            RuvnType = "Тупиковая",
            RuvnTransformerSwitch = "РВЗ",
            RuvnTransformerSwitchNominal = 630,
            RuvnTransformerFuseOn = false,
            RuvnTransformerFuseType = "ПКТ-101",
            RuvnTransformerFuseNominal = "50А",
        };

        var branch = Assert.Single(RuvnEngineering.Branches(config));

        Assert.False(branch.FuseOn);
    }

    [Fact]
    public void CurrentProjectVersion_PreservesDisabledRuvnFuseAfterSaveAndLoad()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ktpn_fuse_state_{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "project.ktpn");
        Directory.CreateDirectory(directory);
        try
        {
            var config = new ProjectConfig
            {
                RuvnTransformerFuseOn = false,
                RuvnTransformerFuseType = "ПКТ-101",
                RuvnTransformerFuseNominal = "50А",
            };
            ProjectStorage.Save(config, path);

            Assert.False(ProjectStorage.Load(path).RuvnTransformerFuseOn);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AuxiliaryOpsLoops_CanBeChanged()
    {
        var vm = new MainViewModel(AppEnvironment.Load());

        vm.OpsLoops = 7;

        Assert.Equal(7, vm.OpsLoops);
        Assert.Equal(7, vm.CurrentConfig.AuxiliaryNeeds.OpsLoops);
    }

    [Fact]
    public void AuxiliaryBranchDefaults_UseCompatibleSmallBreakerNominals()
    {
        var vm = new MainViewModel(AppEnvironment.Load());

        Assert.Contains(vm.SocketBreakerNominal, vm.SocketBreakerNominals);
        Assert.Contains(vm.HeatingBreakerNominal, vm.HeatingBreakerNominals);
        Assert.Contains(vm.VentilationBreakerNominal, vm.VentilationBreakerNominals);
        Assert.True(vm.SocketBreakerNominal <= 16);
        Assert.True(vm.HeatingBreakerNominal <= 16);
        Assert.True(vm.VentilationBreakerNominal <= 16);
    }

    [Fact]
    public void OutgoingFeederModelChange_NormalizesUnsupportedNominal()
    {
        var vm = new MainViewModel(AppEnvironment.Load());
        var feeder = Assert.Single(vm.OutgoingFeeders.Take(1));
        feeder.Nominal = 1000;
        feeder.Manufacturer = "EKF";
        feeder.Model = "PROxima";

        Assert.Equal(630, feeder.Nominal);
        Assert.Contains(feeder.Nominal, feeder.Nominals);
    }

    [Fact]
    public void DocumentsDescribeAllActiveInputDevices()
    {
        var config = new ProjectConfig
        {
            PvrOn = true,
            PvrNominal = 630,
            PvrManufacturer = "CHINT",
            AvInOn = true,
            AvInNominal = 1000,
            AvInManufacturer = "Контактор",
        };

        var description = DocumentBuilder.InputDeviceDescription(config);

        Assert.Contains("ПВР/NH 630 А", description);
        Assert.Contains("Вводной АВ 1000 А", description);
    }

    [Fact]
    public void ValidationRejectsInputNominalMissingFromManufacturerCatalog()
    {
        var store = CatalogStore.Load(DataDir);
        var config = new ProjectConfig
        {
            Mark = "ТМГ-400 (Алагеум)",
            AutoElectricalSelection = false,
            PvrOn = true,
            PvrManufacturer = "CHINT",
            PvrNominal = 1000,
        };

        var result = CalculationEngine.Calculate(config, store);

        Assert.Contains(result.Messages, message => message.Severity == Severity.Error
            && message.Text.Contains("отсутствует в базе", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("РУВН: не выбран ПКТ", "РУВН", 2)]
    [InlineData("Отходящий автомат больше ввода", "РУНН", 3)]
    [InlineData("Для ЩСН не выбран автомат", "Собственные нужды", 4)]
    [InlineData("Не выбран трансформатор", "Трансформатор и корпус", 1)]
    public void ValidationClassifier_RoutesMessageToExpectedTab(string text, string section, int tabIndex)
    {
        Assert.Equal((section, tabIndex), ValidationMessageClassifier.Classify(text));
    }

    [Fact]
    public void DocumentPackage_RemovesEmptyRows()
    {
        var document = new GeneratedDocument
        {
            Sections =
            {
                new DocSection
                {
                    Name = "Пустой",
                    Rows = { new DocRow("Поле", "") },
                },
                new DocSection
                {
                    Name = "Заполненный",
                    Rows = { new DocRow("Поле", "Значение") },
                },
            },
        };

        document.RemoveEmptyRows();

        Assert.Single(document.Sections);
        Assert.Equal("Заполненный", document.Sections[0].Name);
    }

    [Fact]
    public void ProjectStorage_CreatesBackupAndIncrementsRevision()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ktpn_optimized_{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "project.ktpn");
        Directory.CreateDirectory(directory);
        try
        {
            var config = new ProjectConfig { ProjectName = "Первая версия", Revision = 1 };
            ProjectStorage.Save(config, path);
            config.ProjectName = "Вторая версия";
            ProjectStorage.Save(config, path);

            Assert.True(File.Exists(path + ".bak"));
            Assert.Equal("Вторая версия", ProjectStorage.Load(path).ProjectName);
            Assert.Equal("Первая версия", ProjectStorage.Load(path + ".bak").ProjectName);
            Assert.True(config.Revision >= 2);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void EquipmentAudit_SeparatesVerifiedAndUnverifiedItems()
    {
        var audit = EquipmentDatabaseAudit.Analyze(CatalogStore.Load(DataDir));

        Assert.True(audit.Total > 0);
        Assert.True(audit.Verified > 0);
        Assert.True(audit.NeedsVerification > 0);
        Assert.Equal(audit.Total, audit.Verified + audit.NeedsVerification);
    }

    [Fact]
    public void Save_RemembersCurrentPath_AndSaveAsChangesIt()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ktpn_save_flow_{Guid.NewGuid():N}");
        var firstPath = Path.Combine(directory, "first.ktpn");
        var secondPath = Path.Combine(directory, "second.ktpn");
        Directory.CreateDirectory(directory);
        try
        {
            var vm = new MainViewModel(AppEnvironment.Load());
            var dialogCalls = 0;
            vm.AskSavePath = () =>
            {
                dialogCalls++;
                return firstPath;
            };

            Assert.True(vm.IsDirty);
            vm.SaveProjectCommand.Execute(null);

            Assert.False(vm.IsDirty);
            Assert.Equal(Path.GetFullPath(firstPath), vm.CurrentProjectPath);
            Assert.Equal(1, dialogCalls);

            vm.ProjectName = "Изменённый проект";
            Assert.True(vm.IsDirty);
            vm.SaveProjectCommand.Execute(null);

            Assert.Equal(1, dialogCalls);
            Assert.Equal("Изменённый проект", ProjectStorage.Load(firstPath).ProjectName);

            vm.AskSavePath = () => secondPath;
            vm.SaveAsProjectCommand.Execute(null);
            Assert.Equal(Path.GetFullPath(secondPath), vm.CurrentProjectPath);
            Assert.True(File.Exists(secondPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExcelExport_RunsInBackground_AndRestoresCommandState()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ktpn_export_flow_{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "documents.xlsx");
        Directory.CreateDirectory(directory);
        try
        {
            var vm = new MainViewModel(AppEnvironment.Load())
            {
                AskExportPath = _ => path,
            };

            Assert.True(vm.ExportExcelCommand.CanExecute(null));
            vm.ExportExcelCommand.Execute(null);
            Assert.True(vm.IsExporting);
            Assert.False(vm.ExportExcelCommand.CanExecute(null));

            var deadline = DateTime.UtcNow.AddSeconds(20);
            while ((vm.IsExporting || !File.Exists(path)) && DateTime.UtcNow < deadline)
                await Task.Delay(50);

            Assert.False(vm.IsExporting);
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
            Assert.True(vm.ExportExcelCommand.CanExecute(null));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void TabIndicator_UsesBrightErrorStateAndClearText()
    {
        var vm = new MainViewModel(AppEnvironment.Load());
        vm.AutoElectricalSelection = false;
        vm.CurrentConfig.PvrOn = true;
        vm.CurrentConfig.PvrNominal = 1;
        vm.CurrentConfig.ReOn = false;
        vm.CurrentConfig.AvInOn = false;
        vm.Recalculate();

        Assert.Equal("#E53935", vm.RunnTabStatusColor);
        Assert.Equal("Есть ошибка", vm.RunnTabStatusText);
    }

    [Fact]
    public void InputCtValidation_UsesCircuitBreakerNominalWhenBreakerIsSelected()
    {
        var store = CatalogStore.Load(DataDir);
        var config = new ProjectConfig
        {
            Mark = "ТМГ-400 (Алагеум)",
            AutoElectricalSelection = false,
            PvrOn = true,
            PvrManufacturer = "CHINT",
            PvrNominal = 630,
            AvInOn = true,
            AvInManufacturer = "Контактор",
            AvInNominal = 1000,
            HasCt = true,
            CtRatio = "1000/5",
        };

        var result = CalculationEngine.Calculate(config, store);

        Assert.Equal(1000, ElectricalSelectionService.CurrentTransformerReferenceNominal(config));
        Assert.DoesNotContain(result.Messages, message =>
            message.Text.Contains("ТТ учета на вводе", StringComparison.OrdinalIgnoreCase)
            && message.Text.Contains("рекомендуется", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnknownInputManufacturer_DoesNotReceiveGlobalNominalList()
    {
        var store = CatalogStore.Load(DataDir);

        var nominals = ElectricalSelectionService.InputNominals("ПВР/NH", "Нет в базе", store);

        Assert.Empty(nominals);
    }

    [Fact]
    public void RunnInputAndFeederLists_ExcludeAuxiliaryAndWrongPurposeModels()
    {
        var store = CatalogStore.Load(DataDir);
        var vm = new MainViewModel(AppEnvironment.Load());

        Assert.DoesNotContain("IEK", vm.AvInManufacturers);
        Assert.Empty(ElectricalSelectionService.InputNominals("АВ", "IEK", store));
        Assert.DoesNotContain(vm.ModelsForFeeder("АВ", "IEK"), model =>
            model.Contains("ВА47-29", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(vm.ModelsForFeeder("АВ", "CHINT"), model =>
            model.Equals("NA8G", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnknownFeederModel_DoesNotReceiveGlobalNominalListAndIsAnError()
    {
        var vm = new MainViewModel(AppEnvironment.Load());
        Assert.Empty(vm.NominalsForFeeder("АВ", "IEK", "Нет в базе"));

        var config = new ProjectConfig
        {
            Mark = "ТМГ-400 (Алагеум)",
            AutoElectricalSelection = false,
            PvrOn = true,
            PvrManufacturer = "CHINT",
            PvrNominal = 630,
            OutgoingFeeders =
            {
                new OutgoingFeederConfig
                {
                    DeviceType = "АВ",
                    Number = 1,
                    Manufacturer = "IEK",
                    Model = "Нет в базе",
                    Nominal = 400,
                },
            },
        };

        var result = CalculationEngine.Calculate(config, CatalogStore.Load(DataDir));

        Assert.Contains(result.Messages, message => message.Severity == Severity.Error
            && message.Text.Contains("отсутствует в базе оборудования", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnabledCurrentTransformer_RequiresRatio()
    {
        var store = CatalogStore.Load(DataDir);
        var config = new ProjectConfig
        {
            Mark = "ТМГ-400 (Алагеум)",
            AutoElectricalSelection = false,
            PvrOn = true,
            PvrManufacturer = "CHINT",
            PvrNominal = 630,
            HasCtKip = true,
            CtKipRatio = "",
        };

        var result = CalculationEngine.Calculate(config, store);

        Assert.Contains(result.Messages, message => message.Severity == Severity.Error
            && message.Text.Contains("ТТ КИП", StringComparison.OrdinalIgnoreCase)
            && message.Text.Contains("не задан коэффициент", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RemovedServicePlatform_DoesNotAffectMassOrDocuments()
    {
        var store = CatalogStore.Load(DataDir);
        var config = new ProjectConfig
        {
            Mark = "ТМГ-400 (Алагеум)",
            HasServicePlatform = false,
        };
        var withoutPlatform = CalculationEngine.Calculate(config, store);
        config.HasServicePlatform = true;
        var withLegacyFlag = CalculationEngine.Calculate(config, store);
        var document = DocumentBuilder.BuildProductionOrder(
            config, withLegacyFlag, store, DocTemplates.Load(DataDir)).ToPlainText();

        Assert.Equal(withoutPlatform.AdditionalMassEstimate, withLegacyFlag.AdditionalMassEstimate);
        Assert.DoesNotContain("площадка обслуживания", document, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CurrentTransformer_Supports50To5AndAccuracyClass()
    {
        var vm = new MainViewModel(AppEnvironment.Load());
        Assert.Contains("50/5", vm.TtRatios);
        Assert.Contains("0,5S", vm.TtAccuracyClasses);

        var store = CatalogStore.Load(DataDir);
        var config = new ProjectConfig
        {
            Mark = "ТМГ-400 (Алагеум)",
            HasCt = true,
            CtRatio = "50/5",
            CtAccuracyClass = "0,5S",
        };
        var result = CalculationEngine.Calculate(config, store);
        var order = DocumentBuilder.BuildProductionOrder(config, result, store, DocTemplates.Load(DataDir)).ToPlainText();
        var specification = SpecificationBuilder.GenerateSpecification(config, result, store);

        Assert.Contains("ТТ 50/5, класс точности 0,5S", order);
        Assert.Contains(specification, item => item.Name == "Трансформаторы тока учета"
            && item.Nominal.Contains("50/5", StringComparison.OrdinalIgnoreCase)
            && item.Nominal.Contains("0,5S", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CurrentTransformerAccuracyClass_IsPreservedByProjectStorage()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ktpn_ct_class_{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "project.ktpn");
        Directory.CreateDirectory(directory);
        try
        {
            var config = new ProjectConfig
            {
                CtAccuracyClass = "0,2S",
                CtKipAccuracyClass = "1",
            };
            ProjectStorage.Save(config, path);
            var loaded = ProjectStorage.Load(path);

            Assert.Equal("0,2S", loaded.CtAccuracyClass);
            Assert.Equal("1", loaded.CtKipAccuracyClass);
            Assert.Equal(ProjectConfig.CurrentVersion, loaded.ProjectVersion);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void NhWarnsAboutRiseUntilRiseIsEnabled()
    {
        var store = CatalogStore.Load(DataDir);
        var config = new ProjectConfig
        {
            Mark = "ТМГ-400 (Алагеум)",
            PvrOn = true,
            PvrManufacturer = "CHINT",
            PvrNominal = 630,
            AuxiliaryNeeds = new AuxiliaryNeedsConfig { RieseEnabled = false },
        };

        var withoutRise = CalculationEngine.Calculate(config, store);
        Assert.Contains(withoutRise.Messages, message => message.Severity == Severity.Warning
            && message.Text.Contains("NH", StringComparison.OrdinalIgnoreCase)
            && message.Text.Contains("РИСЭ", StringComparison.OrdinalIgnoreCase));

        config.AuxiliaryNeeds.RieseEnabled = true;
        var withRise = CalculationEngine.Calculate(config, store);
        Assert.DoesNotContain(withRise.Messages, message => message.Text.Contains("NH", StringComparison.OrdinalIgnoreCase)
            && message.Text.Contains("РИСЭ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NhInlineWarning_TracksNhAndRiseSelections()
    {
        var vm = new MainViewModel(AppEnvironment.Load());

        vm.PvrOn = true;
        vm.HasRise = false;
        Assert.True(vm.ShowNhRiseWarning);

        vm.HasRise = true;
        Assert.False(vm.ShowNhRiseWarning);

        vm.HasRise = false;
        vm.PvrOn = false;
        Assert.False(vm.ShowNhRiseWarning);
    }

    [Fact]
    public void ProductionOrder_ProductParametersHaveNoSignatureColumnContent()
    {
        var store = CatalogStore.Load(DataDir);
        var config = new ProjectConfig { Mark = "ТМГ-400 (Алагеум)" };
        var result = CalculationEngine.Calculate(config, store);
        var document = DocumentBuilder.BuildProductionOrder(config, result, store, DocTemplates.Load(DataDir));
        var parameters = Assert.Single(document.Sections, section => section.Name == "Параметры изделия");

        Assert.False(parameters.IsSignatureTable);
        Assert.All(parameters.Rows, row => Assert.True(string.IsNullOrWhiteSpace(row.Note)));
        Assert.DoesNotContain("подпись", parameters.Rows.Select(row => row.Note), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionOrderExcel_ProductParameterValuesSpanRemainingColumns()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ktpn_order_layout_{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "order.xlsx");
        Directory.CreateDirectory(directory);
        try
        {
            var store = CatalogStore.Load(DataDir);
            var config = new ProjectConfig { Mark = "ТМГ-400 (Алагеум)" };
            var result = CalculationEngine.Calculate(config, store);
            var order = DocumentBuilder.BuildProductionOrder(config, result, store, DocTemplates.Load(DataDir));
            ExcelExporter.Export(path, config, result, new[] { order }, store);

            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheets.Single(worksheet => worksheet.Name != "Сводка расчёта");
            var labelCell = sheet.CellsUsed().Single(cell => cell.GetString() == "Заводской номер / дата");
            var row = labelCell.Address.RowNumber;
            Assert.Contains(sheet.MergedRanges, range =>
                range.RangeAddress.FirstAddress.RowNumber == row
                && range.RangeAddress.FirstAddress.ColumnNumber == 2
                && range.RangeAddress.LastAddress.ColumnNumber == 3);
            Assert.DoesNotContain(sheet.Row(row).CellsUsed(), cell =>
                cell.GetString().Equals("подпись", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ChecklistExcel_IsPortraitWithoutCommentColumnOrFill()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ktpn_checklist_layout_{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "checklist.xlsx");
        Directory.CreateDirectory(directory);
        try
        {
            var store = CatalogStore.Load(DataDir);
            var config = new ProjectConfig { Mark = "ТМГ-400 (Алагеум)" };
            var result = CalculationEngine.Calculate(config, store);
            var checklist = DocumentBuilder.BuildChecklist(config, result, store, DocTemplates.Load(DataDir));
            ExcelExporter.Export(path, config, result, new[] { checklist }, store);

            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheets.Single(worksheet => worksheet.Name != "Сводка расчёта");
            var used = sheet.RangeUsed()!;

            Assert.Equal(XLPageOrientation.Portrait, sheet.PageSetup.PageOrientation);
            Assert.True(used.LastColumn().ColumnNumber() <= 4);
            Assert.DoesNotContain(used.Cells(), cell =>
                cell.GetString().Equals("Комментарий", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(used.Cells(), cell => cell.Style.Fill.PatternType != XLFillPatternValues.None);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
