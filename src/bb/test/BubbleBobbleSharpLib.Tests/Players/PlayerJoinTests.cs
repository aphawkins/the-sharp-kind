// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// $052A, worked by hand off the reference.
public sealed class PlayerJoinTests
{
    private static readonly byte[] s_fireOnly = [unchecked((byte)~Input.Fire), unchecked((byte)~Input.Fire)];

    // $052C-$0535: a player who is out of the game and holds fire joins it with four lives, dead, and no score.
    [Theory]
    [InlineData(0, 0x02)]
    [InlineData(1, 0x05)]
    public void AnAbsentPlayerHoldingFireJoinsTheGame(int player, int last)
    {
        Rig rig = new();
        rig.Scores.Add(last, 0x42);
        rig.Scores.Add(last - 2, 0x17);
        rig.Players.Lives[player] = 7;

        rig.Join.Update(Fire(player));

        Assert.Equal(PlayerFrame.DeadState, rig.Entities.State[player]);
        Assert.Equal(4, rig.Players.Lives[player]);
        Assert.All(rig.Scores.Bytes[(last - 2)..(last + 1)].ToArray(), b => Assert.Equal(0, b));
        Assert.Equal(0, rig.Players.ExtraLifeStep[player]);
        Assert.Equal(last - 1, rig.Players.ExtraLifeByte[player]);
        Assert.Equal(6, rig.Players.Credits);
    }

    // $0540-$0548: a join clears its own player's score and no one else's.
    [Fact]
    public void JoiningLeavesTheOtherPlayersScoreAlone()
    {
        Rig rig = new();
        rig.Scores.Add(0, 0x12);
        rig.Scores.Add(3, 0x34);

        rig.Join.Update(Fire(1));

        Assert.Equal(0x12, rig.Scores.Bytes[0]);
        Assert.Equal(0x00, rig.Scores.Bytes[3]);
    }

    // $0530-$0535: no fire, no join.
    [Fact]
    public void WithoutFireNoOneJoins()
    {
        Rig rig = new();

        rig.Join.Update([Input.Idle, Input.Idle]);

        Assert.Equal(0, rig.Entities.State[0]);
        Assert.Equal(0, rig.Entities.State[1]);
        Assert.Equal(7, rig.Players.Credits);
    }

    // $052E: a player already in the game, dying or waiting to respawn is not absent.
    [Theory]
    [InlineData(PlayerFrame.PlayingState)]
    [InlineData(PlayerFrame.DyingState)]
    [InlineData(PlayerFrame.DeadState)]
    [InlineData(PlayerRespawn.RespawningState)]
    public void APlayerInTheGameCannotJoinAgain(byte state)
    {
        Rig rig = new();
        rig.Entities.State[0] = state;
        rig.Players.Lives[0] = 2;

        rig.Join.Update(Fire(0));

        Assert.Equal(state, rig.Entities.State[0]);
        Assert.Equal(2, rig.Players.Lives[0]);
        Assert.Equal(7, rig.Players.Credits);
    }

    // $052C, $05A6: player 2 first, then player 1, each taking a credit.
    [Fact]
    public void BothPlayersJoinInOneCall()
    {
        Rig rig = new();

        rig.Join.Update(s_fireOnly);

        Assert.Equal(PlayerFrame.DeadState, rig.Entities.State[0]);
        Assert.Equal(PlayerFrame.DeadState, rig.Entities.State[1]);
        Assert.Equal(5, rig.Players.Credits);
    }

    // $055B-$0561, $049D: the join that takes the credits below zero shuts the door on the next call, not this one.
    [Fact]
    public void TheLastCreditShutsTheJoinAfterTheCallItIsSpentIn()
    {
        Rig rig = new();
        rig.Players.Credits = 1;

        rig.Join.Update(s_fireOnly);

        Assert.Equal(PlayerFrame.DeadState, rig.Entities.State[0]);
        Assert.Equal(PlayerFrame.DeadState, rig.Entities.State[1]);
        Assert.Equal(0xFF, rig.Players.Credits);

        rig.Entities.State[1] = 0;
        rig.Join.Update(Fire(1));

        Assert.Equal(0, rig.Entities.State[1]);
        Assert.Equal(0xFF, rig.Players.Credits);
    }

    // $0977-$0982: a two-player game starts with seven, a one-player game with eight.
    [Theory]
    [InlineData(1, 8)]
    [InlineData(2, 7)]
    public void ANewGameStartsWithItsCredits(int playing, byte credits)
    {
        GameLoop loop = new(new(new FakeRandomSource()), playing);

        Assert.Equal(credits, loop.PlayerTable.Credits);
    }

    private static byte[] Fire(int player)
    {
        byte[] ports = [Input.Idle, Input.Idle];
        ports[player] = unchecked((byte)~Input.Fire);
        return ports;
    }

    private sealed class Rig
    {
        internal Rig()
        {
            Players.Credits = 7;
            Join = new(Entities, Players, Scores);
        }

        internal EntityTable Entities { get; } = new();

        internal PlayerTable Players { get; } = new();

        internal Scores Scores { get; } = new();

        internal PlayerJoin Join { get; }
    }
}
