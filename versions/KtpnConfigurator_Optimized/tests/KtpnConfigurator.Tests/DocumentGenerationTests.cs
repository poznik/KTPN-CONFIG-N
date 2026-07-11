using System.IO;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Documents;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;
using Xunit;

namespace KtpnConfigurator.Tests;

public class DocumentGenerationTests
{
    private static string DataDir => Path.Combine(AppContext.BaseDirectory, "Data");

    private static ProjectConfig SampleConfig() => new()
    {
        ProjectName = "КТПН для проверки документов",
        GridCompany = "АО МРСК",
        Manufacturer = "Алагеум",
        Mark = "ТМГ-400 (Алагеум)",
        SteelType = "ОЦ",
        Thickness = 2.0,
        Channel = "10П",
        BodyColor = "RAL 7035",
        DoorColor = "RAL 7035",
        BusbarHvMaterial = "Медь",
        BusbarLvMaterial = "Алюминий",
        ClimateExecution = "УХЛ1",
        ProtectionDegree = "IP55",
        DoorConfiguration = "Двухстворчатые с сетчатым барьером",
        LockType = "Замок под сетевую компанию",
        HasRigelLock = true,
        NetworkLockType = "Россети",
        HasPadlockProvision = false,
        HasGrounding = true,
        GroundingType = "Внутренняя шина PE",
        VentilationType = "Естественная",
        HasRoofDeflector = true,
        HasNameplate = false,
        EnclosureNotes = "порошковая окраска",
        LenRuvn = 1300,
        LenRunn = 600,
        TransformerTolerance = 300,
        LengthBuffer = 10,
        Voltage = "6 кВ",
        RuvnType = "Тупиковая",
        RuvnSwitch = "РВЗ",
        RuvnSwitchNominal = 630,
        FuseType = "ПКТ",
        FuseNominal = "31.5А",
        RuvnExecution = "Воздух",
        OutgoingExecution = "Воздух",
        RuvnSurgeArrester = true,
        PvrOn = true,
        PvrNominal = 630,
        PvrManufacturer = "CHINT",
        HasCt = true,
        CtRatio = "600/5",
        HasMeter = true,
        AvOn = true,
        AvQty = 2,
        AvBrand = "IEK",
        OutgoingFeeders =
        {
            new OutgoingFeederConfig
            {
                Number = 1,
                DeviceType = "АВ",
                Purpose = "Насосная",
                Manufacturer = "IEK",
                Model = "ВА88",
                Nominal = 630,
                CableMark = "АВБШв",
                CableSection = "4x70",
                MeteringType = "Коммерческий",
                TtRatio = "600/5",
                HasMeter = true,
                Notes = "фидер 1",
            },
        },
    };

    [Fact]
    public void ProductionOrder_UsesProductionFormSections()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = SampleConfig();
        var res = CalculationEngine.Calculate(cfg, store);
        var doc = DocumentBuilder.BuildProductionOrder(cfg, res, store, DocTemplates.Load(DataDir));

        Assert.Equal(GeneratedDocumentKind.ProductionOrder, doc.Kind);
        Assert.Contains(doc.Sections, s => s.Name == "Согласование запуска" && s.IsSignatureTable);
        Assert.Contains(doc.Sections, s => s.Name == "Участок" && s.IsSignatureTable && s.Rows.Count >= 7);

        var text = doc.ToPlainText();
        Assert.Contains("Шины РУВН / РУНН", text);
        Assert.Contains("Медь", text);
        Assert.Contains("Алюминий", text);
        Assert.Contains("Климат / IP", text);
        Assert.Contains("УХЛ1 / IP55", text);
        Assert.Contains("Двухстворчатые с сетчатым барьером", text);
        Assert.Contains("заземление: Внутренняя шина PE; вентиляция: Естественная; дефлектор: предусмотрен", text);
        Assert.Contains("табличка не предусмотрена", text);
        Assert.Contains("порошковая окраска", text);
        Assert.Contains("Дата отгрузки", text);
        Assert.Contains("АВ-1 (Насосная): IEK ВА88 630 А; кабель АВБШв 4x70, учет: коммерческий, ТТ 600/5, фидер 1", text);
        Assert.DoesNotContain("Профиль заказчика", text);
        Assert.DoesNotContain("Требования заказчика", text);
        Assert.DoesNotContain("Доверие базы оборудования", text);
    }

    [Fact]
    public void ChecklistTemplate_CoversPrimMrskSections()
    {
        var tpl = DocTemplates.Load(DataDir);
        var names = tpl.QcChecklist.Sections.Select(s => s.Name).ToList();
        var itemCount = tpl.QcChecklist.Sections.Sum(s => s.Items.Count);

        Assert.Contains("ДВЕРИ РУНН ВНУТРИ", names);
        Assert.Contains("ВНУТРЕННИЙ ОТСЕК ТМГ", names);
        Assert.Contains("ВНУТРИ РУВН", names);
        Assert.True(itemCount >= 100);
        Assert.DoesNotContain(tpl.QcChecklist.Sections.SelectMany(s => s.Items), item => item.StartsWith("=\""));
    }

    [Fact]
    public void Checklist_AddsClosingSectionAndDynamicValues()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = SampleConfig();
        var res = CalculationEngine.Calculate(cfg, store);
        var doc = DocumentBuilder.BuildChecklist(cfg, res, store, DocTemplates.Load(DataDir));

        Assert.Equal(GeneratedDocumentKind.Checklist, doc.Kind);
        Assert.Contains(doc.Sections, s => s.Name == "ЗАМЕЧАНИЯ И ПОДПИСИ");
        Assert.Contains(doc.Sections.SelectMany(s => s.Rows), r => r.Label.Contains($"Ошиновка РУВН: {res.BusbarHv}"));
    }

    [Fact]
    public void Passport_IncludesEnclosureOperationFields()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = SampleConfig();
        var res = CalculationEngine.Calculate(cfg, store);
        var doc = DocumentBuilder.BuildPassport(cfg, res, store);
        var text = doc.ToPlainText();

        Assert.Contains("Климатическое исполнение: УХЛ1", text);
        Assert.Contains("Степень защиты: IP55", text);
        Assert.Contains("Материал пола: Рифленый лист 3.0 мм", text);
        Assert.Contains("Замки: Двухстворчатые с сетчатым барьером; ригельный замок, сетевой замок Россети", text);
        Assert.Contains("Заземление: Внутренняя шина PE", text);
        Assert.Contains("Вентиляция: Естественная", text);
        Assert.Contains("Табличка: не предусмотрена", text);
        Assert.DoesNotContain("Профиль заказчика", text);
        Assert.DoesNotContain("Требования заказчика", text);
    }

    [Fact]
    public void GeneratedDocuments_DoNotContainPersonalSurnames()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = SampleConfig();
        var res = CalculationEngine.Calculate(cfg, store);
        var templates = DocTemplates.Load(DataDir);
        var docs = new[]
        {
            DocumentBuilder.BuildProductionOrder(cfg, res, store, templates),
            DocumentBuilder.BuildPassport(cfg, res, store),
            DocumentBuilder.BuildChecklist(cfg, res, store, templates),
            DocumentBuilder.BuildSpecification(cfg, res, store),
        };
        var text = string.Join("\n", docs.Select(d => d.ToPlainText())) + "\n" + templates.QcChecklist.Committee;

        Assert.DoesNotContain("Жамалетдинов", text);
        Assert.DoesNotContain("Богатенков", text);
        Assert.DoesNotContain("Ершов", text);
    }

    [Fact]
    public void SpecificationBuilder_GeneratesStructuredItemsForDocument()
    {
        var store = CatalogStore.Load(DataDir);
        var cfg = SampleConfig();
        cfg.ReOn = true;
        cfg.ReNominal = 400;
        cfg.ReManufacturer = "IEK";
        cfg.AvInOn = true;
        cfg.AvInNominal = 630;
        cfg.AvInManufacturer = "КЭАЗ";

        var res = CalculationEngine.Calculate(cfg, store);
        var items = SpecificationBuilder.GenerateSpecification(cfg, res, store);
        var doc = DocumentBuilder.BuildSpecification(cfg, res, store);
        var text = doc.ToPlainText();

        Assert.Contains(items, i => i.Designation == "QS2" && i.Name.Contains("ПВР/NH"));
        Assert.Contains(items, i => i.Designation == "QS3" && i.Name.Contains("РЕ"));
        Assert.Contains(items, i => i.Designation == "QF1" && i.Name.Contains("АВ"));
        Assert.Contains(items, i => i.Designation == "QF2"
            && i.Name == "Отходящая линия АВ-1 - Насосная"
            && i.Manufacturer == "IEK"
            && i.Nominal == "630 А"
            && i.Notes.Contains("кабель АВБШв 4x70", StringComparison.OrdinalIgnoreCase)
            && i.Notes.Contains("учет: коммерческий", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(i.SourceConfidence));
        Assert.Contains(items, i => i.Designation == "TA4-TA6" && i.Quantity == 3 && i.Unit == "шт");
        Assert.Contains(items, i => i.Designation == "ШВН" && i.Name == "Шины РУВН" && i.Manufacturer == cfg.BusbarHvMaterial);
        Assert.Contains(items, i => i.Designation == "ШНН" && i.Name == "Шины РУНН" && i.Manufacturer == cfg.BusbarLvMaterial);
        Assert.Contains(doc.Sections, s => s.Name == "Силовая часть и учет");
        Assert.Contains("Количество:", text);
        Assert.DoesNotContain("источник:", text);
        Assert.DoesNotContain("доверие:", text);
    }
}
