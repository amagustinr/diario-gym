using DiarioGym.Core.Datos;
using DiarioGym.ViewModels;
using DiarioGym.Views;
using Microsoft.Extensions.Logging;

namespace DiarioGym;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Capa de datos: una sola base SQLite en el almacenamiento privado de la app
        var ruta = Path.Combine(FileSystem.AppDataDirectory, "diariogym.db3");
        builder.Services.AddSingleton(new GymDb(ruta));
        builder.Services.AddSingleton<AsistenciaRepositorio>();
        builder.Services.AddSingleton<RutinaRepositorio>();
        builder.Services.AddSingleton<SerieRepositorio>();
        builder.Services.AddSingleton<PesoRepositorio>();
        builder.Services.AddSingleton<PerfilRepositorio>();

        // Presentación
        builder.Services.AddSingleton<AsistenciaViewModel>();
        builder.Services.AddSingleton<RutinasViewModel>();
        builder.Services.AddTransient<EditorRutinaViewModel>();
        builder.Services.AddSingleton<EvolucionViewModel>();
        builder.Services.AddSingleton<PesoViewModel>();

        builder.Services.AddSingleton<AsistenciaPage>();
        builder.Services.AddSingleton<RutinasPage>();
        builder.Services.AddTransient<EditorRutinaPage>();
        builder.Services.AddSingleton<EvolucionPage>();
        builder.Services.AddSingleton<PesoPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
