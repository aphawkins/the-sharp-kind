// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Text.Json;
using SharpKind;

namespace BubbleBobbleSharpLib.Levels;

public sealed class ZoneStore
{
    private static readonly JsonSerializerOptions s_options = new() { PropertyNameCaseInsensitive = true };

    private readonly LevelZones[] _zones;

    public ZoneStore(IReadOnlyList<LevelZones> zones)
    {
        ArgumentNullException.ThrowIfNull(zones);

        if (zones.Count != Count)
        {
            throw new SharpKindException($"A zone file has to hold {Count} entries, not {zones.Count}.");
        }

        _zones = [.. zones];
    }

    public static int Count => 100;

    public static ZoneStore Read(string path)
        => new(JsonSerializer.Deserialize<List<LevelZones>>(File.ReadAllText(path), s_options) ?? []);

    public IReadOnlyList<ZoneRect> Zones(int number)
        => number < 1 || number > Count
            ? throw new SharpKindException($"There is no level {number}; they run 1 to {Count}.")
            : (IReadOnlyList<ZoneRect>)_zones[number - 1].Rects;
}
