using System.IO;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.App.ViewModels;
using KtpnConfigurator.Core.Models;
using Xunit;

namespace KtpnConfigurator.Tests;

// Регрессионные тесты фиксов этапа 2 (аудит 2026-07-15):
// сохранность линеек при смене изделия, защита пересчёта от повторного входа,
// null-безопасность привязок, переключение скрытой вкладки.
public class Stage2LineupPreservationTests
{
    private static MainViewModel CreateVm() => new(AppEnvironment.Load());

    private static string TempProjectPath(string name) =>
        Path.Combine(Path.GetTempPath(), $"stage2_{name}_{Guid.NewGuid():N}.ktpn");

    // --- Линейка переживает уход на другое изделие и возврат ---

    [Fact]
    public void LineupSurvivesProductRoundTrip()
    {
        var vm = CreateVm();
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        var templateBefore = vm.LvTemplate;
        for (var i = 0; i < 15; i++)
            vm.AddLvPanelCommand.Execute(null);
        var panelsBefore = vm.LvPanels.Count;
        vm.LvPanels[^1].PanelType = "Учетная";
        vm.LvPanels[^1].Purpose = "Мой учет";

        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.SingleKtpn);
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);

        Assert.Equal(panelsBefore, vm.LvPanels.Count);
        Assert.Equal(templateBefore, vm.LvTemplate);
        Assert.Equal("Мой учет", vm.LvPanels[^1].Purpose);
        Assert.Equal("Учетная", vm.LvPanels[^1].PanelType);
    }

    [Fact]
    public void MediumVoltageLineupSurvivesProductRoundTrip()
    {
        var vm = CreateVm();
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Kru);
        for (var i = 0; i < 10; i++)
            vm.AddMvCellCommand.Execute(null);
        var cellsBefore = vm.MvCells.Count;
        vm.MvCells[^1].Purpose = "Секционный выключатель";

        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.SingleKtpn);
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Kru);

        Assert.Equal(cellsBefore, vm.MvCells.Count);
        Assert.Equal("Секционный выключатель", vm.MvCells[^1].Purpose);
    }

    [Fact]
    public void LineupsAreIsolatedBetweenLvProducts()
    {
        var vm = CreateVm();
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        vm.AddLvPanelCommand.Execute(null);
        var nkuPanels = vm.LvPanels.Count;

        // Первый вход в ЩО получает собственный типовой состав, а не панели НКУ.
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Shcho);
        Assert.Equal(ProductConfigurationDefaults.DefaultLowVoltageTemplate(ProductTypeIds.Shcho), vm.LvTemplate);
        Assert.NotEqual(nkuPanels, vm.LvPanels.Count);
        vm.AddLvPanelCommand.Execute(null);
        var shchoPanels = vm.LvPanels.Count;

        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        Assert.Equal(nkuPanels, vm.LvPanels.Count);

        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Shcho);
        Assert.Equal(shchoPanels, vm.LvPanels.Count);
    }

    [Fact]
    public void EmptyLineupStaysEmptyAfterRoundTrip()
    {
        var vm = CreateVm();
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        while (vm.LvPanels.Count > 0)
            vm.LvPanels[0].DeleteCommand.Execute(null);
        Assert.Empty(vm.LvPanels);

        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.SingleKtpn);
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);

        Assert.Empty(vm.LvPanels);
        Assert.Empty(vm.CurrentConfig.LowVoltageAssembly.Panels);
    }

    [Fact]
    public void LineupStashSurvivesSaveAndLoad()
    {
        var vm = CreateVm();
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        vm.AddLvPanelCommand.Execute(null);
        var nkuPanels = vm.LvPanels.Count;
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Shcho);

        var path = TempProjectPath("lineupstash");
        try
        {
            ProjectStorage.Save(vm.CurrentConfig, path);
            var loaded = ProjectStorage.Load(path);
            Assert.Equal(ProductTypeIds.Shcho, loaded.ProductTypeId);
            loaded.SwitchProductLineups(ProductTypeIds.Shcho, ProductTypeIds.Nku);
            Assert.Equal(nkuPanels, loaded.LowVoltageAssembly.Panels.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadKeepsIntentionallyEmptyLineup()
    {
        var vm = CreateVm();
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        while (vm.LvPanels.Count > 0)
            vm.LvPanels[0].DeleteCommand.Execute(null);

        var path = TempProjectPath("emptylineup");
        try
        {
            ProjectStorage.Save(vm.CurrentConfig, path);
            var loaded = ProjectStorage.Load(path);
            Assert.Equal(ProductTypeIds.Nku, loaded.ProductTypeId);
            Assert.Empty(loaded.LowVoltageAssembly.Panels);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // --- Пересчёт защищён от повторного входа ---

    [Fact]
    public void NestedRecalculateDoesNotRecurse()
    {
        var vm = CreateVm();
        var notifications = 0;
        vm.PropertyChanged += (_, _) =>
        {
            notifications++;
            // Худший случай: обработчик уведомления сам дёргает пересчёт,
            // как это делает WPF при подмене ItemsSource.
            if (notifications < 500)
                vm.Recalculate();
        };

        vm.Recalculate();

        Assert.True(notifications > 0);
    }

    [Fact]
    public void PropertyWriteDuringRecalculateConverges()
    {
        var vm = CreateVm();
        var wrote = false;
        vm.PropertyChanged += (_, args) =>
        {
            if (!wrote && args.PropertyName == nameof(vm.GrossMassText))
            {
                wrote = true;
                vm.LengthBuffer = vm.LengthBuffer + 5;
            }
        };

        vm.Recalculate();

        Assert.True(wrote);
        Assert.NotNull(vm.CurrentResult);
    }

    // --- Null от WPF-привязок не затирает значения ---

    [Fact]
    public void NullFromWpfBindingDoesNotClearValues()
    {
        var vm = CreateVm();
        var mark = vm.Mark;
        var manufacturer = vm.Manufacturer;
        vm.Mark = null!;
        vm.Manufacturer = null!;
        Assert.Equal(mark, vm.Mark);
        Assert.Equal(manufacturer, vm.Manufacturer);

        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        var earthing = vm.LvEarthingSystem;
        vm.LvEarthingSystem = null!;
        Assert.Equal(earthing, vm.LvEarthingSystem);

        var panel = vm.LvPanels[0];
        var panelType = panel.PanelType;
        var panelManufacturer = panel.Manufacturer;
        panel.PanelType = null!;
        panel.Manufacturer = null!;
        Assert.Equal(panelType, panel.PanelType);
        Assert.Equal(panelManufacturer, panel.Manufacturer);

        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.DoubleKtpn);
        var secondMark = vm.SecondTransformerMark;
        vm.SecondTransformerMark = null!;
        vm.SecondTransformerManufacturer = null!;
        Assert.Equal(secondMark, vm.SecondTransformerMark);
    }

    // --- Скрытая вкладка не остаётся активной ---

    [Fact]
    public void HiddenTabSwitchesToVisibleOne()
    {
        var vm = CreateVm();
        vm.SelectedMainTabIndex = 3; // «3. РУНН», видима только для КТПН/2КТПН

        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        Assert.Equal(5, vm.SelectedMainTabIndex); // «5. Конфигурация»

        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.SingleKtpn);
        Assert.Equal(1, vm.SelectedMainTabIndex); // «Трансформатор и корпус»

        // Видимые для всех изделий вкладки не трогаем.
        vm.SelectedMainTabIndex = 6;
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Kso);
        Assert.Equal(6, vm.SelectedMainTabIndex);
    }

    [Fact]
    public void OpeningProjectOfOtherProductLeavesVisibleTab()
    {
        var nku = CreateVm();
        nku.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        var path = TempProjectPath("othertab");
        try
        {
            ProjectStorage.Save(nku.CurrentConfig, path);

            var vm = CreateVm();
            vm.SelectedMainTabIndex = 3; // «3. РУНН» — вкладка КТПН
            vm.AskOpenPath = () => path;
            vm.OpenProjectCommand.Execute(null);

            Assert.Equal(ProductTypeIds.Nku, vm.CurrentConfig.ProductTypeId);
            Assert.Equal(5, vm.SelectedMainTabIndex); // «5. Конфигурация»
        }
        finally
        {
            File.Delete(path);
        }
    }
}
