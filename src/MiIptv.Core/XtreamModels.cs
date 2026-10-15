using System.Text.Json.Serialization;

namespace MiIptv.Core;

/// <summary>Categoría (grupo) de Xtream: TV, películas o series.</summary>
public sealed record Category(
    [property: JsonPropertyName("category_id")] string CategoryId,
    [property: JsonPropertyName("category_name")] string CategoryName);

/// <summary>Canal de TV en vivo.</summary>
public sealed record LiveStream(
    [property: JsonPropertyName("stream_id")] int StreamId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("stream_icon")] string? StreamIcon,
    [property: JsonPropertyName("epg_channel_id")] string? EpgChannelId);

/// <summary>Película (VOD).</summary>
public sealed record VodStream(
    [property: JsonPropertyName("stream_id")] int StreamId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("stream_icon")] string? StreamIcon,
    [property: JsonPropertyName("container_extension")] string? ContainerExtension,
    [property: JsonPropertyName("rating")] string? Rating);

/// <summary>Serie.</summary>
public sealed record SeriesStream(
    [property: JsonPropertyName("series_id")] int SeriesId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("cover")] string? Cover,
    [property: JsonPropertyName("genre")] string? Genre,
    [property: JsonPropertyName("plot")] string? Plot,
    [property: JsonPropertyName("cast")] string? Cast,
    [property: JsonPropertyName("rating")] string? Rating,
    [property: JsonPropertyName("releaseDate")] string? ReleaseDate);

/// <summary>Episodio de una serie.</summary>
public sealed record Episode(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("episode_num")] int EpisodeNum,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("container_extension")] string? ContainerExtension,
    [property: JsonPropertyName("info")] EpisodeInfo? Info);

public sealed record EpisodeInfo(
    [property: JsonPropertyName("plot")] string? Plot,
    [property: JsonPropertyName("duration")] string? Duration,
    [property: JsonPropertyName("rating")] string? Rating);

/// <summary>Programa de la EPG.</summary>
public sealed record EpgProgram(
    string ChannelId,
    string Title,
    DateTimeOffset Start,
    DateTimeOffset Stop,
    string? Description);

/// <summary>Canal de una lista m3u.</summary>
public sealed record M3UChannel(string Name, string Url, string? Logo, string? Group);
