using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Documents;
using KtpnConfigurator.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KtpnConfigurator.App.Services;

/// <summary>Экспорт пакета документов в один PDF средствами QuestPDF.</summary>
public static class PdfExporter
{
    public static void Export(string path, IEnumerable<GeneratedDocument> docs, ProjectConfig? cfg = null, CalculationResult? res = null, CatalogStore? catalog = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var productName = ProductRegistry.ResolveOrDefault(cfg?.ProductTypeId).DisplayName;

        Document.Create(container =>
        {
            foreach (var doc in docs)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(doc.Kind == GeneratedDocumentKind.Checklist ? 8 : 10).FontFamily("Segoe UI"));

                    page.Header().Column(h =>
                    {
                        h.Item().AlignCenter().Text(doc.Title).Bold().FontSize(14).FontColor(Colors.Black);
                        if (!string.IsNullOrWhiteSpace(doc.Subtitle))
                            h.Item().AlignCenter().Text(doc.Subtitle).FontSize(9).FontColor(Colors.Grey.Darken2);
                        h.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(8).Element(content =>
                    {
                        if (doc.Kind == GeneratedDocumentKind.Checklist)
                            RenderChecklist(content, doc);
                        else if (doc.Kind == GeneratedDocumentKind.ProductionOrder)
                            RenderProductionOrder(content, doc);
                        else
                            RenderStandard(content, doc);
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span($"Конфигуратор {productName} · ").FontSize(8).FontColor(Colors.Grey.Medium);
                        t.Span(doc.Title).FontSize(8).FontColor(Colors.Grey.Medium);
                        t.Span("  ·  стр. ").FontSize(8).FontColor(Colors.Grey.Medium);
                        t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }

        }).GeneratePdf(path);
    }

    private static void RenderDiagramPage(IDocumentContainer container, ProjectConfig cfg, CalculationResult res, CatalogStore catalog)
    {
        foreach (var sheet in SingleLineDiagramRenderer.RenderPngSheets(cfg, res, catalog))
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A3.Landscape());
                page.Margin(6);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));
                page.Header().AlignLeft().Text(sheet.Name).Bold().FontSize(10);
                page.Content().Image(sheet.Png).FitArea();
            });
        }
    }

    private static void RenderStandard(IContainer container, GeneratedDocument doc)
    {
        container.Column(col =>
        {
            col.Spacing(2);
            foreach (var sec in doc.Sections)
            {
                if (!string.IsNullOrWhiteSpace(sec.Name))
                    col.Item().PaddingTop(6).Text(sec.Name).Bold().FontSize(11);

                foreach (var row in sec.Rows)
                {
                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).Row(r =>
                    {
                        r.RelativeItem(2).Text(row.Label);
                        r.RelativeItem(2).AlignRight().Text(row.IsChecklistItem ? "[ Да / Нет ]" : row.Value);
                    });
                }
            }
        });
    }

    private static void RenderProductionOrder(IContainer container, GeneratedDocument doc)
    {
        container.Column(col =>
        {
            col.Spacing(7);
            foreach (var sec in doc.Sections)
            {
                col.Item().Table(table =>
                {
                    var signatureColumns = sec.IsSignatureTable;
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(2.4f);
                        if (signatureColumns)
                            columns.RelativeColumn(1.4f);
                    });

                    if (!string.IsNullOrWhiteSpace(sec.Name))
                        table.Cell().ColumnSpan((uint)(signatureColumns ? 3 : 2)).Element(SectionCell).Text(sec.Name).Bold();

                    foreach (var row in sec.Rows)
                    {
                        table.Cell().Element(BodyCell).Text(row.Label);
                        table.Cell().Element(BodyCell).Text(row.Value);
                        if (signatureColumns)
                            table.Cell().Element(BodyCell).Text(row.Note);
                    }
                });
            }
        });
    }

    private static void RenderChecklist(IContainer container, GeneratedDocument doc)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);
                columns.RelativeColumn(1);
                columns.ConstantColumn(34);
                columns.ConstantColumn(34);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("№");
                header.Cell().Element(HeaderCell).Text("Пункт проверки");
                header.Cell().Element(HeaderCell).Text("Да");
                header.Cell().Element(HeaderCell).Text("Нет");
            });

            var itemNo = 1;
            foreach (var sec in doc.Sections)
            {
                if (!string.IsNullOrWhiteSpace(sec.Name))
                    table.Cell().ColumnSpan(4).Element(ChecklistSectionCell).Text(sec.Name).Bold();

                foreach (var row in sec.Rows)
                {
                    if (row.IsChecklistItem)
                    {
                        table.Cell().Element(BodyCell).AlignCenter().Text(itemNo++.ToString());
                        table.Cell().Element(BodyCell).Text(row.Label);
                        table.Cell().Element(BodyCell).Text("");
                        table.Cell().Element(BodyCell).Text("");
                    }
                    else
                    {
                        table.Cell().Element(BodyCell).Text(row.Label);
                        table.Cell().ColumnSpan(3).Element(BodyCell).Text(row.Value);
                    }
                }
            }
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Border(1)
            .BorderColor(Colors.Grey.Darken2)
            .Padding(3)
            .AlignCenter()
            .AlignMiddle()
            .DefaultTextStyle(x => x.Bold());

    private static IContainer ChecklistSectionCell(IContainer container) =>
        container
            .Border(1)
            .BorderColor(Colors.Grey.Darken2)
            .Padding(3)
            .AlignMiddle();

    private static IContainer SectionCell(IContainer container) =>
        container
            .Border(1)
            .BorderColor(Colors.Grey.Darken2)
            .Background(Colors.Grey.Lighten4)
            .Padding(3)
            .AlignMiddle();

    private static IContainer BodyCell(IContainer container) =>
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(3)
            .AlignMiddle();
}
