using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace KtpnConfigurator.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
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
        MessageBox.Show(
            "Произошла непредвиденная ошибка:\n\n" + ex.Message + "\n\nПодробности записаны в:\n" + logPath,
            "Конфигуратор КТПН",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static string WriteErrorLog(Exception ex)
    {
        var path = Path.Combine(Path.GetTempPath(), "KtpnConfigurator_last_error.txt");
        File.WriteAllText(path, ex.ToString());
        return path;
    }
}
