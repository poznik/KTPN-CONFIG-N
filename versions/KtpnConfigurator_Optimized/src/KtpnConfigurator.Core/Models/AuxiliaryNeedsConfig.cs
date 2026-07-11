using System.Text.Json.Serialization;

namespace KtpnConfigurator.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LightingControlType
{
    Manual,
    PhotoRelay,
    AstroTimer,
    TimeRelay,
    Combined,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RiseType
{
    UPS,
    Battery,
    BackupInput,
}

public sealed class AuxiliaryNeedsConfig
{
    public bool Enabled { get; set; }
    public bool SupplyFromRunn { get; set; } = true;
    public string CabinetManufacturer { get; set; } = "";
    public string CabinetModel { get; set; } = "";
    public string MainBreakerManufacturer { get; set; } = "";
    public string MainBreakerModel { get; set; } = "";
    public int MainBreakerNominal { get; set; }

    public bool LightingEnabled { get; set; }
    public int LightingCircuits { get; set; } = 1;
    public string LightingBreakerManufacturer { get; set; } = "";
    public string LightingBreakerModel { get; set; } = "";
    public int LightingBreakerNominal { get; set; }
    public string LightingControlMode { get; set; } = "Ручной";
    public string LightingFixtureModel { get; set; } = "";
    public int LightingFixtureQuantity { get; set; }
    public string LightingAreas { get; set; } = "РУВН, РУНН, трансформаторный отсек";
    public int RepairLightingVoltage { get; set; } = 12;
    public bool OutdoorLightingEnabled { get; set; }
    public string PhotoRelayModel { get; set; } = "";
    public string AstroTimerModel { get; set; } = "";
    public string TimeRelayModel { get; set; } = "";

    public bool SocketEnabled { get; set; }
    public string SocketBreakerManufacturer { get; set; } = "";
    public string SocketBreakerModel { get; set; } = "";
    public int SocketBreakerNominal { get; set; }
    public string SocketModel { get; set; } = "";
    public int SocketQuantity { get; set; } = 1;

    public bool HeatingEnabled { get; set; }
    public string HeatingBreakerManufacturer { get; set; } = "";
    public string HeatingBreakerModel { get; set; } = "";
    public int HeatingBreakerNominal { get; set; }
    public string HeaterModel { get; set; } = "";
    public int HeaterQuantity { get; set; } = 1;
    public string ThermostatModel { get; set; } = "";
    public bool MeterHeatingEnabled { get; set; }

    public bool VentilationEnabled { get; set; }
    public string VentilationBreakerManufacturer { get; set; } = "";
    public string VentilationBreakerModel { get; set; } = "";
    public int VentilationBreakerNominal { get; set; }
    public string FanModel { get; set; } = "";
    public int FanQuantity { get; set; } = 1;

    public bool OpsEnabled { get; set; }
    public string OpsType { get; set; } = "Комбинированная";
    public string OpsManufacturer { get; set; } = "";
    public string OpsModel { get; set; } = "";
    public int OpsLoops { get; set; } = 1;

    public bool RieseEnabled { get; set; }
    public string RieseType { get; set; } = "";
    public string RieseSupply { get; set; } = "ЩСН";
    public string RieseProtectedCircuits { get; set; } = "";
    public int RiesePowerVa { get; set; }
    public int RieseAutonomyMinutes { get; set; }
    public string RieseProtectionManufacturer { get; set; } = "";
    public string RieseProtectionModel { get; set; } = "";
    public string RieseModuleModel { get; set; } = "";
    public string Notes { get; set; } = "";

    // Алиасы для кода и старых файлов. Исключены из сериализации: раньше в JSON
    // попадали оба представления, и при загрузке канонизированный алиас перетирал
    // свободный текст пользователя (например, «ИБП offline 600 ВА» → «ИБП с АКБ»).
    [JsonIgnore]
    public bool HasAuxiliaryCabinet
    {
        get => Enabled;
        set => Enabled = value;
    }

    [JsonIgnore]
    public bool HasLighting
    {
        get => LightingEnabled;
        set => LightingEnabled = value;
    }

    [JsonIgnore]
    public LightingControlType LightingControlType
    {
        get => ParseLightingControlType(LightingControlMode);
        set => LightingControlMode = ToDisplayName(value);
    }

    [JsonIgnore]
    public bool HasRise
    {
        get => RieseEnabled;
        set => RieseEnabled = value;
    }

    [JsonIgnore]
    public RiseType RiseType
    {
        get => ParseRiseType(RieseType);
        set => RieseType = ToDisplayName(value);
    }

    // Приём файлов, записанных до формата 8, где алиасы попадали в JSON.
    // Свойства write-only: читаются при десериализации, но не сериализуются.
    // Строковые варианты уступают каноническому полю, если оно уже заполнено.
    [JsonPropertyName("HasAuxiliaryCabinet")]
    public bool LegacyHasAuxiliaryCabinet { set => Enabled = value; }

    [JsonPropertyName("HasLighting")]
    public bool LegacyHasLighting { set => LightingEnabled = value; }

    [JsonPropertyName("HasRise")]
    public bool LegacyHasRise { set => RieseEnabled = value; }

    [JsonPropertyName("LightingControlType")]
    public LightingControlType LegacyLightingControlType
    {
        set
        {
            if (value != LightingControlType.Manual
                && (string.IsNullOrWhiteSpace(LightingControlMode) || LightingControlMode == "Ручной"))
                LightingControlMode = ToDisplayName(value);
        }
    }

    [JsonPropertyName("RiseType")]
    public RiseType LegacyRiseType
    {
        set
        {
            if (string.IsNullOrWhiteSpace(RieseType))
                RieseType = ToDisplayName(value);
        }
    }

    public AuxiliaryNeedsConfig Clone() =>
        (AuxiliaryNeedsConfig)MemberwiseClone();

    private static LightingControlType ParseLightingControlType(string? value)
    {
        if (Enum.TryParse<LightingControlType>(value, ignoreCase: true, out var parsed))
            return parsed;

        if (Contains(value, "фото"))
            return LightingControlType.PhotoRelay;
        if (Contains(value, "астро"))
            return LightingControlType.AstroTimer;
        if (Contains(value, "врем"))
            return LightingControlType.TimeRelay;
        if (Contains(value, "авто") || Contains(value, "комб"))
            return LightingControlType.Combined;

        return LightingControlType.Manual;
    }

    private static RiseType ParseRiseType(string? value)
    {
        if (Enum.TryParse<RiseType>(value, ignoreCase: true, out var parsed))
            return parsed;

        if (Contains(value, "ибп") || Contains(value, "ups"))
            return RiseType.UPS;
        if (Contains(value, "резерв"))
            return RiseType.BackupInput;
        if (Contains(value, "блок") || Contains(value, "акб") || Contains(value, "battery"))
            return RiseType.Battery;

        return RiseType.UPS;
    }

    private static string ToDisplayName(LightingControlType value) =>
        value switch
        {
            LightingControlType.PhotoRelay => "Фотореле",
            LightingControlType.AstroTimer => "Астротаймер",
            LightingControlType.TimeRelay => "Реле времени",
            LightingControlType.Combined => "Авто + ручной",
            _ => "Ручной",
        };

    private static string ToDisplayName(RiseType value) =>
        value switch
        {
            RiseType.Battery => "Блок питания с АКБ",
            RiseType.BackupInput => "Резервный ввод",
            _ => "ИБП с АКБ",
        };

    private static bool Contains(string? value, string part) =>
        value?.Contains(part, StringComparison.OrdinalIgnoreCase) == true;
}
