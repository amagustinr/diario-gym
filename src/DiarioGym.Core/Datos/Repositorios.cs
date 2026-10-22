using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiarioGym.Core.Entidades;

namespace DiarioGym.Core.Datos
{
    public class AsistenciaRepositorio
    {
        readonly GymDb _db;
        public AsistenciaRepositorio(GymDb db) => _db = db;

        public async Task<List<Asistencia>> TodasAsync() => await (await _db.ConexionAsync()).Table<Asistencia>().ToListAsync();

        public async Task<Asistencia?> ObtenerAsync(string fecha) =>
            await (await _db.ConexionAsync()).FindAsync<Asistencia>(fecha);

        public async Task GuardarAsync(Asistencia a) => await (await _db.ConexionAsync()).InsertOrReplaceAsync(a);

        public async Task EliminarAsync(string fecha) => await (await _db.ConexionAsync()).DeleteAsync<Asistencia>(fecha);
    }

    public class RutinaRepositorio
    {
        readonly GymDb _db;
        public RutinaRepositorio(GymDb db) => _db = db;

        public async Task<List<Rutina>> TodasAsync() =>
            await (await _db.ConexionAsync()).Table<Rutina>().OrderBy(r => r.Orden).ThenBy(r => r.Nombre).ToListAsync();

        public async Task<Rutina?> ObtenerAsync(int id) => await (await _db.ConexionAsync()).FindAsync<Rutina>(id);

        public async Task<List<EjercicioRutina>> EjerciciosAsync(int rutinaId) =>
            await (await _db.ConexionAsync()).Table<EjercicioRutina>().Where(e => e.RutinaId == rutinaId).OrderBy(e => e.Orden).ToListAsync();

        /// <summary>Guarda la rutina y reemplaza su lista de ejercicios en una transacción.</summary>
        public async Task GuardarAsync(Rutina rutina, IList<EjercicioRutina> ejercicios)
        {
            var cn = await _db.ConexionAsync();
            await cn.RunInTransactionAsync(t =>
            {
                if (rutina.Id == 0)
                {
                    rutina.Orden = t.Table<Rutina>().Count();
                    t.Insert(rutina);
                }
                else t.Update(rutina);

                t.Execute("DELETE FROM EjercicioRutina WHERE RutinaId = ?", rutina.Id);
                for (var i = 0; i < ejercicios.Count; i++)
                {
                    var e = ejercicios[i];
                    e.Id = 0;
                    e.RutinaId = rutina.Id;
                    e.Orden = i;
                    t.Insert(e);
                }
            });
        }

        /// <summary>Elimina la rutina y sus ejercicios. Las series ya registradas se conservan.</summary>
        public async Task EliminarAsync(int id)
        {
            var cn = await _db.ConexionAsync();
            await cn.RunInTransactionAsync(t =>
            {
                t.Execute("DELETE FROM EjercicioRutina WHERE RutinaId = ?", id);
                t.Delete<Rutina>(id);
            });
        }
    }

    public class SerieRepositorio
    {
        readonly GymDb _db;
        public SerieRepositorio(GymDb db) => _db = db;

        public async Task<List<Serie>> DeFechaAsync(string fecha) =>
            await (await _db.ConexionAsync()).Table<Serie>().Where(s => s.Fecha == fecha).OrderBy(s => s.Ts).ToListAsync();

        public async Task<List<Serie>> DeEjercicioAsync(string ejercicio) =>
            await (await _db.ConexionAsync()).Table<Serie>().Where(s => s.Ejercicio == ejercicio).ToListAsync();

        public async Task<Serie?> UltimaAsync(string ejercicio) =>
            await (await _db.ConexionAsync()).Table<Serie>().Where(s => s.Ejercicio == ejercicio)
                .OrderByDescending(s => s.Fecha).ThenByDescending(s => s.Ts).FirstOrDefaultAsync();

        /// <summary>Nombres de ejercicio con series, del más reciente al más antiguo.</summary>
        public async Task<List<string>> EjerciciosAsync()
        {
            var filas = await (await _db.ConexionAsync()).QueryAsync<Serie>(
                "SELECT Ejercicio, MAX(Fecha) AS Fecha FROM Serie GROUP BY Ejercicio ORDER BY MAX(Fecha) DESC, Ejercicio");
            return filas.Select(f => f.Ejercicio).ToList();
        }

        public async Task AgregarAsync(Serie s) => await (await _db.ConexionAsync()).InsertAsync(s);
        public async Task EliminarAsync(int id) => await (await _db.ConexionAsync()).DeleteAsync<Serie>(id);
    }

    public class PesoRepositorio
    {
        readonly GymDb _db;
        public PesoRepositorio(GymDb db) => _db = db;

        public async Task<List<RegistroPeso>> TodosAsync() =>
            await (await _db.ConexionAsync()).Table<RegistroPeso>().OrderBy(p => p.Fecha).ToListAsync();

        public async Task GuardarAsync(RegistroPeso p) => await (await _db.ConexionAsync()).InsertOrReplaceAsync(p);
        public async Task EliminarAsync(string fecha) => await (await _db.ConexionAsync()).DeleteAsync<RegistroPeso>(fecha);
    }

    public class PerfilRepositorio
    {
        readonly GymDb _db;
        public PerfilRepositorio(GymDb db) => _db = db;

        public async Task<Perfil> ObtenerAsync() =>
            await (await _db.ConexionAsync()).FindAsync<Perfil>(1) ?? new Perfil();

        public async Task GuardarAsync(Perfil p)
        {
            p.Id = 1;
            await (await _db.ConexionAsync()).InsertOrReplaceAsync(p);
        }
    }
}
