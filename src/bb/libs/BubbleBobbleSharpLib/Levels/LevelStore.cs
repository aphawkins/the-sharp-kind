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

    // `$45A7`-`$4627`: the enemies of every level, the class of each moved a step: 0 to 1 and 6 to 5. Super mode, which a level's
    // data then keeps for as long as the game is on.
    internal LevelStore Super() => new([.. _levels.Select(Harder)]);

    private static Level Harder(Level level) => new()
    {
        Number = level.Number,
        Colours = level.Colours,
        Sidebar = level.Sidebar,
        BubbleCurrent = level.BubbleCurrent,
        WrapOpenings = level.WrapOpenings,
        FoodDrop = level.FoodDrop,
        PowerupSpawn = level.PowerupSpawn,
        SpawnFlags = level.SpawnFlags,
        Enemies = [.. level.Enemies.Select(Harder)],
        Bitmap = level.Bitmap,
    };

    private static EnemySpawn Harder(EnemySpawn enemy) => new()
    {
        X = enemy.X,
        Y = enemy.Y,
        Type = enemy.Type switch
        {
            0 => 1,
            6 => 5,
            _ => enemy.Type,
        },
        Delay = enemy.Delay,
        FaceLeft = enemy.FaceLeft,
        MoveLeft = enemy.MoveLeft,
        Byte1Low = enemy.Byte1Low,
    };
}
