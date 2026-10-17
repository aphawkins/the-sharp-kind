// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Bubbles;

internal sealed class ObjectTable
{
    internal const int Capacity = 18;

    internal const byte FreeType = 0xFF;

    private readonly ObjectSlot[] _slots = new ObjectSlot[Capacity];

    internal ObjectTable()
    {
        for (int slot = 0; slot < Capacity; slot++)
        {
            _slots[slot] = new();
        }

        Reset();
    }

    internal ObjectColumn Type => new(_slots, static slot => ref slot.Type);

    internal ObjectColumn X => new(_slots, static slot => ref slot.X);

    internal ObjectColumn Y => new(_slots, static slot => ref slot.Y);

    internal ObjectColumn SubX => new(_slots, static slot => ref slot.SubX);

    internal ObjectColumn SubY => new(_slots, static slot => ref slot.SubY);

    internal ObjectColumn Column => new(_slots, static slot => ref slot.Column);

    internal ObjectColumn Row => new(_slots, static slot => ref slot.Row);

    internal ObjectColumn Flags => new(_slots, static slot => ref slot.Flags);

    internal ObjectColumn Behaviour => new(_slots, static slot => ref slot.Behaviour);

    internal ObjectColumn State => new(_slots, static slot => ref slot.State);

    internal ObjectColumn Variant => new(_slots, static slot => ref slot.Variant);

    internal ObjectColumn EnemyType => new(_slots, static slot => ref slot.EnemyType);

    internal ObjectColumn Direction => new(_slots, static slot => ref slot.Direction);

    // One slot as a whole, for code that works on a single object.
    internal ObjectSlot Slot(int slot) => _slots[slot];

    internal void Reset()
    {
        for (int slot = 0; slot < Capacity; slot++)
        {
            _slots[slot].Type = FreeType;
            _slots[slot].Column = 0;
            _slots[slot].Row = 0;
        }
    }

    internal int FindFree()
    {
        for (int slot = Capacity - 1; slot >= 0; slot--)
        {
            if ((_slots[slot].Type & 0x80) != 0)
            {
                return slot;
            }
        }

        return -1;
    }
}
