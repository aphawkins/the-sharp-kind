// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// The giant bonus a special item makes a cleared level drop ($1751-$175D, and state $15's `$2ACB`-`$2B30`): a cake, a
// melon or a diamond, four sprites in a block that falls from the top of the screen to `$C8` and waits for a player, who
// scores the `$69` (ARG) the item set. It is drawn from slots 2 to 5, the three past the first in state 0.
internal sealed class SuperBonus
{
    internal const byte State = 0x15;

    private const int FirstSlot = 2;
    private const int Pieces = 4;

    private const byte Top = 0x15;
    private const byte Rest = 0xC8;
    private const byte Fall = 0x04;

    private const byte LeftX = 0x80;
    private const byte RightX = 0x98;

    private const byte PieceBase = 0xF4;

    private const byte ReachX = 0x30;
    private const byte ReachY = 0x24;

    private readonly EntityTable _entities;
    private readonly Scores _scores;
    private readonly LevelClear _clear;

    internal SuperBonus(EntityTable entities, Scores scores, LevelClear clear)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(clear);

        _entities = entities;
        _scores = scores;
        _clear = clear;
    }

    // Whether the block is on the screen.
    internal bool Showing => _entities.Slot(FirstSlot).State == State;

    // `$1E2E`, then `$1757`-`$175D`: the six enemy slots are emptied, and the first made the block, with its lower pair
    // ($C6, $C7: slots 4 and 5) a sprite's height below the upper.
    internal void Begin()
    {
        for (int slot = FirstSlot; slot < EntityTable.Capacity; slot++)
        {
            Empty(slot);
        }

        _entities.Slot(FirstSlot).State = State;
        _entities.Slot(FirstSlot + 2).Y = Top;
        _entities.Slot(FirstSlot + 3).Y = Top;
    }

    // `$2ACB`.
    internal void Step()
    {
        if (_entities.Slot(FirstSlot).Y != Rest)
        {
            _entities.Slot(FirstSlot).X = LeftX;
            _entities.Slot(FirstSlot + 2).X = LeftX;
            _entities.Slot(FirstSlot + 1).X = RightX;
            _entities.Slot(FirstSlot + 3).X = RightX;

            for (int piece = Pieces - 1; piece >= 0; piece--)
            {
                int slot = FirstSlot + piece;

                _entities.Slot(slot).SpriteBase = PieceBase;
                _entities.Slot(slot).Frame = (byte)piece;
                _entities.Slot(slot).Colour = _clear.BonusColour;
                _entities.Slot(slot).Y += Fall;
            }
        }

        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (Touches(player))
            {
                _scores.Add(Scores.Last(player) - 1, _clear.BonusArg);

                for (int piece = Pieces - 1; piece >= 0; piece--)
                {
                    Empty(FirstSlot + piece);
                }
            }
        }
    }

    private bool Touches(int player)
    {
        int across = _entities.Slot(player).X - _entities.Slot(FirstSlot).X;
        int down = _entities.Slot(player).Y - _entities.Slot(FirstSlot).Y;

        return across is >= 0 and < ReachX && down is >= 0 and < ReachY;
    }

    private void Empty(int slot)
    {
        _entities.Slot(slot).State = 0;
        _entities.Slot(slot).X = 0;
        _entities.Slot(slot).Y = 0;
    }
}
