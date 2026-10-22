using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiarioGym.Core.Datos;
using DiarioGym.Core.Entidades;
using DiarioGym.Core.Negocio;
using DiarioGym.Graficos;

namespace DiarioGym.ViewModels;

public partial class PesoViewModel : ObservableObject
{
    readonly PesoRepositorio _pesos;
    readonly PerfilRepositorio _perfil;
    Perfil _p = new();

    public PesoViewModel(PesoRepositorio pesos, PerfilRepositorio perfil)
    {
        _pesos = pesos;
        _perfil = perfil;
    }

    public DateTime Hoy => DateTime.Today;
    public List<string> Sexos { get; } = new() { "Hombre", "Mujer" };

    [ObservableProperty] private DateTime? fechaRegistro = DateTime.Today;
    [ObservableProperty] private string kgTexto = "";
    [ObservableProperty] private string aviso = "";

    [ObservableProperty] private string actual = "—";
    [ObservableProperty] private string cambio30 = "—";
    [ObservableProperty] private string cambioTotal = "—";
    [ObservableProperty] private string imc = "—";
    [ObservableProperty] private string imcEtiqueta = "IMC";
    [ObservableProperty] private string grasa = "—";
    [ObservableProperty] private string magra = "—";
    [ObservableProperty] private bool perfilIncompleto = true;

    [ObservableProperty] private string grafica = "kg";
    [ObservableProperty] private Color fondoKg = Colors.Transparent;
    [ObservableProperty] private Color fondoGrasa = Colors.Transparent;
    [ObservableProperty] private Color textoKg = Colors.Gray;
    [ObservableProperty] private Color textoGrasa = Colors.Gray;
    [ObservableProperty] private List<PuntoGrafica> puntos = new();
    [ObservableProperty] private double? metaGrafica;
    [ObservableProperty] private string unidadGrafica = "kg";

    [ObservableProperty] private string metaTexto = "";
    [ObservableProperty] private string faltaMeta = "";
    [ObservableProperty] private bool hayFaltaMeta;

    [ObservableProperty] private string alturaTexto = "";
    [ObservableProperty] private string? sexo;
    [ObservableProperty] private DateTime? nacimiento = DateTime.Today.AddYears(-30);

    [ObservableProperty] private List<PesoFila> historial = new();
    [ObservableProperty] private bool sinHistorial = true;

    public async Task CargarAsync()
    {
        _p = await _perfil.ObtenerAsync();
        AlturaTexto = _p.AlturaCm is double h ? Formato.N(h) : "";
        Sexo = _p.Sexo == "H" ? "Hombre" : _p.Sexo == "M" ? "Mujer" : null;
        if (_p.Nacimiento != null) Nacimiento = Fechas.DeClave(_p.Nacimiento);
        MetaTexto = _p.PesoMeta is double m ? Formato.N(m) : "";
        PerfilIncompleto = _p.AlturaCm is null || _p.Sexo is null || _p.Nacimiento is null;

        var pesos = await _pesos.TodosAsync();
        var ultimo = pesos.LastOrDefault();
        var primero = pesos.FirstOrDefault();
        if (string.IsNullOrEmpty(KgTexto) && ultimo != null) KgTexto = Formato.N(ultimo.Kg);

        Actual = ultimo != null ? Formato.N(ultimo.Kg) : "—";
        CambioTotal = ultimo != null && pesos.Count > 1 ? Formato.Delta(ultimo.Kg - primero!.Kg) : "—";
        Cambio30 = "—";
        if (ultimo != null && pesos.Count > 1)
        {
            var limite = Fechas.Clave(Fechas.DeClave(ultimo.Fecha).AddDays(-30));
            var base30 = pesos.LastOrDefault(p => string.CompareOrdinal(p.Fecha, limite) <= 0) ?? primero!;
            if (base30 != ultimo) Cambio30 = Formato.Delta(ultimo.Kg - base30.Kg);
        }

        double? GrasaDe(RegistroPeso p) => ComposicionCorporal.GrasaDeurenberg(
            ComposicionCorporal.Imc(p.Kg, _p.AlturaCm), ComposicionCorporal.Edad(_p.Nacimiento, Fechas.DeClave(p.Fecha)), _p.Sexo);

        var imcAct = ultimo != null ? ComposicionCorporal.Imc(ultimo.Kg, _p.AlturaCm) : null;
        var grasaAct = ultimo != null ? GrasaDe(ultimo) : null;
        Imc = imcAct is double i ? Formato.N(i) : "—";
        ImcEtiqueta = imcAct is double i2 ? $"IMC · {ComposicionCorporal.CategoriaImc(i2)}" : "IMC";
        Grasa = grasaAct is double g ? Formato.N(g) : "—";
        Magra = grasaAct is double g2 && ultimo != null ? Formato.N(ultimo.Kg * (1 - g2 / 100)) : "—";

        FaltaMeta = _p.PesoMeta is double meta && ultimo != null
            ? $"Te faltan {Formato.N(Math.Abs(ultimo.Kg - meta))} kg para tu meta de {Formato.N(meta)} kg." : "";
        HayFaltaMeta = FaltaMeta.Length > 0;

        Color F(string m) => Grafica == m ? Formato.ColorTema("Acento") : Colors.Transparent;
        Color T(string m) => Grafica == m ? Formato.ColorTema("AcentoTexto") : Formato.ColorTema("Apagado");
        FondoKg = F("kg"); FondoGrasa = F("gr"); TextoKg = T("kg"); TextoGrasa = T("gr");

        if (Grafica == "gr")
        {
            UnidadGrafica = "%";
            MetaGrafica = null;
            Puntos = pesos.Select(p => (p, g: GrasaDe(p))).Where(t => t.g != null)
                .Select(t => new PuntoGrafica { X = Fechas.DeClave(t.p.Fecha), Y = t.g!.Value }).ToList();
        }
        else
        {
            UnidadGrafica = "kg";
            MetaGrafica = _p.PesoMeta;
            Puntos = pesos.Select(p => new PuntoGrafica { X = Fechas.DeClave(p.Fecha), Y = p.Kg }).ToList();
        }

        var filas = new List<PesoFila>();
        for (var k = pesos.Count - 1; k >= 0; k--)
        {
            var p = pesos[k];
            var delta = k > 0 ? p.Kg - pesos[k - 1].Kg : 0;
            var gi = GrasaDe(p);
            var ii = ComposicionCorporal.Imc(p.Kg, _p.AlturaCm);
            var fecha = p.Fecha;
            filas.Add(new PesoFila
            {
                Titulo = Fechas.Larga(p.Fecha),
                Detalle = string.Join(" · ", new[] { ii is double x ? $"IMC {Formato.N(x)}" : "", gi is double y ? $"{Formato.N(y)} % grasa" : "" }.Where(s => s != "")),
                KgTexto = $"{Formato.N(p.Kg)} kg",
                DeltaTexto = delta == 0 ? "" : Formato.Delta(delta),
                DeltaColor = Formato.ColorTema(delta > 0 ? "Mal" : "Bien"),
                BorrarCommand = new AsyncRelayCommand(async () => { await _pesos.EliminarAsync(fecha); await CargarAsync(); }),
            });
        }
        Historial = filas;
        SinHistorial = filas.Count == 0;
    }

    [RelayCommand]
    async Task Guardar()
    {
        var kg = Formato.LeerDecimal(KgTexto);
        if (kg is null || kg <= 20 || kg >= 400) { Aviso = "Escribe tu peso en kg, por ejemplo 78,5."; return; }
        var fecha = Fechas.Clave((FechaRegistro ?? DateTime.Today).Date);
        await _pesos.GuardarAsync(new RegistroPeso { Fecha = fecha, Kg = Math.Round(kg.Value, 1) });
        Aviso = $"Peso del {Fechas.Corta(fecha)} guardado";
        await CargarAsync();
    }

    [RelayCommand]
    async Task GuardarMeta()
    {
        if (string.IsNullOrWhiteSpace(MetaTexto)) _p.PesoMeta = null;
        else
        {
            var m = Formato.LeerDecimal(MetaTexto);
            if (m is null || m <= 20 || m >= 400) { Aviso = "Escribe una meta en kg."; return; }
            _p.PesoMeta = m;
        }
        await _perfil.GuardarAsync(_p);
        Aviso = _p.PesoMeta is null ? "Meta quitada" : "Meta guardada";
        await CargarAsync();
    }

    [RelayCommand]
    async Task GuardarPerfil()
    {
        var h = Formato.LeerDecimal(AlturaTexto);
        if (h is null || h < 100 || h > 250) { Aviso = "Escribe tu altura en centímetros, por ejemplo 175."; return; }
        if (Sexo is null) { Aviso = "Elige tu sexo para estimar el % de grasa."; return; }
        _p.AlturaCm = Math.Round(h.Value, 1);
        _p.Sexo = Sexo == "Hombre" ? "H" : "M";
        _p.Nacimiento = Fechas.Clave((Nacimiento ?? DateTime.Today.AddYears(-30)).Date);
        await _perfil.GuardarAsync(_p);
        Aviso = "Perfil guardado";
        await CargarAsync();
    }

    [RelayCommand]
    Task CambiarGrafica(string g)
    {
        Grafica = g;
        return CargarAsync();
    }
}
