using DiarioGym.Core.Entidades;
using DiarioGym.Core.Negocio;
using Xunit;

namespace DiarioGym.Core.Tests;

public class NegocioTests
{
    [Fact]
    public void Racha_cuenta_semanas_que_cumplen_la_meta()
    {
        // Hoy: jueves 22 oct 2026. Semana actual con 1 día (no cumple meta 2), las dos anteriores sí.
        var hoy = new DateTime(2026, 10, 22);
        var fechas = new[] { "2026-10-20", "2026-10-13", "2026-10-15", "2026-10-06", "2026-10-08", "2026-09-22" };
        var r = AsistenciaServicio.Resumir(fechas, hoy, metaSemanal: 2);
        Assert.Equal(1, r.Semana);
        Assert.Equal(2, r.Racha);
        Assert.Equal(5, r.Mes);
        Assert.Equal(6, r.Total);
    }

    [Fact]
    public void Racha_incluye_semana_actual_si_ya_cumplio()
    {
        var hoy = new DateTime(2026, 10, 22);
        var r = AsistenciaServicio.Resumir(new[] { "2026-10-19", "2026-10-21", "2026-10-14" }, hoy, 2);
        Assert.Equal(1, r.Racha);
    }

    [Fact]
    public void Imc_y_grasa_deurenberg()
    {
        var imc = ComposicionCorporal.Imc(81, 175)!.Value;
        Assert.Equal(26.45, imc, 2);
        var edad = ComposicionCorporal.Edad("1990-05-10", new DateTime(2026, 10, 22));
        Assert.Equal(36, edad);
        var grasa = ComposicionCorporal.GrasaDeurenberg(imc, edad, "H")!.Value;
        Assert.Equal(1.2 * imc + 0.23 * 36 - 10.8 - 5.4, grasa, 6);
        Assert.Equal("sobrepeso", ComposicionCorporal.CategoriaImc(imc));
        Assert.Null(ComposicionCorporal.GrasaDeurenberg(imc, null, "H"));
    }

    [Fact]
    public void Edad_resta_un_anio_antes_del_cumpleanios()
    {
        Assert.Equal(35, ComposicionCorporal.Edad("1990-11-01", new DateTime(2026, 10, 22)));
    }

    [Fact]
    public void Progreso_agrupa_por_dia()
    {
        var series = new[]
        {
            new Serie { Fecha = "2026-10-20", Ejercicio = "Sentadilla", Peso = 80, Reps = 5, Ts = 1 },
            new Serie { Fecha = "2026-10-20", Ejercicio = "Sentadilla", Peso = 90, Reps = 3, Ts = 2 },
            new Serie { Fecha = "2026-10-15", Ejercicio = "Sentadilla", Peso = 85, Reps = 5, Ts = 3 },
        };
        var dias = Progreso.PorDia(series);
        Assert.Equal(new[] { "2026-10-15", "2026-10-20" }, dias.Select(d => d.Fecha));
        Assert.Equal(90, dias[1].PesoMax);
        Assert.Equal(670, dias[1].Volumen);
        Assert.Equal(99, dias[1].UnaRmEstimada, 6);
    }
}
