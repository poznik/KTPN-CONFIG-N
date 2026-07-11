using System.IO;
using System.Text.Json;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.App.Services;

/// <summary>Сохранение/загрузка проекта КТПН в JSON (*.ktpn).</summary>
public static class ProjectStorage
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static void Save(ProjectConfig cfg, string path)
    {
        cfg.ProjectVersion = ProjectConfig.CurrentVersion;
        var json = JsonSerializer.Serialize(cfg, Opts);
        File.WriteAllText(path, json);
    }

    public static ProjectConfig Load(string path)
    {
        var json = File.ReadAllText(path);
        var sourceVersion = ReadProjectVersion(json);
        var cfg = JsonSerializer.Deserialize<ProjectConfig>(json, Opts) ?? new ProjectConfig();
        Migrate(cfg, sourceVersion);
        return cfg;
    }

    private static int ReadProjectVersion(string json)
    {
        using var document = JsonDocument.Parse(json);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Name.Equals(nameof(ProjectConfig.ProjectVersion), StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.Number
                && property.Value.TryGetInt32(out var version))
            {
                return version;
            }
        }

        return 0;
    }

    private static void Migrate(ProjectConfig cfg, int sourceVersion)
    {
        if (sourceVersion <= 0)
        {
            cfg.AuxiliaryNeeds ??= new AuxiliaryNeedsConfig();
            cfg.OutgoingFeeders ??= new List<OutgoingFeederConfig>();
        }

        cfg.AuxiliaryNeeds ??= new AuxiliaryNeedsConfig();
        cfg.OutgoingFeeders ??= new List<OutgoingFeederConfig>();
        if (string.IsNullOrWhiteSpace(cfg.BusbarHvMaterial))
            cfg.BusbarHvMaterial = "Алюминий";
        if (string.IsNullOrWhiteSpace(cfg.BusbarLvMaterial))
            cfg.BusbarLvMaterial = "Алюминий";
        if (string.IsNullOrWhiteSpace(cfg.BusbarNMaterial))
            cfg.BusbarNMaterial = cfg.BusbarLvMaterial;
        MigrateEnclosure(cfg);
        cfg.AvQty = Math.Clamp(cfg.AvQty, 0, 20);
        cfg.RpsQty = Math.Clamp(cfg.RpsQty, 0, 8);
        MigrateOutgoingFeeders(cfg);
        MigrateRuvn(cfg);
        MigrateAuxiliaryNeeds(cfg.AuxiliaryNeeds);
        MigrateProfileText(cfg);
        cfg.ProjectVersion = ProjectConfig.CurrentVersion;
    }

    private static void MigrateEnclosure(ProjectConfig cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.ClimateExecution))
            cfg.ClimateExecution = "У1";
        if (string.IsNullOrWhiteSpace(cfg.ProtectionDegree))
            cfg.ProtectionDegree = "IP54";
        if (string.IsNullOrWhiteSpace(cfg.RoofColor))
            cfg.RoofColor = cfg.BodyColor;
        if (string.IsNullOrWhiteSpace(cfg.FloorMaterial))
            cfg.FloorMaterial = "Рифленый лист";
        if (cfg.FloorThickness <= 0)
            cfg.FloorThickness = 3.0;
        if (string.IsNullOrWhiteSpace(cfg.DoorMaterial))
            cfg.DoorMaterial = string.IsNullOrWhiteSpace(cfg.SteelType) ? "Оцинкованная" : cfg.SteelType;
        if (cfg.DoorThickness <= 0)
            cfg.DoorThickness = cfg.Thickness > 0 ? cfg.Thickness : 2.0;
        if (string.IsNullOrWhiteSpace(cfg.RemovablePanelMaterial))
            cfg.RemovablePanelMaterial = cfg.DoorMaterial;
        if (cfg.RemovablePanelThickness <= 0)
            cfg.RemovablePanelThickness = cfg.DoorThickness > 0 ? cfg.DoorThickness : 2.0;
        if (string.IsNullOrWhiteSpace(cfg.BaseColor))
            cfg.BaseColor = cfg.BodyColor;
        if (string.IsNullOrWhiteSpace(cfg.InternalPanelColor))
            cfg.InternalPanelColor = cfg.BodyColor;
        if (string.IsNullOrWhiteSpace(cfg.LogoColor))
            cfg.LogoColor = "По ТЗ";
        if (string.IsNullOrWhiteSpace(cfg.ServicePlatformColor))
            cfg.ServicePlatformColor = cfg.BodyColor;
        if (string.IsNullOrWhiteSpace(cfg.DoorConfiguration))
            cfg.DoorConfiguration = "Двухстворчатые распашные";
        if (string.IsNullOrWhiteSpace(cfg.RuvnDoorConfiguration))
            cfg.RuvnDoorConfiguration = cfg.DoorConfiguration;
        if (string.IsNullOrWhiteSpace(cfg.RunnDoorConfiguration))
            cfg.RunnDoorConfiguration = cfg.DoorConfiguration;
        if (string.IsNullOrWhiteSpace(cfg.TransformerDoorConfiguration))
            cfg.TransformerDoorConfiguration = "Распашные с двух сторон";
        if (string.IsNullOrWhiteSpace(cfg.LockType))
            cfg.LockType = "Ригельный замок с фиксацией в трех точках";
        if (!cfg.HasRigelLock && cfg.LockType.Contains("ригель", StringComparison.OrdinalIgnoreCase))
            cfg.HasRigelLock = true;
        if (string.IsNullOrWhiteSpace(cfg.NetworkLockType))
            cfg.NetworkLockType = cfg.LockType.Contains("сетев", StringComparison.OrdinalIgnoreCase)
                ? "Универсальный"
                : "Россети";
        if (cfg.LockType.Contains("навес", StringComparison.OrdinalIgnoreCase))
            cfg.HasPadlockProvision = true;
        cfg.HasGrounding = true;
        if (string.IsNullOrWhiteSpace(cfg.GroundingType)
            || cfg.GroundingType.Equals("Болт заземления на корпусе", StringComparison.OrdinalIgnoreCase))
        {
            cfg.GroundingType = "Контур заземления";
        }
        if (string.IsNullOrWhiteSpace(cfg.VentilationType))
            cfg.VentilationType = "Естественная";
        if (cfg.VentilationType.Equals("Жалюзийные решетки", StringComparison.OrdinalIgnoreCase)
            || cfg.VentilationType.Equals("Естественная вентиляция", StringComparison.OrdinalIgnoreCase)
            || cfg.VentilationType.Equals("Без вентиляции", StringComparison.OrdinalIgnoreCase))
        {
            cfg.VentilationType = "Естественная";
        }
        if (string.IsNullOrWhiteSpace(cfg.LogoPlacement))
            cfg.LogoPlacement = "По ТЗ";
        if (cfg.LogoPlacement.Equals("Фирменный блок по макету заказчика", StringComparison.OrdinalIgnoreCase))
            cfg.LogoPlacement = "Фирменный блок по макету";
    }

    private static void MigrateProfileText(ProjectConfig cfg)
    {
        cfg.EnclosureNotes = CleanKnownProfileText(cfg.EnclosureNotes);
        cfg.MarkingNotes = CleanKnownProfileText(cfg.MarkingNotes);
        if (cfg.AuxiliaryNeeds is not null)
            cfg.AuxiliaryNeeds.Notes = CleanKnownProfileText(cfg.AuxiliaryNeeds.Notes);
    }

    private static string CleanKnownProfileText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value ?? "";

        var result = value;
        foreach (var prefix in new[]
        {
            "Профиль Россети/МРСК:",
            "Профиль ПАО Россети Урал:",
            "Профиль АО МРСК:",
            "Профиль АО ЕЭСК:",
            "Профиль ЛУКОЙЛ:",
            "Профиль АО Облкоммунэнерго:",
            "Профиль РЖД:",
            "Профиль АО ЮРЭСК:",
            "Профиль без логотипа:",
        })
        {
            result = result.Replace(prefix, "", StringComparison.OrdinalIgnoreCase);
        }

        return result
            .Replace("по макету заказчика", "по макету", StringComparison.OrdinalIgnoreCase)
            .Replace("по требованиям Россети Урал", "по актуальному макету", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static void MigrateOutgoingFeeders(ProjectConfig cfg)
    {
        foreach (var feeder in cfg.OutgoingFeeders ?? new List<OutgoingFeederConfig>())
        {
            if (string.IsNullOrWhiteSpace(feeder.MeteringType))
                feeder.MeteringType = feeder.HasMeter ? "Коммерческий" : "Нет";

            feeder.HasMeter = !feeder.MeteringType.Equals("Нет", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void MigrateRuvn(ProjectConfig cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.RuvnTransformerSwitch))
            cfg.RuvnTransformerSwitch = cfg.RuvnSwitch;
        if (cfg.RuvnTransformerSwitchNominal <= 0)
            cfg.RuvnTransformerSwitchNominal = cfg.RuvnSwitchNominal;
        if (string.IsNullOrWhiteSpace(cfg.RuvnTransformerFuseType))
            cfg.RuvnTransformerFuseType = cfg.FuseType;
        if (string.IsNullOrWhiteSpace(cfg.RuvnTransformerFuseNominal))
            cfg.RuvnTransformerFuseNominal = cfg.FuseNominal;
        if (RuvnEngineering.HasDevice(cfg.RuvnTransformerFuseType) || RuvnEngineering.HasDevice(cfg.RuvnTransformerFuseNominal))
            cfg.RuvnTransformerFuseOn = true;

        if (string.IsNullOrWhiteSpace(cfg.RuvnSurgeArresterLocation))
        {
            cfg.RuvnSurgeArresterLocation = cfg.RuvnSurgeArrester
                ? RuvnEngineering.SurgeArresterAtTransformer
                : RuvnEngineering.NoSurgeArrester;
        }

        cfg.RuvnSurgeArrester = !cfg.RuvnSurgeArresterLocation.Equals(
            RuvnEngineering.NoSurgeArrester,
            StringComparison.OrdinalIgnoreCase);

        if (cfg.RuvnSurgeArresterDischargeCurrentKa <= 0)
            cfg.RuvnSurgeArresterDischargeCurrentKa = 5;
        if (string.IsNullOrWhiteSpace(cfg.RuvnSurgeArresterThroughput))
        {
            cfg.RuvnSurgeArresterThroughput = cfg.RuvnSurgeArresterDischargeCurrentKa >= 10
                ? "Класс пропускной способности 2"
                : "Класс пропускной способности 1";
        }

        cfg.RuvnIncomingEquipment ??= new RuvnBranchEquipmentConfig();
        cfg.RuvnOutgoingEquipment ??= new RuvnBranchEquipmentConfig();
        cfg.RuvnTransformerEquipment ??= new RuvnBranchEquipmentConfig();
        MigrateRuvnEquipment(cfg.RuvnIncomingEquipment, cfg.RuvnIncomingSwitchNominal);
        MigrateRuvnEquipment(cfg.RuvnOutgoingEquipment, cfg.RuvnOutgoingSwitchNominal);
        MigrateRuvnEquipment(cfg.RuvnTransformerEquipment, cfg.RuvnTransformerSwitchNominal);

        if (RuvnEngineering.IsVacuumBreaker(cfg.RuvnIncomingSwitch))
            cfg.RuvnIncomingFuseOn = false;
        if (RuvnEngineering.IsVacuumBreaker(cfg.RuvnOutgoingSwitch))
            cfg.RuvnOutgoingFuseOn = false;
        if (RuvnEngineering.IsVacuumBreaker(cfg.RuvnTransformerSwitch))
        {
            cfg.RuvnTransformerFuseOn = false;
            cfg.RuvnSwitch = cfg.RuvnTransformerSwitch;
            cfg.RuvnSwitchNominal = cfg.RuvnTransformerSwitchNominal;
            cfg.FuseType = cfg.RuvnTransformerFuseType;
            cfg.FuseNominal = cfg.RuvnTransformerFuseNominal;
        }
    }

    private static void MigrateRuvnEquipment(RuvnBranchEquipmentConfig equipment, int branchNominal)
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

    private static void MigrateAuxiliaryNeeds(AuxiliaryNeedsConfig aux)
    {
        if (string.IsNullOrWhiteSpace(aux.OpsType))
            aux.OpsType = "Комбинированная";
        if (aux.OpsLoops <= 0)
            aux.OpsLoops = 1;
        if (string.IsNullOrWhiteSpace(aux.LightingAreas))
            aux.LightingAreas = "РУВН, РУНН, трансформаторный отсек";
        if (aux.RepairLightingVoltage <= 0)
            aux.RepairLightingVoltage = 12;
    }
}
