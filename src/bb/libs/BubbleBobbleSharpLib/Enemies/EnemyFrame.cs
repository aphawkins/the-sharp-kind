// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $1CBD's loop over slots 7 to 2, with $1CA0, $1E87 and $1E6C: what happens to an enemy between one
// frame and the next.
//
// The 6502 runs one loop from slot 7 down to slot 0, so the enemies go before the players in the same
// frame. PlayerFrame is slots 1 and 0. A caller runs this first and then PlayerFrame.
//
// Four things decide what an enemy does with its frame, in this order:
//
//   * **Mode not negative** is an enemy still dropping into the level. $1CA0 moves it two pixels down
//     until it reaches the row AttackTimer holds, and $1E87 animates it.
//   * **FlashTimer not zero** is an angry enemy, let out of a bubble. It takes an extra $1E6C on every
//     frame whose counter has bit 1 clear, so it moves one and a half times as fast.
//   * **HoldTimer not zero** is the spawn delay. The enemy only animates, through $1E87.
//   * Otherwise $1E6C dispatches on the state, through the $1E3A table.
//
// **Not here:** $1CFB's test of $67. While that byte is not zero, an enemy in a state below $0B is
// skipped for the whole frame. $2F74 sets it, and that is an item's effect, so Phase 7's.
internal sealed class EnemyFrame
{
    // $1CF5. Slots 0 and 1 are the players.
    private const int FirstEnemy = 2;

    // $1CDB's `lda D_85C0,x` / `bpl`. A player's is $FF from the start.
    private const byte Entered = 0x80;

    // $1CA9. Two `inc` a frame.
    private const byte DropStep = 0x02;

    // $1CB5. What AttackTimer holds as the drop ends.
    private const byte FirstAttackDelay = 0x0A;

    // $1D0A. An angry enemy's extra step is on the frames with this bit of the counter clear.
    private const byte AngerFrames = 0x02;

    // $1CEB. A live player, and the one an enemy follows if it can.
    private const byte PlayingState = 0x01;

    // The $1E3A table's entries this port has translated.
    private const byte WalkerState = 0x02;
    private const byte ClassOneState = 0x03;
    private const byte HopperState = 0x04;
    private const byte DiagonalState = 0x05;
    private const byte FacingDiagonalState = 0x06;
    private const byte ClassFiveState = 0x07;
    private const byte ClassSixState = 0x08;
    private const byte RunnerState = 0x09;

    private readonly EntityTable _entities;
    private readonly EnemyWalker _walker;
    private readonly EnemyShot _shot;
    private readonly EnemyHopper _hopper;
    private readonly DiagonalMover _diagonal;
    private readonly EnemyRunner _runner;

    internal EnemyFrame(
        EntityTable entities,
        EnemyWalker walker,
        EnemyShot shot,
        EnemyHopper hopper,
        DiagonalMover diagonal,
        EnemyRunner runner)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(walker);
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(hopper);
        ArgumentNullException.ThrowIfNull(diagonal);
        ArgumentNullException.ThrowIfNull(runner);

        _entities = entities;
        _walker = walker;
        _shot = shot;
        _hopper = hopper;
        _diagonal = diagonal;
        _runner = runner;
    }

    // $1CBD's loop, from slot 7 down. counter is $08, the frame counter the IRQ steps.
    internal void Step(byte counter, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        for (int slot = EntityTable.Capacity - 1; slot >= FirstEnemy; slot--)
        {
            Step(slot, counter, map);
        }
    }

    // $1CDB to $1D21, for one enemy slot.
    internal void Step(int slot, byte counter, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        if (_entities.State[slot] == 0)
        {
            return;
        }

        if ((_entities.Mode[slot] & Entered) == 0)
        {
            Drop(slot);
            Idle(slot);
            return;
        }

        int target = Target(slot);

        if (_entities.FlashTimer[slot] != 0 && (counter & AngerFrames) == 0)
        {
            Dispatch(slot, target, map);
        }

        if (_entities.HoldTimer[slot] != 0)
        {
            _entities.HoldTimer[slot]--;
            Idle(slot);
            return;
        }

        Dispatch(slot, target, map);
    }

    // $1CE3 to $1CF3. The player on the enemy's own side of the slot numbers if they are playing, and
    // the other one if not. Nothing asks whether the other one is playing.
    private int Target(int slot)
    {
        int player = slot & 0x01;

        return _entities.State[player] == PlayingState ? player : player ^ 0x01;
    }

    // $1CA0. The drop into the level. The `inc` pair is not a compare, so a drop that steps over its
    // row goes on round the byte until it meets it.
    private void Drop(int slot)
    {
        if (_entities.Y[slot] != _entities.AttackTimer[slot])
        {
            _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] + DropStep));
            return;
        }

        _entities.Mode[slot]--;
        _entities.BubbleTimer[slot] = 0xFF;
        _entities.AttackTimer[slot] = FirstAttackDelay;
    }

    // $1E87. An enemy that is not free to move still animates: state 4 by $214A and state 9 by $EE4A,
    // which are the same toggle, state 5 by $EFEA, and the rest by $EB0F.
    private void Idle(int slot)
    {
        switch (_entities.State[slot])
        {
            case DiagonalState:
                _diagonal.Animate(slot);
                break;
            case HopperState:
            case RunnerState:
                EntityAnimation.Toggle(_entities, slot);
                break;
            default:
                EntityAnimation.Step(_entities, slot);
                break;
        }
    }

    // $1E6C. The cell first, then the state, through the $1E3A table. States 2 to 9 are the eight
    // enemy classes; the others belong to things this class does not drive.
    private void Dispatch(int slot, int target, SolidMap map)
    {
        PlayerCell cell = PlayerCell.Of(_entities.X[slot], _entities.Y[slot]);

        switch (_entities.State[slot])
        {
            case WalkerState:
                _walker.Step(slot, target, cell, map);
                break;
            case ClassOneState:
            case ClassFiveState:
            case ClassSixState:
                Shooter(slot, target, cell, map);
                break;
            case HopperState:
                _hopper.Step(slot, target, cell, map);
                break;
            case DiagonalState:
                _diagonal.Step(slot, cell, map);
                break;
            case FacingDiagonalState:
                _diagonal.StepFacing(slot, cell, map);
                break;
            case RunnerState:
                _runner.Step(slot, target, cell, map);
                break;
        }
    }

    // $E9FD. The shot first, then the walk unless a shot is counting down.
    private void Shooter(int slot, int target, in PlayerCell cell, SolidMap map)
    {
        _shot.Aim(slot, target);

        if (_shot.Booked(slot))
        {
            _shot.Fire(slot, cell, map);
            return;
        }

        _walker.Step(slot, target, cell, map);
    }
}
