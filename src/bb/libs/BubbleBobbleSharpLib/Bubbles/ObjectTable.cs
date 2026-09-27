// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Bubbles;

internal sealed class ObjectTable
{
    internal const int Capacity = 18;

    internal const byte FreeType = 0xFF;

    private readonly byte[] _type = new byte[Capacity];
    private readonly byte[] _x = new byte[Capacity];
    private readonly byte[] _y = new byte[Capacity];
    private readonly byte[] _subX = new byte[Capacity];
    private readonly byte[] _subY = new byte[Capacity];
    private readonly byte[] _column = new byte[Capacity];
    private readonly byte[] _row = new byte[Capacity];
    private readonly byte[] _flags = new byte[Capacity];
    private readonly byte[] _behaviour = new byte[Capacity];
    private readonly byte[] _state = new byte[Capacity];
    private readonly byte[] _variant = new byte[Capacity];
    private readonly byte[] _enemyType = new byte[Capacity];
    private readonly byte[] _direction = new byte[Capacity];

    internal ObjectTable() => Reset();

    internal Span<byte> Type => _type;

    internal Span<byte> X => _x;

    internal Span<byte> Y => _y;

    internal Span<byte> SubX => _subX;

    internal Span<byte> SubY => _subY;

    internal Span<byte> Column => _column;

    internal Span<byte> Row => _row;

    internal Span<byte> Flags => _flags;

    internal Span<byte> Behaviour => _behaviour;

    internal Span<byte> State => _state;

    internal Span<byte> Variant => _variant;

    internal Span<byte> EnemyType => _enemyType;

    internal Span<byte> Direction => _direction;

    internal void Reset()
    {
        _type.AsSpan().Fill(FreeType);
        _column.AsSpan().Clear();
        _row.AsSpan().Clear();
    }

    internal int FindFree()
    {
        for (int slot = Capacity - 1; slot >= 0; slot--)
        {
            if ((_type[slot] & 0x80) != 0)
            {
                return slot;
            }
        }

        return -1;
    }
}
