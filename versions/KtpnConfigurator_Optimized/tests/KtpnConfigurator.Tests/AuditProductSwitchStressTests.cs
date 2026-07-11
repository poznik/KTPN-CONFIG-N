using System.Diagnostics;
using System.IO;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.App.ViewModels;
using KtpnConfigurator.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace KtpnConfigurator.Tests;

// Временный аудиторский стресс-тест: воспроизводит смену изделия, как в UI.
public class AuditProductSwitchStressTests
{
    private readonly ITestOutputHelper _output;

    public AuditProductSwitchStressTests(ITestOutputHelper output) => _output = output;

    private static readonly string[] AllProducts =
    {
        ProductTypeIds.SingleKtpn, ProductTypeIds.DoubleKtpn, ProductTypeIds.Nku,
        ProductTypeIds.Shcho, ProductTypeIds.Vru, ProductTypeIds.Kso, ProductTypeIds.Kru,
    };

    [Fact]
    public void SwitchingThroughAllProductsRepeatedlyDoesNotThrow()
    {
        var vm = new MainViewModel(AppEnvironment.Load());
        var sw = new Stopwatch();
        for (var round = 0; round < 3; round++)
        {
            foreach (var id in AllProducts)
            {
                var definition = ProductRegistry.ResolveOrDefault(id);
                sw.Restart();
                vm.SelectedProduct = definition;
                sw.Stop();
                _output.WriteLine($"round {round} -> {id}: {sw.ElapsedMilliseconds} ms");

                // Симулируем правки пользователя в текущем изделии.
                if (vm.ShowLowVoltageConfiguration)
                {
                    vm.LvRatedBusCurrent = 1600;
                    vm.LvSectionCount = 2;
                }
                if (vm.ShowMediumVoltageConfiguration)
                {
                    vm.MvRatedBusCurrent = 1000;
                }
                if (vm.ShowDoubleKtpnConfiguration)
                {
                    vm.SecondTransformerManufacturer = vm.Manufacturers.LastOrDefault() ?? "";
                }
            }
        }
    }

    [Fact]
    public void SwitchingWithPanelEditsAndBack()
    {
        var vm = new MainViewModel(AppEnvironment.Load());
        // НКУ: редактируем панели, добавляем/удаляем, потом уходим в КРУ и назад.
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        for (var i = 0; i < 15; i++)
            vm.AddLvPanelCommand.Execute(null);
        var panel = vm.LvPanels.Last();
        panel.PanelType = "Учетная";
        panel.MainDevice = "Счетчик, ТТ";
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Kru);
        for (var i = 0; i < 10; i++)
            vm.AddMvCellCommand.Execute(null);
        vm.MvCells.Last().Purpose = "Секционный выключатель";
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.SingleKtpn);
        vm.SelectedProduct = ProductRegistry.ResolveOrDefault(ProductTypeIds.Nku);
        _output.WriteLine($"panels after return: {vm.LvPanels.Count}");
    }

    [Fact]
    public void RecalculateTimingPerProduct()
    {
        var vm = new MainViewModel(AppEnvironment.Load());
        foreach (var id in AllProducts)
        {
            vm.SelectedProduct = ProductRegistry.ResolveOrDefault(id);
            var sw = Stopwatch.StartNew();
            const int n = 20;
            for (var i = 0; i < n; i++)
                vm.Recalculate();
            sw.Stop();
            _output.WriteLine($"{id}: Recalculate avg {sw.ElapsedMilliseconds / (double)n:0.0} ms");
        }
    }

    [Fact]
    public void SaveAndReloadEveryProductType()
    {
        foreach (var id in AllProducts)
        {
            var vm = new MainViewModel(AppEnvironment.Load());
            vm.SelectedProduct = ProductRegistry.ResolveOrDefault(id);
            var path = Path.Combine(Path.GetTempPath(), $"audit_{id.Replace('.', '_')}.ktpn");
            ProjectStorage.Save(vm.CurrentConfig, path);
            var loaded = ProjectStorage.Load(path);
            Assert.Equal(id, loaded.ProductTypeId);
            File.Delete(path);
        }
    }
}
