// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Bubbles;

// $0BED (`collision.s`, `enemy_spawn_handler`): the special bubbles that float up from the corners of a level. The
// reference calls it an enemy spawner; it makes objects, not enemies, and only while an enemy is left ($4A).
//
// It is code the level patches. `$0A38` is a `jsr $0BED` or a `bit $0BED` (a no-op): `$3A6C`'s tail picks one from the
// level's spawn flags, so most levels never spawn a bubble. The same tail patches `$0C9C` (a `jmp $0CC1` or a `bit`) and
// the operand of `$0CC1`'s `and #`. `$0BEE`, the operand of `lda #$01` at `$0BED`, is the countdown; nothing resets it
// at a level start, so it carries over from one level to the next.
//
//   * Every 22 passes, if an enemy is left: a roll below $28 skips the spawn; a second roll picks a corner. A corner
//     spawns only where the bubble current at the level's edge flows into the level: down (2) along the top row, up (0)
//     along the bottom row, and the cell is not a wall.
//   * Ten or more special bubbles ($06 to $0B) stop it, and so does having no free object slot.
//   * The standard chooser (`jmp $0CC1`): a roll masked by the level's operand gives bit 0 for a $06 bubble (`$A9E8`
//     $0B), else bit 1 for $08 ($0A), else bit 2 for $0A ($0D), else a plain $04 ($0D); flags $82.
//   * The other chooser (`bit`, reached through `$0C9F`, which the disassembly calls unreachable): a $0C to $14 EXTEND
//     letter bubble, the letter from a roll plus the seed at `$5BFF` (kept, so the letters come round in turn), and
//     `$A9E8` $0D; it sets no flags. `$4C` counts these down; the call that takes it below zero turns `$0C9C` back into
//     a `jmp`, but is still one of these. So a level with this chooser makes `$4C` + 1 letter bubbles, then standard ones.
internal sealed class BubbleSpawner
{
    internal const byte Interval = 0x16;

    private const int LastCorner = 3;
    private const byte SkipBelow = 0x28;
    private const byte SpecialFrom = 0x06;
    private const byte SpecialBelow = 0x0C;
    private const int MostSpecial = 10;

    private const int TopRow = 0;
    private const int BottomRow = 24;
    private const int LeftColumn = 10;
    private const int RightColumn = 20;
    private const byte LeftSpawn = 0x0B;
    private const byte RightSpawn = 0x14;
    private const byte BottomSpawn = 0x1C;

    private const int XOrigin = 0x14;
    private const int YOrigin = 0x15;

    private const byte PlainType = 0x04;
    private const byte FirstLetterType = 0x0C;
    private const byte WalkerBehaviour = 0x0D;
    private const byte FlagsStandard = 0x82;

    private const byte ChooserFlag = 0x01;
    private const byte ChooserRolls = 0x03;
    private const byte MaskShift = 1;
    private const byte MaskBits = 0x07;

    private const byte SeedWrap = 0x05;
    private const byte SeedMask = 0x07;
    private const byte SeedNarrow = 0x03;

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;
    private readonly BbRandom _random;

    internal BubbleSpawner(ObjectTable objects, EntityTable entities, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(random);

        _objects = objects;
        _entities = entities;
        _random = random;
    }

    // $0A38: a `jsr` (true) or a `bit`. The game's image starts with a `jsr`; every level sets it.
    internal bool Enabled { get; set; } = true;

    // $0BEE.
    internal byte Timer { get; set; } = 1;

    // $0C9C: the standard chooser (a `jmp`), or the other one (a `bit`).
    internal bool Standard { get; set; } = true;

    // $0CC2, the `and #` operand of the standard chooser.
    internal byte Mask { get; set; }

    // $4C.
    internal byte LetterCount { get; set; }

    // $5BFF, in the image as $03.
    internal byte Seed { get; set; } = 0x03;

    // The tail of the enemy spawner, $3A6C to $3AB7, from the level's spawn flags. Bit 0 of the flags turns the spawner
    // on. A roll's bit 0 then keeps the standard chooser; otherwise a second roll, masked to two bits, counts the letter
    // bubbles. The other flag bits, shifted down, are the standard chooser's mask.
    internal void Configure(int spawnFlags)
    {
        int flags = spawnFlags & 0xFF;

        if ((flags & ChooserFlag) == 0)
        {
            Enabled = false;
            return;
        }

        Standard = true;

        if ((_random.Next() & ChooserFlag) == 0)
        {
            Standard = false;
            LetterCount = (byte)(_random.Next() & ChooserRolls);
        }

        Mask = (byte)((flags >> MaskShift) & MaskBits);
        Enabled = true;
    }

    internal void Update(SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        if (!Enabled)
        {
            return;
        }

        if (Timer != 0)
        {
            Timer--;
            return;
        }

        Timer = Interval;

        if (_entities.EnemyCount == 0 || _random.Next() < SkipBelow)
        {
            return;
        }

        if (Corner(map) is not (byte column, byte row) || Specials() >= MostSpecial)
        {
            return;
        }

        int slot = _objects.FindFree();

        if (slot < 0)
        {
            return;
        }

        Place(slot, column, row);
    }

    private static bool Free(bool solid, int direction, int wanted) => !solid && direction == wanted;

    private (byte Column, byte Row)? Corner(SolidMap map)
    {
        int corner = _random.Next() & LastCorner;

        return corner switch
        {
            0 => Free(map[TopRow, LeftColumn], map.Direction(TopRow, LeftColumn), EntityMover.Down)
                ? (LeftSpawn, 0)
                : null,
            1 => Free(map[TopRow, RightColumn], map.Direction(TopRow, RightColumn), EntityMover.Down)
                ? (RightSpawn, 0)
                : null,
            2 => Free(map[BottomRow, LeftColumn], map.Direction(BottomRow, LeftColumn), EntityMover.Up)
                ? (LeftSpawn, BottomSpawn)
                : null,
            _ => Free(map[BottomRow, RightColumn], map.Direction(BottomRow, RightColumn), EntityMover.Up)
                ? (RightSpawn, BottomSpawn)
                : null,
        };
    }

    private int Specials()
    {
        int count = 0;

        for (int slot = ObjectTable.Capacity - 1; slot >= 0; slot--)
        {
            if (_objects.Type[slot] is >= SpecialFrom and < SpecialBelow)
            {
                count++;
            }
        }

        return count;
    }

    private void Place(int slot, byte column, byte row)
    {
        _objects.State[slot] = 0;
        _objects.SubX[slot] = 0;
        _objects.SubY[slot] = 0;
        _objects.Column[slot] = column;
        _objects.X[slot] = (byte)((column << 3) + XOrigin);
        _objects.Row[slot] = row;
        _objects.Y[slot] = (byte)((row << 3) + YOrigin);

        byte roll = _random.Next();

        if (Standard)
        {
            Standardly(slot, (byte)(roll & Mask));
            return;
        }

        if (LetterCount-- == 0)
        {
            Standard = true;
        }

        // `$0CA8`: the roll at `$0C99` is not used; a fresh one is.
        int seed = (_random.Next() + Seed) & SeedMask;

        Seed = (byte)(seed < SeedWrap ? seed : seed & SeedNarrow);

        _objects.Type[slot] = (byte)((Seed << 1) + FirstLetterType);
        _objects.Behaviour[slot] = WalkerBehaviour;
    }

    private void Standardly(int slot, byte bits)
    {
        _objects.Flags[slot] = FlagsStandard;

        (byte type, byte behaviour) = (bits & 0x01) != 0
            ? ((byte)0x06, (byte)0x0B)
            : (bits & 0x02) != 0
                ? ((byte)0x08, (byte)0x0A)
                : (bits & 0x04) != 0
                    ? ((byte)0x0A, WalkerBehaviour)
                    : (PlainType, WalkerBehaviour);

        _objects.Type[slot] = type;
        _objects.Behaviour[slot] = behaviour;
    }
}
