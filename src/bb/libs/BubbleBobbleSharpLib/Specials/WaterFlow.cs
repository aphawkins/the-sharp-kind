// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Specials;

// The water bubble's flow: `$1B40` (`level-complete.s`, run by the IRQ every second frame while `$4D` is not zero), the
// start of it in the `$3E02` arm of `$3DB0`, and state `$0D`'s handler at `$284D`. Popping a bubble of type $06, when no
// water is running, starts a stream of eleven cells at the bubble's place. The head falls, walks along a platform (to a
// random side, turning at a wall, dropping at a ledge) and the eleven follow it, until the last has left the bottom.
//
//   * `$4D` (`Mask`) is 0 with no water, $FF while the head falls, and 4 (left) or 5 (right) while it walks. `$A74B`
//     and `$A756` hold the cells' columns and rows; the head is the last, and `$1D` in a row is below the level.
//   * Each pass the eleven shift one place towards the tail and the head moves. The tail leaving (its row `$1D`) ends it.
//   * The cell before the head ($A754/$A75F) takes in anything within $0C pixels: an enemy or player in state 1 to $0A
//     (a player only if it is not rising, `$87A0` negative) goes to state `$0D`, keeping the state it left in `$8840`.
//   * State `$0D` puts the thing at that cell's place each pass. When the cell reaches the bottom an enemy drowns (state
//     `$0C`, one fewer to kill), and a player is put back on its feet at column `$64` or `$B4`.
internal sealed class WaterFlow
{
    internal const byte CarriedState = 0x0D;

    private const int Cells = 11;
    private const int Head = Cells - 1;
    private const int BeforeHead = Head - 1;

    private const byte BelowLevel = 0x1D;
    private const byte FirstRow = 0x04;
    private const byte Falling = 0xFF;
    private const byte WalkingLeft = 0x04;

    private const byte XOrigin = 0x14;
    private const byte YOrigin = 0x05;
    private const byte Reach = 0x0C;
    private const byte CaughtBelow = 0x0B;
    private const byte ReleasingState = 0x0A;
    private const byte ParkedY = 0xED;

    private const byte DrownedState = 0x0C;
    private const byte DrownedTimer = 0x07;
    private const byte DrownedColour = 0x0E;

    private const int RowBias = 4;

    private const byte LeftEdge = 0x64;
    private const byte RightEdge = 0xB4;
    private const byte RightThreshold = 0x94;

    private const int FirstPlayerOnly = 2;

    private readonly EntityTable _entities;
    private readonly BbRandom _random;

    private readonly byte[] _columns = [0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13];
    private readonly byte[] _rows = [0x1D, 0x1D, 0x1D, 0x1D, 0x1D, 0x1D, 0x1D, 0x1D, 0x1D, 0x1D, 0x1D];

    internal WaterFlow(EntityTable entities, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(random);

        _entities = entities;
        _random = random;
    }

    // $4D.
    internal byte Mask { get; set; }

    // $A74B-$A755.
    internal Span<byte> Columns => _columns;

    // $A756-$A760.
    internal Span<byte> Rows => _rows;

    // $3E0A-$3E27, for a bubble at object column and row. The columns are filled before the row is looked at, so a
    // refusal (the row is too low) leaves them changed. Returns whether the water started.
    internal bool TryStart(byte column, byte row)
    {
        _columns.AsSpan().Fill((byte)(column + 1));

        byte next = (byte)(row + 1);

        if (next >= BelowLevel)
        {
            return false;
        }

        _rows.AsSpan().Fill(next);
        Mask = 0xFF;
        return true;
    }

    // $1B40: one pass of the stream. With no water this is not called.
    internal void Update(SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        Take();

        for (int cell = 0; cell < Head; cell++)
        {
            _columns[cell] = _columns[cell + 1];
            _rows[cell] = _rows[cell + 1];
        }

        if (_rows[0] == BelowLevel)
        {
            Mask = 0;
            return;
        }

        Move(map);
    }

    // $284D-$2877: puts a thing carried by the water at the cell before the head. Returns whether that cell is at the
    // bottom of the level ($ED), where an enemy drowns here and a player is let go by the caller.
    internal bool Follow(int slot)
    {
        _entities.Slot(slot).X = (byte)((_columns[BeforeHead] << 3) + XOrigin);
        _entities.Slot(slot).Y = (byte)((_rows[BeforeHead] << 3) + YOrigin);

        if (_entities.Slot(slot).Y != ParkedY)
        {
            return false;
        }

        if (slot >= FirstPlayerOnly)
        {
            _entities.Slot(slot).AttackTimer = DrownedTimer;
            _entities.Slot(slot).State = DrownedState;
            _entities.EnemyCount--;
            _entities.Slot(slot).Colour = DrownedColour;
        }

        return true;
    }

    // $287C-$2886: a player let go at the bottom stands at one of two columns.
    internal void Release(int player) => _entities.Slot(player).X = _entities.Slot(player).X < RightThreshold ? LeftEdge : RightEdge;

    private static int Distance(int a, int b) => Math.Abs(a - b);

    // $1B40's collision, with the cell before the head: `$1BD2`-`$1C0A`.
    private void Take()
    {
        byte row = _rows[BeforeHead];

        if (row is < FirstRow or >= BelowLevel)
        {
            return;
        }

        byte pixelY = (byte)((row << 3) + YOrigin);
        byte pixelX = (byte)((_columns[BeforeHead] << 3) + XOrigin);

        for (int slot = EntityTable.Capacity - 1; slot >= 0; slot--)
        {
            byte state = _entities.Slot(slot).State;

            if (state == 0
                || state >= CaughtBelow
                || (slot < FirstPlayerOnly && (_entities.Slot(slot).RiseCounter & 0x80) == 0)
                || Distance(_entities.Slot(slot).X, pixelX) >= Reach
                || Distance(_entities.Slot(slot).Y, pixelY) >= Reach)
            {
                continue;
            }

            _entities.Slot(slot).LeftFlag = state == ReleasingState ? _entities.Slot(slot).RiseCounter : state;
            _entities.Slot(slot).State = CarriedState;
        }
    }

    // $1C32-$1C97: the head's next place.
    private void Move(SolidMap map)
    {
        int row = _rows[Head];

        if (row >= BelowLevel)
        {
            return;
        }

        int column = _columns[Head];

        bool Solid(int rowOffset, int columnOffset) => map[row + rowOffset - RowBias, column + columnOffset];

        if (Mask == Falling)
        {
            if (Solid(0, 0) || !Solid(1, 0))
            {
                _rows[Head]++;
                return;
            }

            Mask = (byte)((_random.Next() & 0x01) | WalkingLeft);
        }

        if (!Solid(1, 0))
        {
            Mask = Falling;
            _rows[Head]++;
            return;
        }

        if ((Mask & 0x01) == 0)
        {
            if (!Solid(0, -1))
            {
                _columns[Head]--;
                return;
            }

            Mask++;
        }

        if (!Solid(0, 1))
        {
            _columns[Head]++;
            return;
        }

        Mask--;

        if (Solid(0, -1))
        {
            _rows[Head]++;
        }
    }
}
