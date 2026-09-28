// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// $045C, $04BB, $7F53, $16E4 and $290D, worked by hand off the reference.
public sealed class PlayerRespawnTests
{
    // $04BB-$0502: a life off, back to the start in state $10 with $4A passes to go.
    [Theory]
    [InlineData(0, 0x2C, 0x00, 0x01)]
    [InlineData(1, 0xEC, 0x04, 0x02)]
    public void ADeadPlayerLosesALifeAndGoesBackToTheStart(int player, byte x, byte frame, byte bit)
    {
        Rig rig = new();
        rig.Dead(player);
        rig.Items.Undying = 0x03;

        rig.Respawn.Update(0);

        Assert.Equal(2, rig.Players.Lives[player]);
        Assert.Equal(PlayerRespawn.RespawningState, rig.Entities.State[player]);
        Assert.Equal(x, rig.Entities.X[player]);
        Assert.Equal(0xDD, rig.Entities.Y[player]);
        Assert.Equal(frame, rig.Entities.Frame[player]);
        Assert.Equal(0x4A, rig.Entities.TurnInterval[player]);
        Assert.Equal(0xFF, rig.Entities.RiseCounter[player]);
        Assert.Equal(0xFF, rig.Entities.FallCounter[player]);
        Assert.Equal(0xFF, rig.Entities.GroundState[player]);
        Assert.Equal((byte)(0x03 & ~bit), rig.Items.Undying);
    }

    // $7F53: the blow goes back to the plain bubble, and the rings, flash and death counter are cleared.
    [Fact]
    public void RespawningResetsTheBlowAndRings()
    {
        Rig rig = new();
        rig.Dead(0);
        rig.Players.BlowState[0] = 0x00;
        rig.Players.BlowReload[0] = 0x00;
        rig.Players.BlowVariant[0] = 0x00;
        rig.Players.BlowType[0] = 0x00;
        rig.Players.DriftRing[0] = 1;
        rig.Players.WalkRing[0] = 1;
        rig.Players.BlowRing[0] = 1;
        rig.Entities.FlashTimer[0] = 0x40;
        rig.Entities.LeapFlag[0] = 0x12;

        rig.Respawn.Update(0);

        Assert.Equal(0x88, rig.Players.BlowState[0]);
        Assert.Equal(0x08, rig.Players.BlowReload[0]);
        Assert.Equal(0x04, rig.Players.BlowVariant[0]);
        Assert.Equal(0x04, rig.Players.BlowType[0]);
        Assert.Equal(0, rig.Players.DriftRing[0]);
        Assert.Equal(0, rig.Players.WalkRing[0]);
        Assert.Equal(0, rig.Players.BlowRing[0]);
        Assert.Equal(0, rig.Entities.FlashTimer[0]);
        Assert.Equal(0, rig.Entities.LeapFlag[0]);
    }

    // $0460: only state $0F is dealt with.
    [Theory]
    [InlineData(PlayerFrame.PlayingState)]
    [InlineData(PlayerFrame.DyingState)]
    public void APlayerNotDeadIsLeftAlone(byte state)
    {
        Rig rig = new();
        rig.Players.Lives[0] = 3;
        rig.Entities.State[0] = state;

        rig.Respawn.Update(0);

        Assert.Equal(3, rig.Players.Lives[0]);
        Assert.Equal(state, rig.Entities.State[0]);
    }

    // $04E0, $04F0: the last life goes negative, and the player is out: the level reached is kept, and
    // the player is at (0, 0) in state 0 with no respawn to wait for.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void TheLastLifeIsTheGameOver(int player)
    {
        Rig rig = new();
        rig.Dead(player);
        rig.Players.Lives[player] = 0;
        rig.Entities.X[player] = 0x80;
        rig.Entities.Y[player] = 0x70;
        rig.Entities.TurnInterval[player] = 0x12;

        rig.Respawn.Update(0x2F);

        Assert.Equal(0xFF, rig.Players.Lives[player]);
        Assert.Equal(0, rig.Entities.State[player]);
        Assert.Equal(0, rig.Entities.X[player]);
        Assert.Equal(0, rig.Entities.Y[player]);
        Assert.Equal(0x2F, rig.Players.Round[player]);
        Assert.Equal(0x12, rig.Entities.TurnInterval[player]);
    }

    // $04F0: the other player's Round, lives and place are not touched.
    [Fact]
    public void AGameOverLeavesTheOtherPlayerAlone()
    {
        Rig rig = new();
        rig.Dead(0);
        rig.Players.Lives[0] = 0;
        rig.Entities.State[1] = PlayerFrame.PlayingState;
        rig.Entities.X[1] = 0x40;
        rig.Players.Lives[1] = 2;

        rig.Respawn.Update(0x10);

        Assert.Equal(PlayerFrame.PlayingState, rig.Entities.State[1]);
        Assert.Equal(0x40, rig.Entities.X[1]);
        Assert.Equal(2, rig.Players.Lives[1]);
        Assert.Equal(0, rig.Players.Round[1]);
    }

    // $7F77: the bonus level's own setup is step 7's.
    [Fact]
    public void ADeathOnTheBonusLevelIsNotTranslated()
    {
        Rig rig = new();
        rig.Dead(0);

        Assert.Throws<NotSupportedException>(() => rig.Respawn.Update(0x63));
    }

    // $0514-$0527, $16E4: with two or more enemies left, the free ones lose their anger; a caught one keeps it.
    [Theory]
    [InlineData(2, 0x00)]
    [InlineData(1, 0x40)]
    public void RespawningCalmsTheEnemiesWhileTwoAreLeft(byte enemies, byte expected)
    {
        Rig rig = new();
        rig.Dead(0);
        rig.Entities.EnemyCount = enemies;
        rig.Entities.State[2] = 0x01;
        rig.Entities.FlashTimer[2] = 0x40;
        rig.Entities.State[7] = 0x0A;
        rig.Entities.FlashTimer[7] = 0x40;
        rig.Entities.State[3] = 0x0B;
        rig.Entities.FlashTimer[3] = 0x40;
        rig.Entities.FlashTimer[4] = 0x40;

        rig.Respawn.Update(0);

        Assert.Equal(expected, rig.Entities.FlashTimer[2]);
        Assert.Equal(expected, rig.Entities.FlashTimer[7]);
        Assert.Equal(0x40, rig.Entities.FlashTimer[3]);
        Assert.Equal(0x40, rig.Entities.FlashTimer[4]);
    }

    // $290D: $4A passes flashing (colour EOR 5), then state 1; the colour ends where it started.
    [Fact]
    public void RespawningFlashesForSeventyFourPassesThenPlays()
    {
        EntityTable entities = new();
        entities.State[0] = PlayerRespawn.RespawningState;
        entities.TurnInterval[0] = 0x4A;
        entities.Colour[0] = 0x05;
        entities.X[0] = 0x2C;
        entities.Y[0] = 0x80;
        entities.RiseCounter[0] = 0xFF;
        entities.GroundState[0] = 0xFF;
        entities.BubbleTimer[0] = 0xFF;
        PlayerFrame frame = Frame(entities);
        SolidMap map = Floor();

        for (int pass = 1; pass < 0x4A; pass++)
        {
            frame.Step(0, Input.Idle, map);
            Assert.Equal(PlayerRespawn.RespawningState, entities.State[0]);
            Assert.Equal((pass & 1) == 0 ? 0x05 : 0x00, entities.Colour[0]);
        }

        frame.Step(0, Input.Idle, map);

        Assert.Equal(PlayerFrame.PlayingState, entities.State[0]);
        Assert.Equal(0x05, entities.Colour[0]);
    }

    private static SolidMap Floor()
        => SolidMap.Build(
            new() { Number = 1, Bitmap = [.. Enumerable.Repeat(new string('#', 32), 23)] },
            []);

    private static PlayerFrame Frame(EntityTable entities)
    {
        BubbleBlow blow = TestBlow.Of(entities);
        PlayerSteer steer = new(entities, blow);
        PlayerDescent descent = new(entities);

        return new(
            entities,
            new PlayerMovement(entities, blow, TestBlow.NoRings()),
            new PlayerJump(entities, new PlayerDrift(entities, steer, TestBlow.NoRings()), new PlayerLanding(entities, descent)),
            new PlayerFall(entities, steer),
            blow);
    }

    private sealed class Rig
    {
        internal Rig()
        {
            Items = new(Entities, new Scores(), new(Players, Entities), new(new FakeRandomSource()));
            Respawn = new(Entities, Players, Items, Timer);
        }

        internal EntityTable Entities { get; } = new();

        internal PlayerTable Players { get; } = new();

        internal LevelItems Items { get; }

        internal LevelTimer Timer { get; } = new();

        internal PlayerRespawn Respawn { get; }

        internal void Dead(int player)
        {
            Entities.State[player] = PlayerFrame.DeadState;
            Players.Lives[player] = 3;
        }
    }
}
