// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Text.Json;
using SharpKind;

namespace BubbleBobbleSharpLib.Levels;

public sealed class LevelStore
{
    private static readonly JsonSerializerOptions s_options = new() { PropertyNameCaseInsensitive = true };

    private readonly Level[] _levels;

    public LevelStore(IReadOnlyList<Level> levels)
    {
        ArgumentNullException.ThrowIfNull(levels);

        if (levels.Count != Count)
        {
            throw new SharpKindException($"A level file has to hold {Count} entries, not {levels.Count}.");
        }

        _levels = [.. levels];
    }

    public static int Count => 100;

    public static LevelStore Read(string path)
        => new(JsonSerializer.Deserialize<List<Level>>(File.ReadAllText(path), s_options) ?? []);

    public Level Level(int number)
        => number < 1 || number > Count
            ? throw new SharpKindException($"There is no level {number}; they run 1 to {Count}.")
            : _levels[number - 1];
}
