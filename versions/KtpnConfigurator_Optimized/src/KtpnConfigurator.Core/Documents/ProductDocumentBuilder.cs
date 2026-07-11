using System.Globalization;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Documents;

public static class ProductDocumentBuilder
{
    public static IReadOnlyList<GeneratedDocument> BuildAll(ProjectConfig project, CalculationResult result, CatalogStore store)
    {
        var definition = ProductRegistry.ResolveOrDefault(project.ProductTypeId);
        return
        [
            BuildProductionOrder(project, result, definition),
            BuildPassport(project, result, definition),
            BuildChecklist(project, definition),
            BuildSpecification(project, result, definition, store),
        ];
    }

    private static GeneratedDocument BuildProductionOrder(ProjectConfig project, CalculationResult result, ProductDefinition definition)
    {
        var document = NewDocument(GeneratedDocumentKind.ProductionOrder, "ПРИКАЗ НА ПРОИЗВОДСТВО", project, definition);
        document.Sections.Add(new DocSection
        {
            Name = "Согласование запуска",
            IsSignatureTable = true,
            Rows =
            {
                new("В работу", "____________ 202_ г.", "____________ / ____________"),
                new("Конструкторская служба", "____________ 202_ г.", "____________ / ____________"),
                new("Отдел снабжения", "____________ 202_ г.", "____________ / ____________"),
            },
        });
        var parameters = new DocSection { Name = "Параметры изделия" };
        AddCommonRows(parameters, project, result, definition);
        AddProductRows(parameters, project, result);
        document.Sections.Add(parameters);
        foreach (var section in ProductLineupSections(project))
            document.Sections.Add(section);
        document.Sections.Add(new DocSection
        {
            Name = "Участок",
            IsSignatureTable = true,
            Rows =
            {
                new("Заготовительный", "ФИО / подпись", "Дата"),
                new("Малярный", "ФИО / подпись", "Дата"),
                new("Сварочный", "ФИО / подпись", "Дата"),
                new("Монтажный", "ФИО / подпись", "Дата"),
                new("Испытания", "ФИО / подпись", "Дата"),
                new("Комплектация", "ФИО / подпись", "Дата"),
            },
        });
        return document;
    }

    private static GeneratedDocument BuildPassport(ProjectConfig project, CalculationResult result, ProductDefinition definition)
    {
        var document = NewDocument(GeneratedDocumentKind.Passport, $"ПАСПОРТ {definition.DisplayName}", project, definition);
        var section = new DocSection { Name = "Основные технические данные" };
        AddCommonRows(section, project, result, definition);
        AddProductRows(section, project, result);
        document.Sections.Add(section);
        foreach (var lineupSection in ProductLineupSections(project))
            document.Sections.Add(lineupSection);
        document.Sections.Add(new DocSection
        {
            Name = "Приемо-сдаточные проверки",
            Rows =
            {
                new("Внешний осмотр", "Соответствует / не соответствует"),
                new("Проверка механических блокировок", "Соответствует / не соответствует"),
                new("Проверка электрических соединений", "Соответствует / не соответствует"),
                new("Проверка защитного заземления", "Соответствует / не соответствует"),
                new("Электрические испытания", "Протокол № __________"),
            },
        });
        return document;
    }

    private static GeneratedDocument BuildChecklist(ProjectConfig project, ProductDefinition definition)
    {
        var document = NewDocument(GeneratedDocumentKind.Checklist, $"ЧЕК-ЛИСТ ОТК {definition.DisplayName}", project, definition);
        var common = new DocSection { Name = "ОБЩИЕ ПРОВЕРКИ" };
        foreach (var item in new[]
        {
            "Комплектность соответствует приказу и спецификации",
            "Маркировка аппаратов и присоединений выполнена",
            "Двери, замки и механические элементы работают",
            "Защитное заземление и PE-цепи проверены",
            "Затяжка контактных соединений проверена",
            "Документация и протоколы приложены",
        }) common.Rows.Add(new DocRow { Label = item, IsChecklistItem = true });
        document.Sections.Add(common);

        var product = new DocSection { Name = "ПРОВЕРКИ ИЗДЕЛИЯ" };
        foreach (var item in ProductChecklistItems(project))
            product.Rows.Add(new DocRow { Label = item, IsChecklistItem = true });
        document.Sections.Add(product);
        return document;
    }

    private static GeneratedDocument BuildSpecification(ProjectConfig project, CalculationResult result, ProductDefinition definition, CatalogStore store)
    {
        var document = NewDocument(GeneratedDocumentKind.Specification, "СПЕЦИФИКАЦИЯ ОБОРУДОВАНИЯ", project, definition);
        var position = 1;
        if (project.ProductTypeId == ProductTypeIds.DoubleKtpn)
        {
            var first = store.GetTransformer(project.Mark);
            var second = store.GetTransformer(project.DoubleKtpn.SecondTransformerMark);
            var section = new DocSection { Name = "Силовое оборудование" };
            section.Rows.Add(Spec(position++, "T1", project.Mark, project.Manufacturer, first?.PowerKva is > 0 ? $"{first.PowerKva:0} кВА" : ""));
            section.Rows.Add(Spec(position++, "T2", project.DoubleKtpn.SecondTransformerMark, project.DoubleKtpn.SecondTransformerManufacturer, second?.PowerKva is > 0 ? $"{second.PowerKva:0} кВА" : ""));
            section.Rows.Add(Spec(position++, "QF1", "Вводной аппарат секции 1", "", $"{project.DoubleKtpn.Section1InputNominalA} А"));
            section.Rows.Add(Spec(position++, "QF2", "Вводной аппарат секции 2", "", $"{project.DoubleKtpn.Section2InputNominalA} А"));
            section.Rows.Add(Spec(position++, "QFС", "Секционный аппарат", "", $"{project.DoubleKtpn.SectionCouplerNominalA} А"));
            section.Rows.Add(Spec(position++, "ШС1", "Шины РУНН секции 1", "", $"{result.Section1BusbarLv}; N {result.Section1BusbarN}; PE {result.Section1BusbarPe}"));
            section.Rows.Add(Spec(position++, "ШС2", "Шины РУНН секции 2", "", $"{result.Section2BusbarLv}; N {result.Section2BusbarN}; PE {result.Section2BusbarPe}"));
            foreach (var feeder in project.OutgoingFeeders.OrderBy(feeder => feeder.SectionNumber).ThenBy(feeder => feeder.Number))
                section.Rows.Add(Spec(position++, $"{feeder.DeviceType}{feeder.Number}", $"Секция {feeder.SectionNumber}: {feeder.Purpose}", feeder.Manufacturer, $"{feeder.Model}; {feeder.Nominal} А"));
            document.Sections.Add(section);
        }
        else if (project.ProductTypeId is ProductTypeIds.Nku or ProductTypeIds.Shcho or ProductTypeIds.Vru)
        {
            var section = new DocSection { Name = "Панели" };
            foreach (var panel in project.LowVoltageAssembly.Panels)
                section.Rows.Add(Spec(position++, $"П{panel.Number}",
                    $"Секция {panel.SectionNumber}; {panel.PanelType}: {panel.Purpose}",
                    panel.Manufacturer,
                    $"{panel.MainDevice} {panel.Model}; {panel.RatedCurrentA} А; Icu {panel.BreakingCapacityKa:0.#} кА; линий {panel.CircuitCount}; учет {YesNo(panel.HasMetering)}; ОПН {YesNo(panel.HasSurgeProtection)}; {panel.WidthMm:0} мм; {panel.EstimatedMassKg:0} кг; статус {EquipmentStatus(panel.EquipmentSourceConfidence)}"));
            document.Sections.Add(section);
        }
        else
        {
            var section = new DocSection { Name = "Ячейки" };
            foreach (var cell in project.MediumVoltageSwitchgear.Cells)
                section.Rows.Add(Spec(position++, $"Я{cell.Number}",
                    cell.Purpose,
                    "",
                    $"{cell.MainDevice} {cell.DeviceModel}; {cell.RatedCurrentA} А; {cell.BreakingCurrentKa:0.#} кА; ТТ {Value(cell.CtRatio)} {Value(cell.CtAccuracyClass)}; ТН {YesNo(cell.HasVoltageTransformer)} {Value(cell.VoltageTransformerModel)}; РЗА {Value(cell.RelayProtection)}; терминал {Value(cell.RelayTerminal)}; разрывы {VisibleBreakSummary(cell)}; {cell.WidthMm:0} мм; {cell.EstimatedMassKg:0} кг; статус {EquipmentStatus(cell.EquipmentSourceConfidence)}"));
            document.Sections.Add(section);
        }

        document.Sections.Add(new DocSection
        {
            Name = "Сборные шины",
            Rows =
            {
                Spec(position, "ШС", "Комплект сборных шин", "", BusbarSpecificationText(project, result)),
            },
        });
        return document;
    }

    private static void AddCommonRows(DocSection section, ProjectConfig project, CalculationResult result, ProductDefinition definition)
    {
        section.Rows.Add(new("Изделие", definition.DisplayName));
        section.Rows.Add(new("Проект", Value(project.ProjectName)));
        section.Rows.Add(new("Исполнение / IP", $"{Value(project.ClimateExecution)} / {ProductProtectionDegree(project)}"));
        section.Rows.Add(new("Габариты ДхШхВ", $"{F(result.LengthFinal)}x{F(result.WidthFinal)}x{F(result.HeightFinal)} мм"));
        section.Rows.Add(new("Ориентировочная масса", $"{F(result.GrossMassEstimated)} кг"));
    }

    private static void AddProductRows(DocSection section, ProjectConfig project, CalculationResult result)
    {
        if (project.ProductTypeId == ProductTypeIds.DoubleKtpn)
        {
            var config = project.DoubleKtpn;
            section.Rows.Add(new("Трансформаторы", $"Т1: {Value(project.Mark)}; Т2: {Value(config.SecondTransformerMark)}"));
            section.Rows.Add(new("Секция 1", $"ток Т1 {result.Section1RatedCurrentA:0} А; ввод {config.Section1InputNominalA} А; шины {result.Section1BusbarLv}; масса секции {result.Section1EstimatedMass:0} кг"));
            section.Rows.Add(new("Секция 2", $"ток Т2 {result.Section2RatedCurrentA:0} А; ввод {config.Section2InputNominalA} А; шины {result.Section2BusbarLv}; масса секции {result.Section2EstimatedMass:0} кг"));
            section.Rows.Add(new("Вводы секций", $"{config.Section1InputNominalA} А / {config.Section2InputNominalA} А"));
            section.Rows.Add(new("Секционный аппарат", $"{config.SectionCouplerNominalA} А; нормально {config.NormalCouplerPosition.ToLowerInvariant()}"));
            section.Rows.Add(new("АВР", config.AutomaticTransferEnabled ? config.ReserveMode : "Нет"));
            section.Rows.Add(new("Параллельная работа", config.ParallelOperationAllowed ? "Разрешена проектом" : "Запрещена блокировками"));
            section.Rows.Add(new("Ориентировочный КЗ секций", $"{result.Section1ShortCircuitCurrentKa:0.#} / {result.Section2ShortCircuitCurrentKa:0.#} кА"));
            section.Rows.Add(new("Шины РУВН / РУНН", $"{result.BusbarHv} / С1 {result.Section1BusbarLv}; С2 {result.Section2BusbarLv}"));
            return;
        }

        if (project.ProductTypeId is ProductTypeIds.Nku or ProductTypeIds.Shcho or ProductTypeIds.Vru)
        {
            var config = project.LowVoltageAssembly;
            section.Rows.Add(new("Серия / шаблон", $"{Value(config.Series)} / {Value(config.LineupTemplate)}"));
            section.Rows.Add(new("Напряжение / частота", $"{config.RatedVoltageV} В / {config.FrequencyHz} Гц"));
            section.Rows.Add(new("Ток шин / КЗ / Icw / Ipk", $"{config.RatedBusCurrentA} А / {config.DesignShortCircuitCurrentKa:0.#} кА / {config.ShortTimeWithstandCurrentKa:0.#} кА / {config.PeakWithstandCurrentKa:0.#} кА"));
            section.Rows.Add(new("Система заземления", config.EarthingSystem));
            section.Rows.Add(new("Разделение / обслуживание", $"{config.InternalSeparation} / {config.ServiceAccess}"));
            section.Rows.Add(new("Панели / секции", $"{config.Panels.Count.ToString(CultureInfo.InvariantCulture)} / {config.SectionCount}"));
            section.Rows.Add(new("Суммарная ширина панелей", $"{config.Panels.Sum(panel => Math.Max(0, panel.WidthMm)):0} мм"));
            section.Rows.Add(new("Сборные шины", result.BusbarLv));
            section.Rows.Add(new("N / PE", $"{result.BusbarN}; {result.BusbarPe}"));
            section.Rows.Add(new("Статус оборудования", EquipmentTrust(config.Panels.Select(panel => panel.EquipmentSourceConfidence))));
            return;
        }

        var mv = project.MediumVoltageSwitchgear;
        section.Rows.Add(new("Серия / шаблон", $"{Value(mv.Series)} / {Value(mv.LineupTemplate)}"));
        section.Rows.Add(new("Напряжение", $"{mv.RatedVoltageKv:0.#} кВ; наибольшее рабочее {mv.HighestOperatingVoltageKv:0.#} кВ"));
        section.Rows.Add(new("Ток шин", $"{mv.RatedBusCurrentA} А"));
        section.Rows.Add(new("КЗ / термическая / электродинамическая стойкость", $"{mv.DesignShortCircuitCurrentKa:0.#} кА / {mv.ShortTimeWithstandCurrentKa:0.#} кА, {mv.ShortTimeDurationSeconds:0.#} с / {mv.PeakWithstandCurrentKa:0.#} кА"));
        section.Rows.Add(new("Ток отключения", $"{mv.BreakerBreakingCurrentKa:0.#} кА"));
        section.Rows.Add(new("Исполнение ячеек / обслуживание", $"{Value(mv.CellExecution)} / {Value(mv.ServiceAccess)}"));
        section.Rows.Add(new("Ячейки / суммарная ширина", $"{mv.Cells.Count.ToString(CultureInfo.InvariantCulture)} / {mv.Cells.Sum(cell => Math.Max(0, cell.WidthMm)):0} мм"));
        section.Rows.Add(new("Статус оборудования", EquipmentTrust(mv.Cells.Select(cell => cell.EquipmentSourceConfidence))));
        if (project.ProductTypeId == ProductTypeIds.Kru)
            section.Rows.Add(new("IAC / LSC / перегородки", $"{Value(mv.IacClassification)} / {Value(mv.ServiceContinuityCategory)} / {Value(mv.PartitionClass)}"));
    }

    private static IEnumerable<DocSection> ProductLineupSections(ProjectConfig project)
    {
        if (project.ProductTypeId is ProductTypeIds.Nku or ProductTypeIds.Shcho or ProductTypeIds.Vru)
        {
            yield return new DocSection
            {
                Name = "Линейка панелей",
                Rows = project.LowVoltageAssembly.Panels
                    .Select(panel => new DocRow(
                        $"П{panel.Number} / секция {panel.SectionNumber}",
                        $"{panel.PanelType}; {panel.Purpose}; {panel.MainDevice} {Value(panel.Model)}; {panel.RatedCurrentA} А; Icu {panel.BreakingCapacityKa:0.#} кА; линий {panel.CircuitCount}; учет {YesNo(panel.HasMetering)}; ОПН {YesNo(panel.HasSurgeProtection)}; ширина {panel.WidthMm:0} мм; масса {panel.EstimatedMassKg:0} кг; статус {EquipmentStatus(panel.EquipmentSourceConfidence)}"))
                    .ToList(),
            };
            yield break;
        }

        if (project.ProductTypeId is ProductTypeIds.Kso or ProductTypeIds.Kru)
        {
            yield return new DocSection
            {
                Name = "Линейка ячеек",
                Rows = project.MediumVoltageSwitchgear.Cells
                    .Select(cell => new DocRow(
                        $"Я{cell.Number} / {cell.Purpose}",
                        $"{cell.MainDevice} {Value(cell.DeviceModel)}; {cell.RatedCurrentA} А; отключение {cell.BreakingCurrentKa:0.#} кА; ТТ {Value(cell.CtRatio)} {Value(cell.CtAccuracyClass)}; ТН {YesNo(cell.HasVoltageTransformer)} {Value(cell.VoltageTransformerModel)}; РЗА {Value(cell.RelayProtection)}; терминал {Value(cell.RelayTerminal)}; разрывы {VisibleBreakSummary(cell)}; заземлитель {YesNo(cell.HasEarthingSwitch)}; ширина {cell.WidthMm:0} мм; масса {cell.EstimatedMassKg:0} кг; статус {EquipmentStatus(cell.EquipmentSourceConfidence)}"))
                    .ToList(),
            };
        }
    }

    private static IEnumerable<string> ProductChecklistItems(ProjectConfig project)
    {
        if (project.ProductTypeId == ProductTypeIds.DoubleKtpn)
            return new[] { "Проверена фазировка Т1 и Т2", "Проверено чередование фаз секций", "Проверена логика АВР", "Проверен запрет недопустимой параллельной работы", "Проверены вводные и секционный аппараты", "Проверена отключающая способность по КЗ секций" };
        if (project.ProductTypeId is ProductTypeIds.Nku or ProductTypeIds.Shcho or ProductTypeIds.Vru)
            return new[] { "Проверены номиналы вводных и отходящих аппаратов", "Проверены N- и PE-шины", "Проверены Icw/Ipk конструкции", "Проверена форма внутреннего разделения", "Проверены цепи учета, ОПН и пломбирования" };
        return new[] { "Проверены положения аппаратов и заземлителей", "Проверены видимые разрывы до и после выключателей", "Проверены механические и электромагнитные блокировки", "Проверены ТТ, ТН и вторичные цепи", "Проверены РЗА и оперативное питание", "Проверены характеристики стойкости и ток отключения" };
    }

    private static GeneratedDocument NewDocument(GeneratedDocumentKind kind, string title, ProjectConfig project, ProductDefinition definition) =>
        new() { Kind = kind, Title = title, Subtitle = $"{Value(project.ProjectName)} / {definition.DisplayName}" };

    private static DocRow Spec(int position, string designation, string name, string manufacturer, string nominal) =>
        new(position.ToString(CultureInfo.InvariantCulture), string.Join(" | ", new[] { designation, name, manufacturer, nominal }.Where(value => !string.IsNullOrWhiteSpace(value))), "Количество: 1 шт");

    private static string ProductProtectionDegree(ProjectConfig project) =>
        project.ProductTypeId is ProductTypeIds.Nku or ProductTypeIds.Shcho or ProductTypeIds.Vru
            ? Value(project.LowVoltageAssembly.ProtectionDegree)
            : Value(project.ProtectionDegree);

    private static string BusbarSpecificationText(ProjectConfig project, CalculationResult result)
    {
        if (project.ProductTypeId == ProductTypeIds.DoubleKtpn)
            return $"РУВН {result.BusbarHv}; РУНН С1 {result.Section1BusbarLv}; РУНН С2 {result.Section2BusbarLv}";
        return result.BusbarLv != "-" ? result.BusbarLv : result.BusbarHv;
    }

    private static string VisibleBreakSummary(MediumVoltageCellConfig cell)
    {
        if (!string.IsNullOrWhiteSpace(cell.VisibleBreakBefore) || !string.IsNullOrWhiteSpace(cell.VisibleBreakAfter))
            return $"до {Value(cell.VisibleBreakBefore)}, после {Value(cell.VisibleBreakAfter)}";
        return Value(cell.VisibleBreaks);
    }

    private static string EquipmentTrust(IEnumerable<string> confidences)
    {
        var values = confidences.ToList();
        if (values.Count == 0)
            return "нет позиций";
        var verified = values.Count(value => value.Equals("verified", StringComparison.OrdinalIgnoreCase));
        var needsVerification = values.Count(value => value.Equals("needsVerification", StringComparison.OrdinalIgnoreCase));
        var userInput = values.Count(value => value.Equals("userInput", StringComparison.OrdinalIgnoreCase));
        return $"проверено {verified}; требуется проверка {needsVerification}; задано проектом {userInput}";
    }

    private static string EquipmentStatus(string confidence) =>
        confidence.Equals("verified", StringComparison.OrdinalIgnoreCase)
            ? "Проверено"
            : confidence.Equals("userInput", StringComparison.OrdinalIgnoreCase)
                ? "Задано проектом"
                : "Требуется проверка";

    private static string YesNo(bool value) => value ? "да" : "нет";
    private static string Value(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    private static string F(double value) => value.ToString("0", CultureInfo.InvariantCulture);
}
