// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Goods.Classic;
using EliteSharpLib.Ships;
using EliteSharpLib.Trader;
using Microsoft.Extensions.Logging.Abstractions;

namespace EliteSharpLib.Tests;

// Handed straight to a registry rather than found on disk, for tests about what the goods do rather than the loading path.
internal static class TestGoods
{
    internal static GoodsRegistry Registry()
        => new(new ClassicGoodsSet(), NullLogger<GoodsRegistry>.Instance);

    internal static Trade Trade(GameState gameState, PlayerShip ship)
        => new(gameState, ship, Registry());
}
