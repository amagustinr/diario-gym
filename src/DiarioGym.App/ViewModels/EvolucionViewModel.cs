using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiarioGym.Core.Datos;
using DiarioGym.Core.Negocio;
using DiarioGym.Graficos;

namespace DiarioGym.ViewModels;

public partial class EvolucionViewModel : ObservableObject
{
    readonly SerieRepositorio _series;
    bool _cargando;

    public EvolucionViewModel(SerieRepositorio series) => _series = series;

    [ObservableProperty] private List<string> ejercicios = new();
    [ObservableProperty] private string? ejercicioSeleccionado;
    [ObservableProperty] private bool hayDatos;
    [ObservableProperty] private bool sinDatos = true;

    [ObservableProperty] private string mejorPeso = "—";
    [ObservableProperty] private string mejorRm = "—";
    [ObservableProperty] private string cambio = "—";
    [ObservableProperty] private Color colorCambio = Formato.ColorTema("Tinta");

    [ObservableProperty] private string metrica = "max";
    [ObservableProperty] private Color fondoMax = Colors.Transparent;
    [ObservableProperty] private Color fondoRm = Colors.Transparent;
    [ObservableProperty] private Color fondoVol = Colors.Transparent;
    [ObservableProperty] private Color textoMax = Colors.Gray;
    [ObservableProperty] private Color textoRm = Colors.Gray;
    [ObservableProperty] private Color textoVol = Colors.Gray;

    [ObservableProperty] private List<PuntoGrafica> puntos = new();
    [ObservableProperty] private int decimales = 1;
    [ObservableProperty] private string tituloHistorial = "";
    [ObservableProperty] private List<DiaHistorial> historial = new();

    public async Task CargarAsync()
    {
        _cargando = true;
        try
        {
            var lista = await _series.EjerciciosAsync();
            var actual = EjercicioSeleccionado;
            Ejercicios = lista;
            EjercicioSeleccionado = actual != null && lista.Contains(actual) ? actual : lista.FirstOrDefault();
            HayDatos = lista.Count > 0;
            SinDatos = !HayDatos;
        }
        finally { _cargando = false; }
        await CargarEjercicioAsync();
    }

    partial void OnEjercicioSeleccionadoChanged(string? value)
    {
        if (!_cargando) _ = CargarEjercicioAsync();
    }

    async Task CargarEjercicioAsync()
    {
        PintarSegmentos();
        if (EjercicioSeleccionado is not string ej) { Puntos = new(); Historial = new(); return; }

        var dias = Progreso.PorDia(await _series.DeEjercicioAsync(ej));
        if (dias.Count == 0) return;

        MejorPeso = Formato.N(dias.Max(d => d.PesoMax));
        MejorRm = Formato.N(dias.Max(d => d.UnaRmEstimada), 0);
        var dif = dias[^1].PesoMax - dias[0].PesoMax;
        Cambio = Formato.Delta(dif);
        ColorCambio = Formato.ColorTema(dif > 0 ? "Bien" : "Tinta");

        Decimales = Metrica == "max" ? 1 : 0;
        Puntos = dias.Select(d => new PuntoGrafica
        {
            X = Fechas.DeClave(d.Fecha),
            Y = Metrica switch { "rm" => d.UnaRmEstimada, "vol" => d.Volumen, _ => d.PesoMax },
        }).ToList();

        TituloHistorial = dias.Count == 1 ? "HISTORIAL · 1 SESIÓN" : $"HISTORIAL · {dias.Count} SESIONES";
        Historial = dias.AsEnumerable().Reverse().Select(d => new DiaHistorial
        {
            Titulo = Fechas.Larga(d.Fecha),
            Detalle = $"{Formato.N(d.Volumen, 0)} kg de volumen",
            Series = d.Series.Select(s => new SerieChip { Id = s.Id, Texto = $"{Formato.N(s.Peso)}×{s.Reps}", BorrarCommand = BorrarSerieCommand }).ToList(),
        }).ToList();
    }

    void PintarSegmentos()
    {
        Color F(string m) => Metrica == m ? Formato.ColorTema("Acento") : Colors.Transparent;
        Color T(string m) => Metrica == m ? Formato.ColorTema("AcentoTexto") : Formato.ColorTema("Apagado");
        FondoMax = F("max"); FondoRm = F("rm"); FondoVol = F("vol");
        TextoMax = T("max"); TextoRm = T("rm"); TextoVol = T("vol");
    }

    [RelayCommand]
    Task CambiarMetrica(string m)
    {
        Metrica = m;
        return CargarEjercicioAsync();
    }

    [RelayCommand]
    async Task BorrarSerie(SerieChip? chip)
    {
        if (chip is null) return;
        await _series.EliminarAsync(chip.Id);
        await CargarAsync();
    }
}
