using System.ComponentModel;
using DiarioGym.Graficos;
using DiarioGym.ViewModels;

namespace DiarioGym.Views;

public partial class EvolucionPage : ContentPage
{
    readonly EvolucionViewModel _vm;
    readonly GraficaLinea _grafica = new();

    public EvolucionPage(EvolucionViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        Grafica.Drawable = _grafica;
        vm.PropertyChanged += OnVmPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarAsync();
    }

    void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(EvolucionViewModel.Puntos)) return;
        _grafica.Puntos = _vm.Puntos;
        _grafica.Decimales = _vm.Decimales;
        Grafica.Invalidate();
    }
}
