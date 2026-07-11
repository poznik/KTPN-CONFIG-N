using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace KtpnConfigurator.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Фоновые потоки не покрываются DispatcherUnhandledException:
        // без этих подписок исключение в них убивает процесс без следа в логе.
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                WriteErrorLog(ex);
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            WriteErrorLog(args.Exception);
            args.SetObserved();
        };

        // Безголовая самопроверка для CI/диагностики.
        if (e.Args.Any(a => string.Equals(a, "--selftest", StringComparison.OrdinalIgnoreCase)))
        {
            var code = SelfTest.Run();
            Shutdown(code);
            return;
        }

        DispatcherUnhandledException += OnUnhandledException;

        if (e.Args.Any(a => string.Equals(a, "--smoketest-ui", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                _ = new MainWindow();
                Shutdown(0);
            }
            catch (Exception ex)
            {
                WriteErrorLog(ex);
                Shutdown(1);
            }
            return;
        }

        base.OnStartup(e);

        try
        {
            var window = new MainWindow();
            window.Show();
        }
        catch (Exception ex)
        {
            ShowFatalError(ex);
            Shutdown(1);
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowFatalError(e.Exception);
        e.Handled = true;
    }

    private static void ShowFatalError(Exception ex)
    {
        var logPath = WriteErrorLog(ex);
        var details = logPath is null
            ? "Записать файл с подробностями не удалось."
            : "Подробности записаны в:\n" + logPath;
        MessageBox.Show(
            "Произошла непредвиденная ошибка:\n\n" + ex.Message + "\n\n" + details,
            "Конфигуратор КТПН Optimized",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static string? WriteErrorLog(Exception ex)
    {
        // Сбой записи лога не должен порождать вторичное исключение внутри обработчика ошибок.
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "KtpnConfigurator_Optimized_last_error.txt");
            File.WriteAllText(path, ex.ToString());
            return path;
        }
        catch
        {
            return null;
        }
    }
}
