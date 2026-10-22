using DiarioGym.Core.Datos;
using DiarioGym.Core.Entidades;
using Xunit;

namespace DiarioGym.Core.Tests;

public class DatosTests : IDisposable
{
    readonly string _ruta = Path.Combine(Path.GetTempPath(), $"gym-{Guid.NewGuid():N}.db3");
    readonly GymDb _db;

    public DatosTests() => _db = new GymDb(_ruta);

    public void Dispose()
    {
        SQLite.SQLiteAsyncConnection.ResetPool();
        try { File.Delete(_ruta); } catch { }
    }

    [Fact]
    public async Task Primera_vez_siembra_tres_rutinas_con_ejercicios()
    {
        var repo = new RutinaRepositorio(_db);
        var rutinas = await repo.TodasAsync();
        Assert.Equal(3, rutinas.Count);
        Assert.Equal(new[] { 0, 3 }, rutinas[0].DiasLista);
        Assert.Equal(3, (await repo.EjerciciosAsync(rutinas[0].Id)).Count);
    }

    [Fact]
    public async Task Guardar_rutina_reemplaza_ejercicios()
    {
        var repo = new RutinaRepositorio(_db);
        var r = new Rutina { Nombre = "Full body", DiasLista = new[] { 1 } };
        await repo.GuardarAsync(r, new List<EjercicioRutina> { new() { Nombre = "A" }, new() { Nombre = "B" } });
        await repo.GuardarAsync(r, new List<EjercicioRutina> { new() { Nombre = "C" } });
        var ejs = await repo.EjerciciosAsync(r.Id);
        Assert.Equal("C", Assert.Single(ejs).Nombre);
        await repo.EliminarAsync(r.Id);
        Assert.Null(await repo.ObtenerAsync(r.Id));
    }

    [Fact]
    public async Task Series_ultima_y_lista_de_ejercicios()
    {
        var repo = new SerieRepositorio(_db);
        await repo.AgregarAsync(new Serie { Fecha = "2026-10-01", Ejercicio = "Press banca", Peso = 60, Reps = 8, Ts = 1 });
        await repo.AgregarAsync(new Serie { Fecha = "2026-10-10", Ejercicio = "Press banca", Peso = 65, Reps = 6, Ts = 2 });
        await repo.AgregarAsync(new Serie { Fecha = "2026-10-05", Ejercicio = "Sentadilla", Peso = 90, Reps = 5, Ts = 3 });
        Assert.Equal(65, (await repo.UltimaAsync("Press banca"))!.Peso);
        Assert.Equal(new[] { "Press banca", "Sentadilla" }, await repo.EjerciciosAsync());
    }

    [Fact]
    public async Task Asistencia_peso_y_perfil()
    {
        var asis = new AsistenciaRepositorio(_db);
        await asis.GuardarAsync(new Asistencia { Fecha = "2026-10-15", RutinaId = 2 });
        Assert.Equal(2, (await asis.ObtenerAsync("2026-10-15"))!.RutinaId);
        await asis.EliminarAsync("2026-10-15");
        Assert.Empty(await asis.TodasAsync());

        var peso = new PesoRepositorio(_db);
        await peso.GuardarAsync(new RegistroPeso { Fecha = "2026-10-22", Kg = 81 });
        await peso.GuardarAsync(new RegistroPeso { Fecha = "2026-10-22", Kg = 80.5 });
        Assert.Equal(80.5, Assert.Single(await peso.TodosAsync()).Kg);

        var perfil = new PerfilRepositorio(_db);
        var p = await perfil.ObtenerAsync();
        Assert.Equal(4, p.MetaSemanal);
        p.AlturaCm = 175; p.Sexo = "H";
        await perfil.GuardarAsync(p);
        Assert.Equal(175, (await perfil.ObtenerAsync()).AlturaCm);
    }
}
