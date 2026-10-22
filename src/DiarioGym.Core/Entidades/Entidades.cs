using System;
using SQLite;

namespace DiarioGym.Core.Entidades
{
    /// <summary>Un día en que fuiste al gym. La fecha (yyyy-MM-dd) es la clave.</summary>
    [Table("Asistencia")]
    public class Asistencia
    {
        [PrimaryKey] public string Fecha { get; set; } = "";
        public int? RutinaId { get; set; }
    }

    [Table("Rutina")]
    public class Rutina
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [NotNull] public string Nombre { get; set; } = "";
        /// <summary>Días de la semana separados por coma: 0 = lunes … 6 = domingo.</summary>
        public string Dias { get; set; } = "";
        public int Orden { get; set; }

        [Ignore]
        public int[] DiasLista
        {
            get => string.IsNullOrWhiteSpace(Dias) ? new int[0] : Array.ConvertAll(Dias.Split(','), int.Parse);
            set => Dias = string.Join(",", value);
        }

        public override string ToString() => Nombre;
    }

    [Table("EjercicioRutina")]
    public class EjercicioRutina
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public int RutinaId { get; set; }
        [NotNull] public string Nombre { get; set; } = "";
        public int? Series { get; set; }
        public int? Reps { get; set; }
        public double? PesoObjetivo { get; set; }
        public int Orden { get; set; }
    }

    /// <summary>Una serie registrada: peso × repeticiones de un ejercicio en una fecha.</summary>
    [Table("Serie")]
    public class Serie
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public string Fecha { get; set; } = "";
        [Indexed] public string Ejercicio { get; set; } = "";
        public double Peso { get; set; }
        public int Reps { get; set; }
        public int? RutinaId { get; set; }
        public long Ts { get; set; }
    }

    [Table("RegistroPeso")]
    public class RegistroPeso
    {
        [PrimaryKey] public string Fecha { get; set; } = "";
        public double Kg { get; set; }
    }

    /// <summary>Datos personales y metas. Siempre hay una sola fila (Id = 1).</summary>
    [Table("Perfil")]
    public class Perfil
    {
        [PrimaryKey] public int Id { get; set; } = 1;
        public double? AlturaCm { get; set; }
        /// <summary>"H" hombre, "M" mujer.</summary>
        public string? Sexo { get; set; }
        public string? Nacimiento { get; set; }
        public int MetaSemanal { get; set; } = 4;
        public double? PesoMeta { get; set; }
    }
}
