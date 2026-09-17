// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Text.Json.Serialization;

namespace BubbleBobbleSharpLib.Levels;

//// JSON serializable

// One of the hundred levels, as rebb64's levels.txt describes it.
public class Level
{
    public int Number { get; set; }

    // Spelled "colors" in levels.txt and in the export, so the name is pinned rather than renamed.
    [JsonPropertyName("colors")]
    public int Colours { get; set; }

    public int Sidebar { get; set; }

    public int BubbleCurrent { get; set; }

    public int WrapOpenings { get; set; }

    public LevelPoint FoodDrop { get; set; } = new();

    public LevelPoint PowerupSpawn { get; set; } = new();

    public int SpawnFlags { get; set; }

    public IList<EnemySpawn> Enemies { get; init; } = [];

    // 23 rows of 32 characters, '#' solid and '.' open - the same shape levels.txt draws them in,
    // which is worth keeping: a level is readable in the asset file and in a failing test's output.
    public IList<string> Bitmap { get; init; } = [];
}
