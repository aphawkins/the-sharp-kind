// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Bubbles;

// $0AAB against the real thing, a pass at a time.
//
// Both captures were read out of VICE running rebb64, on 2026-09-26, on level 1 with one player. Two
// exec checkpoints bracket the routine: $0A4B, the `jsr D_0AAB`, and $0A4E, where it returns. A pass
// is left out if $08 moved between the two, or if any entity byte did: $0AAB writes none, so a
// changed frame or Y means the IRQ's $1CBD ran inside it. $08 alone does not catch that - four
// off-map passes changed a player's frame or Y with $08 still.
//
// **Both are interventions.** Blowing into a wall pops the bubble at once, so a plain stick never
// leaves a bubble at the player's height. Every twelfth pass, at $0A4B and before the bytes were
// read, a plain bubble (type $04, state 0) was written into slot 17 beside the player.
//
//   * push-level-1.txt: ten pixels away on the side the player faced, at a Y offset of -6 to +6 so
//     that $A9D6 takes 0, 2, 4 and 6, and its row and column set as $232A and $2341 set them.
//   * push-offmap-level-1.txt: six pixels to the left, at the player's Y, but with the row written
//     as $00 to $03 - the rows that leave the row in Y at $0B0E. To keep the player alive and
//     facing left, $B4-$B9 were zeroed (the enemies' states; their X and Y stay, which is all an
//     index of 3 reads) and $8520 was set to 4 if it was below 4. When the row was 3, a second
//     bubble went into slot 10 eight pixels left of entity 3, for Y = 3 to push. Rows $1D and $1E
//     were tried too: the C64 crashed to $0000 a pass after the first, somewhere after $0AAB.
//
// Each line is the bytes before and after: $B2 to $FF, $8520 to $8527, $A9B2 to $A9E7 and $AA0C to
// $AA2F. Across the whole dumps nothing else changed but $3C and $40/$41, the routine's own
// scratch, and bytes of the IRQ's.
//
// The map is the port's own level 1.
[Trait("Level", "Integration")]
public sealed class BubblePushGoldenTests
{
    private const int States = 0;
    private const int PlayerX = States + EntityTable.Capacity;
    private const int PlayerY = PlayerX + EntityTable.Capacity;
    private const int Types = PlayerY + EntityTable.Capacity;
    private const int Columns = Types + ObjectTable.Capacity;
    private const int Rows = Columns + ObjectTable.Capacity;
    private const int Frames = Rows + ObjectTable.Capacity;
    private const int ObjectStates = Frames + EntityTable.Capacity;
    private const int SubXs = ObjectStates + ObjectTable.Capacity;
    private const int SubYs = SubXs + ObjectTable.Capacity;
    private const int Xs = SubYs + ObjectTable.Capacity;
    private const int Ys = Xs + ObjectTable.Capacity;
    private const int Length = Ys + ObjectTable.Capacity;

    private const string Plain = "push-level-1.txt";
    private const string OffMap = "push-offmap-level-1.txt";

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Asset("zones.json"));
    private static readonly SolidMap s_map = SolidMap.Build(s_levels.Level(1), s_zones.Zones(1));

    public static TheoryData<string, int> Lines()
    {
        TheoryData<string, int> lines = [];

        foreach (string file in (string[])[Plain, OffMap])
        {
            for (int i = 0; i < Capture(file).Count; i++)
            {
                lines.Add(file, i);
            }
        }

        return lines;
    }

    [Theory]
    [MemberData(nameof(Lines))]
    public void PushesExactlyAsTheGameDoes(string file, int line)
    {
        (byte[] before, byte[] after) = Capture(file)[line];

        EntityTable entities = new();
        ObjectTable objects = new();
        Load(before, entities, objects);

        new BubblePush(entities, objects).Update(s_map);

        Assert.Equal(Convert.ToHexString(after), Convert.ToHexString(Save(entities, objects)));
    }

    // A push each way, one across a column, and a snap back out of a wall each way.
    [Fact]
    public void CoversEveryArm()
    {
        List<(byte[] Before, byte[] After)> capture = Capture(Plain);

        Assert.Contains(capture, c => Pushed(c, frameBelow4: true, snapped: false));
        Assert.Contains(capture, c => Pushed(c, frameBelow4: false, snapped: false));
        Assert.Contains(capture, c => Pushed(c, frameBelow4: true, snapped: true));
        Assert.Contains(capture, c => Pushed(c, frameBelow4: false, snapped: true));
        Assert.Contains(capture, c => c.Before[Columns + 17] != c.After[Columns + 17]);
    }

    // Rows $00 to $03, several pushes in one call, and entity 3 pushing a bubble in slot 10.
    [Fact]
    public void CoversTheLostIndex()
    {
        List<(byte[] Before, byte[] After)> capture = Capture(OffMap);

        foreach (byte row in (byte[])[0x00, 0x01, 0x02, 0x03])
        {
            Assert.Contains(capture, c => c.Before[Rows + 17] == row && c.Before[Xs + 17] != c.After[Xs + 17]);
        }

        Assert.Contains(capture, c => (byte)(c.Before[Xs + 17] - c.After[Xs + 17]) > 8);
        Assert.Contains(capture, ByEntityThree);
    }

    // Slot 10 pushed left while the player stood $14 or more from it: only entity 3 could have.
    private static bool ByEntityThree((byte[] Before, byte[] After) c)
    {
        bool rowThree = c.Before[Rows + 17] == 0x03;
        bool pushed = (byte)(c.Before[Xs + 10] - c.After[Xs + 10]) == 4;
        bool playerFar = (byte)(c.Before[PlayerX] - c.Before[Xs + 10]) >= 0x14;

        return rowThree && pushed && playerFar;
    }

    // A right push that snaps ends further left, and a left push that snaps ends further right.
    private static bool Pushed((byte[] Before, byte[] After) c, bool frameBelow4, bool snapped)
    {
        byte x = c.Before[Xs + 17];
        byte pushed = (byte)(frameBelow4 ? x + 4 : x - 4);

        bool facingRight = c.Before[Frames] < 4;
        bool moved = c.After[Xs + 17] != x;
        bool elsewhere = c.After[Xs + 17] != pushed;

        return facingRight == frameBelow4 && moved && elsewhere == snapped;
    }

    private static void Load(byte[] b, EntityTable entities, ObjectTable objects)
    {
        Assert.Equal(Length, b.Length);

        for (int player = 0; player < EntityTable.Capacity; player++)
        {
            entities.State[player] = b[States + player];
            entities.X[player] = b[PlayerX + player];
            entities.Y[player] = b[PlayerY + player];
            entities.Frame[player] = b[Frames + player];
        }

        b.AsSpan(Types, ObjectTable.Capacity).CopyTo(objects.Type);
        b.AsSpan(Columns, ObjectTable.Capacity).CopyTo(objects.Column);
        b.AsSpan(Rows, ObjectTable.Capacity).CopyTo(objects.Row);
        b.AsSpan(ObjectStates, ObjectTable.Capacity).CopyTo(objects.State);
        b.AsSpan(SubXs, ObjectTable.Capacity).CopyTo(objects.SubX);
        b.AsSpan(SubYs, ObjectTable.Capacity).CopyTo(objects.SubY);
        b.AsSpan(Xs, ObjectTable.Capacity).CopyTo(objects.X);
        b.AsSpan(Ys, ObjectTable.Capacity).CopyTo(objects.Y);
    }

    private static byte[] Save(EntityTable entities, ObjectTable objects)
    {
        byte[] b = new byte[Length];

        for (int player = 0; player < EntityTable.Capacity; player++)
        {
            b[States + player] = entities.State[player];
            b[PlayerX + player] = entities.X[player];
            b[PlayerY + player] = entities.Y[player];
            b[Frames + player] = entities.Frame[player];
        }

        objects.Type.CopyTo(b.AsSpan(Types));
        objects.Column.CopyTo(b.AsSpan(Columns));
        objects.Row.CopyTo(b.AsSpan(Rows));
        objects.State.CopyTo(b.AsSpan(ObjectStates));
        objects.SubX.CopyTo(b.AsSpan(SubXs));
        objects.SubY.CopyTo(b.AsSpan(SubYs));
        objects.X.CopyTo(b.AsSpan(Xs));
        objects.Y.CopyTo(b.AsSpan(Ys));

        return b;
    }

    private static List<(byte[] Before, byte[] After)> Capture(string file)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Bubbles", "Captures", file);

        return [.. File.ReadAllLines(path).Select(line =>
        {
            string[] parts = line.Split(' ');
            return (Convert.FromHexString(parts[0]), Convert.FromHexString(parts[1]));
        })];
    }

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}
