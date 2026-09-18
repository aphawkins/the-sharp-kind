// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// One jump, against the game itself.
//
// Read out of VICE the same way the walk was: a one-player game on level 1, a store checkpoint on
// $C2, and W held. The capture gives the program counter and both counters beside every Y, so the
// arc separates into its three parts without any guessing - fifteen stores at $23FE rising, four at
// $243C falling, and one at $2480 landing.
[Trait("Level", "Integration")]
public sealed class PlayerJumpGoldenTests
{
    private const byte StartX = 0x2C;
    private const byte StartY = 0xDD;

    // $222B sets the rise counter to $0F as a jump begins. The capture caught it doing so: the byte
    // reads $FF while the player stands still and $0E after the first store of the rise.
    private const byte JumpRise = 0x0F;

    // $05C5 parks both counters at $FF between jumps.
    private const byte Idle = 0xFF;

    // The capture was taken with W held and neither left nor right, so the port's direction bits are
    // all high. PlayerSteer reads only those two, so this is the port it saw.
    private const byte NoDirection = 0xFF;

    // $C2 after each store, in order. The first fifteen are the rise, the next three the fall, and
    // the last is the landing - which is two stores in one frame, $243A putting the player at $B6
    // and $247E snapping them back to $B5.
    private static readonly byte[] s_y =
    [
        0xD9, 0xD5, 0xD1, 0xCD, 0xC9, 0xC6, 0xC3, 0xC0,
        0xBD, 0xBA, 0xB8, 0xB6, 0xB4, 0xB3, 0xB2,
        0xB2, 0xB3, 0xB4, 0xB5,
    ];

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    [Fact]
    public void JumpsAndLandsOnLevelOneExactlyAsTheGameDoes()
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
        entities.Frame[0] = 0x01;
        entities.RiseCounter[0] = JumpRise;
        entities.FallCounter[0] = Idle;

        byte[] y = new byte[s_y.Length];

        for (int tick = 0; tick < s_y.Length; tick++)
        {
            // $E9B8 runs at the top of the frame, so the cell a step reasons about is the one the
            // player was in before it moved.
            PlayerCell cell = PlayerCell.Of(players.X[0], players.Y[0]);

            jump.Step(0, NoDirection, cell, map);
            y[tick] = players.Y[0];
        }

        Assert.Equal(s_y, y);
    }

    // The counters the arc is indexed by, which the capture also read out. The rise counts down from
    // $0F and the fall up from zero, and both are what make the arc slow at the top.
    [Fact]
    public void RunsTheCountersTheGameRuns()
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
        entities.RiseCounter[0] = JumpRise;
        entities.FallCounter[0] = Idle;

        for (int tick = 0; tick < 15; tick++)
        {
            jump.Step(0, NoDirection, PlayerCell.Of(players.X[0], players.Y[0]), map);
            Assert.Equal(0x0E - tick, entities.RiseCounter[0]);
        }

        for (int tick = 0; tick < 3; tick++)
        {
            jump.Step(0, NoDirection, PlayerCell.Of(players.X[0], players.Y[0]), map);
            Assert.Equal(tick + 1, entities.FallCounter[0]);
        }

        // The fourth step of the fall is the one that lands, and $2519 puts both counters back to
        // the idle the caller tests for. Before $2519 was translated this asserted a fall counter of
        // four, which is what the capture reads at $2480 - one instruction before the reset.
        jump.Step(0, NoDirection, PlayerCell.Of(players.X[0], players.Y[0]), map);

        Assert.Equal(0xFF, entities.FallCounter[0]);
        Assert.Equal(0xFF, entities.RiseCounter[0]);
    }

    // $2519, from the one frame of the capture that lands. Going in, the player is mid-fall at X
    // $53, Y $B4 with the fall counter on $03; coming out, the arc has ended. Y goes to $B5, which
    // is $B6 snapped to the row grid, and X to $54, which is $53 squared up to an even column - the
    // pixel that had no explanation until this routine was read.
    [Fact]
    public void EndsTheArcExactlyAsTheGameDoes()
    {
        PlayerTable players = new();
        EntityTable entities = new();
        PlayerSteer steer = new(players, entities);
        PlayerDrift drift = new(players, entities, steer);
        PlayerDescent descent = new(players, entities);
        PlayerLanding landing = new(players, entities, descent);
        PlayerJump jump = new(players, entities, drift, landing);
        SolidMap map = SolidMap.Build(s_levels.Level(1));

        players.X[0] = 0x53;
        players.Y[0] = 0xB4;
        entities.Frame[0] = 0x00;
        entities.RiseCounter[0] = 0x00;
        entities.FallCounter[0] = 0x03;
        entities.BubbleTimer[0] = Idle;
        entities.GroundState[0] = Idle;

        jump.Step(0, NoDirection, PlayerCell.Of(players.X[0], players.Y[0]), map);

        Assert.Equal(0xB5, players.Y[0]);
        Assert.Equal(0x54, players.X[0]);
        Assert.Equal(Idle, entities.RiseCounter[0]);
        Assert.Equal(Idle, entities.FallCounter[0]);

        // The capture read $87F0 still negative after the landing, so the branch that starts the
        // player falling again did not fire: there was a floor.
        Assert.Equal(Idle, entities.GroundState[0]);
    }

    // $2474. A landing puts the player on a row boundary, counted from $2D.
    [Fact]
    public void ALandingSnapsToTheRowGrid()
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
        entities.RiseCounter[0] = JumpRise;
        entities.FallCounter[0] = Idle;

        for (int tick = 0; tick < s_y.Length; tick++)
        {
            jump.Step(0, NoDirection, PlayerCell.Of(players.X[0], players.Y[0]), map);
        }

        Assert.Equal(0, (players.Y[0] - 0x2D) % 8);
    }
}
