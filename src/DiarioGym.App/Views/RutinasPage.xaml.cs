using DiarioGym.ViewModels;

namespace DiarioGym.Views;

public partial class RutinasPage : ContentPage
{
    readonly RutinasViewModel _vm;

    public RutinasPage(RutinasViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarAsync();
    }
}
