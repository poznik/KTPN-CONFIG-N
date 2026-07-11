using System;
using System.IO;
using System.Linq;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.Core.Documents;
using KtpnConfigurator.Core.Models;
using Xunit;

namespace KtpnConfigurator.Tests;

public class AuxiliaryNeedsPlanTests
{
    [Fact]
    public void SpecificationBuilder_GeneratesTransformerAndAuxiliaryCabinet()
    {
        var cfg = new ProjectConfig
        {
            Mark = "ТМГ-400",
            AuxiliaryNeeds = new AuxiliaryNeedsConfig
            {
                HasAuxiliaryCabinet = true,
                CabinetModel = "ЩСН-01",
            },
        };

        var items = SpecificationBuilder.GenerateSpecification(cfg).ToList();

        Assert.Collection(items,
            item =>
            {
                Assert.Equal("1", item.Position);
                Assert.Equal("Силовой трансформатор", item.Name);
                Assert.Equal("ТМГ-400", item.Type);
                Assert.Equal(1, item.Quantity);
            },
            item =>
            {
                Assert.Equal("2", item.Position);
                Assert.Equal("Шкаф собственных нужд", item.Name);
                Assert.Equal("ЩСН-01", item.Type);
                Assert.Equal(1, item.Quantity);
            });
    }

    [Fact]
    public void ProjectStorage_PreservesAuxiliaryNeedsFields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aux_{Guid.NewGuid():N}.ktpn");
        var cfg = new ProjectConfig
        {
            ProjectName = "Проверка собственных нужд",
            Mark = "ТМГ-250",
            AuxiliaryNeeds = new AuxiliaryNeedsConfig
            {
                HasAuxiliaryCabinet = true,
                HasLighting = true,
                LightingControlType = LightingControlType.PhotoRelay,
                HasRise = true,
                RiseType = RiseType.UPS,
                RiesePowerVa = 1000,
                RieseAutonomyMinutes = 45,
            },
        };

        try
        {
            ProjectStorage.Save(cfg, path);
            var json = File.ReadAllText(path);

            // С формата 8 в файл пишутся только канонические поля: дублирующие
            // алиасы при загрузке перетирали свободный текст пользователя.
            Assert.Contains("\"Enabled\": true", json);
            Assert.DoesNotContain("\"HasAuxiliaryCabinet\"", json);
            Assert.DoesNotContain("\"LightingControlType\"", json);

            var loaded = ProjectStorage.Load(path);

            Assert.True(loaded.AuxiliaryNeeds.HasAuxiliaryCabinet);
            Assert.True(loaded.AuxiliaryNeeds.HasLighting);
            Assert.Equal(LightingControlType.PhotoRelay, loaded.AuxiliaryNeeds.LightingControlType);
            Assert.True(loaded.AuxiliaryNeeds.HasRise);
            Assert.Equal(RiseType.UPS, loaded.AuxiliaryNeeds.RiseType);
            Assert.Equal(1000, loaded.AuxiliaryNeeds.RiesePowerVa);
            Assert.Equal(45, loaded.AuxiliaryNeeds.RieseAutonomyMinutes);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ProjectStorage_LoadsNewAuxiliaryNeedsJsonAliases()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aux_alias_{Guid.NewGuid():N}.ktpn");
        File.WriteAllText(path, """
        {
          "ProjectName": "Новый формат",
          "Mark": "ТМГ-160",
          "AuxiliaryNeeds": {
            "HasAuxiliaryCabinet": true,
            "HasLighting": true,
            "LightingControlType": "AstroTimer",
            "HasRise": true,
            "RiseType": "BackupInput"
          }
        }
        """);

        try
        {
            var loaded = ProjectStorage.Load(path);

            Assert.True(loaded.AuxiliaryNeeds.Enabled);
            Assert.True(loaded.AuxiliaryNeeds.LightingEnabled);
            Assert.Equal(LightingControlType.AstroTimer, loaded.AuxiliaryNeeds.LightingControlType);
            Assert.Equal("Астротаймер", loaded.AuxiliaryNeeds.LightingControlMode);
            Assert.True(loaded.AuxiliaryNeeds.RieseEnabled);
            Assert.Equal(RiseType.BackupInput, loaded.AuxiliaryNeeds.RiseType);
            Assert.Equal("Резервный ввод", loaded.AuxiliaryNeeds.RieseType);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
