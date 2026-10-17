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
    private readonly bool[] _climbNext = new bool[Capacity];

    internal EntityTable()
    {
        for (int slot = 0; slot < Capacity; slot++)
        {
            _slots[slot] = new();
        }
    }

    internal SlotColumn<EntitySlot> State => new(_slots, static slot => ref slot.State);

    internal SlotColumn<EntitySlot> X => new(_slots, static slot => ref slot.X);

    internal SlotColumn<EntitySlot> Y => new(_slots, static slot => ref slot.Y);

    internal SlotColumn<EntitySlot> Frame => new(_slots, static slot => ref slot.Frame);

    internal SlotColumn<EntitySlot> AnimationTimer => new(_slots, static slot => ref slot.AnimationTimer);

    internal SlotColumn<EntitySlot> BubbleTimer => new(_slots, static slot => ref slot.BubbleTimer);

    internal SlotColumn<EntitySlot> RiseCounter => new(_slots, static slot => ref slot.RiseCounter);

    internal SlotColumn<EntitySlot> FallCounter => new(_slots, static slot => ref slot.FallCounter);

    internal SlotColumn<EntitySlot> LeftFlag => new(_slots, static slot => ref slot.LeftFlag);

    internal SlotColumn<EntitySlot> RightFlag => new(_slots, static slot => ref slot.RightFlag);

    internal SlotColumn<EntitySlot> Colour => new(_slots, static slot => ref slot.Colour);

    internal SlotColumn<EntitySlot> FlashTimer => new(_slots, static slot => ref slot.FlashTimer);

    internal SlotColumn<EntitySlot> Mode => new(_slots, static slot => ref slot.Mode);

    internal SlotColumn<EntitySlot> HoldTimer => new(_slots, static slot => ref slot.HoldTimer);

    internal SlotColumn<EntitySlot> GroundState => new(_slots, static slot => ref slot.GroundState);

    internal SlotColumn<EntitySlot> Heading => new(_slots, static slot => ref slot.Heading);

    internal SlotColumn<EntitySlot> SpriteBase => new(_slots, static slot => ref slot.SpriteBase);

    internal SlotColumn<EntitySlot> TurnTimer => new(_slots, static slot => ref slot.TurnTimer);

    internal SlotColumn<EntitySlot> TurnInterval => new(_slots, static slot => ref slot.TurnInterval);

    internal SlotColumn<EntitySlot> FrameCount => new(_slots, static slot => ref slot.FrameCount);

    internal SlotColumn<EntitySlot> FrameMask => new(_slots, static slot => ref slot.FrameMask);

    internal SlotColumn<EntitySlot> LeapFlag => new(_slots, static slot => ref slot.LeapFlag);

    internal SlotColumn<EntitySlot> ClimbFlag => new(_slots, static slot => ref slot.ClimbFlag);

    internal Span<bool> ClimbNext => _climbNext;

    internal SlotColumn<EntitySlot> AttackTimer => new(_slots, static slot => ref slot.AttackTimer);

    internal byte EnemyCount { get; set; }

    internal byte SpriteEnable { get; set; }

    // `$D017` and `$D01D`, which the game only ever writes together: the sprites drawn twice the size, a bit a slot.
    internal byte SpriteExpand { get; set; }

    public bool Solid(int row, int index) => (Tail(row, index) & 0x80) != 0;

    // One slot as a whole, for code that works on a single entity.
    internal EntitySlot Slot(int slot) => _slots[slot];

    // What the sprite views draw: the entities as they stand, with the extras the game puts on top.
    internal SpriteModel Sprites(IReadOnlyList<int> foodArt, int bonusStage, BossPicture? boss, IReadOnlyList<byte> pictures)
        => new(
            State.ToArray(),
            X.ToArray(),
            Y.ToArray(),
            Frame.ToArray(),
            SpriteBase.ToArray(),
            Colour.ToArray(),
            FlashTimer.ToArray(),
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
