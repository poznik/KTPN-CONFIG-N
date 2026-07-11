using System.IO;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.Core.Documents;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.App.ViewModels;

public sealed partial class MainViewModel
{
    private string? _currentProjectPath;
    private bool _isDirty;
    private bool _trackProjectChanges;
    private bool _isExporting;

    public string? CurrentProjectPath => _currentProjectPath;
    public bool IsDirty => _isDirty;
    public bool HasUnsavedChanges => _isDirty;
    public string UnsavedChangesText => _isDirty ? "Есть несохранённые изменения" : "Все изменения сохранены";
    public string CurrentProjectFileText => string.IsNullOrWhiteSpace(_currentProjectPath)
        ? "Файл ещё не выбран"
        : Path.GetFileName(_currentProjectPath);
    public string WindowTitle => $"{ApplicationTitle} - {(_currentProjectPath is null ? _cfg.ProjectName : Path.GetFileNameWithoutExtension(_currentProjectPath))}{(_isDirty ? " *" : "")}";
    public bool IsExporting => _isExporting;
    public string ExportProgressText => _isExporting ? "Формируются документы..." : "";

    private bool CanSaveProject() => _isDirty || string.IsNullOrWhiteSpace(_currentProjectPath);

    private void SaveProjectTo(string path)
    {
        try
        {
            ProjectStorage.Save(_cfg, path);
        }
        catch (Exception ex)
        {
            Notify?.Invoke($"Не удалось сохранить проект «{path}»: {ex.Message}");
            return;
        }
        SetCurrentProjectPath(path);
        SetDirty(false);
        try
        {
            ProjectStorage.ClearRecovery();
        }
        catch
        {
            // Сохранение уже прошло; проблема с recovery-файлом не должна его маскировать.
        }
        OnPropertyChanged(nameof(ProjectVersionText));
        OnPropertyChanged(nameof(ProjectVersionNumberText));
        OnPropertyChanged(nameof(HasRecoveryProject));
        Notify?.Invoke($"Проект сохранён: {path}");
    }

    private void SetCurrentProjectPath(string? path)
    {
        _currentProjectPath = string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
        OnPropertyChanged(nameof(CurrentProjectPath));
        OnPropertyChanged(nameof(CurrentProjectFileText));
        OnPropertyChanged(nameof(WindowTitle));
        SaveProjectCommand.RaiseCanExecuteChanged();
    }

    private void SetDirty(bool value)
    {
        if (_isDirty == value)
            return;
        _isDirty = value;
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(UnsavedChangesText));
        OnPropertyChanged(nameof(WindowTitle));
        SaveProjectCommand.RaiseCanExecuteChanged();
    }

    private void MarkProjectChanged()
    {
        if (_trackProjectChanges)
            SetDirty(true);
    }

    private void SuspendProjectTracking() => _trackProjectChanges = false;

    private void EnableProjectTracking(bool markDirty)
    {
        _trackProjectChanges = true;
        SetDirty(markDirty);
    }

    private async Task ExportInBackground(
        string path,
        string format,
        Action<ProjectConfig, CalculationResult, IReadOnlyList<GeneratedDocument>> export)
    {
        var snapshot = _cfg.Clone();
        var result = CalculationEngine.Calculate(snapshot, _env.Catalog);
        ValidationMessageClassifier.Apply(result.Messages);
        var documents = DocumentPackageBuilder.BuildAll(snapshot, result, _env.Catalog, _env.Templates);

        SetExporting(true);
        try
        {
            await Task.Run(() =>
            {
                export(snapshot, result, documents);
                ProjectStorage.SaveArchiveSnapshot(snapshot, path);
            });
            Notify?.Invoke($"Документы выгружены в {format}: {path}");
        }
        catch (Exception ex)
        {
            Notify?.Invoke($"Не удалось выгрузить документы в {format}: {ex.Message}");
        }
        finally
        {
            SetExporting(false);
        }
    }

    private void SetExporting(bool value)
    {
        if (_isExporting == value)
            return;
        _isExporting = value;
        OnPropertyChanged(nameof(IsExporting));
        OnPropertyChanged(nameof(ExportProgressText));
        ExportExcelCommand.RaiseCanExecuteChanged();
        ExportPdfCommand.RaiseCanExecuteChanged();
    }
}
