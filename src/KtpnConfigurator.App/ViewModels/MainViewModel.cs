using KtpnConfigurator.Core.Catalogs;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using KtpnConfigurator.App.Mvvm;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.Core.Documents;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly AppEnvironment _env;
    private ProjectConfig _cfg;
    private CalculationResult _res = new();
    private bool _suspendRecalc;

    public MainViewModel(AppEnvironment env)
    {
        _env = env;
        _cfg = CreateDefaultConfig();

        Manufacturers = new ObservableCollection<string>(env.Catalog.Manufacturers());
        Marks = new ObservableCollection<string>(env.Catalog.MarksFor(_cfg.Manufacturer));
        SteelTypes = env.Catalog.Options.SteelTypes;
        Thicknesses = env.Catalog.Options.SteelThicknesses;
        Channels = env.Catalog.Options.Channels.Select(c => c.Size).ToList();
        RalColors = env.Catalog.Options.RalColors;
        GridCompanies = env.Catalog.Options.GridCompanies;
        Voltages = env.Catalog.Options.Voltages;
        RuvnTypes = env.Catalog.Options.RuvnTypes;
        RuvnSwitches = env.Catalog.Options.RuvnSwitches
            .Concat(new[] { RuvnEngineering.VacuumBreaker })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        RuvnNominals = env.Catalog.Options.RuvnNominals;
        FuseTypes = env.Catalog.Options.FuseTypes;
        FuseNominals = env.Catalog.Options.FuseNominals;
        CableExecutions = env.Catalog.Options.CableExecutions;
        LvNominals = env.Catalog.Options.LvNominals;
        TtRatios = env.Catalog.Options.TtRatios;
        PvrManufacturers = env.Catalog.Options.PvrManufacturers;
        ReManufacturers = env.Catalog.Options.ReManufacturers;
        RpsManufacturers = env.Catalog.Options.RpsManufacturers;
        AvManufacturers = env.Catalog.Options.AvManufacturers;
        OutgoingFeeders = new ObservableCollection<OutgoingFeederViewModel>();

        NewProjectCommand = new RelayCommand(NewProject);
        SaveProjectCommand = new RelayCommand(SaveProject);
        OpenProjectCommand = new RelayCommand(OpenProject);
        ExportExcelCommand = new RelayCommand(ExportExcel, CanExportDocuments);
        ExportPdfCommand = new RelayCommand(ExportPdf, CanExportDocuments);
        SaveChecklistCommand = new RelayCommand(SaveChecklist);

        ValidationMessages = new ObservableCollection<ValidationMessage>();
        CustomerRequirementMessages = new ObservableCollection<ValidationMessage>();
        LoadTemplatesForGridCompany();
        EnsureRuvnDefaults();
        RefreshRuvnEquipmentViewModels();
        NormalizeInputMeterCtState();
        EnsureAuxiliaryDefaults();
        ApplyGridCompanyProfile(notify: false);
        SyncOutgoingFeeders(recalculate: false);
        Recalculate();
    }

    private ProjectConfig CreateDefaultConfig()
    {
        var o = _env.Catalog.Options;
        var firstManuf = _env.Catalog.Manufacturers().FirstOrDefault() ?? "";
        var firstMark = _env.Catalog.MarksFor(firstManuf).FirstOrDefault() ?? "";
        // Воспроизводим разумную стартовую конфигурацию из исходной книги.
        var cfg = new ProjectConfig
        {
            ProjectName = "Новый проект КТПН",
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            SteelType = o.SteelTypes.FirstOrDefault() ?? "",
            Thickness = 2.0,
            FloorMaterial = "Рифленый лист",
            FloorThickness = 3.0,
            DoorMaterial = "Оцинкованная",
            DoorThickness = 2.0,
            RemovablePanelMaterial = "Оцинкованная",
            RemovablePanelThickness = 2.0,
            Channel = "10П",
            BodyColor = o.RalColors.FirstOrDefault() ?? "",
            DoorColor = o.RalColors.Skip(1).FirstOrDefault() ?? o.RalColors.FirstOrDefault() ?? "",
            RoofColor = o.RalColors.FirstOrDefault() ?? "",
            BaseColor = o.RalColors.FirstOrDefault() ?? "",
            InternalPanelColor = o.RalColors.FirstOrDefault() ?? "",
            LogoColor = "По ТЗ",
            ServicePlatformColor = o.RalColors.Skip(1).FirstOrDefault() ?? o.RalColors.FirstOrDefault() ?? "",
            BusbarHvMaterial = "Алюминий",
            BusbarLvMaterial = "Алюминий",
            BusbarNMaterial = "Алюминий",
            ClimateExecution = "У1",
            ProtectionDegree = "IP54",
            DoorConfiguration = "Двухстворчатые распашные",
            RuvnDoorConfiguration = "Двухстворчатые распашные",
            RunnDoorConfiguration = "Двухстворчатые распашные",
            TransformerDoorConfiguration = "Распашные с двух сторон",
            LockType = "Ригельный замок с фиксацией в трех точках",
            HasRigelLock = true,
            NetworkLockType = "Россети",
            HasPadlockProvision = false,
            HasGrounding = true,
            GroundingType = "Контур заземления",
            VentilationType = "Естественная",
            HasRoofDeflector = true,
            HasNameplate = true,
            HasDoorCanopies = false,
            HasDoorSeals = true,
            HasTransformerMeshDoors = false,
            HasLouverAnimalProtection = true,
            HasAntiVandalHinges = false,
            HasDoorSealing = false,
            HasServicePlatform = false,
            HasLogo = true,
            LogoPlacement = "По ТЗ",
            HasWarningLabels = true,
            HasDispatcherNameplate = false,
            HasFeederLabels = true,
            GridCompany = o.GridCompanies.FirstOrDefault() ?? "",
            LenRuvn = 1300, LenRunn = 600, TransformerTolerance = 300, LengthBuffer = 10,
            Voltage = o.Voltages.FirstOrDefault() ?? "",
            RuvnType = "Тупиковая",
            RuvnSwitch = o.RuvnSwitches.FirstOrDefault() ?? "",
            RuvnSwitchNominal = 630,
            FuseType = o.FuseTypes.FirstOrDefault() ?? "",
            FuseNominal = "50А",
            RuvnExecution = o.CableExecutions.FirstOrDefault() ?? "",
            RuvnSurgeArrester = true,
            RuvnSurgeArresterLocation = RuvnEngineering.SurgeArresterAtTransformer,
            RuvnSurgeArresterDischargeCurrentKa = 5,
            RuvnSurgeArresterThroughput = "Класс пропускной способности 2",
            RuvnTransformerSwitch = o.RuvnSwitches.FirstOrDefault() ?? "",
            RuvnTransformerSwitchNominal = 630,
            RuvnTransformerFuseOn = true,
            RuvnTransformerFuseType = o.FuseTypes.FirstOrDefault() ?? "",
            RuvnTransformerFuseNominal = "50А",
            RuvnIncomingSwitch = o.RuvnSwitches.FirstOrDefault() ?? "",
            RuvnIncomingSwitchNominal = 630,
            RuvnOutgoingSwitch = o.RuvnSwitches.FirstOrDefault() ?? "",
            RuvnOutgoingSwitchNominal = 630,
            PvrOn = true, PvrNominal = 630, PvrManufacturer = "CHINT",
            ReOn = false, ReNominal = 630, ReManufacturer = "КЭАЗ",
            AvInOn = false, AvInNominal = 630, AvInManufacturer = "Контактор",
            RunnSurgeArrester = true, HasCt = true, CtRatio = "600/5",
            HasCtKip = false, CtKipRatio = "600/5", HasMeter = true,
            AvOn = true, AvQty = 2, AvBrand = "IEK",
            RpsOn = false, RpsQty = 0, RpsBrand = o.RpsManufacturers.FirstOrDefault() ?? "",
            OutgoingExecution = o.CableExecutions.FirstOrDefault() ?? "",
            AuxiliaryNeeds = CreateDefaultAuxiliaryNeeds(),
        };
        cfg.RuvnTransformerFuseNominal = RuvnEngineering.RecommendedFuseNominal(cfg, _env.Catalog);
        cfg.FuseNominal = cfg.RuvnTransformerFuseNominal;
        return cfg;
    }

    // ===== Combo sources =====
    public ObservableCollection<string> Manufacturers { get; }
    public ObservableCollection<string> Marks { get; }
    public IReadOnlyList<string> SteelTypes { get; }
    public IReadOnlyList<double> Thicknesses { get; }
    public IReadOnlyList<string> Channels { get; }
    public IReadOnlyList<string> RalColors { get; }
    public IReadOnlyList<string> BusbarMaterials { get; } = new[] { "Алюминий", "Медь" };
    public IReadOnlyList<string> FloorMaterials { get; } = new[] { "Рифленый лист", "Оцинкованная", "Холоднокатаная (х.к.)", "По ТЗ" };
    public IReadOnlyList<string> DoorMaterials { get; } = new[] { "Оцинкованная", "Холоднокатаная (х.к.)", "По ТЗ" };
    public IReadOnlyList<string> RemovablePanelMaterials { get; } = new[] { "Оцинкованная", "Холоднокатаная (х.к.)", "По ТЗ" };
    public IReadOnlyList<string> ClimateExecutions { get; } = new[] { "У1", "УХЛ1", "УХЛ3", "ХЛ1" };
    public IReadOnlyList<string> ProtectionDegrees { get; } = new[] { "IP20", "IP21", "IP23", "IP30", "IP31", "IP32", "IP34", "IP43", "IP44", "IP54", "IP55", "IP65", "По ТЗ" };
    public IReadOnlyList<string> DoorConfigurations { get; } = new[]
    {
        "Двухстворчатые распашные",
        "Одностворчатые распашные",
        "Двухстворчатые с сетчатым барьером",
        "По ТЗ",
    };
    public IReadOnlyList<string> CompartmentDoorConfigurations { get; } = new[]
    {
        "Двухстворчатые распашные",
        "Одностворчатые распашные",
        "Распашные с сетчатым барьером",
        "Съемная панель",
        "По ТЗ",
    };
    public IReadOnlyList<string> TransformerDoorConfigurations { get; } = new[]
    {
        "Распашные с двух сторон",
        "Распашные с одной стороны",
        "Распашные с одной стороны, панель с другой",
        "Одна распашная дверь",
        "По ТЗ",
    };
    public IReadOnlyList<string> NetworkLockTypes { get; } = new[] { "Россети", "ЕЭСК", "Универсальный", "Нет", "По ТЗ" };
    public IReadOnlyList<string> GroundingTypes { get; } = new[]
    {
        "Контур заземления",
        "Внутренняя шина PE",
        "По ТЗ",
    };
    public IReadOnlyList<string> VentilationTypes { get; } = new[]
    {
        "Естественная",
        "Принудительная вентиляция",
        "По ТЗ",
    };
    public IReadOnlyList<string> LogoPlacements { get; } = new[]
    {
        "По ТЗ",
        "Фирменный блок по макету",
        "По инструкции маркировки ЕЭСК",
        "По правилам цветового оформления РЖД",
        "На наружных сторонах дверей РУ-0,4 кВ и трансформаторного отсека",
        "Не задано в извлеченном ТЗ",
    };
    public IReadOnlyList<string> GridCompanies { get; }
    public IReadOnlyList<string> Voltages { get; }
    public IReadOnlyList<string> RuvnTypes { get; }
    public IReadOnlyList<string> RuvnSwitches { get; }
    public IReadOnlyList<int> RuvnNominals { get; }
    public IReadOnlyList<string> FuseTypes { get; }
    public IReadOnlyList<string> FuseNominals { get; }
    public IReadOnlyList<string> RuvnExecutions { get; } = new[] { "Кабельный", "Воздушный", "Кабель-воздух", "Шинный мост" };
    public IReadOnlyList<string> CableExecutions { get; }
    public IReadOnlyList<string> RuvnSurgeArresterLocations { get; } = RuvnEngineering.SurgeArresterLocations;
    public IReadOnlyList<string> RuvnSurgeArresterThroughputs { get; } = RuvnEngineering.SurgeArresterThroughputs;
    public IReadOnlyList<string> RuvnVisibleBreakOptions { get; } = new[] { "РВЗ", "По схеме ячейки", "Нет" };
    public IReadOnlyList<string> RuvnEarthingSwitchOptions { get; } = new[] { "По схеме ячейки", "До выключателя", "После выключателя", "До и после выключателя", "Нет" };
    public IReadOnlyList<string> VacuumBreakerModels { get; } = new[] { "ВВ/TEL-10", "ВБЭК-10", "ВВ-10", "ВБП-10", "EVOLIS", "По ТЗ" };
    public IReadOnlyList<int> VacuumBreakerNominals { get; } = new[] { 630, 1000, 1250, 1600 };
    public IReadOnlyList<double> VacuumBreakerBreakingCurrentsKa { get; } = new[] { 12.5, 20, 25, 31.5 };
    public IReadOnlyList<string> VacuumBreakerDrives { get; } = new[] { "Блок управления", "Пружинно-моторный привод", "Электромагнитный привод", "По ТЗ" };
    public IReadOnlyList<string> VacuumBreakerInstallations { get; } = new[] { "Стационарный", "Выкатной", "Выдвижной", "По ТЗ" };
    public IReadOnlyList<string> OperationalPowerOptions { get; } = new[] { "220 В AC", "220 В DC", "Выпрямленное", "ШП/АКБ", "Конденсаторный блок", "По ТЗ" };
    public IReadOnlyList<string> RzaTerminals { get; } = new[] { "Сириус-2-Л", "БМРЗ", "МРЗС", "Sepam", "Орион", "УЗА", "По ТЗ" };
    public IReadOnlyList<int> LvNominals { get; }
    public IReadOnlyList<string> TtRatios { get; }
    public IReadOnlyList<string> PvrManufacturers { get; }
    public IReadOnlyList<string> ReManufacturers { get; }
    public IReadOnlyList<string> RpsManufacturers { get; }
    public IReadOnlyList<string> AvManufacturers { get; }
    public IReadOnlyList<string> LightingControlModes { get; } = new[]
    {
        "Ручной",
        "Фотореле",
        "Астротаймер",
        "Реле времени",
        "Авто + ручной",
    };
    public IReadOnlyList<LightingControlType> LightingControlTypeOptions { get; } = Enum.GetValues<LightingControlType>();
    public IReadOnlyList<string> RieseTypes { get; } = new[]
    {
        "ИБП с АКБ",
        "Блок питания с АКБ",
        "Резервный ввод",
        "Готовый шкаф/модуль",
    };
    public IReadOnlyList<RiseType> RiseTypeOptions { get; } = Enum.GetValues<RiseType>();
    public IReadOnlyList<string> RieseSupplySources { get; } = new[]
    {
        "ЩСН",
        "РУНН",
    };
    public IReadOnlyList<int> AvQuantityOptions { get; } = Enumerable.Range(0, 21).ToList();
    public IReadOnlyList<int> RpsQuantityOptions { get; } = Enumerable.Range(0, 9).ToList();
    public IReadOnlyList<string> OpsTypes { get; } = new[] { "Пожарная", "Охранная сигнализация", "Комбинированная" };
    public IReadOnlyList<string> OpsManufacturers { get; } = new[] { "Болид", "Рубеж", "Стрелец", "Сибирский Арсенал", "ИВС-Сигналспецавтоматика", "По ТЗ" };
    public IReadOnlyList<string> FeederMeteringTypes { get; } = new[]
    {
        "Нет",
        "Технический",
        "Коммерческий",
    };
    public ObservableCollection<OutgoingFeederViewModel> OutgoingFeeders { get; }
    public int OutgoingFeederCount => OutgoingFeeders.Count;

    // ===== Commands =====
    public RelayCommand NewProjectCommand { get; }
    public RelayCommand SaveProjectCommand { get; }
    public RelayCommand OpenProjectCommand { get; }
    public RelayCommand ExportExcelCommand { get; }
    public RelayCommand ExportPdfCommand { get; }

    // ===== Meta =====
    public string ProjectName { get => _cfg.ProjectName; set { if (_cfg.ProjectName != value) { _cfg.ProjectName = value; OnPropertyChanged(); } } }
    public string Author { get => _cfg.Author; set { if (_cfg.Author != value) { _cfg.Author = value; OnPropertyChanged(); } } }
    public bool ErrorsAcceptedForWork
    {
        get => _cfg.ErrorsAcceptedForWork;
        set
        {
            if (_cfg.ErrorsAcceptedForWork == value)
                return;

            _cfg.ErrorsAcceptedForWork = value;
            OnPropertyChanged();
            Recalculate();
        }
    }
    public string GridCompany 
    { 
        get => _cfg.GridCompany; 
        set 
        { 
            if (_cfg.GridCompany != value) 
            { 
                _cfg.GridCompany = value; 
                OnPropertyChanged(); 
                LoadTemplatesForGridCompany();
                ApplyGridCompanyProfile(notify: true);
                Recalculate(); 
            } 
        } 
    }

    public string CustomerProfileSummary
    {
        get
        {
            var profile = _env.Catalog.GetCustomerProfile(_cfg.GridCompany);
            return profile is null
                ? "Автонастройка не задана"
                : "Автонастройка применена";
        }
    }
    public string CustomerProfileAppliedSettings
    {
        get
        {
            var profile = _env.Catalog.GetCustomerProfile(_cfg.GridCompany);
            if (profile is null)
                return "нет автопрофиля";

            var s = profile.Settings;
            var parts = new List<string>();
            Add("цвет корпуса", s.BodyColor);
            Add("цвет дверей", s.DoorColor);
            Add("крыша", s.RoofColor);
            Add("основание", s.BaseColor);
            Add("климат", s.ClimateExecution);
            Add("IP", s.ProtectionDegree);
            Add("РУВН", s.RuvnExecution);
            Add("ОПН", s.RuvnSurgeArresterLocation);
            if (s.HasRigelLock.HasValue)
                parts.Add($"ригельный замок: {(s.HasRigelLock.Value ? "да" : "нет")}");
            Add("сетевой замок", s.NetworkLockType);
            if (s.HasPadlockProvision.HasValue)
                parts.Add($"проушины под навесной замок: {(s.HasPadlockProvision.Value ? "да" : "нет")}");
            Add("вентиляция", s.VentilationType);
            Add("логотип", s.LogoPlacement);
            Add("освещение", s.LightingAreas);
            if (s.RepairLightingVoltage.HasValue)
                parts.Add($"ремонтное освещение: {s.RepairLightingVoltage.Value} В");
            if (s.OutdoorLightingEnabled.HasValue)
                parts.Add($"наружное освещение: {(s.OutdoorLightingEnabled.Value ? "да" : "нет")}");
            if (s.MeterHeatingEnabled.HasValue)
                parts.Add($"обогрев счетчиков: {(s.MeterHeatingEnabled.Value ? "да" : "нет")}");
            return parts.Count == 0 ? "только примечания профиля" : string.Join("; ", parts);

            void Add(string label, string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    parts.Add($"{label}: {value}");
            }
        }
    }
    public string CustomerProfileManualChecks
    {
        get
        {
            var profile = _env.Catalog.GetCustomerProfile(_cfg.GridCompany);
            if (profile is null || profile.Notes.Count == 0)
                return "";

            return string.Join("; ", profile.Notes.Where(n => !string.IsNullOrWhiteSpace(n)));
        }
    }

    private void ApplyGridCompanyProfile(bool notify)
    {
        var profile = _env.Catalog.GetCustomerProfile(_cfg.GridCompany);
        if (profile is null)
        {
            OnPropertyChanged(nameof(CustomerProfileSummary));
            OnPropertyChanged(nameof(CustomerProfileAppliedSettings));
            OnPropertyChanged(nameof(CustomerProfileManualChecks));
            return;
        }

        var changed = CustomerProfileApplier.Apply(_cfg, profile);
        EnsureAuxiliaryDefaults();
        OnPropertyChanged(nameof(CustomerProfileSummary));
        OnPropertyChanged(nameof(CustomerProfileAppliedSettings));
        OnPropertyChanged(nameof(CustomerProfileManualChecks));
        if (!changed)
            return;

        OnPropertyChanged(string.Empty);
        if (notify)
            Notify?.Invoke("Автонастройка применена");
    }

    private void LoadTemplatesForGridCompany()
    {
        _env.Templates = DocTemplates.Load(_env.DataDir, _cfg.GridCompany);
        ChecklistTemplateText = _env.Templates.QcChecklist.ToPlainTextTemplate();
    }

    private string _checklistTemplateText = "";
    public string ChecklistTemplateText
    {
        get => _checklistTemplateText;
        set
        {
            if (_checklistTemplateText != value)
            {
                _checklistTemplateText = value;
                OnPropertyChanged();
            }
        }
    }

    public RelayCommand SaveChecklistCommand { get; }

    private void SaveChecklist()
    {
        _env.Templates.QcChecklist.FromPlainTextTemplate(_checklistTemplateText);
        _env.Templates.Save(_env.DataDir, _cfg.GridCompany);
        Recalculate();
        Notify?.Invoke($"Шаблон чек-листа сохранён для сети: {_cfg.GridCompany}");
    }

    // ===== Transformer =====
    public string Manufacturer
    {
        get => _cfg.Manufacturer;
        set
        {
            if (_cfg.Manufacturer == value) return;
            _cfg.Manufacturer = value;
            OnPropertyChanged();
            var marks = _env.Catalog.MarksFor(value);
            Marks.Clear();
            foreach (var m in marks) Marks.Add(m);
            // выбрать первую марку нового производителя
            Mark = marks.FirstOrDefault() ?? "";
        }
    }

    public string Mark { get => _cfg.Mark; set { if (_cfg.Mark != value) { _cfg.Mark = value; OnPropertyChanged(); ApplyRecommendedRuvnFuse(); Recalculate(); } } }

    // ===== Enclosure =====
    public string SteelType { get => _cfg.SteelType; set { if (_cfg.SteelType != value) { _cfg.SteelType = value; OnPropertyChanged(); Recalculate(); } } }
    public double Thickness { get => _cfg.Thickness; set { if (_cfg.Thickness != value) { _cfg.Thickness = value; OnPropertyChanged(); Recalculate(); } } }
    public string FloorMaterial { get => _cfg.FloorMaterial; set { if (_cfg.FloorMaterial != value) { _cfg.FloorMaterial = value; OnPropertyChanged(); Recalculate(); } } }
    public double FloorThickness { get => _cfg.FloorThickness; set { if (_cfg.FloorThickness != value) { _cfg.FloorThickness = value; OnPropertyChanged(); Recalculate(); } } }
    public string DoorMaterial { get => _cfg.DoorMaterial; set { if (_cfg.DoorMaterial != value) { _cfg.DoorMaterial = value; OnPropertyChanged(); Recalculate(); } } }
    public double DoorThickness { get => _cfg.DoorThickness; set { if (_cfg.DoorThickness != value) { _cfg.DoorThickness = value; OnPropertyChanged(); Recalculate(); } } }
    public string RemovablePanelMaterial { get => _cfg.RemovablePanelMaterial; set { if (_cfg.RemovablePanelMaterial != value) { _cfg.RemovablePanelMaterial = value; OnPropertyChanged(); Recalculate(); } } }
    public double RemovablePanelThickness { get => _cfg.RemovablePanelThickness; set { if (_cfg.RemovablePanelThickness != value) { _cfg.RemovablePanelThickness = value; OnPropertyChanged(); Recalculate(); } } }
    public string Channel { get => _cfg.Channel; set { if (_cfg.Channel != value) { _cfg.Channel = value; OnPropertyChanged(); Recalculate(); } } }
    public string BodyColor { get => _cfg.BodyColor; set { if (_cfg.BodyColor != value) { _cfg.BodyColor = value; OnPropertyChanged(); Recalculate(); } } }
    public string DoorColor { get => _cfg.DoorColor; set { if (_cfg.DoorColor != value) { _cfg.DoorColor = value; OnPropertyChanged(); Recalculate(); } } }
    public string RoofColor { get => _cfg.RoofColor; set { if (_cfg.RoofColor != value) { _cfg.RoofColor = value; OnPropertyChanged(); Recalculate(); } } }
    public string BaseColor { get => _cfg.BaseColor; set { if (_cfg.BaseColor != value) { _cfg.BaseColor = value; OnPropertyChanged(); Recalculate(); } } }
    public string InternalPanelColor { get => _cfg.InternalPanelColor; set { if (_cfg.InternalPanelColor != value) { _cfg.InternalPanelColor = value; OnPropertyChanged(); Recalculate(); } } }
    public string LogoColor { get => _cfg.LogoColor; set { if (_cfg.LogoColor != value) { _cfg.LogoColor = value; OnPropertyChanged(); Recalculate(); } } }
    public string ServicePlatformColor { get => _cfg.ServicePlatformColor; set { if (_cfg.ServicePlatformColor != value) { _cfg.ServicePlatformColor = value; OnPropertyChanged(); Recalculate(); } } }
    public string BusbarHvMaterial { get => _cfg.BusbarHvMaterial; set { if (_cfg.BusbarHvMaterial != value) { _cfg.BusbarHvMaterial = value; OnPropertyChanged(); Recalculate(); } } }
    public string BusbarLvMaterial { get => _cfg.BusbarLvMaterial; set { if (_cfg.BusbarLvMaterial != value) { _cfg.BusbarLvMaterial = value; OnPropertyChanged(); Recalculate(); } } }
    public string BusbarNMaterial { get => _cfg.BusbarNMaterial; set { if (_cfg.BusbarNMaterial != value) { _cfg.BusbarNMaterial = value; OnPropertyChanged(); Recalculate(); } } }
    public string ClimateExecution { get => _cfg.ClimateExecution; set { if (_cfg.ClimateExecution != value) { _cfg.ClimateExecution = value; OnPropertyChanged(); Recalculate(); } } }
    public string ProtectionDegree { get => _cfg.ProtectionDegree; set { if (_cfg.ProtectionDegree != value) { _cfg.ProtectionDegree = value; OnPropertyChanged(); Recalculate(); } } }
    public string DoorConfiguration { get => _cfg.DoorConfiguration; set { if (_cfg.DoorConfiguration != value) { _cfg.DoorConfiguration = value; OnPropertyChanged(); Recalculate(); } } }
    public string RuvnDoorConfiguration { get => _cfg.RuvnDoorConfiguration; set { if (_cfg.RuvnDoorConfiguration != value) { _cfg.RuvnDoorConfiguration = value; OnPropertyChanged(); Recalculate(); } } }
    public string RunnDoorConfiguration { get => _cfg.RunnDoorConfiguration; set { if (_cfg.RunnDoorConfiguration != value) { _cfg.RunnDoorConfiguration = value; OnPropertyChanged(); Recalculate(); } } }
    public string TransformerDoorConfiguration { get => _cfg.TransformerDoorConfiguration; set { if (_cfg.TransformerDoorConfiguration != value) { _cfg.TransformerDoorConfiguration = value; OnPropertyChanged(); Recalculate(); } } }
    public string LockType { get => _cfg.LockType; set { if (_cfg.LockType != value) { _cfg.LockType = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasRigelLock { get => _cfg.HasRigelLock; set { if (_cfg.HasRigelLock != value) { _cfg.HasRigelLock = value; OnPropertyChanged(); Recalculate(); } } }
    public string NetworkLockType { get => _cfg.NetworkLockType; set { if (_cfg.NetworkLockType != value) { _cfg.NetworkLockType = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasPadlockProvision { get => _cfg.HasPadlockProvision; set { if (_cfg.HasPadlockProvision != value) { _cfg.HasPadlockProvision = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasGrounding { get => true; set { _cfg.HasGrounding = true; OnPropertyChanged(); } }
    public string GroundingType { get => _cfg.GroundingType; set { if (_cfg.GroundingType != value) { _cfg.GroundingType = value; OnPropertyChanged(); Recalculate(); } } }
    public string VentilationType { get => _cfg.VentilationType; set { if (_cfg.VentilationType != value) { _cfg.VentilationType = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasRoofDeflector { get => _cfg.HasRoofDeflector; set { if (_cfg.HasRoofDeflector != value) { _cfg.HasRoofDeflector = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasNameplate { get => _cfg.HasNameplate; set { if (_cfg.HasNameplate != value) { _cfg.HasNameplate = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasDoorCanopies { get => _cfg.HasDoorCanopies; set { if (_cfg.HasDoorCanopies != value) { _cfg.HasDoorCanopies = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasDoorSeals { get => _cfg.HasDoorSeals; set { if (_cfg.HasDoorSeals != value) { _cfg.HasDoorSeals = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasTransformerMeshDoors { get => _cfg.HasTransformerMeshDoors; set { if (_cfg.HasTransformerMeshDoors != value) { _cfg.HasTransformerMeshDoors = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasLouverAnimalProtection { get => _cfg.HasLouverAnimalProtection; set { if (_cfg.HasLouverAnimalProtection != value) { _cfg.HasLouverAnimalProtection = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasAntiVandalHinges { get => _cfg.HasAntiVandalHinges; set { if (_cfg.HasAntiVandalHinges != value) { _cfg.HasAntiVandalHinges = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasDoorSealing { get => _cfg.HasDoorSealing; set { if (_cfg.HasDoorSealing != value) { _cfg.HasDoorSealing = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasServicePlatform { get => _cfg.HasServicePlatform; set { if (_cfg.HasServicePlatform != value) { _cfg.HasServicePlatform = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasLogo { get => _cfg.HasLogo; set { if (_cfg.HasLogo != value) { _cfg.HasLogo = value; OnPropertyChanged(); Recalculate(); } } }
    public string LogoPlacement { get => _cfg.LogoPlacement; set { if (_cfg.LogoPlacement != value) { _cfg.LogoPlacement = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasWarningLabels { get => _cfg.HasWarningLabels; set { if (_cfg.HasWarningLabels != value) { _cfg.HasWarningLabels = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasDispatcherNameplate { get => _cfg.HasDispatcherNameplate; set { if (_cfg.HasDispatcherNameplate != value) { _cfg.HasDispatcherNameplate = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasFeederLabels { get => _cfg.HasFeederLabels; set { if (_cfg.HasFeederLabels != value) { _cfg.HasFeederLabels = value; OnPropertyChanged(); Recalculate(); } } }
    public string MarkingNotes { get => _cfg.MarkingNotes; set { if (_cfg.MarkingNotes != value) { _cfg.MarkingNotes = value; OnPropertyChanged(); Recalculate(); } } }
    public string EnclosureNotes { get => _cfg.EnclosureNotes; set { if (_cfg.EnclosureNotes != value) { _cfg.EnclosureNotes = value; OnPropertyChanged(); Recalculate(); } } }

    // ===== Compartments =====
    public double LenRuvn { get => _cfg.LenRuvn; set { if (_cfg.LenRuvn != value) { _cfg.LenRuvn = value; OnPropertyChanged(); Recalculate(); } } }
    public double LenRunn { get => _cfg.LenRunn; set { if (_cfg.LenRunn != value) { _cfg.LenRunn = value; OnPropertyChanged(); Recalculate(); } } }
    public double TransformerTolerance { get => _cfg.TransformerTolerance; set { if (_cfg.TransformerTolerance != value) { _cfg.TransformerTolerance = value; OnPropertyChanged(); Recalculate(); } } }
    public double LengthBuffer { get => _cfg.LengthBuffer; set { if (_cfg.LengthBuffer != value) { _cfg.LengthBuffer = value; OnPropertyChanged(); Recalculate(); } } }

    // ===== РУВН =====
    public RuvnBranchEquipmentViewModel RuvnIncomingEquipment { get; private set; } = null!;
    public RuvnBranchEquipmentViewModel RuvnOutgoingEquipment { get; private set; } = null!;
    public RuvnBranchEquipmentViewModel RuvnTransformerEquipment { get; private set; } = null!;

    public string Voltage
    {
        get => _cfg.Voltage;
        set
        {
            if (_cfg.Voltage != value)
            {
                _cfg.Voltage = value;
                OnPropertyChanged();
                ApplyRecommendedRuvnFuse();
                OnPropertyChanged(nameof(RuvnRecommendedSurgeArresterVoltage));
                Recalculate();
            }
        }
    }

    public string RuvnType
    {
        get => _cfg.RuvnType;
        set
        {
            if (_cfg.RuvnType != value)
            {
                _cfg.RuvnType = value;
                OnPropertyChanged();
                EnsureRuvnDefaults();
                RaiseRuvnStateChanged();
                Recalculate();
            }
        }
    }
    public string RuvnSwitch { get => _cfg.RuvnSwitch; set { if (_cfg.RuvnSwitch != value) { _cfg.RuvnSwitch = value; OnPropertyChanged(); Recalculate(); } } }
    public int RuvnSwitchNominal { get => _cfg.RuvnSwitchNominal; set { if (_cfg.RuvnSwitchNominal != value) { _cfg.RuvnSwitchNominal = value; OnPropertyChanged(); Recalculate(); } } }
    public string FuseType { get => _cfg.FuseType; set { if (_cfg.FuseType != value) { _cfg.FuseType = value; OnPropertyChanged(); Recalculate(); } } }
    public string FuseNominal { get => _cfg.FuseNominal; set { if (_cfg.FuseNominal != value) { _cfg.FuseNominal = value; OnPropertyChanged(); Recalculate(); } } }
    public string RuvnExecution { get => _cfg.RuvnExecution; set { if (_cfg.RuvnExecution != value) { _cfg.RuvnExecution = value; OnPropertyChanged(); Recalculate(); } } }
    public bool RuvnSurgeArrester { get => _cfg.RuvnSurgeArrester; set { if (_cfg.RuvnSurgeArrester != value) { _cfg.RuvnSurgeArrester = value; OnPropertyChanged(); Recalculate(); } } }
    public bool IsRuvnEnabled => RuvnEngineering.HasRuvn(_cfg);
    public bool IsRuvnPassThrough => RuvnEngineering.IsPassThrough(_cfg);
    public string RuvnRecommendedFuseNominal => RuvnEngineering.RecommendedFuseNominal(_cfg, _env.Catalog);
    public string RuvnRecommendedSurgeArresterVoltage => RuvnEngineering.RecommendedSurgeArresterOperatingVoltage(_cfg.Voltage);
    public string RuvnSurgeArresterLocation
    {
        get => string.IsNullOrWhiteSpace(_cfg.RuvnSurgeArresterLocation) ? RuvnEngineering.NoSurgeArrester : _cfg.RuvnSurgeArresterLocation;
        set
        {
            if (_cfg.RuvnSurgeArresterLocation != value)
            {
                _cfg.RuvnSurgeArresterLocation = value;
                _cfg.RuvnSurgeArrester = !value.Equals(RuvnEngineering.NoSurgeArrester, StringComparison.OrdinalIgnoreCase);
                OnPropertyChanged();
                OnPropertyChanged(nameof(RuvnSurgeArrester));
                Recalculate();
            }
        }
    }
    public int RuvnSurgeArresterDischargeCurrentKa { get => _cfg.RuvnSurgeArresterDischargeCurrentKa; set { if (_cfg.RuvnSurgeArresterDischargeCurrentKa != value) { _cfg.RuvnSurgeArresterDischargeCurrentKa = value; OnPropertyChanged(); Recalculate(); } } }
    public string RuvnSurgeArresterThroughput { get => _cfg.RuvnSurgeArresterThroughput; set { if (_cfg.RuvnSurgeArresterThroughput != value) { _cfg.RuvnSurgeArresterThroughput = value; OnPropertyChanged(); Recalculate(); } } }
    public bool IsRuvnIncomingVacuum => RuvnEngineering.IsVacuumBreaker(_cfg.RuvnIncomingSwitch);
    public bool IsRuvnOutgoingVacuum => RuvnEngineering.IsVacuumBreaker(_cfg.RuvnOutgoingSwitch);
    public bool IsRuvnTransformerVacuum => RuvnEngineering.IsVacuumBreaker(_cfg.RuvnTransformerSwitch);
    public bool IsRuvnIncomingVacuumActive => IsRuvnPassThrough && IsRuvnIncomingVacuum;
    public bool IsRuvnOutgoingVacuumActive => IsRuvnPassThrough && IsRuvnOutgoingVacuum;
    public bool IsRuvnTransformerVacuumActive => IsRuvnEnabled && IsRuvnTransformerVacuum;
    public bool HasRuvnVacuumBranches => IsRuvnIncomingVacuumActive || IsRuvnOutgoingVacuumActive || IsRuvnTransformerVacuumActive;
    public bool ShowRuvnRecommendedFuse => IsRuvnEnabled && !IsRuvnTransformerVacuum;
    public bool IsRuvnIncomingFuseEnabled => !IsRuvnIncomingVacuum;
    public bool IsRuvnOutgoingFuseEnabled => !IsRuvnOutgoingVacuum;
    public bool IsRuvnTransformerFuseEnabled => !IsRuvnTransformerVacuum;
    public string RuvnIncomingSwitch
    {
        get => _cfg.RuvnIncomingSwitch;
        set
        {
            if (_cfg.RuvnIncomingSwitch != value)
            {
                _cfg.RuvnIncomingSwitch = value;
                ApplyRuvnBranchSwitchRules("incoming");
                RaiseRuvnStateChanged();
                Recalculate();
            }
        }
    }
    public int RuvnIncomingSwitchNominal
    {
        get => _cfg.RuvnIncomingSwitchNominal;
        set
        {
            if (_cfg.RuvnIncomingSwitchNominal != value)
            {
                _cfg.RuvnIncomingSwitchNominal = value;
                SyncRuvnEquipmentNominal(_cfg.RuvnIncomingEquipment, value);
                RuvnIncomingEquipment?.Refresh();
                OnPropertyChanged();
                Recalculate();
            }
        }
    }
    public bool RuvnIncomingFuseOn
    {
        get => _cfg.RuvnIncomingFuseOn;
        set
        {
            var normalized = IsRuvnIncomingVacuum ? false : value;
            if (_cfg.RuvnIncomingFuseOn != normalized)
            {
                _cfg.RuvnIncomingFuseOn = normalized;
                OnPropertyChanged();
                Recalculate();
            }
        }
    }
    public string RuvnIncomingFuseType { get => _cfg.RuvnIncomingFuseType; set { if (_cfg.RuvnIncomingFuseType != value) { _cfg.RuvnIncomingFuseType = value; OnPropertyChanged(); Recalculate(); } } }
    public string RuvnIncomingFuseNominal { get => _cfg.RuvnIncomingFuseNominal; set { if (_cfg.RuvnIncomingFuseNominal != value) { _cfg.RuvnIncomingFuseNominal = value; OnPropertyChanged(); Recalculate(); } } }
    public string RuvnOutgoingSwitch
    {
        get => _cfg.RuvnOutgoingSwitch;
        set
        {
            if (_cfg.RuvnOutgoingSwitch != value)
            {
                _cfg.RuvnOutgoingSwitch = value;
                ApplyRuvnBranchSwitchRules("outgoing");
                RaiseRuvnStateChanged();
                Recalculate();
            }
        }
    }
    public int RuvnOutgoingSwitchNominal
    {
        get => _cfg.RuvnOutgoingSwitchNominal;
        set
        {
            if (_cfg.RuvnOutgoingSwitchNominal != value)
            {
                _cfg.RuvnOutgoingSwitchNominal = value;
                SyncRuvnEquipmentNominal(_cfg.RuvnOutgoingEquipment, value);
                RuvnOutgoingEquipment?.Refresh();
                OnPropertyChanged();
                Recalculate();
            }
        }
    }
    public bool RuvnOutgoingFuseOn
    {
        get => _cfg.RuvnOutgoingFuseOn;
        set
        {
            var normalized = IsRuvnOutgoingVacuum ? false : value;
            if (_cfg.RuvnOutgoingFuseOn != normalized)
            {
                _cfg.RuvnOutgoingFuseOn = normalized;
                OnPropertyChanged();
                Recalculate();
            }
        }
    }
    public string RuvnOutgoingFuseType { get => _cfg.RuvnOutgoingFuseType; set { if (_cfg.RuvnOutgoingFuseType != value) { _cfg.RuvnOutgoingFuseType = value; OnPropertyChanged(); Recalculate(); } } }
    public string RuvnOutgoingFuseNominal { get => _cfg.RuvnOutgoingFuseNominal; set { if (_cfg.RuvnOutgoingFuseNominal != value) { _cfg.RuvnOutgoingFuseNominal = value; OnPropertyChanged(); Recalculate(); } } }
    public string RuvnTransformerSwitch
    {
        get => _cfg.RuvnTransformerSwitch;
        set
        {
            if (_cfg.RuvnTransformerSwitch != value)
            {
                _cfg.RuvnTransformerSwitch = value;
                _cfg.RuvnSwitch = value;
                ApplyRuvnBranchSwitchRules("transformer");
                RaiseRuvnStateChanged();
                Recalculate();
            }
        }
    }
    public int RuvnTransformerSwitchNominal
    {
        get => _cfg.RuvnTransformerSwitchNominal;
        set
        {
            if (_cfg.RuvnTransformerSwitchNominal != value)
            {
                _cfg.RuvnTransformerSwitchNominal = value;
                _cfg.RuvnSwitchNominal = value;
                SyncRuvnEquipmentNominal(_cfg.RuvnTransformerEquipment, value);
                RuvnTransformerEquipment?.Refresh();
                OnPropertyChanged();
                OnPropertyChanged(nameof(RuvnSwitchNominal));
                Recalculate();
            }
        }
    }
    public bool RuvnTransformerFuseOn
    {
        get => _cfg.RuvnTransformerFuseOn;
        set
        {
            var normalized = IsRuvnTransformerVacuum ? false : value;
            if (_cfg.RuvnTransformerFuseOn != normalized)
            {
                _cfg.RuvnTransformerFuseOn = normalized;
                OnPropertyChanged();
                Recalculate();
            }
        }
    }
    public string RuvnTransformerFuseType
    {
        get => _cfg.RuvnTransformerFuseType;
        set
        {
            if (_cfg.RuvnTransformerFuseType != value)
            {
                _cfg.RuvnTransformerFuseType = value;
                _cfg.FuseType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FuseType));
                Recalculate();
            }
        }
    }
    public string RuvnTransformerFuseNominal
    {
        get => _cfg.RuvnTransformerFuseNominal;
        set
        {
            if (_cfg.RuvnTransformerFuseNominal != value)
            {
                _cfg.RuvnTransformerFuseNominal = value;
                _cfg.FuseNominal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FuseNominal));
                Recalculate();
            }
        }
    }

    private void EnsureRuvnDefaults()
    {
        var defaultSwitch = RuvnSwitches.FirstOrDefault(s => !s.Equals("Нет", StringComparison.OrdinalIgnoreCase)) ?? "";
        var defaultFuse = FuseTypes.FirstOrDefault(s => !s.Equals("Нет", StringComparison.OrdinalIgnoreCase)) ?? "";
        var recommendedFuse = RuvnEngineering.RecommendedFuseNominal(_cfg, _env.Catalog);
        _cfg.RuvnIncomingEquipment ??= new RuvnBranchEquipmentConfig();
        _cfg.RuvnOutgoingEquipment ??= new RuvnBranchEquipmentConfig();
        _cfg.RuvnTransformerEquipment ??= new RuvnBranchEquipmentConfig();

        if (string.IsNullOrWhiteSpace(_cfg.RuvnTransformerSwitch))
            _cfg.RuvnTransformerSwitch = RuvnEngineering.HasDevice(_cfg.RuvnSwitch) ? _cfg.RuvnSwitch : defaultSwitch;
        if (_cfg.RuvnTransformerSwitchNominal <= 0)
            _cfg.RuvnTransformerSwitchNominal = _cfg.RuvnSwitchNominal > 0 ? _cfg.RuvnSwitchNominal : 630;
        if (string.IsNullOrWhiteSpace(_cfg.RuvnTransformerFuseType))
            _cfg.RuvnTransformerFuseType = RuvnEngineering.HasDevice(_cfg.FuseType) ? _cfg.FuseType : defaultFuse;
        if (string.IsNullOrWhiteSpace(_cfg.RuvnTransformerFuseNominal))
            _cfg.RuvnTransformerFuseNominal = !string.IsNullOrWhiteSpace(recommendedFuse) ? recommendedFuse : _cfg.FuseNominal;
        _cfg.RuvnTransformerFuseOn = !RuvnEngineering.IsVacuumBreaker(_cfg.RuvnTransformerSwitch)
            && (_cfg.RuvnTransformerFuseOn || RuvnEngineering.HasDevice(_cfg.RuvnTransformerFuseType));

        if (string.IsNullOrWhiteSpace(_cfg.RuvnIncomingSwitch))
            _cfg.RuvnIncomingSwitch = defaultSwitch;
        if (_cfg.RuvnIncomingSwitchNominal <= 0)
            _cfg.RuvnIncomingSwitchNominal = 630;
        if (string.IsNullOrWhiteSpace(_cfg.RuvnOutgoingSwitch))
            _cfg.RuvnOutgoingSwitch = defaultSwitch;
        if (_cfg.RuvnOutgoingSwitchNominal <= 0)
            _cfg.RuvnOutgoingSwitchNominal = 630;

        if (string.IsNullOrWhiteSpace(_cfg.RuvnSurgeArresterLocation))
            _cfg.RuvnSurgeArresterLocation = _cfg.RuvnSurgeArrester
                ? RuvnEngineering.SurgeArresterAtTransformer
                : RuvnEngineering.NoSurgeArrester;
        _cfg.RuvnSurgeArrester = !_cfg.RuvnSurgeArresterLocation.Equals(RuvnEngineering.NoSurgeArrester, StringComparison.OrdinalIgnoreCase);
        if (_cfg.RuvnSurgeArresterDischargeCurrentKa <= 0)
            _cfg.RuvnSurgeArresterDischargeCurrentKa = 5;
        if (string.IsNullOrWhiteSpace(_cfg.RuvnSurgeArresterThroughput))
            _cfg.RuvnSurgeArresterThroughput = "Класс пропускной способности 2";

        _cfg.RuvnSwitch = _cfg.RuvnTransformerSwitch;
        _cfg.RuvnSwitchNominal = _cfg.RuvnTransformerSwitchNominal;
        _cfg.FuseType = _cfg.RuvnTransformerFuseType;
        _cfg.FuseNominal = _cfg.RuvnTransformerFuseNominal;

        ApplyRuvnBranchSwitchRules("incoming");
        ApplyRuvnBranchSwitchRules("outgoing");
        ApplyRuvnBranchSwitchRules("transformer");
    }

    private void ApplyRuvnBranchSwitchRules(string branchKey)
    {
        var (isVacuum, equipment, nominal) = branchKey switch
        {
            "incoming" => (RuvnEngineering.IsVacuumBreaker(_cfg.RuvnIncomingSwitch), _cfg.RuvnIncomingEquipment, _cfg.RuvnIncomingSwitchNominal),
            "outgoing" => (RuvnEngineering.IsVacuumBreaker(_cfg.RuvnOutgoingSwitch), _cfg.RuvnOutgoingEquipment, _cfg.RuvnOutgoingSwitchNominal),
            _ => (RuvnEngineering.IsVacuumBreaker(_cfg.RuvnTransformerSwitch), _cfg.RuvnTransformerEquipment, _cfg.RuvnTransformerSwitchNominal),
        };

        NormalizeRuvnEquipment(equipment, nominal);
        if (!isVacuum)
            return;

        SyncRuvnEquipmentNominal(equipment, nominal);
        switch (branchKey)
        {
            case "incoming":
                _cfg.RuvnIncomingFuseOn = false;
                break;
            case "outgoing":
                _cfg.RuvnOutgoingFuseOn = false;
                break;
            default:
                _cfg.RuvnTransformerFuseOn = false;
                break;
        }
    }

    private static void NormalizeRuvnEquipment(RuvnBranchEquipmentConfig equipment, int branchNominal)
    {
        if (string.IsNullOrWhiteSpace(equipment.VisibleBreakBefore))
            equipment.VisibleBreakBefore = "РВЗ";
        if (string.IsNullOrWhiteSpace(equipment.VisibleBreakAfter))
            equipment.VisibleBreakAfter = "РВЗ";
        if (string.IsNullOrWhiteSpace(equipment.EarthingSwitch))
            equipment.EarthingSwitch = "По схеме ячейки";
        if (string.IsNullOrWhiteSpace(equipment.VacuumBreakerModel))
            equipment.VacuumBreakerModel = "ВВ/TEL-10";
        if (equipment.VacuumBreakerNominal <= 0)
            equipment.VacuumBreakerNominal = branchNominal > 0 ? branchNominal : 630;
        if (equipment.VacuumBreakerBreakingCurrentKa <= 0)
            equipment.VacuumBreakerBreakingCurrentKa = 20;
        if (string.IsNullOrWhiteSpace(equipment.VacuumBreakerDrive))
            equipment.VacuumBreakerDrive = "Блок управления";
        if (string.IsNullOrWhiteSpace(equipment.VacuumBreakerInstallation))
            equipment.VacuumBreakerInstallation = "Стационарный";
        if (string.IsNullOrWhiteSpace(equipment.OperationalPower))
            equipment.OperationalPower = "220 В AC";
        if (string.IsNullOrWhiteSpace(equipment.RzaTerminal))
            equipment.RzaTerminal = "Сириус-2-Л";
        if (string.IsNullOrWhiteSpace(equipment.ProtectionCtRatio))
            equipment.ProtectionCtRatio = "600/5";
        if (equipment.ProtectionCtQuantity <= 0)
            equipment.ProtectionCtQuantity = 3;
        if (equipment.HasVoltageTransformer && string.IsNullOrWhiteSpace(equipment.VoltageTransformerModel))
            equipment.VoltageTransformerModel = "НАЛИ";
    }

    private static void SyncRuvnEquipmentNominal(RuvnBranchEquipmentConfig equipment, int branchNominal)
    {
        if (branchNominal > 0)
            equipment.VacuumBreakerNominal = branchNominal;
    }

    private void RefreshRuvnEquipmentViewModels()
    {
        _cfg.RuvnIncomingEquipment ??= new RuvnBranchEquipmentConfig();
        _cfg.RuvnOutgoingEquipment ??= new RuvnBranchEquipmentConfig();
        _cfg.RuvnTransformerEquipment ??= new RuvnBranchEquipmentConfig();
        NormalizeRuvnEquipment(_cfg.RuvnIncomingEquipment, _cfg.RuvnIncomingSwitchNominal);
        NormalizeRuvnEquipment(_cfg.RuvnOutgoingEquipment, _cfg.RuvnOutgoingSwitchNominal);
        NormalizeRuvnEquipment(_cfg.RuvnTransformerEquipment, _cfg.RuvnTransformerSwitchNominal);

        RuvnIncomingEquipment = new RuvnBranchEquipmentViewModel(this, _cfg.RuvnIncomingEquipment);
        RuvnOutgoingEquipment = new RuvnBranchEquipmentViewModel(this, _cfg.RuvnOutgoingEquipment);
        RuvnTransformerEquipment = new RuvnBranchEquipmentViewModel(this, _cfg.RuvnTransformerEquipment);
        OnPropertyChanged(nameof(RuvnIncomingEquipment));
        OnPropertyChanged(nameof(RuvnOutgoingEquipment));
        OnPropertyChanged(nameof(RuvnTransformerEquipment));
    }

    private void ApplyRecommendedRuvnFuse()
    {
        if (RuvnEngineering.IsVacuumBreaker(_cfg.RuvnTransformerSwitch))
        {
            OnPropertyChanged(nameof(RuvnRecommendedFuseNominal));
            return;
        }

        var recommended = RuvnEngineering.RecommendedFuseNominal(_cfg, _env.Catalog);
        if (string.IsNullOrWhiteSpace(recommended))
            return;

        _cfg.RuvnTransformerFuseNominal = recommended;
        _cfg.FuseNominal = recommended;
        OnPropertyChanged(nameof(RuvnTransformerFuseNominal));
        OnPropertyChanged(nameof(FuseNominal));
        OnPropertyChanged(nameof(RuvnRecommendedFuseNominal));
    }

    private void RaiseRuvnStateChanged()
    {
        foreach (var name in new[]
        {
            nameof(IsRuvnEnabled), nameof(IsRuvnPassThrough), nameof(RuvnRecommendedFuseNominal),
            nameof(RuvnRecommendedSurgeArresterVoltage), nameof(RuvnSwitch), nameof(RuvnSwitchNominal),
            nameof(FuseType), nameof(FuseNominal), nameof(RuvnSurgeArrester),
            nameof(RuvnSurgeArresterLocation), nameof(RuvnSurgeArresterDischargeCurrentKa), nameof(RuvnSurgeArresterThroughput),
            nameof(RuvnIncomingSwitch), nameof(RuvnIncomingSwitchNominal), nameof(RuvnIncomingFuseOn),
            nameof(RuvnIncomingFuseType), nameof(RuvnIncomingFuseNominal),
            nameof(IsRuvnIncomingVacuum), nameof(IsRuvnIncomingVacuumActive), nameof(IsRuvnIncomingFuseEnabled), nameof(RuvnIncomingEquipment),
            nameof(RuvnOutgoingSwitch), nameof(RuvnOutgoingSwitchNominal), nameof(RuvnOutgoingFuseOn),
            nameof(RuvnOutgoingFuseType), nameof(RuvnOutgoingFuseNominal),
            nameof(IsRuvnOutgoingVacuum), nameof(IsRuvnOutgoingVacuumActive), nameof(IsRuvnOutgoingFuseEnabled), nameof(RuvnOutgoingEquipment),
            nameof(RuvnTransformerSwitch), nameof(RuvnTransformerSwitchNominal), nameof(RuvnTransformerFuseOn),
            nameof(RuvnTransformerFuseType), nameof(RuvnTransformerFuseNominal),
            nameof(IsRuvnTransformerVacuum), nameof(IsRuvnTransformerVacuumActive), nameof(IsRuvnTransformerFuseEnabled), nameof(RuvnTransformerEquipment),
            nameof(HasRuvnVacuumBranches), nameof(ShowRuvnRecommendedFuse),
        })
        {
            OnPropertyChanged(name);
        }
    }

    // ===== РУНН ввод =====
    public bool PvrOn { get => _cfg.PvrOn; set { if (_cfg.PvrOn != value) { _cfg.PvrOn = value; OnPropertyChanged(); Recalculate(); } } }
    public int PvrNominal { get => _cfg.PvrNominal; set { if (_cfg.PvrNominal != value) { _cfg.PvrNominal = value; OnPropertyChanged(); EnsureInputMeterCt(forceRatio: true); Recalculate(); } } }
    public string PvrManufacturer { get => _cfg.PvrManufacturer; set { if (_cfg.PvrManufacturer != value) { _cfg.PvrManufacturer = value; OnPropertyChanged(); Recalculate(); } } }
    public bool ReOn { get => _cfg.ReOn; set { if (_cfg.ReOn != value) { _cfg.ReOn = value; OnPropertyChanged(); Recalculate(); } } }
    public int ReNominal { get => _cfg.ReNominal; set { if (_cfg.ReNominal != value) { _cfg.ReNominal = value; OnPropertyChanged(); EnsureInputMeterCt(forceRatio: true); Recalculate(); } } }
    public string ReManufacturer { get => _cfg.ReManufacturer; set { if (_cfg.ReManufacturer != value) { _cfg.ReManufacturer = value; OnPropertyChanged(); Recalculate(); } } }
    public bool AvInOn { get => _cfg.AvInOn; set { if (_cfg.AvInOn != value) { _cfg.AvInOn = value; OnPropertyChanged(); Recalculate(); } } }
    public int AvInNominal { get => _cfg.AvInNominal; set { if (_cfg.AvInNominal != value) { _cfg.AvInNominal = value; OnPropertyChanged(); EnsureInputMeterCt(forceRatio: true); Recalculate(); } } }
    public string AvInManufacturer { get => _cfg.AvInManufacturer; set { if (_cfg.AvInManufacturer != value) { _cfg.AvInManufacturer = value; OnPropertyChanged(); Recalculate(); } } }
    public bool RunnSurgeArrester { get => _cfg.RunnSurgeArrester; set { if (_cfg.RunnSurgeArrester != value) { _cfg.RunnSurgeArrester = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasCt { get => _cfg.HasCt; set { if (_cfg.HasCt != value) { _cfg.HasCt = value; OnPropertyChanged(); Recalculate(); } } }
    public string CtRatio { get => _cfg.CtRatio; set { if (_cfg.CtRatio != value) { _cfg.CtRatio = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasCtKip { get => _cfg.HasCtKip; set { if (_cfg.HasCtKip != value) { _cfg.HasCtKip = value; OnPropertyChanged(); Recalculate(); } } }
    public string CtKipRatio { get => _cfg.CtKipRatio; set { if (_cfg.CtKipRatio != value) { _cfg.CtKipRatio = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasMeter
    {
        get => _cfg.HasMeter;
        set
        {
            if (_cfg.HasMeter != value)
            {
                _cfg.HasMeter = value;
                OnPropertyChanged();
                if (value)
                {
                    EnsureInputMeterCt(forceRatio: true);
                }
                else if (_cfg.HasCt)
                {
                    _cfg.HasCt = false;
                    OnPropertyChanged(nameof(HasCt));
                }
                Recalculate();
            }
        }
    }

    // ===== Отходящие линии =====
    public bool AvOn { get => _cfg.AvOn; set { if (_cfg.AvOn != value) { _cfg.AvOn = value; OnPropertyChanged(); SyncOutgoingFeeders(); } } }
    public int AvQty { get => _cfg.AvQty; set { var normalized = Math.Clamp(value, 0, 20); if (_cfg.AvQty != normalized) { _cfg.AvQty = normalized; OnPropertyChanged(); SyncOutgoingFeeders(); } } }
    public string AvBrand { get => _cfg.AvBrand; set { if (_cfg.AvBrand != value) { _cfg.AvBrand = value; OnPropertyChanged(); SyncOutgoingFeeders(); } } }
    public bool RpsOn { get => _cfg.RpsOn; set { if (_cfg.RpsOn != value) { _cfg.RpsOn = value; OnPropertyChanged(); SyncOutgoingFeeders(); } } }
    public int RpsQty { get => _cfg.RpsQty; set { var normalized = Math.Clamp(value, 0, 8); if (_cfg.RpsQty != normalized) { _cfg.RpsQty = normalized; OnPropertyChanged(); SyncOutgoingFeeders(); } } }
    public string RpsBrand { get => _cfg.RpsBrand; set { if (_cfg.RpsBrand != value) { _cfg.RpsBrand = value; OnPropertyChanged(); SyncOutgoingFeeders(); } } }
    public string OutgoingExecution { get => _cfg.OutgoingExecution; set { if (_cfg.OutgoingExecution != value) { _cfg.OutgoingExecution = value; OnPropertyChanged(); Recalculate(); } } }

    // ===== Собственные нужды / ЩСН =====
    private AuxiliaryNeedsConfig Aux => _cfg.AuxiliaryNeeds ??= new AuxiliaryNeedsConfig();

    public IReadOnlyList<string> AuxCabinetManufacturers => DeviceManufacturers("Шкаф собственных нужд", "ЩСН");
    public IReadOnlyList<string> AuxCabinetModels => DeviceModelNames("Шкаф собственных нужд", AuxCabinetManufacturer, "ЩСН");
    public IReadOnlyList<string> AuxBreakerManufacturers => DeviceManufacturers("АВ", "ЩСН");
    public IReadOnlyList<string> AuxMainBreakerModels => DeviceModelNames("АВ", AuxMainBreakerManufacturer, "ЩСН", "Ввод ЩСН");
    public IReadOnlyList<int> AuxMainBreakerNominals => DeviceNominals("АВ", AuxMainBreakerManufacturer, AuxMainBreakerModel, "ЩСН", "Ввод ЩСН");
    public IReadOnlyList<string> LightingBreakerModels => DeviceModelNames("АВ", LightingBreakerManufacturer, "ЩСН", "Цепь освещения");
    public IReadOnlyList<int> LightingBreakerNominals => DeviceNominals("АВ", LightingBreakerManufacturer, LightingBreakerModel, "ЩСН", "Цепь освещения");
    public IReadOnlyList<string> LightingFixtureModels => DeviceModelNames("светильник", "", "Освещение");
    public IReadOnlyList<string> PhotoRelayModels => DeviceModelNames("фотореле", "", "Освещение");
    public IReadOnlyList<string> AstroTimerModels => DeviceModelNames("астротаймер", "", "Освещение");
    public IReadOnlyList<string> TimeRelayModels => DeviceModelNames("реле времени", "", "ЩСН");
    public IReadOnlyList<string> SocketBreakerModels => DeviceModelNames("АВ", SocketBreakerManufacturer, "ЩСН");
    public IReadOnlyList<int> SocketBreakerNominals => DeviceNominals("АВ", SocketBreakerManufacturer, SocketBreakerModel, "ЩСН");
    public IReadOnlyList<string> SocketModels => DeviceModelNames("розетка", "", "ЩСН");
    public IReadOnlyList<string> HeatingBreakerModels => DeviceModelNames("АВ", HeatingBreakerManufacturer, "ЩСН");
    public IReadOnlyList<int> HeatingBreakerNominals => DeviceNominals("АВ", HeatingBreakerManufacturer, HeatingBreakerModel, "ЩСН");
    public IReadOnlyList<string> HeaterModels => DeviceModelNames("обогреватель", "", "ЩСН");
    public IReadOnlyList<string> ThermostatModels => DeviceModelNames("термостат", "", "ЩСН");
    public IReadOnlyList<string> VentilationBreakerModels => DeviceModelNames("АВ", VentilationBreakerManufacturer, "ЩСН");
    public IReadOnlyList<int> VentilationBreakerNominals => DeviceNominals("АВ", VentilationBreakerManufacturer, VentilationBreakerModel, "ЩСН");
    public IReadOnlyList<string> FanModels => DeviceModelNames("вентилятор", "", "ЩСН");
    public IReadOnlyList<string> RieseProtectionModels => DeviceModelNames("АВ", RieseProtectionManufacturer, "ЩСН");
    public IReadOnlyList<string> RieseModuleModels => DeviceModelNames("РИСЭ", "", "РИСЭ");
    public IReadOnlyList<string> OpsModels => OpsModelOptions(OpsManufacturer);

    public bool AuxEnabled { get => HasAuxiliaryCabinet; set => HasAuxiliaryCabinet = value; }
    public bool HasAuxiliaryCabinet
    {
        get => Aux.HasAuxiliaryCabinet;
        set
        {
            if (Aux.HasAuxiliaryCabinet == value) return;
            Aux.HasAuxiliaryCabinet = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AuxEnabled));
            Recalculate();
        }
    }
    public bool AuxSupplyFromRunn { get => Aux.SupplyFromRunn; set { if (Aux.SupplyFromRunn != value) { Aux.SupplyFromRunn = value; OnPropertyChanged(); Recalculate(); } } }
    public string AuxCabinetManufacturer { get => Aux.CabinetManufacturer; set { SetAuxString(nameof(AuxCabinetManufacturer), value, v => Aux.CabinetManufacturer = v, RefreshAuxCabinet); } }
    public string AuxCabinetModel { get => Aux.CabinetModel; set { SetAuxString(nameof(AuxCabinetModel), value, v => Aux.CabinetModel = v); } }
    public string AuxMainBreakerManufacturer { get => Aux.MainBreakerManufacturer; set { SetAuxString(nameof(AuxMainBreakerManufacturer), value, v => Aux.MainBreakerManufacturer = v, RefreshAuxMainBreaker); } }
    public string AuxMainBreakerModel { get => Aux.MainBreakerModel; set { SetAuxString(nameof(AuxMainBreakerModel), value, v => Aux.MainBreakerModel = v, RefreshAuxMainBreakerNominals); } }
    public int AuxMainBreakerNominal { get => Aux.MainBreakerNominal; set { SetAuxInt(nameof(AuxMainBreakerNominal), value, v => Aux.MainBreakerNominal = v); } }

    public bool LightingEnabled { get => HasLighting; set => HasLighting = value; }
    public bool HasLighting
    {
        get => Aux.HasLighting;
        set
        {
            if (Aux.HasLighting == value) return;
            Aux.HasLighting = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LightingEnabled));
            OnPropertyChanged(nameof(IsLightingControlEnabled));
            Recalculate();
        }
    }
    public bool IsLightingControlEnabled => HasLighting;
    public int LightingCircuits { get => Aux.LightingCircuits; set { SetAuxInt(nameof(LightingCircuits), Math.Max(0, value), v => Aux.LightingCircuits = v); } }
    public string LightingBreakerManufacturer { get => Aux.LightingBreakerManufacturer; set { SetAuxString(nameof(LightingBreakerManufacturer), value, v => Aux.LightingBreakerManufacturer = v, RefreshLightingBreaker); } }
    public string LightingBreakerModel { get => Aux.LightingBreakerModel; set { SetAuxString(nameof(LightingBreakerModel), value, v => Aux.LightingBreakerModel = v, RefreshLightingBreakerNominals); } }
    public int LightingBreakerNominal { get => Aux.LightingBreakerNominal; set { SetAuxInt(nameof(LightingBreakerNominal), value, v => Aux.LightingBreakerNominal = v); } }
    public string LightingControlMode { get => Aux.LightingControlMode; set { SetAuxString(nameof(LightingControlMode), value, v => Aux.LightingControlMode = v, () => OnPropertyChanged(nameof(LightingControlType))); } }
    public LightingControlType LightingControlType
    {
        get => Aux.LightingControlType;
        set
        {
            if (Aux.LightingControlType == value) return;
            Aux.LightingControlType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LightingControlMode));
            Recalculate();
        }
    }
    public string LightingFixtureModel { get => Aux.LightingFixtureModel; set { SetAuxString(nameof(LightingFixtureModel), value, v => Aux.LightingFixtureModel = v); } }
    public int LightingFixtureQuantity { get => Aux.LightingFixtureQuantity; set { SetAuxInt(nameof(LightingFixtureQuantity), Math.Max(0, value), v => Aux.LightingFixtureQuantity = v); } }
    public string LightingAreas { get => Aux.LightingAreas; set { SetAuxString(nameof(LightingAreas), value, v => Aux.LightingAreas = v); } }
    public int RepairLightingVoltage { get => Aux.RepairLightingVoltage; set { SetAuxInt(nameof(RepairLightingVoltage), Math.Max(0, value), v => Aux.RepairLightingVoltage = v); } }
    public bool OutdoorLightingEnabled { get => Aux.OutdoorLightingEnabled; set { if (Aux.OutdoorLightingEnabled != value) { Aux.OutdoorLightingEnabled = value; OnPropertyChanged(); Recalculate(); } } }
    public string PhotoRelayModel { get => Aux.PhotoRelayModel; set { SetAuxString(nameof(PhotoRelayModel), value, v => Aux.PhotoRelayModel = v); } }
    public string AstroTimerModel { get => Aux.AstroTimerModel; set { SetAuxString(nameof(AstroTimerModel), value, v => Aux.AstroTimerModel = v); } }
    public string TimeRelayModel { get => Aux.TimeRelayModel; set { SetAuxString(nameof(TimeRelayModel), value, v => Aux.TimeRelayModel = v); } }

    public bool SocketEnabled { get => Aux.SocketEnabled; set { if (Aux.SocketEnabled != value) { Aux.SocketEnabled = value; OnPropertyChanged(); Recalculate(); } } }
    public string SocketBreakerManufacturer { get => Aux.SocketBreakerManufacturer; set { SetAuxString(nameof(SocketBreakerManufacturer), value, v => Aux.SocketBreakerManufacturer = v, RefreshSocketBreaker); } }
    public string SocketBreakerModel { get => Aux.SocketBreakerModel; set { SetAuxString(nameof(SocketBreakerModel), value, v => Aux.SocketBreakerModel = v, RefreshSocketBreakerNominals); } }
    public int SocketBreakerNominal { get => Aux.SocketBreakerNominal; set { SetAuxInt(nameof(SocketBreakerNominal), value, v => Aux.SocketBreakerNominal = v); } }
    public string SocketModel { get => Aux.SocketModel; set { SetAuxString(nameof(SocketModel), value, v => Aux.SocketModel = v); } }
    public int SocketQuantity { get => Aux.SocketQuantity; set { SetAuxInt(nameof(SocketQuantity), Math.Max(0, value), v => Aux.SocketQuantity = v); } }

    public bool HeatingEnabled { get => Aux.HeatingEnabled; set { if (Aux.HeatingEnabled != value) { Aux.HeatingEnabled = value; OnPropertyChanged(); Recalculate(); } } }
    public string HeatingBreakerManufacturer { get => Aux.HeatingBreakerManufacturer; set { SetAuxString(nameof(HeatingBreakerManufacturer), value, v => Aux.HeatingBreakerManufacturer = v, RefreshHeatingBreaker); } }
    public string HeatingBreakerModel { get => Aux.HeatingBreakerModel; set { SetAuxString(nameof(HeatingBreakerModel), value, v => Aux.HeatingBreakerModel = v, RefreshHeatingBreakerNominals); } }
    public int HeatingBreakerNominal { get => Aux.HeatingBreakerNominal; set { SetAuxInt(nameof(HeatingBreakerNominal), value, v => Aux.HeatingBreakerNominal = v); } }
    public string HeaterModel { get => Aux.HeaterModel; set { SetAuxString(nameof(HeaterModel), value, v => Aux.HeaterModel = v); } }
    public int HeaterQuantity { get => Aux.HeaterQuantity; set { SetAuxInt(nameof(HeaterQuantity), Math.Max(0, value), v => Aux.HeaterQuantity = v); } }
    public string ThermostatModel { get => Aux.ThermostatModel; set { SetAuxString(nameof(ThermostatModel), value, v => Aux.ThermostatModel = v); } }
    public bool MeterHeatingEnabled { get => Aux.MeterHeatingEnabled; set { if (Aux.MeterHeatingEnabled != value) { Aux.MeterHeatingEnabled = value; OnPropertyChanged(); Recalculate(); } } }

    public bool VentilationEnabled { get => Aux.VentilationEnabled; set { if (Aux.VentilationEnabled != value) { Aux.VentilationEnabled = value; OnPropertyChanged(); Recalculate(); } } }
    public string VentilationBreakerManufacturer { get => Aux.VentilationBreakerManufacturer; set { SetAuxString(nameof(VentilationBreakerManufacturer), value, v => Aux.VentilationBreakerManufacturer = v, RefreshVentilationBreaker); } }
    public string VentilationBreakerModel { get => Aux.VentilationBreakerModel; set { SetAuxString(nameof(VentilationBreakerModel), value, v => Aux.VentilationBreakerModel = v, RefreshVentilationBreakerNominals); } }
    public int VentilationBreakerNominal { get => Aux.VentilationBreakerNominal; set { SetAuxInt(nameof(VentilationBreakerNominal), value, v => Aux.VentilationBreakerNominal = v); } }
    public string FanModel { get => Aux.FanModel; set { SetAuxString(nameof(FanModel), value, v => Aux.FanModel = v); } }
    public int FanQuantity { get => Aux.FanQuantity; set { SetAuxInt(nameof(FanQuantity), Math.Max(0, value), v => Aux.FanQuantity = v); } }

    public bool OpsEnabled { get => Aux.OpsEnabled; set { if (Aux.OpsEnabled != value) { Aux.OpsEnabled = value; OnPropertyChanged(); Recalculate(); } } }
    public string OpsType { get => Aux.OpsType; set { SetAuxString(nameof(OpsType), value, v => Aux.OpsType = v); } }
    public string OpsManufacturer
    {
        get => Aux.OpsManufacturer;
        set
        {
            if (Aux.OpsManufacturer == value)
                return;
            Aux.OpsManufacturer = value;
            Aux.OpsModel = OpsModelOptions(value).FirstOrDefault() ?? "";
            OnPropertyChanged();
            OnPropertyChanged(nameof(OpsModels));
            OnPropertyChanged(nameof(OpsModel));
            Recalculate();
        }
    }
    public string OpsModel { get => Aux.OpsModel; set { SetAuxString(nameof(OpsModel), value, v => Aux.OpsModel = v); } }
    public int OpsLoops { get => Aux.OpsLoops; set { SetAuxInt(nameof(OpsLoops), Math.Max(1, value), v => Aux.OpsLoops = v); } }

    public bool RieseEnabled { get => HasRise; set => HasRise = value; }
    public bool HasRise
    {
        get => Aux.HasRise;
        set
        {
            if (Aux.HasRise == value) return;
            Aux.HasRise = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RieseEnabled));
            Recalculate();
        }
    }
    public string RieseType { get => Aux.RieseType; set { SetAuxString(nameof(RieseType), value, v => Aux.RieseType = v, () => OnPropertyChanged(nameof(RiseType))); } }
    public RiseType RiseType
    {
        get => Aux.RiseType;
        set
        {
            if (Aux.RiseType == value) return;
            Aux.RiseType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RieseType));
            Recalculate();
        }
    }
    public string RieseSupply { get => Aux.RieseSupply; set { SetAuxString(nameof(RieseSupply), value, v => Aux.RieseSupply = v); } }
    public string RieseProtectedCircuits { get => Aux.RieseProtectedCircuits; set { SetAuxString(nameof(RieseProtectedCircuits), value, v => Aux.RieseProtectedCircuits = v); } }
    public int RiesePowerVa { get => Aux.RiesePowerVa; set { SetAuxInt(nameof(RiesePowerVa), Math.Max(0, value), v => Aux.RiesePowerVa = v); } }
    public int RieseAutonomyMinutes { get => Aux.RieseAutonomyMinutes; set { SetAuxInt(nameof(RieseAutonomyMinutes), Math.Max(0, value), v => Aux.RieseAutonomyMinutes = v); } }
    public string RieseProtectionManufacturer { get => Aux.RieseProtectionManufacturer; set { SetAuxString(nameof(RieseProtectionManufacturer), value, v => Aux.RieseProtectionManufacturer = v, RefreshRieseProtection); } }
    public string RieseProtectionModel { get => Aux.RieseProtectionModel; set { SetAuxString(nameof(RieseProtectionModel), value, v => Aux.RieseProtectionModel = v); } }
    public string RieseModuleModel { get => Aux.RieseModuleModel; set { SetAuxString(nameof(RieseModuleModel), value, v => Aux.RieseModuleModel = v); } }
    public string AuxNotes { get => Aux.Notes; set { SetAuxString(nameof(AuxNotes), value, v => Aux.Notes = v); } }

    private AuxiliaryNeedsConfig CreateDefaultAuxiliaryNeeds()
    {
        var auxBreakerManufacturer = DeviceManufacturers("АВ", "ЩСН").FirstOrDefault() ?? "";
        var cabinetManufacturer = DeviceManufacturers("Шкаф собственных нужд", "ЩСН").FirstOrDefault() ?? "";
        var cabinetModel = DefaultDeviceModel("Шкаф собственных нужд", cabinetManufacturer, "ЩСН");
        var mainBreakerModel = DefaultDeviceModel("АВ", auxBreakerManufacturer, "ЩСН", "Ввод ЩСН");
        var lightingBreakerModel = DefaultDeviceModel("АВ", auxBreakerManufacturer, "ЩСН", "Цепь освещения");
        var opsManufacturer = OpsManufacturers.FirstOrDefault() ?? "";

        return new AuxiliaryNeedsConfig
        {
            Enabled = true,
            SupplyFromRunn = true,
            CabinetManufacturer = cabinetManufacturer,
            CabinetModel = cabinetModel,
            MainBreakerManufacturer = auxBreakerManufacturer,
            MainBreakerModel = mainBreakerModel,
            MainBreakerNominal = DefaultDeviceNominal("АВ", auxBreakerManufacturer, mainBreakerModel, 25, "ЩСН", "Ввод ЩСН"),
            LightingEnabled = true,
            LightingCircuits = 1,
            LightingBreakerManufacturer = auxBreakerManufacturer,
            LightingBreakerModel = lightingBreakerModel,
            LightingBreakerNominal = DefaultDeviceNominal("АВ", auxBreakerManufacturer, lightingBreakerModel, 10, "ЩСН", "Цепь освещения"),
            LightingControlMode = "Ручной",
            LightingFixtureModel = DefaultDeviceModel("светильник", "", "Освещение"),
            LightingFixtureQuantity = 2,
            LightingAreas = "РУВН, РУНН, трансформаторный отсек",
            RepairLightingVoltage = 12,
            OutdoorLightingEnabled = false,
            SocketEnabled = true,
            SocketBreakerManufacturer = auxBreakerManufacturer,
            SocketBreakerModel = DefaultDeviceModel("АВ", auxBreakerManufacturer, "ЩСН"),
            SocketBreakerNominal = 16,
            SocketModel = DefaultDeviceModel("розетка", "", "ЩСН"),
            SocketQuantity = 1,
            HeatingEnabled = false,
            HeatingBreakerManufacturer = auxBreakerManufacturer,
            HeatingBreakerModel = DefaultDeviceModel("АВ", auxBreakerManufacturer, "ЩСН"),
            HeatingBreakerNominal = 10,
            HeaterModel = DefaultDeviceModel("обогреватель", "", "ЩСН"),
            HeaterQuantity = 1,
            ThermostatModel = DefaultDeviceModel("термостат", "", "ЩСН"),
            MeterHeatingEnabled = false,
            VentilationEnabled = false,
            VentilationBreakerManufacturer = auxBreakerManufacturer,
            VentilationBreakerModel = DefaultDeviceModel("АВ", auxBreakerManufacturer, "ЩСН"),
            VentilationBreakerNominal = 6,
            FanModel = DefaultDeviceModel("вентилятор", "", "ЩСН"),
            FanQuantity = 1,
            OpsEnabled = false,
            OpsType = OpsTypes.LastOrDefault() ?? "Комбинированная",
            OpsManufacturer = opsManufacturer,
            OpsModel = OpsModelOptions(opsManufacturer).FirstOrDefault() ?? "",
            OpsLoops = 1,
            RieseEnabled = false,
            RieseType = RieseTypes.FirstOrDefault() ?? "",
            RieseSupply = RieseSupplySources.FirstOrDefault() ?? "",
            RieseProtectedCircuits = "освещение, связь",
            RiesePowerVa = 1000,
            RieseAutonomyMinutes = 60,
            RieseProtectionManufacturer = auxBreakerManufacturer,
            RieseProtectionModel = DefaultDeviceModel("АВ", auxBreakerManufacturer, "ЩСН"),
            RieseModuleModel = DefaultDeviceModel("РИСЭ", "", "РИСЭ"),
        };
    }

    private void EnsureAuxiliaryDefaults()
    {
        _cfg.AuxiliaryNeeds ??= new AuxiliaryNeedsConfig();
        if (string.IsNullOrWhiteSpace(Aux.LightingControlMode))
            Aux.LightingControlMode = LightingControlModes.FirstOrDefault() ?? "Ручной";
        if (string.IsNullOrWhiteSpace(Aux.RieseSupply))
            Aux.RieseSupply = RieseSupplySources.FirstOrDefault() ?? "ЩСН";
        if (Aux.LightingCircuits <= 0)
            Aux.LightingCircuits = 1;
        if (string.IsNullOrWhiteSpace(Aux.LightingAreas))
            Aux.LightingAreas = "РУВН, РУНН, трансформаторный отсек";
        if (Aux.RepairLightingVoltage <= 0)
            Aux.RepairLightingVoltage = 12;
        if (Aux.SocketQuantity <= 0)
            Aux.SocketQuantity = 1;
        if (Aux.HeaterQuantity <= 0)
            Aux.HeaterQuantity = 1;
        if (Aux.FanQuantity <= 0)
            Aux.FanQuantity = 1;
        if (string.IsNullOrWhiteSpace(Aux.OpsType))
            Aux.OpsType = OpsTypes.LastOrDefault() ?? "Комбинированная";
        if (string.IsNullOrWhiteSpace(Aux.OpsManufacturer))
            Aux.OpsManufacturer = OpsManufacturers.FirstOrDefault() ?? "";
        if (string.IsNullOrWhiteSpace(Aux.OpsModel))
            Aux.OpsModel = OpsModelOptions(Aux.OpsManufacturer).FirstOrDefault() ?? "";
        if (Aux.OpsLoops <= 0)
            Aux.OpsLoops = 1;
    }

    private IReadOnlyList<string> OpsModelOptions(string manufacturer)
    {
        var fromCatalog = DeviceModelNames("ОПС", manufacturer, "ЩСН").ToList();
        if (fromCatalog.Count > 0)
            return fromCatalog;

        return manufacturer switch
        {
            "Болид" => new[] { "С2000М", "Сигнал-20П", "С2000-КДЛ", "По ТЗ" },
            "Рубеж" => new[] { "Рубеж-2ОП", "Рубеж-4А", "Рубеж-20П", "По ТЗ" },
            "Стрелец" => new[] { "Стрелец-Интеграл", "Стрелец-ПРО", "По ТЗ" },
            "Сибирский Арсенал" => new[] { "Гранит", "Ладога", "По ТЗ" },
            "ИВС-Сигналспецавтоматика" => new[] { "Гранд МАГИСТР", "По ТЗ" },
            _ => new[] { "По ТЗ" },
        };
    }

    private IReadOnlyList<DeviceModel> DeviceModelsFor(string type, string installationArea = "", string purpose = "")
    {
        return _env.Catalog.DeviceModels
            .Where(d => d.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .Where(d => string.IsNullOrWhiteSpace(installationArea)
                || d.InstallationArea.Contains(installationArea, StringComparison.OrdinalIgnoreCase)
                || d.ApplicationRange.Contains(installationArea, StringComparison.OrdinalIgnoreCase))
            .Where(d => string.IsNullOrWhiteSpace(purpose)
                || d.Purpose.Contains(purpose, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private IReadOnlyList<string> DeviceManufacturers(string type, string installationArea = "", string purpose = "")
    {
        return DeviceModelsFor(type, installationArea, purpose)
            .Select(d => d.Manufacturer)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IReadOnlyList<string> DeviceModelNames(string type, string manufacturer = "", string installationArea = "", string purpose = "")
    {
        return DeviceModelsFor(type, installationArea, purpose)
            .Where(d => string.IsNullOrWhiteSpace(manufacturer) || d.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase))
            .Select(CatalogModelName)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IReadOnlyList<int> DeviceNominals(string type, string manufacturer, string model, string installationArea = "", string purpose = "")
    {
        var dm = FindDeviceModel(type, manufacturer, model, installationArea, purpose);
        return dm is not null && dm.Nominals.Count > 0
            ? dm.Nominals
            : LvNominals;
    }

    private string DefaultDeviceModel(string type, string manufacturer = "", string installationArea = "", string purpose = "") =>
        DeviceModelNames(type, manufacturer, installationArea, purpose).FirstOrDefault() ?? "";

    private int DefaultDeviceNominal(string type, string manufacturer, string model, int fallback, string installationArea = "", string purpose = "")
    {
        return DeviceNominals(type, manufacturer, model, installationArea, purpose)
            .Where(n => n > 0)
            .OrderBy(n => n)
            .FirstOrDefault(n => n >= fallback) is var nominal && nominal > 0
            ? nominal
            : fallback;
    }

    private DeviceModel? FindDeviceModel(string type, string manufacturer, string model, string installationArea = "", string purpose = "")
    {
        return DeviceModelsFor(type, installationArea, purpose)
            .FirstOrDefault(d =>
                d.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase)
                && CatalogModelName(d).Equals(model, StringComparison.OrdinalIgnoreCase));
    }

    private static string CatalogModelName(DeviceModel model) =>
        !string.IsNullOrWhiteSpace(model.Model) ? model.Model : model.Series;

    private void SetAuxString(string propertyName, string value, Action<string> assign, Action? after = null)
    {
        var normalized = value ?? "";
        if (propertyName switch
        {
            nameof(AuxCabinetManufacturer) => Aux.CabinetManufacturer == normalized,
            nameof(AuxCabinetModel) => Aux.CabinetModel == normalized,
            nameof(AuxMainBreakerManufacturer) => Aux.MainBreakerManufacturer == normalized,
            nameof(AuxMainBreakerModel) => Aux.MainBreakerModel == normalized,
            nameof(LightingBreakerManufacturer) => Aux.LightingBreakerManufacturer == normalized,
            nameof(LightingBreakerModel) => Aux.LightingBreakerModel == normalized,
            nameof(LightingControlMode) => Aux.LightingControlMode == normalized,
            nameof(LightingFixtureModel) => Aux.LightingFixtureModel == normalized,
            nameof(LightingAreas) => Aux.LightingAreas == normalized,
            nameof(PhotoRelayModel) => Aux.PhotoRelayModel == normalized,
            nameof(AstroTimerModel) => Aux.AstroTimerModel == normalized,
            nameof(TimeRelayModel) => Aux.TimeRelayModel == normalized,
            nameof(SocketBreakerManufacturer) => Aux.SocketBreakerManufacturer == normalized,
            nameof(SocketBreakerModel) => Aux.SocketBreakerModel == normalized,
            nameof(SocketModel) => Aux.SocketModel == normalized,
            nameof(HeatingBreakerManufacturer) => Aux.HeatingBreakerManufacturer == normalized,
            nameof(HeatingBreakerModel) => Aux.HeatingBreakerModel == normalized,
            nameof(HeaterModel) => Aux.HeaterModel == normalized,
            nameof(ThermostatModel) => Aux.ThermostatModel == normalized,
            nameof(VentilationBreakerManufacturer) => Aux.VentilationBreakerManufacturer == normalized,
            nameof(VentilationBreakerModel) => Aux.VentilationBreakerModel == normalized,
            nameof(FanModel) => Aux.FanModel == normalized,
            nameof(RieseType) => Aux.RieseType == normalized,
            nameof(RieseSupply) => Aux.RieseSupply == normalized,
            nameof(RieseProtectedCircuits) => Aux.RieseProtectedCircuits == normalized,
            nameof(RieseProtectionManufacturer) => Aux.RieseProtectionManufacturer == normalized,
            nameof(RieseProtectionModel) => Aux.RieseProtectionModel == normalized,
            nameof(RieseModuleModel) => Aux.RieseModuleModel == normalized,
            nameof(AuxNotes) => Aux.Notes == normalized,
            _ => false,
        }) return;

        assign(normalized);
        OnPropertyChanged(propertyName);
        after?.Invoke();
        Recalculate();
    }

    private void SetAuxInt(string propertyName, int value, Action<int> assign)
    {
        var current = propertyName switch
        {
            nameof(AuxMainBreakerNominal) => Aux.MainBreakerNominal,
            nameof(LightingCircuits) => Aux.LightingCircuits,
            nameof(LightingBreakerNominal) => Aux.LightingBreakerNominal,
            nameof(LightingFixtureQuantity) => Aux.LightingFixtureQuantity,
            nameof(RepairLightingVoltage) => Aux.RepairLightingVoltage,
            nameof(SocketBreakerNominal) => Aux.SocketBreakerNominal,
            nameof(SocketQuantity) => Aux.SocketQuantity,
            nameof(HeatingBreakerNominal) => Aux.HeatingBreakerNominal,
            nameof(HeaterQuantity) => Aux.HeaterQuantity,
            nameof(VentilationBreakerNominal) => Aux.VentilationBreakerNominal,
            nameof(FanQuantity) => Aux.FanQuantity,
            nameof(RiesePowerVa) => Aux.RiesePowerVa,
            nameof(RieseAutonomyMinutes) => Aux.RieseAutonomyMinutes,
            _ => value,
        };
        if (current == value) return;

        assign(value);
        OnPropertyChanged(propertyName);
        Recalculate();
    }

    private void RefreshAuxCabinet()
    {
        Aux.CabinetModel = DefaultDeviceModel("Шкаф собственных нужд", Aux.CabinetManufacturer, "ЩСН");
        OnPropertyChanged(nameof(AuxCabinetModels));
        OnPropertyChanged(nameof(AuxCabinetModel));
    }

    private void RefreshAuxMainBreaker()
    {
        Aux.MainBreakerModel = DefaultDeviceModel("АВ", Aux.MainBreakerManufacturer, "ЩСН", "Ввод ЩСН");
        Aux.MainBreakerNominal = DefaultDeviceNominal("АВ", Aux.MainBreakerManufacturer, Aux.MainBreakerModel, 25, "ЩСН", "Ввод ЩСН");
        OnPropertyChanged(nameof(AuxMainBreakerModels));
        OnPropertyChanged(nameof(AuxMainBreakerModel));
        RefreshAuxMainBreakerNominals();
    }

    private void RefreshAuxMainBreakerNominals()
    {
        OnPropertyChanged(nameof(AuxMainBreakerNominals));
        OnPropertyChanged(nameof(AuxMainBreakerNominal));
    }

    private void RefreshLightingBreaker()
    {
        Aux.LightingBreakerModel = DefaultDeviceModel("АВ", Aux.LightingBreakerManufacturer, "ЩСН", "Цепь освещения");
        Aux.LightingBreakerNominal = DefaultDeviceNominal("АВ", Aux.LightingBreakerManufacturer, Aux.LightingBreakerModel, 10, "ЩСН", "Цепь освещения");
        OnPropertyChanged(nameof(LightingBreakerModels));
        OnPropertyChanged(nameof(LightingBreakerModel));
        RefreshLightingBreakerNominals();
    }

    private void RefreshLightingBreakerNominals()
    {
        OnPropertyChanged(nameof(LightingBreakerNominals));
        OnPropertyChanged(nameof(LightingBreakerNominal));
    }

    private void RefreshSocketBreaker()
    {
        Aux.SocketBreakerModel = DefaultDeviceModel("АВ", Aux.SocketBreakerManufacturer, "ЩСН");
        Aux.SocketBreakerNominal = DefaultDeviceNominal("АВ", Aux.SocketBreakerManufacturer, Aux.SocketBreakerModel, 16, "ЩСН");
        OnPropertyChanged(nameof(SocketBreakerModels));
        OnPropertyChanged(nameof(SocketBreakerModel));
        RefreshSocketBreakerNominals();
    }

    private void RefreshSocketBreakerNominals()
    {
        OnPropertyChanged(nameof(SocketBreakerNominals));
        OnPropertyChanged(nameof(SocketBreakerNominal));
    }

    private void RefreshHeatingBreaker()
    {
        Aux.HeatingBreakerModel = DefaultDeviceModel("АВ", Aux.HeatingBreakerManufacturer, "ЩСН");
        Aux.HeatingBreakerNominal = DefaultDeviceNominal("АВ", Aux.HeatingBreakerManufacturer, Aux.HeatingBreakerModel, 10, "ЩСН");
        OnPropertyChanged(nameof(HeatingBreakerModels));
        OnPropertyChanged(nameof(HeatingBreakerModel));
        RefreshHeatingBreakerNominals();
    }

    private void RefreshHeatingBreakerNominals()
    {
        OnPropertyChanged(nameof(HeatingBreakerNominals));
        OnPropertyChanged(nameof(HeatingBreakerNominal));
    }

    private void RefreshVentilationBreaker()
    {
        Aux.VentilationBreakerModel = DefaultDeviceModel("АВ", Aux.VentilationBreakerManufacturer, "ЩСН");
        Aux.VentilationBreakerNominal = DefaultDeviceNominal("АВ", Aux.VentilationBreakerManufacturer, Aux.VentilationBreakerModel, 6, "ЩСН");
        OnPropertyChanged(nameof(VentilationBreakerModels));
        OnPropertyChanged(nameof(VentilationBreakerModel));
        RefreshVentilationBreakerNominals();
    }

    private void RefreshVentilationBreakerNominals()
    {
        OnPropertyChanged(nameof(VentilationBreakerNominals));
        OnPropertyChanged(nameof(VentilationBreakerNominal));
    }

    private void RefreshRieseProtection()
    {
        Aux.RieseProtectionModel = DefaultDeviceModel("АВ", Aux.RieseProtectionManufacturer, "ЩСН");
        OnPropertyChanged(nameof(RieseProtectionModels));
        OnPropertyChanged(nameof(RieseProtectionModel));
    }

    private void SyncOutgoingFeeders(bool recalculate = true)
    {
        _cfg.OutgoingFeeders ??= new List<OutgoingFeederConfig>();
        _cfg.AvQty = Math.Clamp(_cfg.AvQty, 0, 20);
        _cfg.RpsQty = Math.Clamp(_cfg.RpsQty, 0, 8);
        var existing = _cfg.OutgoingFeeders;
        var next = new List<OutgoingFeederConfig>();

        AddFeeders("АВ", _cfg.AvOn ? _cfg.AvQty : 0, _cfg.AvBrand, _env.Catalog.Options.AvManufacturers);
        AddFeeders("РПС", _cfg.RpsOn ? _cfg.RpsQty : 0, _cfg.RpsBrand, _env.Catalog.Options.RpsManufacturers);

        _cfg.OutgoingFeeders = next;
        OutgoingFeeders.Clear();
        foreach (var feeder in _cfg.OutgoingFeeders)
            OutgoingFeeders.Add(new OutgoingFeederViewModel(this, feeder));
        OnPropertyChanged(nameof(OutgoingFeederCount));

        if (recalculate && !_suspendRecalc)
            Recalculate();

        void AddFeeders(string deviceType, int count, string defaultManufacturer, IReadOnlyList<string> manufacturers)
        {
            count = Math.Max(0, count);
            var fallbackManufacturer = string.IsNullOrWhiteSpace(defaultManufacturer)
                ? manufacturers.FirstOrDefault() ?? ""
                : defaultManufacturer;

            for (var number = 1; number <= count; number++)
            {
                var feeder = existing.FirstOrDefault(f => f.DeviceType == deviceType && f.Number == number) ?? new OutgoingFeederConfig();
                feeder.DeviceType = deviceType;
                feeder.Number = number;
                if (string.IsNullOrWhiteSpace(feeder.Manufacturer))
                    feeder.Manufacturer = fallbackManufacturer;
                if (feeder.Nominal <= 0)
                    feeder.Nominal = deviceType == "РПС" ? 400 : 630;
                if (string.IsNullOrWhiteSpace(feeder.Model))
                    feeder.Model = DefaultFeederModel(deviceType, feeder.Manufacturer);
                if (string.IsNullOrWhiteSpace(feeder.TtRatio))
                    feeder.TtRatio = SuggestedCtRatio(feeder.Nominal);
                NormalizeFeederMetering(feeder);
                next.Add(feeder);
            }
        }
    }

    private static void NormalizeFeederMetering(OutgoingFeederConfig feeder)
    {
        if (string.IsNullOrWhiteSpace(feeder.MeteringType))
            feeder.MeteringType = feeder.HasMeter ? "Коммерческий" : "Нет";

        feeder.HasMeter = !feeder.MeteringType.Equals("Нет", StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureInputMeterCt(bool forceRatio)
    {
        if (!_cfg.HasMeter)
            return;

        if (!_cfg.HasCt)
        {
            _cfg.HasCt = true;
            OnPropertyChanged(nameof(HasCt));
        }

        if (forceRatio || string.IsNullOrWhiteSpace(_cfg.CtRatio))
        {
            _cfg.CtRatio = SuggestedCtRatio(InputNominalFromConfig());
            OnPropertyChanged(nameof(CtRatio));
        }
    }

    private void NormalizeInputMeterCtState()
    {
        if (_cfg.HasMeter)
        {
            EnsureInputMeterCt(forceRatio: false);
        }
        else if (_cfg.HasCt)
        {
            _cfg.HasCt = false;
            OnPropertyChanged(nameof(HasCt));
        }
    }

    private int InputNominalFromConfig() =>
        Math.Max(_cfg.PvrOn ? _cfg.PvrNominal : 0,
            Math.Max(_cfg.ReOn ? _cfg.ReNominal : 0,
                _cfg.AvInOn ? _cfg.AvInNominal : 0));

    public IReadOnlyList<string> ManufacturersForFeeder(string deviceType) =>
        deviceType == "РПС" ? RpsManufacturers : AvManufacturers;

    public string DefaultFeederModel(string deviceType, string manufacturer)
    {
        return ModelsForFeeder(deviceType, manufacturer).FirstOrDefault() ?? "";
    }

    public IReadOnlyList<string> ModelsForFeeder(string deviceType, string manufacturer)
    {
        var models = _env.Catalog.DeviceModels
            .Where(a => a.Type == deviceType && a.Manufacturer == manufacturer)
            .Select(CatalogModelName)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (models.Count == 0)
        {
            models = _env.Catalog.Apparatus
                .Where(a =>
                a.Manufacturer == manufacturer
                && (deviceType == "РПС"
                    ? a.Type.Contains("РПС", StringComparison.OrdinalIgnoreCase)
                    : a.Type.Contains("АВ", StringComparison.OrdinalIgnoreCase)))
                .SelectMany(a => a.Series.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        return models;
    }

    public IReadOnlyList<int> NominalsForFeeder(string deviceType, string manufacturer, string model)
    {
        var dm = _env.Catalog.DeviceModels.FirstOrDefault(a =>
            a.Type == deviceType
            && a.Manufacturer == manufacturer
            && CatalogModelName(a).Equals(model, StringComparison.OrdinalIgnoreCase));
        if (dm != null && dm.Nominals.Count > 0)
            return dm.Nominals;
        return LvNominals;
    }

    public DeviceModel? GetDeviceModel(string deviceType, string manufacturer, string model)
    {
        return _env.Catalog.DeviceModels.FirstOrDefault(a =>
            a.Type == deviceType
            && a.Manufacturer == manufacturer
            && CatalogModelName(a).Equals(model, StringComparison.OrdinalIgnoreCase));
    }

    public string SuggestedCtRatio(int nominal)
    {
        if (TtRatios.Count == 0)
            return "";
        if (nominal <= 0)
            return TtRatios.First();

        return TtRatios
            .Select(ratio => new { Ratio = ratio, Primary = TryParseRatioNominal(ratio, out var ratioNominal) ? ratioNominal : 0 })
            .Where(x => x.Primary > 0)
            .OrderBy(x => Math.Abs(x.Primary - nominal))
            .ThenBy(x => x.Primary < nominal ? 1 : 0)
            .Select(x => x.Ratio)
            .FirstOrDefault() ?? TtRatios.Last();
    }

    internal void OutgoingFeederChanged()
    {
        Recalculate();
    }

    internal void RuvnEquipmentChanged()
    {
        Recalculate();
    }

    private static bool TryParseRatioNominal(string ratio, out int nominal)
    {
        var slash = ratio.IndexOf('/');
        var value = slash >= 0 ? ratio[..slash] : ratio;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out nominal);
    }

    // ===== Ручные переопределения (строки, чтобы допускать пустое значение) =====
    public string ManualLengthText { get => ToText(_cfg.ManualLength); set { _cfg.ManualLength = ParseNullable(value); OnPropertyChanged(); Recalculate(); } }
    public string ManualWidthText { get => ToText(_cfg.ManualWidth); set { _cfg.ManualWidth = ParseNullable(value); OnPropertyChanged(); Recalculate(); } }
    public string ManualHeightText { get => ToText(_cfg.ManualHeight); set { _cfg.ManualHeight = ParseNullable(value); OnPropertyChanged(); Recalculate(); } }
    public string ManualBaseMassText { get => ToText(_cfg.ManualBaseMass); set { _cfg.ManualBaseMass = ParseNullable(value); OnPropertyChanged(); Recalculate(); } }
    public string ManualBodyMassText { get => ToText(_cfg.ManualBodyMass); set { _cfg.ManualBodyMass = ParseNullable(value); OnPropertyChanged(); Recalculate(); } }
    public string ManualGrossMassText { get => ToText(_cfg.ManualGrossMass); set { _cfg.ManualGrossMass = ParseNullable(value); OnPropertyChanged(); Recalculate(); } }

    private static string ToText(double? v) => v.HasValue ? v.Value.ToString("0.###", CultureInfo.CurrentCulture) : "";
    private static double? ParseNullable(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Replace(',', '.');
        return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    // ===== Результат: автозаполнение трансформатора =====
    private TransformerSpec? Tr => _env.Catalog.GetTransformer(_cfg.Mark);
    public string TrPowerText => Tr is null ? "—" : $"{Tr.PowerKva:0} кВА";
    public string TrLengthText => Tr is null ? "—" : $"{Tr.LengthMm:0} мм";
    public string TrWidthText => Tr is null ? "—" : $"{Tr.WidthMm:0} мм";
    public string TrHeightText => Tr is null ? "—" : $"{Tr.HeightMm:0} мм";
    public string TrMassText => Tr is null ? "—" : $"{Tr.MassKg:0} кг";
    public string TrCurrentText => Tr is null ? "—" : $"{Tr.RatedCurrentA:0} А";

    // ===== Результат: габариты и массы =====
    public string LengthCalcText => $"{_res.LengthCalc:0}";
    public string LengthFinalText => $"{_res.LengthFinal:0}";
    public string WidthCalcText => $"{_res.WidthCalc:0}";
    public string WidthFinalText => $"{_res.WidthFinal:0}";
    public string HeightCalcText => $"{_res.HeightCalc:0}";
    public string HeightFinalText => $"{_res.HeightFinal:0}";
    public string BaseMassCalcText => $"{_res.BaseMassCalc:0}";
    public string BaseMassText => $"{_res.BaseMass:0}";
    public string BodyMassCalcText => $"{_res.BodyMassCalc:0}";
    public string BodyMassText => $"{_res.BodyMass:0}";
    public string GrossMassCalcText => $"{_res.GrossMassCalc:0}";
    public string GrossMassText => $"{_res.GrossMass:0}";
    public string GrossMassEstimatedText => $"{_res.GrossMassEstimated:0}";
    public string BusbarHv => _res.BusbarHv;
    public string BusbarLv => _res.BusbarLv;
    public string BusbarN => _res.BusbarN;
    public string BusbarPe => _res.BusbarPe;
    public string FinalDimensionsSummary => $"{_res.LengthFinal:0} x {_res.WidthFinal:0} x {_res.HeightFinal:0} мм";
    public string GrossMassSummary => $"{_res.GrossMass:0} кг";
    public string MassBreakdownSummary =>
        $"ТМГ {Tr?.MassKg ?? 0:0} кг + основание {_res.BaseMass:0} кг + корпус {_res.BodyMass:0} кг = {_res.GrossMass:0} кг";
    public string AdditionalMassSummary =>
        $"Предварительный довес: аппараты {_res.EquipmentMassEstimate:0} кг + шины {_res.BusbarMassEstimate:0} кг + двери {_res.DoorMassEstimate:0} кг + ЩСН {_res.AuxiliaryMassEstimate:0} кг + опции корпуса {_res.EnclosureOptionMassEstimate:0} кг = {_res.AdditionalMassEstimate:0} кг";
    public string GrossMassWithOptionsSummary =>
        $"Ориентировочная масса с оборудованием: {_res.GrossMassEstimated:0} кг";
    public string MassMethodologySummary
    {
        get
        {
            var m = _env.Catalog.Options.Methodology;
            return $"Основание: швеллер x {m.FrameCoef:0.##} + пол {m.FloorSheetKgPerM2:0.##} кг/м2; корпус: стены и крыша x лист x {m.BodyWasteCoef:0.##}.";
        }
    }
    public string MassScopeWarning =>
        "Ориентировочный довес не заменяет взвешивание и уточнение по паспортам; кабели, крепеж и упаковка остаются расчетным запасом.";
    public string TransformerResultSummary => Tr is null
        ? "ТМГ не выбран"
        : $"{Tr.PowerKva:0} кВА; ток ТМГ {_res.RatedCurrentA:0} А";
    public string InputResultSummary => _res.InputNominal > 0
        ? $"{_res.InputNominal:0} А"
        : "не задан";
    public string FeedersResultSummary
    {
        get
        {
            var feeders = _cfg.OutgoingFeeders ?? new List<OutgoingFeederConfig>();
            var metered = feeders.Count(HasFeederMetering);
            var reserve = feeders.Count(f => f.IsReserve);
            return $"{feeders.Count} шт.; учет {metered}; резерв {reserve}";
        }
    }
    public string BusbarResultSummary =>
        $"РУВН {BusbarDescription(_cfg.BusbarHvMaterial, _res.BusbarHv)}; РУНН {BusbarDescription(_cfg.BusbarLvMaterial, _res.BusbarLv)}; N {BusbarDescription(_cfg.BusbarNMaterial, _res.BusbarN)}; PE/PEN {ValueOrDash(_res.BusbarPe)}";
    public string ColorZonesSummary =>
        $"корпус {ValueOrDash(_cfg.BodyColor)}; двери {ValueOrDash(_cfg.DoorColor)}; крыша {ValueOrDash(_cfg.RoofColor)}; основание/цоколь {ValueOrDash(_cfg.BaseColor)}; внутренние панели {ValueOrDash(_cfg.InternalPanelColor)}; логотип {ValueOrDash(_cfg.LogoColor)}";
    public string EnclosureDetailsSummary =>
        string.Join("; ", new[]
        {
            _cfg.HasDoorCanopies ? "козырьки" : "",
            _cfg.HasDoorSeals ? "уплотнения дверей" : "",
            _cfg.HasTransformerMeshDoors ? "сетчатые двери ТМГ" : "",
            _cfg.HasLouverAnimalProtection ? "защита жалюзи" : "",
            _cfg.HasAntiVandalHinges ? "антивандальные петли" : "",
            _cfg.HasDoorSealing ? "пломбировка дверей" : "",
            _cfg.HasServicePlatform ? "площадка обслуживания" : "",
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
    public string MarkingSummary =>
        string.Join("; ", new[]
        {
            _cfg.HasLogo ? $"логотип: {ValueOrDash(_cfg.LogoPlacement)}" : "логотип не предусмотрен",
            _cfg.HasWarningLabels ? "предупреждающие надписи" : "",
            _cfg.HasDispatcherNameplate ? "диспетчерское наименование" : "",
            _cfg.HasFeederLabels ? "пофидерная маркировка" : "",
            ValueOrDash(_cfg.MarkingNotes) == "-" ? "" : _cfg.MarkingNotes.Trim(),
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
    public string EquipmentTrustSummary
    {
        get
        {
            var items = SpecificationBuilder.GenerateSpecification(_cfg, _res, _env.Catalog);
            if (items.Count == 0)
                return "Позиций спецификации нет";

            var verified = items.Count(i => IsSourceConfidence(i, "verified"));
            var needsVerification = items.Count(i => IsSourceConfidence(i, "needsVerification"));
            var projectInput = items.Count(i => IsSourceConfidence(i, "userInput") || i.Source.Equals("project", StringComparison.OrdinalIgnoreCase));
            var unknown = items.Count - verified - needsVerification - projectInput;
            var parts = new List<string>
            {
                $"позиций спецификации: {items.Count}",
                $"проверено в базе: {verified}",
                $"требуют уточнения: {needsVerification}",
                $"задано проектом: {projectInput}",
            };
            if (unknown > 0)
                parts.Add($"без статуса: {unknown}");
            return string.Join("; ", parts);
        }
    }

    private static bool IsSourceConfidence(SpecificationItem item, string value) =>
        item.SourceConfidence.Equals(value, StringComparison.OrdinalIgnoreCase);
    public string ManualOverrideSummary
    {
        get
        {
            var count = new[]
            {
                _cfg.ManualLength, _cfg.ManualWidth, _cfg.ManualHeight,
                _cfg.ManualBaseMass, _cfg.ManualBodyMass, _cfg.ManualGrossMass,
            }.Count(v => v.HasValue);
            return count == 0 ? "ручных правок нет" : $"ручных правок: {count}";
        }
    }

    // ===== Валидация =====
    public ObservableCollection<ValidationMessage> ValidationMessages { get; }
    public ObservableCollection<ValidationMessage> CustomerRequirementMessages { get; }
    public bool IsValid => !_res.HasErrors;
    public int ErrorCount => ValidationMessages.Count(m => m.Severity == Severity.Error);
    public int WarningCount => ValidationMessages.Count(m => m.Severity == Severity.Warning);
    public int InfoCount => ValidationMessages.Count(m => m.Severity == Severity.Info);
    public bool HasValidationMessages => ErrorCount > 0 || WarningCount > 0;
    public bool HasErrorMessages => ErrorCount > 0;
    public bool HasWarningMessages => WarningCount > 0;
    public bool HasInfoMessages => HasValidationMessages && InfoCount > 0;
    public IReadOnlyList<ValidationMessage> ErrorMessages => ValidationMessages.Where(m => m.Severity == Severity.Error).ToList();
    public IReadOnlyList<ValidationMessage> WarningMessages => ValidationMessages.Where(m => m.Severity == Severity.Warning).ToList();
    public IReadOnlyList<ValidationMessage> InfoMessages => ValidationMessages.Where(m => m.Severity == Severity.Info).ToList();
    public bool HasCustomerRequirementMessages => CustomerRequirementMessages.Count > 0;
    public int CustomerRequirementWarningCount => CustomerRequirementMessages.Count(m => m.Severity == Severity.Warning);
    public IReadOnlyList<ValidationMessage> CustomerRequirements => CustomerRequirementMessages.ToList();
    public string ValidationSummaryText
    {
        get
        {
            if (!HasValidationMessages)
                return "";

            var parts = new List<string>();
            if (ErrorCount > 0)
                parts.Add($"Ошибки: {ErrorCount}");
            if (WarningCount > 0)
                parts.Add($"Предупреждения: {WarningCount}");
            if (InfoCount > 0)
                parts.Add($"Информация: {InfoCount}");
            return string.Join("   ", parts);
        }
    }
    public bool CanReleaseDocuments => !_res.HasErrors || _cfg.ErrorsAcceptedForWork;
    public string ExportReadinessText => _res.HasErrors
        ? (_cfg.ErrorsAcceptedForWork ? "Выпуск разрешён с согласованными ошибками" : "Выпуск заблокирован")
        : "Выпуск доступен";
    public string StatusText => _res.HasErrors
        ? (_cfg.ErrorsAcceptedForWork ? "⚠ Ошибки согласованы, выпуск разрешён" : "🛑 ЕСТЬ ОШИБКИ ПРОЕКТИРОВАНИЯ")
        : (ValidationMessages.Any(m => m.Severity == Severity.Warning) ? "⚠ Есть предупреждения" : "✅ Проверки пройдены");
    public string StatusColor => _res.HasErrors
        ? (_cfg.ErrorsAcceptedForWork ? "#8A743F" : "#8A5F5F")
        : (ValidationMessages.Any(m => m.Severity == Severity.Warning) ? "#8A743F" : "#607D68");

    private static string ValueOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private static string BusbarDescription(string material, string section)
    {
        var mat = ValueOrDash(material);
        var sec = ValueOrDash(section);
        if (mat == "-" && sec == "-")
            return "-";
        if (mat == "-")
            return sec;
        if (sec == "-")
            return mat;
        return $"{mat} {sec}";
    }

    private static bool HasFeederMetering(OutgoingFeederConfig feeder)
    {
        var type = string.IsNullOrWhiteSpace(feeder.MeteringType)
            ? (feeder.HasMeter ? "Коммерческий" : "Нет")
            : feeder.MeteringType.Trim();
        return feeder.HasMeter || !type.Equals("Нет", StringComparison.OrdinalIgnoreCase);
    }

    // ===== Превью документов =====
    public string PassportPreview { get; private set; } = "";
    public string OrderPreview { get; private set; } = "";
    public string ChecklistPreview { get; private set; } = "";
    public string SpecificationPreview { get; private set; } = "";

    private List<GeneratedDocument> BuildDocuments()
    {
        return new List<GeneratedDocument>
        {
            DocumentBuilder.BuildProductionOrder(_cfg, _res, _env.Catalog, _env.Templates),
            DocumentBuilder.BuildPassport(_cfg, _res, _env.Catalog),
            DocumentBuilder.BuildChecklist(_cfg, _res, _env.Catalog, _env.Templates),
            DocumentBuilder.BuildSpecification(_cfg, _res, _env.Catalog),
        };
    }

    public void Recalculate()
    {
        if (_suspendRecalc) return;
        _res = CalculationEngine.Calculate(_cfg, _env.Catalog);

        ValidationMessages.Clear();
        foreach (var msg in _res.Messages)
            ValidationMessages.Add(msg);
        CustomerRequirementMessages.Clear();
        foreach (var msg in CustomerRequirementEngine.Build(_cfg, _env.Catalog))
            CustomerRequirementMessages.Add(msg);

        if (_res.HasErrors && !_cfg.ErrorsAcceptedForWork)
        {
            var msg = "ДОКУМЕНТ ЗАБЛОКИРОВАН: исправьте ошибки проектирования.";
            OrderPreview = msg;
            PassportPreview = msg;
            ChecklistPreview = msg;
            SpecificationPreview = msg;
        }
        else
        {
            var order = DocumentBuilder.BuildProductionOrder(_cfg, _res, _env.Catalog, _env.Templates);
            var passport = DocumentBuilder.BuildPassport(_cfg, _res, _env.Catalog);
            var checklist = DocumentBuilder.BuildChecklist(_cfg, _res, _env.Catalog, _env.Templates);
            var specification = DocumentBuilder.BuildSpecification(_cfg, _res, _env.Catalog);
            OrderPreview = order.ToPlainText();
            PassportPreview = passport.ToPlainText();
            ChecklistPreview = checklist.ToPlainText();
            SpecificationPreview = specification.ToPlainText();
        }

        RaiseOutputs();
    }

    private void RaiseOutputs()
    {
        foreach (var name in new[]
        {
            nameof(TrPowerText), nameof(TrLengthText), nameof(TrWidthText), nameof(TrHeightText),
            nameof(TrMassText), nameof(TrCurrentText),
            nameof(LengthCalcText), nameof(LengthFinalText), nameof(WidthCalcText), nameof(WidthFinalText),
            nameof(HeightCalcText), nameof(HeightFinalText),
            nameof(BaseMassCalcText), nameof(BaseMassText),
            nameof(BodyMassCalcText), nameof(BodyMassText),
            nameof(GrossMassCalcText), nameof(GrossMassText), nameof(GrossMassEstimatedText),
            nameof(MassBreakdownSummary), nameof(AdditionalMassSummary), nameof(GrossMassWithOptionsSummary),
            nameof(MassMethodologySummary), nameof(MassScopeWarning),
            nameof(BusbarHv), nameof(BusbarLv), nameof(BusbarN), nameof(BusbarPe),
            nameof(FinalDimensionsSummary), nameof(GrossMassSummary), nameof(TransformerResultSummary),
            nameof(InputResultSummary), nameof(FeedersResultSummary), nameof(BusbarResultSummary),
            nameof(ColorZonesSummary), nameof(EnclosureDetailsSummary), nameof(MarkingSummary),
            nameof(EquipmentTrustSummary),
            nameof(ManualOverrideSummary),
            nameof(CustomerProfileSummary), nameof(CustomerProfileAppliedSettings), nameof(CustomerProfileManualChecks),
            nameof(RuvnRecommendedFuseNominal), nameof(RuvnRecommendedSurgeArresterVoltage),
            nameof(IsRuvnEnabled), nameof(IsRuvnPassThrough),
            nameof(IsRuvnIncomingVacuum), nameof(IsRuvnOutgoingVacuum), nameof(IsRuvnTransformerVacuum),
            nameof(IsRuvnIncomingVacuumActive), nameof(IsRuvnOutgoingVacuumActive), nameof(IsRuvnTransformerVacuumActive),
            nameof(HasRuvnVacuumBranches), nameof(ShowRuvnRecommendedFuse),
            nameof(IsRuvnIncomingFuseEnabled), nameof(IsRuvnOutgoingFuseEnabled), nameof(IsRuvnTransformerFuseEnabled),
            nameof(CurrentConfig), nameof(CurrentResult),
            nameof(IsValid), nameof(CanReleaseDocuments), nameof(ErrorsAcceptedForWork),
            nameof(ErrorCount), nameof(WarningCount), nameof(InfoCount),
            nameof(HasValidationMessages), nameof(HasErrorMessages), nameof(HasWarningMessages), nameof(HasInfoMessages),
            nameof(ErrorMessages), nameof(WarningMessages), nameof(InfoMessages),
            nameof(HasCustomerRequirementMessages), nameof(CustomerRequirementWarningCount), nameof(CustomerRequirements),
            nameof(ValidationSummaryText), nameof(ExportReadinessText), nameof(StatusText), nameof(StatusColor),
            nameof(PassportPreview), nameof(OrderPreview), nameof(ChecklistPreview), nameof(SpecificationPreview),
        })
            OnPropertyChanged(name);

        ExportExcelCommand.RaiseCanExecuteChanged();
        ExportPdfCommand.RaiseCanExecuteChanged();
    }

    // ===== Команды =====
    public Func<string?>? AskSavePath { get; set; }
    public Func<string?>? AskOpenPath { get; set; }
    public Func<string, string?>? AskExportPath { get; set; }
    public Action<string>? Notify { get; set; }

    private void NewProject()
    {
        _suspendRecalc = true;
        _cfg = CreateDefaultConfig();
        EnsureRuvnDefaults();
        RefreshRuvnEquipmentViewModels();
        EnsureAuxiliaryDefaults();
        ApplyGridCompanyProfile(notify: false);
        Marks.Clear();
        foreach (var m in _env.Catalog.MarksFor(_cfg.Manufacturer)) Marks.Add(m);
        _suspendRecalc = false;
        SyncOutgoingFeeders(recalculate: false);
        OnPropertyChanged(string.Empty); // refresh all inputs
        Recalculate();
        Notify?.Invoke("Создан новый проект.");
    }

    private void SaveProject()
    {
        var path = AskSavePath?.Invoke();
        if (string.IsNullOrEmpty(path)) return;
        ProjectStorage.Save(_cfg, path);
        Notify?.Invoke($"Проект сохранён: {path}");
    }

    private void OpenProject()
    {
        var path = AskOpenPath?.Invoke();
        if (string.IsNullOrEmpty(path)) return;
        _suspendRecalc = true;
        _cfg = ProjectStorage.Load(path);
        EnsureRuvnDefaults();
        RefreshRuvnEquipmentViewModels();
        NormalizeInputMeterCtState();
        EnsureAuxiliaryDefaults();
        Marks.Clear();
        foreach (var m in _env.Catalog.MarksFor(_cfg.Manufacturer)) Marks.Add(m);
        _suspendRecalc = false;
        SyncOutgoingFeeders(recalculate: false);
        OnPropertyChanged(string.Empty);
        Recalculate();
        Notify?.Invoke($"Проект загружен: {path}");
    }

    private bool CanExportDocuments() => CanReleaseDocuments;

    private bool EnsureCanExportDocuments()
    {
        if (CanExportDocuments())
            return true;

        Notify?.Invoke("Экспорт заблокирован: исправьте ошибки или отметьте, что ошибки согласованы для выпуска.");
        return false;
    }

    private void ExportExcel()
    {
        if (!EnsureCanExportDocuments()) return;
        var path = AskExportPath?.Invoke("xlsx");
        if (string.IsNullOrEmpty(path)) return;
        ExcelExporter.Export(path, _cfg, _res, BuildDocuments(), _env.Catalog);
        Notify?.Invoke($"Документы выгружены в Excel: {path}");
    }

    private void ExportPdf()
    {
        if (!EnsureCanExportDocuments()) return;
        var path = AskExportPath?.Invoke("pdf");
        if (string.IsNullOrEmpty(path)) return;
        PdfExporter.Export(path, BuildDocuments(), _cfg, _res, _env.Catalog);
        Notify?.Invoke($"Документы выгружены в PDF: {path}");
    }

    public ProjectConfig CurrentConfig => _cfg;
    public CalculationResult CurrentResult => _res;
    public CatalogStore Catalog => _env.Catalog;
}

public sealed class RuvnBranchEquipmentViewModel : ObservableObject
{
    private readonly MainViewModel _owner;
    private readonly RuvnBranchEquipmentConfig _model;

    public RuvnBranchEquipmentViewModel(MainViewModel owner, RuvnBranchEquipmentConfig model)
    {
        _owner = owner;
        _model = model;
    }

    public IReadOnlyList<string> VisibleBreakOptions => _owner.RuvnVisibleBreakOptions;
    public IReadOnlyList<string> EarthingSwitchOptions => _owner.RuvnEarthingSwitchOptions;
    public IReadOnlyList<string> VacuumBreakerModels => _owner.VacuumBreakerModels;
    public IReadOnlyList<int> VacuumBreakerNominals => _owner.VacuumBreakerNominals;
    public IReadOnlyList<double> VacuumBreakerBreakingCurrentsKa => _owner.VacuumBreakerBreakingCurrentsKa;
    public IReadOnlyList<string> VacuumBreakerDrives => _owner.VacuumBreakerDrives;
    public IReadOnlyList<string> VacuumBreakerInstallations => _owner.VacuumBreakerInstallations;
    public IReadOnlyList<string> OperationalPowerOptions => _owner.OperationalPowerOptions;
    public IReadOnlyList<string> RzaTerminals => _owner.RzaTerminals;
    public IReadOnlyList<string> TtRatios => _owner.TtRatios;

    public string VisibleBreakBefore { get => _model.VisibleBreakBefore; set { if (_model.VisibleBreakBefore != value) { _model.VisibleBreakBefore = value; Changed(); } } }
    public string VisibleBreakAfter { get => _model.VisibleBreakAfter; set { if (_model.VisibleBreakAfter != value) { _model.VisibleBreakAfter = value; Changed(); } } }
    public string EarthingSwitch { get => _model.EarthingSwitch; set { if (_model.EarthingSwitch != value) { _model.EarthingSwitch = value; Changed(); } } }
    public string VacuumBreakerModel { get => _model.VacuumBreakerModel; set { if (_model.VacuumBreakerModel != value) { _model.VacuumBreakerModel = value; Changed(); } } }
    public int VacuumBreakerNominal { get => _model.VacuumBreakerNominal; set { var normalized = Math.Max(0, value); if (_model.VacuumBreakerNominal != normalized) { _model.VacuumBreakerNominal = normalized; Changed(); } } }
    public double VacuumBreakerBreakingCurrentKa { get => _model.VacuumBreakerBreakingCurrentKa; set { if (Math.Abs(_model.VacuumBreakerBreakingCurrentKa - value) > 0.001) { _model.VacuumBreakerBreakingCurrentKa = value; Changed(); } } }
    public string VacuumBreakerDrive { get => _model.VacuumBreakerDrive; set { if (_model.VacuumBreakerDrive != value) { _model.VacuumBreakerDrive = value; Changed(); } } }
    public string VacuumBreakerInstallation { get => _model.VacuumBreakerInstallation; set { if (_model.VacuumBreakerInstallation != value) { _model.VacuumBreakerInstallation = value; Changed(); } } }
    public string OperationalPower { get => _model.OperationalPower; set { if (_model.OperationalPower != value) { _model.OperationalPower = value; Changed(); } } }
    public string RzaTerminal { get => _model.RzaTerminal; set { if (_model.RzaTerminal != value) { _model.RzaTerminal = value; Changed(); } } }
    public bool RzaMtz { get => _model.RzaMtz; set { if (_model.RzaMtz != value) { _model.RzaMtz = value; Changed(); } } }
    public bool RzaCurrentCutoff { get => _model.RzaCurrentCutoff; set { if (_model.RzaCurrentCutoff != value) { _model.RzaCurrentCutoff = value; Changed(); } } }
    public bool RzaGroundFault { get => _model.RzaGroundFault; set { if (_model.RzaGroundFault != value) { _model.RzaGroundFault = value; Changed(); } } }
    public bool RzaOverload { get => _model.RzaOverload; set { if (_model.RzaOverload != value) { _model.RzaOverload = value; Changed(); } } }
    public bool RzaUrov { get => _model.RzaUrov; set { if (_model.RzaUrov != value) { _model.RzaUrov = value; Changed(); } } }
    public bool RzaLzsh { get => _model.RzaLzsh; set { if (_model.RzaLzsh != value) { _model.RzaLzsh = value; Changed(); } } }
    public bool RzaApv { get => _model.RzaApv; set { if (_model.RzaApv != value) { _model.RzaApv = value; Changed(); } } }
    public bool RzaAvr { get => _model.RzaAvr; set { if (_model.RzaAvr != value) { _model.RzaAvr = value; Changed(); } } }
    public bool RzaArcProtection { get => _model.RzaArcProtection; set { if (_model.RzaArcProtection != value) { _model.RzaArcProtection = value; Changed(); } } }
    public bool RzaTransformerGas { get => _model.RzaTransformerGas; set { if (_model.RzaTransformerGas != value) { _model.RzaTransformerGas = value; Changed(); } } }
    public string ProtectionCtRatio { get => _model.ProtectionCtRatio; set { if (_model.ProtectionCtRatio != value) { _model.ProtectionCtRatio = value; Changed(); } } }
    public int ProtectionCtQuantity { get => _model.ProtectionCtQuantity; set { var normalized = Math.Max(1, value); if (_model.ProtectionCtQuantity != normalized) { _model.ProtectionCtQuantity = normalized; Changed(); } } }
    public bool HasTtnp
    {
        get => _model.HasTtnp;
        set
        {
            if (_model.HasTtnp == value)
                return;
            _model.HasTtnp = value;
            if (value && string.IsNullOrWhiteSpace(_model.TtnpModel))
                _model.TtnpModel = "ТТНП";
            OnPropertyChanged();
            OnPropertyChanged(nameof(TtnpModel));
            _owner.RuvnEquipmentChanged();
        }
    }
    public string TtnpModel { get => _model.TtnpModel; set { if (_model.TtnpModel != value) { _model.TtnpModel = value; Changed(); } } }
    public bool HasVoltageTransformer
    {
        get => _model.HasVoltageTransformer;
        set
        {
            if (_model.HasVoltageTransformer == value)
                return;
            _model.HasVoltageTransformer = value;
            if (value && string.IsNullOrWhiteSpace(_model.VoltageTransformerModel))
                _model.VoltageTransformerModel = "НАЛИ";
            OnPropertyChanged();
            OnPropertyChanged(nameof(VoltageTransformerModel));
            _owner.RuvnEquipmentChanged();
        }
    }
    public string VoltageTransformerModel { get => _model.VoltageTransformerModel; set { if (_model.VoltageTransformerModel != value) { _model.VoltageTransformerModel = value; Changed(); } } }
    public string Notes { get => _model.Notes; set { if (_model.Notes != value) { _model.Notes = value; Changed(); } } }

    public void Refresh()
    {
        foreach (var name in new[]
        {
            nameof(VisibleBreakBefore), nameof(VisibleBreakAfter), nameof(EarthingSwitch),
            nameof(VacuumBreakerModel), nameof(VacuumBreakerNominal), nameof(VacuumBreakerBreakingCurrentKa),
            nameof(VacuumBreakerDrive), nameof(VacuumBreakerInstallation), nameof(OperationalPower),
            nameof(RzaTerminal), nameof(RzaMtz), nameof(RzaCurrentCutoff), nameof(RzaGroundFault),
            nameof(RzaOverload), nameof(RzaUrov), nameof(RzaLzsh), nameof(RzaApv), nameof(RzaAvr),
            nameof(RzaArcProtection), nameof(RzaTransformerGas), nameof(ProtectionCtRatio),
            nameof(ProtectionCtQuantity), nameof(HasTtnp), nameof(TtnpModel),
            nameof(HasVoltageTransformer), nameof(VoltageTransformerModel), nameof(Notes),
        })
        {
            OnPropertyChanged(name);
        }
    }

    private void Changed([CallerMemberName] string? name = null)
    {
        OnPropertyChanged(name);
        _owner.RuvnEquipmentChanged();
    }
}

public sealed class OutgoingFeederViewModel : ObservableObject
{
    private readonly MainViewModel _owner;
    private readonly OutgoingFeederConfig _model;

    public OutgoingFeederViewModel(MainViewModel owner, OutgoingFeederConfig model)
    {
        _owner = owner;
        _model = model;
        NormalizeMetering();
    }

    public string Title => $"{_model.DeviceType}-{_model.Number}";
    public int Number => _model.Number;
    public string DeviceType => _model.DeviceType;
    public IReadOnlyList<string> MeteringTypes => _owner.FeederMeteringTypes;
    public IReadOnlyList<string> Manufacturers => _owner.ManufacturersForFeeder(_model.DeviceType);
    public IReadOnlyList<string> ModelOptions
    {
        get
        {
            var options = _owner.ModelsForFeeder(_model.DeviceType, _model.Manufacturer).ToList();
            if (!string.IsNullOrWhiteSpace(_model.Model)
                && !options.Contains(_model.Model, StringComparer.OrdinalIgnoreCase))
                options.Insert(0, _model.Model);
            return options;
        }
    }
    public IReadOnlyList<int> Nominals => _owner.NominalsForFeeder(_model.DeviceType, _model.Manufacturer, _model.Model);
    public IReadOnlyList<string> TtRatios => _owner.TtRatios;

    public string Purpose
    {
        get => _model.Purpose;
        set
        {
            if (_model.Purpose == value)
                return;
            _model.Purpose = value;
            OnPropertyChanged();
            _owner.OutgoingFeederChanged();
        }
    }

    public string Manufacturer
    {
        get => _model.Manufacturer;
        set
        {
            if (_model.Manufacturer == value)
                return;
            _model.Manufacturer = value;
            _model.Model = _owner.DefaultFeederModel(_model.DeviceType, value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ModelOptions));
            OnPropertyChanged(nameof(Model));
            OnPropertyChanged(nameof(Nominals));
            _owner.OutgoingFeederChanged();
        }
    }

    public string Model
    {
        get => _model.Model;
        set
        {
            if (_model.Model == value)
                return;
            _model.Model = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Nominals));
            _owner.OutgoingFeederChanged();
        }
    }

    public int Nominal
    {
        get => _model.Nominal;
        set
        {
            if (_model.Nominal == value)
                return;
            _model.Nominal = value;
            if (_model.HasMeter)
            {
                _model.TtRatio = _owner.SuggestedCtRatio(value);
                OnPropertyChanged(nameof(TtRatio));
            }
            OnPropertyChanged();
            _owner.OutgoingFeederChanged();
        }
    }

    public bool IsReserve
    {
        get => _model.IsReserve;
        set
        {
            if (_model.IsReserve == value)
                return;
            _model.IsReserve = value;
            OnPropertyChanged();
            _owner.OutgoingFeederChanged();
        }
    }

    public string CableMark
    {
        get => _model.CableMark;
        set
        {
            if (_model.CableMark == value)
                return;
            _model.CableMark = value;
            OnPropertyChanged();
            _owner.OutgoingFeederChanged();
        }
    }

    public string CableSection
    {
        get => _model.CableSection;
        set
        {
            if (_model.CableSection == value)
                return;
            _model.CableSection = value;
            OnPropertyChanged();
            _owner.OutgoingFeederChanged();
        }
    }

    public string MeteringType
    {
        get
        {
            NormalizeMetering();
            return _model.MeteringType;
        }
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "Нет" : value;
            if (_model.MeteringType == normalized)
                return;
            _model.MeteringType = normalized;
            _model.HasMeter = !normalized.Equals("Нет", StringComparison.OrdinalIgnoreCase);
            if (_model.HasMeter && string.IsNullOrWhiteSpace(_model.TtRatio))
            {
                _model.TtRatio = _owner.SuggestedCtRatio(_model.Nominal);
                OnPropertyChanged(nameof(TtRatio));
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasMeter));
            _owner.OutgoingFeederChanged();
        }
    }

    public string TtRatio
    {
        get => _model.TtRatio;
        set
        {
            if (_model.TtRatio == value)
                return;
            _model.TtRatio = value;
            OnPropertyChanged();
            _owner.OutgoingFeederChanged();
        }
    }

    public bool HasMeter
    {
        get => _model.HasMeter;
        set
        {
            if (_model.HasMeter == value)
                return;
            _model.HasMeter = value;
            _model.MeteringType = value ? "Коммерческий" : "Нет";
            if (value)
            {
                _model.TtRatio = _owner.SuggestedCtRatio(_model.Nominal);
                OnPropertyChanged(nameof(TtRatio));
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(MeteringType));
            _owner.OutgoingFeederChanged();
        }
    }

    public string Notes
    {
        get => _model.Notes;
        set
        {
            if (_model.Notes == value)
                return;
            _model.Notes = value;
            OnPropertyChanged();
            _owner.OutgoingFeederChanged();
        }
    }

    private void NormalizeMetering()
    {
        if (string.IsNullOrWhiteSpace(_model.MeteringType))
            _model.MeteringType = _model.HasMeter ? "Коммерческий" : "Нет";

        _model.HasMeter = !_model.MeteringType.Equals("Нет", StringComparison.OrdinalIgnoreCase);
    }
}
