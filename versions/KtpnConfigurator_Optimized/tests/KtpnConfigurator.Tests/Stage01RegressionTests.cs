using System.IO;
using System.Text.Json;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.App.ViewModels;
using KtpnConfigurator.Core.Engine;
using KtpnConfigurator.Core.Models;
using Xunit;

namespace KtpnConfigurator.Tests;

// Регрессионные тесты фиксов этапов 0 и 1 (аудит 2026-07-11):
// вылеты при смене изделия, целостность данных, устойчивость загрузки.
public class Stage01RegressionTests
{
    private static MainViewModel CreateVm() => new(AppEnvironment.Load());

    private static string TempProjectPath(string name) =>
        Path.Combine(Path.GetTempPath(), $"stage01_{name}_{Guid.NewGuid():N}.ktpn");

    // --- Этап 0.1: петля шаблонов ---

    [Fact]
    public void TemplateOptionListsAreStableInstances()
    {
        // Новый массив на каждый вызов заставлял WPF сбрасывать SelectedItem в null.
        Assert.Same(
            ProductConfigurationDefaults.LowVoltageTemplateNames(ProductTypeIds.Shcho),
            ProductConfigurationDefaults.LowVoltageTemplateNames(ProductTypeIds.Shcho));
        Assert.Same(
            ProductConfigurationDefaults.MediumVoltageTemplateNames(ProductTypeIds.Kru),
            ProductConfigurationDefaults.MediumVoltageTemplateNames(ProductTypeIds.Kru));
    }

    [Fact]
    public void LvTemplateIgnoresNullFromWpfBinding()
    {
        var vm = CreateVm();
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        var before = vm.LvTemplate;
        var panelsBefore = vm.LvPanels.Count;

        vm.LvTemplate = null!;
        vm.LvTemplate = "";

        Assert.Equal(before, vm.LvTemplate);
        Assert.Equal(panelsBefore, vm.LvPanels.Count);
    }

    [Fact]
    public void MvTemplateIgnoresNullFromWpfBinding()
    {
        var vm = CreateVm();
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Kso);
        var before = vm.MvTemplate;

        vm.MvTemplate = null!;

        Assert.Equal(before, vm.MvTemplate);
    }

    [Fact]
    public void SwitchingBetweenTemplateFamiliesKeepsTemplateConsistent()
    {
        // Смены НКУ->ЩО->ВРУ и КСО->КРУ раньше приводили к stack overflow через UI.
        var vm = CreateVm();
        foreach (var id in new[]
        {
            ProductTypeIds.Nku, ProductTypeIds.Shcho, ProductTypeIds.Vru,
            ProductTypeIds.Kso, ProductTypeIds.Kru, ProductTypeIds.SingleKtpn,
        })
        {
            vm.SelectedProduct = ProductRegistry.ResolveOrDefault(id);
            if (vm.ShowLowVoltageConfiguration)
                Assert.Contains(vm.LvTemplate, vm.LvTemplateOptions);
            if (vm.ShowMediumVoltageConfiguration)
                Assert.Contains(vm.MvTemplate, vm.MvTemplateOptions);
        }
    }

    // --- Этап 0.2: устойчивость загрузки ---

    [Fact]
    public void OpenProjectWithCorruptedFileKeepsViewModelAlive()
    {
        var path = TempProjectPath("corrupted");
        File.WriteAllText(path, "{ это не json ");
        try
        {
            var vm = CreateVm();
            string? notified = null;
            vm.Notify = message => notified = message;
            vm.AskOpenPath = () => path;

            vm.OpenProjectCommand.Execute(null);

            Assert.NotNull(notified);
            Assert.Contains("Не удалось открыть проект", notified);

            // Пересчёт не должен остаться замороженным (_suspendRecalc).
            var resultBefore = vm.CurrentResult;
            vm.Recalculate();
            Assert.NotSame(resultBefore, vm.CurrentResult);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadRejectsNonObjectJson()
    {
        var path = TempProjectPath("array");
        File.WriteAllText(path, "[1, 2, 3]");
        try
        {
            Assert.ThrowsAny<Exception>(() => ProjectStorage.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FailedSaveDoesNotBumpRevision()
    {
        var cfg = new ProjectConfig { Revision = 3 };
        var invalidPath = Path.Combine(Path.GetTempPath(), "stage01_bad\0name.ktpn");
        Assert.ThrowsAny<Exception>(() => ProjectStorage.Save(cfg, invalidPath));
        Assert.Equal(3, cfg.Revision);
    }

    // --- Этап 1.4: расчёт не мутирует проект ---

    [Fact]
    public void DeletingAllPanelsDoesNotResurrectThem()
    {
        var vm = CreateVm();
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        Assert.True(vm.LvPanels.Count > 0);

        while (vm.LvPanels.Count > 0)
            vm.LvPanels[0].DeleteCommand.Execute(null);

        Assert.Empty(vm.LvPanels);
        vm.Recalculate();
        Assert.Empty(vm.LvPanels);
        Assert.Empty(vm.CurrentConfig.LowVoltageAssembly.Panels);
    }

    [Fact]
    public void CalculateDoesNotAddPanelsToEmptyLineup()
    {
        var env = AppEnvironment.Load();
        var cfg = new ProjectConfig { ProductTypeId = ProductTypeIds.Nku };
        cfg.LowVoltageAssembly.Panels.Clear();

        CalculationEngine.Calculate(cfg, env.Catalog);

        Assert.Empty(cfg.LowVoltageAssembly.Panels);
    }

    // --- Этап 1.5: ручные переопределения не перетекают между изделиями ---

    [Fact]
    public void ManualOverridesAreIsolatedPerProduct()
    {
        var vm = CreateVm();
        vm.ManualLengthText = "3500";
        Assert.Equal(3500, vm.CurrentConfig.ManualLength);

        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        Assert.Null(vm.CurrentConfig.ManualLength);
        vm.ManualLengthText = "1200";

        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.SingleKtpn);
        Assert.Equal(3500, vm.CurrentConfig.ManualLength);

        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        Assert.Equal(1200, vm.CurrentConfig.ManualLength);
    }

    [Fact]
    public void ManualOverridesSurviveSaveAndLoad()
    {
        var vm = CreateVm();
        vm.ManualLengthText = "3500";
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        vm.ManualLengthText = "1200";

        var path = TempProjectPath("manual");
        try
        {
            ProjectStorage.Save(vm.CurrentConfig, path);
            var loaded = ProjectStorage.Load(path);
            Assert.Equal(1200, loaded.ManualLength);
            loaded.SwitchManualOverrides(ProductTypeIds.Nku, ProductTypeIds.SingleKtpn);
            Assert.Equal(3500, loaded.ManualLength);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // --- Этап 1.6: миграции ---

    [Fact]
    public void StringProjectVersionDoesNotResetProductType()
    {
        var path = TempProjectPath("stringversion");
        File.WriteAllText(path, """
            {
              "ProjectVersion": "5",
              "ProductTypeId": "nku.general",
              "ProjectName": "Проект НКУ"
            }
            """);
        try
        {
            var loaded = ProjectStorage.Load(path);
            Assert.Equal(ProductTypeIds.Nku, loaded.ProductTypeId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ValidProductTypeSurvivesMissingVersion()
    {
        var path = TempProjectPath("noversion");
        File.WriteAllText(path, """{ "ProductTypeId": "mv.kru" }""");
        try
        {
            var loaded = ProjectStorage.Load(path);
            Assert.Equal(ProductTypeIds.Kru, loaded.ProductTypeId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FreeTextRiseTypeSurvivesSaveLoadRoundtrip()
    {
        var cfg = new ProjectConfig();
        cfg.AuxiliaryNeeds.RieseEnabled = true;
        cfg.AuxiliaryNeeds.RieseType = "ИБП offline 600 ВА";
        cfg.AuxiliaryNeeds.LightingControlMode = "Ручной + фотореле";

        var path = TempProjectPath("risetype");
        try
        {
            ProjectStorage.Save(cfg, path);
            var loaded = ProjectStorage.Load(path);
            Assert.Equal("ИБП offline 600 ВА", loaded.AuxiliaryNeeds.RieseType);
            Assert.Equal("Ручной + фотореле", loaded.AuxiliaryNeeds.LightingControlMode);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AliasesAreNotSerialized()
    {
        var json = JsonSerializer.Serialize(new AuxiliaryNeedsConfig());
        Assert.DoesNotContain("HasAuxiliaryCabinet", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RiseType", json);
        Assert.DoesNotContain("LightingControlType", json);
    }

    // --- Этап 1.7: null-безопасность ---

    [Fact]
    public void NullRuvnTypeDoesNotCrashCalculation()
    {
        var env = AppEnvironment.Load();
        var cfg = new ProjectConfig
        {
            Mark = "ТМГ-400 (Алагеум)",
            Manufacturer = "Алагеум",
            RuvnType = null!,
            RuvnExecution = null!,
        };

        var result = CalculationEngine.Calculate(cfg, env.Catalog);
        Assert.NotNull(result);
    }

    [Fact]
    public void NullProductTypeIdFallsBackToSingleKtpn()
    {
        var env = AppEnvironment.Load();
        var cfg = new ProjectConfig { ProductTypeId = null!, Mark = "ТМГ-400 (Алагеум)" };
        var result = CalculationEngine.Calculate(cfg, env.Catalog);
        Assert.True(result.RatedCurrentA > 0);
    }
}
