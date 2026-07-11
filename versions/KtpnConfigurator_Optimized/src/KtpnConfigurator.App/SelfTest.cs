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

            var baseline = new MainViewModel(env).CurrentConfig;
            foreach (var product in KtpnConfigurator.Core.Models.ProductRegistry.All)
            {
                var cfg = baseline.Clone();
                cfg.ProductTypeId = product.Id;
                KtpnConfigurator.Core.Models.ProductConfigurationDefaults.Normalize(cfg);
                var res = CalculationEngine.Calculate(cfg, env.Catalog);
                var docs = DocumentPackageBuilder.BuildAll(cfg, res, env.Catalog, env.Templates);
                Write($"[selftest] {product.DisplayName}: ДxШxВ = {res.LengthFinal:0}x{res.WidthFinal:0}x{res.HeightFinal:0} мм, масса {res.GrossMass:0} кг.");

                var safeId = product.Id.Replace('.', '_');
                var xlsx = Path.Combine(tmp, $"selftest_{safeId}.xlsx");
                var pdf = Path.Combine(tmp, $"selftest_{safeId}.pdf");
                ExcelExporter.Export(xlsx, cfg, res, docs, env.Catalog);
                PdfExporter.Export(pdf, docs, cfg, res, env.Catalog);
                if (new FileInfo(xlsx).Length == 0 || new FileInfo(pdf).Length == 0)
                {
                    Write($"[selftest] ОШИБКА: пустой экспорт для {product.DisplayName}.");
                    return 2;
                }
            }
            Write($"[selftest] Log: {logPath}");

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
