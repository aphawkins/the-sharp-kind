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
        Assert.Equal(PlayerRespawn.RespawningState, rig.Entities.Slot(player).State);
        Assert.Equal(x, rig.Entities.Slot(player).X);
        Assert.Equal(0xDD, rig.Entities.Slot(player).Y);
        Assert.Equal(frame, rig.Entities.Slot(player).Frame);
        Assert.Equal(0x4A, rig.Entities.Slot(player).TurnInterval);
        Assert.Equal(0xFF, rig.Entities.Slot(player).RiseCounter);
        Assert.Equal(0xFF, rig.Entities.Slot(player).FallCounter);
        Assert.Equal(0xFF, rig.Entities.Slot(player).GroundState);
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
        rig.Entities.Slot(0).FlashTimer = 0x40;
        rig.Entities.Slot(0).LeapFlag = 0x12;

        rig.Respawn.Update(0);

        Assert.Equal(0x88, rig.Players.BlowState[0]);
        Assert.Equal(0x08, rig.Players.BlowReload[0]);
        Assert.Equal(0x04, rig.Players.BlowVariant[0]);
        Assert.Equal(0x04, rig.Players.BlowType[0]);
        Assert.Equal(0, rig.Players.DriftRing[0]);
        Assert.Equal(0, rig.Players.WalkRing[0]);
        Assert.Equal(0, rig.Players.BlowRing[0]);
        Assert.Equal(0, rig.Entities.Slot(0).FlashTimer);
        Assert.Equal(0, rig.Entities.Slot(0).LeapFlag);
    }

    // $0460: only state $0F is dealt with.
    [Theory]
    [InlineData(PlayerFrame.PlayingState)]
    [InlineData(PlayerFrame.DyingState)]
    public void APlayerNotDeadIsLeftAlone(byte state)
    {
        Rig rig = new();
        rig.Players.Lives[0] = 3;
        rig.Entities.Slot(0).State = state;

        rig.Respawn.Update(0);

        Assert.Equal(3, rig.Players.Lives[0]);
        Assert.Equal(state, rig.Entities.Slot(0).State);
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
        rig.Entities.Slot(player).X = 0x80;
        rig.Entities.Slot(player).Y = 0x70;
        rig.Entities.Slot(player).TurnInterval = 0x12;

        rig.Respawn.Update(0x2F);

        Assert.Equal(0xFF, rig.Players.Lives[player]);
        Assert.Equal(0, rig.Entities.Slot(player).State);
        Assert.Equal(0, rig.Entities.Slot(player).X);
        Assert.Equal(0, rig.Entities.Slot(player).Y);
        Assert.Equal(0x2F, rig.Players.Round[player]);
        Assert.Equal(0x12, rig.Entities.Slot(player).TurnInterval);
    }

    // $04F0: the other player's Round, lives and place are not touched.
    [Fact]
    public void AGameOverLeavesTheOtherPlayerAlone()
    {
        Rig rig = new();
        rig.Dead(0);
        rig.Players.Lives[0] = 0;
        rig.Entities.Slot(1).State = PlayerFrame.PlayingState;
        rig.Entities.Slot(1).X = 0x40;
        rig.Players.Lives[1] = 2;

        rig.Respawn.Update(0x10);

        Assert.Equal(PlayerFrame.PlayingState, rig.Entities.Slot(1).State);
        Assert.Equal(0x40, rig.Entities.Slot(1).X);
        Assert.Equal(2, rig.Players.Lives[1]);
        Assert.Equal(0, rig.Players.Round[1]);
    }

    // $7F7D: the last level's giant patches the game as a player's reset finds it, which is the giant's own to do.
    [Fact]
    public void ADeathOnTheLastLevelIsAResetLikeAnother()
    {
        Rig rig = new();
        rig.Dead(0);

        rig.Respawn.Update(0x63);

        Assert.Equal(0, rig.Entities.Slot(0).FlashTimer);
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
        rig.Entities.Slot(2).State = 0x01;
        rig.Entities.Slot(2).FlashTimer = 0x40;
        rig.Entities.Slot(7).State = 0x0A;
        rig.Entities.Slot(7).FlashTimer = 0x40;
        rig.Entities.Slot(3).State = 0x0B;
        rig.Entities.Slot(3).FlashTimer = 0x40;
        rig.Entities.Slot(4).FlashTimer = 0x40;

        rig.Respawn.Update(0);

        Assert.Equal(expected, rig.Entities.Slot(2).FlashTimer);
        Assert.Equal(expected, rig.Entities.Slot(7).FlashTimer);
        Assert.Equal(0x40, rig.Entities.Slot(3).FlashTimer);
        Assert.Equal(0x40, rig.Entities.Slot(4).FlashTimer);
    }

    // $290D: $4A passes flashing (colour EOR 5), then state 1; the colour ends where it started.
    [Fact]
    public void RespawningFlashesForSeventyFourPassesThenPlays()
    {
        EntityTable entities = new();
        entities.Slot(0).State = PlayerRespawn.RespawningState;
        entities.Slot(0).TurnInterval = 0x4A;
        entities.Slot(0).Colour = 0x05;
        entities.Slot(0).X = 0x2C;
        entities.Slot(0).Y = 0x80;
        entities.Slot(0).RiseCounter = 0xFF;
        entities.Slot(0).GroundState = 0xFF;
        entities.Slot(0).BubbleTimer = 0xFF;
        PlayerFrame frame = Frame(entities);
        SolidMap map = Floor();

        for (int pass = 1; pass < 0x4A; pass++)
        {
            frame.Step(0, Input.Idle, map);
            Assert.Equal(PlayerRespawn.RespawningState, entities.Slot(0).State);
            Assert.Equal((pass & 1) == 0 ? 0x05 : 0x00, entities.Slot(0).Colour);
        }

        frame.Step(0, Input.Idle, map);

        Assert.Equal(PlayerFrame.PlayingState, entities.Slot(0).State);
        Assert.Equal(0x05, entities.Slot(0).Colour);
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
            blow,
            TestBlow.NoWater(entities));
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
            Entities.Slot(player).State = PlayerFrame.DeadState;
            Players.Lives[player] = 3;
        }
    }
}
