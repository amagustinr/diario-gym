using DiarioGym.Core.Negocio;

namespace DiarioGym.Graficos;

public class PuntoGrafica
{
    public DateTime X { get; init; }
    public double Y { get; init; }
}

/// <summary>Gráfica de línea simple: rejilla, área, puntos y valor final resaltado.</summary>
public class GraficaLinea : IDrawable
{
    public IReadOnlyList<PuntoGrafica> Puntos { get; set; } = Array.Empty<PuntoGrafica>();
    public double? Meta { get; set; }
    public string Unidad { get; set; } = "kg";
    public int Decimales { get; set; } = 1;

    public void Draw(ICanvas canvas, RectF r)
    {
        var tinta = Formato.ColorTema("Tinta");
        var apagado = Formato.ColorTema("Apagado");
        var linea = Formato.ColorTema("Linea");
        var acento = Formato.ColorTema("Acento");
        var superficie = Formato.ColorTema("Superficie");
        var racha = Formato.ColorTema("Racha");

        canvas.FontSize = 11;
        if (Puntos.Count < 2)
        {
            canvas.FontColor = apagado;
            canvas.FontSize = 14;
            canvas.DrawString("Con dos registros o más verás aquí la gráfica.", r, HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        const float pl = 42, pr = 14, pt = 24, pb = 24;
        var t0 = Puntos[0].X.Ticks;
        var t1 = Puntos[^1].X.Ticks;
        if (t1 == t0) t1 = t0 + TimeSpan.TicksPerDay;

        var valores = Puntos.Select(p => p.Y).ToList();
        if (Meta is double m) valores.Add(m);
        double lo = valores.Min(), hi = valores.Max();
        var pad = (hi - lo) * 0.12;
        if (pad == 0) pad = 1;
        lo -= pad; hi += pad;

        float X(long t) => r.Left + pl + (float)((t - t0) / (double)(t1 - t0)) * (r.Width - pl - pr);
        float Y(double v) => r.Top + pt + (float)((hi - v) / (hi - lo)) * (r.Height - pt - pb);

        // Rejilla y etiquetas del eje Y
        canvas.StrokeSize = 1;
        for (var i = 0; i < 4; i++)
        {
            var v = lo + (hi - lo) * i / 3;
            var y = Y(v);
            canvas.StrokeColor = linea;
            canvas.DrawLine(r.Left + pl, y, r.Right - pr, y);
            canvas.FontColor = apagado;
            canvas.DrawString(Formato.N(v, hi - lo < 10 ? 1 : 0), r.Left, y - 7, pl - 6, 14, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        if (Meta is double meta)
        {
            var y = Y(meta);
            canvas.StrokeColor = racha;
            canvas.StrokeSize = 1.5f;
            canvas.StrokeDashPattern = new float[] { 4, 4 };
            canvas.DrawLine(r.Left + pl, y, r.Right - pr, y);
            canvas.StrokeDashPattern = null;
            canvas.FontColor = apagado;
            canvas.DrawString($"meta {Formato.N(meta)}", r.Right - pr - 100, y - 18, 100, 14, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        var xy = Puntos.Select(p => new PointF(X(p.X.Ticks), Y(p.Y))).ToList();

        var area = new PathF();
        area.MoveTo(xy[0].X, r.Bottom - pb);
        foreach (var p in xy) area.LineTo(p);
        area.LineTo(xy[^1].X, r.Bottom - pb);
        area.Close();
        canvas.FillColor = acento.WithAlpha(0.12f);
        canvas.FillPath(area);

        var trazo = new PathF();
        trazo.MoveTo(xy[0]);
        foreach (var p in xy.Skip(1)) trazo.LineTo(p);
        canvas.StrokeColor = acento;
        canvas.StrokeSize = 2.5f;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawPath(trazo);

        if (xy.Count <= 40)
        {
            canvas.FillColor = acento;
            foreach (var p in xy.Take(xy.Count - 1)) canvas.FillCircle(p, 2.5f);
        }

        var fin = xy[^1];
        canvas.FillColor = superficie;
        canvas.FillCircle(fin, 5);
        canvas.StrokeColor = acento;
        canvas.StrokeSize = 3;
        canvas.DrawCircle(fin, 5);

        canvas.FontColor = tinta;
        canvas.FontSize = 13;
        canvas.DrawString($"{Formato.N(Puntos[^1].Y, Decimales)} {Unidad}", fin.X - 120, Math.Max(fin.Y - 26, r.Top), 116, 16,
            HorizontalAlignment.Right, VerticalAlignment.Center);

        canvas.FontColor = apagado;
        canvas.FontSize = 11;
        canvas.DrawString(Fechas.Corta(Fechas.Clave(Puntos[0].X)), r.Left + pl, r.Bottom - 16, 100, 14, HorizontalAlignment.Left, VerticalAlignment.Center);
        canvas.DrawString(Fechas.Corta(Fechas.Clave(Puntos[^1].X)), r.Right - pr - 100, r.Bottom - 16, 100, 14, HorizontalAlignment.Right, VerticalAlignment.Center);
    }
}
