using System;
using System.Threading;
using System.Threading.Tasks;
using DiarioGym.Core.Entidades;
using SQLite;

namespace DiarioGym.Core.Datos
{
    /// <summary>Conexión única a la base SQLite. Crea las tablas y siembra rutinas de ejemplo la primera vez.</summary>
    public class GymDb
    {
        readonly string _ruta;
        readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        SQLiteAsyncConnection? _cn;

        public GymDb(string ruta) => _ruta = ruta;

        public async Task<SQLiteAsyncConnection> ConexionAsync()
        {
            if (_cn != null) return _cn;
            await _lock.WaitAsync();
            try
            {
                if (_cn != null) return _cn;
                var cn = new SQLiteAsyncConnection(_ruta,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
                await cn.CreateTablesAsync(CreateFlags.None,
                    typeof(Asistencia), typeof(Rutina), typeof(EjercicioRutina),
                    typeof(Serie), typeof(RegistroPeso), typeof(Perfil));
                if (await cn.Table<Perfil>().CountAsync() == 0)
                {
                    await cn.InsertAsync(new Perfil());
                    await SembrarRutinasAsync(cn);
                }
                _cn = cn;
                return cn;
            }
            finally { _lock.Release(); }
        }

        static async Task SembrarRutinasAsync(SQLiteAsyncConnection cn)
        {
            var ejemplos = new[]
            {
                ("Ejemplo · Empuje", new[] { 0, 3 }, new[] { ("Press banca", 4, 8), ("Press militar", 3, 10), ("Fondos", 3, 12) }),
                ("Ejemplo · Tirón", new[] { 1, 4 }, new[] { ("Dominadas", 4, 8), ("Remo con barra", 4, 10), ("Curl de bíceps", 3, 12) }),
                ("Ejemplo · Pierna", new[] { 2, 5 }, new[] { ("Sentadilla", 4, 6), ("Peso muerto rumano", 3, 10), ("Prensa", 3, 12) }),
            };
            var orden = 0;
            foreach (var (nombre, dias, ejercicios) in ejemplos)
            {
                var r = new Rutina { Nombre = nombre, DiasLista = dias, Orden = orden++ };
                await cn.InsertAsync(r);
                var i = 0;
                foreach (var (ej, series, reps) in ejercicios)
                    await cn.InsertAsync(new EjercicioRutina { RutinaId = r.Id, Nombre = ej, Series = series, Reps = reps, Orden = i++ });
            }
        }
    }
}
