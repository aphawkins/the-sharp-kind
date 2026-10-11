// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// `$1AE8`-`$1B18`, which the Barons' call (`$1621`) and the giant bubble (`$1AB6`) both open with: whatever is in the players' two
// object slots is popped, and the game waits, an object update at a time, until both are free.
internal sealed class SlotPop
{
    private const byte PopType = 0x3A;
    private const byte FirstMovableType = 0x18;
    private const byte AfterMovableType = 0x24;

    private readonly ObjectTable _objects;
    private readonly BubblePop _pop;

    internal SlotPop(ObjectTable objects, BubblePop pop)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(pop);

        _objects = objects;
        _pop = pop;
    }

    // `$1AE8-$1B01`: whatever is in the two slots is kept, unless it is not one of the movable types, and is popped.
    internal void Pop()
    {
        for (int slot = PlayerTable.Capacity - 1; slot >= 0; slot--)
        {
            byte type = _objects.Type[slot];

            if ((sbyte)type < 0)
            {
                continue;
            }

            _objects.EnemyType[slot] = type is >= FirstMovableType and < AfterMovableType ? type : (byte)0;
            _objects.Type[slot] = PopType;
        }
    }

    // `$1B02`: one update of the objects while the game waits, which is a frame.
    internal void Update() => _pop.Update();

    // Whether both slots are free.
    internal bool Free()
    {
        for (int slot = PlayerTable.Capacity - 1; slot >= 0; slot--)
        {
            if ((sbyte)_objects.Type[slot] >= 0)
            {
                return false;
            }
        }

        return true;
    }
}
