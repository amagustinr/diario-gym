using DiarioGym.Core.Negocio;

namespace DiarioGym.Graficos;

/// <summary>Calendario de las últimas 16 semanas: una columna por semana (lunes arriba).</summary>
public class MapaAsistencia : IDrawable
{
    public const int Semanas = 16;
    const float Hueco = 4;

    public ISet<string> Dias { get; set; } = new HashSet<string>();
    public string? Seleccion { get; set; }

    DateTime _inicio;
    float _lado, _x0, _y0;

    public void Draw(ICanvas canvas, RectF r)
    {
        var hoy = DateTime.Today;
        _inicio = Fechas.Lunes(hoy).AddDays(-(Semanas - 1) * 7);
        _lado = Math.Min((r.Width - Hueco * (Semanas - 1)) / Semanas, (r.Height - Hueco * 6) / 7);
        _x0 = r.Left + (r.Width - (_lado * Semanas + Hueco * (Semanas - 1))) / 2;
        _y0 = r.Top;

        var celda = Formato.ColorTema("Celda");
        var acento = Formato.ColorTema("Acento");
        var racha = Formato.ColorTema("Racha");
        var tinta = Formato.ColorTema("Tinta");

        for (var i = 0; i < Semanas * 7; i++)
        {
            var dia = _inicio.AddDays(i);
            if (dia > hoy) break;
            var clave = Fechas.Clave(dia);
            var (x, y) = Posicion(i);

            canvas.FillColor = Dias.Contains(clave) ? acento : celda;
            canvas.FillRoundedRectangle(x, y, _lado, _lado, 3);

            var borde = dia == hoy ? racha : clave == Seleccion ? tinta : null;
            if (borde != null)
            {
                canvas.StrokeColor = borde;
                canvas.StrokeSize = 2;
                canvas.DrawRoundedRectangle(x - 1.5f, y - 1.5f, _lado + 3, _lado + 3, 4);
            }
        }
    }

    (float x, float y) Posicion(int i) => (_x0 + (i / 7) * (_lado + Hueco), _y0 + (i % 7) * (_lado + Hueco));

    /// <summary>Fecha (yyyy-MM-dd) de la celda tocada, o null si el toque cayó fuera o en el futuro.</summary>
    public string? FechaEn(float x, float y)
    {
        if (_lado <= 0) return null;
        var col = (int)Math.Floor((x - _x0) / (_lado + Hueco));
        var fila = (int)Math.Floor((y - _y0) / (_lado + Hueco));
        if (col < 0 || col >= Semanas || fila < 0 || fila > 6) return null;
        var dia = _inicio.AddDays(col * 7 + fila);
        return dia > DateTime.Today ? null : Fechas.Clave(dia);
    }
}
