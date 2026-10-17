// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

internal sealed class EntityTable : IRowTails
{
    internal const int Capacity = 8;

    private const int FirstEnemy = 2;
    private const byte CaughtState = 0x0B;

    private readonly EntitySlot[] _slots = new EntitySlot[Capacity];

    internal EntityTable()
    {
        for (int slot = 0; slot < Capacity; slot++)
        {
            _slots[slot] = new();
        }
    }

    internal byte EnemyCount { get; set; }

    internal byte SpriteEnable { get; set; }

    // `$D017` and `$D01D`, which the game only ever writes together: the sprites drawn twice the size, a bit a slot.
    internal byte SpriteExpand { get; set; }

    public bool Solid(int row, int index) => (Tail(row, index) & 0x80) != 0;

    // One slot as a whole, for code that works on a single entity.
    internal EntitySlot Slot(int slot) => _slots[slot];

    // Every slot's byte of one field, as it stands.
    internal byte[] Snapshot(SlotField<EntitySlot> field)
    {
        byte[] bytes = new byte[Capacity];

        for (int slot = 0; slot < Capacity; slot++)
        {
            bytes[slot] = field(_slots[slot]);
        }

        return bytes;
    }

    // What the sprite views draw: the entities as they stand, with the extras the game puts on top.
    internal SpriteModel Sprites(IReadOnlyList<int> foodArt, int bonusStage, BossPicture? boss, IReadOnlyList<byte> pictures)
        => new(
            Snapshot(static slot => ref slot.State),
            Snapshot(static slot => ref slot.X),
            Snapshot(static slot => ref slot.Y),
            Snapshot(static slot => ref slot.Frame),
            Snapshot(static slot => ref slot.SpriteBase),
            Snapshot(static slot => ref slot.Colour),
            Snapshot(static slot => ref slot.FlashTimer),
            SpriteEnable)
        {
            FoodArt = foodArt,
            BonusStage = bonusStage,
            Boss = boss,
            Pictures = pictures,
            Expanded = SpriteExpand,
        };

    // $16E4: the enemies not yet out of their bubbles ($01 to $0A) take an anger. $16EF is its operand: $FF makes
    // them angry (when one is left), and 0 calms them (a respawn).
    internal void SetAnger(byte anger)
    {
        for (int slot = Capacity - 1; slot >= FirstEnemy; slot--)
        {
            byte state = _slots[slot].State;

            if (state is not 0 and < CaughtState)
            {
                _slots[slot].FlashTimer = anger;
            }
        }
    }

    private byte Tail(int row, int index) => row switch
    {
        0 => _slots[index].Frame,
        1 => _slots[index].Colour,
        3 => _slots[index].SpriteBase,
        4 => _slots[index].Mode,
        5 => _slots[index].Heading,
        6 => _slots[index].AnimationTimer,
        7 => _slots[index].HoldTimer,
        8 => _slots[index].TurnTimer,
        9 => _slots[index].TurnInterval,
        11 => _slots[index].LeapFlag,
        12 => _slots[index].ClimbFlag,
        13 => _slots[index].FlashTimer,
        14 => _slots[index].FrameCount,
        15 => _slots[index].FrameMask,
        16 => _slots[index].RiseCounter,
        17 => _slots[index].FallCounter,
        18 => _slots[index].GroundState,
        19 => _slots[index].BubbleTimer,
        20 => _slots[index].LeftFlag,
        21 => _slots[index].RightFlag,
        22 => _slots[index].AttackTimer,
        _ => 0,
    };
}
