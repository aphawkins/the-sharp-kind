// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// The level's items appearing, being taken and going, against the real thing.
//
// Captures/items-pass-candy.txt and items-pass-crystal.txt were read out of VICE running rebb64, on
// 2026-09-24, on level 1 with one player. Each is an intervention three times over: $B4 to $B9 were
// zeroed on every pass so the player lived, the special item was written as candy 3 in one and the
// crystal, 9, in the other, and player 1's X and Y were written onto each item the pass it appeared.
// Exec checkpoints bracket $0A13, `jsr $1578`, and $0A54, `jsr $2CB7`, and only passes that changed
// something are kept.
//
// Each line is the call, then the bytes before and after: SUBFLG, $52 to $5E, $B2 and $B3, $BA and
// $BB, $C2 and $C3, $61 to $66, $A77B to $A782, $0400 to $0405, and $8728 and $8729.
//
// **The IRQ's once-a-second tick can land inside the window**, and on those lines both timers are one
// lower after than the call alone leaves them. The test applies $06C6's tick where that is so.
[Trait("Level", "Integration")]
public sealed class LevelItemsPassGoldenTests
{
    private const int Number = 0;
    private const int Types = 1;
    private const int X = 8;
    private const int Y = 10;
    private const int Timers = 12;
    private const int States = 14;
    private const int PlayerX = 16;
    private const int PlayerY = 18;
    private const int Rings = 20;
    private const int Blow = 26;
    private const int Score = 34;
    private const int Flash = 40;

    public static TheoryData<string, int> Lines()
    {
        TheoryData<string, int> lines = [];

        foreach (string name in new[] { "candy", "crystal" })
        {
            for (int i = 0; i < Capture(name).Count; i++)
            {
                lines.Add(name, i);
            }
        }

        return lines;
    }

    [Theory]
    [MemberData(nameof(Lines))]
    public void StepsExactlyAsTheGameDoes(string name, int line)
    {
        (string call, byte[] before, byte[] after) = Capture(name)[line];

        EntityTable entities = new();
        PlayerTable players = new();
        Scores scores = new();
        LevelItems items = new(entities, scores, new(players, entities), new(new FakeRandomSource()));
        Load(before, entities, players, scores, items);

        if (call == "0A13")
        {
            items.Update();
        }
        else
        {
            items.Collect(before[Number]);
        }

        if (after[Timers] == (byte)(items.Timer[0] - 1) && after[Timers + 1] == (byte)(items.Timer[1] - 1))
        {
            items.Tick();
        }

        Assert.Equal(Convert.ToHexString(after), Convert.ToHexString(Save(before, entities, players, scores, items)));
    }

    // Both items appear, are taken and go, and both effects ran.
    [Fact]
    public void CoversEveryStep()
    {
        List<(string Call, byte[] Before, byte[] After)> all = [.. Capture("candy"), .. Capture("crystal")];

        for (int item = 0; item < LevelItems.Count; item++)
        {
            Assert.Contains(all, c => c.Before[Types + item] >= 0x80 && c.After[Types + item] < 0x80);
            Assert.Contains(all, c => c.Before[Types + item] < 0x80 && c.After[Types + item] == 0xFF);
            Assert.Contains(all, c => c.Before[Timers + item] != 0xFF && c.After[Timers + item] == 0xFF);
        }

        Assert.Contains(all, c => c.Before[Blow] == 0x08 && c.After[Blow] == 0x03);
        Assert.Contains(all, c => c.Before[Rings] == 0x00 && c.After[Rings] == 0xFF);
    }

    private static void Load(byte[] b, EntityTable entities, PlayerTable players, Scores scores, LevelItems items)
    {
        for (int i = 0; i < LevelItems.Count; i++)
        {
            items.Type[i] = b[Types + i];
            items.X[i] = b[X + i];
            items.Y[i] = b[Y + i];
            items.Timer[i] = b[Timers + i];

            entities.State[i] = b[States + i];
            entities.X[i] = b[PlayerX + i];
            entities.Y[i] = b[PlayerY + i];
            entities.FlashTimer[i] = b[Flash + i];

            players.DriftRing[i] = b[Rings + i];
            players.WalkRing[i] = b[Rings + 2 + i];
            players.BlowRing[i] = b[Rings + 4 + i];
            players.BlowReload[i] = b[Blow + i];
            players.BlowState[i] = b[Blow + 2 + i];
            players.BlowVariant[i] = b[Blow + 4 + i];
            players.BlowType[i] = b[Blow + 6 + i];
        }

        for (int i = 0; i < 6; i++)
        {
            scores.Add(i, b[Score + i]);
        }
    }

    // The capture's own layout, with every byte this does not own copied from before.
    private static byte[] Save(byte[] before, EntityTable entities, PlayerTable players, Scores scores, LevelItems items)
    {
        byte[] b = [.. before];

        for (int i = 0; i < LevelItems.Count; i++)
        {
            b[Types + i] = items.Type[i];
            b[X + i] = items.X[i];
            b[Y + i] = items.Y[i];
            b[Timers + i] = items.Timer[i];
            b[Flash + i] = entities.FlashTimer[i];
            b[Rings + i] = players.DriftRing[i];
            b[Rings + 2 + i] = players.WalkRing[i];
            b[Rings + 4 + i] = players.BlowRing[i];
            b[Blow + i] = players.BlowReload[i];
            b[Blow + 2 + i] = players.BlowState[i];
            b[Blow + 4 + i] = players.BlowVariant[i];
            b[Blow + 6 + i] = players.BlowType[i];
        }

        scores.Bytes.CopyTo(b.AsSpan(Score));
        return b;
    }

    private static List<(string Call, byte[] Before, byte[] After)> Capture(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Items", "Captures", $"items-pass-{name}.txt");

        return [.. File.ReadAllLines(path).Where(l => l.Length > 0).Select(Parse)];
    }

    private static (string Call, byte[] Before, byte[] After) Parse(string line)
    {
        string[] parts = line.Split(' ');
        return (parts[0], Convert.FromHexString(parts[1]), Convert.FromHexString(parts[2]));
    }
}
