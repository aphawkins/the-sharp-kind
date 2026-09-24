// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// One bubble on level 1, from the blow to the pop, against the VICE capture in the plan's Phase 6.
//
// The capture counted $1CBD anchors, which tick three times for every two passes of the game loop,
// so its frame numbers are converted here at two thirds from the blow at frame 32. Every pass is
// $0CF2 and then $13BE, the order $0A51 calls them in. The bubble is placed as $232A leaves it -
// X $34, Y $DD, column 4, row $19, counter $88 - and nothing else is in the table, so the draw at
// $0D86 never matters: with nobody near, every path through it ends at $0E00.
//
// This is the first test in the phase with data from outside the reading behind it.
public sealed class BubbleTravelTests
{
    private const int Slot = 17;

    // The whole of the capture, in passes. Frames 44, 151, 157, 195 to 215 and 219.
    private const int ShotEnds = 8;
    private const int RiseEnds = 80;
    private const int DriftEnds = 84;
    private const int Pops = 0x7D;

    private const byte Popping = 0x3A;

    private static readonly SolidMap s_level1 = LevelOne();

    // Frames 32 to 44: eight moves of eight pixels, X $34 to $74, and no change in Y.
    [Fact]
    public void ShootsEightColumnsRight()
    {
        byte[][] path = Replay(ShotEnds);

        for (int pass = 1; pass <= ShotEnds; pass++)
        {
            Assert.Equal(path[pass - 1][0] + 8, path[pass][0]);
            Assert.Equal(0xDD, path[pass][1]);
        }

        Assert.Equal(0x74, path[ShotEnds][0]);
    }

    // Frames 45 to 151: two pixels up a pass, Y $DD to $4D, through map rows whose current is 0.
    [Fact]
    public void RisesTwoPixelsAPass()
    {
        byte[][] path = Replay(RiseEnds);

        for (int pass = ShotEnds + 1; pass <= RiseEnds; pass++)
        {
            Assert.Equal(0x74, path[pass][0]);
            Assert.Equal(path[pass - 1][1] - 2, path[pass][1]);
        }

        Assert.Equal(0x4D, path[RiseEnds][1]);
    }

    // Frames 153 to 157: right from X $74 to $7C, which is $0E00 reading the current zone_type 1
    // lays across the top of the level.
    [Fact]
    public void DriftsRightUnderTheCeiling()
    {
        byte[][] path = Replay(DriftEnds);

        for (int pass = RiseEnds + 1; pass <= DriftEnds; pass++)
        {
            Assert.Equal(path[pass - 1][0] + 2, path[pass][0]);
            Assert.Equal(0x4D, path[pass][1]);
        }

        Assert.Equal(0x7C, path[DriftEnds][0]);
    }

    // Frames 157 to 195. It rises to $47 and then goes up and down between $45 and $47. Row 4 of the
    // map pushes up and row 3 pushes down, so this is what the code does. The capture's own
    // summary reads it as parked at $47, and nothing has settled whether the real game also
    // moves between the two values.
    [Fact]
    public void SettlesUnderTheCeiling()
    {
        byte[][] path = Replay(Pops - 1);

        for (int pass = DriftEnds + 3; pass < Pops; pass++)
        {
            Assert.Equal(0x7C, path[pass][0]);
            Assert.InRange(path[pass][1], 0x45, 0x47);
        }
    }

    // Frames 195 to 215, then 219: the wobble while the clock is below $11, and the pop the pass
    // it reaches zero - $7D passes after the blow, which is the $7D $232A starts it at.
    [Fact]
    public void WobblesAndPopsOnTheCapturesPass()
    {
        byte[][] path = Replay(Pops);

        Assert.Contains(path[(Pops - 0x10)..Pops], step => step[2] == 0x48);
        Assert.DoesNotContain(path[..(Pops - 0x11)], step => step[2] == 0x48);
        Assert.NotEqual(Popping, path[Pops - 1][2]);
        Assert.Equal(Popping, path[Pops][2]);
    }

    // X, Y and type after each pass, with the state before the first pass at index 0.
    private static byte[][] Replay(int passes)
    {
        ObjectTable objects = new();
        EntityTable entities = new();
        EnemyAiLoop loop = new(
            objects,
            entities,
            new EnemyDispatcher(objects, entities, new BubbleCollision(objects, entities)),
            new EntityMover(objects),
            new BbRandom(new FakeRandomSource()),
            new Baron(objects, entities));
        EntityTimers timers = new(objects, entities);

        objects.Type[Slot] = BubbleBlow.BubbleType;
        objects.X[Slot] = 0x34;
        objects.Y[Slot] = 0xDD;
        objects.Column[Slot] = 0x04;
        objects.Row[Slot] = 0x19;
        objects.Flags[Slot] = 0x7D;
        objects.Behaviour[Slot] = 0x7D;
        objects.State[Slot] = 0x88;
        objects.Variant[Slot] = 0x04;
        objects.EnemyType[Slot] = 0x04;

        byte[][] path = new byte[passes + 1][];
        path[0] = Snapshot(objects);

        for (int pass = 1; pass <= passes; pass++)
        {
            loop.Update(s_level1);
            timers.Update(0);
            path[pass] = Snapshot(objects);
        }

        return path;
    }

    private static byte[] Snapshot(ObjectTable objects) => [objects.X[Slot], objects.Y[Slot], objects.Type[Slot]];

    private static SolidMap LevelOne()
    {
        string folder = Path.Combine(
            AppContext.BaseDirectory,
            "Renditions",
            "BubbleBobbleSharp.Renditions.EightBit",
            "Assets",
            "Levels");

        return SolidMap.Build(
            LevelStore.Read(Path.Combine(folder, "levels.json")).Level(1),
            ZoneStore.Read(Path.Combine(folder, "zones.json")).Zones(1));
    }
}
