using System.IO;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.App.ViewModels;
using KtpnConfigurator.Core.Documents;
using KtpnConfigurator.Core.Engine;

namespace KtpnConfigurator.App;

/// <summary>
/// Безголовая самопроверка (--selftest): загрузка справочников, расчёт,
/// сборка документов и экспорт в Excel/PDF без показа окна. Возвращает код 0 при успехе.
/// </summary>
public static class SelfTest
{
    public static int Run()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "ktpn_selftest");
        Directory.CreateDirectory(tmp);
        var logPath = Path.Combine(tmp, "selftest.log");
        using var log = new StreamWriter(logPath, append: false);

        void Write(string message)
        {
            Console.WriteLine(message);
            log.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            log.Flush();
        }

        try
        {
            var env = AppEnvironment.Load();
            Write($"[selftest] Справочники: {env.Catalog.Transformers.Count} трансформаторов, "
                              + $"{env.Catalog.Apparatus.Count} аппаратов, {env.Catalog.Manufacturers().Count} производителей.");

            var vm = new MainViewModel(env);
            var cfg = vm.CurrentConfig;
            var firstFeeder = cfg.OutgoingFeeders.FirstOrDefault();
            if (firstFeeder is not null)
            {
                firstFeeder.HasMeter = true;
                if (string.IsNullOrWhiteSpace(firstFeeder.TtRatio))
                    firstFeeder.TtRatio = vm.SuggestedCtRatio(firstFeeder.Nominal);
            }

            var res = CalculationEngine.Calculate(cfg, env.Catalog);
            Write($"[selftest] {cfg.Mark}: ДxШxВ = {res.LengthFinal:0}x{res.WidthFinal:0}x{res.HeightFinal:0} мм, "
                              + $"брутто {res.GrossMass:0} кг, ток НН {res.RatedCurrentA:0} А, "
                              + $"ввод {res.InputNominal:0} А, проверка {(res.ValidationOk ? "OK" : "FAIL")}.");

            var docs = new List<GeneratedDocument>
            {
                DocumentBuilder.BuildProductionOrder(cfg, res, env.Catalog, env.Templates),
                DocumentBuilder.BuildPassport(cfg, res, env.Catalog),
                DocumentBuilder.BuildChecklist(cfg, res, env.Catalog, env.Templates),
                DocumentBuilder.BuildSpecification(cfg, res, env.Catalog),
            };

            var xlsx = Path.Combine(tmp, "selftest.xlsx");
            var pdf = Path.Combine(tmp, "selftest.pdf");
            var png = Path.Combine(tmp, "selftest_diagram.png");
            var diagramSheets = SingleLineDiagramRenderer.RenderPngSheets(cfg, res, env.Catalog);
            File.WriteAllBytes(png, diagramSheets.FirstOrDefault()?.Png ?? SingleLineDiagramRenderer.RenderPng(cfg, res, env.Catalog));
            for (var i = 0; i < diagramSheets.Count; i++)
            {
                var sheet = diagramSheets[i];
                var sheetPng = Path.Combine(tmp, $"selftest_diagram_{i + 1}_{SafeFilePart(sheet.Name)}.png");
                File.WriteAllBytes(sheetPng, sheet.Png);
                Write($"[selftest] PNG листа схемы: {sheetPng} ({new FileInfo(sheetPng).Length} б).");
            }

            ExcelExporter.Export(xlsx, cfg, res, docs, env.Catalog);
            PdfExporter.Export(pdf, docs, cfg, res, env.Catalog);

            var pngOk = new FileInfo(png).Length > 0;
            var xlsxOk = new FileInfo(xlsx).Length > 0;
            var pdfOk = new FileInfo(pdf).Length > 0;
            Write($"[selftest] PNG:   {png} ({new FileInfo(png).Length} б) -> {(pngOk ? "OK" : "EMPTY")}");
            Write($"[selftest] Excel: {xlsx} ({new FileInfo(xlsx).Length} б) -> {(xlsxOk ? "OK" : "EMPTY")}");
            Write($"[selftest] PDF:   {pdf} ({new FileInfo(pdf).Length} б) -> {(pdfOk ? "OK" : "EMPTY")}");
            Write($"[selftest] Log:   {logPath}");

            if (!pngOk || !xlsxOk || !pdfOk)
            {
                Write("[selftest] ОШИБКА: пустой файл экспорта.");
                return 2;
            }

            Write("[selftest] УСПЕХ.");
            return 0;
        }
        catch (Exception ex)
        {
            Write("[selftest] ИСКЛЮЧЕНИЕ: " + ex);
            return 1;
        }
    }

    private static string SafeFilePart(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var safe = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "sheet" : safe;
    }
}
