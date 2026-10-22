using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiarioGym.Core.Datos;
using DiarioGym.Core.Entidades;
using DiarioGym.Core.Negocio;

namespace DiarioGym.ViewModels;

public partial class EditorRutinaViewModel : ObservableObject, IQueryAttributable
{
    readonly RutinaRepositorio _rutinas;
    Rutina _rutina = new();

    public EditorRutinaViewModel(RutinaRepositorio rutinas)
    {
        _rutinas = rutinas;
        for (var i = 0; i < 7; i++)
        {
            var dia = new DiaToggle { Indice = i, Letra = Fechas.DiasCortos[i] };
            dia.ToggleCommand = new RelayCommand(() => dia.Activo = !dia.Activo);
            Dias.Add(dia);
        }
        AgregarEjercicio();
    }

    [ObservableProperty] private string titulo = "Nueva rutina";
    [ObservableProperty] private string nombre = "";
    [ObservableProperty] private string aviso = "";
    public ObservableCollection<DiaToggle> Dias { get; } = new();
    public ObservableCollection<EjercicioEdit> Ejercicios { get; } = new();

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            if (query.TryGetValue("id", out var v) && int.TryParse(v?.ToString(), out var id) && await _rutinas.ObtenerAsync(id) is Rutina r)
            {
                _rutina = r;
                Titulo = "Editar rutina";
                Nombre = r.Nombre;
                foreach (var d in Dias) d.Activo = r.DiasLista.Contains(d.Indice);
                Ejercicios.Clear();
                foreach (var e in await _rutinas.EjerciciosAsync(id))
                    Agregar(new EjercicioEdit
                    {
                        Nombre = e.Nombre,
                        Series = e.Series?.ToString() ?? "",
                        Reps = e.Reps?.ToString() ?? "",
                        Peso = e.PesoObjetivo is double p ? Formato.N(p) : "",
                    });
            }
            if (Ejercicios.Count == 0) AgregarEjercicio();
        }
        catch (Exception ex) { Aviso = "No se pudo abrir la rutina: " + ex.Message; }
    }

    void Agregar(EjercicioEdit e)
    {
        e.QuitarCommand = new RelayCommand(() => Ejercicios.Remove(e));
        Ejercicios.Add(e);
    }

    [RelayCommand]
    void AgregarEjercicio() => Agregar(new EjercicioEdit());

    [RelayCommand]
    async Task Guardar()
    {
        if (string.IsNullOrWhiteSpace(Nombre)) { Aviso = "Ponle un nombre a la rutina."; return; }
        _rutina.Nombre = Nombre.Trim();
        _rutina.DiasLista = Dias.Where(d => d.Activo).Select(d => d.Indice).ToArray();
        var ejercicios = Ejercicios.Where(e => !string.IsNullOrWhiteSpace(e.Nombre)).Select(e => new EjercicioRutina
        {
            Nombre = e.Nombre.Trim(),
            Series = Formato.LeerEntero(e.Series),
            Reps = Formato.LeerEntero(e.Reps),
            PesoObjetivo = Formato.LeerDecimal(e.Peso),
        }).ToList();
        try
        {
            await _rutinas.GuardarAsync(_rutina, ejercicios);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) { Aviso = "No se pudo guardar: " + ex.Message; }
    }

    [RelayCommand]
    Task Cancelar() => Shell.Current.GoToAsync("..");
}
