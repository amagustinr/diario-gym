using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiarioGym.Core.Datos;
using DiarioGym.Core.Entidades;
using DiarioGym.Core.Negocio;

namespace DiarioGym.ViewModels;

public partial class AsistenciaViewModel : ObservableObject
{
    static readonly Rutina SinRutina = new() { Id = 0, Nombre = "Sin rutina" };

    readonly AsistenciaRepositorio _asistencias;
    readonly RutinaRepositorio _rutinas;
    readonly SerieRepositorio _series;
    readonly PerfilRepositorio _perfil;

    /// <summary>Rutina elegida a mano para una fecha sin asistencia aún (para no perderla al recargar).</summary>
    readonly Dictionary<string, int> _rutinaElegida = new();
    bool _cargando;

    public AsistenciaViewModel(AsistenciaRepositorio asistencias, RutinaRepositorio rutinas, SerieRepositorio series, PerfilRepositorio perfil)
    {
        _asistencias = asistencias;
        _rutinas = rutinas;
        _series = series;
        _perfil = perfil;
    }

    public DateTime Hoy => DateTime.Today;
    public string HoyDia => DateTime.Today.ToString("dddd d", Formato.Es);
    public string HoyMes => DateTime.Today.ToString("MMMM yyyy", Formato.Es);

    [ObservableProperty] private bool asistioHoy;
    [ObservableProperty] private string textoBotonHoy = "Registrar asistencia";
    [ObservableProperty] private Color colorBotonHoy = Formato.ColorTema("Acento");

    [ObservableProperty] private int semana;
    [ObservableProperty] private int meta = 4;
    [ObservableProperty] private int mes;
    [ObservableProperty] private int racha;
    [ObservableProperty] private string totalTexto = "";
    [ObservableProperty] private double progresoSemana;
    [ObservableProperty] private HashSet<string> diasAsistidos = new();

    [ObservableProperty] private DateTime? fechaEntreno = DateTime.Today;
    [ObservableProperty] private string tituloEntreno = "Entreno de hoy";
    [ObservableProperty] private bool noEsHoy;
    [ObservableProperty] private string textoBotonFecha = "";
    [ObservableProperty] private Color colorBotonFecha = Formato.ColorTema("Acento");

    [ObservableProperty] private List<Rutina> rutinas = new() { SinRutina };
    [ObservableProperty] private Rutina? rutinaSeleccionada;
    public ObservableCollection<EjercicioHoy> Ejercicios { get; } = new();
    [ObservableProperty] private bool sinEjercicios = true;
    [ObservableProperty] private string aviso = "";

    [ObservableProperty] private string otroNombre = "";
    [ObservableProperty] private string otroPeso = "";
    [ObservableProperty] private string otroReps = "";

    string ClaveEntreno => Fechas.Clave(FechaEntreno is DateTime f && f.Date <= DateTime.Today ? f.Date : DateTime.Today);

    public async Task CargarAsync()
    {
        _cargando = true;
        try
        {
            var perfil = await _perfil.ObtenerAsync();
            Meta = perfil.MetaSemanal;

            var claves = (await _asistencias.TodasAsync()).Select(a => a.Fecha).ToList();
            DiasAsistidos = new HashSet<string>(claves);
            var r = AsistenciaServicio.Resumir(claves, DateTime.Today, Meta);
            Semana = r.Semana;
            Mes = r.Mes;
            Racha = r.Racha;
            TotalTexto = r.Total == 1 ? "1 día en total" : $"{r.Total} días en total";
            ProgresoSemana = Math.Min(1, (double)r.Semana / Meta);

            AsistioHoy = DiasAsistidos.Contains(Fechas.Clave(DateTime.Today));
            TextoBotonHoy = AsistioHoy ? "✓  Asistencia registrada" : "Registrar asistencia";
            ColorBotonHoy = Formato.ColorTema(AsistioHoy ? "Bien" : "Acento");

            var lista = new List<Rutina> { SinRutina };
            lista.AddRange(await _rutinas.TodasAsync());
            Rutinas = lista;

            await CargarEntrenoAsync();
        }
        finally { _cargando = false; }
    }

    async Task CargarEntrenoAsync()
    {
        var clave = ClaveEntreno;
        var fecha = Fechas.DeClave(clave);
        NoEsHoy = fecha != DateTime.Today;
        TituloEntreno = NoEsHoy ? $"Entreno del {Fechas.Corta(clave)}" : "Entreno de hoy";

        var asistencia = await _asistencias.ObtenerAsync(clave);
        TextoBotonFecha = asistencia != null ? $"✓  Asististe el {Fechas.Larga(clave)}" : $"Marcar asistencia del {Fechas.Larga(clave)}";
        ColorBotonFecha = Formato.ColorTema(asistencia != null ? "Bien" : "Acento");

        int? id = asistencia?.RutinaId ?? (_rutinaElegida.TryGetValue(clave, out var e) ? e : null);
        var rutina = id is int rid
            ? Rutinas.FirstOrDefault(x => x.Id == rid)
            : Rutinas.FirstOrDefault(x => x.Id != 0 && x.DiasLista.Contains(Fechas.DiaSemana(fecha)));

        var antes = _cargando;
        _cargando = true;
        RutinaSeleccionada = rutina ?? SinRutina;
        _cargando = antes;

        await CargarEjerciciosAsync();
    }

    async Task CargarEjerciciosAsync()
    {
        var clave = ClaveEntreno;
        var delDia = await _series.DeFechaAsync(clave);
        List<SerieChip> Chips(string ejercicio) => delDia.Where(s => s.Ejercicio == ejercicio)
            .Select(s => new SerieChip { Id = s.Id, Texto = $"{Formato.N(s.Peso)}×{s.Reps}", BorrarCommand = BorrarSerieCommand })
            .ToList();

        var items = new List<EjercicioHoy>();
        if (RutinaSeleccionada is { Id: > 0 } rutina)
        {
            foreach (var ej in await _rutinas.EjerciciosAsync(rutina.Id))
            {
                var ultima = await _series.UltimaAsync(ej.Nombre);
                var objetivo = $"{ej.Series?.ToString() ?? "?"}×{ej.Reps?.ToString() ?? "?"}";
                if (ej.PesoObjetivo is double po) objetivo += $" · {Formato.N(po)} kg";
                if (ultima != null) objetivo += $" · última {Formato.N(ultima.Peso)}×{ultima.Reps}";

                var item = new EjercicioHoy
                {
                    Nombre = ej.Nombre,
                    Objetivo = objetivo,
                    EnRutina = true,
                    Series = Chips(ej.Nombre),
                    PesoTexto = ultima != null ? Formato.N(ultima.Peso) : ej.PesoObjetivo is double p ? Formato.N(p) : "",
                    RepsTexto = ej.Reps?.ToString() ?? "",
                };
                item.AgregarCommand = new AsyncRelayCommand(() => GuardarSerieAsync(item.Nombre, item.PesoTexto, item.RepsTexto));
                items.Add(item);
            }
        }
        var enRutina = items.Select(i => i.Nombre).ToHashSet();
        foreach (var nombre in delDia.Select(s => s.Ejercicio).Distinct().Where(n => !enRutina.Contains(n)))
            items.Add(new EjercicioHoy { Nombre = nombre, Series = Chips(nombre) });

        Ejercicios.Clear();
        foreach (var i in items) Ejercicios.Add(i);
        SinEjercicios = Ejercicios.Count == 0;
    }

    partial void OnFechaEntrenoChanged(DateTime? value)
    {
        if (_cargando) return;
        Aviso = "";
        _ = Ejecutar(CargarConBloqueoAsync);
    }

    partial void OnRutinaSeleccionadaChanged(Rutina? value)
    {
        if (_cargando || value is null) return;
        _ = Ejecutar(async () =>
        {
            var clave = ClaveEntreno;
            _rutinaElegida[clave] = value.Id;
            var asistencia = await _asistencias.ObtenerAsync(clave);
            if (asistencia != null && asistencia.RutinaId != (value.Id == 0 ? null : value.Id))
            {
                asistencia.RutinaId = value.Id == 0 ? null : value.Id;
                await _asistencias.GuardarAsync(asistencia);
            }
            await CargarEjerciciosAsync();
        });
    }

    async Task CargarConBloqueoAsync()
    {
        _cargando = true;
        try { await CargarEntrenoAsync(); }
        finally { _cargando = false; }
    }

    async Task Ejecutar(Func<Task> accion)
    {
        try { await accion(); }
        catch (Exception ex) { Aviso = "No se pudo guardar: " + ex.Message; }
    }

    async Task AlternarAsync(string clave)
    {
        if (await _asistencias.ObtenerAsync(clave) != null)
            await _asistencias.EliminarAsync(clave);
        else
        {
            int? rid = clave == ClaveEntreno && RutinaSeleccionada is { Id: > 0 } r ? r.Id : null;
            await _asistencias.GuardarAsync(new Asistencia { Fecha = clave, RutinaId = rid });
        }
        await CargarAsync();
    }

    [RelayCommand]
    Task ToggleHoy() => Ejecutar(() => AlternarAsync(Fechas.Clave(DateTime.Today)));

    [RelayCommand]
    Task ToggleFecha() => Ejecutar(() => AlternarAsync(ClaveEntreno));

    [RelayCommand]
    void VolverAHoy() => FechaEntreno = DateTime.Today;

    public void SeleccionarDia(string clave) => FechaEntreno = Fechas.DeClave(clave);

    [RelayCommand]
    Task SubirMeta() => CambiarMetaAsync(1);

    [RelayCommand]
    Task BajarMeta() => CambiarMetaAsync(-1);

    Task CambiarMetaAsync(int delta) => Ejecutar(async () =>
    {
        var p = await _perfil.ObtenerAsync();
        p.MetaSemanal = Math.Clamp(p.MetaSemanal + delta, 1, 7);
        await _perfil.GuardarAsync(p);
        await CargarAsync();
    });

    [RelayCommand]
    Task AgregarSerieOtro() => GuardarSerieAsync(OtroNombre.Trim(), OtroPeso, OtroReps);

    async Task GuardarSerieAsync(string ejercicio, string pesoTexto, string repsTexto)
    {
        if (string.IsNullOrWhiteSpace(ejercicio)) { Aviso = "Escribe el nombre del ejercicio."; return; }
        var peso = Formato.LeerDecimal(pesoTexto);
        var reps = Formato.LeerEntero(repsTexto);
        if (peso is null || peso < 0 || reps is null || reps <= 0) { Aviso = "Pon el peso en kg y las repeticiones."; return; }

        await Ejecutar(async () =>
        {
            var clave = ClaveEntreno;
            int? rid = RutinaSeleccionada is { Id: > 0 } r ? r.Id : null;
            await _series.AgregarAsync(new Serie
            {
                Fecha = clave, Ejercicio = ejercicio, Peso = peso.Value, Reps = reps.Value,
                RutinaId = rid, Ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });
            if (await _asistencias.ObtenerAsync(clave) == null)
                await _asistencias.GuardarAsync(new Asistencia { Fecha = clave, RutinaId = rid });
            await CargarAsync();
            Aviso = $"Guardado: {ejercicio} {Formato.N(peso.Value)} kg × {reps}";
        });
    }

    [RelayCommand]
    Task BorrarSerie(SerieChip? chip) => chip is null ? Task.CompletedTask : Ejecutar(async () =>
    {
        await _series.EliminarAsync(chip.Id);
        await CargarEjerciciosAsync();
        Aviso = "Serie borrada";
    });
}
