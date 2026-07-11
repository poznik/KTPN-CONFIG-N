using KtpnConfigurator.App.Mvvm;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.App.ViewModels;

public sealed partial class MainViewModel
{
    private int _selectedMainTabIndex;
    private DateTimeOffset _lastRecoverySave = DateTimeOffset.MinValue;
    private bool _recoverySnapshotsEnabled;
    private IReadOnlyList<ElectricalSelectionChange> _autoSelectionChanges = Array.Empty<ElectricalSelectionChange>();
    private IReadOnlyList<ValidationIssueGroup> _validationGroups = Array.Empty<ValidationIssueGroup>();
    private EquipmentDatabaseAuditResult _equipmentDatabaseAudit = null!;

    public RelayCommand ApplyElectricalSelectionCommand { get; }
    public RelayCommand RestoreRecoveryCommand { get; }

    public int SelectedMainTabIndex
    {
        get => _selectedMainTabIndex;
        set
        {
            if (_selectedMainTabIndex == value)
                return;
            _selectedMainTabIndex = value;
            OnPropertyChanged();
        }
    }

    public string AutoSelectionSummary
    {
        get
        {
            if (!_cfg.AutoElectricalSelection)
                return "Автоподбор отключен: выбранные номиналы сохраняются без перезаписи.";

            var transformer = _env.Catalog.GetTransformer(_cfg.Mark);
            if (transformer is null)
                return "Сначала выберите трансформатор.";

            var inputs = ProjectConfigNormalizer.GetLvInputDevices(_cfg);
            var inputText = inputs.Count == 0
                ? "вводное устройство не выбрано"
                : string.Join(", ", inputs.Select(device => $"{device.DeviceType} {device.Nominal} А"));
            var ctReference = ElectricalSelectionService.CurrentTransformerReferenceNominal(_cfg);
            var ctText = _cfg.HasCt || _cfg.HasCtKip
                ? ElectricalSelectionService.RecommendedCtRatio(TtRatios, ctReference)
                : "не требуется";
            var unavailable = inputs
                .Where(device => ElectricalSelectionService.RecommendedInputNominal(
                    device.DeviceType, device.Manufacturer, transformer.RatedCurrentA, _env.Catalog) <= 0)
                .Select(device => $"для {device.DeviceType} {device.Manufacturer} нет номинала не ниже {transformer.RatedCurrentA:0} А")
                .ToList();
            var availabilityText = unavailable.Count == 0
                ? ""
                : $" Требуется другой аппарат: {string.Join("; ", unavailable)}.";
            var changed = _autoSelectionChanges.Count == 0
                ? ""
                : $" Изменено: {string.Join("; ", _autoSelectionChanges.Select(change => $"{change.Field} {change.PreviousValue} -> {change.NewValue}"))}.";

            return $"Основание: ТМГ {transformer.PowerKva:0} кВА, ток НН {transformer.RatedCurrentA:0} А; "
                + $"ввод: {inputText}; ТТ по номиналу ввода: {ctText}. "
                + $"Шины по таблице ТМГ и материалу: РУВН {_cfg.BusbarHvMaterial} {_res.BusbarHv}, "
                + $"РУНН {_cfg.BusbarLvMaterial} {_res.BusbarLv}, N {_cfg.BusbarNMaterial} {_res.BusbarN}.{changed}"
                + availabilityText;
        }
    }

    public string EquipmentDatabaseSummary => _equipmentDatabaseAudit.Summary;
    public string EquipmentDatabaseDetails =>
        $"Без источника: {_equipmentDatabaseAudit.MissingSource}; без даты проверки: {_equipmentDatabaseAudit.MissingVerificationDate}";

    public IReadOnlyList<ValidationIssueGroup> ValidationGroups => _validationGroups;
    public bool HasRecoveryProject => ProjectStorage.HasRecovery;

    public string ProjectTabStatusColor => TabStatus(0, IsProjectSectionComplete).Color;
    public string ProjectTabStatusText => TabStatus(0, IsProjectSectionComplete).Text;
    public string TransformerTabStatusColor => TabStatus(1, !string.IsNullOrWhiteSpace(_cfg.Mark)).Color;
    public string TransformerTabStatusText => TabStatus(1, !string.IsNullOrWhiteSpace(_cfg.Mark)).Text;
    public string RuvnTabStatusColor => TabStatus(2, !IsRuvnEnabled || !string.IsNullOrWhiteSpace(_cfg.RuvnExecution)).Color;
    public string RuvnTabStatusText => TabStatus(2, !IsRuvnEnabled || !string.IsNullOrWhiteSpace(_cfg.RuvnExecution)).Text;
    public string RunnTabStatusColor => TabStatus(3, InputNominalFromConfig() > 0).Color;
    public string RunnTabStatusText => TabStatus(3, InputNominalFromConfig() > 0).Text;
    public string AuxiliaryTabStatusColor => TabStatus(4, !_cfg.AuxiliaryNeeds.HasAuxiliaryCabinet || _cfg.AuxiliaryNeeds.MainBreakerNominal > 0).Color;
    public string AuxiliaryTabStatusText => TabStatus(4, !_cfg.AuxiliaryNeeds.HasAuxiliaryCabinet || _cfg.AuxiliaryNeeds.MainBreakerNominal > 0).Text;
    public string ProductTabStatusColor => TabStatus(5, !_res.HasErrors).Color;
    public string ProductTabStatusText => TabStatus(5, !_res.HasErrors).Text;
    public string ResultTabStatusColor => TabStatus(6, !_res.HasErrors).Color;
    public string ResultTabStatusText => TabStatus(6, !_res.HasErrors).Text;
    public string DocumentsTabStatusColor => DocumentsStatus.Color;
    public string DocumentsTabStatusText => DocumentsStatus.Text;

    private bool IsProjectSectionComplete =>
        !string.IsNullOrWhiteSpace(_cfg.ProjectName) && !string.IsNullOrWhiteSpace(_cfg.GridCompany);

    private (string Color, string Text) TabStatus(int tabIndex, bool complete)
    {
        var messages = ValidationMessages.Where(message => message.TabIndex == tabIndex).ToList();
        if (messages.Any(message => message.Severity == Severity.Error))
            return ("#E53935", "Есть ошибка");
        if (messages.Any(message => message.Severity == Severity.Warning))
            return ("#FB8C00", "Требуется проверка");
        return complete
            ? ("#00A152", "Заполнено")
            : ("#FB8C00", "Требуется проверка");
    }

    private (string Color, string Text) DocumentsStatus => !CanReleaseDocuments
        ? ("#E53935", "Есть ошибка")
        : WarningCount > 0
            ? ("#FB8C00", "Требуется проверка")
            : ("#00A152", "Заполнено");

    private void UpdateAutoSelectionState(IReadOnlyList<ElectricalSelectionChange> changes)
    {
        _autoSelectionChanges = changes;
        if (changes.Count == 0)
            return;

        OnPropertyChanged(nameof(PvrNominal));
        OnPropertyChanged(nameof(ReNominal));
        OnPropertyChanged(nameof(AvInNominal));
        OnPropertyChanged(nameof(CtRatio));
        OnPropertyChanged(nameof(CtKipRatio));
        foreach (var feeder in OutgoingFeeders)
            feeder.RefreshElectricalSelection();
    }

    private void ApplyElectricalSelection()
    {
        _cfg.AutoElectricalSelection = true;
        OnPropertyChanged(nameof(AutoElectricalSelection));
        Recalculate();
    }

    private void RefreshWorkflowState()
    {
        _validationGroups = ValidationMessages
            .OrderBy(message => message.TabIndex)
            .ThenByDescending(message => message.Severity)
            .GroupBy(message => new { message.Section, message.TabIndex })
            .Select(group => new ValidationIssueGroup(
                group.Key.Section,
                group.Key.TabIndex,
                group.Select(message => new ValidationIssueViewModel(message, () => SelectedMainTabIndex = message.TabIndex)).ToList()))
            .ToList();
    }

    private void SaveRecoverySnapshot()
    {
        if (!_recoverySnapshotsEnabled)
            return;

        var now = DateTimeOffset.UtcNow;
        if (now - _lastRecoverySave < TimeSpan.FromSeconds(15))
            return;

        try
        {
            ProjectStorage.SaveRecovery(_cfg);
            _lastRecoverySave = now;
        }
        catch
        {
            // Recovery is best-effort and must not interrupt engineering work.
        }
    }

    private void EnableRecoverySnapshots() => _recoverySnapshotsEnabled = true;

    private void RestoreRecovery()
    {
        if (!ProjectStorage.HasRecovery)
            return;

        ProjectConfig recovered;
        try
        {
            recovered = ProjectStorage.LoadRecovery();
        }
        catch (Exception ex)
        {
            Notify?.Invoke($"Не удалось восстановить автоматическую копию: {ex.Message}");
            return;
        }
        ApplyConfig(recovered, applyGridProfile: false, normalizeMeterCt: true);
        SetCurrentProjectPath(null);
        EnableProjectTracking(markDirty: true);
        Notify?.Invoke("Восстановлена последняя автоматическая копия проекта.");
    }

    internal bool CanDuplicateFeeder(OutgoingFeederViewModel feeder) =>
        feeder.DeviceType.Equals("АВ", StringComparison.OrdinalIgnoreCase) ? _cfg.AvQty < 20 : _cfg.RpsQty < 8;

    internal void DuplicateFeeder(OutgoingFeederViewModel source)
    {
        var type = source.DeviceType;
        var nextNumber = type.Equals("АВ", StringComparison.OrdinalIgnoreCase) ? ++_cfg.AvQty : ++_cfg.RpsQty;
        var clone = source.Config.Clone();
        clone.Number = nextNumber;
        _cfg.OutgoingFeeders.Add(clone);
        OnPropertyChanged(type.Equals("АВ", StringComparison.OrdinalIgnoreCase) ? nameof(AvQty) : nameof(RpsQty));
        SyncOutgoingFeeders();
    }

    internal void DeleteFeeder(OutgoingFeederViewModel source)
    {
        var type = source.DeviceType;
        _cfg.OutgoingFeeders.Remove(source.Config);
        var sameType = _cfg.OutgoingFeeders.Where(feeder => feeder.DeviceType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        for (var index = 0; index < sameType.Count; index++)
            sameType[index].Number = index + 1;

        if (type.Equals("АВ", StringComparison.OrdinalIgnoreCase))
        {
            _cfg.AvQty = sameType.Count;
            _cfg.AvOn = sameType.Count > 0;
            OnPropertyChanged(nameof(AvQty));
            OnPropertyChanged(nameof(AvOn));
        }
        else
        {
            _cfg.RpsQty = sameType.Count;
            _cfg.RpsOn = sameType.Count > 0;
            OnPropertyChanged(nameof(RpsQty));
            OnPropertyChanged(nameof(RpsOn));
        }

        SyncOutgoingFeeders();
    }
}

public sealed class ValidationIssueGroup
{
    public ValidationIssueGroup(string section, int tabIndex, IReadOnlyList<ValidationIssueViewModel> issues)
    {
        Section = section;
        TabIndex = tabIndex;
        Issues = issues;
    }

    public string Section { get; }
    public int TabIndex { get; }
    public IReadOnlyList<ValidationIssueViewModel> Issues { get; }
}

public sealed class ValidationIssueViewModel
{
    public ValidationIssueViewModel(ValidationMessage message, Action navigate)
    {
        Severity = message.Severity;
        Text = message.Text;
        NavigateCommand = new RelayCommand(navigate);
    }

    public Severity Severity { get; }
    public string Text { get; }
    public RelayCommand NavigateCommand { get; }
}
