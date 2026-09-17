// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Trading;
using EliteSharp.Goods.Classic;

namespace EliteSharpLib.Tests.Trader;

/// <summary>
/// The classic seventeen, pinned where the game used to know them by number or
/// by a second list kept beside them. These are characterization tests: they
/// say what the enum and the three hardcoded lists said, so that the set having
/// moved to a plugin has something to be checked against.
/// </summary>
public class ClassicGoodsTests
{
    private static IReadOnlyList<Good> Goods => new ClassicGoodsSet().Goods;

    [Fact]
    public void TheGoodsAreTheClassicSeventeenInOrder()
    {
        string[] expected =
        [
            "Food", "Textiles", "Radioactives", "Slaves", "LiquorWines", "Luxuries",
            "Narcotics", "Computers", "Machinery", "Alloys", "Firearms", "Furs",
            "Minerals", "Gold", "Platinum", "GemStones", "AlienItems",
        ];

        Assert.Equal(expected, Goods.Select(good => good.Id));
    }

    // Pins the canister loot to the classic first eight goods, Food through Computers.
    [Fact]
    public void ACanisterHoldsTheFirstEightGoods()
    {
        string[] expected =
        [
            "Food", "Textiles", "Radioactives", "Slaves",
            "LiquorWines", "Luxuries", "Narcotics", "Computers",
        ];

        Assert.Equal(expected, Goods.Where(good => good.IsDroppedByShips).Select(good => good.Id));
    }

    [Fact]
    public void OnlyAlienItemsAreNeverSoldByAStation()
        => Assert.Equal(
            ["AlienItems"],
            Goods.Where(good => !good.IsSoldByStations).Select(good => good.Id));

    [Fact]
    public void SlavesAndNarcoticsWeighDoubleAndFirearmsSingle()
    {
        Dictionary<string, int> contraband = Goods
            .Where(good => good.ContrabandWeight > 0)
            .ToDictionary(good => good.Id, good => good.ContrabandWeight, StringComparer.Ordinal);

        Assert.Equal(
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["Slaves"] = 2,
                ["Narcotics"] = 2,
                ["Firearms"] = 1,
            },
            contraband);
    }

    // Gold and platinum are kilograms and gem-stones are grams, so a hold full of tonnes still has room for them.
    [Fact]
    public void OnlyTheMetalsAndTheGemStonesStayOutOfTheHold()
        => Assert.Equal(
            ["Gold", "Platinum", "GemStones"],
            Goods.Where(good => !good.FillsHold).Select(good => good.Id));

    // Pins Commander Max's hold: everything a station sells that is not contraband.
    [Fact]
    public void CommanderMaxCarriesEverythingLegalAndBuyable()
    {
        string[] expected =
        [
            "Food", "Textiles", "Radioactives", "LiquorWines", "Luxuries", "Computers",
            "Machinery", "Alloys", "Furs", "Minerals", "Gold", "Platinum", "GemStones",
        ];

        IEnumerable<string> carried = CommanderFactory.Max(Goods).Cargo
            .Where(item => item.Value > 0)
            .Select(item => item.Key);

        Assert.Equal(expected, carried);
    }

    [Fact]
    public void CommanderJamesonCarriesNothing()
        => Assert.DoesNotContain(CommanderFactory.Jameson(Goods).Cargo, item => item.Value != 0);

    // Spot check of the classic starting shelf, via each good's OpeningStationStock.
    [Fact]
    public void TheStartingStationOpensWithTheClassicShelf()
    {
        IDictionary<string, int> stock = CommanderFactory.Jameson(Goods).StationStock;

        Assert.Equal(0x10, stock["Food"]);
        Assert.Equal(0x3A, stock["Minerals"]);
        Assert.Equal(0x00, stock["AlienItems"]);
    }
}
