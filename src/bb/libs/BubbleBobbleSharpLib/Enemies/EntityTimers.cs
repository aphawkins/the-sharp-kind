// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $13BE in player-sprites.s, `enemy_timer_handler`: the clock every one of the eighteen slots runs
// on, and what happens as it runs out. The game loop calls it at $0A51, straight after the AI loop.
//
// **This is what ends a bubble.** Nothing in enemy-ai.s expires anything. A bubble is made with $7D
// in $A9FA, this takes one off it every pass, and the last few counts are the wobble and then the
// pop. Two of the four phases the VICE capture recorded belong here rather than to any mover.
//
// It counts a different byte from the AI. $13C0 decrements $A9FA, which ObjectTable calls Flags.
// The AI state counter is $A9B2, and nothing in here touches that one at all.
internal sealed class EntityTimers
{
    // $13C6 and $1422. A free-floating bubble.
    private const byte BubbleType = 0x04;

    // $1425. What a bubble alternates with while it wobbles. $04 eor $4C is $48, and $48 eor $4C is
    // back to $04, so one exclusive-or does both halves of the toggle.
    private const byte WobbleType = 0x48;
    private const byte WobbleToggle = 0x4C;

    // $13CC and $1466. The first frame of the pop.
    private const byte PoppingType = 0x3A;

    // $13D7 and $142E. At or above the first and below the second, the slot is a bubble with an
    // enemy inside it, and which enemy is in the type byte - see BubbleCollision.
    private const byte CapturedFrom = 0x18;
    private const byte SpecialFrom = 0x24;

    // $1404. What a bubble becomes once the enemy inside it is let out.
    private const byte FreedType = 0x38;

    // $140B. The one high type this routine has anything to say about.
    private const byte ExpiredType = 0x42;

    // $144F and $1457. Slots at or above $34 are counted, and two of them is enough.
    private const byte CapturedCountFrom = 0x34;
    private const int CapturedWanted = 0x02;

    // $1415. Below this the slot flickers, on the even counts only.
    private const byte FlickerBelow = 0x11;

    // $105B's enemies are slots 2 to 7 of the eight, and a release puts one back where it came from.
    private const int EnemyBase = 2;

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;

    internal EntityTimers(ObjectTable objects, EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);

        _objects = objects;
        _entities = entities;
    }

    // $13BE. Every slot's clock, then the pressure pass.
    //
    // `releasedState` is $5ABF, a byte that lives inside unused sprite data - the conversion is out
    // of room and keeps it there. bonus-round.s writes it and nothing translated so far does, so it
    // is passed in rather than guessed at.
    internal void Update(byte releasedState)
    {
        for (int slot = ObjectTable.Capacity - 1; slot >= 0; slot--)
        {
            Tick(slot, releasedState);
        }

        Pressure();
    }

    // $13C0. One off the clock, and everything else hangs on whether that reached zero.
    private void Tick(int slot, byte releasedState)
    {
        _objects.Flags[slot]--;

        if (_objects.Flags[slot] != 0)
        {
            Flicker(slot);
            return;
        }

        byte type = _objects.Type[slot];

        // $13C6. A bubble that runs out of clock keeps what it was and starts popping.
        if (type == BubbleType)
        {
            _objects.EnemyType[slot] = BubbleType;
            _objects.Type[slot] = PoppingType;
            return;
        }

        if (type >= SpecialFrom)
        {
            // $140B. Only $42 means anything up here, and it frees the slot outright.
            if (type == ExpiredType)
            {
                _objects.Type[slot] = ObjectTable.FreeType;
            }

            return;
        }

        if (type < CapturedFrom)
        {
            return;
        }

        Release(slot, type, releasedState);
    }

    // $13DA. A bubble with an enemy in it has run out, so the enemy goes back into the eight slots.
    //
    // The two bit twiddles at $13E8 and $13EE are the reference's own and are not symmetrical: the
    // row gets its bottom bit set and the column gets its bottom bit cleared. That nudges the enemy
    // half a step off the grid in one axis only, and a translation that tidies them into a pair
    // puts every released enemy in the wrong place.
    private void Release(int slot, byte type, byte releasedState)
    {
        // $13DC. The inverse of what BubbleCollision's $1091 encoded.
        int entity = ((type - CapturedFrom) >> 1) + EnemyBase;

        _entities.State[entity] = releasedState;
        _entities.Y[entity] |= 0x01;
        _entities.X[entity] &= 0xFE;
        _entities.FlashTimer[entity] = 0xFF;

        // $13FD. Whatever the capture stashed comes back as the enemy's rise counter - the same byte
        // $10B1 put there, which is why that one matters.
        _entities.RiseCounter[entity] = _objects.Variant[slot];
        _entities.Frame[entity] = 0;

        _objects.Type[slot] = FreedType;
    }

    // $1413. The last sixteen counts, on the even ones only.
    private void Flicker(int slot)
    {
        byte timer = _objects.Flags[slot];

        if (timer >= FlickerBelow || (timer & 0x01) != 0)
        {
            return;
        }

        byte type = _objects.Type[slot];

        // $1428. The wobble the capture read at frames 195 to 215: $04 and $48, turn about.
        // $142E's other arm has a captured enemy flicker instead, by toggling its bit in the VIC's
        // sprite enable register through the mask table at $AB55. That is VIC-II work and section 1
        // puts it out of scope, so it is not translated - nothing else in here depends on it.
        if (type is BubbleType or WobbleType)
        {
            _objects.Type[slot] = (byte)(type ^ WobbleToggle);
        }
    }

    // $1449. If fewer than two enemies are held, pop free bubbles until they are - which is what
    // stops a player parking the level full of bubbles and waiting.
    private void Pressure()
    {
        int captured = 0;

        foreach (byte type in _objects.Type)
        {
            if (type >= CapturedCountFrom)
            {
                captured++;
            }
        }

        if (captured >= CapturedWanted)
        {
            return;
        }

        // $145C is a `jsr` to the very next instruction, so $145F runs once through the call and
        // once by falling into it. Two bubbles a pass, not one. Read as a plain call it is half the
        // rate, and the level fills up.
        PopFirst();
        PopFirst();
    }

    // $145F. The highest-numbered free bubble, and only that one.
    private void PopFirst()
    {
        for (int slot = ObjectTable.Capacity - 1; slot >= 0; slot--)
        {
            if (_objects.Type[slot] != BubbleType)
            {
                continue;
            }

            _objects.EnemyType[slot] = BubbleType;
            _objects.Type[slot] = PoppingType;
            return;
        }
    }
}
