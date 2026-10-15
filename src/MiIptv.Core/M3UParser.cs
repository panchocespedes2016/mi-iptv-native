using System.Text.RegularExpressions;

namespace MiIptv.Core;

/// <summary>Parser mínimo de listas m3u/m3u8 (#EXTINF).</summary>
public static class M3UParser
{
    private static readonly Regex ExtInf = new(@"#EXTINF:[^,]*tvg-id=""([^""]*)""[^,]*tvg-logo=""([^""]*)""[^,]*group-title=""([^""]*)"",\s*(.*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ExtInfLoose = new(@"#EXTINF:[^,]*,\s*(.*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string Attr(string line, string name)
    {
        var m = Regex.Match(line, name + @"=""([^""]*)""", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : "";
    }

    public static List<M3UChannel> Parse(string text)
    {
        var channels = new List<M3UChannel>();
        string? name = null, logo = null, group = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                var m = ExtInf.Match(line);
                if (m.Success)
                {
                    group = m.Groups[3].Value;
                    logo = m.Groups[2].Value;
                    name = m.Groups[4].Value.Trim();
                }
                else
                {
                    logo = Attr(line, "tvg-logo");
                    group = Attr(line, "group-title");
                    var loose = ExtInfLoose.Match(line);
                    name = loose.Success ? loose.Groups[1].Value.Trim() : null;
                }
            }
            else if (!string.IsNullOrEmpty(line) && !line.StartsWith('#') && name is not null)
            {
                channels.Add(new M3UChannel(name, line,
                    string.IsNullOrEmpty(logo) ? null : logo,
                    string.IsNullOrEmpty(group) ? null : group));
                name = logo = group = null;
            }
        }
        return channels;
    }
}
