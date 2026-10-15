using System.Net;
using System.Text.Json;
using System.Xml.Linq;

namespace MiIptv.Core;

/// <summary>Cliente de la API player_api.php de Xtream Codes.</summary>
public sealed class XtreamClient
{
    private readonly string _host;
    private readonly string _user;
    private readonly string _pass;
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public XtreamClient(string host, string username, string password, HttpClient? http = null)
    {
        _host = host.TrimEnd('/');
        _user = username;
        _pass = password;
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(40) };
    }

    private string Url(string action, string extra = "") =>
        $"{_host}/player_api.php?username={WebUtility.UrlEncode(_user)}" +
        $"&password={WebUtility.UrlEncode(_pass)}&action={action}{extra}";

    private async Task<JsonElement> GetJsonAsync(string action, string extra = "", CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(Url(action, extra), ct);
        resp.EnsureSuccessStatusCode();
        var text = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<JsonElement>(text, JsonOpts);
    }

    private static List<T> ToList<T>(JsonElement el) =>
        el.ValueKind == JsonValueKind.Array
            ? JsonSerializer.Deserialize<List<T>>(el.GetRawText(), JsonOpts) ?? new()
            : new();

    public async Task<bool> LoginAsync(CancellationToken ct = default)
    {
        var el = await GetJsonAsync("", ct: ct);
        return el.TryGetProperty("user_info", out var u) &&
               u.TryGetProperty("auth", out var a) && a.GetInt32() == 1;
    }

    public async Task<List<Category>> GetLiveCategoriesAsync(CancellationToken ct = default) =>
        ToList<Category>(await GetJsonAsync("get_live_categories", ct: ct));

    public async Task<List<LiveStream>> GetLiveStreamsAsync(string? categoryId = null, CancellationToken ct = default) =>
        ToList<LiveStream>(await GetJsonAsync("get_live_streams",
            categoryId is null ? "" : $"&category_id={WebUtility.UrlEncode(categoryId)}", ct));

    public string LiveUrl(int streamId, string ext = "m3u8") =>
        $"{_host}/live/{_user}/{_pass}/{streamId}.{ext}";

    public async Task<List<Category>> GetVodCategoriesAsync(CancellationToken ct = default) =>
        ToList<Category>(await GetJsonAsync("get_vod_categories", ct: ct));

    public async Task<List<VodStream>> GetVodStreamsAsync(string? categoryId = null, CancellationToken ct = default) =>
        ToList<VodStream>(await GetJsonAsync("get_vod_streams",
            categoryId is null ? "" : $"&category_id={WebUtility.UrlEncode(categoryId)}", ct));

    public string MovieUrl(int streamId, string container) =>
        $"{_host}/movie/{_user}/{_pass}/{streamId}.{container}";

    public async Task<JsonElement> GetVodInfoAsync(int vodId, CancellationToken ct = default) =>
        await GetJsonAsync("get_vod_info", $"&vod_id={vodId}", ct);

    public async Task<List<Category>> GetSeriesCategoriesAsync(CancellationToken ct = default) =>
        ToList<Category>(await GetJsonAsync("get_series_categories", ct: ct));

    public async Task<List<SeriesStream>> GetSeriesAsync(string? categoryId = null, CancellationToken ct = default) =>
        ToList<SeriesStream>(await GetJsonAsync("get_series",
            categoryId is null ? "" : $"&category_id={WebUtility.UrlEncode(categoryId)}", ct));

    public async Task<JsonElement> GetSeriesInfoAsync(int seriesId, CancellationToken ct = default) =>
        await GetJsonAsync("get_series_info", $"&series_id={seriesId}", ct);

    public string EpisodeUrl(string episodeId, string container) =>
        $"{_host}/series/{_user}/{_pass}/{episodeId}.{container}";

    /// <summary>EPG corta de un canal (horarios "yyyyMMddTHHmmss +0000").</summary>
    public async Task<List<EpgProgram>> GetShortEpgAsync(int streamId, int limit = 20, CancellationToken ct = default)
    {
        var el = await GetJsonAsync("get_short_epg", $"&stream_id={streamId}&limit={limit}", ct);
        var list = new List<EpgProgram>();
        if (el.TryGetProperty("epg_listings", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in arr.EnumerateArray())
            {
                var title = p.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                var desc = p.TryGetProperty("description", out var d) ? d.GetString() : null;
                var chId = p.TryGetProperty("epg_id", out var e) ? e.GetString() ?? "" : "";
                if (p.TryGetProperty("start", out var s) && p.TryGetProperty("stop", out var e2) &&
                    TryParseXtreamTime(s.GetString(), out var start) &&
                    TryParseXtreamTime(e2.GetString(), out var stop))
                {
                    list.Add(new EpgProgram(chId, title, start, stop, desc));
                }
            }
        }
        return list;
    }

    /// <summary>Guía XMLTV completa del proveedor.</summary>
    public async Task<List<EpgProgram>> GetXmlTvAsync(CancellationToken ct = default)
    {
        var url = $"{_host}/xmltv.php?username={WebUtility.UrlEncode(_user)}&password={WebUtility.UrlEncode(_pass)}";
        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var doc = await Task.Run(() => XDocument.Load(stream), ct);
        var list = new List<EpgProgram>();
        foreach (var p in doc.Descendants("programme"))
        {
            var ch = p.Attribute("channel")?.Value ?? "";
            var title = p.Element("title")?.Value ?? "";
            var desc = p.Element("desc")?.Value;
            if (TryParseXtreamTime(p.Attribute("start")?.Value, out var start) &&
                TryParseXtreamTime(p.Attribute("stop")?.Value, out var stop))
            {
                list.Add(new EpgProgram(ch, title, start, stop, desc));
            }
        }
        return list;
    }

    internal static bool TryParseXtreamTime(string? s, out DateTimeOffset dto)
    {
        dto = default;
        if (string.IsNullOrWhiteSpace(s) || s.Length < 14) return false;
        // "20231015T120000 +0000" o "20231015120000 +0000"
        var t = s.Replace("T", "").Trim();
        var parts = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts[0].Length < 14) return false;
        var d = parts[0];
        var off = parts.Length > 1 ? parts[1] : "+0000";
        if (!DateTimeOffset.TryParseExact(d[..14], "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var baseDt)) return false;
        var sign = off.StartsWith('-') ? -1 : 1;
        var digits = new string(off.Where(char.IsDigit).ToArray()).PadRight(4, '0');
        var oh = int.Parse(digits[..2]); var om = int.Parse(digits[2..4]);
        dto = new DateTimeOffset(baseDt.Ticks, TimeSpan.FromMinutes(sign * (oh * 60 + om)));
        return true;
    }
}
