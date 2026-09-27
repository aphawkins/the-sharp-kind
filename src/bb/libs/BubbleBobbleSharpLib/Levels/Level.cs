// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Text.Json.Serialization;

namespace BubbleBobbleSharpLib.Levels;

public class Level
{
    public int Number { get; set; }

    [JsonPropertyName("colors")]
    public int Colours { get; set; }

    public int Sidebar { get; set; }

    public int BubbleCurrent { get; set; }

    public int WrapOpenings { get; set; }

    public LevelPoint FoodDrop { get; set; } = new();

    public LevelPoint PowerupSpawn { get; set; } = new();

    public int SpawnFlags { get; set; }

    public IList<EnemySpawn> Enemies { get; init; } = [];

    public IList<string> Bitmap { get; init; } = [];
}
