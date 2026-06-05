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
        try
        {
            var env = AppEnvironment.Load();
            Console.WriteLine($"[selftest] Справочники: {env.Catalog.Transformers.Count} трансформаторов, "
                              + $"{env.Catalog.Apparatus.Count} аппаратов, {env.Catalog.Manufacturers().Count} производителей.");

            var vm = new MainViewModel(env);
            var cfg = vm.CurrentConfig;
            var res = CalculationEngine.Calculate(cfg, env.Catalog);
            Console.WriteLine($"[selftest] {cfg.Mark}: ДxШxВ = {res.LengthFinal:0}x{res.WidthFinal:0}x{res.HeightFinal:0} мм, "
                              + $"брутто {res.GrossMass:0} кг, ток НН {res.RatedCurrentA:0} А, "
                              + $"ввод {res.InputNominal:0} А, проверка {(res.ValidationOk ? "OK" : "FAIL")}.");

            var docs = new List<GeneratedDocument>
            {
                DocumentBuilder.BuildProductionOrder(cfg, res, env.Catalog, env.Templates),
                DocumentBuilder.BuildPassport(cfg, res, env.Catalog),
                DocumentBuilder.BuildChecklist(cfg, res, env.Catalog, env.Templates),
            };

            var tmp = Path.Combine(Path.GetTempPath(), "ktpn_selftest");
            Directory.CreateDirectory(tmp);
            var xlsx = Path.Combine(tmp, "selftest.xlsx");
            var pdf = Path.Combine(tmp, "selftest.pdf");
            ExcelExporter.Export(xlsx, cfg, res, docs);
            PdfExporter.Export(pdf, docs);

            var xlsxOk = new FileInfo(xlsx).Length > 0;
            var pdfOk = new FileInfo(pdf).Length > 0;
            Console.WriteLine($"[selftest] Excel: {xlsx} ({new FileInfo(xlsx).Length} б) -> {(xlsxOk ? "OK" : "EMPTY")}");
            Console.WriteLine($"[selftest] PDF:   {pdf} ({new FileInfo(pdf).Length} б) -> {(pdfOk ? "OK" : "EMPTY")}");

            if (!xlsxOk || !pdfOk)
            {
                Console.WriteLine("[selftest] ОШИБКА: пустой файл экспорта.");
                return 2;
            }

            Console.WriteLine("[selftest] УСПЕХ.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[selftest] ИСКЛЮЧЕНИЕ: " + ex);
            return 1;
        }
    }
}
