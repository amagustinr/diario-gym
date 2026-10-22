using System.Collections.Generic;
using System.Linq;
using DiarioGym.Core.Entidades;

namespace DiarioGym.Core.Negocio
{
    public class ResumenDia
    {
        public string Fecha { get; set; } = "";
        public double PesoMax { get; set; }
        public double UnaRmEstimada { get; set; }
        public double Volumen { get; set; }
        public List<Serie> Series { get; set; } = new List<Serie>();
    }

    public static class Progreso
    {
        /// <summary>1RM estimado con la fórmula de Epley: peso × (1 + reps/30).</summary>
        public static double Epley(double peso, int reps) => reps <= 1 ? peso : peso * (1 + reps / 30.0);

        public static List<ResumenDia> PorDia(IEnumerable<Serie> series) =>
            series.GroupBy(s => s.Fecha)
                  .OrderBy(g => g.Key)
                  .Select(g => new ResumenDia
                  {
                      Fecha = g.Key,
                      PesoMax = g.Max(s => s.Peso),
                      UnaRmEstimada = g.Max(s => Epley(s.Peso, s.Reps)),
                      Volumen = g.Sum(s => s.Peso * s.Reps),
                      Series = g.OrderBy(s => s.Ts).ToList(),
                  })
                  .ToList();
    }
}
