// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Trading;
using EliteSharpLib.Ships;

namespace EliteSharpLib.Trader;

internal sealed class Trade
{
    private readonly GameState _gameState;

    private readonly PlayerShip _ship;

    private readonly Dictionary<string, StockItem> _byId;

    internal Trade(GameState gameState, PlayerShip ship, GoodsRegistry goods)
    {
        ArgumentNullException.ThrowIfNull(goods);

        _gameState = gameState;
        _ship = ship;

        Goods = goods.Goods;
        StockMarket = [.. goods.Goods.Select(good => new StockItem(good))];
        _byId = StockMarket.ToDictionary(stock => stock.Good.Id, StringComparer.Ordinal);
        DroppedByShips = [.. StockMarket.Where(stock => stock.Good.IsDroppedByShips)];
    }

    /// <summary>
    /// Gets the goods the game is trading, for the parts that build a starting
    /// commander or check a save against the set.
    /// </summary>
    internal IReadOnlyList<Good> Goods { get; }

    internal float Credits { get; set; }

    internal int MarketRandomiser { get; set; }

    /// <summary>
    /// Gets every good, in the order the market screen lists them. An ordered
    /// list rather than a dictionary because that order is shown to the
    /// commander and saved to file, and a dictionary only happens to keep it.
    /// </summary>
    internal IReadOnlyList<StockItem> StockMarket { get; }

    /// <summary>
    /// Gets the goods a cargo canister can hold, in the same order. Held rather
    /// than filtered each time because a canister is opened mid-combat and the
    /// answer never changes.
    /// </summary>
    internal IReadOnlyList<StockItem> DroppedByShips { get; }

    /// <summary>
    /// The good that goes by this name.
    /// </summary>
    /// <param name="id">The good's <see cref="Good.Id"/>.</param>
    /// <returns>That good's current standing.</returns>
    internal StockItem this[string id] => _byId[id];

    internal void BuyStock(StockItem stock)
    {
        ArgumentNullException.ThrowIfNull(stock);

        if (stock.CurrentQuantity == 0 || Credits < stock.CurrentPrice)
        {
            return;
        }

        if (stock.Good.FillsHold && TotalCargoTonnage() == _ship.CargoCapacity)
        {
            return;
        }

        stock.CurrentCargo++;
        stock.CurrentQuantity--;
        Credits -= stock.CurrentPrice;
    }

    internal void ClearCurrentCargo()
    {
        foreach (StockItem stock in StockMarket)
        {
            stock.CurrentCargo = 0;
        }
    }

    /// <summary>
    /// Generate the Elite stock market.
    /// The prices and quantities are affected by the planet's economy.
    /// There is also a slight amount of randomness added in.
    /// The random value is changed each time we hyperspace.
    /// </summary>
    internal void GenerateStockMarket()
    {
        foreach (StockItem stock in StockMarket)
        {
            Good good = stock.Good;

            float price = good.BasePrice;
            price += (MarketRandomiser & good.Mask) / 10f;
            price += _gameState.CurrentPlanetData.Economy * good.EconomyAdjust / 10f;

            int quant = good.BaseQuantity;
            quant += MarketRandomiser & good.Mask;
            quant -= _gameState.CurrentPlanetData.Economy * good.EconomyAdjust;

            quant = Math.Clamp(quant, 0, 63);

            stock.CurrentPrice = price * 4;
            stock.CurrentQuantity = good.IsSoldByStations ? quant : 0;
        }
    }

    internal int IsCarryingContraband()
        => StockMarket.Sum(stock => stock.CurrentCargo * stock.Good.ContrabandWeight);

    internal void SellStock(StockItem stock)
    {
        ArgumentNullException.ThrowIfNull(stock);

        if (stock.CurrentCargo == 0)
        {
            return;
        }

        stock.CurrentCargo--;
        stock.CurrentQuantity++;
        Credits += stock.CurrentPrice;
    }

    internal void SetStockQuantities()
    {
        foreach (StockItem stock in StockMarket)
        {
            stock.CurrentQuantity = stock.Good.IsSoldByStations ? stock.StationStock : 0;
        }
    }

    internal int TotalCargoTonnage()
    {
        int cargo = 0;

        foreach (StockItem stock in StockMarket)
        {
            if (stock.CurrentCargo > 0 && stock.Good.FillsHold)
            {
                cargo += stock.CurrentCargo;
            }
        }

        return cargo;
    }
}
