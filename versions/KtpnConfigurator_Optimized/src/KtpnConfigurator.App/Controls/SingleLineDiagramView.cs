using KtpnConfigurator.Core.Catalogs;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using KtpnConfigurator.App.Services;
using KtpnConfigurator.App.ViewModels;

namespace KtpnConfigurator.App.Controls;

public sealed class SingleLineDiagramView : FrameworkElement
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(MainViewModel),
            typeof(SingleLineDiagramView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnViewModelChanged));

    public MainViewModel? ViewModel
    {
        get => (MainViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return ViewModel is null
            ? new Size(1680, 1188)
            : SingleLineDiagramRenderer.Measure(ViewModel.CurrentConfig);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (ViewModel is null)
            return;

        var desired = SingleLineDiagramRenderer.Measure(ViewModel.CurrentConfig);
        var size = new Size(Math.Max(RenderSize.Width, desired.Width), Math.Max(RenderSize.Height, desired.Height));
        SingleLineDiagramRenderer.Draw(
            drawingContext,
            ViewModel.CurrentConfig,
            ViewModel.CurrentResult,
            ViewModel.Catalog,
            size,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (SingleLineDiagramView)d;
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= view.OnViewModelPropertyChanged;
        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += view.OnViewModelPropertyChanged;
        view.InvalidateMeasure();
        view.InvalidateVisual();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateMeasure();
        InvalidateVisual();
    }
}
