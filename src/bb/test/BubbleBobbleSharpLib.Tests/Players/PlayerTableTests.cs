// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// The table is storage rather than behaviour, so what there is to prove is that it is the storage the
// 6502 has: two bytes per field, starting at zero, and each player's byte its own.
public sealed class PlayerTableTests
{
    [Fact]
    public void HoldsOneByteEachForTwoPlayers()
    {
        PlayerTable players = new();

        Assert.Equal(2, PlayerTable.Capacity);
        Assert.Equal(PlayerTable.Capacity, players.State.Length);
        Assert.Equal(PlayerTable.Capacity, players.X.Length);
        Assert.Equal(PlayerTable.Capacity, players.Y.Length);
        Assert.Equal(PlayerTable.Capacity, players.BubbleTimer.Length);
        Assert.Equal(PlayerTable.Capacity, players.Lives.Length);
    }

    // A fresh table is the cleared page the game starts from, so no field may arrive carrying a value.
    [Fact]
    public void StartsEveryByteAtZero()
    {
        PlayerTable players = new();

        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            Assert.Equal(0, players.State[player]);
            Assert.Equal(0, players.X[player]);
            Assert.Equal(0, players.Y[player]);
            Assert.Equal(0, players.BubbleTimer[player]);
            Assert.Equal(0, players.Lives[player]);
        }
    }

    // The spans are windows onto the arrays, not copies of them, and the two players do not share a
    // byte. Both halves matter: a copy would lose every write, and a shared byte would move player 2
    // whenever player 1 moved.
    [Fact]
    public void KeepsEachPlayersWritesApartAndKeepsThem()
    {
        PlayerTable players = new();

        players.X[0] = 0x24;
        players.X[1] = 0xF4;
        players.Lives[0] = 3;

        Assert.Equal(0x24, players.X[0]);
        Assert.Equal(0xF4, players.X[1]);
        Assert.Equal(3, players.Lives[0]);
        Assert.Equal(0, players.Lives[1]);
        Assert.Equal(0, players.Y[0]);
    }
}
