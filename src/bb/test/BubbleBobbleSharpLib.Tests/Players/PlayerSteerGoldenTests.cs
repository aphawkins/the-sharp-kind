// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// A steered jump, against the game: W and D held together on level 1.
//
// This is the case that caught the drift being only half of airborne movement. X does not climb by a
// pixel a frame, it climbs by two and then one, over and over, because $2483 moves the player every
// frame and $25F1 moves them again on every second one. Anything that translated only the first
// would pass a test of the arc and fail this.
//
// The capture samples at the arc's own store, which is before the frame's horizontal work, so the X
// it reads is what the *previous* frame left behind. The expectations below are paired back up
// accordingly: the state after frame N is the Y sampled at N and the X sampled at N plus one.
[Trait("Level", "Integration")]
public sealed class PlayerSteerGoldenTests
{
    // Both directions held. Only the left and right bits are read by anything under test.
    private const byte PushUpAndRight = 0xFF & unchecked((byte)~0x08) & unchecked((byte)~0x01);

    // The start of the second frame of the jump, read straight out of the capture.
    private const byte StartX = 0x38;
    private const byte StartY = 0xD9;
    private const byte StartRise = 0x0E;
    private const byte StartFrame = 0x02;
    private const byte StartTimer = 0x00;

    private static readonly byte[] s_x =
    [
        0x39, 0x3B, 0x3C, 0x3E, 0x3F, 0x41, 0x42, 0x44, 0x45,
        0x47, 0x48, 0x4A, 0x4B, 0x4D, 0x4E, 0x50, 0x51,
    ];

    private static readonly byte[] s_y =
    [
        0xD5, 0xD1, 0xCD, 0xC9, 0xC6, 0xC3, 0xC0, 0xBD, 0xBA,
        0xB8, 0xB6, 0xB4, 0xB3, 0xB2, 0xB2, 0xB3, 0xB4,
    ];

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    [Fact]
    public void SteersThroughAJumpExactlyAsTheGameDoes()
    {
        PlayerTable players = new();
        EntityTable entities = new();
        PlayerSteer steer = new(players, entities);
        PlayerDrift drift = new(players, entities, steer);
        PlayerDescent descent = new(players, entities);
        PlayerLanding landing = new(players, entities, descent);
        PlayerJump jump = new(players, entities, drift, landing);
        SolidMap map = SolidMap.Build(s_levels.Level(1));

        players.X[0] = StartX;
        players.Y[0] = StartY;
        entities.Frame[0] = StartFrame;
        entities.AnimationTimer[0] = StartTimer;
        entities.RiseCounter[0] = StartRise;
        entities.FallCounter[0] = 0xFF;
        entities.BubbleTimer[0] = 0xFF;

        // $222B latched right as the jump began, and nothing in the air clears it.
        entities.RightFlag[0] = 0xFF;
        entities.LeftFlag[0] = 0x00;

        byte[] x = new byte[s_x.Length];
        byte[] y = new byte[s_y.Length];

        for (int tick = 0; tick < s_x.Length; tick++)
        {
            jump.Step(0, PushUpAndRight, PlayerCell.Of(players.X[0], players.Y[0]), map);
            x[tick] = players.X[0];
            y[tick] = players.Y[0];
        }

        Assert.Equal(s_y, y);
        Assert.Equal(s_x, x);
    }

    // The tell-tale on its own: two pixels then one, repeating. A drift-only translation gives a
    // steady one and passes nothing here.
    [Fact]
    public void MovesTwoPixelsThenOneWhileSteering()
    {
        int[] deltas = new int[s_x.Length - 1];

        for (int i = 1; i < s_x.Length; i++)
        {
            deltas[i - 1] = s_x[i] - s_x[i - 1];
        }

        for (int i = 0; i < deltas.Length; i++)
        {
            Assert.Equal(i % 2 == 0 ? 2 : 1, deltas[i]);
        }
    }
}
