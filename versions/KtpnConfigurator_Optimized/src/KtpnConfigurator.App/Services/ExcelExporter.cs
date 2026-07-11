using KtpnConfigurator.Core.Catalogs;
using System.IO;
using ClosedXML.Excel;
using KtpnConfigurator.Core.Documents;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.App.Services;

/// <summary>Экспорт пакета документов в книгу Excel (.xlsx) средствами ClosedXML.</summary>
public static class ExcelExporter
{
    public static void Export(string path, ProjectConfig cfg, CalculationResult res, IEnumerable<GeneratedDocument> docs, CatalogStore catalog)
    {
        using var wb = new XLWorkbook();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Сводка расчёта
        var sum = wb.AddWorksheet(UniqueSheetName("Сводка расчёта", usedNames));
        int r = 1;
        var product = ProductRegistry.ResolveOrDefault(cfg.ProductTypeId);
        sum.Cell(r, 1).Value = $"СВОДКА РАСЧЁТА {product.DisplayName}";
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
        KV("Изделие", product.DisplayName);
        if (cfg.ProductTypeId is ProductTypeIds.SingleKtpn or ProductTypeIds.DoubleKtpn)
            KV("Трансформатор Т1", cfg.Mark);
        if (cfg.ProductTypeId == ProductTypeIds.DoubleKtpn)
        {
            KV("Трансформатор Т2", cfg.DoubleKtpn.SecondTransformerMark);
            KV("Токи Т1/Т2, А", $"{res.Section1RatedCurrentA:0} / {res.Section2RatedCurrentA:0}");
            KV("Вводы секций, А", $"{cfg.DoubleKtpn.Section1InputNominalA} / {cfg.DoubleKtpn.Section2InputNominalA}");
            KV("Ориентировочный КЗ секций, кА", $"{res.Section1ShortCircuitCurrentKa:0.#} / {res.Section2ShortCircuitCurrentKa:0.#}");
            KV("Шины РУНН секций", $"С1 {res.Section1BusbarLv}; С2 {res.Section2BusbarLv}");
            KV("Масса секций, кг", $"{res.Section1EstimatedMass:0} / {res.Section2EstimatedMass:0}");
        }
        if (cfg.ProductTypeId is ProductTypeIds.Nku or ProductTypeIds.Shcho or ProductTypeIds.Vru)
        {
            KV("Серия", cfg.LowVoltageAssembly.Series);
            KV("Шаблон компоновки", cfg.LowVoltageAssembly.LineupTemplate);
            KV("КЗ / Icw / Ipk, кА", $"{cfg.LowVoltageAssembly.DesignShortCircuitCurrentKa:0.#} / {cfg.LowVoltageAssembly.ShortTimeWithstandCurrentKa:0.#} / {cfg.LowVoltageAssembly.PeakWithstandCurrentKa:0.#}");
        }
        if (cfg.ProductTypeId is ProductTypeIds.Kso or ProductTypeIds.Kru)
        {
            KV("Серия", cfg.MediumVoltageSwitchgear.Series);
            KV("Шаблон компоновки", cfg.MediumVoltageSwitchgear.LineupTemplate);
            KV("Исполнение ячеек", cfg.MediumVoltageSwitchgear.CellExecution);
            KV("КЗ / стойкость / отключение, кА", $"{cfg.MediumVoltageSwitchgear.DesignShortCircuitCurrentKa:0.#} / {cfg.MediumVoltageSwitchgear.ShortTimeWithstandCurrentKa:0.#} / {cfg.MediumVoltageSwitchgear.BreakerBreakingCurrentKa:0.#}");
        }
        KV("Итоговая длина, мм", res.LengthFinal.ToString("0"));
        KV("Итоговая ширина, мм", res.WidthFinal.ToString("0"));
        KV("Итоговая высота, мм", res.HeightFinal.ToString("0"));
        KV("Масса основания, кг", res.BaseMass.ToString("0"));
        KV("Масса корпуса, кг", res.BodyMass.ToString("0"));
        KV("Масса брутто, кг", res.GrossMass.ToString("0"));
        KV("Расчетный ток, А", res.RatedCurrentA.ToString("0"));
        KV("Номинальный ток изделия/ввода, А", res.InputNominal.ToString("0"));
        KV("Проверка", res.ValidationOk ? "ПРОЙДЕНА" : "НЕ ПРОЙДЕНА");
        sum.Columns().AdjustToContents();

        foreach (var doc in docs)
            WriteDoc(wb, doc, usedNames);

        wb.SaveAs(path);
    }

    private static void WriteDiagramSheet(XLWorkbook wb, ProjectConfig cfg, CalculationResult res, HashSet<string> usedNames, CatalogStore catalog)
    {
        foreach (var sheet in SingleLineDiagramRenderer.RenderPngSheets(cfg, res, catalog))
        {
            var sheetName = sheet.Kind == KtpnConfigurator.Core.Diagrams.DiagramSheetKind.OneLine
                ? "Схема"
                : sheet.Name;
            var ws = wb.AddWorksheet(UniqueSheetName(SafeSheetName(sheetName), usedNames));
            using var stream = new MemoryStream(sheet.Png);
            var picture = ws.AddPicture(stream, SafeSheetName(sheet.Name));
            picture.MoveTo(ws.Cell(1, 1));
            picture.Scale(sheet.Kind == KtpnConfigurator.Core.Diagrams.DiagramSheetKind.OneLine ? 0.58 : 0.62);

            for (var col = 1; col <= 18; col++)
                ws.Column(col).Width = 12;
            for (var row = 1; row <= 58; row++)
                ws.Row(row).Height = 18;

            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.FitToPages(1, 1);
            ws.SheetView.ZoomScale = 70;
        }
    }

    private static void WriteDoc(XLWorkbook wb, GeneratedDocument doc, HashSet<string> usedNames)
    {
        if (doc.Kind == GeneratedDocumentKind.Checklist)
        {
            WriteChecklistDoc(wb, doc, usedNames);
            return;
        }

        var name = UniqueSheetName(SafeSheetName(doc.Title), usedNames);
        var ws = wb.AddWorksheet(name);
        var lastCol = doc.Kind == GeneratedDocumentKind.ProductionOrder ? 3 : 2;
        int r = 1;
        ws.Cell(r, 1).Value = doc.Title;
        ws.Cell(r, 1).Style.Font.Bold = true;
        ws.Cell(r, 1).Style.Font.FontSize = 13;
        ws.Range(r, 1, r, lastCol).Merge();
        ws.Range(r, 1, r, lastCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        r++;
        if (!string.IsNullOrWhiteSpace(doc.Subtitle))
        {
            ws.Cell(r, 1).Value = doc.Subtitle;
            ws.Range(r, 1, r, lastCol).Merge();
            ws.Range(r, 1, r, lastCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            r++;
        }
        r++;
        foreach (var sec in doc.Sections)
        {
            if (!string.IsNullOrWhiteSpace(sec.Name))
            {
                ws.Cell(r, 1).Value = sec.Name;
                var sectionRange = ws.Range(r, 1, r, lastCol);
                sectionRange.Merge();
                sectionRange.Style.Font.Bold = true;
                sectionRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E7EAE7");
                sectionRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
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
                    if (lastCol >= 3 && sec.IsSignatureTable)
                    {
                        ws.Cell(r, 3).Value = row.Note;
                        ws.Cell(r, 3).Style.Alignment.WrapText = true;
                    }
                    else if (lastCol >= 3)
                    {
                        ws.Range(r, 2, r, 3).Merge();
                    }
                }
                var rowRange = ws.Range(r, 1, r, lastCol);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                if (sec.IsSignatureTable)
                    ws.Row(r).Height = 24;
                r++;
            }
            r++;
        }
        ws.Column(1).Width = doc.Kind == GeneratedDocumentKind.ProductionOrder ? 28 : 45;
        ws.Column(2).Width = doc.Kind == GeneratedDocumentKind.ProductionOrder ? 58 : 45;
        if (lastCol >= 3)
            ws.Column(3).Width = 34;
        ws.Columns(1, lastCol).Style.Alignment.WrapText = true;
        ws.PageSetup.PageOrientation = doc.Kind == GeneratedDocumentKind.ProductionOrder
            ? XLPageOrientation.Landscape
            : XLPageOrientation.Portrait;
        ws.PageSetup.FitToPages(1, 0);
    }

    private static void WriteChecklistDoc(XLWorkbook wb, GeneratedDocument doc, HashSet<string> usedNames)
    {
        var name = UniqueSheetName(SafeSheetName(doc.Title), usedNames);
        var ws = wb.AddWorksheet(name);
        int r = 1;

        ws.Cell(r, 1).Value = doc.Title;
        ws.Range(r, 1, r, 4).Merge();
        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Font.FontSize = 14;
        ws.Range(r, 1, r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        r++;

        if (!string.IsNullOrWhiteSpace(doc.Subtitle))
        {
            ws.Cell(r, 1).Value = doc.Subtitle;
            ws.Range(r, 1, r, 4).Merge();
            ws.Range(r, 1, r, 4).Style.Alignment.WrapText = true;
            r++;
        }

        WriteChecklistHeader(ws, r++);
        var itemNo = 1;
        foreach (var sec in doc.Sections)
        {
            if (!string.IsNullOrWhiteSpace(sec.Name))
            {
                var sectionRange = ws.Range(r, 1, r, 4);
                sectionRange.Merge();
                sectionRange.FirstCell().Value = sec.Name;
                sectionRange.Style.Font.Bold = true;
                sectionRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                r++;
            }

            foreach (var row in sec.Rows)
            {
                if (row.IsChecklistItem)
                {
                    ws.Cell(r, 1).Value = itemNo++;
                    ws.Cell(r, 2).Value = row.Label;
                    ws.Cell(r, 3).Value = "";
                    ws.Cell(r, 4).Value = "";
                }
                else
                {
                    ws.Cell(r, 1).Value = row.Label;
                    var valueRange = ws.Range(r, 2, r, 4);
                    valueRange.Merge();
                    valueRange.FirstCell().Value = string.IsNullOrWhiteSpace(row.Note)
                        ? row.Value
                        : $"{row.Value}  {row.Note}";
                }

                var rowRange = ws.Range(r, 1, r, 4);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rowRange.Style.Alignment.WrapText = true;
                r++;
            }
        }

        ws.Column(1).Width = 7;
        ws.Column(2).Width = 72;
        ws.Column(3).Width = 8;
        ws.Column(4).Width = 8;
        ws.SheetView.FreezeRows(3);
        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        ws.PageSetup.FitToPages(1, 0);
    }

    private static void WriteChecklistHeader(IXLWorksheet ws, int row)
    {
        ws.Cell(row, 1).Value = "№";
        ws.Cell(row, 2).Value = "Пункт проверки";
        ws.Cell(row, 3).Value = "Да";
        ws.Cell(row, 4).Value = "Нет";
        var range = ws.Range(row, 1, row, 4);
        range.Style.Font.Bold = true;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
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
