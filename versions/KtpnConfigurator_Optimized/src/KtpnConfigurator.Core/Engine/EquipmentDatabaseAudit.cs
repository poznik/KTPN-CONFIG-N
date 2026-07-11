using KtpnConfigurator.Core.Catalogs;
using KtpnConfigurator.Core.Models;

namespace KtpnConfigurator.Core.Engine;

public sealed class EquipmentDatabaseAuditResult
{
    public int Total { get; init; }
    public int Verified { get; init; }
    public int NeedsVerification { get; init; }
    public int MissingSource { get; init; }
    public int MissingVerificationDate { get; init; }
    public int DuplicateCount { get; init; }
    public string Summary =>
        $"Позиций: {Total}; проверено: {Verified}; требуют проверки: {NeedsVerification}; дубли: {DuplicateCount}";
}

public static class EquipmentDatabaseAudit
{
    public static EquipmentDatabaseAuditResult Analyze(CatalogStore store)
    {
        var models = store.DeviceModels;
        var duplicateCount = models
            .GroupBy(model => $"{model.Type}|{model.Manufacturer}|{model.Model}|{model.Series}", StringComparer.OrdinalIgnoreCase)
            .Sum(group => Math.Max(0, group.Count() - 1));

        return new EquipmentDatabaseAuditResult
        {
            Total = models.Count,
            Verified = models.Count(IsVerified),
            NeedsVerification = models.Count(model => !IsVerified(model)),
            MissingSource = models.Count(model => string.IsNullOrWhiteSpace(model.DataSource)),
            MissingVerificationDate = models.Count(model => IsVerified(model) && string.IsNullOrWhiteSpace(model.VerificationDate)),
            DuplicateCount = duplicateCount,
        };
    }

    private static bool IsVerified(DeviceModel model) =>
        model.SourceConfidence.Equals("verified", StringComparison.OrdinalIgnoreCase);
}
