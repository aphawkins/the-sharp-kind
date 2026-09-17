// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Trading;
using EliteSharp.Goods.Classic;
using EliteSharpLib.Trader;
using Microsoft.Extensions.Logging.Abstractions;

namespace EliteSharpLib.Tests.Trader;

// A goods set must give every id as a usable key, and every wrecked-ship drop must exist in it.
public class GoodsRegistryTests
{
    private static readonly Good[] s_shipDrops =
    [
        MakeGood("Alloys"),
        MakeGood("Slaves"),
        MakeGood("Minerals"),
        MakeGood("AlienItems"),
    ];

    [Fact]
    public void AcceptsTheClassicSet()
    {
        GoodsRegistry registry = new(new ClassicGoodsSet(), NullLogger<GoodsRegistry>.Instance);

        Assert.Equal("Classic", registry.Name);
        Assert.Equal(17, registry.Goods.Count);
    }

    [Fact]
    public void RejectsASetMissingAGoodThatShipsDrop()
    {
        FakeSet set = new("Partial", [MakeGood("Alloys"), MakeGood("Slaves"), MakeGood("Minerals")]);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => new GoodsRegistry(set, NullLogger<GoodsRegistry>.Instance));

        Assert.Contains("AlienItems", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsASetThatNamesAGoodTwice()
    {
        FakeSet set = new("Doubled", [.. s_shipDrops, MakeGood("Slaves")]);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => new GoodsRegistry(set, NullLogger<GoodsRegistry>.Instance));

        Assert.Contains("Slaves", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsASetWithABlankId()
    {
        FakeSet set = new("Blank", [.. s_shipDrops, MakeGood("  ")]);

        Assert.Throws<InvalidOperationException>(() => new GoodsRegistry(set, NullLogger<GoodsRegistry>.Instance));
    }

    private static Good MakeGood(string id)
        => new(id, id, 1.0f, 0, 1, 1, "t", true, 0, true, 0, false);

    private sealed record FakeSet(string Name, IReadOnlyList<Good> Goods) : IGoodsSet;
}
