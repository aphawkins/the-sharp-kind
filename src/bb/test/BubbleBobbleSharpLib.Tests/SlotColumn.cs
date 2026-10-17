// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Tests;

// One field of every slot, indexed by slot: a view over the slots, so `objects.Slot(slot).X` reads and writes the slot's X.
// Goes when callers work on a whole slot (see `ObjectTable.Slot`).
internal readonly ref struct SlotColumn<TSlot>(TSlot[] slots, SlotField<TSlot> field, int start, int length)
{
    private readonly TSlot[] _slots = slots;
    private readonly SlotField<TSlot> _field = field;
    private readonly int _start = start;

    public SlotColumn(TSlot[] slots, SlotField<TSlot> field)
        : this(slots, field, 0, slots.Length)
    {
    }

    public int Length { get; } = length;

    public ref byte this[int slot] => ref _field(_slots[_start + slot]);

    public void Fill(byte value)
    {
        for (int slot = 0; slot < Length; slot++)
        {
            this[slot] = value;
        }
    }

    public void Clear() => Fill(0);

    public void Load(ReadOnlySpan<byte> values)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Length, Length);

        for (int slot = 0; slot < values.Length; slot++)
        {
            this[slot] = values[slot];
        }
    }

    public void CopyTo(Span<byte> destination)
    {
        for (int slot = 0; slot < Length; slot++)
        {
            destination[slot] = this[slot];
        }
    }

    public byte[] ToArray()
    {
        byte[] values = new byte[Length];
        CopyTo(values);
        return values;
    }

    // What `column[start..]` and `column[start..end]` reach.
    public SlotColumn<TSlot> Slice(int start, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, Length);

        return new(_slots, _field, _start + start, length);
    }

    public Enumerator GetEnumerator() => new(this);

    internal ref struct Enumerator(SlotColumn<TSlot> column)
    {
        private readonly SlotColumn<TSlot> _column = column;
        private int _slot = -1;

        public readonly byte Current => _column[_slot];

        public bool MoveNext() => ++_slot < _column.Length;
    }
}
