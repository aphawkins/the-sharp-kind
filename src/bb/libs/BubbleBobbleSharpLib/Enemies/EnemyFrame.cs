// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;

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
    private const byte ReleasedState = 0x0A;
    private const byte LeftWall = 0x24;
    private const byte RightWall = 0xF4;

    private readonly EntityTable _entities;
    private readonly EnemyWalker _walker;
    private readonly EnemyShot _shot;
    private readonly EnemyHopper _hopper;
    private readonly DiagonalMover _diagonal;
    private readonly EnemyRunner _runner;
    private readonly FoodDrop _food;
    private readonly EnemyTumble _tumble;
    private readonly LevelFlow _flow;
    private readonly WaterFlow _water;

    internal EnemyFrame(
        EntityTable entities,
        EnemyWalker walker,
        EnemyShot shot,
        EnemyHopper hopper,
        DiagonalMover diagonal,
        EnemyRunner runner,
        FoodDrop food,
        LevelFlow flow,
        WaterFlow water)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(walker);
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(hopper);
        ArgumentNullException.ThrowIfNull(diagonal);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(food);
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentNullException.ThrowIfNull(water);

        _entities = entities;
        _walker = walker;
        _shot = shot;
        _hopper = hopper;
        _diagonal = diagonal;
        _runner = runner;
        _food = food;
        _flow = flow;
        _water = water;
        _tumble = new(entities, food);
    }

    // The giant bonus a cleared level drops (state $15), which the game wires in.
    internal SuperBonus? SuperBonus { get; set; }

    // The sprites' show of the special items of types 11 and 29 (states $13 and $14), which the game wires in.
    internal SpriteShow? Show { get; set; }

    internal void Enter()
    {
        for (int slot = FirstEnemy; slot < EntityTable.Capacity; slot++)
        {
            _entities.Slot(slot).AttackTimer = _entities.Slot(slot).Y;
            _entities.Slot(slot).Y = EntryRow;
            _entities.Slot(slot).Mode = 0;
        }

        for (int frame = 0; frame < EntryFrames; frame++)
        {
            for (int slot = EntityTable.Capacity - 1; slot >= FirstEnemy; slot--)
            {
                if (_entities.Slot(slot).State == 0)
                {
                    continue;
                }

                Idle(slot);

                if (_entities.Slot(slot).Y == _entities.Slot(slot).AttackTimer)
                {
                    _entities.Slot(slot).Mode--;
                }
                else
                {
                    _entities.Slot(slot).Y = unchecked((byte)(_entities.Slot(slot).Y + DropStep));
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

        if (_entities.Slot(slot).State == 0)
        {
            return;
        }

        if ((_entities.Slot(slot).Mode & Entered) == 0)
        {
            Drop(slot);
            Idle(slot);
            return;
        }

        // $1CF5-$1D01: a frozen level keeps every enemy that is not out of its bubble still.
        if (_flow.Freeze != 0 && _entities.Slot(slot).State < CaughtState)
        {
            return;
        }

        int target = Target(slot);

        if (_entities.Slot(slot).FlashTimer != 0 && (counter & AngerFrames) == 0)
        {
            Dispatch(slot, target, counter, map);
        }

        if (_entities.Slot(slot).HoldTimer != 0)
        {
            _entities.Slot(slot).HoldTimer--;
            Idle(slot);
            return;
        }

        Dispatch(slot, target, counter, map);
    }

    private int Target(int slot)
    {
        int player = slot & 0x01;

        return _entities.Slot(player).State == PlayingState ? player : player ^ 0x01;
    }

    private void Drop(int slot)
    {
        if (_entities.Slot(slot).Y != _entities.Slot(slot).AttackTimer)
        {
            _entities.Slot(slot).Y = unchecked((byte)(_entities.Slot(slot).Y + DropStep));
            return;
        }

        _entities.Slot(slot).Mode--;
        _entities.Slot(slot).BubbleTimer = 0xFF;
        _entities.Slot(slot).AttackTimer = FirstAttackDelay;
    }

    private void Idle(int slot)
    {
        switch (_entities.Slot(slot).State)
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

    private void Dispatch(int slot, int target, byte counter, SolidMap map)
    {
        PlayerCell cell = PlayerCell.Of(_entities.Slot(slot).X, _entities.Slot(slot).Y);

        switch (_entities.Slot(slot).State)
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
            case ReleasedState:
                Released(slot, cell, map);
                break;
            case WaterFlow.CarriedState:
                _ = _water.Follow(slot);
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
            case SuperBonus.State:
                SuperBonus?.Step();
                break;
            case SpriteShow.DiamondState:
            case SpriteShow.GiantState:
                Show?.Step(slot, counter);
                break;
            case EnemyTumble.TumbleState:
                _tumble.Step(slot, cell, map);
                break;
            case EnemyTumble.WaitingState:
                _tumble.Touch(slot);
                break;
        }
    }

    // $2808-$284C: out of its bubble an enemy drops, and lands as the class it was (held in RiseCounter). Once down it
    // is kept inside the walls and its jump, fall and ground state start afresh.
    private void Released(int slot, in PlayerCell cell, SolidMap map)
    {
        EntityAnimation.Step(_entities, slot);
        _food.Drop(slot, cell, map, _entities.Slot(slot).RiseCounter);

        if (_entities.Slot(slot).State == ReleasedState)
        {
            return;
        }

        if (_entities.Slot(slot).State == RunnerState)
        {
            _entities.Slot(slot).X = (byte)(((byte)(_entities.Slot(slot).X - 0x14) & 0xFC) + 0x14);
        }

        _entities.Slot(slot).X = Math.Clamp(_entities.Slot(slot).X, LeftWall, RightWall);
        _entities.Slot(slot).RiseCounter = 0xFF;
        _entities.Slot(slot).FallCounter = 0xFF;
        _entities.Slot(slot).GroundState = 0xFF;
        _entities.Slot(slot).BubbleTimer = 0xFF;
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
