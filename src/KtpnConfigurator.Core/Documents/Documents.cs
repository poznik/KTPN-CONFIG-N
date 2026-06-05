using System.Globalization;
using System.Text;
using System.Text.Json;
using KtpnConfigurator.Core.Catalogs;
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
        
        using var fs = File.OpenRead(pathToLoad);
        return JsonSerializer.Deserialize<DocTemplates>(fs,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
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
public sealed class GeneratedDocument
{
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
                    sb.AppendLine($"  {r.Label}: {val}");
                }
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

public sealed class DocSection
{
    public string Name { get; set; } = "";
    public List<DocRow> Rows { get; set; } = new();
}

public sealed class DocRow
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsChecklistItem { get; set; }

    public DocRow() { }
    public DocRow(string label, string value) { Label = label; Value = value; }
}

/// <summary>Сборка трёх выходных документов из конфигурации и результата расчёта.</summary>
public static class DocumentBuilder
{
    private static string I(double v) => v.ToString("0");
    /// <summary>Толщина металла — с одним знаком (1.5 / 2.0 / 2.5 / 3.0), как в исходной книге.</summary>
    private static string Th(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    public static GeneratedDocument BuildPassport(ProjectConfig c, CalculationResult res, CatalogStore store)
    {
        var t = store.GetTransformer(c.Mark);
        var doc = new GeneratedDocument { Title = "ПАСПОРТ МЕТАЛЛОКОНСТРУКЦИИ И ГАБАРИТЫ" };
        var s = new DocSection { Name = "" };
        s.Rows.Add(new("Сетевая компания", c.GridCompany));
        s.Rows.Add(new("Материал корпуса", c.SteelType));
        s.Rows.Add(new("Толщина металла (мм)", Th(c.Thickness)));
        s.Rows.Add(new("Основание (Швеллер)", c.Channel));
        s.Rows.Add(new("Цвет окраски корпуса", c.BodyColor));
        s.Rows.Add(new("Цвет окраски дверей", c.DoorColor));
        s.Rows.Add(new("Итоговая длина КТПН (мм)", I(res.LengthFinal)));
        s.Rows.Add(new("Итоговая ширина КТПН (мм)", I(res.WidthFinal)));
        s.Rows.Add(new("Итоговая высота КТПН (мм)", I(res.HeightFinal)));
        s.Rows.Add(new("Масса основания (кг)", I(res.BaseMass)));
        s.Rows.Add(new("Масса корпуса без основания (кг)", I(res.BodyMass)));
        s.Rows.Add(new("Масса силового трансформатора (кг)", I(t?.MassKg ?? 0)));
        s.Rows.Add(new("Итоговая Масса Брутто (кг)", I(res.GrossMass)));
        doc.Sections.Add(s);
        return doc;
    }

    public static GeneratedDocument BuildProductionOrder(ProjectConfig c, CalculationResult res, CatalogStore store, DocTemplates tpl)
    {
        var t = store.GetTransformer(c.Mark);
        var po = tpl.ProductionOrder;
        // Преамбула: маршрут согласования (исходный лист, A1) + подзаголовок.
        var preamble = string.Join("\n",
            new[] { po.Header, po.Subtitle }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var doc = new GeneratedDocument
        {
            Title = string.IsNullOrWhiteSpace(po.OrderLine) ? "ПРИКАЗ НА ПРОИЗВОДСТВО" : po.OrderLine,
            Subtitle = preamble,
        };
        var s = new DocSection { Name = "" };
        s.Rows.Add(new("Заводской номер / Дата\nСети / Типовой проект", $"№________ / __.__.202_ г.\n{c.GridCompany}"));
        s.Rows.Add(new("Напряжение / Мощность / ТМГ",
            $"{c.Voltage} / {I(t?.PowerKva ?? 0)} кВА / {c.Mark}"));
        s.Rows.Add(new("ПКТ (101,102,103)\nТип КТПН\nГабариты ШхДхВ",
            $"{c.FuseType} {c.FuseNominal}\n{c.RuvnType}\n{I(res.WidthFinal)}x{I(res.LengthFinal)}x{I(res.HeightFinal)}"));
        s.Rows.Add(new("Металл корпуса", $"{c.SteelType} {Th(c.Thickness)} мм"));
        s.Rows.Add(new("Металл пола", $"Швеллер {c.Channel}"));
        s.Rows.Add(new("Кол-во дверей тр-ра\nЦвет корпуса / толщина ЛКП",
            $"Двухстворчатые распашные двери\n{c.BodyColor} / ____ мкм"));
        s.Rows.Add(new("Цвет дверей / толщина ЛКП", $"{c.DoorColor} / ____ мкм"));
        s.Rows.Add(new("Разъединитель РУВН", $"{c.RuvnSwitch} {c.RuvnSwitchNominal}А"));
        s.Rows.Add(new("Аппаратура РУНН (Ввод)", InputDeviceDescription(c)));
        s.Rows.Add(new("Ошиновка РУНН (Ввод)", res.BusbarLv));
        s.Rows.Add(new("Ошиновка РУВН", res.BusbarHv));
        s.Rows.Add(new("Исполнение ввода / вывода", $"{c.RuvnExecution} / {c.OutgoingExecution}"));
        doc.Sections.Add(s);
        if (!string.IsNullOrWhiteSpace(po.Signatures))
        {
            var sig = new DocSection { Name = "" };
            sig.Rows.Add(new("Подписи", po.Signatures.Replace("Подписи:  ", "")));
            doc.Sections.Add(sig);
        }
        return doc;
    }

    /// <summary>Приоритет описания ввода РУНН: ПВР → РЕ → АВ (как в Приказе B16).</summary>
    public static string InputDeviceDescription(ProjectConfig c)
    {
        if (c.PvrOn) return $"ПВР {c.PvrNominal}А ({c.PvrManufacturer})";
        if (c.ReOn) return $"Рубильник РЕ {c.ReNominal}А ({c.ReManufacturer})";
        if (c.AvInOn) return $"Вводной АВ {c.AvInNominal}А ({c.AvInManufacturer})";
        return "Не установлен";
    }

    public static GeneratedDocument BuildChecklist(ProjectConfig c, CalculationResult res, CatalogStore store, DocTemplates tpl)
    {
        var qc = tpl.QcChecklist;
        var doc = new GeneratedDocument
        {
            Title = qc.Title,
            Subtitle = qc.Committee,
        };
        foreach (var sec in qc.Sections)
        {
            // Skip portal section if no RUVN or if RUVN execution is Cable
            if (sec.Name.Contains("ВЫСОКОВОЛЬТНЫЙ ПОРТАЛ", StringComparison.OrdinalIgnoreCase))
            {
                if (c.RuvnType == "Нет" || c.RuvnExecution.Equals("Кабельный", StringComparison.OrdinalIgnoreCase))
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
        return doc;
    }

    private static string Substitute(string text, ProjectConfig c, CalculationResult res) => text
        .Replace("{width}", I(res.WidthFinal))
        .Replace("{length}", I(res.LengthFinal))
        .Replace("{height}", I(res.HeightFinal))
        .Replace("{steelType}", c.SteelType)
        .Replace("{thickness}", Th(c.Thickness))
        .Replace("{bodyColor}", c.BodyColor)
        .Replace("{doorColor}", c.DoorColor);
}
