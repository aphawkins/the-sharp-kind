// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Bubbles;

// One object: a bubble, an item, a shot or a caught enemy. Each field is the byte the 6502 keeps in the table named in
// the conversion notes. The members return refs so a slot's field reads and writes in place.
internal sealed class ObjectSlot
{
    private byte _type;
    private byte _x;
    private byte _y;
    private byte _subX;
    private byte _subY;
    private byte _column;
    private byte _row;
    private byte _flags;
    private byte _behaviour;
    private byte _state;
    private byte _variant;
    private byte _enemyType;
    private byte _direction;

    internal ref byte Type => ref _type;

    internal ref byte X => ref _x;

    internal ref byte Y => ref _y;

    internal ref byte SubX => ref _subX;

    internal ref byte SubY => ref _subY;

    internal ref byte Column => ref _column;

    internal ref byte Row => ref _row;

    internal ref byte Flags => ref _flags;

    internal ref byte Behaviour => ref _behaviour;

    internal ref byte State => ref _state;

    internal ref byte Variant => ref _variant;

    internal ref byte EnemyType => ref _enemyType;

    internal ref byte Direction => ref _direction;
}
