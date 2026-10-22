using System.Globalization;

namespace DiarioGym;

/// <summary>Formato de números y colores del tema actual.</summary>
public static class Formato
{
    public static readonly CultureInfo Es = new("es-ES");

    public static string N(double v, int decimales = 1) =>
        v.ToString(decimales == 0 ? "0" : "0." + new string('#', decimales), Es);

    public static string Delta(double v, int decimales = 1) => (v > 0 ? "+" : "") + N(v, decimales);

    /// <summary>Acepta "82,5" o "82.5".</summary>
    public static double? LeerDecimal(string? texto) =>
        double.TryParse((texto ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;

    public static int? LeerEntero(string? texto) =>
        int.TryParse((texto ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    public static Color ColorTema(string clave)
    {
        var oscuro = Application.Current?.RequestedTheme == AppTheme.Dark;
        var k = oscuro ? clave + "Oscuro" : clave;
        return Application.Current?.Resources.TryGetValue(k, out var v) == true && v is Color c ? c : Colors.Gray;
    }
}
