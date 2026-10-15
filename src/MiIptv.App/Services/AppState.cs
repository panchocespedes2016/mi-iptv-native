using MiIptv.Core;

namespace MiIptv.App.Services;

/// <summary>Estado global: configuración, perfil activo y favoritos.</summary>
public sealed class AppState
{
    public static AppState Current { get; } = new();

    public AppConfig Config { get; } = AppConfig.Load();
    public Profile? ActiveProfile { get; private set; }
    public XtreamClient? Xtream { get; private set; }
    public HashSet<string> Favorites { get; }

    private AppState()
    {
        Favorites = Config.LoadFavorites();
        var last = Config.Profiles.FirstOrDefault(p => p.Id == Config.LastProfileId)
                   ?? Config.Profiles.FirstOrDefault();
        if (last is not null) SetActiveProfile(last, save: false);
    }

    public void SetActiveProfile(Profile p, bool save = true)
    {
        ActiveProfile = p;
        Config.LastProfileId = p.Id;
        Xtream = p.Type == "xtream" ? new XtreamClient(p.Host, p.User, p.Pass) : null;
        if (save) Save();
    }

    public void Save()
    {
        Config.Save();
        Config.SaveFavorites(Favorites);
    }
}
