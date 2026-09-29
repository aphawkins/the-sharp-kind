// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// An enemy in a bonus round ($34A0): state $16 (`$2785`) drops through the platforms, and state $17 (`$27CB`) waits on
// the one it landed on until a player is within $10 of it, when it goes as food. Neither animates.
internal sealed class EnemyTumble
{
    internal const byte TumbleState = 0x16;
    internal const byte WaitingState = 0x17;

    private const byte Colour = 0x0E;

    private const int InsideLeft = 0x29;
    private const int UnderLeft = 0x51;

    private const byte Reach = 0x10;
    private const byte TouchedFood = 0x0A;

    private const int FirstEnemy = 2;

    private readonly EntityTable _entities;
    private readonly FoodDrop _food;

    internal EnemyTumble(EntityTable entities, FoodDrop food)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(food);

        _entities = entities;
        _food = food;
    }

    // $2785-$27CA, then $27CB.
    internal void Step(int slot, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        _entities.Colour[slot] = Colour;

        if (!EnemyProbes.Blocked(cell, map, InsideLeft, InsideLeft + 1, InsideLeft + 2)
            && (cell.Solid(map, UnderLeft) || cell.Solid(map, UnderLeft + 1)))
        {
            _entities.State[slot] = WaitingState;
            Touch(slot);
            return;
        }

        // $27B2: down a step, from an even column and an odd row.
        _entities.X[slot] &= 0xFE;
        _entities.Y[slot] |= 0x01;
        _food.Drop(slot, cell, map);

        if (_entities.State[slot] == FoodDrop.LandedState)
        {
            _entities.State[slot] = WaitingState;
        }

        Touch(slot);
    }

    // $27CB-$2807: a player, of the two, that is in the game and near enough takes it: the state the enemy had comes back
    // (`$87A0`) for `Kill`, and its food is entry $0A.
    internal void Touch(int slot)
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (_entities.State[player] == 0
                || Distance.Absolute(_entities.X[slot], _entities.X[player]) >= Reach
                || Distance.Absolute(_entities.Y[slot], _entities.Y[player]) >= Reach)
            {
                continue;
            }

            _entities.State[slot] = _entities.RiseCounter[slot];
            _food.Kill(slot - FirstEnemy);
            _entities.AttackTimer[slot] = TouchedFood;
            return;
        }
    }
}
