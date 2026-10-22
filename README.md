# Diario de Gym

App Android personal para registrar **asistencia** al gym, **rutinas**, **series por ejercicio** (evolución) y **peso corporal** con IMC y % de grasa estimado. Hecha con .NET MAUI (C#) y SQLite local.

## Descargar e instalar en el celular

1. Desde el celular abre: **https://github.com/amagustinr/diario-gym/releases/latest/download/diario-gym.apk**
2. Abre el archivo descargado. Si Android lo pide, permite *Instalar apps desconocidas* para tu navegador.
3. Listo: aparece **Diario de Gym** en tus apps.

Cada push a `main` compila un APK nuevo (GitHub Actions) y lo publica en *Releases*.

## App web para compartir (Android y iPhone)

La carpeta `web/` es una PWA instalable con cuentas de usuario en **Supabase**: cada persona crea su cuenta y solo ve sus propios datos (Row Level Security). Se publica sola en **https://amagustinr.github.io/diario-gym/** con cada push a `main` que toque `web/` (workflow `pages.yml`).

- `supabase/schema.sql`: tablas `perfiles`, `rutinas`, `asistencias`, `registros`, `pesos` y sus políticas RLS. Se ejecuta una vez en el SQL Editor de Supabase.
- `web/config.js`: URL del proyecto y *anon key* (es pública por diseño; la seguridad la dan las políticas RLS).
- Funciona sin señal: los cambios se guardan en el teléfono y se suben al volver la conexión.
- Instalar: en Android, Chrome ofrece *Instalar app*; en iPhone, Safari → Compartir → *Añadir a pantalla de inicio*.

Para probarla en tu PC: `cd web && python -m http.server 8000` y abre http://localhost:8000.

## Estructura (N-capas)

```
src/
  DiarioGym.Core/        Biblioteca .NET Standard 2.1
    Entidades/           Tablas SQLite: Asistencia, Rutina, EjercicioRutina, Serie, RegistroPeso, Perfil
    Datos/               GymDb (conexión + creación de tablas) y repositorios
    Negocio/             Rachas, IMC, % grasa (Deurenberg), 1RM (Epley), fechas
  DiarioGym.App/         .NET MAUI (net10.0-android), MVVM con CommunityToolkit.Mvvm
    ViewModels/  Views/  Graficos/ (gráfica de línea y calendario con GraphicsView)
tests/
  DiarioGym.Core.Tests/  xUnit sobre Negocio y Datos
```

La base de datos vive en el almacenamiento privado de la app (`diariogym.db3`). Android la respalda en tu cuenta de Google (`allowBackup`).

## Compilar en tu PC

Con Visual Studio 2022/2026 y la carga de trabajo *.NET MAUI*: abre `DiarioGym.sln`, elige `DiarioGym.App` y ejecuta en tu celular (depuración USB) o en un emulador.

Por consola:

```
dotnet workload install maui-android
dotnet test tests/DiarioGym.Core.Tests
dotnet publish src/DiarioGym.App -f net10.0-android -c Release
```

## Firma del APK (para actualizar sin desinstalar)

Android solo instala una versión nueva encima de la anterior si ambas están firmadas con la **misma llave**. Sin llave propia, cada compilación usa una llave de depuración distinta y para actualizar tendrías que desinstalar (y perder los datos).

Para fijar la llave, en *Settings → Secrets and variables → Actions* del repo crea:

- `ANDROID_KEYSTORE_BASE64`: el archivo `.keystore` en base64 (alias `diariogym`)
- `ANDROID_KEYSTORE_PASSWORD`: su contraseña

Para generar una: `keytool -genkeypair -v -keystore diariogym.keystore -alias diariogym -keyalg RSA -keysize 2048 -validity 10000` y luego `base64 -w0 diariogym.keystore`.
