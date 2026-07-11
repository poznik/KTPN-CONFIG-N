using System.IO;
using System.Text.Json;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.Core.Models;
using Xunit;

namespace KtpnConfigurator.Tests;

public class ProductArchitectureTests
{
    [Fact]
    public void ProductRegistry_ContainsPlannedProductFamiliesWithStableIds()
    {
        Assert.Equal(7, ProductRegistry.All.Count);
        Assert.Equal(ProductRegistry.All.Count, ProductRegistry.All.Select(product => product.Id).Distinct().Count());
        Assert.Equal(ProductAvailability.Available, ProductRegistry.SingleKtpn.Availability);

        Assert.Equal(ProductFamily.TransformerSubstation, ProductRegistry.Find(ProductTypeIds.DoubleKtpn)!.Family);
        Assert.Equal(ProductFamily.LowVoltageAssembly, ProductRegistry.Find(ProductTypeIds.Shcho)!.Family);
        Assert.Equal(ProductFamily.LowVoltageAssembly, ProductRegistry.Find(ProductTypeIds.Vru)!.Family);
        Assert.Equal(ProductFamily.MediumVoltageSwitchgear, ProductRegistry.Find(ProductTypeIds.Kso)!.Family);
        Assert.Equal(ProductFamily.MediumVoltageSwitchgear, ProductRegistry.Find(ProductTypeIds.Kru)!.Family);
    }

    [Fact]
    public void PlannedProducts_DeclareRequiredStructuralCapabilities()
    {
        var doubleKtpn = ProductRegistry.Find(ProductTypeIds.DoubleKtpn)!;
        Assert.True(doubleKtpn.Capabilities.HasFlag(ProductCapability.MultipleSources));
        Assert.True(doubleKtpn.Capabilities.HasFlag(ProductCapability.BusSections));
        Assert.True(doubleKtpn.Capabilities.HasFlag(ProductCapability.AutomaticTransfer));

        var kru = ProductRegistry.Find(ProductTypeIds.Kru)!;
        Assert.True(kru.Capabilities.HasFlag(ProductCapability.MediumVoltageCellLineup));
        Assert.True(kru.Capabilities.HasFlag(ProductCapability.RelayProtection));
        Assert.True(kru.Capabilities.HasFlag(ProductCapability.InternalArcClassification));
    }

    [Fact]
    public void LegacyProject_MigratesToSingleKtpnProductIdentity()
    {
        var path = Path.Combine(Path.GetTempPath(), $"legacy_product_{Guid.NewGuid():N}.ktpn");
        File.WriteAllText(path, """
        {
          "ProjectVersion": 3,
          "ProjectName": "Старая КТПН"
        }
        """);

        try
        {
            var project = ProjectStorage.Load(path);

            Assert.Equal(ProjectConfig.CurrentVersion, project.ProjectVersion);
            Assert.Equal(ProductTypeIds.SingleKtpn, project.ProductTypeId);
            Assert.Equal(ProductRegistry.SingleKtpn.CurrentDataVersion, project.ProductDataVersion);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SavedProject_PersistsProductIdentityWithoutChangingLegacyExtension()
    {
        var path = Path.Combine(Path.GetTempPath(), $"product_identity_{Guid.NewGuid():N}.ktpn");

        try
        {
            ProjectStorage.Save(new ProjectConfig(), path);
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            Assert.Equal(ProductTypeIds.SingleKtpn, root.GetProperty(nameof(ProjectConfig.ProductTypeId)).GetString());
            Assert.Equal(1, root.GetProperty(nameof(ProjectConfig.ProductDataVersion)).GetInt32());
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
