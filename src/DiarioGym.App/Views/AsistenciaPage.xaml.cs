using System.ComponentModel;
using DiarioGym.Graficos;
using DiarioGym.ViewModels;
using DiarioGym.Core.Negocio;

namespace DiarioGym.Views;

public partial class AsistenciaPage : ContentPage
{
    readonly AsistenciaViewModel _vm;
    readonly MapaAsistencia _mapa = new();

    public AsistenciaPage(AsistenciaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        Mapa.Drawable = _mapa;
        vm.PropertyChanged += OnVmPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarAsync();
    }

    void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AsistenciaViewModel.DiasAsistidos) or nameof(AsistenciaViewModel.FechaEntreno))
        {
            _mapa.Dias = _vm.DiasAsistidos;
            _mapa.Seleccion = _vm.FechaEntreno is DateTime f && f.Date != DateTime.Today ? Fechas.Clave(f) : null;
            Mapa.Invalidate();
        }
    }

    async void OnMapaTapped(object? sender, TappedEventArgs e)
    {
        if (e.GetPosition(Mapa) is not Point p) return;
        if (_mapa.FechaEn((float)p.X, (float)p.Y) is not string fecha) return;
        _vm.SeleccionarDia(fecha);
        // Lleva la vista a la tarjeta del entreno de ese día
        if (Content is ScrollView sv) await sv.ScrollToAsync(TarjetaEntreno, ScrollToPosition.Start, true);
    }
}
