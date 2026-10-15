# MiIPTV Nativa (Windows)

Reproductor IPTV portable para Windows, **nativo** (sin motor web): C# + WPF con
**LibVLCSharp** (motor de VLC) como reproductor. Reproduce todo lo que VLC
reproduce: HLS, MPEG-TS, MP4/MKV, HEVC/H.265, AC3/DTS, etc.

- Un solo `.exe` portable (self-contained); la configuración vive en `data\` junto al exe.
- Fuentes: Xtream Codes y listas m3u/m3u8.
- Interfaz en español, tema oscuro.

## Etapas

1. ✅ Esqueleto + pipeline CI (ventana principal, navegación, motor VLC inicializado)
2. ⏳ Perfiles y configuración (Xtream + m3u, `data\config.json`, favoritos)
3. ⏳ TV en vivo + reproductor nativo (LibVLC)
4. ⏳ Guía EPG
5. ⏳ Películas y series
6. ⏳ Favoritos, multivista, timeshift, ajustes
7. ⏳ Pulido y despliegue

## Compilación

Cada push a `main` compila en GitHub Actions (`windows-latest`):
`dotnet publish` → `mi-iptv-native-X.Y.Z.exe` + `MiIptv-Native-X.Y.Z-win64.zip`.
La versión sube sola (patch +1) con `scripts/bump_version.py`.
