// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// The walk against the real thing, which is what Phase 4's verify step asks for.
//
// The numbers below were not worked out by hand. They were read out of VICE running the PRG that
// `make verify` proves byte-identical to the original: a one-player game on level 1, a store
// checkpoint on $BA, and D held. Every time the game stored the player's X, the checkpoint took
// $BA and $8520 and wrote them down. This replays the same start through the port and expects the
// same two sequences.
[Trait("Level", "Integration")]
public sealed class PlayerWalkGoldenTests
{
    // Where level 1 puts player 1, read from $BA and $C2 with the game running.
    private const byte StartX = 0x2C;
    private const byte StartY = 0xDD;

    // A port byte with right pushed: the bit goes clear.
    private const byte PushRight = 0xFF & unchecked((byte)~0x08);

    // $BA after each of the first twenty-four stores.
    private static readonly byte[] s_x =
    [
        0x2E, 0x30, 0x32, 0x34, 0x36, 0x38, 0x3A, 0x3C,
        0x3E, 0x40, 0x42, 0x44, 0x46, 0x48, 0x4A, 0x4C,
        0x4E, 0x50, 0x52, 0x54, 0x56, 0x58, 0x5A, 0x5C,
    ];

    // $8520 alongside each of them. The walk cycle is the low bit, and it turns over every fourth
    // store - which is why the first run of the same frame is three long and the rest are four.
    private static readonly byte[] s_frame =
    [
        0, 0, 0, 1, 1, 1, 1, 0,
        0, 0, 0, 1, 1, 1, 1, 0,
        0, 0, 0, 1, 1, 1, 1, 0,
    ];

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    [Fact]
    public void WalksRightAcrossLevelOneExactlyAsTheGameDoes()
    {
        PlayerTable players = new();
        EntityTable entities = new();
        PlayerMovement movement = new(players, entities);
        SolidMap map = SolidMap.Build(s_levels.Level(1));

        players.X[0] = StartX;
        players.Y[0] = StartY;
        entities.Frame[0] = 0x00;
        entities.BubbleTimer[0] = 0xFF;

        byte[] x = new byte[s_x.Length];
        byte[] frame = new byte[s_frame.Length];

        for (int tick = 0; tick < s_x.Length; tick++)
        {
            movement.Step(0, PushRight, map);
            x[tick] = players.X[0];
            frame[tick] = entities.Frame[0];
        }

        Assert.Equal(s_x, x);
        Assert.Equal(s_frame, frame);
    }

    // The other half of the verify step: a player may not walk through the level. Every one of the
    // hundred levels, both directions, from every open cell on a row the wall check applies to.
    [Fact]
    public void NeverWalksThroughASolidCellOnAnyLevel()
    {
        for (int number = 1; number <= LevelStore.Count; number++)
        {
            SolidMap map = SolidMap.Build(s_levels.Level(number));

            for (byte y = 0x2D; y < 0xF4; y += 8)
            {
                for (byte start = 0x24; start < 0xF4; start += 8)
                {
                    Walk(map, number, start, y, PushRight, 0x00, 0x2B);
                    Walk(map, number, start, y, 0xFF & unchecked((byte)~0x04), 0x04, 0x28);
                }
            }
        }
    }

    // One step from one place, checking the cell the mover looked at was not solid.
    private static void Walk(SolidMap map, int number, byte startX, byte y, byte port, byte frame, int probe)
    {
        PlayerTable players = new();
        EntityTable entities = new();
        PlayerMovement movement = new(players, entities);

        players.X[0] = startX;
        players.Y[0] = y;
        entities.Frame[0] = frame;
        entities.BubbleTimer[0] = 0xFF;

        PlayerCell cell = PlayerCell.Of(startX, y);
        bool blocked = cell.Solid(map, probe);

        movement.Step(0, port, map);

        bool moved = players.X[0] != startX;

        Assert.False(
            blocked && moved,
            $"Level {number}: walked out of X {startX:X2}, Y {y:X2} into a solid cell.");
    }
}
