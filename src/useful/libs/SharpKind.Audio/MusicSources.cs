// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.Midi;
using SharpKind.Audio.SoundFonts;
using SharpKind.Audio.Wav;

namespace SharpKind.Audio;

/// <summary>Opens a music track of any supported format as a stream, for both backends to play the same way.</summary>
public static class MusicSources
{
    private static readonly Lock s_gate = new();
    private static readonly Dictionary<string, SoundFont> s_soundFonts = new(StringComparer.Ordinal);

    /// <summary>Opens a track: a <c>.wav</c>, or a <c>.mid</c> played through a SoundFont. SID songs are <see cref="SidSound"/>'s.</summary>
    /// <param name="path">The track.</param>
    /// <param name="soundFontPath">The SoundFont a <c>.mid</c> plays through, if one is configured.</param>
    /// <param name="repeat">Whether to start the track again when it ends.</param>
    /// <returns>The stream, which the caller disposes.</returns>
    /// <exception cref="SharpKindException">The format is not supported, or a <c>.mid</c> has no SoundFont.</exception>
    public static IMusicSource Open(string path, string? soundFontPath, bool repeat)
    {
        ArgumentNullException.ThrowIfNull(path);

        return HasExtension(path, ".mid")
            ? new MidiMusicStream(MidiFile.Read(path), LoadSoundFont(soundFontPath), repeat)
            : HasExtension(path, ".wav")
                ? new SampleMusicStream(WavDecoder.Decode(path), repeat)
                : throw UnsupportedFormat(path);
    }

    internal static bool HasExtension(string path, string extension)
        => string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase);

    internal static SharpKindException UnsupportedFormat(string path) => new($"'{path}' is not a supported audio format.");

    // A SoundFont is several megabytes, so each is read once and shared; the synthesizers only read it.
    internal static SoundFont LoadSoundFont(string? path)
    {
        if (path is null)
        {
            throw new SharpKindException("No SoundFont is configured in the asset manifest; cannot play a .mid asset.");
        }

        lock (s_gate)
        {
            if (!s_soundFonts.TryGetValue(path, out SoundFont? soundFont))
            {
                soundFont = SoundFont.Read(path);
                s_soundFonts[path] = soundFont;
            }

            return soundFont;
        }
    }
}
