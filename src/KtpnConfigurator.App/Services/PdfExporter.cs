using KtpnConfigurator.Core.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KtpnConfigurator.App.Services;

/// <summary>Экспорт пакета документов в один PDF средствами QuestPDF.</summary>
public static class PdfExporter
{
    public static void Export(string path, IEnumerable<GeneratedDocument> docs)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            foreach (var doc in docs)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                    page.Header().Column(h =>
                    {
                        h.Item().Text(doc.Title).Bold().FontSize(14).FontColor(Colors.Blue.Darken3);
                        if (!string.IsNullOrWhiteSpace(doc.Subtitle))
                            h.Item().Text(doc.Subtitle).Italic().FontSize(9).FontColor(Colors.Grey.Darken1);
                        h.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Spacing(2);
                        foreach (var sec in doc.Sections)
                        {
                            if (!string.IsNullOrWhiteSpace(sec.Name))
                                col.Item().PaddingTop(6).Text(sec.Name).Bold().FontSize(11).FontColor(Colors.Blue.Darken2);

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

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Конфигуратор КТПН · ").FontSize(8).FontColor(Colors.Grey.Medium);
                        t.Span(doc.Title).FontSize(8).FontColor(Colors.Grey.Medium);
                        t.Span("  ·  стр. ").FontSize(8).FontColor(Colors.Grey.Medium);
                        t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }
        }).GeneratePdf(path);
    }
}
