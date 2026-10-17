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
