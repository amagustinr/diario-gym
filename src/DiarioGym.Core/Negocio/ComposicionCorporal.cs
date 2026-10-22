using System;

namespace DiarioGym.Core.Negocio
{
    public static class ComposicionCorporal
    {
        public static double? Imc(double kg, double? alturaCm)
        {
            if (alturaCm is null || alturaCm <= 0) return null;
            var m = alturaCm.Value / 100.0;
            return kg / (m * m);
        }

        public static int? Edad(string? nacimiento, DateTime enFecha)
        {
            if (string.IsNullOrEmpty(nacimiento)) return null;
            var n = Fechas.DeClave(nacimiento!);
            var edad = enFecha.Year - n.Year;
            if (enFecha.Date < n.AddYears(edad)) edad--;
            return edad > 0 ? edad : (int?)null;
        }

        /// <summary>% de grasa estimado (Deurenberg 1991): 1,2·IMC + 0,23·edad − 10,8·sexo − 5,4 (sexo: 1 hombre, 0 mujer).</summary>
        public static double? GrasaDeurenberg(double? imc, int? edad, string? sexo)
        {
            if (imc is null || edad is null || string.IsNullOrEmpty(sexo)) return null;
            var s = sexo == "H" ? 1 : 0;
            return 1.2 * imc.Value + 0.23 * edad.Value - 10.8 * s - 5.4;
        }

        public static string CategoriaImc(double imc) =>
            imc < 18.5 ? "bajo peso" : imc < 25 ? "normal" : imc < 30 ? "sobrepeso" : "obesidad";
    }
}
