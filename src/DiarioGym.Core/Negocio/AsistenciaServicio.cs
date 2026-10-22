using System;
using System.Collections.Generic;
using System.Linq;

namespace DiarioGym.Core.Negocio
{
    public class ResumenAsistencia
    {
        public int Semana { get; set; }
        public int Mes { get; set; }
        public int Total { get; set; }
        /// <summary>Semanas seguidas cumpliendo la meta (la actual cuenta solo si ya la cumpliste).</summary>
        public int Racha { get; set; }
    }

    public static class AsistenciaServicio
    {
        public static ResumenAsistencia Resumir(IEnumerable<string> fechas, DateTime hoy, int metaSemanal)
        {
            var dias = fechas.Select(Fechas.DeClave).Where(d => d <= hoy.Date).ToList();
            var lunes = Fechas.Lunes(hoy);
            var porSemana = dias.GroupBy(Fechas.Lunes).ToDictionary(g => g.Key, g => g.Count());

            int Cuenta(DateTime l) => porSemana.TryGetValue(l, out var n) ? n : 0;

            var semana = lunes;
            if (Cuenta(semana) < metaSemanal) semana = semana.AddDays(-7);
            int racha = 0;
            while (Cuenta(semana) >= metaSemanal) { racha++; semana = semana.AddDays(-7); }

            return new ResumenAsistencia
            {
                Semana = Cuenta(lunes),
                Mes = dias.Count(d => d.Year == hoy.Year && d.Month == hoy.Month),
                Total = dias.Count,
                Racha = racha,
            };
        }
    }
}
