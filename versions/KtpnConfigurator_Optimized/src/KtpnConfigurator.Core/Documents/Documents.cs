using System.Globalization;
using System.Text;
using System.Text.Json;
using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Documents;

// ----- Шаблоны документов (doc_templates.json) -----
public sealed class DocTemplates
{
    public QcChecklistTemplate QcChecklist { get; set; } = new();
    public ProductionOrderTemplate ProductionOrder { get; set; } = new();

    public static DocTemplates Load(string dataDir, string? gridCompany = null)
    {
        var basePath = Path.Combine(dataDir, "doc_templates.json");
        var customPath = string.IsNullOrWhiteSpace(gridCompany) 
            ? basePath 
            : Path.Combine(dataDir, $"doc_templates_{SafeFilePart(gridCompany)}.json");

        var pathToLoad = File.Exists(customPath) ? customPath : basePath;
        if (!File.Exists(pathToLoad)) return new();
        
        try
        {
            using var fs = File.OpenRead(pathToLoad);
            return JsonSerializer.Deserialize<DocTemplates>(fs,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }

    public void Save(string dataDir, string gridCompany)
    {
        Directory.CreateDirectory(dataDir);
        var suffix = string.IsNullOrWhiteSpace(gridCompany) ? "default" : SafeFilePart(gridCompany);
        var path = Path.Combine(dataDir, $"doc_templates_{suffix}.json");
        using var fs = File.Create(path);
        JsonSerializer.Serialize(fs, this, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private static string SafeFilePart(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');
        return value.Trim();
    }
}

public sealed class QcChecklistTemplate
{
    public string Title { get; set; } = "";
    public string Committee { get; set; } = "";
    public List<QcSection> Sections { get; set; } = new();

    public string ToPlainTextTemplate()
    {
        var sb = new StringBuilder();
        foreach (var sec in Sections)
        {
            sb.AppendLine($"[{sec.Name}]");
            foreach (var item in sec.Items)
            {
                sb.AppendLine(item);
            }
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    public void FromPlainTextTemplate(string text)
    {
        Sections.Clear();
        QcSection? currentSection = null;
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.StartsWith("[") && t.EndsWith("]"))
            {
                currentSection = new QcSection { Name = t.Substring(1, t.Length - 2).Trim() };
                Sections.Add(currentSection);
            }
            else if (currentSection != null && !string.IsNullOrWhiteSpace(t))
            {
                currentSection.Items.Add(t);
            }
        }
    }
}

public sealed class QcSection
{
    public string Name { get; set; } = "";
    public List<string> Items { get; set; } = new();
}

public sealed class ProductionOrderTemplate
{
    public string Header { get; set; } = "";
    public string OrderLine { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Signatures { get; set; } = "";
}

// ----- Сгенерированный документ (структура для рендера в Excel/PDF/превью) -----
public enum GeneratedDocumentKind
{
    Generic,
    ProductionOrder,
    Passport,
    Checklist,
    Specification,
}

public sealed class GeneratedDocument
{
    public GeneratedDocumentKind Kind { get; set; } = GeneratedDocumentKind.Generic;
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public List<DocSection> Sections { get; set; } = new();

    public string ToPlainText()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Title);
        if (!string.IsNullOrWhiteSpace(Subtitle)) sb.AppendLine(Subtitle);
        sb.AppendLine();
        foreach (var s in Sections)
        {
            if (!string.IsNullOrWhiteSpace(s.Name))
            {
                sb.AppendLine("── " + s.Name + " ──");
            }
            foreach (var r in s.Rows)
            {
                if (r.IsChecklistItem)
                    sb.AppendLine($"  [ ] {r.Label}");
                else
                {
                    var val = (r.Value ?? "").Replace("\n", "\n        ");
                    var note = string.IsNullOrWhiteSpace(r.Note) ? "" : $" | {r.Note}";
                    sb.AppendLine($"  {r.Label}: {val}{note}");
                }
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public GeneratedDocument RemoveEmptyRows()
    {
        foreach (var section in Sections)
        {
            section.Rows.RemoveAll(row => !row.IsChecklistItem
                && IsBlankValue(row.Value)
                && string.IsNullOrWhiteSpace(row.Note));
        }
        Sections.RemoveAll(section => section.Rows.Count == 0 && !section.IsSignatureTable);
        return this;
    }

    private static bool IsBlankValue(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() is "-" or "—";
}

public sealed class DocSection
{
    public string Name { get; set; } = "";
    public bool IsSignatureTable { get; set; }
    public List<DocRow> Rows { get; set; } = new();
}

public sealed class DocRow
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Note { get; set; } = "";
    public bool IsChecklistItem { get; set; }

    public DocRow() { }
    public DocRow(string label, string value) { Label = label; Value = value; }
    public DocRow(string label, string value, string note) { Label = label; Value = value; Note = note; }
}

/// <summary>Сборка трёх выходных документов из конфигурации и результата расчёта.</summary>
public static class DocumentBuilder
{
    private static string I(double v) => v.ToString("0");
    private static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);
    private static string CurrentA(double v) => $"{I(v)} А";
    /// <summary>Толщина металла — с одним знаком (1.5 / 2.0 / 2.5 / 3.0), как в исходной книге.</summary>
    private static string Th(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    public static GeneratedDocument BuildPassport(ProjectConfig c, CalculationResult res, CatalogStore store)
    {
        var t = store.GetTransformer(c.Mark);
        var doc = new GeneratedDocument { Kind = GeneratedDocumentKind.Passport, Title = "ПАСПОРТ МЕТАЛЛОКОНСТРУКЦИИ И ГАБАРИТЫ" };
        var s = new DocSection { Name = "" };
        s.Rows.Add(new("Сетевая компания", c.GridCompany));
        s.Rows.Add(new("Материал корпуса", c.SteelType));
        s.Rows.Add(new("Толщина металла (мм)", Th(c.Thickness)));
        s.Rows.Add(new("Материал пола", MaterialThicknessDescription(c.FloorMaterial, c.FloorThickness)));
        s.Rows.Add(new("Материал дверей", MaterialThicknessDescription(c.DoorMaterial, c.DoorThickness)));
        s.Rows.Add(new("Материал съемной панели", MaterialThicknessDescription(c.RemovablePanelMaterial, c.RemovablePanelThickness)));
        s.Rows.Add(new("Основание (Швеллер)", c.Channel));
        s.Rows.Add(new("Цвет окраски корпуса", c.BodyColor));
        s.Rows.Add(new("Цвет окраски дверей", c.DoorColor));
        s.Rows.Add(new("Окраска по зонам", ColorZonesDescription(c)));
        s.Rows.Add(new("Климатическое исполнение", ValueOrDash(c.ClimateExecution)));
        s.Rows.Add(new("Степень защиты", ValueOrDash(c.ProtectionDegree)));
        s.Rows.Add(new("Двери по отсекам", DoorCompartmentsDescription(c)));
        s.Rows.Add(new("Замки", DoorLockDescription(c)));
        s.Rows.Add(new("Детали корпуса", EnclosureDetailsDescription(c)));
        s.Rows.Add(new("Маркировка и логотип", MarkingDescription(c)));
        s.Rows.Add(new("Заземление", GroundingDescription(c)));
        s.Rows.Add(new("Вентиляция", ValueOrDash(c.VentilationType)));
        s.Rows.Add(new("Табличка", NameplateDescription(c)));
        if (!string.IsNullOrWhiteSpace(c.EnclosureNotes))
            s.Rows.Add(new("Примечание по корпусу", c.EnclosureNotes.Trim()));
        s.Rows.Add(new("Итоговая длина КТПН (мм)", I(res.LengthFinal)));
        s.Rows.Add(new("Итоговая ширина КТПН (мм)", I(res.WidthFinal)));
        s.Rows.Add(new("Итоговая высота КТПН (мм)", I(res.HeightFinal)));
        s.Rows.Add(new("Масса основания (кг)", I(res.BaseMass)));
        s.Rows.Add(new("Масса корпуса без основания (кг)", I(res.BodyMass)));
        s.Rows.Add(new("Масса силового трансформатора (кг)", I(t?.MassKg ?? 0)));
        s.Rows.Add(new("Итоговая масса брутто (кг)", I(res.GrossMass)));
        s.Rows.Add(new("Предварительный довес оборудования (кг)", I(res.AdditionalMassEstimate)));
        s.Rows.Add(new("Ориентировочная масса с оборудованием (кг)", I(res.GrossMassEstimated)));
        doc.Sections.Add(s);
        return doc;
    }

    public static GeneratedDocument BuildProductionOrder(ProjectConfig c, CalculationResult res, CatalogStore store, DocTemplates tpl)
    {
        var t = store.GetTransformer(c.Mark);
        var po = tpl.ProductionOrder;
        var doc = new GeneratedDocument
        {
            Kind = GeneratedDocumentKind.ProductionOrder,
            Title = string.IsNullOrWhiteSpace(po.OrderLine) ? "ПРИКАЗ НА ПРОИЗВОДСТВО" : po.OrderLine,
            Subtitle = string.IsNullOrWhiteSpace(po.Subtitle)
                ? "На запуск в производство электрооборудования"
                : po.Subtitle,
        };

        var approvals = new DocSection
        {
            Name = "Согласование запуска",
            IsSignatureTable = true,
        };
        var approvalNotes = new[]
        {
            "____________ / ____________",
            "____________ / ____________",
            "____________ / ____________",
        };
        var approvalLabels = ApprovalLabels(po.Header);
        for (var i = 0; i < approvalLabels.Count; i++)
            approvals.Rows.Add(new(approvalLabels[i], "____________ 202_ г.", i < approvalNotes.Length ? approvalNotes[i] : "____________ / ____________"));
        if (!string.IsNullOrWhiteSpace(po.Signatures))
            approvals.Rows.Add(new("Подписи", po.Signatures.Replace("Подписи:", "").Trim()));
        doc.Sections.Add(approvals);

        doc.Sections.Add(new DocSection
        {
            Name = "Параметры изделия",
            Rows =
            {
                new("Заводской номер / дата", "№ ________ / __.__.202_ г."),
                new("Выпуск", ReleaseStatusDescription(c, res)),
                new("Сетевая компания / типоразмер", $"{ValueOrDash(c.GridCompany)} / {Meters(res.WidthFinal)}x{Meters(res.LengthFinal)}"),
                new("Напряжение / мощность / ТМГ", $"{ValueOrDash(c.Voltage)} / {I(t?.PowerKva ?? 0)} кВА / {ValueOrDash(c.Mark)}"),
                new("Тип КТПН, габариты ШхДхВ", $"{ValueOrDash(c.RuvnType)} РУВН - {ValueOrDash(c.RuvnExecution)} / РУНН - {ValueOrDash(c.OutgoingExecution)}\n{I(res.WidthFinal)}x{I(res.LengthFinal)}x{I(res.HeightFinal)}"),
                new("Материал корпуса", $"{ValueOrDash(c.SteelType)} {Th(c.Thickness)} мм"),
                new("Пол / двери / съемная панель", $"{MaterialThicknessDescription(c.FloorMaterial, c.FloorThickness)} / {MaterialThicknessDescription(c.DoorMaterial, c.DoorThickness)} / {MaterialThicknessDescription(c.RemovablePanelMaterial, c.RemovablePanelThickness)}"),
                new("Основание", $"Швеллер {ValueOrDash(c.Channel)}"),
                new("Климат / IP", $"{ValueOrDash(c.ClimateExecution)} / {ValueOrDash(c.ProtectionDegree)}"),
                new("Двери / замки", DoorLockDescription(c)),
                new("Детали корпуса", EnclosureDetailsDescription(c)),
                new("Маркировка / логотип", MarkingDescription(c)),
                new("Заземление / вентиляция", GroundingVentilationDescription(c)),
                new("Окраска / толщина ЛКП", $"{ColorZonesDescription(c)} / ____ мкм"),
                new("Ввод 6-10 кВ", RuvnEquipmentDescription(c)),
                new("Ввод 0,4 кВ (оборудование)", LvEquipmentDescription(c)),
                new("Собственные нужды / РИСЭ", AuxiliaryNeedsDescription(c)),
                new("Шины РУВН / РУНН / N", $"{BusbarDescription(c.BusbarHvMaterial, res.BusbarHv)} / {BusbarDescription(c.BusbarLvMaterial, res.BusbarLv)} / {BusbarDescription(c.BusbarNMaterial, res.BusbarN)}"),
                new("Масса с оборудованием", MassEstimateDescription(res)),
                new("Примечание", ProductionNotes(c)),
            },
        });

        doc.Sections.Add(new DocSection
        {
            Name = "Участок",
            IsSignatureTable = true,
            Rows =
            {
                new("Заготовительный", "ФИО / подпись", "Дата"),
                new("Малярный", "ФИО / подпись", "Дата"),
                new("Сварочный", "ФИО / подпись", "Дата"),
                new("Монтажный ВН", "ФИО / подпись", "Дата"),
                new("Монтажный НН", "ФИО / подпись", "Дата"),
                new("Испытания", "ФИО / подпись", "Дата"),
                new("Комплектация", "ФИО / подпись", "Дата"),
            },
        });

        doc.Sections.Add(new DocSection
        {
            Name = "Завершение",
            Rows =
            {
                new("Дата изготовления", "____________________ 202_ г."),
                new("Дата отгрузки", "____________________ 202_ г."),
            },
        });

        return doc;
    }

    /// <summary>Описание всех последовательно выбранных вводных аппаратов РУНН.</summary>
    public static string InputDeviceDescription(ProjectConfig c)
    {
        var devices = ProjectConfigNormalizer.GetLvInputDevices(c);
        if (devices.Count == 0)
            return "Не установлен";

        return string.Join("; ", devices.Select(device =>
        {
            var name = device.Kind switch
            {
                LvInputDeviceKind.SwitchDisconnectFuse => "ПВР/NH",
                LvInputDeviceKind.Disconnector => "Рубильник РЕ",
                _ => "Вводной АВ",
            };
            return $"{name} {CurrentA(device.Nominal)} ({ValueOrDash(device.Manufacturer)})";
        }));
    }

    private static string ReleaseStatusDescription(ProjectConfig c, CalculationResult res)
    {
        if (!res.HasErrors)
            return "без блокирующих ошибок";
        return c.ErrorsAcceptedForWork
            ? "с согласованными ошибками проектирования"
            : "заблокирован: есть ошибки проектирования";
    }

    private static string RuvnEquipmentDescription(ProjectConfig c)
    {
        if (!RuvnEngineering.HasRuvn(c))
            return "РУВН не предусмотрена";

        var parts = new List<string>
        {
            $"{ValueOrDash(c.RuvnType)} РУВН",
            ValueOrDash(c.RuvnExecution),
        };

        foreach (var branch in RuvnEngineering.Branches(c))
        {
            if (RuvnEngineering.IsVacuumBreaker(branch.SwitchType))
            {
                parts.Add(RuvnVacuumBranchDescription(branch));
                continue;
            }

            var apparatus = string.Join(" ", new[]
            {
                branch.Title,
                ValueOrDash(branch.SwitchType),
                branch.SwitchNominal > 0 ? CurrentA(branch.SwitchNominal) : "",
            }.Where(x => !string.IsNullOrWhiteSpace(x) && x != "-"));
            if (!string.IsNullOrWhiteSpace(apparatus))
                parts.Add(apparatus);
        }

        if (c.RuvnSurgeArrester)
            parts.Add($"ОПН: {ValueOrDash(c.RuvnSurgeArresterLocation)}, {ValueOrDash(c.RuvnSurgeArresterThroughput)}");

        return string.Join("; ", parts.Where(p => !string.IsNullOrWhiteSpace(p) && p != "-"));
    }

    private static string RuvnVacuumBranchDescription(RuvnBranchSelection branch)
    {
        var equipment = branch.Equipment ?? new RuvnBranchEquipmentConfig();
        var composition = string.Join(" - ", new[]
        {
            RuvnEngineering.HasVisibleBreak(equipment.VisibleBreakBefore) ? equipment.VisibleBreakBefore : "",
            $"{ValueOrDash(equipment.VacuumBreakerModel)} {CurrentA(equipment.VacuumBreakerNominal)} {F(equipment.VacuumBreakerBreakingCurrentKa)} кА".Trim(),
            RuvnEngineering.HasVisibleBreak(equipment.VisibleBreakAfter) ? equipment.VisibleBreakAfter : "",
        }.Where(x => !string.IsNullOrWhiteSpace(x) && x != "-"));

        var parts = new List<string>
        {
            $"{branch.Title}: {composition}",
            string.IsNullOrWhiteSpace(equipment.RzaTerminal) ? "" : $"РЗА {equipment.RzaTerminal}",
            string.IsNullOrWhiteSpace(RuvnEngineering.RzaFunctions(equipment)) ? "" : RuvnEngineering.RzaFunctions(equipment),
            string.IsNullOrWhiteSpace(equipment.ProtectionCtRatio) ? "" : $"ТТ защиты {equipment.ProtectionCtRatio} x{Math.Max(1, equipment.ProtectionCtQuantity)}",
            equipment.HasTtnp ? $"ТТНП {ValueOrDash(equipment.TtnpModel)}" : "",
            equipment.HasVoltageTransformer ? $"ТН {ValueOrDash(equipment.VoltageTransformerModel)}" : "",
        };

        return string.Join("; ", parts.Where(p => !string.IsNullOrWhiteSpace(p) && p != "-"));
    }

    private static string RuvnFuseDescription(ProjectConfig c, CatalogStore store)
    {
        if (!RuvnEngineering.HasRuvn(c))
            return "Не предусмотрены";

        var parts = new List<string>();
        var hasVacuumProtection = false;
        var transformerNeedsFuseRecommendation = false;
        foreach (var branch in RuvnEngineering.Branches(c))
        {
            if (RuvnEngineering.IsVacuumBreaker(branch.SwitchType))
            {
                hasVacuumProtection = true;
                continue;
            }

            if (branch.Key == "transformer")
                transformerNeedsFuseRecommendation = true;

            if (!branch.FuseOn)
                continue;

            parts.Add($"{branch.Title}: {ValueOrDash(branch.FuseType)} {CurrentTextOrDash(branch.FuseNominal)}".Trim());
        }

        var recommended = RuvnEngineering.RecommendedFuseNominal(c, store);
        if (transformerNeedsFuseRecommendation && !string.IsNullOrWhiteSpace(recommended))
            parts.Add($"рекоменд. под ТМГ: {recommended}");

        if (parts.Count == 0 && hasVacuumProtection)
            return "Защита вакуумным выключателем и РЗА";

        return parts.Count == 0 ? "Не выбраны" : string.Join("; ", parts);
    }

    private static string LvEquipmentDescription(ProjectConfig c)
    {
        var parts = new List<string> { InputDeviceDescription(c) };
        if (c.HasCt)
            parts.Add($"ТТ {CtDescription(c.CtRatio, c.CtAccuracyClass)}");
        if (c.HasCtKip)
            parts.Add($"ТТ КИП {CtDescription(c.CtKipRatio, c.CtKipAccuracyClass)}");
        if (c.HasMeter)
            parts.Add("прибор учета");
        var feeders = OutgoingFeederDescriptions(c).ToList();
        if (feeders.Count > 0)
        {
            parts.Add("отх. линии: " + string.Join("; ", feeders));
        }
        else
        {
            if (c.AvOn && c.AvQty > 0)
                parts.Add($"отх. линии АВ {ValueOrDash(c.AvBrand)} - {c.AvQty}");
            if (c.RpsOn && c.RpsQty > 0)
                parts.Add($"РПС {ValueOrDash(c.RpsBrand)} - {c.RpsQty}");
        }
        return string.Join("; ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string AuxiliaryNeedsDescription(ProjectConfig c)
    {
        var aux = c.AuxiliaryNeeds;
        if (aux is null || (!aux.HasAuxiliaryCabinet && !aux.HasRise))
            return "Не предусмотрены";

        var parts = new List<string>();
        if (aux.HasAuxiliaryCabinet)
        {
            var cabinet = string.Join(" ", new[]
            {
                "ЩСН",
                ValueOrDash(aux.CabinetModel),
                aux.MainBreakerNominal > 0 ? $"ввод {CurrentA(aux.MainBreakerNominal)}" : "",
            }.Where(x => !string.IsNullOrWhiteSpace(x) && x != "-"));
            parts.Add(cabinet);

            if (aux.HasLighting)
            {
                var fixtureQuantity = aux.LightingFixtureQuantity > 0
                    ? $"{aux.LightingFixtureQuantity} шт."
                    : "количество уточнить";
                var lighting = new List<string>
                {
                    fixtureQuantity,
                    $"управление {ValueOrDash(aux.LightingControlMode)}",
                };
                if (!string.IsNullOrWhiteSpace(aux.LightingAreas))
                    lighting.Add($"отсеки {aux.LightingAreas.Trim()}");
                if (aux.RepairLightingVoltage > 0)
                    lighting.Add($"ремонтное {aux.RepairLightingVoltage} В");
                if (aux.OutdoorLightingEnabled)
                    lighting.Add("наружное освещение");
                parts.Add($"освещение: {string.Join(", ", lighting)}");
            }

            if (aux.SocketEnabled)
                parts.Add($"розетка: {Math.Max(1, aux.SocketQuantity)} шт., {ValueOrDash(aux.SocketModel)}");

            if (aux.HeatingEnabled)
            {
                var heating = new List<string>
                {
                    $"{Math.Max(1, aux.HeaterQuantity)} шт.",
                    ValueOrDash(aux.HeaterModel),
                    $"термостат {ValueOrDash(aux.ThermostatModel)}",
                };
                if (aux.MeterHeatingEnabled)
                    heating.Add("обогрев счетчиков");
                parts.Add($"обогрев: {string.Join(", ", heating.Where(x => !string.IsNullOrWhiteSpace(x) && x != "-"))}");
            }

            if (aux.VentilationEnabled)
            {
                parts.Add($"вентиляция: {Math.Max(1, aux.FanQuantity)} шт., {ValueOrDash(aux.FanModel)}");
            }

            if (aux.OpsEnabled)
            {
                var ops = string.Join(" ", new[]
                {
                    "ОПС",
                    ValueOrDash(aux.OpsType),
                    ValueOrDash(aux.OpsManufacturer),
                    ValueOrDash(aux.OpsModel),
                    aux.OpsLoops > 0 ? $"{aux.OpsLoops} шлейф." : "",
                }.Where(x => !string.IsNullOrWhiteSpace(x) && x != "-"));
                parts.Add(ops);
            }
        }

        if (aux.HasRise)
        {
            var rise = string.Join(", ", new[]
            {
                ValueOrDash(aux.RieseType),
                aux.RiesePowerVa > 0 ? $"{aux.RiesePowerVa} ВА" : "",
                aux.RieseAutonomyMinutes > 0 ? $"{aux.RieseAutonomyMinutes} мин" : "",
            }.Where(x => !string.IsNullOrWhiteSpace(x) && x != "-"));
            parts.Add($"РИСЭ: {(string.IsNullOrWhiteSpace(rise) ? "предусмотрен" : rise)}");
        }

        return string.Join("; ", parts);
    }

    private static IEnumerable<string> OutgoingFeederDescriptions(ProjectConfig c)
    {
        foreach (var feeder in (c.OutgoingFeeders ?? new List<OutgoingFeederConfig>()).OrderBy(f => f.DeviceType).ThenBy(f => f.Number))
        {
            var title = $"{ValueOrDash(feeder.DeviceType)}-{feeder.Number}";
            var purpose = string.IsNullOrWhiteSpace(feeder.Purpose)
                ? ""
                : $" ({feeder.Purpose.Trim()})";
            var device = string.Join(" ", new[]
            {
                ValueOrDash(feeder.Manufacturer),
                ValueOrDash(feeder.Model),
                feeder.Nominal > 0 ? CurrentA(feeder.Nominal) : "",
            }.Where(x => !string.IsNullOrWhiteSpace(x) && x != "-"));
            var details = new List<string>();
            if (feeder.IsReserve)
                details.Add("резерв");
            if (!string.IsNullOrWhiteSpace(feeder.CableMark) || !string.IsNullOrWhiteSpace(feeder.CableSection))
                details.Add($"кабель {CableDescription(feeder)}");
            details.Add(MeteringDescription(feeder));
            if (HasFeederMetering(feeder) && !string.IsNullOrWhiteSpace(feeder.TtRatio))
                details.Add($"ТТ {feeder.TtRatio}");
            if (!string.IsNullOrWhiteSpace(feeder.Notes))
                details.Add(feeder.Notes.Trim());

            yield return $"{title}{purpose}: {device}; {string.Join(", ", details.Where(x => !string.IsNullOrWhiteSpace(x)))}";
        }
    }

    private static string MeteringDescription(OutgoingFeederConfig feeder)
    {
        var type = string.IsNullOrWhiteSpace(feeder.MeteringType)
            ? (feeder.HasMeter ? "Коммерческий" : "Нет")
            : feeder.MeteringType.Trim();

        return type.Equals("Нет", StringComparison.OrdinalIgnoreCase)
            ? "без учета"
            : $"учет: {type.ToLowerInvariant()}";
    }

    private static bool HasFeederMetering(OutgoingFeederConfig feeder)
    {
        var type = string.IsNullOrWhiteSpace(feeder.MeteringType)
            ? (feeder.HasMeter ? "Коммерческий" : "Нет")
            : feeder.MeteringType.Trim();
        return feeder.HasMeter || !type.Equals("Нет", StringComparison.OrdinalIgnoreCase);
    }

    private static string CableDescription(OutgoingFeederConfig feeder)
    {
        var mark = ValueOrDash(feeder.CableMark);
        var section = ValueOrDash(feeder.CableSection);
        if (mark == "-" && section == "-")
            return "-";
        if (mark == "-")
            return section;
        if (section == "-")
            return mark;
        return $"{mark} {section}";
    }

    private static string CtDescription(string ratio, string accuracyClass)
    {
        var result = ValueOrDash(ratio);
        return string.IsNullOrWhiteSpace(accuracyClass)
            ? result
            : $"{result}, класс точности {accuracyClass.Trim()}";
    }

    private static string DoorLockDescription(ProjectConfig c)
    {
        var parts = new List<string>
        {
            DoorCompartmentsDescription(c),
            LockingDescription(c),
        };
        return string.Join("; ", parts.Where(p => !string.IsNullOrWhiteSpace(p) && p != "-"));
    }

    private static string LockingDescription(ProjectConfig c)
    {
        var parts = new List<string>();
        if (c.HasRigelLock)
            parts.Add("ригельный замок");

        var networkLock = ValueOrDash(c.NetworkLockType);
        if (networkLock != "-" && !networkLock.Equals("Нет", StringComparison.OrdinalIgnoreCase))
            parts.Add($"сетевой замок {networkLock}");

        if (c.HasPadlockProvision)
            parts.Add("проушины под навесной замок");

        if (parts.Count == 0 && !string.IsNullOrWhiteSpace(c.LockType))
            parts.Add(c.LockType.Trim());

        return parts.Count == 0 ? "замки не заданы" : string.Join(", ", parts);
    }

    private static string DoorCompartmentsDescription(ProjectConfig c) =>
        UseLegacyDoorConfiguration(c)
            ? ValueOrDash(c.DoorConfiguration)
            : $"РУВН: {ValueOrDash(c.RuvnDoorConfiguration)}; РУНН: {ValueOrDash(c.RunnDoorConfiguration)}; ТР: {ValueOrDash(c.TransformerDoorConfiguration)}";

    private static bool UseLegacyDoorConfiguration(ProjectConfig c) =>
        !string.IsNullOrWhiteSpace(c.DoorConfiguration)
        && !c.DoorConfiguration.Equals("Двухстворчатые распашные", StringComparison.OrdinalIgnoreCase)
        && c.RuvnDoorConfiguration.Equals("Двухстворчатые распашные", StringComparison.OrdinalIgnoreCase)
        && c.RunnDoorConfiguration.Equals("Двухстворчатые распашные", StringComparison.OrdinalIgnoreCase)
        && c.TransformerDoorConfiguration.Equals("Распашные с двух сторон", StringComparison.OrdinalIgnoreCase);

    private static string GroundingDescription(ProjectConfig c) =>
        ValueOrDash(c.GroundingType);

    private static string GroundingVentilationDescription(ProjectConfig c) =>
        $"заземление: {GroundingDescription(c)}; вентиляция: {ValueOrDash(c.VentilationType)}; дефлектор: {(c.HasRoofDeflector ? "предусмотрен" : "не предусмотрен")}";

    private static string NameplateDescription(ProjectConfig c) =>
        c.HasNameplate ? "предусмотрена" : "не предусмотрена";

    private static string ProductionNotes(ProjectConfig c)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(c.Notes))
            parts.Add(c.Notes.Trim());
        else
            parts.Add("Сетчатый барьер");

        parts.Add(c.HasNameplate ? "паспортная / предупреждающая табличка" : "табличка не предусмотрена");

        if (!string.IsNullOrWhiteSpace(c.EnclosureNotes))
            parts.Add(c.EnclosureNotes.Trim());
        if (!string.IsNullOrWhiteSpace(c.MarkingNotes))
            parts.Add(c.MarkingNotes.Trim());

        return string.Join("; ", parts.Where(p => !string.IsNullOrWhiteSpace(p) && p != "-"));
    }

    private static string ColorZonesDescription(ProjectConfig c) =>
        string.Join("; ", new[]
        {
            $"корпус {ValueOrDash(c.BodyColor)}",
            $"двери {ValueOrDash(c.DoorColor)}",
            $"крыша {ValueOrDash(c.RoofColor)}",
            $"основание/цоколь {ValueOrDash(c.BaseColor)}",
            $"внутренние панели {ValueOrDash(c.InternalPanelColor)}",
            $"логотип {ValueOrDash(c.LogoColor)}",
        });

    private static string MaterialThicknessDescription(string material, double thickness) =>
        $"{ValueOrDash(material)} {Th(thickness)} мм";

    private static string EnclosureDetailsDescription(ProjectConfig c) =>
        string.Join("; ", new[]
        {
            c.HasRoofDeflector ? "дефлектор на крыше" : "",
            c.HasDoorCanopies ? "козырьки над дверями" : "",
            c.HasDoorSeals ? "уплотнения дверей" : "",
            c.HasTransformerMeshDoors ? "сетчатые двери трансформаторного отсека" : "",
            c.HasLouverAnimalProtection ? "защита жалюзи от животных" : "",
            c.HasAntiVandalHinges ? "антивандальные петли" : "",
            c.HasDoorSealing ? "пломбировка дверей" : "",
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string MarkingDescription(ProjectConfig c) =>
        string.Join("; ", new[]
        {
            c.HasLogo ? $"логотип: {ValueOrDash(c.LogoPlacement)}" : "логотип не предусмотрен",
            c.HasWarningLabels ? "предупреждающие надписи" : "",
            c.HasDispatcherNameplate ? "диспетчерское наименование" : "",
            c.HasFeederLabels ? "пофидерная маркировка" : "",
            string.IsNullOrWhiteSpace(c.MarkingNotes) ? "" : c.MarkingNotes.Trim(),
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string MassEstimateDescription(CalculationResult res) =>
        $"база {I(res.GrossMass)} кг; довес {I(res.AdditionalMassEstimate)} кг; ориентировочно {I(res.GrossMassEstimated)} кг";

    private static string Meters(double millimeters) =>
        (millimeters / 1000.0).ToString("0.#", CultureInfo.GetCultureInfo("ru-RU"));

    private static string ValueOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private static string BusbarDescription(string material, string section)
    {
        var mat = ValueOrDash(material);
        var sec = ValueOrDash(section);
        if (mat == "-" && sec == "-")
            return "-";
        if (mat == "-")
            return sec;
        if (sec == "-")
            return mat;
        return $"{mat} {sec}";
    }

    private static string CurrentTextOrDash(string? value)
    {
        var text = ValueOrDash(value);
        if (text == "-")
            return text;

        var last = text[^1];
        if (last is 'А' or 'а' or 'A' or 'a')
        {
            var number = text[..^1].TrimEnd();
            return string.IsNullOrWhiteSpace(number) ? text : $"{number} А";
        }

        return text;
    }

    private static List<string> ApprovalLabels(string? header)
    {
        var labels = (header ?? "")
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        return labels.Count > 0
            ? labels
            : new List<string> { "В работу", "Конструкторская служба", "Отдел снабжения" };
    }

    public static GeneratedDocument BuildChecklist(ProjectConfig c, CalculationResult res, CatalogStore store, DocTemplates tpl)
    {
        var qc = tpl.QcChecklist;
        var doc = new GeneratedDocument
        {
            Kind = GeneratedDocumentKind.Checklist,
            Title = string.IsNullOrWhiteSpace(qc.Title) ? "Акт осмотра КТПН №_______" : qc.Title,
            Subtitle = qc.Committee,
        };
        foreach (var sec in qc.Sections)
        {
            // Skip portal section if no RUVN or if RUVN execution is Cable
            if (sec.Name.Contains("ВЫСОКОВОЛЬТНЫЙ ПОРТАЛ", StringComparison.OrdinalIgnoreCase))
            {
                if (c.RuvnType == "Нет"
                    || c.RuvnExecution?.Contains("возд", StringComparison.OrdinalIgnoreCase) != true)
                    continue; // Skip this section
            }

            var ds = new DocSection { Name = sec.Name };
            foreach (var item in sec.Items)
            {
                // Skip specific RUVN items if RuvnType is "Нет"
                if (c.RuvnType == "Нет" && item.Contains("РУВН", StringComparison.OrdinalIgnoreCase))
                    continue;

                ds.Rows.Add(new DocRow { Label = Substitute(item, c, res), IsChecklistItem = true });
            }
            if (ds.Rows.Any())
                doc.Sections.Add(ds);
        }

        if (!doc.Sections.Any(s => s.Name.Contains("ЗАМЕЧАН", StringComparison.OrdinalIgnoreCase)))
        {
            doc.Sections.Add(new DocSection
            {
                Name = "ЗАМЕЧАНИЯ И ПОДПИСИ",
                Rows =
                {
                    new("Замечания ОТК", "________________________________________________________________"),
                    new("Инженер по качеству", "________________ / ____________________    Дата ____________"),
                    new("Представитель производства", "________________ / ____________________    Дата ____________"),
                },
            });
        }

        return doc;
    }

    public static GeneratedDocument BuildSpecification(ProjectConfig c, CalculationResult res, CatalogStore store)
    {
        var doc = new GeneratedDocument
        {
            Kind = GeneratedDocumentKind.Specification,
            Title = "СПЕЦИФИКАЦИЯ ОБОРУДОВАНИЯ",
            Subtitle = $"{ValueOrDash(c.ProjectName)} / {ValueOrDash(c.Mark)}",
        };

        var items = SpecificationBuilder.GenerateSpecification(c, res, store);
        foreach (var group in items.GroupBy(i => string.IsNullOrWhiteSpace(i.Section) ? "Оборудование" : i.Section))
        {
            var section = new DocSection { Name = group.Key };
            foreach (var item in group)
            {
                section.Rows.Add(new(item.Position, SpecificationValue(item), SpecificationNote(item)));
            }
            doc.Sections.Add(section);
        }

        return doc;
    }

    private static string SpecificationValue(SpecificationItem item) =>
        string.Join(" | ", new[]
        {
            ValueOrDash(item.Designation),
            ValueOrDash(item.Name),
            ValueOrDash(item.Type),
            ValueOrDash(item.Manufacturer),
            ValueOrDash(item.Nominal),
        }.Where(x => x != "-"));

    private static string SpecificationNote(SpecificationItem item)
    {
        var quantity = $"{Math.Max(1, item.Quantity).ToString(CultureInfo.InvariantCulture)} {ValueOrDash(item.Unit)}".Trim();
        var parts = new List<string> { $"Количество: {quantity}" };
        if (!string.IsNullOrWhiteSpace(item.Notes) && item.Notes != "-")
            parts.Add(item.Notes);
        return string.Join("; ", parts);
    }

    // Строки могут прийти как JSON null из файла, отредактированного извне;
    // Replace с null-заменой бросает ArgumentNullException.
    private static string Substitute(string text, ProjectConfig c, CalculationResult res) => text
        .Replace("{width}", I(res.WidthFinal))
        .Replace("{length}", I(res.LengthFinal))
        .Replace("{height}", I(res.HeightFinal))
        .Replace("{steelType}", c.SteelType ?? "")
        .Replace("{thickness}", Th(c.Thickness))
        .Replace("{bodyColor}", c.BodyColor ?? "")
        .Replace("{doorColor}", c.DoorColor ?? "")
        .Replace("{gridCompany}", c.GridCompany ?? "")
        .Replace("{voltage}", c.Voltage ?? "")
        .Replace("{busbarHv}", res.BusbarHv ?? "")
        .Replace("{busbarLv}", res.BusbarLv ?? "")
        .Replace("{busbarN}", res.BusbarN ?? "");
}

public static class DocumentPackageBuilder
{
    public static IReadOnlyList<GeneratedDocument> BuildAll(
        ProjectConfig config,
        CalculationResult result,
        CatalogStore store,
        DocTemplates templates) =>
        string.IsNullOrWhiteSpace(config.ProductTypeId)
        || config.ProductTypeId.Equals(ProductTypeIds.SingleKtpn, StringComparison.OrdinalIgnoreCase)
            ? new[]
            {
                DocumentBuilder.BuildProductionOrder(config, result, store, templates).RemoveEmptyRows(),
                DocumentBuilder.BuildPassport(config, result, store).RemoveEmptyRows(),
                DocumentBuilder.BuildChecklist(config, result, store, templates).RemoveEmptyRows(),
                DocumentBuilder.BuildSpecification(config, result, store).RemoveEmptyRows(),
            }
            : ProductDocumentBuilder.BuildAll(config, result, store)
                .Select(document => document.RemoveEmptyRows())
                .ToArray();
}
