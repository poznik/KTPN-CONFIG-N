using System.Collections.ObjectModel;
using System.Globalization;
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
        RuvnSwitches = env.Catalog.Options.RuvnSwitches;
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

        NewProjectCommand = new RelayCommand(NewProject);
        SaveProjectCommand = new RelayCommand(SaveProject);
        OpenProjectCommand = new RelayCommand(OpenProject);
        ExportExcelCommand = new RelayCommand(ExportExcel, CanExportDocuments);
        ExportPdfCommand = new RelayCommand(ExportPdf, CanExportDocuments);
        SaveChecklistCommand = new RelayCommand(SaveChecklist);

        ValidationMessages = new ObservableCollection<ValidationMessage>();
        LoadTemplatesForGridCompany();
        Recalculate();
    }

    private ProjectConfig CreateDefaultConfig()
    {
        var o = _env.Catalog.Options;
        var firstManuf = _env.Catalog.Manufacturers().FirstOrDefault() ?? "";
        var firstMark = _env.Catalog.MarksFor(firstManuf).FirstOrDefault() ?? "";
        // Воспроизводим разумную стартовую конфигурацию из исходной книги.
        return new ProjectConfig
        {
            ProjectName = "Новый проект КТПН",
            Manufacturer = "Алагеум",
            Mark = "ТМГ-400 (Алагеум)",
            SteelType = o.SteelTypes.FirstOrDefault() ?? "",
            Thickness = 2.0,
            Channel = "10П",
            BodyColor = o.RalColors.FirstOrDefault() ?? "",
            DoorColor = o.RalColors.Skip(1).FirstOrDefault() ?? o.RalColors.FirstOrDefault() ?? "",
            GridCompany = o.GridCompanies.FirstOrDefault() ?? "",
            LenRuvn = 1300, LenRunn = 600, TransformerTolerance = 300, LengthBuffer = 10,
            Voltage = o.Voltages.FirstOrDefault() ?? "",
            RuvnType = "Тупиковая",
            RuvnSwitch = o.RuvnSwitches.FirstOrDefault() ?? "",
            RuvnSwitchNominal = 630,
            FuseType = o.FuseTypes.FirstOrDefault() ?? "",
            FuseNominal = "31.5А",
            RuvnExecution = o.CableExecutions.FirstOrDefault() ?? "",
            RuvnSurgeArrester = true,
            PvrOn = true, PvrNominal = 630, PvrManufacturer = "CHINT",
            ReOn = false, ReNominal = 630, ReManufacturer = "КЭАЗ",
            AvInOn = false, AvInNominal = 630, AvInManufacturer = "Контактор",
            RunnSurgeArrester = true, HasCt = true, CtRatio = "600/5",
            HasCtKip = false, CtKipRatio = "600/5", HasMeter = true,
            AvOn = true, AvQty = 2, AvBrand = "IEK",
            RpsOn = false, RpsQty = 0, RpsBrand = o.RpsManufacturers.FirstOrDefault() ?? "",
            OutgoingExecution = o.CableExecutions.FirstOrDefault() ?? "",
        };
    }

    // ===== Combo sources =====
    public ObservableCollection<string> Manufacturers { get; }
    public ObservableCollection<string> Marks { get; }
    public IReadOnlyList<string> SteelTypes { get; }
    public IReadOnlyList<double> Thicknesses { get; }
    public IReadOnlyList<string> Channels { get; }
    public IReadOnlyList<string> RalColors { get; }
    public IReadOnlyList<string> GridCompanies { get; }
    public IReadOnlyList<string> Voltages { get; }
    public IReadOnlyList<string> RuvnTypes { get; }
    public IReadOnlyList<string> RuvnSwitches { get; }
    public IReadOnlyList<int> RuvnNominals { get; }
    public IReadOnlyList<string> FuseTypes { get; }
    public IReadOnlyList<string> FuseNominals { get; }
    public IReadOnlyList<string> CableExecutions { get; }
    public IReadOnlyList<int> LvNominals { get; }
    public IReadOnlyList<string> TtRatios { get; }
    public IReadOnlyList<string> PvrManufacturers { get; }
    public IReadOnlyList<string> ReManufacturers { get; }
    public IReadOnlyList<string> RpsManufacturers { get; }
    public IReadOnlyList<string> AvManufacturers { get; }

    // ===== Commands =====
    public RelayCommand NewProjectCommand { get; }
    public RelayCommand SaveProjectCommand { get; }
    public RelayCommand OpenProjectCommand { get; }
    public RelayCommand ExportExcelCommand { get; }
    public RelayCommand ExportPdfCommand { get; }

    // ===== Meta =====
    public string ProjectName { get => _cfg.ProjectName; set { if (_cfg.ProjectName != value) { _cfg.ProjectName = value; OnPropertyChanged(); } } }
    public string Author { get => _cfg.Author; set { if (_cfg.Author != value) { _cfg.Author = value; OnPropertyChanged(); } } }
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
                Recalculate(); 
            } 
        } 
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

    public string Mark { get => _cfg.Mark; set { if (_cfg.Mark != value) { _cfg.Mark = value; OnPropertyChanged(); Recalculate(); } } }

    // ===== Enclosure =====
    public string SteelType { get => _cfg.SteelType; set { if (_cfg.SteelType != value) { _cfg.SteelType = value; OnPropertyChanged(); Recalculate(); } } }
    public double Thickness { get => _cfg.Thickness; set { if (_cfg.Thickness != value) { _cfg.Thickness = value; OnPropertyChanged(); Recalculate(); } } }
    public string Channel { get => _cfg.Channel; set { if (_cfg.Channel != value) { _cfg.Channel = value; OnPropertyChanged(); Recalculate(); } } }
    public string BodyColor { get => _cfg.BodyColor; set { if (_cfg.BodyColor != value) { _cfg.BodyColor = value; OnPropertyChanged(); Recalculate(); } } }
    public string DoorColor { get => _cfg.DoorColor; set { if (_cfg.DoorColor != value) { _cfg.DoorColor = value; OnPropertyChanged(); Recalculate(); } } }

    // ===== Compartments =====
    public double LenRuvn { get => _cfg.LenRuvn; set { if (_cfg.LenRuvn != value) { _cfg.LenRuvn = value; OnPropertyChanged(); Recalculate(); } } }
    public double LenRunn { get => _cfg.LenRunn; set { if (_cfg.LenRunn != value) { _cfg.LenRunn = value; OnPropertyChanged(); Recalculate(); } } }
    public double TransformerTolerance { get => _cfg.TransformerTolerance; set { if (_cfg.TransformerTolerance != value) { _cfg.TransformerTolerance = value; OnPropertyChanged(); Recalculate(); } } }
    public double LengthBuffer { get => _cfg.LengthBuffer; set { if (_cfg.LengthBuffer != value) { _cfg.LengthBuffer = value; OnPropertyChanged(); Recalculate(); } } }

    // ===== РУВН =====
    public string Voltage { get => _cfg.Voltage; set { if (_cfg.Voltage != value) { _cfg.Voltage = value; OnPropertyChanged(); Recalculate(); } } }
    public string RuvnType { get => _cfg.RuvnType; set { if (_cfg.RuvnType != value) { _cfg.RuvnType = value; OnPropertyChanged(); Recalculate(); } } }
    public string RuvnSwitch { get => _cfg.RuvnSwitch; set { if (_cfg.RuvnSwitch != value) { _cfg.RuvnSwitch = value; OnPropertyChanged(); Recalculate(); } } }
    public int RuvnSwitchNominal { get => _cfg.RuvnSwitchNominal; set { if (_cfg.RuvnSwitchNominal != value) { _cfg.RuvnSwitchNominal = value; OnPropertyChanged(); Recalculate(); } } }
    public string FuseType { get => _cfg.FuseType; set { if (_cfg.FuseType != value) { _cfg.FuseType = value; OnPropertyChanged(); Recalculate(); } } }
    public string FuseNominal { get => _cfg.FuseNominal; set { if (_cfg.FuseNominal != value) { _cfg.FuseNominal = value; OnPropertyChanged(); Recalculate(); } } }
    public string RuvnExecution { get => _cfg.RuvnExecution; set { if (_cfg.RuvnExecution != value) { _cfg.RuvnExecution = value; OnPropertyChanged(); Recalculate(); } } }
    public bool RuvnSurgeArrester { get => _cfg.RuvnSurgeArrester; set { if (_cfg.RuvnSurgeArrester != value) { _cfg.RuvnSurgeArrester = value; OnPropertyChanged(); Recalculate(); } } }

    // ===== РУНН ввод =====
    public bool PvrOn { get => _cfg.PvrOn; set { if (_cfg.PvrOn != value) { _cfg.PvrOn = value; OnPropertyChanged(); Recalculate(); } } }
    public int PvrNominal { get => _cfg.PvrNominal; set { if (_cfg.PvrNominal != value) { _cfg.PvrNominal = value; OnPropertyChanged(); Recalculate(); } } }
    public string PvrManufacturer { get => _cfg.PvrManufacturer; set { if (_cfg.PvrManufacturer != value) { _cfg.PvrManufacturer = value; OnPropertyChanged(); Recalculate(); } } }
    public bool ReOn { get => _cfg.ReOn; set { if (_cfg.ReOn != value) { _cfg.ReOn = value; OnPropertyChanged(); Recalculate(); } } }
    public int ReNominal { get => _cfg.ReNominal; set { if (_cfg.ReNominal != value) { _cfg.ReNominal = value; OnPropertyChanged(); Recalculate(); } } }
    public string ReManufacturer { get => _cfg.ReManufacturer; set { if (_cfg.ReManufacturer != value) { _cfg.ReManufacturer = value; OnPropertyChanged(); Recalculate(); } } }
    public bool AvInOn { get => _cfg.AvInOn; set { if (_cfg.AvInOn != value) { _cfg.AvInOn = value; OnPropertyChanged(); Recalculate(); } } }
    public int AvInNominal { get => _cfg.AvInNominal; set { if (_cfg.AvInNominal != value) { _cfg.AvInNominal = value; OnPropertyChanged(); Recalculate(); } } }
    public string AvInManufacturer { get => _cfg.AvInManufacturer; set { if (_cfg.AvInManufacturer != value) { _cfg.AvInManufacturer = value; OnPropertyChanged(); Recalculate(); } } }
    public bool RunnSurgeArrester { get => _cfg.RunnSurgeArrester; set { if (_cfg.RunnSurgeArrester != value) { _cfg.RunnSurgeArrester = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasCt { get => _cfg.HasCt; set { if (_cfg.HasCt != value) { _cfg.HasCt = value; OnPropertyChanged(); Recalculate(); } } }
    public string CtRatio { get => _cfg.CtRatio; set { if (_cfg.CtRatio != value) { _cfg.CtRatio = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasCtKip { get => _cfg.HasCtKip; set { if (_cfg.HasCtKip != value) { _cfg.HasCtKip = value; OnPropertyChanged(); Recalculate(); } } }
    public string CtKipRatio { get => _cfg.CtKipRatio; set { if (_cfg.CtKipRatio != value) { _cfg.CtKipRatio = value; OnPropertyChanged(); Recalculate(); } } }
    public bool HasMeter { get => _cfg.HasMeter; set { if (_cfg.HasMeter != value) { _cfg.HasMeter = value; OnPropertyChanged(); Recalculate(); } } }

    // ===== Отходящие линии =====
    public bool AvOn { get => _cfg.AvOn; set { if (_cfg.AvOn != value) { _cfg.AvOn = value; OnPropertyChanged(); Recalculate(); } } }
    public int AvQty { get => _cfg.AvQty; set { if (_cfg.AvQty != value) { _cfg.AvQty = value; OnPropertyChanged(); Recalculate(); } } }
    public string AvBrand { get => _cfg.AvBrand; set { if (_cfg.AvBrand != value) { _cfg.AvBrand = value; OnPropertyChanged(); Recalculate(); } } }
    public bool RpsOn { get => _cfg.RpsOn; set { if (_cfg.RpsOn != value) { _cfg.RpsOn = value; OnPropertyChanged(); Recalculate(); } } }
    public int RpsQty { get => _cfg.RpsQty; set { if (_cfg.RpsQty != value) { _cfg.RpsQty = value; OnPropertyChanged(); Recalculate(); } } }
    public string RpsBrand { get => _cfg.RpsBrand; set { if (_cfg.RpsBrand != value) { _cfg.RpsBrand = value; OnPropertyChanged(); Recalculate(); } } }
    public string OutgoingExecution { get => _cfg.OutgoingExecution; set { if (_cfg.OutgoingExecution != value) { _cfg.OutgoingExecution = value; OnPropertyChanged(); Recalculate(); } } }

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
    public string BusbarHv => _res.BusbarHv;
    public string BusbarLv => _res.BusbarLv;
    public string BusbarPe => _res.BusbarPe;

    // ===== Валидация =====
    public ObservableCollection<ValidationMessage> ValidationMessages { get; }
    public bool IsValid => !_res.HasErrors;
    public string StatusText => _res.HasErrors
        ? "🛑 ЕСТЬ ОШИБКИ ПРОЕКТИРОВАНИЯ"
        : (ValidationMessages.Any(m => m.Severity == Severity.Warning) ? "⚠ Есть предупреждения" : "✅ Проверки пройдены");
    public string StatusColor => _res.HasErrors ? "#8A5F5F" : (ValidationMessages.Any(m => m.Severity == Severity.Warning) ? "#8A743F" : "#607D68");

    // ===== Превью документов =====
    public string PassportPreview { get; private set; } = "";
    public string OrderPreview { get; private set; } = "";
    public string ChecklistPreview { get; private set; } = "";

    private List<GeneratedDocument> BuildDocuments()
    {
        return new List<GeneratedDocument>
        {
            DocumentBuilder.BuildProductionOrder(_cfg, _res, _env.Catalog, _env.Templates),
            DocumentBuilder.BuildPassport(_cfg, _res, _env.Catalog),
            DocumentBuilder.BuildChecklist(_cfg, _res, _env.Catalog, _env.Templates),
        };
    }

    public void Recalculate()
    {
        if (_suspendRecalc) return;
        _res = CalculationEngine.Calculate(_cfg, _env.Catalog);

        ValidationMessages.Clear();
        foreach (var msg in _res.Messages)
            ValidationMessages.Add(msg);

        var order = DocumentBuilder.BuildProductionOrder(_cfg, _res, _env.Catalog, _env.Templates);
        var passport = DocumentBuilder.BuildPassport(_cfg, _res, _env.Catalog);
        var checklist = DocumentBuilder.BuildChecklist(_cfg, _res, _env.Catalog, _env.Templates);
        OrderPreview = order.ToPlainText();
        PassportPreview = passport.ToPlainText();
        ChecklistPreview = checklist.ToPlainText();

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
            nameof(GrossMassCalcText), nameof(GrossMassText),
            nameof(BusbarHv), nameof(BusbarLv), nameof(BusbarPe),
            nameof(IsValid), nameof(StatusText), nameof(StatusColor),
            nameof(PassportPreview), nameof(OrderPreview), nameof(ChecklistPreview),
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
        Marks.Clear();
        foreach (var m in _env.Catalog.MarksFor(_cfg.Manufacturer)) Marks.Add(m);
        _suspendRecalc = false;
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
        Marks.Clear();
        foreach (var m in _env.Catalog.MarksFor(_cfg.Manufacturer)) Marks.Add(m);
        _suspendRecalc = false;
        OnPropertyChanged(string.Empty);
        Recalculate();
        Notify?.Invoke($"Проект загружен: {path}");
    }

    private bool CanExportDocuments() => !_res.HasErrors;

    private bool EnsureCanExportDocuments()
    {
        if (CanExportDocuments())
            return true;

        Notify?.Invoke("Экспорт заблокирован: сначала исправьте ошибки проектирования.");
        return false;
    }

    private void ExportExcel()
    {
        if (!EnsureCanExportDocuments()) return;
        var path = AskExportPath?.Invoke("xlsx");
        if (string.IsNullOrEmpty(path)) return;
        ExcelExporter.Export(path, _cfg, _res, BuildDocuments());
        Notify?.Invoke($"Документы выгружены в Excel: {path}");
    }

    private void ExportPdf()
    {
        if (!EnsureCanExportDocuments()) return;
        var path = AskExportPath?.Invoke("pdf");
        if (string.IsNullOrEmpty(path)) return;
        PdfExporter.Export(path, BuildDocuments());
        Notify?.Invoke($"Документы выгружены в PDF: {path}");
    }

    public ProjectConfig CurrentConfig => _cfg;
    public CalculationResult CurrentResult => _res;
}
