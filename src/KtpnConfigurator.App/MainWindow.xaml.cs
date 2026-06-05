using System.IO;
using System.Windows;
using Microsoft.Win32;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.App.ViewModels;

namespace KtpnConfigurator.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        var env = AppEnvironment.Load();
        _vm = new MainViewModel(env)
        {
            AskSavePath = AskSavePath,
            AskOpenPath = AskOpenPath,
            AskExportPath = AskExportPath,
            Notify = msg => MessageBox.Show(this, msg, "Конфигуратор КТПН",
                MessageBoxButton.OK, MessageBoxImage.Information),
        };
        DataContext = _vm;
    }

    private string? AskSavePath()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить проект КТПН",
            Filter = "Проект КТПН (*.ktpn)|*.ktpn|JSON (*.json)|*.json",
            FileName = SanitizeFileName(_vm.ProjectName) + ".ktpn",
            DefaultExt = ".ktpn",
        };
        return dlg.ShowDialog(this) == true ? dlg.FileName : null;
    }

    private string? AskOpenPath()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Открыть проект КТПН",
            Filter = "Проект КТПН (*.ktpn;*.json)|*.ktpn;*.json|Все файлы (*.*)|*.*",
        };
        return dlg.ShowDialog(this) == true ? dlg.FileName : null;
    }

    private string? AskExportPath(string ext)
    {
        var (filter, defExt) = ext == "pdf"
            ? ("PDF (*.pdf)|*.pdf", ".pdf")
            : ("Книга Excel (*.xlsx)|*.xlsx", ".xlsx");
        var dlg = new SaveFileDialog
        {
            Title = "Экспорт документов",
            Filter = filter,
            DefaultExt = defExt,
            FileName = SanitizeFileName(_vm.ProjectName) + defExt,
        };
        return dlg.ShowDialog(this) == true ? dlg.FileName : null;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "КТПН" : name;
    }
}
