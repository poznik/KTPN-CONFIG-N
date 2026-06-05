using ClosedXML.Excel;
using KtpnConfigurator.Core.Documents;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.App.Services;

/// <summary>Экспорт пакета документов в книгу Excel (.xlsx) средствами ClosedXML.</summary>
public static class ExcelExporter
{
    public static void Export(string path, ProjectConfig cfg, CalculationResult res, IEnumerable<GeneratedDocument> docs)
    {
        using var wb = new XLWorkbook();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Сводка расчёта
        var sum = wb.AddWorksheet(UniqueSheetName("Сводка расчёта", usedNames));
        int r = 1;
        sum.Cell(r, 1).Value = "СВОДКА РАСЧЁТА КТПН";
        sum.Cell(r, 1).Style.Font.Bold = true;
        sum.Cell(r, 1).Style.Font.FontSize = 14;
        r += 2;
        void KV(string k, string v)
        {
            sum.Cell(r, 1).Value = k;
            sum.Cell(r, 2).Value = v;
            r++;
        }
        KV("Проект", cfg.ProjectName);
        KV("Трансформатор", cfg.Mark);
        KV("Итоговая длина, мм", res.LengthFinal.ToString("0"));
        KV("Итоговая ширина, мм", res.WidthFinal.ToString("0"));
        KV("Итоговая высота, мм", res.HeightFinal.ToString("0"));
        KV("Масса основания, кг", res.BaseMass.ToString("0"));
        KV("Масса корпуса, кг", res.BodyMass.ToString("0"));
        KV("Масса брутто, кг", res.GrossMass.ToString("0"));
        KV("Ток НН, А", res.RatedCurrentA.ToString("0"));
        KV("Номинал ввода, А", res.InputNominal.ToString("0"));
        KV("Проверка", res.ValidationOk ? "ПРОЙДЕНА" : "НЕ ПРОЙДЕНА");
        sum.Columns().AdjustToContents();

        foreach (var doc in docs)
            WriteDoc(wb, doc, usedNames);

        wb.SaveAs(path);
    }

    private static void WriteDoc(XLWorkbook wb, GeneratedDocument doc, HashSet<string> usedNames)
    {
        var name = UniqueSheetName(SafeSheetName(doc.Title), usedNames);
        var ws = wb.AddWorksheet(name);
        int r = 1;
        ws.Cell(r, 1).Value = doc.Title;
        ws.Cell(r, 1).Style.Font.Bold = true;
        ws.Cell(r, 1).Style.Font.FontSize = 13;
        r++;
        if (!string.IsNullOrWhiteSpace(doc.Subtitle))
        {
            ws.Cell(r, 1).Value = doc.Subtitle;
            r++;
        }
        r++;
        foreach (var sec in doc.Sections)
        {
            if (!string.IsNullOrWhiteSpace(sec.Name))
            {
                ws.Cell(r, 1).Value = sec.Name;
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Range(r, 1, r, 2).Merge();
                r++;
            }
            foreach (var row in sec.Rows)
            {
                if (row.IsChecklistItem)
                {
                    ws.Cell(r, 1).Value = row.Label;
                    ws.Cell(r, 2).Value = "[ Да / Нет ]";
                }
                else
                {
                    ws.Cell(r, 1).Value = row.Label;
                    ws.Cell(r, 1).Style.Alignment.WrapText = true;
                    ws.Cell(r, 2).Value = row.Value;
                    ws.Cell(r, 2).Style.Alignment.WrapText = true;
                }
                r++;
            }
            r++;
        }
        ws.Column(1).Width = 45;
        ws.Column(2).Width = 45;
    }

    private static string SafeSheetName(string title)
    {
        var name = title;
        foreach (var c in new[] { '\\', '/', '?', '*', '[', ']', ':' })
            name = name.Replace(c, ' ');
        name = name.Trim();
        if (name.Length > 28) name = name[..28].Trim();
        return string.IsNullOrWhiteSpace(name) ? "Документ" : name;
    }

    /// <summary>Гарантирует уникальность имени листа (≤ 31 символ, лимит Excel).</summary>
    private static string UniqueSheetName(string baseName, HashSet<string> used)
    {
        var name = baseName;
        int n = 2;
        while (!used.Add(name))
        {
            var suffix = $" ({n++})";
            var trimLen = Math.Min(baseName.Length, 31 - suffix.Length);
            name = baseName[..trimLen] + suffix;
        }
        return name;
    }
}
