namespace KtpnConfigurator.Core.Models;

/// <summary>
/// Конфигурация проекта. Текущая реализация хранит данные КТПН; ProductTypeId
/// резервирует устойчивую точку расширения для следующих модулей изделий.
/// Сохраняется/загружается как файл проекта (*.ktpn, JSON).
/// </summary>
public sealed class ProjectConfig
{
    public const int CurrentVersion = 9;

    // --- Мета ---
    public int ProjectVersion { get; set; } = CurrentVersion;
    public string ProductTypeId { get; set; } = ProductTypeIds.SingleKtpn;
    public int ProductDataVersion { get; set; } = ProductRegistry.SingleKtpn.CurrentDataVersion;
    public string ProjectName { get; set; } = "Новый проект КТПН";
    public string Author { get; set; } = "";
    public string GridCompany { get; set; } = "";
    public bool ErrorsAcceptedForWork { get; set; }
    public bool AutoElectricalSelection { get; set; } = true;
    public int Revision { get; set; } = 1;
    public DateTimeOffset? LastSavedUtc { get; set; }

    // --- Конфигурации семейств изделий ---
    public DoubleKtpnConfig DoubleKtpn { get; set; } = new();
    public LowVoltageAssemblyConfig LowVoltageAssembly { get; set; } = new();
    public MediumVoltageSwitchgearConfig MediumVoltageSwitchgear { get; set; } = new();
    // Линейки, настроенные для других изделий того же семейства: активные
    // LowVoltageAssembly/MediumVoltageSwitchgear относятся к текущему ProductTypeId,
    // остальные хранятся здесь и подменяются при смене изделия (как ручные габариты).
    public Dictionary<string, LowVoltageAssemblyConfig> LowVoltageAssemblyByProduct { get; set; } = new();
    public Dictionary<string, MediumVoltageSwitchgearConfig> MediumVoltageSwitchgearByProduct { get; set; } = new();

    // --- Трансформатор ---
    public string Manufacturer { get; set; } = "";
    public string Mark { get; set; } = "";

    // --- Конструктив корпуса ---
    public string SteelType { get; set; } = "";
    public double Thickness { get; set; } = 2.0;
    public string FloorMaterial { get; set; } = "Рифленый лист";
    public double FloorThickness { get; set; } = 3.0;
    public string DoorMaterial { get; set; } = "Оцинкованная";
    public double DoorThickness { get; set; } = 2.0;
    public string RemovablePanelMaterial { get; set; } = "Оцинкованная";
    public double RemovablePanelThickness { get; set; } = 2.0;
    public string Channel { get; set; } = "";
    public string BodyColor { get; set; } = "";
    public string DoorColor { get; set; } = "";
    public string RoofColor { get; set; } = "";
    public string BaseColor { get; set; } = "";
    public string InternalPanelColor { get; set; } = "";
    public string LogoColor { get; set; } = "";
    public string ServicePlatformColor { get; set; } = "";
    public string BusbarHvMaterial { get; set; } = "Алюминий";
    public string BusbarLvMaterial { get; set; } = "Алюминий";
    public string BusbarNMaterial { get; set; } = "Алюминий";
    public string ClimateExecution { get; set; } = "У1";
    public string ProtectionDegree { get; set; } = "IP54";
    public string DoorConfiguration { get; set; } = "Двухстворчатые распашные";
    public string RuvnDoorConfiguration { get; set; } = "Двухстворчатые распашные";
    public string RunnDoorConfiguration { get; set; } = "Двухстворчатые распашные";
    public string TransformerDoorConfiguration { get; set; } = "Распашные с двух сторон";
    public string LockType { get; set; } = "Ригельный замок с фиксацией в трех точках";
    public bool HasRigelLock { get; set; } = true;
    public string NetworkLockType { get; set; } = "Россети";
    public bool HasPadlockProvision { get; set; }
    public bool HasGrounding { get; set; } = true;
    public string GroundingType { get; set; } = "Контур заземления";
    public string VentilationType { get; set; } = "Естественная";
    public bool HasRoofDeflector { get; set; } = true;
    public bool HasNameplate { get; set; } = true;
    public bool HasDoorCanopies { get; set; }
    public bool HasDoorSeals { get; set; } = true;
    public bool HasTransformerMeshDoors { get; set; }
    public bool HasLouverAnimalProtection { get; set; } = true;
    public bool HasAntiVandalHinges { get; set; }
    public bool HasDoorSealing { get; set; }
    public bool HasServicePlatform { get; set; }
    public bool HasLogo { get; set; } = true;
    public string LogoPlacement { get; set; } = "По ТЗ";
    public bool HasWarningLabels { get; set; } = true;
    public bool HasDispatcherNameplate { get; set; }
    public bool HasFeederLabels { get; set; } = true;
    public string MarkingNotes { get; set; } = "";
    public string EnclosureNotes { get; set; } = "";

    // --- Габаритные настройки отсеков ---
    public double LenRuvn { get; set; } = 1300;
    public double LenRunn { get; set; } = 600;
    public double TransformerTolerance { get; set; } = 300;
    public double LengthBuffer { get; set; } = 10;

    // --- Ввод РУВН ---
    public string Voltage { get; set; } = "";
    public string RuvnType { get; set; } = "";
    public string RuvnSwitch { get; set; } = "";
    public int RuvnSwitchNominal { get; set; }
    public string FuseType { get; set; } = "";
    public string FuseNominal { get; set; } = "";
    public string RuvnExecution { get; set; } = "";
    public bool RuvnSurgeArrester { get; set; }
    public string RuvnSurgeArresterLocation { get; set; } = "";
    public int RuvnSurgeArresterDischargeCurrentKa { get; set; } = 5;
    public string RuvnSurgeArresterThroughput { get; set; } = "Класс пропускной способности 2";
    public string RuvnIncomingSwitch { get; set; } = "";
    public int RuvnIncomingSwitchNominal { get; set; }
    public bool RuvnIncomingFuseOn { get; set; }
    public string RuvnIncomingFuseType { get; set; } = "";
    public string RuvnIncomingFuseNominal { get; set; } = "";
    public string RuvnOutgoingSwitch { get; set; } = "";
    public int RuvnOutgoingSwitchNominal { get; set; }
    public bool RuvnOutgoingFuseOn { get; set; }
    public string RuvnOutgoingFuseType { get; set; } = "";
    public string RuvnOutgoingFuseNominal { get; set; } = "";
    public string RuvnTransformerSwitch { get; set; } = "";
    public int RuvnTransformerSwitchNominal { get; set; }
    public bool RuvnTransformerFuseOn { get; set; }
    public string RuvnTransformerFuseType { get; set; } = "";
    public string RuvnTransformerFuseNominal { get; set; } = "";
    public RuvnBranchEquipmentConfig RuvnIncomingEquipment { get; set; } = new();
    public RuvnBranchEquipmentConfig RuvnOutgoingEquipment { get; set; } = new();
    public RuvnBranchEquipmentConfig RuvnTransformerEquipment { get; set; } = new();

    // --- Ввод РУНН ---
    public bool PvrOn { get; set; }
    public int PvrNominal { get; set; }
    public string PvrManufacturer { get; set; } = "";
    public bool ReOn { get; set; }
    public int ReNominal { get; set; }
    public string ReManufacturer { get; set; } = "";
    public bool AvInOn { get; set; }
    public int AvInNominal { get; set; }
    public string AvInManufacturer { get; set; } = "";
    public bool RunnSurgeArrester { get; set; }
    public bool HasCt { get; set; }
    public string CtRatio { get; set; } = "";
    public string CtAccuracyClass { get; set; } = "0,5S";
    public bool HasCtKip { get; set; }
    public string CtKipRatio { get; set; } = "";
    public string CtKipAccuracyClass { get; set; } = "0,5";
    public bool HasMeter { get; set; }

    // --- Отходящие линии РУНН ---
    public bool AvOn { get; set; }
    public int AvQty { get; set; }
    public string AvBrand { get; set; } = "";
    public bool RpsOn { get; set; }
    public int RpsQty { get; set; }
    public string RpsBrand { get; set; } = "";
    public List<OutgoingFeederConfig> OutgoingFeeders { get; set; } = new();
    public string OutgoingExecution { get; set; } = "";

    // --- Собственные нужды, освещение, РИСЭ ---
    public AuxiliaryNeedsConfig AuxiliaryNeeds { get; set; } = new();

    // --- Ручное переопределение габаритов (null = не задано / ISNUMBER=ЛОЖЬ) ---
    // Действующие значения относятся к текущему ProductTypeId; значения остальных
    // изделий хранятся в ManualOverridesByProduct и подменяются при смене изделия.
    public double? ManualLength { get; set; }
    public double? ManualWidth { get; set; }
    public double? ManualHeight { get; set; }
    public double? ManualBaseMass { get; set; }
    public double? ManualBodyMass { get; set; }
    public double? ManualGrossMass { get; set; }
    public Dictionary<string, ManualDimensionOverrides> ManualOverridesByProduct { get; set; } = new();

    public string Notes { get; set; } = "";

    /// <summary>
    /// Переносит ручные переопределения при смене изделия: текущие значения
    /// запоминаются за прежним типом, для нового восстанавливаются его собственные.
    /// Без этого длина, заданная вручную для КТПН, молча применялась бы к НКУ.
    /// </summary>
    public void SwitchManualOverrides(string? fromProductTypeId, string? toProductTypeId)
    {
        ManualOverridesByProduct ??= new Dictionary<string, ManualDimensionOverrides>();
        if (!string.IsNullOrWhiteSpace(fromProductTypeId))
        {
            var stash = new ManualDimensionOverrides
            {
                Length = ManualLength,
                Width = ManualWidth,
                Height = ManualHeight,
                BaseMass = ManualBaseMass,
                BodyMass = ManualBodyMass,
                GrossMass = ManualGrossMass,
            };
            if (stash.IsEmpty)
                ManualOverridesByProduct.Remove(fromProductTypeId);
            else
                ManualOverridesByProduct[fromProductTypeId] = stash;
        }

        ManualDimensionOverrides? restored = null;
        if (!string.IsNullOrWhiteSpace(toProductTypeId))
            ManualOverridesByProduct.TryGetValue(toProductTypeId, out restored);
        ManualLength = restored?.Length;
        ManualWidth = restored?.Width;
        ManualHeight = restored?.Height;
        ManualBaseMass = restored?.BaseMass;
        ManualBodyMass = restored?.BodyMass;
        ManualGrossMass = restored?.GrossMass;
    }

    /// <summary>
    /// Переносит линейки панелей/ячеек при смене изделия: текущая линейка
    /// запоминается за прежним типом, для нового восстанавливается его собственная.
    /// Без этого настроенная линейка НКУ молча перезаписывалась типовым шаблоном
    /// при любом уходе на другое изделие и возврате.
    /// </summary>
    public void SwitchProductLineups(string? fromProductTypeId, string? toProductTypeId)
    {
        LowVoltageAssemblyByProduct ??= new Dictionary<string, LowVoltageAssemblyConfig>(StringComparer.OrdinalIgnoreCase);
        MediumVoltageSwitchgearByProduct ??= new Dictionary<string, MediumVoltageSwitchgearConfig>(StringComparer.OrdinalIgnoreCase);

        if (IsLowVoltageProduct(fromProductTypeId) && LowVoltageAssembly is not null)
            LowVoltageAssemblyByProduct[fromProductTypeId!] = LowVoltageAssembly;
        if (IsMediumVoltageProduct(fromProductTypeId) && MediumVoltageSwitchgear is not null)
            MediumVoltageSwitchgearByProduct[fromProductTypeId!] = MediumVoltageSwitchgear;

        if (IsLowVoltageProduct(toProductTypeId))
        {
            if (LowVoltageAssemblyByProduct.TryGetValue(toProductTypeId!, out var storedLv))
            {
                LowVoltageAssembly = storedLv;
                LowVoltageAssemblyByProduct.Remove(toProductTypeId!);
            }
            else
            {
                // Первый вход в изделие: пустая заготовка, типовой состав
                // применит ProductConfigurationDefaults.Normalize.
                LowVoltageAssembly = new LowVoltageAssemblyConfig { LineupTemplate = "" };
                LowVoltageAssembly.Panels.Clear();
            }
        }

        if (IsMediumVoltageProduct(toProductTypeId))
        {
            if (MediumVoltageSwitchgearByProduct.TryGetValue(toProductTypeId!, out var storedMv))
            {
                MediumVoltageSwitchgear = storedMv;
                MediumVoltageSwitchgearByProduct.Remove(toProductTypeId!);
            }
            else
            {
                MediumVoltageSwitchgear = new MediumVoltageSwitchgearConfig { LineupTemplate = "" };
                MediumVoltageSwitchgear.Cells.Clear();
            }
        }
    }

    private static bool IsLowVoltageProduct(string? productTypeId) =>
        productTypeId is ProductTypeIds.Nku or ProductTypeIds.Shcho or ProductTypeIds.Vru;

    private static bool IsMediumVoltageProduct(string? productTypeId) =>
        productTypeId is ProductTypeIds.Kso or ProductTypeIds.Kru;

    public ProjectConfig Clone()
    {
        var clone = (ProjectConfig)MemberwiseClone();
        clone.ManualOverridesByProduct = (ManualOverridesByProduct ?? new Dictionary<string, ManualDimensionOverrides>())
            .ToDictionary(pair => pair.Key, pair => pair.Value.Clone());
        clone.LowVoltageAssemblyByProduct = (LowVoltageAssemblyByProduct ?? new Dictionary<string, LowVoltageAssemblyConfig>())
            .ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        clone.MediumVoltageSwitchgearByProduct = (MediumVoltageSwitchgearByProduct ?? new Dictionary<string, MediumVoltageSwitchgearConfig>())
            .ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        clone.AuxiliaryNeeds = AuxiliaryNeeds?.Clone() ?? new AuxiliaryNeedsConfig();
        clone.DoubleKtpn = DoubleKtpn?.Clone() ?? new DoubleKtpnConfig();
        clone.LowVoltageAssembly = LowVoltageAssembly?.Clone() ?? new LowVoltageAssemblyConfig();
        clone.MediumVoltageSwitchgear = MediumVoltageSwitchgear?.Clone() ?? new MediumVoltageSwitchgearConfig();
        clone.RuvnIncomingEquipment = RuvnIncomingEquipment?.Clone() ?? new RuvnBranchEquipmentConfig();
        clone.RuvnOutgoingEquipment = RuvnOutgoingEquipment?.Clone() ?? new RuvnBranchEquipmentConfig();
        clone.RuvnTransformerEquipment = RuvnTransformerEquipment?.Clone() ?? new RuvnBranchEquipmentConfig();
        clone.OutgoingFeeders = (OutgoingFeeders ?? new List<OutgoingFeederConfig>())
            .Select(f => f.Clone())
            .ToList();
        return clone;
    }
}

public sealed class ManualDimensionOverrides
{
    public double? Length { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public double? BaseMass { get; set; }
    public double? BodyMass { get; set; }
    public double? GrossMass { get; set; }

    public bool IsEmpty =>
        Length is null && Width is null && Height is null
        && BaseMass is null && BodyMass is null && GrossMass is null;

    public ManualDimensionOverrides Clone() => (ManualDimensionOverrides)MemberwiseClone();
}

public sealed class RuvnBranchEquipmentConfig
{
    public string VisibleBreakBefore { get; set; } = "РВЗ";
    public string VisibleBreakAfter { get; set; } = "РВЗ";
    public string EarthingSwitch { get; set; } = "По схеме ячейки";
    public string VacuumBreakerModel { get; set; } = "ВВ/TEL-10";
    public int VacuumBreakerNominal { get; set; } = 630;
    public double VacuumBreakerBreakingCurrentKa { get; set; } = 20;
    public string VacuumBreakerDrive { get; set; } = "Блок управления";
    public string VacuumBreakerInstallation { get; set; } = "Стационарный";
    public string OperationalPower { get; set; } = "220 В AC";
    public string RzaTerminal { get; set; } = "Сириус-2-Л";
    public bool RzaMtz { get; set; } = true;
    public bool RzaCurrentCutoff { get; set; } = true;
    public bool RzaGroundFault { get; set; } = true;
    public bool RzaOverload { get; set; } = true;
    public bool RzaUrov { get; set; }
    public bool RzaLzsh { get; set; }
    public bool RzaApv { get; set; }
    public bool RzaAvr { get; set; }
    public bool RzaArcProtection { get; set; }
    public bool RzaTransformerGas { get; set; }
    public string ProtectionCtRatio { get; set; } = "600/5";
    public int ProtectionCtQuantity { get; set; } = 3;
    public bool HasTtnp { get; set; }
    public string TtnpModel { get; set; } = "";
    public bool HasVoltageTransformer { get; set; }
    public string VoltageTransformerModel { get; set; } = "НАЛИ";
    public string Notes { get; set; } = "";

    public RuvnBranchEquipmentConfig Clone() =>
        (RuvnBranchEquipmentConfig)MemberwiseClone();
}

public sealed class OutgoingFeederConfig
{
    public int Number { get; set; }
    public int SectionNumber { get; set; } = 1;
    public string DeviceType { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Model { get; set; } = "";
    public int Nominal { get; set; }
    public bool IsReserve { get; set; }
    public string CableMark { get; set; } = "";
    public string CableSection { get; set; } = "";
    public string MeteringType { get; set; } = "";
    public string TtRatio { get; set; } = "";
    public bool HasMeter { get; set; }
    public string Notes { get; set; } = "";

    public OutgoingFeederConfig Clone() =>
        (OutgoingFeederConfig)MemberwiseClone();
}
