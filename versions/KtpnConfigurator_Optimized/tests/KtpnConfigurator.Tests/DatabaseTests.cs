using System;
using System.Collections.Generic;
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

public class DatabaseTests
{
    private static string DataDir => Path.Combine(AppContext.BaseDirectory, "Data");

    [Fact]
    public void CanLoadNewCatalogs()
    {
        var store = CatalogStore.Load(DataDir);
        Assert.NotNull(store.DeviceModels);
        Assert.True(store.DeviceModels.Count >= 42);
        Assert.Contains(store.DeviceModels, d => d.Type == "Шкаф собственных нужд" && d.SymbolKey == "auxiliaryCabinet");
        Assert.Contains(store.DeviceModels, d => d.Type == "РИСЭ" && d.SourceConfidence == "needsVerification");
        Assert.NotNull(store.DiagramSymbols);
        Assert.True(store.DiagramSymbols.Count > 0);
        Assert.Contains(store.DiagramSymbols, s => s.SymbolKey == "busbar");
        Assert.Contains(store.DiagramSymbols, s => s.SymbolKey == "ground");
        Assert.Contains(store.DiagramSymbols, s => s.SymbolKey == "auxiliaryCabinet");
        Assert.Contains(store.DiagramSymbols, s => s.SymbolKey == "backupPowerSource");
        Assert.NotNull(store.DiagramRules);
        Assert.NotEmpty(store.CustomerProfiles);
        Assert.Contains(store.CustomerProfiles, p => p.Name == "АО ЮРЭСК");
        Assert.Contains(store.CustomerProfiles, p => p.Name == "АО Лукоил");
        Assert.Contains(store.Options.GridCompanies, c => c == "АО МРСК");
        Assert.Contains(store.Options.GridCompanies, c => c == "АО ЮРЭСК");
        Assert.Contains(store.Options.GridCompanies, c => c == "АО Лукоил");
        Assert.Contains(store.Options.GridCompanies, c => c == "АО РЖД");
        Assert.True(store.DiagramRules.FrameMarginsMm.Left > 0);
        Assert.Equal("leftRotated", store.DiagramRules.TitleBlock.Placement);
        Assert.Contains("reference_materials/prim/Схемы", store.DiagramRules.Style.ReferenceStyle);
        Assert.Contains("2145.pdf", store.DiagramRules.Style.ReferenceExamples);
        Assert.Contains(store.DiagramRules.Style.SheetSets, s => s.ReferenceExample == "2145.pdf" && s.Sheets.Contains("АВР"));
        Assert.True(store.DiagramRules.Style.LineWeights.Frame > store.DiagramRules.Style.LineWeights.Thin);
    }

    [Fact]
    public void CustomerProfilesApplyKnownRequirements()
    {
        var env = AppEnvironment.Load();
        var store = env.Catalog;
        var vm = new MainViewModel(env);

        Assert.NotNull(store.GetCustomerProfile("АО МРСК"));
        Assert.NotNull(store.GetCustomerProfile("АО РЖД"));
        Assert.NotNull(store.GetCustomerProfile("ПАО НК ЛУКОЙЛ"));

        vm.GridCompany = "АО ЮРЭСК";

        Assert.Equal("RAL 5026 (Жемчужно-ночной синий)", vm.BodyColor);
        Assert.Equal("RAL 5026 (Жемчужно-ночной синий)", vm.DoorColor);
        Assert.Equal("УХЛ1", vm.ClimateExecution);
        Assert.Equal("IP34", vm.ProtectionDegree);
        Assert.Equal("Алюминий", vm.BusbarHvMaterial);
        Assert.Equal("РУНН, трансформаторный отсек", vm.LightingAreas);
        Assert.Equal(12, vm.RepairLightingVoltage);
        Assert.Equal("Автонастройка применена", vm.CustomerProfileSummary);

        vm.GridCompany = "АО Лукоил";

        Assert.Equal("У1", vm.ClimateExecution);
        Assert.Equal("IP34", vm.ProtectionDegree);
        Assert.Equal("Воздушный", vm.RuvnExecution);
        Assert.Equal(RuvnEngineering.SurgeArresterAtAirPortal, vm.RuvnSurgeArresterLocation);
        Assert.True(vm.SocketEnabled);
        Assert.Equal(2, vm.SocketQuantity);
        Assert.True(vm.HeatingEnabled);
        Assert.True(vm.MeterHeatingEnabled);
        Assert.Equal("РУВН, РУНН, трансформаторный отсек", vm.LightingAreas);
        Assert.False(vm.OpsEnabled);
        Assert.DoesNotContain("ЛУКОЙЛ", vm.EnclosureNotes);
        Assert.DoesNotContain("Профиль", vm.EnclosureNotes);
        Assert.DoesNotContain("Профиль", vm.AuxNotes);
    }

    [Fact]
    public void DeviceModelsCoverConfiguredEquipmentLists()
    {
        var store = CatalogStore.Load(DataDir);

        AssertManufacturersCovered(store, "АВ", store.Options.AvManufacturers);
        AssertManufacturersCovered(store, "РПС", store.Options.RpsManufacturers);
        AssertManufacturersCovered(store, "ПВР/NH", store.Options.PvrManufacturers);
        AssertManufacturersCovered(store, "РЕ", store.Options.ReManufacturers);
    }

    [Fact]
    public void ProtectiveDeviceModelsHaveProtectionMetadata()
    {
        var store = CatalogStore.Load(DataDir);
        var protectiveTypes = new[] { "АВ", "РПС", "ПВР/NH", "РЕ" };
        var protectiveModels = store.DeviceModels
            .Where(d => protectiveTypes.Contains(d.Type, StringComparer.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(protectiveModels);
        Assert.All(protectiveModels, model =>
        {
            Assert.False(string.IsNullOrWhiteSpace(model.ProtectionKind), $"{model.Type} {model.Manufacturer} {model.Model}: нет protectionKind");
            Assert.False(string.IsNullOrWhiteSpace(model.ProtectionNotes), $"{model.Type} {model.Manufacturer} {model.Model}: нет пояснения по ПУЭ-данным");
        });
        Assert.All(protectiveModels.Where(d => d.Type == "АВ"), model => Assert.Equal("circuitBreaker", model.ProtectionKind));
        Assert.All(protectiveModels.Where(d => d.Type == "РЕ"), model => Assert.Equal("disconnector", model.ProtectionKind));
    }

    [Fact]
    public void AvModelsAreLoadedForManufacturer()
    {
        var env = AppEnvironment.Load();
        var vm = new MainViewModel(env);
        var models = vm.ModelsForFeeder("АВ", "КЭАЗ");
        Assert.NotEmpty(models);
    }

    [Fact]
    public void OutgoingBreakerNominalCannotExceedInputNominal()
    {
        var store = CatalogStore.Load(DataDir);
        var transformer = store.Transformers.First(t => t.RatedCurrentA < 400);
        var cfg = new ProjectConfig
        {
            Manufacturer = transformer.Manufacturer,
            Mark = transformer.Mark,
            PvrOn = true,
            PvrManufacturer = "КЭАЗ",
            PvrNominal = 400,
            OutgoingFeeders =
            {
                new OutgoingFeederConfig
                {
                    Number = 1,
                    DeviceType = "АВ",
                    Manufacturer = "КЭАЗ",
                    Model = "OptiMat D",
                    Nominal = 630,
                },
            },
        };

        var res = CalculationEngine.Calculate(cfg, store);

        Assert.Contains(res.Messages, m =>
            m.Severity == Severity.Error
            && m.Text.Contains("отходящий автомат", StringComparison.OrdinalIgnoreCase)
            && m.Text.Contains("больше номинала ввода", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CurrentTransformerRatioFollowsBreakerNominal()
    {
        var env = AppEnvironment.Load();
        var vm = new MainViewModel(env);

        Assert.Equal("400/5", vm.SuggestedCtRatio(400));
        Assert.Equal("600/5", vm.SuggestedCtRatio(630));
        Assert.Equal("1000/5", vm.SuggestedCtRatio(1000));
        Assert.Equal("1500/5", vm.SuggestedCtRatio(1600));
    }

    [Fact]
    public void MainViewModelExposesSeparateBusbarMaterialsAndRuvnExecutions()
    {
        var env = AppEnvironment.Load();
        var vm = new MainViewModel(env);

        Assert.Contains("Алюминий", vm.BusbarMaterials);
        Assert.Contains("Медь", vm.BusbarMaterials);
        Assert.Equal("Алюминий", vm.BusbarHvMaterial);
        Assert.Equal("Алюминий", vm.BusbarLvMaterial);
        Assert.Contains("У1", vm.ClimateExecutions);
        Assert.Contains("IP54", vm.ProtectionDegrees);
        Assert.Equal("У1", vm.ClimateExecution);
        Assert.Equal("IP54", vm.ProtectionDegree);
        Assert.Contains("Двухстворчатые распашные", vm.DoorConfigurations);
        Assert.Contains("Контур заземления", vm.GroundingTypes);
        Assert.Contains("Шинный мост", vm.RuvnExecutions);
        Assert.DoesNotContain("Шинный мост", vm.CableExecutions);
    }

    [Fact]
    public void OutgoingFeederMeteringTypeControlsMeterAndTt()
    {
        var env = AppEnvironment.Load();
        var vm = new MainViewModel(env);
        var feeder = vm.OutgoingFeeders.First();

        Assert.Contains("Нет", feeder.MeteringTypes);
        Assert.Contains("Технический", feeder.MeteringTypes);
        Assert.Contains("Коммерческий", feeder.MeteringTypes);

        feeder.MeteringType = "Технический";

        Assert.True(feeder.HasMeter);
        Assert.False(string.IsNullOrWhiteSpace(feeder.TtRatio));

        feeder.MeteringType = "Нет";

        Assert.False(feeder.HasMeter);
    }

    [Fact]
    public void ResultSummaryExposesGroupedValidationAndKeyTotals()
    {
        var env = AppEnvironment.Load();
        var vm = new MainViewModel(env);

        Assert.Contains("мм", vm.FinalDimensionsSummary);
        Assert.Contains("кг", vm.GrossMassSummary);
        Assert.Contains("ТМГ", vm.MassBreakdownSummary);
        Assert.Contains("основание", vm.MassBreakdownSummary);
        Assert.Contains("корпус", vm.MassBreakdownSummary);
        Assert.Contains("швеллер", vm.MassMethodologySummary);
        Assert.Contains("Ориентировочный довес", vm.MassScopeWarning);
        Assert.Contains("Предварительный довес", vm.AdditionalMassSummary);
        Assert.Contains("Ориентировочная масса", vm.GrossMassWithOptionsSummary);
        Assert.Contains("крыша", vm.ColorZonesSummary);
        Assert.DoesNotContain("кабельные вводы", vm.EnclosureDetailsSummary);
        Assert.Contains("позиций спецификации", vm.EquipmentTrustSummary);
        Assert.DoesNotContain("needsVerification", vm.EquipmentTrustSummary);
        Assert.DoesNotContain("userInput", vm.EquipmentTrustSummary);
        Assert.Contains("кВА", vm.TransformerResultSummary);
        Assert.Contains("шт.", vm.FeedersResultSummary);
        Assert.Contains("РУВН", vm.BusbarResultSummary);
        Assert.Equal(vm.ErrorCount, vm.ErrorMessages.Count);
        Assert.Equal(vm.WarningCount, vm.WarningMessages.Count);
        Assert.Equal(vm.InfoCount, vm.InfoMessages.Count);
        Assert.Equal(vm.ErrorCount > 0 || vm.WarningCount > 0, vm.HasValidationMessages);
        Assert.DoesNotContain("Ошибки: 0", vm.ValidationSummaryText);
        Assert.DoesNotContain("Предупреждения: 0", vm.ValidationSummaryText);
        Assert.DoesNotContain("Информация: 0", vm.ValidationSummaryText);
        if (vm.HasValidationMessages)
            Assert.False(string.IsNullOrWhiteSpace(vm.ValidationSummaryText));
        else
            Assert.Equal("", vm.ValidationSummaryText);
        Assert.False(string.IsNullOrWhiteSpace(vm.ExportReadinessText));
    }

    [Fact]
    public void AcceptedErrorsAllowDocumentsReleaseButKeepErrorsVisible()
    {
        var env = AppEnvironment.Load();
        var vm = new MainViewModel(env);

        vm.AutoElectricalSelection = false;
        vm.CurrentConfig.PvrOn = true;
        vm.CurrentConfig.PvrNominal = 1;
        vm.CurrentConfig.ReOn = false;
        vm.CurrentConfig.AvInOn = false;
        vm.Recalculate();

        Assert.True(vm.CurrentResult.HasErrors);
        Assert.False(vm.ExportExcelCommand.CanExecute(null));
        Assert.Contains("заблокирован", vm.ExportReadinessText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ДОКУМЕНТ ЗАБЛОКИРОВАН", vm.OrderPreview);

        vm.ErrorsAcceptedForWork = true;

        Assert.True(vm.CurrentResult.HasErrors);
        Assert.True(vm.ExportExcelCommand.CanExecute(null));
        Assert.Contains("согласованными ошибками", vm.ExportReadinessText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("с согласованными ошибками проектирования", vm.OrderPreview);
    }

    [Fact]
    public void ValidationWarnsWhenOutgoingFeederCableOrMeteringAreIncomplete()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = new ProjectConfig
        {
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            PvrOn = true,
            PvrManufacturer = "КЭАЗ",
            PvrNominal = 620,
            OutgoingFeeders =
            {
                new OutgoingFeederConfig
                {
                    Number = 1,
                    DeviceType = "АВ",
                    Nominal = 400,
                    CableMark = "АВБШв",
                    MeteringType = "Коммерческий",
                },
            },
        };

        var res = CalculationEngine.Calculate(cfg, store);

        Assert.DoesNotContain(res.Messages, m =>
            m.Text.Contains("не указано назначение", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(res.Messages, m =>
            m.Text.Contains("не выбран производитель", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(res.Messages, m =>
            m.Text.Contains("не выбрана модель", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(res.Messages, m =>
            m.Severity == Severity.Warning
            && m.Text.Contains("кабель заполнен не полностью", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(res.Messages, m =>
            m.Severity == Severity.Error
            && m.Text.Contains("выбран счетчик", StringComparison.OrdinalIgnoreCase)
            && m.Text.Contains("не задан ТТ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidationWarnsWhenEnclosureRequiredFieldsAreEmpty()
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
            PvrManufacturer = "КЭАЗ",
            PvrNominal = 630,
            ClimateExecution = "",
            ProtectionDegree = "",
            DoorConfiguration = "",
            RuvnDoorConfiguration = "",
            RunnDoorConfiguration = "",
            TransformerDoorConfiguration = "",
            LockType = "",
            HasRigelLock = false,
            NetworkLockType = "",
            HasPadlockProvision = false,
            HasGrounding = true,
            GroundingType = "",
            VentilationType = "",
        };

        var res = CalculationEngine.Calculate(cfg, store);

        Assert.Contains(res.Messages, m => m.Severity == Severity.Warning && m.Text.Contains("климатическое исполнение"));
        Assert.Contains(res.Messages, m => m.Severity == Severity.Warning && m.Text.Contains("степень защиты"));
        Assert.Contains(res.Messages, m => m.Severity == Severity.Warning && m.Text.Contains("конфигурация дверей"));
        Assert.Contains(res.Messages, m => m.Severity == Severity.Warning && m.Text.Contains("замок дверей"));
        Assert.Contains(res.Messages, m => m.Severity == Severity.Warning && m.Text.Contains("тип заземления"));
        Assert.Contains(res.Messages, m => m.Severity == Severity.Warning && m.Text.Contains("тип вентиляции"));
    }

    [Fact]
    public void RuvnFuseRecommendationFollowsTransformerAndVoltage()
    {
        Assert.Equal("80А", RuvnEngineering.RecommendedFuseNominal(400, "6 кВ"));
        Assert.Equal("50А", RuvnEngineering.RecommendedFuseNominal(400, "10 кВ"));
        Assert.Equal("100А", RuvnEngineering.RecommendedFuseNominal(1000, "10 кВ"));
        Assert.Equal("7,2 кВ", RuvnEngineering.RecommendedSurgeArresterOperatingVoltage("6 кВ"));
        Assert.Equal("12 кВ", RuvnEngineering.RecommendedSurgeArresterOperatingVoltage("10 кВ"));
    }

    [Fact]
    public void PassThroughRuvnGeneratesThreeDisconnectorsAndSelectedFuses()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = new ProjectConfig
        {
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            Voltage = "10 кВ",
            RuvnType = "Проходная",
            RuvnExecution = "Воздушный",
            RuvnIncomingSwitch = "ВНА",
            RuvnIncomingSwitchNominal = 630,
            RuvnIncomingFuseOn = true,
            RuvnIncomingFuseType = "ПКТ-101",
            RuvnIncomingFuseNominal = "50А",
            RuvnOutgoingSwitch = "ВНР",
            RuvnOutgoingSwitchNominal = 630,
            RuvnTransformerSwitch = "РВЗ",
            RuvnTransformerSwitchNominal = 630,
            RuvnTransformerFuseOn = true,
            RuvnTransformerFuseType = "ПКТ-101",
            RuvnTransformerFuseNominal = "50А",
            RuvnSurgeArrester = true,
            RuvnSurgeArresterLocation = RuvnEngineering.SurgeArresterAtAirPortal,
            RuvnSurgeArresterDischargeCurrentKa = 10,
            PvrOn = true,
            PvrNominal = 630,
            PvrManufacturer = "КЭАЗ",
        };
        var res = CalculationEngine.Calculate(cfg, store);

        var items = SpecificationBuilder.GenerateSpecification(cfg, res, store);

        Assert.Contains(items, i => i.Name == "Разъединитель РУВН - Входящая линия" && i.Type == "ВНА");
        Assert.Contains(items, i => i.Name == "Разъединитель РУВН - Отходящая линия" && i.Type == "ВНР");
        Assert.Contains(items, i => i.Name == "Разъединитель РУВН - Ответвление на ТМГ" && i.Type == "РВЗ");
        Assert.Contains(items, i => i.Name == "ПКТ РУВН - Входящая линия" && i.Nominal == "50 А");
        Assert.Contains(items, i => i.Name == "ПКТ РУВН - Ответвление на ТМГ" && i.Nominal == "50 А");
        Assert.Contains(items, i => i.Name == "ОПН РУВН" && i.Notes.Contains("воздушном портале", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VacuumRuvnBranchGeneratesBreakerRzaAndDoesNotRequirePkt()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = new ProjectConfig
        {
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            Voltage = "10 кВ",
            RuvnType = "Тупиковая",
            RuvnExecution = "Кабельный",
            RuvnTransformerSwitch = RuvnEngineering.VacuumBreaker,
            RuvnTransformerSwitchNominal = 630,
            RuvnTransformerFuseOn = false,
            RuvnTransformerEquipment = new RuvnBranchEquipmentConfig
            {
                VisibleBreakBefore = "РВЗ",
                VisibleBreakAfter = "РВЗ",
                VacuumBreakerModel = "ВВ/TEL-10",
                VacuumBreakerNominal = 630,
                VacuumBreakerBreakingCurrentKa = 20,
                RzaTerminal = "Сириус-2-Л",
                ProtectionCtRatio = "600/5",
                ProtectionCtQuantity = 3,
                HasTtnp = true,
                TtnpModel = "ТТНП-10",
            },
            PvrOn = true,
            PvrNominal = 630,
            PvrManufacturer = "КЭАЗ",
        };

        var res = CalculationEngine.Calculate(cfg, store);
        var items = SpecificationBuilder.GenerateSpecification(cfg, res, store);
        var order = DocumentBuilder.BuildProductionOrder(cfg, res, store, DocTemplates.Load(DataDir)).ToPlainText();

        Assert.DoesNotContain(res.Messages, m => m.Text.Contains("для защиты ТМГ не выбран ПКТ", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items, i => i.Name == "Видимый разрыв перед вакуумным выключателем - Ответвление на ТМГ" && i.Type == "РВЗ");
        Assert.Contains(items, i => i.Name == "Вакуумный выключатель РУВН - Ответвление на ТМГ" && i.Type == "ВВ/TEL-10" && i.Nominal.Contains("20 кА", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items, i => i.Name == "Видимый разрыв после вакуумного выключателя - Ответвление на ТМГ" && i.Type == "РВЗ");
        Assert.Contains(items, i => i.Name == "Терминал РЗА РУВН - Ответвление на ТМГ" && i.Nominal.Contains("МТЗ", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items, i => i.Name == "ТТ защиты РУВН - Ответвление на ТМГ" && i.Nominal == "600/5" && i.Quantity == 3);
        Assert.DoesNotContain(items, i => i.Name == "ПКТ РУВН - Ответвление на ТМГ");
        Assert.Contains("РВЗ - ВВ/TEL-10 630 А 20 кА - РВЗ", order);
        Assert.Contains("РЗА", order);
    }

    [Fact]
    public void ValidationWarnsWhenRuvnTransformerFuseDiffersFromRecommendation()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = new ProjectConfig
        {
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            Voltage = "10 кВ",
            RuvnType = "Тупиковая",
            RuvnExecution = "Кабельный",
            RuvnTransformerSwitch = "РВЗ",
            RuvnTransformerSwitchNominal = 630,
            RuvnTransformerFuseOn = true,
            RuvnTransformerFuseType = "ПКТ-101",
            RuvnTransformerFuseNominal = "31.5А",
            PvrOn = true,
            PvrNominal = 630,
            PvrManufacturer = "КЭАЗ",
        };

        var res = CalculationEngine.Calculate(cfg, store);

        Assert.Contains(res.Messages, m =>
            m.Severity == Severity.Warning
            && m.Text.Contains("рекомендуется ПКТ 50А", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidationWarnsWhenCurrentTransformerRatioDoesNotMatchNominal()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = new ProjectConfig
        {
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            PvrOn = true,
            PvrManufacturer = "КЭАЗ",
            PvrNominal = 630,
            HasCt = true,
            CtRatio = "800/5",
        };

        var res = CalculationEngine.Calculate(cfg, store);

        Assert.Contains(res.Messages, m =>
            m.Severity == Severity.Warning
            && m.Text.Contains("ТТ учета на вводе", StringComparison.OrdinalIgnoreCase)
            && m.Text.Contains("600/5", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertManufacturersCovered(CatalogStore store, string type, IReadOnlyList<string> manufacturers)
    {
        foreach (var manufacturer in manufacturers)
        {
            Assert.Contains(store.DeviceModels, d =>
                d.Type.Equals(type, StringComparison.OrdinalIgnoreCase)
                && d.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void ModelUpdatesWhenManufacturerChanges()
    {
        var env = AppEnvironment.Load();
        var vm = new MainViewModel(env);
        var feeder = new OutgoingFeederConfig { DeviceType = "АВ", Manufacturer = "КЭАЗ", Number = 1 };
        var fvm = new OutgoingFeederViewModel(vm, feeder);
        
        fvm.Manufacturer = "Контактор";
        Assert.Equal("Контактор", fvm.Manufacturer);
        Assert.NotEmpty(fvm.ModelOptions);
    }

    [Fact]
    public void SymbolLetterCodesAreCorrect()
    {
        var store = CatalogStore.Load(DataDir);
        
        var qf = store.DiagramSymbols.FirstOrDefault(s => s.SymbolKey == "circuitBreaker");
        Assert.Equal("QF", qf?.LetterCode);
        
        var qs = store.DiagramSymbols.FirstOrDefault(s => s.SymbolKey == "disconnector");
        Assert.Equal("QS", qs?.LetterCode);
        
        var fu = store.DiagramSymbols.FirstOrDefault(s => s.SymbolKey == "fuse");
        Assert.Equal("FU", fu?.LetterCode);
        
        var fv = store.DiagramSymbols.FirstOrDefault(s => s.SymbolKey == "surgeArrester");
        Assert.Equal("FV", fv?.LetterCode);
        
        var ta = store.DiagramSymbols.FirstOrDefault(s => s.SymbolKey == "currentTransformer");
        Assert.Equal("TA", ta?.LetterCode);
    }

    [Fact]
    public void DiagramDesignationsUseCatalogPatterns()
    {
        var store = CatalogStore.Load(DataDir);
        SetSymbol(store, "circuitBreaker", "QB", "QB{n}");
        SetSymbol(store, "disconnector", "DS", "DS{n}");
        SetSymbol(store, "switchDisconnectFuse", "SR", "SR{n}");
        SetSymbol(store, "fuse", "FS", "FS{n}");
        SetSymbol(store, "surgeArrester", "SV", "SV{n}");
        SetSymbol(store, "currentTransformer", "CT", "CT{start}-CT{end}");
        SetSymbol(store, "meter", "PM", "PM{n}");
        SetSymbol(store, "powerTransformer", "TR", "TR{n}");

        var cfg = new ProjectConfig
        {
            RuvnSwitch = "РВЗ",
            FuseType = "ПКТ",
            RuvnSurgeArrester = true,
            AvInOn = true,
            AvInNominal = 630,
            AvInManufacturer = "КЭАЗ",
            RunnSurgeArrester = true,
            HasCt = true,
            HasMeter = true,
            OutgoingFeeders =
            {
                new OutgoingFeederConfig
                {
                    Number = 1,
                    DeviceType = "АВ",
                    Manufacturer = "КЭАЗ",
                    Model = "OptiMat D",
                    Nominal = 630,
                    HasMeter = true,
                },
                new OutgoingFeederConfig
                {
                    Number = 1,
                    DeviceType = "РПС",
                    Manufacturer = "Кореневский завод НВА",
                    Model = "РПС",
                    Nominal = 400,
                },
            },
        };

        var designations = SingleLineDiagramRenderer.CollectDesignations(cfg, store);

        Assert.Contains("DS1", designations);
        Assert.Contains("FS1-FS3", designations);
        Assert.Contains("SV1-SV3", designations);
        Assert.Contains("TR1", designations);
        Assert.Contains("QB1", designations);
        Assert.Contains("SV4-SV6", designations);
        Assert.Contains("CT1-CT3", designations);
        Assert.Contains("PM1", designations);
        Assert.Contains("QB2", designations);
        Assert.Contains("CT4-CT6", designations);
        Assert.Contains("PM2", designations);
        Assert.Contains("SR4", designations);

        Assert.DoesNotContain("QF1", designations);
        Assert.DoesNotContain("TA1-TA3", designations);
        Assert.DoesNotContain("PI1", designations);
    }

    [Fact]
    public void LegacyProjectWithoutAuxiliaryNeedsLoads()
    {
        var path = Path.Combine(Path.GetTempPath(), $"legacy_{Guid.NewGuid():N}.ktpn");
        File.WriteAllText(path, """
        {
          "projectName": "Старый проект",
          "manufacturer": "Алагеум",
          "mark": "ТМГ-400 (Алагеум)"
        }
        """);

        try
        {
            var cfg = ProjectStorage.Load(path);
            Assert.NotNull(cfg.AuxiliaryNeeds);
            Assert.False(cfg.AuxiliaryNeeds.Enabled);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AuxiliaryNeedsValidationFindsMissingLightingBreaker()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = new ProjectConfig
        {
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            Channel = "10П",
            SteelType = "Оцинкованная",
            Thickness = 2.0,
            Voltage = "10 кВ",
            PvrOn = true,
            PvrNominal = 630,
            PvrManufacturer = "CHINT",
            AuxiliaryNeeds = new AuxiliaryNeedsConfig
            {
                Enabled = true,
                MainBreakerManufacturer = "IEK",
                MainBreakerModel = "ВА47-29 3P",
                MainBreakerNominal = 25,
                LightingEnabled = true,
                LightingCircuits = 1,
                LightingFixtureQuantity = 1,
            },
        };

        var res = CalculationEngine.Calculate(cfg, store);

        Assert.Contains(res.Messages, m => m.Severity == Severity.Error && m.Text.Contains("автомат освещения"));
    }

    [Fact]
    public void SpecificationIncludesAuxiliaryNeeds()
    {
        var env = AppEnvironment.Load();
        var vm = new MainViewModel(env);
        var spec = DocumentBuilder.BuildSpecification(vm.CurrentConfig, vm.CurrentResult, env.Catalog).ToPlainText();

        Assert.Contains("СПЕЦИФИКАЦИЯ ОБОРУДОВАНИЯ", spec);
        Assert.Contains("ЩСН1", spec);
        Assert.Contains("EL1", spec);
    }

    [Fact]
    public void AuxiliaryDesignationsAreCollectedFromCatalog()
    {
        var store = CatalogStore.Load(DataDir);
        SetSymbol(store, "auxiliaryCabinet", "AUX", "AUX{n}");
        SetSymbol(store, "lighting", "LT", "LT{n}");
        SetSymbol(store, "socket", "SO", "SO{n}");
        SetSymbol(store, "backupPowerSource", "UPS", "UPS{n}");

        var cfg = new ProjectConfig
        {
            AuxiliaryNeeds = new AuxiliaryNeedsConfig
            {
                Enabled = true,
                LightingEnabled = true,
                SocketEnabled = true,
                RieseEnabled = true,
                RiesePowerVa = 1000,
            },
        };

        var designations = SingleLineDiagramRenderer.CollectDesignations(cfg, store);

        Assert.Contains("AUX1", designations);
        Assert.Contains("LT1", designations);
        Assert.Contains("SO1", designations);
        Assert.Contains("UPS1", designations);
    }

    [Fact]
    public void MainViewModelLimitsOutgoingFeederQuantities()
    {
        var vm = new MainViewModel(AppEnvironment.Load());

        vm.AvQty = 99;
        vm.RpsOn = true;
        vm.RpsQty = 99;

        Assert.Equal(20, vm.AvQty);
        Assert.Equal(8, vm.RpsQty);
        Assert.Equal(28, vm.OutgoingFeederCount);
    }

    [Fact]
    public void BusbarCalculationUsesTransformerSectionTable()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = new ProjectConfig
        {
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            Voltage = "10 кВ",
            RuvnType = "Тупиковая",
            RuvnTransformerSwitch = "РВЗ",
            RuvnTransformerSwitchNominal = 630,
            PvrOn = true,
            PvrNominal = 620,
            BusbarHvMaterial = "Алюминий",
            BusbarLvMaterial = "Алюминий",
            BusbarNMaterial = "Алюминий",
        };

        var aluminum = CalculationEngine.Calculate(cfg, store);
        cfg.BusbarHvMaterial = "Медь";
        cfg.BusbarLvMaterial = "Медь";
        cfg.BusbarNMaterial = "Медь";
        var copper = CalculationEngine.Calculate(cfg, store);
        cfg.PvrNominal = 1000;
        var higherInputNominal = CalculationEngine.Calculate(cfg, store);
        cfg.Mark = "ТМГ-630 (Алагеум)";
        var higherTransformerNominal = CalculationEngine.Calculate(cfg, store);

        Assert.Equal("50х5", aluminum.BusbarHv);
        Assert.Equal("50х5", aluminum.BusbarLv);
        Assert.Equal("50х5", aluminum.BusbarN);
        Assert.Equal("40х5", aluminum.BusbarPe);
        Assert.Equal("40х4", copper.BusbarLv);
        Assert.Equal("40х4", copper.BusbarN);
        Assert.Equal("40х4", copper.BusbarHv);
        Assert.Equal("40х4", higherInputNominal.BusbarLv);
        Assert.Equal("60х6", higherTransformerNominal.BusbarLv);
        Assert.Equal("60х6", higherTransformerNominal.BusbarN);
    }

    private static void SetSymbol(CatalogStore store, string symbolKey, string letterCode, string designationPattern)
    {
        var symbol = store.DiagramSymbols.First(s => s.SymbolKey == symbolKey);
        symbol.LetterCode = letterCode;
        symbol.DesignationPattern = designationPattern;
    }
}
