// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Bubbles;

// The eighteen-slot table, and the one piece of behaviour it has: finding somewhere to put a
// bubble. Everything else about it is storage, and is proved where it is written.
public sealed class ObjectTableTests
{
    // $2321 counts from $11 down, and $217C from $11 down as well.
    [Fact]
    public void HoldsEighteenSlots()
    {
        ObjectTable objects = new();

        Assert.Equal(18, ObjectTable.Capacity);
        Assert.Equal(ObjectTable.Capacity, objects.Type.Length);
        Assert.Equal(ObjectTable.Capacity, objects.X.Length);
        Assert.Equal(ObjectTable.Capacity, objects.Direction.Length);
    }

    // $0620. Every slot free from the start, which is what makes the first bubble of a level
    // findable at all.
    [Fact]
    public void StartsWithEverySlotFree()
    {
        ObjectTable objects = new();

        foreach (byte type in objects.Type)
        {
            Assert.Equal(ObjectTable.FreeType, type);
        }
    }

    // $2321's direction of travel, which is downwards. It matters because it decides which slot a
    // bubble lands in, and the drawing order later reads the table in slot order.
    [Fact]
    public void FindsTheLastFreeSlotFirst()
    {
        ObjectTable objects = new();

        Assert.Equal(17, objects.FindFree());
    }

    // The next one down once the last is taken, and so on.
    [Fact]
    public void WorksDownwardsAsSlotsFill()
    {
        ObjectTable objects = new();
        objects.Slot(17).Type = BubbleBlow.BubbleType;
        objects.Slot(16).Type = BubbleBlow.BubbleType;

        Assert.Equal(15, objects.FindFree());
    }

    // A gap high in the table is found even with lower slots free, because the search never looks
    // for the lowest free slot - only for the first one counting down.
    [Fact]
    public void TakesTheHighestFreeSlotRatherThanTheLowest()
    {
        ObjectTable objects = new();
        objects.Type.Fill(BubbleBlow.BubbleType);
        objects.Slot(3).Type = ObjectTable.FreeType;
        objects.Slot(12).Type = ObjectTable.FreeType;

        Assert.Equal(12, objects.FindFree());
    }

    // $2329. A full table has nothing to give, and the caller does nothing rather than overwriting
    // something that is already there.
    [Fact]
    public void FindsNothingWhenEverySlotIsTaken()
    {
        ObjectTable objects = new();
        objects.Type.Fill(BubbleBlow.BubbleType);

        Assert.Equal(-1, objects.FindFree());
    }

    // Free is bit 7, not the exact value $FF. $217C treats a zero slot as empty and $2321 does not,
    // so a slot holding zero is neither free nor catchable - a state the reference does leave
    // behind, and one the search must not hand out.
    [Fact]
    public void ReadsFreeAsTheTopBitRatherThanAsAValue()
    {
        ObjectTable objects = new();
        objects.Type.Clear();
        objects.Slot(5).Type = 0x80;

        Assert.Equal(5, objects.FindFree());
    }

    // $0620 and $062B, as a level starts. Bubbles from the last level do not survive into this one,
    // and the two cell arrays go back to zero with them.
    [Fact]
    public void ResetFreesEverySlotAndClearsTheCells()
    {
        ObjectTable objects = new();
        objects.Type.Fill(BubbleBlow.BubbleType);
        objects.Slot(4).Column = 9;
        objects.Slot(4).Row = 7;

        objects.Reset();

        Assert.Equal(17, objects.FindFree());
        Assert.Equal(0, objects.Slot(4).Column);
        Assert.Equal(0, objects.Slot(4).Row);
    }
}
