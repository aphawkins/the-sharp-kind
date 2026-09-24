// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// The table is storage rather than behaviour, so what there is to prove is that it is the storage the
// 6502 has: two bytes per field, starting at zero, and each player's byte its own.
//
// The state, X and Y this file used to cover are in EntityTableTests now. They are eight-slot arrays
// shared with the six enemies, not two-slot arrays of the players' own - see that file.
public sealed class PlayerTableTests
{
    [Fact]
    public void HoldsOneByteEachForTwoPlayers()
    {
        PlayerTable players = new();

        Assert.Equal(2, PlayerTable.Capacity);
        Assert.Equal(PlayerTable.Capacity, players.Lives.Length);
        Assert.Equal(PlayerTable.Capacity, players.Reload.Length);
        Assert.Equal(PlayerTable.Capacity, players.BlowFacing.Length);
    }

    // A fresh table is the cleared page the game starts from, so no field may arrive carrying a value.
    [Fact]
    public void StartsEveryByteAtZero()
    {
        PlayerTable players = new();

        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            Assert.Equal(0, players.Lives[player]);
            Assert.Equal(0, players.Reload[player]);
            Assert.Equal(0, players.BlowFacing[player]);
        }
    }

    // The spans are windows onto the arrays, not copies of them, and the two players do not share a
    // byte. Both halves matter: a copy would lose every write, and a shared byte would give player 2
    // whatever player 1 was given.
    [Fact]
    public void KeepsEachPlayersWritesApartAndKeepsThem()
    {
        PlayerTable players = new();

        players.Lives[0] = 3;
        players.Reload[1] = 0x08;

        Assert.Equal(3, players.Lives[0]);
        Assert.Equal(0, players.Lives[1]);
        Assert.Equal(0x08, players.Reload[1]);
        Assert.Equal(0, players.Reload[0]);
    }
}
