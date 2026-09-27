// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Items;

internal sealed class FoodDrop
{
    internal const byte FlyingState = 0x0B;
    internal const byte FallingState = 0x0C;
    internal const byte LandedState = 0x11;
    internal const byte FoodState = 0x12;

    private const byte RisingState = 0x0A;
    private const byte NoClassState = 0x0D;

    private const byte KillBubbleTimer = 0x07;
    private const byte KillFood = 0x09;
    private const byte KillColour = 0x0E;

    private const byte FlightPace = 0x03;

    private const byte RightWall = 0xF4;
    private const byte LeftWall = 0x24;

    private const byte Top = 0x15;
    private const byte Bottom = 0xF5;

    private const byte FallStep = 0x02;

    private const byte FirstRow = 0x1F;
    private const byte RowOrigin = 0x2D;

    private const int InsideLeft = 0x51;
    private const int UnderLeft = 0x79;

    private const byte FoodLife = 0xC8;

    private const byte FoodSpriteBase = 0xF2;

    private const byte Reach = 0x10;

    private const byte DeadState = 0x0F;

    private const byte LowScore = 0x50;

    private static readonly byte[] s_flightFrames = [0x07, 0x05, 0x07, 0x07, 0x07, 0x03, 0x07, 0x03, 0x03, 0x01];

    private static readonly byte[] s_foods = [0x7E, 0x08, 0x11, 0x02, 0x18, 0x0A, 0x1D, 0x29, 0x2B, 0x2C];

    private static readonly byte[] s_scores = [0x22, 0x50, 0x01, 0x02, 0x03, 0x04, 0x05, 0x08, 0x09, 0x0A];

    private static readonly byte[] s_colours =
    [
        0x0C, 0x09, 0x09, 0x09, 0x0F, 0x0A, 0x0A, 0x0E, 0x0F, 0x0F, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0F,
        0x0B, 0x0F, 0x0A, 0x0C, 0x0D, 0x0F, 0x0F, 0x0A, 0x0D, 0x09, 0x0A, 0x0A, 0x0F, 0x0F, 0x0F, 0x09,
        0x0F, 0x0A, 0x0C, 0x0B, 0x0F, 0x0A, 0x0F, 0x0F, 0x0A, 0x0F, 0x0D, 0x0D, 0x0C, 0x0F, 0x0F,
    ];

    private readonly EntityTable _entities;
    private readonly Scores _scores;
    private readonly BbRandom _random;

    private bool _placed;

    internal FoodDrop(EntityTable entities, Scores scores, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(random);

        _entities = entities;
        _scores = scores;
        _random = random;
    }

    internal void BeginFrame() => _placed = false;

    internal void Kill(int enemy)
    {
        int slot = enemy + 2;
        byte kind = _entities.State[slot];

        if (kind == RisingState)
        {
            kind = _entities.RiseCounter[slot];
        }

        if (kind != NoClassState)
        {
            _entities.LeftFlag[slot] = kind;
        }

        _entities.State[slot] = FlyingState;
        _entities.EnemyCount--;
        _entities.RiseCounter[slot] = (byte)((_random.Next() & 0x07) + 1);

        byte climb = (byte)((_random.Next() & 0x07) + 1);
        _entities.FallCounter[slot] = climb;
        _entities.RightFlag[slot] = climb;

        _entities.GroundState[slot] = (byte)(_random.Next() & 0x01);
        _entities.BubbleTimer[slot] = KillBubbleTimer;
        _entities.AttackTimer[slot] = KillFood;
        _entities.Colour[slot] = KillColour;
    }

    internal void Fly(int slot)
    {
        _entities.BubbleTimer[slot]--;

        if ((sbyte)_entities.BubbleTimer[slot] < 0)
        {
            _entities.BubbleTimer[slot] = FlightPace;

            if ((sbyte)_entities.FallCounter[slot] >= 0)
            {
                _entities.RightFlag[slot]--;

                if (_entities.RightFlag[slot] == 0)
                {
                    _entities.FallCounter[slot] = (byte)(((_random.Next() & 0x07) + 1) | 0x80);
                }
            }
            else
            {
                _entities.RightFlag[slot]++;

                if ((_entities.RightFlag[slot] | 0x80) > _entities.FallCounter[slot])
                {
                    _entities.X[slot] &= 0xFE;
                    _entities.Y[slot] |= 0x01;
                    _entities.State[slot] = FallingState;
                    return;
                }
            }
        }

        Travel(slot);
    }

    internal void Fall(int slot, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte y = unchecked((byte)(_entities.Y[slot] + FallStep));

        if (y == Bottom)
        {
            y = Top;
        }

        _entities.Y[slot] = y;

        if (y >= FirstRow
            && ((y - RowOrigin) & 0x07) == 0
            && !EnemyProbes.Blocked(cell, map, InsideLeft, InsideLeft + 1, InsideLeft + 2)
            && EnemyProbes.Blocked(cell, map, UnderLeft, UnderLeft + 1, UnderLeft + 2))
        {
            _entities.State[slot] = LandedState;
        }

        Animate(slot);
    }

    internal void Land(int slot)
    {
        if (_placed)
        {
            return;
        }

        _placed = true;

        byte food = s_foods[_entities.AttackTimer[slot]];

        _entities.State[slot]++;
        _entities.RiseCounter[slot] = FoodLife;
        _entities.Frame[slot] = (byte)slot;
        _entities.SpriteBase[slot] = FoodSpriteBase;
        _entities.Colour[slot] = (byte)(s_colours[food] & 0x07);
        _entities.FlashTimer[slot] = 0;
    }

    internal void Wait(int slot)
    {
        _entities.RiseCounter[slot]--;

        if (_entities.RiseCounter[slot] == 0)
        {
            Clear(slot);
            return;
        }

        for (int player = 1; player >= 0; player--)
        {
            byte state = _entities.State[player];

            if (state is 0 or DeadState
                || Distance.Absolute(_entities.X[slot], _entities.X[player]) >= Reach
                || Distance.Absolute(_entities.Y[slot], _entities.Y[player]) >= Reach)
            {
                continue;
            }

            byte score = s_scores[_entities.AttackTimer[slot]];
            int index = Scores.Last(player);

            if (score != LowScore)
            {
                index--;
            }

            _scores.Add(index, score);
            Clear(slot);
            return;
        }
    }

    private void Travel(int slot)
    {
        byte speed = _entities.RiseCounter[slot];

        if (_entities.GroundState[slot] == 0)
        {
            _entities.X[slot] = unchecked((byte)(_entities.X[slot] + speed));

            if (_entities.X[slot] >= RightWall)
            {
                _entities.X[slot] = RightWall;
                _entities.GroundState[slot]++;
            }
        }
        else
        {
            _entities.X[slot] = unchecked((byte)(_entities.X[slot] - speed));

            if (_entities.X[slot] < LeftWall)
            {
                _entities.X[slot] = LeftWall;
                _entities.GroundState[slot]--;
            }
        }

        byte climb = _entities.RightFlag[slot];

        if ((sbyte)_entities.FallCounter[slot] >= 0)
        {
            _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] - climb));

            if (_entities.Y[slot] < Top)
            {
                _entities.Y[slot] = Bottom;
            }
        }
        else
        {
            _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] + climb));

            if (_entities.Y[slot] >= Bottom)
            {
                _entities.Y[slot] = Top;
            }
        }

        Animate(slot);
    }

    private void Animate(int slot)
    {
        _entities.Heading[slot]++;

        int step = (_entities.Heading[slot] >> 2) & 0x03;

        if (step != 0)
        {
            step += s_flightFrames[_entities.LeftFlag[slot]];
        }

        _entities.Frame[slot] = (byte)step;
        _entities.FlashTimer[slot] = 0;
    }

    private void Clear(int slot)
    {
        _entities.X[slot] = 0;
        _entities.Y[slot] = 0;
        _entities.State[slot] = 0;
    }
}
