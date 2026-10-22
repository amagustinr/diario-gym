using System.ComponentModel;
using DiarioGym.Graficos;
using DiarioGym.ViewModels;

namespace DiarioGym.Views;

public partial class PesoPage : ContentPage
{
    readonly PesoViewModel _vm;
    readonly GraficaLinea _grafica = new();

    public PesoPage(PesoViewModel vm)
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
        if (e.PropertyName != nameof(PesoViewModel.Puntos)) return;
        _grafica.Puntos = _vm.Puntos;
        _grafica.Meta = _vm.MetaGrafica;
        _grafica.Unidad = _vm.UnidadGrafica;
        Grafica.Invalidate();
    }
}
