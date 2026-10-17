// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// Which item art an enemy that has become food is drawn from: the food is `$A790` by `AttackTimer`, and its art is `$A892` by
// the food.
public sealed class FoodArtTests
{
    private const int Slot = 3;

    [Theory]
    [InlineData(1, 0x00)]
    [InlineData(2, 0x01)]
    [InlineData(9, 0x07)]
    public void IsTheArtOfTheFoodTheTimerNames(byte timer, int art)
    {
        GameLoop loop = new(new(new SharpKind.RandomSource(new Random(1))), 1);
        loop.Entities.Slot(Slot).State = FoodDrop.FoodState;
        loop.Entities.Slot(Slot).AttackTimer = timer;

        int[] arts = loop.FoodArt();

        Assert.Equal(art, arts[Slot]);
        Assert.All(Enumerable.Range(0, EntityTable.Capacity).Where(s => s != Slot), s => Assert.Equal(-1, arts[s]));
    }

    [Fact]
    public void IsNoneForAFoodPastTheEndOfTheArt()
    {
        GameLoop loop = new(new(new SharpKind.RandomSource(new Random(1))), 1);
        loop.Entities.Slot(Slot).State = FoodDrop.FoodState;
        loop.Entities.Slot(Slot).AttackTimer = 0;

        Assert.Equal(-1, loop.FoodArt()[Slot]);
    }
}
