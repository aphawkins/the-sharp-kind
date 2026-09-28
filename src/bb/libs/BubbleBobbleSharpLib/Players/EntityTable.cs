// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

internal sealed class EntityTable : IRowTails
{
    internal const int Capacity = 8;

    private const int FirstEnemy = 2;
    private const byte CaughtState = 0x0B;

    private readonly byte[] _state = new byte[Capacity];
    private readonly byte[] _x = new byte[Capacity];
    private readonly byte[] _y = new byte[Capacity];
    private readonly byte[] _frame = new byte[Capacity];
    private readonly byte[] _animationTimer = new byte[Capacity];
    private readonly byte[] _bubbleTimer = new byte[Capacity];
    private readonly byte[] _riseCounter = new byte[Capacity];
    private readonly byte[] _fallCounter = new byte[Capacity];
    private readonly byte[] _leftFlag = new byte[Capacity];
    private readonly byte[] _rightFlag = new byte[Capacity];
    private readonly byte[] _groundState = new byte[Capacity];
    private readonly byte[] _colour = new byte[Capacity];
    private readonly byte[] _mode = new byte[Capacity];
    private readonly byte[] _holdTimer = new byte[Capacity];
    private readonly byte[] _flashTimer = new byte[Capacity];
    private readonly byte[] _heading = new byte[Capacity];
    private readonly byte[] _spriteBase = new byte[Capacity];
    private readonly byte[] _turnTimer = new byte[Capacity];
    private readonly byte[] _turnInterval = new byte[Capacity];
    private readonly byte[] _frameCount = new byte[Capacity];
    private readonly byte[] _frameMask = new byte[Capacity];
    private readonly bool[] _climbNext = new bool[Capacity];
    private readonly byte[] _leapFlag = new byte[Capacity];
    private readonly byte[] _climbFlag = new byte[Capacity];
    private readonly byte[] _attackTimer = new byte[Capacity];

    internal Span<byte> State => _state;

    internal Span<byte> X => _x;

    internal Span<byte> Y => _y;

    internal Span<byte> Frame => _frame;

    internal Span<byte> AnimationTimer => _animationTimer;

    internal Span<byte> BubbleTimer => _bubbleTimer;

    internal Span<byte> RiseCounter => _riseCounter;

    internal Span<byte> FallCounter => _fallCounter;

    internal Span<byte> LeftFlag => _leftFlag;

    internal Span<byte> RightFlag => _rightFlag;

    internal Span<byte> Colour => _colour;

    internal Span<byte> FlashTimer => _flashTimer;

    internal Span<byte> Mode => _mode;

    internal Span<byte> HoldTimer => _holdTimer;

    internal Span<byte> GroundState => _groundState;

    internal Span<byte> Heading => _heading;

    internal Span<byte> SpriteBase => _spriteBase;

    internal Span<byte> TurnTimer => _turnTimer;

    internal Span<byte> TurnInterval => _turnInterval;

    internal Span<byte> FrameCount => _frameCount;

    internal Span<byte> FrameMask => _frameMask;

    internal Span<byte> LeapFlag => _leapFlag;

    internal Span<byte> ClimbFlag => _climbFlag;

    internal Span<bool> ClimbNext => _climbNext;

    internal Span<byte> AttackTimer => _attackTimer;

    internal byte EnemyCount { get; set; }

    internal byte SpriteEnable { get; set; }

    public bool Solid(int row, int index) => (Tail(row, index) & 0x80) != 0;

    // $16E4: the enemies not yet out of their bubbles ($01 to $0A) take an anger. $16EF is its operand: $FF makes
    // them angry (when one is left), and 0 calms them (a respawn).
    internal void SetAnger(byte anger)
    {
        for (int slot = Capacity - 1; slot >= FirstEnemy; slot--)
        {
            byte state = _state[slot];

            if (state is not 0 and < CaughtState)
            {
                _flashTimer[slot] = anger;
            }
        }
    }

    private byte Tail(int row, int index) => row switch
    {
        0 => _frame[index],
        1 => _colour[index],
        3 => _spriteBase[index],
        4 => _mode[index],
        5 => _heading[index],
        6 => _animationTimer[index],
        7 => _holdTimer[index],
        8 => _turnTimer[index],
        9 => _turnInterval[index],
        11 => _leapFlag[index],
        12 => _climbFlag[index],
        13 => _flashTimer[index],
        14 => _frameCount[index],
        15 => _frameMask[index],
        16 => _riseCounter[index],
        17 => _fallCounter[index],
        18 => _groundState[index],
        19 => _bubbleTimer[index],
        20 => _leftFlag[index],
        21 => _rightFlag[index],
        22 => _attackTimer[index],
        _ => 0,
    };
}
