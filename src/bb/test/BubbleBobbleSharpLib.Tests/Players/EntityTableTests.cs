// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// Storage rather than behaviour, so what there is to prove is that it is the storage the 6502 has.
//
// The point worth a test of its own is the width. State, X and Y were held as two bytes each for a
// while, on the reading that they were the players' own. They are not: $105B runs `ldy #$05` and
// walks $B4, $BC and $C4, which are these three arrays two bytes along, so the six enemies live in
// slots 2 to 7 of the same eight. A table two wide cannot hold a game.
public sealed class EntityTableTests
{
    [Fact]
    public void HoldsOneByteEachForEightSlots()
    {
        EntityTable entities = new();

        Assert.Equal(8, EntityTable.Capacity);
        Assert.Equal(EntityTable.Capacity, entities.State.Length);
        Assert.Equal(EntityTable.Capacity, entities.X.Length);
        Assert.Equal(EntityTable.Capacity, entities.Y.Length);
        Assert.Equal(EntityTable.Capacity, entities.Frame.Length);
        Assert.Equal(EntityTable.Capacity, entities.GroundState.Length);
    }

    // A fresh table is the cleared page the game starts from, so no field may arrive with a value.
    [Fact]
    public void StartsEveryByteAtZero()
    {
        EntityTable entities = new();

        for (int slot = 0; slot < EntityTable.Capacity; slot++)
        {
            Assert.Equal(0, entities.State[slot]);
            Assert.Equal(0, entities.X[slot]);
            Assert.Equal(0, entities.Y[slot]);
        }
    }

    // The spans are windows onto the arrays rather than copies, and no two slots share a byte. Both
    // halves matter: a copy would lose every write, and a shared byte would drag the first enemy
    // about whenever player one moved.
    [Fact]
    public void KeepsEachSlotsWritesApartAndKeepsThem()
    {
        EntityTable entities = new();

        entities.X[0] = 0x24;
        entities.X[1] = 0xF4;
        entities.X[7] = 0x80;

        Assert.Equal(0x24, entities.X[0]);
        Assert.Equal(0xF4, entities.X[1]);
        Assert.Equal(0x80, entities.X[7]);
        Assert.Equal(0, entities.Y[0]);
    }
}
