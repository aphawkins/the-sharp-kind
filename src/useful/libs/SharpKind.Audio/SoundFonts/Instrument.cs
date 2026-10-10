// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.SoundFonts;

// An instrument's zones, each already holding the defaults and its global zone's values beneath its own.
internal sealed class Instrument(string name, IReadOnlyList<Zone> zones)
{
    internal string Name { get; } = name;

    internal IReadOnlyList<Zone> Zones { get; } = zones;
}
