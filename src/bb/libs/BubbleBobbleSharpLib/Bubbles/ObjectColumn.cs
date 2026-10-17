// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Bubbles;

// One field of every slot, indexed by slot: a view over the slots, so `objects.X[slot]` reads and writes the slot's X.
// Goes when callers work on a whole slot (see `ObjectTable.Slot`).
internal readonly ref struct ObjectColumn(ObjectSlot[] slots, ObjectField field)
{
    private readonly ObjectSlot[] _slots = slots;
    private readonly ObjectField _field = field;

    public int Length => _slots.Length;

    public ref byte this[int slot] => ref _field(_slots[slot]);

    public void Fill(byte value)
    {
        for (int slot = 0; slot < _slots.Length; slot++)
        {
            this[slot] = value;
        }
    }

    public void Clear() => Fill(0);

    public void Load(ReadOnlySpan<byte> values)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Length, _slots.Length);

        for (int slot = 0; slot < values.Length; slot++)
        {
            this[slot] = values[slot];
        }
    }

    public void CopyTo(Span<byte> destination)
    {
        for (int slot = 0; slot < _slots.Length; slot++)
        {
            destination[slot] = this[slot];
        }
    }

    public byte[] ToArray()
    {
        byte[] values = new byte[_slots.Length];
        CopyTo(values);
        return values;
    }

    public Enumerator GetEnumerator() => new(this);

    public ref struct Enumerator(ObjectColumn column)
    {
        private readonly ObjectColumn _column = column;
        private int _slot = -1;

        public readonly byte Current => _column[_slot];

        public bool MoveNext() => ++_slot < _column.Length;
    }
}
