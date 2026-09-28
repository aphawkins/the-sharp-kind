// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class EnemyFrame
{
    private const int FirstEnemy = 2;

    private const byte Entered = 0x80;

    private const byte DropStep = 0x02;

    private const byte EntryRow = 0x15;
    private const int EntryFrames = 0x16;

    private const byte FirstAttackDelay = 0x0A;

    private const byte AngerFrames = 0x02;

    private const byte PlayingState = 0x01;

    private const byte CaughtState = 0x0B;

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
    private readonly FoodDrop _food;
    private readonly LevelFlow _flow;

    internal EnemyFrame(
        EntityTable entities,
        EnemyWalker walker,
        EnemyShot shot,
        EnemyHopper hopper,
        DiagonalMover diagonal,
        EnemyRunner runner,
        FoodDrop food,
        LevelFlow flow)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(walker);
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(hopper);
        ArgumentNullException.ThrowIfNull(diagonal);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(food);
        ArgumentNullException.ThrowIfNull(flow);

        _entities = entities;
        _walker = walker;
        _shot = shot;
        _hopper = hopper;
        _diagonal = diagonal;
        _runner = runner;
        _food = food;
        _flow = flow;
    }

    internal void Enter()
    {
        for (int slot = FirstEnemy; slot < EntityTable.Capacity; slot++)
        {
            _entities.AttackTimer[slot] = _entities.Y[slot];
            _entities.Y[slot] = EntryRow;
            _entities.Mode[slot] = 0;
        }

        for (int frame = 0; frame < EntryFrames; frame++)
        {
            for (int slot = EntityTable.Capacity - 1; slot >= FirstEnemy; slot--)
            {
                if (_entities.State[slot] == 0)
                {
                    continue;
                }

                Idle(slot);

                if (_entities.Y[slot] == _entities.AttackTimer[slot])
                {
                    _entities.Mode[slot]--;
                }
                else
                {
                    _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] + DropStep));
                }
            }
        }
    }

    internal void Step(byte counter, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        _food.BeginFrame();

        for (int slot = EntityTable.Capacity - 1; slot >= FirstEnemy; slot--)
        {
            Step(slot, counter, map);
        }
    }

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

        // $1CF5-$1D01: a frozen level keeps every enemy that is not out of its bubble still.
        if (_flow.Freeze != 0 && _entities.State[slot] < CaughtState)
        {
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

    private int Target(int slot)
    {
        int player = slot & 0x01;

        return _entities.State[player] == PlayingState ? player : player ^ 0x01;
    }

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
            case FoodDrop.FlyingState:
                _food.Fly(slot);
                break;
            case FoodDrop.FallingState:
                _food.Fall(slot, cell, map);
                break;
            case FoodDrop.LandedState:
                _food.Land(slot);
                break;
            case FoodDrop.FoodState:
                _food.Wait(slot);
                break;
        }
    }

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
