namespace KtpnConfigurator.Core.Models;

public sealed class DoubleKtpnConfig
{
    public string SecondTransformerManufacturer { get; set; } = "Алагеум";
    public string SecondTransformerMark { get; set; } = "ТМГ-400 (Алагеум)";
    public int Section1InputNominalA { get; set; } = 630;
    public int Section2InputNominalA { get; set; } = 630;
    public int SectionCouplerNominalA { get; set; } = 630;
    public bool AutomaticTransferEnabled { get; set; } = true;
    public bool ParallelOperationAllowed { get; set; }
    public string NormalCouplerPosition { get; set; } = "Отключен";
    public string ReserveMode { get; set; } = "Взаимное резервирование секций";

    public DoubleKtpnConfig Clone() => (DoubleKtpnConfig)MemberwiseClone();
}

public sealed class LowVoltageAssemblyConfig
{
    public string Series { get; set; } = "НКУ";
    public string LineupTemplate { get; set; } = "";
    public int RatedVoltageV { get; set; } = 400;
    public int FrequencyHz { get; set; } = 50;
    public int RatedBusCurrentA { get; set; } = 630;
    public double DesignShortCircuitCurrentKa { get; set; } = 25;
    public double ShortTimeWithstandCurrentKa { get; set; } = 25;
    public double PeakWithstandCurrentKa { get; set; } = 55;
    public string EarthingSystem { get; set; } = "TN-S";
    public string InternalSeparation { get; set; } = "2b";
    public string ServiceAccess { get; set; } = "Одностороннее";
    public string BusbarMaterial { get; set; } = "Медь";
    public string ProtectionDegree { get; set; } = "IP31";
    public int SectionCount { get; set; } = 1;
    public bool AutomaticTransferEnabled { get; set; }
    public double HeightMm { get; set; } = 2200;
    public double DepthMm { get; set; } = 800;
    public List<LowVoltagePanelConfig> Panels { get; set; } = new();

    public LowVoltageAssemblyConfig Clone()
    {
        var clone = (LowVoltageAssemblyConfig)MemberwiseClone();
        clone.Panels = (Panels ?? new()).Select(panel => panel.Clone()).ToList();
        return clone;
    }
}

public sealed class LowVoltagePanelConfig
{
    public int Number { get; set; }
    public int SectionNumber { get; set; } = 1;
    public string PanelType { get; set; } = "Линейная";
    public string Purpose { get; set; } = "Отходящие линии";
    public string MainDevice { get; set; } = "АВ";
    public string Manufacturer { get; set; } = "";
    public string Model { get; set; } = "";
    public int RatedCurrentA { get; set; } = 250;
    public double BreakingCapacityKa { get; set; } = 25;
    public int CircuitCount { get; set; } = 1;
    public bool HasMetering { get; set; }
    public bool HasSurgeProtection { get; set; }
    public double WidthMm { get; set; } = 800;
    public double EstimatedMassKg { get; set; } = 150;
    public string EquipmentSourceConfidence { get; set; } = "needsVerification";
    public string EquipmentSourceNotes { get; set; } = "Типовой подбор требует проверки по паспорту аппарата.";

    public LowVoltagePanelConfig Clone() => (LowVoltagePanelConfig)MemberwiseClone();
}

public sealed class MediumVoltageSwitchgearConfig
{
    public string Series { get; set; } = "КСО";
    public string LineupTemplate { get; set; } = "";
    public double RatedVoltageKv { get; set; } = 10;
    public double HighestOperatingVoltageKv { get; set; } = 12;
    public int RatedBusCurrentA { get; set; } = 630;
    public double DesignShortCircuitCurrentKa { get; set; } = 20;
    public double ShortTimeWithstandCurrentKa { get; set; } = 20;
    public double ShortTimeDurationSeconds { get; set; } = 1;
    public double PeakWithstandCurrentKa { get; set; } = 51;
    public double BreakerBreakingCurrentKa { get; set; } = 20;
    public string NeutralMode { get; set; } = "Изолированная";
    public string ServiceAccess { get; set; } = "Одностороннее";
    public string OperationalPower { get; set; } = "220 В DC";
    public string IacClassification { get; set; } = "AFLR 20 кА 1 с";
    public string ServiceContinuityCategory { get; set; } = "LSC2B";
    public string PartitionClass { get; set; } = "PM";
    public string ArcRelief { get; set; } = "Дефлектор";
    public string CellExecution { get; set; } = "Стационарное";
    public double HeightMm { get; set; } = 2400;
    public double DepthMm { get; set; } = 1400;
    public List<MediumVoltageCellConfig> Cells { get; set; } = new();

    public MediumVoltageSwitchgearConfig Clone()
    {
        var clone = (MediumVoltageSwitchgearConfig)MemberwiseClone();
        clone.Cells = (Cells ?? new()).Select(cell => cell.Clone()).ToList();
        return clone;
    }
}

public sealed class MediumVoltageCellConfig
{
    public int Number { get; set; }
    public string Purpose { get; set; } = "Линия";
    public string MainDevice { get; set; } = "Вакуумный выключатель";
    public string DeviceModel { get; set; } = "ВВ/TEL-10";
    public int RatedCurrentA { get; set; } = 630;
    public double BreakingCurrentKa { get; set; } = 20;
    public string CtRatio { get; set; } = "600/5";
    public string CtAccuracyClass { get; set; } = "0,5/10P";
    public bool HasVoltageTransformer { get; set; }
    public string VoltageTransformerModel { get; set; } = "";
    public string RelayProtection { get; set; } = "МТЗ, ТО, ОЗЗ";
    public string RelayTerminal { get; set; } = "";
    public string VisibleBreaks { get; set; } = "По конструкции ячейки";
    public string VisibleBreakBefore { get; set; } = "РВЗ";
    public string VisibleBreakAfter { get; set; } = "РВЗ";
    public bool HasEarthingSwitch { get; set; } = true;
    public double WidthMm { get; set; } = 750;
    public double EstimatedMassKg { get; set; } = 650;
    public string EquipmentSourceConfidence { get; set; } = "needsVerification";
    public string EquipmentSourceNotes { get; set; } = "Типовая ячейка требует подтверждения по серии и протоколам испытаний.";

    public MediumVoltageCellConfig Clone() => (MediumVoltageCellConfig)MemberwiseClone();
}

public static class ProductConfigurationDefaults
{
    /// <summary>Только починка null-вложений; не изменяет данные проекта.</summary>
    public static void EnsureNestedConfigs(ProjectConfig project)
    {
        project.DoubleKtpn ??= new DoubleKtpnConfig();
        project.LowVoltageAssembly ??= new LowVoltageAssemblyConfig();
        project.MediumVoltageSwitchgear ??= new MediumVoltageSwitchgearConfig();
        project.LowVoltageAssembly.Panels ??= new List<LowVoltagePanelConfig>();
        project.MediumVoltageSwitchgear.Cells ??= new List<MediumVoltageCellConfig>();
    }

    /// <summary>
    /// Полная подготовка проекта: дефолтный шаблон и типовая линейка для пустого
    /// проекта. Вызывается при загрузке и смене изделия — но НЕ из расчёта и не
    /// при обновлении коллекций, иначе намеренно удалённые панели «воскресают».
    /// </summary>
    public static void Normalize(ProjectConfig project)
    {
        EnsureNestedConfigs(project);

        if (project.ProductTypeId is ProductTypeIds.Nku or ProductTypeIds.Shcho or ProductTypeIds.Vru
            && string.IsNullOrWhiteSpace(project.LowVoltageAssembly.LineupTemplate))
        {
            project.LowVoltageAssembly.LineupTemplate = DefaultLowVoltageTemplate(project.ProductTypeId);
        }

        if (project.ProductTypeId is ProductTypeIds.Kso or ProductTypeIds.Kru
            && string.IsNullOrWhiteSpace(project.MediumVoltageSwitchgear.LineupTemplate))
        {
            project.MediumVoltageSwitchgear.LineupTemplate = DefaultMediumVoltageTemplate(project.ProductTypeId);
        }

        if (project.ProductTypeId is ProductTypeIds.Nku or ProductTypeIds.Shcho or ProductTypeIds.Vru
            && project.LowVoltageAssembly.Panels.Count == 0)
        {
            ApplyLowVoltageTemplate(project, project.LowVoltageAssembly.LineupTemplate);
        }

        if (project.ProductTypeId is ProductTypeIds.Kso or ProductTypeIds.Kru
            && project.MediumVoltageSwitchgear.Cells.Count == 0)
        {
            ApplyMediumVoltageTemplate(project, project.MediumVoltageSwitchgear.LineupTemplate);
        }

        Renumber(project.LowVoltageAssembly.Panels);
        Renumber(project.MediumVoltageSwitchgear.Cells);
        foreach (var panel in project.LowVoltageAssembly.Panels)
            NormalizeEquipmentTrust(panel);
        foreach (var cell in project.MediumVoltageSwitchgear.Cells)
            NormalizeEquipmentTrust(cell);
    }

    // Списки шаблонов должны быть стабильными экземплярами: WPF-привязки сравнивают
    // ItemsSource по ссылке, и новый массив на каждый вызов заставляет ComboBox
    // сбрасывать SelectedItem.
    private static readonly IReadOnlyList<string> VruTemplates =
    [
        "ВРУ: ввод + учет + отходящие",
        "ВРУ: два ввода + АВР",
        "ВРУ: учет + распределение",
    ];

    private static readonly IReadOnlyList<string> ShchoTemplates =
    [
        "ЩО: две секции 630 А",
        "ЩО: одна секция 400 А",
        "ЩО: две секции 1000 А",
    ];

    private static readonly IReadOnlyList<string> NkuTemplates =
    [
        "НКУ: ввод + распределение + ЩСН",
        "НКУ: два ввода + секционный аппарат",
        "НКУ: распределительный шкаф",
    ];

    private static readonly IReadOnlyList<string> KsoTemplates =
    [
        "КСО: ввод + ТН + трансформатор",
        "КСО: проходная линия + трансформатор",
        "КСО: секционированное РУ",
    ];

    private static readonly IReadOnlyList<string> KruTemplates =
    [
        "КРУ: ввод + ТН + секция + отходящая",
        "КРУ: две секции с АВР",
        "КРУ: ввод + отходящие линии",
    ];

    public static IReadOnlyList<string> LowVoltageTemplateNames(string productTypeId) =>
        productTypeId switch
        {
            ProductTypeIds.Vru => VruTemplates,
            ProductTypeIds.Shcho => ShchoTemplates,
            _ => NkuTemplates,
        };

    public static IReadOnlyList<string> MediumVoltageTemplateNames(string productTypeId) =>
        productTypeId == ProductTypeIds.Kso ? KsoTemplates : KruTemplates;

    public static string DefaultLowVoltageTemplate(string productTypeId) =>
        LowVoltageTemplateNames(productTypeId)[0];

    public static string DefaultMediumVoltageTemplate(string productTypeId) =>
        MediumVoltageTemplateNames(productTypeId)[0];

    public static void ApplyLowVoltageTemplate(ProjectConfig project, string? template)
    {
        var config = project.LowVoltageAssembly;
        var selected = NormalizeTemplate(template, LowVoltageTemplateNames(project.ProductTypeId));
        config.LineupTemplate = selected;

        switch (selected)
        {
            case "ВРУ: два ввода + АВР":
                config.Series = "ВРУ";
                config.RatedBusCurrentA = 630;
                config.DesignShortCircuitCurrentKa = 25;
                config.ShortTimeWithstandCurrentKa = 25;
                config.PeakWithstandCurrentKa = 55;
                config.SectionCount = 2;
                config.AutomaticTransferEnabled = true;
                config.InternalSeparation = "2b";
                config.ProtectionDegree = "IP31";
                break;
            case "ВРУ: учет + распределение":
                config.Series = "ВРУ";
                config.RatedBusCurrentA = 250;
                config.DesignShortCircuitCurrentKa = 15;
                config.ShortTimeWithstandCurrentKa = 15;
                config.PeakWithstandCurrentKa = 33;
                config.SectionCount = 1;
                config.AutomaticTransferEnabled = false;
                config.InternalSeparation = "1";
                config.ProtectionDegree = "IP31";
                break;
            case "ЩО: одна секция 400 А":
                config.Series = "ЩО-70";
                config.RatedBusCurrentA = 400;
                config.DesignShortCircuitCurrentKa = 25;
                config.ShortTimeWithstandCurrentKa = 25;
                config.PeakWithstandCurrentKa = 55;
                config.SectionCount = 1;
                config.AutomaticTransferEnabled = false;
                config.InternalSeparation = "2b";
                config.ProtectionDegree = "IP20";
                break;
            case "ЩО: две секции 1000 А":
                config.Series = "ЩО-70";
                config.RatedBusCurrentA = 1000;
                config.DesignShortCircuitCurrentKa = 31.5;
                config.ShortTimeWithstandCurrentKa = 31.5;
                config.PeakWithstandCurrentKa = 70;
                config.SectionCount = 2;
                config.AutomaticTransferEnabled = false;
                config.InternalSeparation = "2b";
                config.ProtectionDegree = "IP20";
                break;
            case "НКУ: два ввода + секционный аппарат":
                config.Series = "НКУ";
                config.RatedBusCurrentA = 1000;
                config.DesignShortCircuitCurrentKa = 31.5;
                config.ShortTimeWithstandCurrentKa = 31.5;
                config.PeakWithstandCurrentKa = 70;
                config.SectionCount = 2;
                config.AutomaticTransferEnabled = true;
                config.InternalSeparation = "3b";
                config.ProtectionDegree = "IP31";
                break;
            case "НКУ: распределительный шкаф":
                config.Series = "НКУ";
                config.RatedBusCurrentA = 400;
                config.DesignShortCircuitCurrentKa = 25;
                config.ShortTimeWithstandCurrentKa = 25;
                config.PeakWithstandCurrentKa = 55;
                config.SectionCount = 1;
                config.AutomaticTransferEnabled = false;
                config.InternalSeparation = "2b";
                config.ProtectionDegree = "IP31";
                break;
            case "ВРУ: ввод + учет + отходящие":
                config.Series = "ВРУ";
                config.RatedBusCurrentA = 400;
                config.DesignShortCircuitCurrentKa = 25;
                config.ShortTimeWithstandCurrentKa = 25;
                config.PeakWithstandCurrentKa = 55;
                config.SectionCount = 1;
                config.AutomaticTransferEnabled = false;
                config.InternalSeparation = "2b";
                config.ProtectionDegree = "IP31";
                break;
            case "ЩО: две секции 630 А":
                config.Series = "ЩО-70";
                config.RatedBusCurrentA = 630;
                config.DesignShortCircuitCurrentKa = 31.5;
                config.ShortTimeWithstandCurrentKa = 31.5;
                config.PeakWithstandCurrentKa = 70;
                config.SectionCount = 2;
                config.AutomaticTransferEnabled = false;
                config.InternalSeparation = "2b";
                config.ProtectionDegree = "IP20";
                break;
            default:
                config.Series = "НКУ";
                config.RatedBusCurrentA = 630;
                config.DesignShortCircuitCurrentKa = 25;
                config.ShortTimeWithstandCurrentKa = 25;
                config.PeakWithstandCurrentKa = 55;
                config.SectionCount = 1;
                config.AutomaticTransferEnabled = false;
                config.InternalSeparation = "2b";
                config.ProtectionDegree = "IP31";
                break;
        }

        config.Panels = LowVoltagePanelsForTemplate(project.ProductTypeId, selected)
            .Select(panel => panel.Clone())
            .ToList();
        foreach (var panel in config.Panels)
            ApplyLowVoltageDevicePreset(panel, project.ProductTypeId, config.RatedBusCurrentA, config.ShortTimeWithstandCurrentKa);
        Renumber(config.Panels);
    }

    public static void ApplyMediumVoltageTemplate(ProjectConfig project, string? template)
    {
        var config = project.MediumVoltageSwitchgear;
        var selected = NormalizeTemplate(template, MediumVoltageTemplateNames(project.ProductTypeId));
        config.LineupTemplate = selected;

        switch (selected)
        {
            case "КСО: проходная линия + трансформатор":
                config.Series = "КСО";
                config.RatedBusCurrentA = 630;
                config.DesignShortCircuitCurrentKa = 20;
                config.ShortTimeWithstandCurrentKa = 20;
                config.PeakWithstandCurrentKa = 51;
                config.BreakerBreakingCurrentKa = 20;
                config.CellExecution = "Стационарное";
                break;
            case "КСО: секционированное РУ":
                config.Series = "КСО";
                config.RatedBusCurrentA = 1000;
                config.DesignShortCircuitCurrentKa = 20;
                config.ShortTimeWithstandCurrentKa = 20;
                config.PeakWithstandCurrentKa = 51;
                config.BreakerBreakingCurrentKa = 20;
                config.CellExecution = "Стационарное";
                break;
            case "КРУ: две секции с АВР":
                config.Series = "КРУ";
                config.RatedBusCurrentA = 1600;
                config.DesignShortCircuitCurrentKa = 31.5;
                config.ShortTimeWithstandCurrentKa = 31.5;
                config.PeakWithstandCurrentKa = 80;
                config.BreakerBreakingCurrentKa = 31.5;
                config.IacClassification = "AFLR 31,5 кА 1 с";
                config.CellExecution = "Выдвижное";
                break;
            case "КРУ: ввод + отходящие линии":
                config.Series = "КРУ";
                config.RatedBusCurrentA = 1000;
                config.DesignShortCircuitCurrentKa = 25;
                config.ShortTimeWithstandCurrentKa = 25;
                config.PeakWithstandCurrentKa = 63;
                config.BreakerBreakingCurrentKa = 25;
                config.IacClassification = "AFLR 25 кА 1 с";
                config.CellExecution = "Выдвижное";
                break;
            case "КРУ: ввод + ТН + секция + отходящая":
                config.Series = "КРУ";
                config.RatedBusCurrentA = 1000;
                config.DesignShortCircuitCurrentKa = 25;
                config.ShortTimeWithstandCurrentKa = 25;
                config.PeakWithstandCurrentKa = 63;
                config.BreakerBreakingCurrentKa = 25;
                config.IacClassification = "AFLR 25 кА 1 с";
                config.CellExecution = "Выдвижное";
                break;
            default:
                config.Series = "КСО";
                config.RatedBusCurrentA = 630;
                config.DesignShortCircuitCurrentKa = 20;
                config.ShortTimeWithstandCurrentKa = 20;
                config.PeakWithstandCurrentKa = 51;
                config.BreakerBreakingCurrentKa = 20;
                config.CellExecution = "Стационарное";
                break;
        }

        config.Cells = MediumVoltageCellsForTemplate(project.ProductTypeId, selected)
            .Select(cell => cell.Clone())
            .ToList();
        foreach (var cell in config.Cells)
            ApplyMediumVoltageDevicePreset(cell, project.ProductTypeId, config.RatedBusCurrentA, config.BreakerBreakingCurrentKa);
        Renumber(config.Cells);
    }

    public static void ApplyLowVoltagePanelPreset(
        LowVoltagePanelConfig panel,
        string productTypeId,
        int busCurrentA,
        double shortCircuitCurrentKa)
    {
        var current = NormalizeCurrent(busCurrentA, 630);
        var panelType = panel.PanelType.Trim();
        if (panelType.Contains("ввод", StringComparison.OrdinalIgnoreCase))
        {
            panel.Purpose = "Ввод секции";
            panel.MainDevice = "АВ";
            panel.RatedCurrentA = current;
            panel.BreakingCapacityKa = NormalizeBreaking(shortCircuitCurrentKa, 25);
            panel.CircuitCount = 1;
            panel.HasMetering = false;
            panel.HasSurgeProtection = true;
            panel.WidthMm = productTypeId == ProductTypeIds.Vru ? 800 : 800;
            panel.EstimatedMassKg = current >= 1000 ? 220 : 185;
        }
        else if (panelType.Contains("секцион", StringComparison.OrdinalIgnoreCase))
        {
            panel.Purpose = "Секционный аппарат";
            panel.MainDevice = "Секционный АВ";
            panel.RatedCurrentA = current;
            panel.BreakingCapacityKa = NormalizeBreaking(shortCircuitCurrentKa, 25);
            panel.CircuitCount = 1;
            panel.HasMetering = false;
            panel.HasSurgeProtection = false;
            panel.WidthMm = current >= 1000 ? 800 : 600;
            panel.EstimatedMassKg = current >= 1000 ? 220 : 170;
        }
        else if (panelType.Contains("уч", StringComparison.OrdinalIgnoreCase))
        {
            panel.Purpose = "Коммерческий учет";
            panel.MainDevice = "Счетчик, ТТ";
            panel.RatedCurrentA = current;
            panel.BreakingCapacityKa = NormalizeBreaking(shortCircuitCurrentKa, 25);
            panel.CircuitCount = 1;
            panel.HasMetering = true;
            panel.HasSurgeProtection = false;
            panel.WidthMm = 600;
            panel.EstimatedMassKg = 130;
        }
        else if (panelType.Equals("АВР", StringComparison.OrdinalIgnoreCase))
        {
            panel.Purpose = "Автоматическое включение резерва";
            panel.MainDevice = "АВР";
            panel.RatedCurrentA = current;
            panel.BreakingCapacityKa = NormalizeBreaking(shortCircuitCurrentKa, 25);
            panel.CircuitCount = 1;
            panel.HasMetering = false;
            panel.HasSurgeProtection = false;
            panel.WidthMm = 600;
            panel.EstimatedMassKg = 165;
        }
        else if (panelType.Contains("укрм", StringComparison.OrdinalIgnoreCase))
        {
            panel.Purpose = "Компенсация реактивной мощности";
            panel.MainDevice = "УКРМ";
            panel.RatedCurrentA = Math.Min(current, 250);
            panel.BreakingCapacityKa = NormalizeBreaking(shortCircuitCurrentKa, 25);
            panel.CircuitCount = 1;
            panel.HasMetering = false;
            panel.HasSurgeProtection = false;
            panel.WidthMm = 600;
            panel.EstimatedMassKg = 180;
        }
        else if (panelType.Contains("щсн", StringComparison.OrdinalIgnoreCase)
            || panelType.Contains("управ", StringComparison.OrdinalIgnoreCase))
        {
            panel.Purpose = "Собственные нужды и сигнализация";
            panel.MainDevice = "АВ, ОПС";
            panel.RatedCurrentA = 100;
            panel.BreakingCapacityKa = Math.Min(NormalizeBreaking(shortCircuitCurrentKa, 25), 25);
            panel.CircuitCount = 4;
            panel.HasMetering = false;
            panel.HasSurgeProtection = false;
            panel.WidthMm = 600;
            panel.EstimatedMassKg = 120;
        }
        else if (panelType.Contains("резерв", StringComparison.OrdinalIgnoreCase))
        {
            panel.Purpose = "Резерв";
            panel.MainDevice = "Нет";
            panel.RatedCurrentA = 0;
            panel.BreakingCapacityKa = NormalizeBreaking(shortCircuitCurrentKa, 25);
            panel.CircuitCount = 0;
            panel.HasMetering = false;
            panel.HasSurgeProtection = false;
            panel.WidthMm = 600;
            panel.EstimatedMassKg = 90;
        }
        else
        {
            panel.Purpose = "Отходящие линии";
            panel.MainDevice = string.IsNullOrWhiteSpace(panel.MainDevice) ? "АВ" : panel.MainDevice;
            panel.RatedCurrentA = Math.Min(current, current >= 1000 ? 400 : 250);
            panel.BreakingCapacityKa = NormalizeBreaking(shortCircuitCurrentKa, 25);
            panel.CircuitCount = panel.PanelType.Contains("расп", StringComparison.OrdinalIgnoreCase) ? 10 : 8;
            panel.HasMetering = false;
            panel.HasSurgeProtection = false;
            panel.WidthMm = 800;
            panel.EstimatedMassKg = 180;
        }

        ApplyLowVoltageDevicePreset(panel, productTypeId, busCurrentA, shortCircuitCurrentKa);
    }

    public static void ApplyLowVoltageDevicePreset(
        LowVoltagePanelConfig panel,
        string productTypeId,
        int busCurrentA,
        double shortCircuitCurrentKa)
    {
        var device = panel.MainDevice.Trim();
        if (device.Contains("рпс", StringComparison.OrdinalIgnoreCase))
        {
            panel.Manufacturer = "КЭАЗ";
            panel.Model = "РПС";
            panel.EquipmentSourceConfidence = "needsVerification";
            panel.EquipmentSourceNotes = "Проверить исполнение, предохранители и категорию применения.";
        }
        else if (device.Contains("nh", StringComparison.OrdinalIgnoreCase)
            || device.Contains("пвр", StringComparison.OrdinalIgnoreCase))
        {
            panel.Manufacturer = "По проекту";
            panel.Model = panel.RatedCurrentA <= 160 ? "NH00" : panel.RatedCurrentA <= 250 ? "NH1" : panel.RatedCurrentA <= 630 ? "NH2" : "NH3";
            panel.EquipmentSourceConfidence = "needsVerification";
            panel.EquipmentSourceNotes = "NH/ПВР требует проверки держателя, плавкой вставки и цепей РИСЭ.";
        }
        else if (device.Contains("счет", StringComparison.OrdinalIgnoreCase)
            || device.Contains("тт", StringComparison.OrdinalIgnoreCase))
        {
            panel.Manufacturer = "По проекту";
            panel.Model = "Узел учета с ТТ";
            panel.EquipmentSourceConfidence = "userInput";
            panel.EquipmentSourceNotes = "Состав учета задается проектом и сетевой организацией.";
        }
        else if (device.Contains("авр", StringComparison.OrdinalIgnoreCase))
        {
            panel.Manufacturer = "По проекту";
            panel.Model = "АВР";
            panel.EquipmentSourceConfidence = "needsVerification";
            panel.EquipmentSourceNotes = "Проверить логику АВР, блокировки и ток аппаратов.";
        }
        else if (device.Contains("опс", StringComparison.OrdinalIgnoreCase))
        {
            panel.Manufacturer = "По проекту";
            panel.Model = "ОПС";
            panel.EquipmentSourceConfidence = "needsVerification";
            panel.EquipmentSourceNotes = "ОПС уточнить по профилю объекта и требованиям эксплуатации.";
        }
        else if (device.Contains("укрм", StringComparison.OrdinalIgnoreCase))
        {
            panel.Manufacturer = "По проекту";
            panel.Model = "УКРМ";
            panel.EquipmentSourceConfidence = "needsVerification";
            panel.EquipmentSourceNotes = "Мощность УКРМ подобрать по расчету компенсации.";
        }
        else if (device.Contains("нет", StringComparison.OrdinalIgnoreCase))
        {
            panel.Manufacturer = "";
            panel.Model = "";
            panel.EquipmentSourceConfidence = "userInput";
            panel.EquipmentSourceNotes = "Резервная позиция.";
        }
        else
        {
            panel.Manufacturer = "КЭАЗ";
            panel.Model = panel.RatedCurrentA <= 800 ? "OptiMat D" : "OptiMat A";
            panel.EquipmentSourceConfidence = panel.Model.Equals("OptiMat D", StringComparison.OrdinalIgnoreCase) ? "verified" : "needsVerification";
            panel.EquipmentSourceNotes = panel.EquipmentSourceConfidence == "verified"
                ? "Есть проверенная запись в базе; Icu/Ics все равно сверить по паспорту исполнения."
                : "Параметры серии требуют проверки по актуальному каталогу.";
        }

        if (panel.RatedCurrentA > 0)
            panel.RatedCurrentA = NormalizeCurrent(panel.RatedCurrentA, Math.Min(NormalizeCurrent(busCurrentA, panel.RatedCurrentA), panel.RatedCurrentA));
        panel.BreakingCapacityKa = NormalizeBreaking(panel.BreakingCapacityKa > 0 ? panel.BreakingCapacityKa : shortCircuitCurrentKa, 25);
    }

    public static void ApplyMediumVoltageCellPreset(
        MediumVoltageCellConfig cell,
        string productTypeId,
        int busCurrentA,
        double breakingCurrentKa)
    {
        var isKru = productTypeId == ProductTypeIds.Kru;
        var busCurrent = NormalizeCurrent(busCurrentA, isKru ? 1000 : 630);
        var breaking = NormalizeBreaking(breakingCurrentKa, isKru ? 25 : 20);
        var purpose = cell.Purpose.Trim();

        if (purpose.Contains("тн", StringComparison.OrdinalIgnoreCase))
        {
            cell.MainDevice = "ТН";
            cell.DeviceModel = isKru ? "По серии КРУ" : "НАЛИ";
            cell.RatedCurrentA = 630;
            cell.BreakingCurrentKa = 0;
            cell.CtRatio = "";
            cell.CtAccuracyClass = "";
            cell.HasVoltageTransformer = true;
            cell.VoltageTransformerModel = isKru ? "НАМИ/НАЛИ" : "НАЛИ";
            cell.RelayProtection = "";
            cell.RelayTerminal = "";
            cell.HasEarthingSwitch = true;
            cell.WidthMm = isKru ? 800 : 750;
            cell.EstimatedMassKg = isKru ? 780 : 540;
        }
        else if (purpose.Contains("секцион", StringComparison.OrdinalIgnoreCase))
        {
            cell.MainDevice = purpose.Contains("разъедин", StringComparison.OrdinalIgnoreCase) ? "Разъединитель" : "Вакуумный выключатель";
            cell.RatedCurrentA = busCurrent;
            cell.BreakingCurrentKa = cell.MainDevice.Contains("Вакуум", StringComparison.OrdinalIgnoreCase) ? breaking : 0;
            cell.CtRatio = cell.MainDevice.Contains("Вакуум", StringComparison.OrdinalIgnoreCase) ? CtRatioFor(busCurrent) : "";
            cell.CtAccuracyClass = cell.MainDevice.Contains("Вакуум", StringComparison.OrdinalIgnoreCase) ? "10P" : "";
            cell.HasVoltageTransformer = false;
            cell.VoltageTransformerModel = "";
            cell.RelayProtection = cell.MainDevice.Contains("Вакуум", StringComparison.OrdinalIgnoreCase) ? "МТЗ, АВР" : "";
            cell.RelayTerminal = cell.MainDevice.Contains("Вакуум", StringComparison.OrdinalIgnoreCase) ? (isKru ? "БМРЗ/Сириус" : "Сириус-2-Л") : "";
            cell.HasEarthingSwitch = true;
            cell.WidthMm = isKru ? 800 : 750;
            cell.EstimatedMassKg = isKru ? 900 : 720;
        }
        else if (purpose.Contains("ввод", StringComparison.OrdinalIgnoreCase)
            || purpose.Contains("отход", StringComparison.OrdinalIgnoreCase)
            || purpose.Contains("трансформ", StringComparison.OrdinalIgnoreCase))
        {
            cell.MainDevice = isKru || purpose.Contains("отход", StringComparison.OrdinalIgnoreCase) || purpose.Contains("трансформ", StringComparison.OrdinalIgnoreCase)
                ? "Вакуумный выключатель"
                : "ВНА/ВНР/РВЗ";
            cell.RatedCurrentA = purpose.Contains("отход", StringComparison.OrdinalIgnoreCase) || purpose.Contains("трансформ", StringComparison.OrdinalIgnoreCase)
                ? Math.Min(busCurrent, 630)
                : busCurrent;
            cell.BreakingCurrentKa = cell.MainDevice.Contains("Вакуум", StringComparison.OrdinalIgnoreCase) ? breaking : 0;
            cell.CtRatio = cell.MainDevice.Contains("Вакуум", StringComparison.OrdinalIgnoreCase) ? CtRatioFor(cell.RatedCurrentA) : "";
            cell.CtAccuracyClass = cell.MainDevice.Contains("Вакуум", StringComparison.OrdinalIgnoreCase) ? "10P/0,5" : "";
            cell.HasVoltageTransformer = false;
            cell.VoltageTransformerModel = "";
            cell.RelayProtection = cell.MainDevice.Contains("Вакуум", StringComparison.OrdinalIgnoreCase) ? "МТЗ, ТО, ОЗЗ" : "";
            cell.RelayTerminal = cell.MainDevice.Contains("Вакуум", StringComparison.OrdinalIgnoreCase) ? (isKru ? "БМРЗ/Сириус" : "Сириус-2-Л") : "";
            cell.HasEarthingSwitch = true;
            cell.WidthMm = isKru ? 800 : 750;
            cell.EstimatedMassKg = cell.MainDevice.Contains("Вакуум", StringComparison.OrdinalIgnoreCase) ? (isKru ? 850 : 680) : 520;
        }

        ApplyMediumVoltageDevicePreset(cell, productTypeId, busCurrentA, breakingCurrentKa);
    }

    public static void ApplyMediumVoltageDevicePreset(
        MediumVoltageCellConfig cell,
        string productTypeId,
        int busCurrentA,
        double breakingCurrentKa)
    {
        var isKru = productTypeId == ProductTypeIds.Kru;
        var visibleBefore = isKru ? "Выдвижной элемент" : "РВЗ";
        var visibleAfter = isKru ? "Шторочный разрыв" : "РВЗ";

        if (cell.MainDevice.Contains("вакуум", StringComparison.OrdinalIgnoreCase))
        {
            cell.DeviceModel = isKru ? "По серии КРУ" : "ВВ/TEL-10";
            cell.BreakingCurrentKa = NormalizeBreaking(cell.BreakingCurrentKa > 0 ? cell.BreakingCurrentKa : breakingCurrentKa, isKru ? 25 : 20);
            cell.CtRatio = string.IsNullOrWhiteSpace(cell.CtRatio) ? CtRatioFor(cell.RatedCurrentA) : cell.CtRatio;
            cell.CtAccuracyClass = string.IsNullOrWhiteSpace(cell.CtAccuracyClass) ? "10P/0,5" : cell.CtAccuracyClass;
            cell.RelayProtection = string.IsNullOrWhiteSpace(cell.RelayProtection) ? "МТЗ, ТО, ОЗЗ" : cell.RelayProtection;
            cell.RelayTerminal = string.IsNullOrWhiteSpace(cell.RelayTerminal) ? (isKru ? "БМРЗ/Сириус" : "Сириус-2-Л") : cell.RelayTerminal;
            cell.VisibleBreakBefore = visibleBefore;
            cell.VisibleBreakAfter = visibleAfter;
            cell.EquipmentSourceConfidence = "needsVerification";
            cell.EquipmentSourceNotes = "Проверить выключатель, РЗА, ТТ и видимые разрывы по серии ячейки.";
        }
        else if (cell.MainDevice.Contains("вна", StringComparison.OrdinalIgnoreCase)
            || cell.MainDevice.Contains("внр", StringComparison.OrdinalIgnoreCase)
            || cell.MainDevice.Contains("рвз", StringComparison.OrdinalIgnoreCase)
            || cell.MainDevice.Contains("разъедин", StringComparison.OrdinalIgnoreCase)
            || cell.MainDevice.Contains("нагруз", StringComparison.OrdinalIgnoreCase))
        {
            cell.DeviceModel = isKru ? "По серии КРУ" : "По серии КСО";
            cell.BreakingCurrentKa = cell.MainDevice.Contains("нагруз", StringComparison.OrdinalIgnoreCase) ? NormalizeBreaking(cell.BreakingCurrentKa, 20) : 0;
            cell.CtRatio = "";
            cell.CtAccuracyClass = "";
            cell.RelayProtection = "";
            cell.RelayTerminal = "";
            cell.VisibleBreakBefore = visibleBefore;
            cell.VisibleBreakAfter = visibleAfter;
            cell.EquipmentSourceConfidence = "needsVerification";
            cell.EquipmentSourceNotes = "Проверить исполнение аппарата и блокировки по серии ячейки.";
        }
        else if (cell.MainDevice.Equals("ТН", StringComparison.OrdinalIgnoreCase))
        {
            cell.DeviceModel = isKru ? "По серии КРУ" : "НАЛИ";
            cell.HasVoltageTransformer = true;
            cell.VoltageTransformerModel = string.IsNullOrWhiteSpace(cell.VoltageTransformerModel)
                ? (isKru ? "НАМИ/НАЛИ" : "НАЛИ")
                : cell.VoltageTransformerModel;
            cell.EquipmentSourceConfidence = "needsVerification";
            cell.EquipmentSourceNotes = "Проверить тип ТН, предохранители и схему вторичных цепей.";
        }
        else if (cell.MainDevice.Equals("Нет", StringComparison.OrdinalIgnoreCase))
        {
            cell.DeviceModel = "";
            cell.EquipmentSourceConfidence = "userInput";
            cell.EquipmentSourceNotes = "Резервная позиция.";
        }

        cell.RatedCurrentA = Math.Max(0, cell.RatedCurrentA);
        if (cell.WidthMm <= 0) cell.WidthMm = isKru ? 800 : 750;
        if (cell.EstimatedMassKg <= 0) cell.EstimatedMassKg = isKru ? 800 : 520;
    }

    public static IReadOnlyList<LowVoltagePanelConfig> DefaultPanels(string productTypeId) =>
        LowVoltagePanelsForTemplate(productTypeId, DefaultLowVoltageTemplate(productTypeId));

    private static IReadOnlyList<LowVoltagePanelConfig> LowVoltagePanelsForTemplate(string productTypeId, string template) =>
        template switch
        {
            "ВРУ: два ввода + АВР" =>
            [
                new() { Number = 1, SectionNumber = 1, PanelType = "Вводная", Purpose = "Ввод 1", MainDevice = "АВ", RatedCurrentA = 630, BreakingCapacityKa = 25, HasSurgeProtection = true, WidthMm = 800, EstimatedMassKg = 180 },
                new() { Number = 2, SectionNumber = 2, PanelType = "Вводная", Purpose = "Ввод 2", MainDevice = "АВ", RatedCurrentA = 630, BreakingCapacityKa = 25, HasSurgeProtection = true, WidthMm = 800, EstimatedMassKg = 180 },
                new() { Number = 3, SectionNumber = 1, PanelType = "АВР", Purpose = "Автоматическое включение резерва", MainDevice = "АВР", RatedCurrentA = 630, WidthMm = 600, EstimatedMassKg = 165 },
                new() { Number = 4, SectionNumber = 1, PanelType = "Учетная", Purpose = "Коммерческий учет", MainDevice = "Счетчик, ТТ", RatedCurrentA = 630, HasMetering = true, CircuitCount = 1, WidthMm = 600, EstimatedMassKg = 130 },
                new() { Number = 5, SectionNumber = 2, PanelType = "Распределительная", Purpose = "Отходящие линии", MainDevice = "АВ", RatedCurrentA = 250, CircuitCount = 10, WidthMm = 800, EstimatedMassKg = 190 },
            ],
            "ВРУ: учет + распределение" =>
            [
                new() { Number = 1, PanelType = "Учетная", Purpose = "Коммерческий учет", MainDevice = "Счетчик, ТТ", RatedCurrentA = 250, HasMetering = true, CircuitCount = 1, WidthMm = 600, EstimatedMassKg = 115 },
                new() { Number = 2, PanelType = "Распределительная", Purpose = "Отходящие линии", MainDevice = "АВ", RatedCurrentA = 160, CircuitCount = 6, WidthMm = 800, EstimatedMassKg = 150 },
            ],
            "ЩО: одна секция 400 А" =>
            [
                new() { Number = 1, SectionNumber = 1, PanelType = "Вводная", Purpose = "Ввод секции", MainDevice = "АВ", RatedCurrentA = 400, BreakingCapacityKa = 25, HasSurgeProtection = true, WidthMm = 800, EstimatedMassKg = 175 },
                new() { Number = 2, SectionNumber = 1, PanelType = "Линейная", Purpose = "Отходящие линии", MainDevice = "АВ", RatedCurrentA = 250, CircuitCount = 6, WidthMm = 800, EstimatedMassKg = 175 },
            ],
            "ЩО: две секции 1000 А" =>
            [
                new() { Number = 1, SectionNumber = 1, PanelType = "Вводная", Purpose = "Ввод секции 1", MainDevice = "АВ", RatedCurrentA = 1000, BreakingCapacityKa = 31.5, HasSurgeProtection = true, WidthMm = 800, EstimatedMassKg = 220 },
                new() { Number = 2, SectionNumber = 1, PanelType = "Линейная", Purpose = "Отходящие линии секции 1", MainDevice = "АВ", RatedCurrentA = 400, CircuitCount = 8, WidthMm = 800, EstimatedMassKg = 195 },
                new() { Number = 3, SectionNumber = 1, PanelType = "Секционная", Purpose = "Секционирование", MainDevice = "Секционный АВ", RatedCurrentA = 1000, BreakingCapacityKa = 31.5, WidthMm = 800, EstimatedMassKg = 220 },
                new() { Number = 4, SectionNumber = 2, PanelType = "Вводная", Purpose = "Ввод секции 2", MainDevice = "АВ", RatedCurrentA = 1000, BreakingCapacityKa = 31.5, HasSurgeProtection = true, WidthMm = 800, EstimatedMassKg = 220 },
                new() { Number = 5, SectionNumber = 2, PanelType = "Линейная", Purpose = "Отходящие линии секции 2", MainDevice = "АВ", RatedCurrentA = 400, CircuitCount = 8, WidthMm = 800, EstimatedMassKg = 195 },
            ],
            "НКУ: два ввода + секционный аппарат" =>
            [
                new() { Number = 1, SectionNumber = 1, PanelType = "Вводная", Purpose = "Ввод 1", MainDevice = "АВ", RatedCurrentA = 1000, BreakingCapacityKa = 31.5, HasSurgeProtection = true, WidthMm = 800, EstimatedMassKg = 220 },
                new() { Number = 2, SectionNumber = 1, PanelType = "Распределительная", Purpose = "Отходящие линии секции 1", MainDevice = "АВ", RatedCurrentA = 400, CircuitCount = 8, WidthMm = 800, EstimatedMassKg = 190 },
                new() { Number = 3, SectionNumber = 1, PanelType = "Секционная", Purpose = "Секционный аппарат", MainDevice = "Секционный АВ", RatedCurrentA = 1000, BreakingCapacityKa = 31.5, WidthMm = 800, EstimatedMassKg = 220 },
                new() { Number = 4, SectionNumber = 2, PanelType = "Вводная", Purpose = "Ввод 2", MainDevice = "АВ", RatedCurrentA = 1000, BreakingCapacityKa = 31.5, HasSurgeProtection = true, WidthMm = 800, EstimatedMassKg = 220 },
                new() { Number = 5, SectionNumber = 2, PanelType = "Распределительная", Purpose = "Отходящие линии секции 2", MainDevice = "АВ", RatedCurrentA = 400, CircuitCount = 8, WidthMm = 800, EstimatedMassKg = 190 },
                new() { Number = 6, SectionNumber = 1, PanelType = "ЩСН", Purpose = "Собственные нужды и сигнализация", MainDevice = "АВ, ОПС", RatedCurrentA = 100, CircuitCount = 4, WidthMm = 600, EstimatedMassKg = 120 },
            ],
            "НКУ: распределительный шкаф" =>
            [
                new() { Number = 1, PanelType = "Вводная", Purpose = "Ввод", MainDevice = "АВ", RatedCurrentA = 400, BreakingCapacityKa = 25, HasSurgeProtection = true, WidthMm = 600, EstimatedMassKg = 145 },
                new() { Number = 2, PanelType = "Распределительная", Purpose = "Отходящие линии", MainDevice = "АВ", RatedCurrentA = 250, CircuitCount = 10, WidthMm = 800, EstimatedMassKg = 165 },
            ],
            "ВРУ: ввод + учет + отходящие" =>
            [
                new() { Number = 1, PanelType = "Вводная", Purpose = "Ввод и защита", MainDevice = "АВ", RatedCurrentA = 400, BreakingCapacityKa = 25, HasSurgeProtection = true, WidthMm = 800, EstimatedMassKg = 165 },
                new() { Number = 2, PanelType = "Учетная", Purpose = "Коммерческий учет", MainDevice = "Счетчик, ТТ", RatedCurrentA = 400, HasMetering = true, CircuitCount = 1, WidthMm = 600, EstimatedMassKg = 120 },
                new() { Number = 3, PanelType = "Распределительная", Purpose = "Отходящие линии", MainDevice = "АВ", RatedCurrentA = 250, CircuitCount = 8, WidthMm = 800, EstimatedMassKg = 180 },
            ],
            "ЩО: две секции 630 А" =>
            [
                new() { Number = 1, SectionNumber = 1, PanelType = "Вводная", Purpose = "Ввод секции 1", MainDevice = "АВ", RatedCurrentA = 630, BreakingCapacityKa = 31.5, HasSurgeProtection = true, WidthMm = 800, EstimatedMassKg = 190 },
                new() { Number = 2, SectionNumber = 1, PanelType = "Линейная", Purpose = "Отходящие линии секции 1", MainDevice = "АВ", RatedCurrentA = 250, CircuitCount = 6, WidthMm = 800, EstimatedMassKg = 180 },
                new() { Number = 3, SectionNumber = 1, PanelType = "Секционная", Purpose = "Секционирование", MainDevice = "Секционный АВ", RatedCurrentA = 630, BreakingCapacityKa = 31.5, WidthMm = 600, EstimatedMassKg = 170 },
                new() { Number = 4, SectionNumber = 2, PanelType = "Вводная", Purpose = "Ввод секции 2", MainDevice = "АВ", RatedCurrentA = 630, BreakingCapacityKa = 31.5, HasSurgeProtection = true, WidthMm = 800, EstimatedMassKg = 190 },
                new() { Number = 5, SectionNumber = 2, PanelType = "Линейная", Purpose = "Отходящие линии секции 2", MainDevice = "АВ", RatedCurrentA = 250, CircuitCount = 6, WidthMm = 800, EstimatedMassKg = 180 },
            ],
            _ =>
            [
                new() { Number = 1, PanelType = "Вводная", Purpose = "Ввод", MainDevice = "АВ", RatedCurrentA = 630, BreakingCapacityKa = 31.5, HasSurgeProtection = true, WidthMm = 800, EstimatedMassKg = 190 },
                new() { Number = 2, PanelType = "Распределительная", Purpose = "Распределение", MainDevice = "АВ", RatedCurrentA = 400, CircuitCount = 6, WidthMm = 800, EstimatedMassKg = 175 },
                new() { Number = 3, PanelType = "Управление", Purpose = "Собственные нужды и сигнализация", MainDevice = "АВ, ОПС", RatedCurrentA = 100, CircuitCount = 4, WidthMm = 600, EstimatedMassKg = 120 },
            ],
        };

    public static IReadOnlyList<MediumVoltageCellConfig> DefaultCells(string productTypeId) =>
        MediumVoltageCellsForTemplate(productTypeId, DefaultMediumVoltageTemplate(productTypeId));

    private static IReadOnlyList<MediumVoltageCellConfig> MediumVoltageCellsForTemplate(string productTypeId, string template) =>
        template switch
        {
            "КСО: проходная линия + трансформатор" =>
            [
                new() { Number = 1, Purpose = "Ввод линии", MainDevice = "ВНА/ВНР/РВЗ", DeviceModel = "По серии КСО", RatedCurrentA = 630, BreakingCurrentKa = 20, CtRatio = "", CtAccuracyClass = "", RelayProtection = "", RelayTerminal = "", VisibleBreakBefore = "РВЗ", VisibleBreakAfter = "РВЗ", WidthMm = 750, EstimatedMassKg = 520 },
                new() { Number = 2, Purpose = "Вывод линии", MainDevice = "ВНА/ВНР/РВЗ", DeviceModel = "По серии КСО", RatedCurrentA = 630, BreakingCurrentKa = 20, CtRatio = "", CtAccuracyClass = "", RelayProtection = "", RelayTerminal = "", VisibleBreakBefore = "РВЗ", VisibleBreakAfter = "РВЗ", WidthMm = 750, EstimatedMassKg = 520 },
                new() { Number = 3, Purpose = "ТН", MainDevice = "ТН", DeviceModel = "НАЛИ", RatedCurrentA = 630, BreakingCurrentKa = 0, CtRatio = "", CtAccuracyClass = "", HasVoltageTransformer = true, VoltageTransformerModel = "НАЛИ", RelayProtection = "", RelayTerminal = "", VisibleBreakBefore = "РВЗ", VisibleBreakAfter = "РВЗ", WidthMm = 750, EstimatedMassKg = 540 },
                new() { Number = 4, Purpose = "Трансформатор", MainDevice = "Вакуумный выключатель", DeviceModel = "ВВ/TEL-10", RatedCurrentA = 630, BreakingCurrentKa = 20, CtRatio = "600/5", CtAccuracyClass = "10P/0,5", RelayProtection = "МТЗ, ТО, ОЗЗ", RelayTerminal = "Сириус-2-Л", VisibleBreakBefore = "РВЗ", VisibleBreakAfter = "РВЗ", WidthMm = 750, EstimatedMassKg = 680 },
            ],
            "КСО: секционированное РУ" =>
            [
                new() { Number = 1, Purpose = "Ввод 1", MainDevice = "Вакуумный выключатель", DeviceModel = "ВВ/TEL-10", RatedCurrentA = 1000, BreakingCurrentKa = 20, CtRatio = "1000/5", CtAccuracyClass = "10P/0,5", RelayProtection = "МТЗ, ТО, ОЗЗ", RelayTerminal = "Сириус-2-Л", VisibleBreakBefore = "РВЗ", VisibleBreakAfter = "РВЗ", WidthMm = 750, EstimatedMassKg = 720 },
                new() { Number = 2, Purpose = "Секционный выключатель", MainDevice = "Вакуумный выключатель", DeviceModel = "ВВ/TEL-10", RatedCurrentA = 1000, BreakingCurrentKa = 20, CtRatio = "1000/5", CtAccuracyClass = "10P", RelayProtection = "МТЗ, АВР", RelayTerminal = "Сириус-2-Л", VisibleBreakBefore = "РВЗ", VisibleBreakAfter = "РВЗ", WidthMm = 750, EstimatedMassKg = 720 },
                new() { Number = 3, Purpose = "Ввод 2", MainDevice = "Вакуумный выключатель", DeviceModel = "ВВ/TEL-10", RatedCurrentA = 1000, BreakingCurrentKa = 20, CtRatio = "1000/5", CtAccuracyClass = "10P/0,5", RelayProtection = "МТЗ, ТО, ОЗЗ", RelayTerminal = "Сириус-2-Л", VisibleBreakBefore = "РВЗ", VisibleBreakAfter = "РВЗ", WidthMm = 750, EstimatedMassKg = 720 },
                new() { Number = 4, Purpose = "ТН", MainDevice = "ТН", DeviceModel = "НАЛИ", RatedCurrentA = 630, BreakingCurrentKa = 0, CtRatio = "", CtAccuracyClass = "", HasVoltageTransformer = true, VoltageTransformerModel = "НАЛИ", RelayProtection = "", RelayTerminal = "", VisibleBreakBefore = "РВЗ", VisibleBreakAfter = "РВЗ", WidthMm = 750, EstimatedMassKg = 540 },
            ],
            "КРУ: две секции с АВР" =>
            [
                new() { Number = 1, Purpose = "Ввод 1", MainDevice = "Вакуумный выключатель", DeviceModel = "По серии КРУ", RatedCurrentA = 1600, BreakingCurrentKa = 31.5, CtRatio = "1600/5", CtAccuracyClass = "10P/0,5", RelayProtection = "МТЗ, ТО, ОЗЗ, УРОВ", RelayTerminal = "БМРЗ/Сириус", VisibleBreakBefore = "Выдвижной элемент", VisibleBreakAfter = "Шторочный разрыв", WidthMm = 800, EstimatedMassKg = 900 },
                new() { Number = 2, Purpose = "ТН секции 1", MainDevice = "ТН", DeviceModel = "По серии КРУ", RatedCurrentA = 630, BreakingCurrentKa = 0, CtRatio = "", CtAccuracyClass = "", HasVoltageTransformer = true, VoltageTransformerModel = "НАМИ/НАЛИ", RelayProtection = "", RelayTerminal = "", VisibleBreakBefore = "Выдвижной элемент", VisibleBreakAfter = "Шторочный разрыв", WidthMm = 800, EstimatedMassKg = 780 },
                new() { Number = 3, Purpose = "Секционный выключатель", MainDevice = "Вакуумный выключатель", DeviceModel = "По серии КРУ", RatedCurrentA = 1600, BreakingCurrentKa = 31.5, CtRatio = "1600/5", CtAccuracyClass = "10P", RelayProtection = "МТЗ, АВР, УРОВ", RelayTerminal = "БМРЗ/Сириус", VisibleBreakBefore = "Выдвижной элемент", VisibleBreakAfter = "Шторочный разрыв", WidthMm = 800, EstimatedMassKg = 900 },
                new() { Number = 4, Purpose = "Ввод 2", MainDevice = "Вакуумный выключатель", DeviceModel = "По серии КРУ", RatedCurrentA = 1600, BreakingCurrentKa = 31.5, CtRatio = "1600/5", CtAccuracyClass = "10P/0,5", RelayProtection = "МТЗ, ТО, ОЗЗ, УРОВ", RelayTerminal = "БМРЗ/Сириус", VisibleBreakBefore = "Выдвижной элемент", VisibleBreakAfter = "Шторочный разрыв", WidthMm = 800, EstimatedMassKg = 900 },
                new() { Number = 5, Purpose = "Отходящая линия", MainDevice = "Вакуумный выключатель", DeviceModel = "По серии КРУ", RatedCurrentA = 630, BreakingCurrentKa = 31.5, CtRatio = "600/5", CtAccuracyClass = "10P/0,5", RelayProtection = "МТЗ, ТО, ОЗЗ", RelayTerminal = "БМРЗ/Сириус", VisibleBreakBefore = "Выдвижной элемент", VisibleBreakAfter = "Шторочный разрыв", WidthMm = 800, EstimatedMassKg = 850 },
            ],
            "КРУ: ввод + отходящие линии" =>
            [
                new() { Number = 1, Purpose = "Ввод", MainDevice = "Вакуумный выключатель", DeviceModel = "По серии КРУ", RatedCurrentA = 1000, BreakingCurrentKa = 25, CtRatio = "1000/5", CtAccuracyClass = "10P/0,5", RelayProtection = "МТЗ, ТО, ОЗЗ, УРОВ", RelayTerminal = "БМРЗ/Сириус", VisibleBreakBefore = "Выдвижной элемент", VisibleBreakAfter = "Шторочный разрыв", WidthMm = 800, EstimatedMassKg = 850 },
                new() { Number = 2, Purpose = "Отходящая линия 1", MainDevice = "Вакуумный выключатель", DeviceModel = "По серии КРУ", RatedCurrentA = 630, BreakingCurrentKa = 25, CtRatio = "600/5", CtAccuracyClass = "10P/0,5", RelayProtection = "МТЗ, ТО, ОЗЗ", RelayTerminal = "БМРЗ/Сириус", VisibleBreakBefore = "Выдвижной элемент", VisibleBreakAfter = "Шторочный разрыв", WidthMm = 800, EstimatedMassKg = 830 },
                new() { Number = 3, Purpose = "Отходящая линия 2", MainDevice = "Вакуумный выключатель", DeviceModel = "По серии КРУ", RatedCurrentA = 630, BreakingCurrentKa = 25, CtRatio = "600/5", CtAccuracyClass = "10P/0,5", RelayProtection = "МТЗ, ТО, ОЗЗ", RelayTerminal = "БМРЗ/Сириус", VisibleBreakBefore = "Выдвижной элемент", VisibleBreakAfter = "Шторочный разрыв", WidthMm = 800, EstimatedMassKg = 830 },
            ],
            "КСО: ввод + ТН + трансформатор" =>
            [
                new() { Number = 1, Purpose = "Ввод", MainDevice = "ВНА/ВНР/РВЗ", DeviceModel = "По серии КСО", RatedCurrentA = 630, BreakingCurrentKa = 20, CtRatio = "", CtAccuracyClass = "", RelayProtection = "", RelayTerminal = "", VisibleBreakBefore = "РВЗ", VisibleBreakAfter = "РВЗ", WidthMm = 750, EstimatedMassKg = 520 },
                new() { Number = 2, Purpose = "ТН", MainDevice = "ТН", DeviceModel = "НАЛИ", RatedCurrentA = 630, BreakingCurrentKa = 0, CtRatio = "", CtAccuracyClass = "", HasVoltageTransformer = true, VoltageTransformerModel = "НАЛИ", RelayProtection = "", RelayTerminal = "", VisibleBreakBefore = "РВЗ", VisibleBreakAfter = "РВЗ", WidthMm = 750, EstimatedMassKg = 540 },
                new() { Number = 3, Purpose = "Трансформатор", MainDevice = "Вакуумный выключатель", DeviceModel = "ВВ/TEL-10", RatedCurrentA = 630, BreakingCurrentKa = 20, CtRatio = "600/5", CtAccuracyClass = "10P/0,5", RelayProtection = "МТЗ, ТО, ОЗЗ", RelayTerminal = "Сириус-2-Л", VisibleBreakBefore = "РВЗ", VisibleBreakAfter = "РВЗ", WidthMm = 750, EstimatedMassKg = 680 },
            ],
            _ =>
            [
                new() { Number = 1, Purpose = "Ввод", MainDevice = "Вакуумный выключатель", DeviceModel = "По серии КРУ", RatedCurrentA = 1000, BreakingCurrentKa = 25, CtRatio = "1000/5", CtAccuracyClass = "10P/0,5", RelayProtection = "МТЗ, ТО, ОЗЗ, УРОВ", RelayTerminal = "БМРЗ/Сириус", VisibleBreakBefore = "Выдвижной элемент", VisibleBreakAfter = "Шторочный разрыв", WidthMm = 800, EstimatedMassKg = 850 },
                new() { Number = 2, Purpose = "ТН", MainDevice = "ТН", DeviceModel = "По серии КРУ", RatedCurrentA = 630, BreakingCurrentKa = 0, CtRatio = "", CtAccuracyClass = "", HasVoltageTransformer = true, VoltageTransformerModel = "НАМИ/НАЛИ", RelayProtection = "", RelayTerminal = "", VisibleBreakBefore = "Выдвижной элемент", VisibleBreakAfter = "Шторочный разрыв", WidthMm = 800, EstimatedMassKg = 780 },
                new() { Number = 3, Purpose = "Секционный выключатель", MainDevice = "Вакуумный выключатель", DeviceModel = "По серии КРУ", RatedCurrentA = 1000, BreakingCurrentKa = 25, CtRatio = "1000/5", CtAccuracyClass = "10P", RelayProtection = "МТЗ, АВР, УРОВ", RelayTerminal = "БМРЗ/Сириус", VisibleBreakBefore = "Выдвижной элемент", VisibleBreakAfter = "Шторочный разрыв", WidthMm = 800, EstimatedMassKg = 850 },
                new() { Number = 4, Purpose = "Отходящая линия", MainDevice = "Вакуумный выключатель", DeviceModel = "По серии КРУ", RatedCurrentA = 630, BreakingCurrentKa = 25, CtRatio = "600/5", CtAccuracyClass = "10P/0,5", RelayProtection = "МТЗ, ТО, ОЗЗ", RelayTerminal = "БМРЗ/Сириус", VisibleBreakBefore = "Выдвижной элемент", VisibleBreakAfter = "Шторочный разрыв", WidthMm = 800, EstimatedMassKg = 830 },
            ],
        };

    private static void NormalizeEquipmentTrust(LowVoltagePanelConfig panel)
    {
        if (string.IsNullOrWhiteSpace(panel.EquipmentSourceConfidence))
            panel.EquipmentSourceConfidence = "needsVerification";
        if (string.IsNullOrWhiteSpace(panel.EquipmentSourceNotes))
            panel.EquipmentSourceNotes = "Типовой подбор требует проверки по паспорту аппарата.";
    }

    private static void NormalizeEquipmentTrust(MediumVoltageCellConfig cell)
    {
        if (string.IsNullOrWhiteSpace(cell.EquipmentSourceConfidence))
            cell.EquipmentSourceConfidence = "needsVerification";
        if (string.IsNullOrWhiteSpace(cell.EquipmentSourceNotes))
            cell.EquipmentSourceNotes = "Типовая ячейка требует подтверждения по серии и протоколам испытаний.";
    }

    private static int NormalizeCurrent(int value, int fallback)
    {
        if (value <= 0)
            return fallback;

        var ladder = new[] { 100, 160, 250, 400, 630, 800, 1000, 1250, 1600, 2000, 2500, 3200, 4000, 5000, 6300 };
        return ladder.FirstOrDefault(item => value <= item) is var selected && selected > 0
            ? selected
            : value;
    }

    private static double NormalizeBreaking(double value, double fallback) =>
        value > 0 ? value : fallback;

    private static string CtRatioFor(int current)
    {
        if (current <= 50) return "50/5";
        if (current <= 75) return "75/5";
        if (current <= 100) return "100/5";
        if (current <= 150) return "150/5";
        if (current <= 200) return "200/5";
        if (current <= 300) return "300/5";
        if (current <= 400) return "400/5";
        if (current <= 630) return "600/5";
        if (current <= 800) return "800/5";
        if (current <= 1000) return "1000/5";
        if (current <= 1600) return "1600/5";
        return "2000/5";
    }

    private static string NormalizeTemplate(string? template, IReadOnlyList<string> allowed)
    {
        if (!string.IsNullOrWhiteSpace(template))
        {
            var match = allowed.FirstOrDefault(value => value.Equals(template.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
        }

        return allowed[0];
    }

    public static void Renumber(IList<LowVoltagePanelConfig> panels)
    {
        for (var i = 0; i < panels.Count; i++) panels[i].Number = i + 1;
    }

    public static void Renumber(IList<MediumVoltageCellConfig> cells)
    {
        for (var i = 0; i < cells.Count; i++) cells[i].Number = i + 1;
    }
}
