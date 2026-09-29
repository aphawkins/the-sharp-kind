// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;

namespace SharpKind.Audio;

/// <summary>A music entry of an asset manifest, which for a SID file may name one of its songs.</summary>
public static class MusicPath
{
    /// <summary>Splits a music path into its file and the song a trailing <c>#n</c> asks for.</summary>
    /// <param name="path">The path: <c>tune.sid#8</c> is the eighth song of <c>tune.sid</c>, and <c>tune.mid</c> is that file.</param>
    /// <returns>The file, and the 1-based song, or none if the path does not ask for one.</returns>
    public static (string File, int? Song) Parse(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        int mark = path.LastIndexOf('#');

        return mark > 0 && int.TryParse(path.AsSpan(mark + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int song)
            ? (path[..mark], song)
            : (path, null);
    }

    /// <summary>Gets a value indicating whether a path names a SID file.</summary>
    /// <param name="path">The path, with or without a song.</param>
    /// <returns>Whether it is a SID file.</returns>
    public static bool IsSid(string path)
        => string.Equals(Path.GetExtension(Parse(path).File), ".sid", StringComparison.OrdinalIgnoreCase);
}
