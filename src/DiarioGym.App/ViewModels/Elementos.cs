using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DiarioGym.ViewModels;

/// <summary>Una serie mostrada como chip "80×5" con su botón para borrarla.</summary>
public class SerieChip
{
    public int Id { get; init; }
    public string Texto { get; init; } = "";
    public ICommand? BorrarCommand { get; init; }
}

/// <summary>Un ejercicio en la tarjeta del entreno del día.</summary>
public partial class EjercicioHoy : ObservableObject
{
    public string Nombre { get; init; } = "";
    public string Objetivo { get; init; } = "";
    public bool EnRutina { get; init; }
    public List<SerieChip> Series { get; init; } = new();
    public ICommand? AgregarCommand { get; set; }

    [ObservableProperty] private string pesoTexto = "";
    [ObservableProperty] private string repsTexto = "";
}

public class DiaHistorial
{
    public string Titulo { get; init; } = "";
    public string Detalle { get; init; } = "";
    public List<SerieChip> Series { get; init; } = new();
}

public class PesoFila
{
    public string Titulo { get; init; } = "";
    public string Detalle { get; init; } = "";
    public string KgTexto { get; init; } = "";
    public string DeltaTexto { get; init; } = "";
    public Color DeltaColor { get; init; } = Colors.Transparent;
    public ICommand? BorrarCommand { get; init; }
}

public class RutinaItem
{
    public string Nombre { get; init; } = "";
    public string DiasTexto { get; init; } = "";
    public string EjerciciosTexto { get; init; } = "";
    public ICommand? EditarCommand { get; init; }
    public ICommand? EliminarCommand { get; init; }
}

public partial class DiaToggle : ObservableObject
{
    public int Indice { get; init; }
    public string Letra { get; init; } = "";
    public ICommand? ToggleCommand { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Fondo), nameof(ColorTexto))]
    private bool activo;

    public Color Fondo => Activo ? Formato.ColorTema("Acento") : Colors.Transparent;
    public Color ColorTexto => Activo ? Formato.ColorTema("AcentoTexto") : Formato.ColorTema("Tinta");
}

public partial class EjercicioEdit : ObservableObject
{
    [ObservableProperty] private string nombre = "";
    [ObservableProperty] private string series = "";
    [ObservableProperty] private string reps = "";
    [ObservableProperty] private string peso = "";
    public ICommand? QuitarCommand { get; set; }
}
