using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiarioGym.Core.Datos;
using DiarioGym.Core.Negocio;

namespace DiarioGym.ViewModels;

public partial class RutinasViewModel : ObservableObject
{
    readonly RutinaRepositorio _rutinas;

    public RutinasViewModel(RutinaRepositorio rutinas) => _rutinas = rutinas;

    [ObservableProperty] private List<RutinaItem> items = new();
    [ObservableProperty] private bool vacio;

    public async Task CargarAsync()
    {
        var lista = new List<RutinaItem>();
        foreach (var r in await _rutinas.TodasAsync())
        {
            var ejercicios = await _rutinas.EjerciciosAsync(r.Id);
            var id = r.Id;
            var nombre = r.Nombre;
            lista.Add(new RutinaItem
            {
                Nombre = r.Nombre,
                DiasTexto = r.DiasLista.Length == 0 ? "Sin días fijos" : string.Join(" · ", r.DiasLista.Select(d => Fechas.DiasCortos[d])),
                EjerciciosTexto = ejercicios.Count == 0 ? "Sin ejercicios" : string.Join("\n", ejercicios.Select(e =>
                    $"{e.Nombre}   {e.Series?.ToString() ?? "?"}×{e.Reps?.ToString() ?? "?"}" + (e.PesoObjetivo is double p ? $" · {Formato.N(p)} kg" : ""))),
                EditarCommand = new AsyncRelayCommand(() => Shell.Current.GoToAsync($"editor?id={id}")),
                EliminarCommand = new AsyncRelayCommand(() => EliminarAsync(id, nombre)),
            });
        }
        Items = lista;
        Vacio = lista.Count == 0;
    }

    [RelayCommand]
    Task Nueva() => Shell.Current.GoToAsync("editor");

    async Task EliminarAsync(int id, string nombre)
    {
        var ok = await Shell.Current.DisplayAlert("Eliminar rutina", $"¿Eliminar “{nombre}”? Tus series registradas se conservan.", "Eliminar", "Cancelar");
        if (!ok) return;
        await _rutinas.EliminarAsync(id);
        await CargarAsync();
    }
}
