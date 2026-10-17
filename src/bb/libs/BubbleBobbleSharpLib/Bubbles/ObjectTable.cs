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

    internal SlotColumn<ObjectSlot> Type => new(_slots, static slot => ref slot.Type);

    internal SlotColumn<ObjectSlot> X => new(_slots, static slot => ref slot.X);

    internal SlotColumn<ObjectSlot> Y => new(_slots, static slot => ref slot.Y);

    internal SlotColumn<ObjectSlot> SubX => new(_slots, static slot => ref slot.SubX);

    internal SlotColumn<ObjectSlot> SubY => new(_slots, static slot => ref slot.SubY);

    internal SlotColumn<ObjectSlot> Column => new(_slots, static slot => ref slot.Column);

    internal SlotColumn<ObjectSlot> Row => new(_slots, static slot => ref slot.Row);

    internal SlotColumn<ObjectSlot> Flags => new(_slots, static slot => ref slot.Flags);

    internal SlotColumn<ObjectSlot> Behaviour => new(_slots, static slot => ref slot.Behaviour);

    internal SlotColumn<ObjectSlot> State => new(_slots, static slot => ref slot.State);

    internal SlotColumn<ObjectSlot> Variant => new(_slots, static slot => ref slot.Variant);

    internal SlotColumn<ObjectSlot> EnemyType => new(_slots, static slot => ref slot.EnemyType);

    internal SlotColumn<ObjectSlot> Direction => new(_slots, static slot => ref slot.Direction);

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
