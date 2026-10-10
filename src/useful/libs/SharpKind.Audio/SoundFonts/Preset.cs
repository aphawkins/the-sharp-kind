// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.SoundFonts;

// A preset's zones, each already holding its global zone's values beneath its own.
internal sealed class Preset(string name, int bank, int patch, IReadOnlyList<Zone> zones)
{
    internal string Name { get; } = name;

    internal int Bank { get; } = bank;

    internal int Patch { get; } = patch;

    internal IReadOnlyList<Zone> Zones { get; } = zones;
}
