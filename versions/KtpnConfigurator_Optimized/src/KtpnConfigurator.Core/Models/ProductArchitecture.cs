namespace KtpnConfigurator.Core.Models;

public static class ProductTypeIds
{
    public const string SingleKtpn = "ktpn.single";
    public const string DoubleKtpn = "ktpn.double";
    public const string Nku = "nku.general";
    public const string Shcho = "nku.shcho";
    public const string Vru = "nku.vru";
    public const string Kso = "mv.kso";
    public const string Kru = "mv.kru";
}

public enum ProductFamily
{
    TransformerSubstation,
    LowVoltageAssembly,
    MediumVoltageSwitchgear,
}

public enum ProductAvailability
{
    Available,
    Planned,
}

[Flags]
public enum ProductCapability
{
    None = 0,
    Transformer = 1 << 0,
    Enclosure = 1 << 1,
    HighVoltageDistribution = 1 << 2,
    LowVoltageDistribution = 1 << 3,
    AuxiliaryNeeds = 1 << 4,
    MultipleSources = 1 << 5,
    BusSections = 1 << 6,
    AutomaticTransfer = 1 << 7,
    PanelLineup = 1 << 8,
    Metering = 1 << 9,
    MediumVoltageCellLineup = 1 << 10,
    RelayProtection = 1 << 11,
    InternalArcClassification = 1 << 12,
}

public sealed record ProductDefinition(
    string Id,
    ProductFamily Family,
    string DisplayName,
    ProductAvailability Availability,
    int CurrentDataVersion,
    ProductCapability Capabilities);

public static class ProductRegistry
{
    private static readonly IReadOnlyList<ProductDefinition> Definitions =
    [
        new(
            ProductTypeIds.SingleKtpn,
            ProductFamily.TransformerSubstation,
            "КТПН",
            ProductAvailability.Available,
            1,
            ProductCapability.Transformer
            | ProductCapability.Enclosure
            | ProductCapability.HighVoltageDistribution
            | ProductCapability.LowVoltageDistribution
            | ProductCapability.AuxiliaryNeeds),
        new(
            ProductTypeIds.DoubleKtpn,
            ProductFamily.TransformerSubstation,
            "2КТПН",
            ProductAvailability.Available,
            3,
            ProductCapability.Transformer
            | ProductCapability.Enclosure
            | ProductCapability.HighVoltageDistribution
            | ProductCapability.LowVoltageDistribution
            | ProductCapability.AuxiliaryNeeds
            | ProductCapability.MultipleSources
            | ProductCapability.BusSections
            | ProductCapability.AutomaticTransfer),
        new(
            ProductTypeIds.Nku,
            ProductFamily.LowVoltageAssembly,
            "НКУ",
            ProductAvailability.Available,
            3,
            ProductCapability.Enclosure
            | ProductCapability.LowVoltageDistribution
            | ProductCapability.BusSections
            | ProductCapability.PanelLineup
            | ProductCapability.Metering),
        new(
            ProductTypeIds.Shcho,
            ProductFamily.LowVoltageAssembly,
            "ЩО",
            ProductAvailability.Available,
            3,
            ProductCapability.Enclosure
            | ProductCapability.LowVoltageDistribution
            | ProductCapability.BusSections
            | ProductCapability.PanelLineup
            | ProductCapability.Metering),
        new(
            ProductTypeIds.Vru,
            ProductFamily.LowVoltageAssembly,
            "ВРУ",
            ProductAvailability.Available,
            3,
            ProductCapability.Enclosure
            | ProductCapability.LowVoltageDistribution
            | ProductCapability.BusSections
            | ProductCapability.AutomaticTransfer
            | ProductCapability.PanelLineup
            | ProductCapability.Metering),
        new(
            ProductTypeIds.Kso,
            ProductFamily.MediumVoltageSwitchgear,
            "КСО",
            ProductAvailability.Available,
            3,
            ProductCapability.Enclosure
            | ProductCapability.HighVoltageDistribution
            | ProductCapability.BusSections
            | ProductCapability.MediumVoltageCellLineup
            | ProductCapability.Metering
            | ProductCapability.RelayProtection),
        new(
            ProductTypeIds.Kru,
            ProductFamily.MediumVoltageSwitchgear,
            "КРУ",
            ProductAvailability.Available,
            3,
            ProductCapability.Enclosure
            | ProductCapability.HighVoltageDistribution
            | ProductCapability.BusSections
            | ProductCapability.MediumVoltageCellLineup
            | ProductCapability.Metering
            | ProductCapability.RelayProtection
            | ProductCapability.InternalArcClassification),
    ];

    private static readonly IReadOnlyDictionary<string, ProductDefinition> ById = Definitions
        .ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<ProductDefinition> All => Definitions;

    public static ProductDefinition SingleKtpn => ById[ProductTypeIds.SingleKtpn];

    public static ProductDefinition? Find(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : ById.GetValueOrDefault(id.Trim());

    public static ProductDefinition ResolveOrDefault(string? id) => Find(id) ?? SingleKtpn;
}
