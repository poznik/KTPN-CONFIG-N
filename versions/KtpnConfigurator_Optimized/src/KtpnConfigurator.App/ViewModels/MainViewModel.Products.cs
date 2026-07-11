using System.Collections.ObjectModel;
using KtpnConfigurator.App.Mvvm;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.App.ViewModels;

public sealed partial class MainViewModel
{
    public IReadOnlyList<ProductDefinition> ProductOptions => ProductRegistry.All;
    public IReadOnlyList<string> AllTransformerMarks { get; private set; } = Array.Empty<string>();
    public ObservableCollection<string> SecondTransformerMarks { get; private set; } = new();
    public IReadOnlyList<int> ProductCurrentOptions { get; } = new[] { 100, 160, 250, 400, 630, 800, 1000, 1250, 1600, 2000, 2500, 3200, 4000, 5000, 6300 };
    public IReadOnlyList<string> EarthingSystems { get; } = new[] { "TN-C", "TN-S", "TN-C-S", "TT", "IT" };
    public IReadOnlyList<string> SeparationForms { get; } = new[] { "1", "2a", "2b", "3a", "3b", "4a", "4b" };
    public IReadOnlyList<string> ServiceAccessOptions { get; } = new[] { "Одностороннее", "Двухстороннее" };
    public IReadOnlyList<string> CouplerPositions { get; } = new[] { "Отключен", "Включен" };
    public IReadOnlyList<int> DoubleKtpnSectionOptions { get; } = new[] { 1, 2 };
    public IReadOnlyList<string> LvPanelTypes { get; } = new[] { "Вводная", "Секционная", "Линейная", "Распределительная", "Учетная", "АВР", "УКРМ", "Управление", "ЩСН", "Резерв" };
    public IReadOnlyList<string> LvMainDevices { get; } = new[] { "АВ", "РПС", "ПВР/NH", "Рубильник", "Секционный АВ", "АВР", "Счетчик, ТТ", "ОПН", "УКРМ", "АВ, ОПС", "Нет" };
    public IReadOnlyList<string> LvDeviceManufacturers { get; } = new[] { "", "IEK", "КЭАЗ", "CHINT", "Контактор", "EKF", "DEKraft", "ABB", "Schneider Electric", "Siemens", "По проекту" };
    public IReadOnlyList<string> LvDeviceModels { get; } = new[] { "", "ВА88", "ВА57", "Compact NSX", "EasyPact CVS", "SACE Tmax", "NXM", "OptiMat", "ПВР", "РПС", "NH00", "NH1", "NH2", "NH3", "По проекту" };
    public IReadOnlyList<string> MvCellPurposes { get; } = new[] { "Ввод", "Отходящая линия", "Трансформатор", "Секционный выключатель", "Секционный разъединитель", "ТН", "ТСН", "Связь шин" };
    public IReadOnlyList<string> MvMainDevices { get; } = new[] { "ВНА/ВНР/РВЗ", "Вакуумный выключатель", "Разъединитель", "Выключатель нагрузки", "ТН", "Заземлитель", "Нет" };
    public IReadOnlyList<string> MvDeviceModels { get; } = new[] { "", "ВВ/TEL-10", "ВБЭ-10", "ВБП-10", "ВРС-10", "BB/TEL", "ВНА", "ВНР", "РВЗ", "По серии КСО", "По серии КРУ" };
    public IReadOnlyList<string> MvCtRatios { get; } = new[] { "", "50/5", "75/5", "100/5", "150/5", "200/5", "300/5", "400/5", "600/5", "800/5", "1000/5", "1600/5", "2000/5" };
    public IReadOnlyList<string> MvCtAccuracyClasses { get; } = new[] { "", "0,5", "0,5S", "10P", "10P/0,5", "10P/0,5S", "5P/0,5" };
    public IReadOnlyList<string> MvVoltageTransformerModels { get; } = new[] { "", "НАЛИ", "НАМИ", "ЗНОЛ", "ЗНОЛП", "По серии КСО", "По серии КРУ" };
    public IReadOnlyList<string> MvRelayProtectionOptions { get; } = new[] { "", "МТЗ, ТО, ОЗЗ", "МТЗ, ТО, ОЗЗ, УРОВ", "МТЗ, АВР", "Защита ТН", "По проекту" };
    public IReadOnlyList<string> MvRelayTerminalOptions { get; } = new[] { "", "Сириус-2-Л", "Сириус-2-МЛ", "БМРЗ", "БМРЗ/Сириус", "Sepam", "REF615", "MiCOM", "По проекту" };
    public IReadOnlyList<string> MvVisibleBreakOptions { get; } = new[] { "", "РВЗ", "Выдвижной элемент", "Шторочный разрыв", "Разъединитель", "По конструкции ячейки" };
    public IReadOnlyList<int> ProductSectionOptions { get; } = new[] { 1, 2, 3, 4 };
    public IReadOnlyList<string> LvTemplateOptions => ProductConfigurationDefaults.LowVoltageTemplateNames(_cfg.ProductTypeId);
    public IReadOnlyList<string> MvTemplateOptions => ProductConfigurationDefaults.MediumVoltageTemplateNames(_cfg.ProductTypeId);

    public ObservableCollection<LowVoltagePanelViewModel> LvPanels { get; private set; } = new();
    public ObservableCollection<MediumVoltageCellViewModel> MvCells { get; private set; } = new();
    public RelayCommand AddLvPanelCommand { get; private set; } = null!;
    public RelayCommand AddMvCellCommand { get; private set; } = null!;
    public RelayCommand ResetLvPanelsCommand { get; private set; } = null!;
    public RelayCommand ResetMvCellsCommand { get; private set; } = null!;

    private void InitializeProductArchitecture()
    {
        AllTransformerMarks = Manufacturers
            .SelectMany(manufacturer => _env.Catalog.MarksFor(manufacturer))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(mark => mark)
            .ToList();
        AddLvPanelCommand = new RelayCommand(AddLvPanel);
        AddMvCellCommand = new RelayCommand(AddMvCell);
        ResetLvPanelsCommand = new RelayCommand(ResetLvPanels);
        ResetMvCellsCommand = new RelayCommand(ResetMvCells);
        ProductConfigurationDefaults.Normalize(_cfg);
        RefreshProductCollections();
    }

    private void RefreshProductCollections()
    {
        // Только починка null: полный Normalize восстанавливал бы типовую линейку
        // после того, как пользователь намеренно удалил все панели/ячейки.
        ProductConfigurationDefaults.EnsureNestedConfigs(_cfg);
        RefreshSecondTransformerMarks();
        LvPanels = new ObservableCollection<LowVoltagePanelViewModel>(
            _cfg.LowVoltageAssembly.Panels.Select(panel => new LowVoltagePanelViewModel(this, panel)));
        MvCells = new ObservableCollection<MediumVoltageCellViewModel>(
            _cfg.MediumVoltageSwitchgear.Cells.Select(cell => new MediumVoltageCellViewModel(this, cell)));
        OnPropertyChanged(nameof(LvPanels));
        OnPropertyChanged(nameof(MvCells));
        OnPropertyChanged(nameof(LvTemplateOptions));
        OnPropertyChanged(nameof(MvTemplateOptions));
    }

    private bool _switchingProduct;

    public ProductDefinition SelectedProduct
    {
        get => ProductRegistry.ResolveOrDefault(_cfg.ProductTypeId);
        set
        {
            if (value is null || _switchingProduct || _cfg.ProductTypeId.Equals(value.Id, StringComparison.OrdinalIgnoreCase)) return;
            _switchingProduct = true;
            try
            {
                SwitchProduct(value);
            }
            finally
            {
                _switchingProduct = false;
            }
        }
    }

    private void SwitchProduct(ProductDefinition value)
    {
        var previousProductId = _cfg.ProductTypeId;
        _cfg.ProductTypeId = value.Id;
        _cfg.ProductDataVersion = value.CurrentDataVersion;
        _cfg.SwitchManualOverrides(previousProductId, value.Id);
        ProductConfigurationDefaults.Normalize(_cfg);
        if (ShowLowVoltageConfiguration)
            ProductConfigurationDefaults.ApplyLowVoltageTemplate(_cfg, ProductConfigurationDefaults.DefaultLowVoltageTemplate(_cfg.ProductTypeId));
        if (ShowMediumVoltageConfiguration)
            ProductConfigurationDefaults.ApplyMediumVoltageTemplate(_cfg, ProductConfigurationDefaults.DefaultMediumVoltageTemplate(_cfg.ProductTypeId));
        RefreshProductCollections();
        SyncOutgoingFeeders(recalculate: false);
        OnPropertyChanged(nameof(SelectedProduct));
        NotifyManualOverrideInputs();
        OnPropertyChanged(nameof(ApplicationTitle));
        OnPropertyChanged(nameof(ShowKtpnTabs));
        OnPropertyChanged(nameof(ShowProductConfigurationTab));
        OnPropertyChanged(nameof(ShowDoubleKtpnConfiguration));
        OnPropertyChanged(nameof(ShowLowVoltageConfiguration));
        OnPropertyChanged(nameof(ShowMediumVoltageConfiguration));
        OnPropertyChanged(nameof(ProductConfigurationHeader));
        OnPropertyChanged(nameof(ProductDimensionsTitle));
        OnPropertyChanged(nameof(ProductPassportTabTitle));
        OnPropertyChanged(nameof(ProductPrimaryResultLabel));
        OnPropertyChanged(nameof(ProductInputResultLabel));
        OnPropertyChanged(nameof(ProductLineupResultLabel));
        NotifyLowVoltageConfigurationProperties();
        NotifyMediumVoltageConfigurationProperties();
        Recalculate();
    }

    private void NotifyManualOverrideInputs()
    {
        OnPropertyChanged(nameof(ManualLengthText));
        OnPropertyChanged(nameof(ManualWidthText));
        OnPropertyChanged(nameof(ManualHeightText));
        OnPropertyChanged(nameof(ManualBaseMassText));
        OnPropertyChanged(nameof(ManualBodyMassText));
        OnPropertyChanged(nameof(ManualGrossMassText));
    }

    public string ApplicationTitle => $"Конфигуратор {SelectedProduct.DisplayName} Optimized";
    public string ProductConfigurationHeader => $"Конфигурация {SelectedProduct.DisplayName}";
    public string ProductDimensionsTitle => $"Габариты и масса {SelectedProduct.DisplayName}";
    public string ProductPassportTabTitle => $"Паспорт {SelectedProduct.DisplayName}";
    public string ProductPrimaryResultLabel => ShowKtpnTabs ? (ShowDoubleKtpnConfiguration ? "Трансформаторы:" : "Трансформатор:") : "Изделие:";
    public string ProductInputResultLabel => ShowDoubleKtpnConfiguration ? "Вводы и секции:" : ShowLowVoltageConfiguration ? "Параметры НКУ:" : ShowMediumVoltageConfiguration ? "Параметры РУ:" : "Ввод РУНН:";
    public string ProductLineupResultLabel => ShowLowVoltageConfiguration ? "Панели:" : ShowMediumVoltageConfiguration ? "Ячейки:" : "Отходящие:";
    public bool ShowKtpnTabs => _cfg.ProductTypeId is ProductTypeIds.SingleKtpn or ProductTypeIds.DoubleKtpn;
    public bool ShowProductConfigurationTab => !_cfg.ProductTypeId.Equals(ProductTypeIds.SingleKtpn, StringComparison.OrdinalIgnoreCase);
    public bool ShowDoubleKtpnConfiguration => _cfg.ProductTypeId == ProductTypeIds.DoubleKtpn;
    public bool ShowLowVoltageConfiguration => _cfg.ProductTypeId is ProductTypeIds.Nku or ProductTypeIds.Shcho or ProductTypeIds.Vru;
    public bool ShowMediumVoltageConfiguration => _cfg.ProductTypeId is ProductTypeIds.Kso or ProductTypeIds.Kru;

    public string SecondTransformerManufacturer
    {
        get => _cfg.DoubleKtpn.SecondTransformerManufacturer;
        set
        {
            if (_cfg.DoubleKtpn.SecondTransformerManufacturer == value) return;
            _cfg.DoubleKtpn.SecondTransformerManufacturer = value;
            RefreshSecondTransformerMarks();
            if (!SecondTransformerMarks.Contains(_cfg.DoubleKtpn.SecondTransformerMark))
                _cfg.DoubleKtpn.SecondTransformerMark = SecondTransformerMarks.FirstOrDefault() ?? "";
            UpdateSecondTransformerInputNominal();
            OnPropertyChanged();
            OnPropertyChanged(nameof(SecondTransformerMark));
            Recalculate();
        }
    }
    public string SecondTransformerMark
    {
        get => _cfg.DoubleKtpn.SecondTransformerMark;
        set
        {
            if (_cfg.DoubleKtpn.SecondTransformerMark == value) return;
            _cfg.DoubleKtpn.SecondTransformerMark = value;
            UpdateSecondTransformerInputNominal();
            OnPropertyChanged();
            OnPropertyChanged(nameof(Section2InputNominal));
            Recalculate();
        }
    }
    public int Section1InputNominal { get => _cfg.DoubleKtpn.Section1InputNominalA; set => SetProduct(_cfg.DoubleKtpn.Section1InputNominalA, value, v => _cfg.DoubleKtpn.Section1InputNominalA = v); }
    public int Section2InputNominal { get => _cfg.DoubleKtpn.Section2InputNominalA; set => SetProduct(_cfg.DoubleKtpn.Section2InputNominalA, value, v => _cfg.DoubleKtpn.Section2InputNominalA = v); }
    public int SectionCouplerNominal { get => _cfg.DoubleKtpn.SectionCouplerNominalA; set => SetProduct(_cfg.DoubleKtpn.SectionCouplerNominalA, value, v => _cfg.DoubleKtpn.SectionCouplerNominalA = v); }
    public bool ProductAutomaticTransfer
    {
        get => ShowDoubleKtpnConfiguration ? _cfg.DoubleKtpn.AutomaticTransferEnabled : _cfg.LowVoltageAssembly.AutomaticTransferEnabled;
        set
        {
            if (ShowDoubleKtpnConfiguration) _cfg.DoubleKtpn.AutomaticTransferEnabled = value;
            else _cfg.LowVoltageAssembly.AutomaticTransferEnabled = value;
            OnPropertyChanged(); Recalculate();
        }
    }
    public bool ParallelOperationAllowed { get => _cfg.DoubleKtpn.ParallelOperationAllowed; set => SetProduct(_cfg.DoubleKtpn.ParallelOperationAllowed, value, v => _cfg.DoubleKtpn.ParallelOperationAllowed = v); }
    public string NormalCouplerPosition { get => _cfg.DoubleKtpn.NormalCouplerPosition; set => SetProduct(_cfg.DoubleKtpn.NormalCouplerPosition, value, v => _cfg.DoubleKtpn.NormalCouplerPosition = v); }
    public string ReserveMode { get => _cfg.DoubleKtpn.ReserveMode; set => SetProduct(_cfg.DoubleKtpn.ReserveMode, value, v => _cfg.DoubleKtpn.ReserveMode = v); }

    public string LvSeries { get => _cfg.LowVoltageAssembly.Series; set => SetProduct(_cfg.LowVoltageAssembly.Series, value, v => _cfg.LowVoltageAssembly.Series = v); }
    public string LvTemplate
    {
        get => _cfg.LowVoltageAssembly.LineupTemplate;
        set
        {
            // WPF пишет null в SelectedItem, когда при смене изделия подменяется
            // ItemsSource; реакция на null зацикливает обновления до переполнения стека.
            if (string.IsNullOrEmpty(value)) return;
            var previous = _cfg.LowVoltageAssembly.LineupTemplate;
            if (previous == value) return;
            ProductConfigurationDefaults.ApplyLowVoltageTemplate(_cfg, value);
            if (_cfg.LowVoltageAssembly.LineupTemplate == previous)
            {
                OnPropertyChanged();
                return;
            }
            RefreshProductCollections();
            NotifyLowVoltageConfigurationProperties();
            Recalculate();
        }
    }
    public int LvRatedVoltage { get => _cfg.LowVoltageAssembly.RatedVoltageV; set => SetProduct(_cfg.LowVoltageAssembly.RatedVoltageV, value, v => _cfg.LowVoltageAssembly.RatedVoltageV = v); }
    public int LvFrequency { get => _cfg.LowVoltageAssembly.FrequencyHz; set => SetProduct(_cfg.LowVoltageAssembly.FrequencyHz, value, v => _cfg.LowVoltageAssembly.FrequencyHz = v); }
    public int LvRatedBusCurrent { get => _cfg.LowVoltageAssembly.RatedBusCurrentA; set => SetProduct(_cfg.LowVoltageAssembly.RatedBusCurrentA, value, v => _cfg.LowVoltageAssembly.RatedBusCurrentA = v); }
    public double LvDesignShortCircuitCurrent { get => _cfg.LowVoltageAssembly.DesignShortCircuitCurrentKa; set => SetProduct(_cfg.LowVoltageAssembly.DesignShortCircuitCurrentKa, value, v => _cfg.LowVoltageAssembly.DesignShortCircuitCurrentKa = v); }
    public double LvIcw { get => _cfg.LowVoltageAssembly.ShortTimeWithstandCurrentKa; set => SetProduct(_cfg.LowVoltageAssembly.ShortTimeWithstandCurrentKa, value, v => _cfg.LowVoltageAssembly.ShortTimeWithstandCurrentKa = v); }
    public double LvIpk { get => _cfg.LowVoltageAssembly.PeakWithstandCurrentKa; set => SetProduct(_cfg.LowVoltageAssembly.PeakWithstandCurrentKa, value, v => _cfg.LowVoltageAssembly.PeakWithstandCurrentKa = v); }
    public string LvEarthingSystem { get => _cfg.LowVoltageAssembly.EarthingSystem; set => SetProduct(_cfg.LowVoltageAssembly.EarthingSystem, value, v => _cfg.LowVoltageAssembly.EarthingSystem = v); }
    public string LvInternalSeparation { get => _cfg.LowVoltageAssembly.InternalSeparation; set => SetProduct(_cfg.LowVoltageAssembly.InternalSeparation, value, v => _cfg.LowVoltageAssembly.InternalSeparation = v); }
    public string LvServiceAccess { get => _cfg.LowVoltageAssembly.ServiceAccess; set => SetProduct(_cfg.LowVoltageAssembly.ServiceAccess, value, v => _cfg.LowVoltageAssembly.ServiceAccess = v); }
    public string LvBusbarMaterial { get => _cfg.LowVoltageAssembly.BusbarMaterial; set => SetProduct(_cfg.LowVoltageAssembly.BusbarMaterial, value, v => _cfg.LowVoltageAssembly.BusbarMaterial = v); }
    public string LvProtectionDegree { get => _cfg.LowVoltageAssembly.ProtectionDegree; set => SetProduct(_cfg.LowVoltageAssembly.ProtectionDegree, value, v => _cfg.LowVoltageAssembly.ProtectionDegree = v); }
    public int LvSectionCount { get => _cfg.LowVoltageAssembly.SectionCount; set => SetProduct(_cfg.LowVoltageAssembly.SectionCount, Math.Clamp(value, 1, 4), v => _cfg.LowVoltageAssembly.SectionCount = v); }
    public double LvHeight { get => _cfg.LowVoltageAssembly.HeightMm; set => SetProduct(_cfg.LowVoltageAssembly.HeightMm, value, v => _cfg.LowVoltageAssembly.HeightMm = v); }
    public double LvDepth { get => _cfg.LowVoltageAssembly.DepthMm; set => SetProduct(_cfg.LowVoltageAssembly.DepthMm, value, v => _cfg.LowVoltageAssembly.DepthMm = v); }

    public string MvSeries { get => _cfg.MediumVoltageSwitchgear.Series; set => SetProduct(_cfg.MediumVoltageSwitchgear.Series, value, v => _cfg.MediumVoltageSwitchgear.Series = v); }
    public string MvTemplate
    {
        get => _cfg.MediumVoltageSwitchgear.LineupTemplate;
        set
        {
            // См. LvTemplate: null от WPF при подмене ItemsSource игнорируем.
            if (string.IsNullOrEmpty(value)) return;
            var previous = _cfg.MediumVoltageSwitchgear.LineupTemplate;
            if (previous == value) return;
            ProductConfigurationDefaults.ApplyMediumVoltageTemplate(_cfg, value);
            if (_cfg.MediumVoltageSwitchgear.LineupTemplate == previous)
            {
                OnPropertyChanged();
                return;
            }
            RefreshProductCollections();
            NotifyMediumVoltageConfigurationProperties();
            Recalculate();
        }
    }
    public double MvRatedVoltage { get => _cfg.MediumVoltageSwitchgear.RatedVoltageKv; set => SetProduct(_cfg.MediumVoltageSwitchgear.RatedVoltageKv, value, v => _cfg.MediumVoltageSwitchgear.RatedVoltageKv = v); }
    public double MvHighestVoltage { get => _cfg.MediumVoltageSwitchgear.HighestOperatingVoltageKv; set => SetProduct(_cfg.MediumVoltageSwitchgear.HighestOperatingVoltageKv, value, v => _cfg.MediumVoltageSwitchgear.HighestOperatingVoltageKv = v); }
    public int MvRatedBusCurrent { get => _cfg.MediumVoltageSwitchgear.RatedBusCurrentA; set => SetProduct(_cfg.MediumVoltageSwitchgear.RatedBusCurrentA, value, v => _cfg.MediumVoltageSwitchgear.RatedBusCurrentA = v); }
    public double MvDesignShortCircuitCurrent { get => _cfg.MediumVoltageSwitchgear.DesignShortCircuitCurrentKa; set => SetProduct(_cfg.MediumVoltageSwitchgear.DesignShortCircuitCurrentKa, value, v => _cfg.MediumVoltageSwitchgear.DesignShortCircuitCurrentKa = v); }
    public double MvThermalCurrent { get => _cfg.MediumVoltageSwitchgear.ShortTimeWithstandCurrentKa; set => SetProduct(_cfg.MediumVoltageSwitchgear.ShortTimeWithstandCurrentKa, value, v => _cfg.MediumVoltageSwitchgear.ShortTimeWithstandCurrentKa = v); }
    public double MvThermalDuration { get => _cfg.MediumVoltageSwitchgear.ShortTimeDurationSeconds; set => SetProduct(_cfg.MediumVoltageSwitchgear.ShortTimeDurationSeconds, value, v => _cfg.MediumVoltageSwitchgear.ShortTimeDurationSeconds = v); }
    public double MvPeakCurrent { get => _cfg.MediumVoltageSwitchgear.PeakWithstandCurrentKa; set => SetProduct(_cfg.MediumVoltageSwitchgear.PeakWithstandCurrentKa, value, v => _cfg.MediumVoltageSwitchgear.PeakWithstandCurrentKa = v); }
    public double MvBreakingCurrent { get => _cfg.MediumVoltageSwitchgear.BreakerBreakingCurrentKa; set => SetProduct(_cfg.MediumVoltageSwitchgear.BreakerBreakingCurrentKa, value, v => _cfg.MediumVoltageSwitchgear.BreakerBreakingCurrentKa = v); }
    public string MvNeutralMode { get => _cfg.MediumVoltageSwitchgear.NeutralMode; set => SetProduct(_cfg.MediumVoltageSwitchgear.NeutralMode, value, v => _cfg.MediumVoltageSwitchgear.NeutralMode = v); }
    public string MvServiceAccess { get => _cfg.MediumVoltageSwitchgear.ServiceAccess; set => SetProduct(_cfg.MediumVoltageSwitchgear.ServiceAccess, value, v => _cfg.MediumVoltageSwitchgear.ServiceAccess = v); }
    public string MvOperationalPower { get => _cfg.MediumVoltageSwitchgear.OperationalPower; set => SetProduct(_cfg.MediumVoltageSwitchgear.OperationalPower, value, v => _cfg.MediumVoltageSwitchgear.OperationalPower = v); }
    public string MvIac { get => _cfg.MediumVoltageSwitchgear.IacClassification; set => SetProduct(_cfg.MediumVoltageSwitchgear.IacClassification, value, v => _cfg.MediumVoltageSwitchgear.IacClassification = v); }
    public string MvLsc { get => _cfg.MediumVoltageSwitchgear.ServiceContinuityCategory; set => SetProduct(_cfg.MediumVoltageSwitchgear.ServiceContinuityCategory, value, v => _cfg.MediumVoltageSwitchgear.ServiceContinuityCategory = v); }
    public string MvPartitionClass { get => _cfg.MediumVoltageSwitchgear.PartitionClass; set => SetProduct(_cfg.MediumVoltageSwitchgear.PartitionClass, value, v => _cfg.MediumVoltageSwitchgear.PartitionClass = v); }
    public string MvArcRelief { get => _cfg.MediumVoltageSwitchgear.ArcRelief; set => SetProduct(_cfg.MediumVoltageSwitchgear.ArcRelief, value, v => _cfg.MediumVoltageSwitchgear.ArcRelief = v); }
    public string MvCellExecution { get => _cfg.MediumVoltageSwitchgear.CellExecution; set => SetProduct(_cfg.MediumVoltageSwitchgear.CellExecution, value, v => _cfg.MediumVoltageSwitchgear.CellExecution = v); }
    public double MvHeight { get => _cfg.MediumVoltageSwitchgear.HeightMm; set => SetProduct(_cfg.MediumVoltageSwitchgear.HeightMm, value, v => _cfg.MediumVoltageSwitchgear.HeightMm = v); }
    public double MvDepth { get => _cfg.MediumVoltageSwitchgear.DepthMm; set => SetProduct(_cfg.MediumVoltageSwitchgear.DepthMm, value, v => _cfg.MediumVoltageSwitchgear.DepthMm = v); }

    private void AddLvPanel()
    {
        var panel = new LowVoltagePanelConfig { Number = _cfg.LowVoltageAssembly.Panels.Count + 1 };
        _cfg.LowVoltageAssembly.Panels.Add(panel);
        LvPanels.Add(new LowVoltagePanelViewModel(this, panel));
        ProductConfigurationChanged();
    }

    internal void DuplicateLvPanel(LowVoltagePanelViewModel viewModel)
    {
        var panel = viewModel.Model.Clone();
        panel.Number = _cfg.LowVoltageAssembly.Panels.Count + 1;
        _cfg.LowVoltageAssembly.Panels.Add(panel);
        LvPanels.Add(new LowVoltagePanelViewModel(this, panel));
        ProductConfigurationChanged();
    }

    private void ResetLvPanels()
    {
        ProductConfigurationDefaults.ApplyLowVoltageTemplate(_cfg, _cfg.LowVoltageAssembly.LineupTemplate);
        RefreshProductCollections();
        NotifyLowVoltageConfigurationProperties();
        ProductConfigurationChanged();
    }

    internal void DeleteLvPanel(LowVoltagePanelViewModel viewModel)
    {
        _cfg.LowVoltageAssembly.Panels.Remove(viewModel.Model);
        ProductConfigurationDefaults.Renumber(_cfg.LowVoltageAssembly.Panels);
        RefreshProductCollections();
        ProductConfigurationChanged();
    }

    private void AddMvCell()
    {
        var cell = new MediumVoltageCellConfig { Number = _cfg.MediumVoltageSwitchgear.Cells.Count + 1 };
        _cfg.MediumVoltageSwitchgear.Cells.Add(cell);
        MvCells.Add(new MediumVoltageCellViewModel(this, cell));
        ProductConfigurationChanged();
    }

    internal void DuplicateMvCell(MediumVoltageCellViewModel viewModel)
    {
        var cell = viewModel.Model.Clone();
        cell.Number = _cfg.MediumVoltageSwitchgear.Cells.Count + 1;
        _cfg.MediumVoltageSwitchgear.Cells.Add(cell);
        MvCells.Add(new MediumVoltageCellViewModel(this, cell));
        ProductConfigurationChanged();
    }

    private void ResetMvCells()
    {
        ProductConfigurationDefaults.ApplyMediumVoltageTemplate(_cfg, _cfg.MediumVoltageSwitchgear.LineupTemplate);
        RefreshProductCollections();
        NotifyMediumVoltageConfigurationProperties();
        ProductConfigurationChanged();
    }

    internal void DeleteMvCell(MediumVoltageCellViewModel viewModel)
    {
        _cfg.MediumVoltageSwitchgear.Cells.Remove(viewModel.Model);
        ProductConfigurationDefaults.Renumber(_cfg.MediumVoltageSwitchgear.Cells);
        RefreshProductCollections();
        ProductConfigurationChanged();
    }

    internal void ProductConfigurationChanged() => Recalculate();

    internal void ApplyLowVoltagePanelPreset(LowVoltagePanelViewModel viewModel)
    {
        ProductConfigurationDefaults.ApplyLowVoltagePanelPreset(
            viewModel.Model,
            _cfg.ProductTypeId,
            _cfg.LowVoltageAssembly.RatedBusCurrentA,
            _cfg.LowVoltageAssembly.ShortTimeWithstandCurrentKa);
        viewModel.Refresh();
        ProductConfigurationChanged();
    }

    internal void ApplyLowVoltageDevicePreset(LowVoltagePanelViewModel viewModel)
    {
        ProductConfigurationDefaults.ApplyLowVoltageDevicePreset(
            viewModel.Model,
            _cfg.ProductTypeId,
            _cfg.LowVoltageAssembly.RatedBusCurrentA,
            _cfg.LowVoltageAssembly.ShortTimeWithstandCurrentKa);
        viewModel.Refresh();
        ProductConfigurationChanged();
    }

    internal void ApplyMediumVoltageCellPreset(MediumVoltageCellViewModel viewModel)
    {
        ProductConfigurationDefaults.ApplyMediumVoltageCellPreset(
            viewModel.Model,
            _cfg.ProductTypeId,
            _cfg.MediumVoltageSwitchgear.RatedBusCurrentA,
            _cfg.MediumVoltageSwitchgear.BreakerBreakingCurrentKa);
        viewModel.Refresh();
        ProductConfigurationChanged();
    }

    internal void ApplyMediumVoltageDevicePreset(MediumVoltageCellViewModel viewModel)
    {
        ProductConfigurationDefaults.ApplyMediumVoltageDevicePreset(
            viewModel.Model,
            _cfg.ProductTypeId,
            _cfg.MediumVoltageSwitchgear.RatedBusCurrentA,
            _cfg.MediumVoltageSwitchgear.BreakerBreakingCurrentKa);
        viewModel.Refresh();
        ProductConfigurationChanged();
    }

    internal static string EquipmentConfidenceText(string value) =>
        value.Equals("verified", StringComparison.OrdinalIgnoreCase)
            ? "Проверено"
            : value.Equals("userInput", StringComparison.OrdinalIgnoreCase)
                ? "Задано проектом"
                : "Требуется проверка";

    internal static string EquipmentConfidenceColor(string value) =>
        value.Equals("verified", StringComparison.OrdinalIgnoreCase)
            ? "#00A152"
            : value.Equals("userInput", StringComparison.OrdinalIgnoreCase)
                ? "#1565C0"
                : "#FB8C00";

    private void NotifyLowVoltageConfigurationProperties()
    {
        OnPropertyChanged(nameof(LvSeries));
        OnPropertyChanged(nameof(LvTemplate));
        OnPropertyChanged(nameof(LvTemplateOptions));
        OnPropertyChanged(nameof(LvRatedVoltage));
        OnPropertyChanged(nameof(LvFrequency));
        OnPropertyChanged(nameof(LvRatedBusCurrent));
        OnPropertyChanged(nameof(LvDesignShortCircuitCurrent));
        OnPropertyChanged(nameof(LvIcw));
        OnPropertyChanged(nameof(LvIpk));
        OnPropertyChanged(nameof(LvEarthingSystem));
        OnPropertyChanged(nameof(LvInternalSeparation));
        OnPropertyChanged(nameof(LvServiceAccess));
        OnPropertyChanged(nameof(LvBusbarMaterial));
        OnPropertyChanged(nameof(LvProtectionDegree));
        OnPropertyChanged(nameof(LvSectionCount));
        OnPropertyChanged(nameof(ProductAutomaticTransfer));
        OnPropertyChanged(nameof(LvHeight));
        OnPropertyChanged(nameof(LvDepth));
    }

    private void NotifyMediumVoltageConfigurationProperties()
    {
        OnPropertyChanged(nameof(MvSeries));
        OnPropertyChanged(nameof(MvTemplate));
        OnPropertyChanged(nameof(MvTemplateOptions));
        OnPropertyChanged(nameof(MvRatedVoltage));
        OnPropertyChanged(nameof(MvHighestVoltage));
        OnPropertyChanged(nameof(MvRatedBusCurrent));
        OnPropertyChanged(nameof(MvDesignShortCircuitCurrent));
        OnPropertyChanged(nameof(MvThermalCurrent));
        OnPropertyChanged(nameof(MvThermalDuration));
        OnPropertyChanged(nameof(MvPeakCurrent));
        OnPropertyChanged(nameof(MvBreakingCurrent));
        OnPropertyChanged(nameof(MvNeutralMode));
        OnPropertyChanged(nameof(MvServiceAccess));
        OnPropertyChanged(nameof(MvOperationalPower));
        OnPropertyChanged(nameof(MvIac));
        OnPropertyChanged(nameof(MvLsc));
        OnPropertyChanged(nameof(MvPartitionClass));
        OnPropertyChanged(nameof(MvArcRelief));
        OnPropertyChanged(nameof(MvCellExecution));
        OnPropertyChanged(nameof(MvHeight));
        OnPropertyChanged(nameof(MvDepth));
    }

    internal int RecommendedProductCurrent(double current) =>
        ProductCurrentOptions.FirstOrDefault(value => value >= current) is var selected && selected > 0
            ? selected
            : ProductCurrentOptions[^1];

    private void RefreshSecondTransformerMarks()
    {
        SecondTransformerMarks = new ObservableCollection<string>(_env.Catalog.MarksFor(_cfg.DoubleKtpn.SecondTransformerManufacturer));
        OnPropertyChanged(nameof(SecondTransformerMarks));
    }

    private void UpdateSecondTransformerInputNominal()
    {
        var transformer = _env.Catalog.GetTransformer(_cfg.DoubleKtpn.SecondTransformerMark);
        if (transformer is not null)
            _cfg.DoubleKtpn.Section2InputNominalA = RecommendedProductCurrent(transformer.RatedCurrentA);
    }

    private void SetProduct<T>(T current, T value, Action<T> assign, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value)) return;
        assign(value);
        OnPropertyChanged(name);
        Recalculate();
    }
}

public sealed class LowVoltagePanelViewModel : ObservableObject
{
    private readonly MainViewModel _owner;
    internal LowVoltagePanelConfig Model { get; }
    public LowVoltagePanelViewModel(MainViewModel owner, LowVoltagePanelConfig model) { _owner = owner; Model = model; DuplicateCommand = new RelayCommand(() => owner.DuplicateLvPanel(this)); DeleteCommand = new RelayCommand(() => owner.DeleteLvPanel(this)); }
    public RelayCommand DuplicateCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public int Number => Model.Number;
    public int SectionNumber { get => Model.SectionNumber; set => Set(Model.SectionNumber, Math.Max(1, value), v => Model.SectionNumber = v); }
    public string PanelType
    {
        get => Model.PanelType;
        set
        {
            if (Model.PanelType == value) return;
            Model.PanelType = value;
            OnPropertyChanged();
            _owner.ApplyLowVoltagePanelPreset(this);
        }
    }
    public string Purpose { get => Model.Purpose; set => Set(Model.Purpose, value, v => Model.Purpose = v); }
    public string MainDevice
    {
        get => Model.MainDevice;
        set
        {
            if (Model.MainDevice == value) return;
            Model.MainDevice = value;
            OnPropertyChanged();
            _owner.ApplyLowVoltageDevicePreset(this);
        }
    }
    public string Manufacturer { get => Model.Manufacturer; set => Set(Model.Manufacturer, value, v => Model.Manufacturer = v); }
    public string ModelName { get => Model.Model; set => Set(Model.Model, value, v => Model.Model = v); }
    public int RatedCurrent { get => Model.RatedCurrentA; set => Set(Model.RatedCurrentA, value, v => Model.RatedCurrentA = v); }
    public double BreakingCapacity { get => Model.BreakingCapacityKa; set => Set(Model.BreakingCapacityKa, value, v => Model.BreakingCapacityKa = v); }
    public int CircuitCount { get => Model.CircuitCount; set => Set(Model.CircuitCount, Math.Max(0, value), v => Model.CircuitCount = v); }
    public bool HasMetering { get => Model.HasMetering; set => Set(Model.HasMetering, value, v => Model.HasMetering = v); }
    public bool HasSurgeProtection { get => Model.HasSurgeProtection; set => Set(Model.HasSurgeProtection, value, v => Model.HasSurgeProtection = v); }
    public double Width { get => Model.WidthMm; set => Set(Model.WidthMm, value, v => Model.WidthMm = v); }
    public double Mass { get => Model.EstimatedMassKg; set => Set(Model.EstimatedMassKg, value, v => Model.EstimatedMassKg = v); }
    public string EquipmentStatus => MainViewModel.EquipmentConfidenceText(Model.EquipmentSourceConfidence);
    public string EquipmentStatusColor => MainViewModel.EquipmentConfidenceColor(Model.EquipmentSourceConfidence);
    public string EquipmentNotes => Model.EquipmentSourceNotes;
    internal void Refresh()
    {
        foreach (var name in new[]
        {
            nameof(Number), nameof(SectionNumber), nameof(PanelType), nameof(Purpose), nameof(MainDevice),
            nameof(Manufacturer), nameof(ModelName), nameof(RatedCurrent), nameof(BreakingCapacity),
            nameof(CircuitCount), nameof(HasMetering), nameof(HasSurgeProtection), nameof(Width), nameof(Mass),
            nameof(EquipmentStatus), nameof(EquipmentStatusColor), nameof(EquipmentNotes),
        })
            OnPropertyChanged(name);
    }
    private void Set<T>(T current, T value, Action<T> assign, [System.Runtime.CompilerServices.CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(current, value)) return; assign(value); OnPropertyChanged(name); _owner.ProductConfigurationChanged(); }
}

public sealed class MediumVoltageCellViewModel : ObservableObject
{
    private readonly MainViewModel _owner;
    internal MediumVoltageCellConfig Model { get; }
    public MediumVoltageCellViewModel(MainViewModel owner, MediumVoltageCellConfig model) { _owner = owner; Model = model; DuplicateCommand = new RelayCommand(() => owner.DuplicateMvCell(this)); DeleteCommand = new RelayCommand(() => owner.DeleteMvCell(this)); }
    public RelayCommand DuplicateCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public int Number => Model.Number;
    public string Purpose
    {
        get => Model.Purpose;
        set
        {
            if (Model.Purpose == value) return;
            Model.Purpose = value;
            OnPropertyChanged();
            _owner.ApplyMediumVoltageCellPreset(this);
        }
    }
    public string MainDevice
    {
        get => Model.MainDevice;
        set
        {
            if (Model.MainDevice == value) return;
            Model.MainDevice = value;
            OnPropertyChanged();
            _owner.ApplyMediumVoltageDevicePreset(this);
        }
    }
    public string DeviceModel { get => Model.DeviceModel; set => Set(Model.DeviceModel, value, v => Model.DeviceModel = v); }
    public int RatedCurrent { get => Model.RatedCurrentA; set => Set(Model.RatedCurrentA, value, v => Model.RatedCurrentA = v); }
    public double BreakingCurrent { get => Model.BreakingCurrentKa; set => Set(Model.BreakingCurrentKa, value, v => Model.BreakingCurrentKa = v); }
    public string CtRatio { get => Model.CtRatio; set => Set(Model.CtRatio, value, v => Model.CtRatio = v); }
    public string CtAccuracyClass { get => Model.CtAccuracyClass; set => Set(Model.CtAccuracyClass, value, v => Model.CtAccuracyClass = v); }
    public bool HasVoltageTransformer { get => Model.HasVoltageTransformer; set => Set(Model.HasVoltageTransformer, value, v => Model.HasVoltageTransformer = v); }
    public string VoltageTransformerModel { get => Model.VoltageTransformerModel; set => Set(Model.VoltageTransformerModel, value, v => Model.VoltageTransformerModel = v); }
    public string RelayProtection { get => Model.RelayProtection; set => Set(Model.RelayProtection, value, v => Model.RelayProtection = v); }
    public string RelayTerminal { get => Model.RelayTerminal; set => Set(Model.RelayTerminal, value, v => Model.RelayTerminal = v); }
    public string VisibleBreaks { get => Model.VisibleBreaks; set => Set(Model.VisibleBreaks, value, v => Model.VisibleBreaks = v); }
    public string VisibleBreakBefore { get => Model.VisibleBreakBefore; set => Set(Model.VisibleBreakBefore, value, v => Model.VisibleBreakBefore = v); }
    public string VisibleBreakAfter { get => Model.VisibleBreakAfter; set => Set(Model.VisibleBreakAfter, value, v => Model.VisibleBreakAfter = v); }
    public bool HasEarthingSwitch { get => Model.HasEarthingSwitch; set => Set(Model.HasEarthingSwitch, value, v => Model.HasEarthingSwitch = v); }
    public double Width { get => Model.WidthMm; set => Set(Model.WidthMm, value, v => Model.WidthMm = v); }
    public double Mass { get => Model.EstimatedMassKg; set => Set(Model.EstimatedMassKg, value, v => Model.EstimatedMassKg = v); }
    public string EquipmentStatus => MainViewModel.EquipmentConfidenceText(Model.EquipmentSourceConfidence);
    public string EquipmentStatusColor => MainViewModel.EquipmentConfidenceColor(Model.EquipmentSourceConfidence);
    public string EquipmentNotes => Model.EquipmentSourceNotes;
    internal void Refresh()
    {
        foreach (var name in new[]
        {
            nameof(Number), nameof(Purpose), nameof(MainDevice), nameof(DeviceModel), nameof(RatedCurrent),
            nameof(BreakingCurrent), nameof(CtRatio), nameof(CtAccuracyClass), nameof(HasVoltageTransformer),
            nameof(VoltageTransformerModel), nameof(RelayProtection), nameof(RelayTerminal), nameof(VisibleBreaks),
            nameof(VisibleBreakBefore), nameof(VisibleBreakAfter), nameof(HasEarthingSwitch), nameof(Width), nameof(Mass),
            nameof(EquipmentStatus), nameof(EquipmentStatusColor), nameof(EquipmentNotes),
        })
            OnPropertyChanged(name);
    }
    private void Set<T>(T current, T value, Action<T> assign, [System.Runtime.CompilerServices.CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(current, value)) return; assign(value); OnPropertyChanged(name); _owner.ProductConfigurationChanged(); }
}
