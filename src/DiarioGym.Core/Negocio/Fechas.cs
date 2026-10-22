using System;
using System.Globalization;

namespace DiarioGym.Core.Negocio
{
    public static class Fechas
    {
        static readonly CultureInfo Es = new CultureInfo("es-ES");
        public static readonly string[] DiasCortos = { "L", "M", "X", "J", "V", "S", "D" };

        public static string Clave(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static DateTime DeClave(string s) => DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>Día de la semana con lunes = 0.</summary>
        public static int DiaSemana(DateTime d) => ((int)d.DayOfWeek + 6) % 7;
        public static DateTime Lunes(DateTime d) => d.Date.AddDays(-DiaSemana(d));

        public static string Corta(string clave) => DeClave(clave).ToString("d MMM", Es).Replace(".", "");
        public static string Larga(string clave) => DeClave(clave).ToString("dddd d MMM", Es).Replace(".", "");
    }
}
