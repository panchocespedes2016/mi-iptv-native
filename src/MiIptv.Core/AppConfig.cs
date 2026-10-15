using System.Text.Json;

namespace MiIptv.Core;

/// <summary>Configuración portable: vive en data\ junto al exe. Formato compatible con la app Tauri.</summary>
public sealed class AppConfig
{
    public List<Profile> Profiles { get; set; } = new();
    public string? LastProfileId { get; set; }
    public AppSettings Settings { get; set; } = new();

    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string DataDir
    {
        get
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "data");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static AppConfig Load()
    {
        try
        {
            var p = Path.Combine(DataDir, "config.json");
            if (File.Exists(p))
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(p), Opts) ?? new();
        }
        catch { }
        return new AppConfig();
    }

    public void Save()
    {
        try { File.WriteAllText(Path.Combine(DataDir, "config.json"), JsonSerializer.Serialize(this, Opts)); }
        catch { }
    }

    public HashSet<string> LoadFavorites()
    {
        try
        {
            var p = Path.Combine(DataDir, "favoritos.json");
            if (File.Exists(p))
                return JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(p), Opts) ?? new();
        }
        catch { }
        return new HashSet<string>();
    }

    public void SaveFavorites(HashSet<string> favs)
    {
        try { File.WriteAllText(Path.Combine(DataDir, "favoritos.json"), JsonSerializer.Serialize(favs, Opts)); }
        catch { }
    }
}

public sealed class Profile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Type { get; set; } = "xtream"; // xtream | m3u
    public string Host { get; set; } = "";
    public string User { get; set; } = "";
    public string Pass { get; set; } = "";
    public string? Url { get; set; }       // m3u por URL
    public string? Content { get; set; }   // m3u local (contenido)
}

public sealed class AppSettings
{
    public int TimeshiftMinutes { get; set; } = 30; // 0 = desactivado
    public SubtitleStyle Subs { get; set; } = new();
}

public sealed class SubtitleStyle
{
    public string Font { get; set; } = "Arial";
    public int Size { get; set; } = 24;
    public string Color { get; set; } = "#ffffff";
    public string Bg { get; set; } = "#000000";
    public int BgOpacity { get; set; } = 75;
    public bool Bold { get; set; } = false;
}
