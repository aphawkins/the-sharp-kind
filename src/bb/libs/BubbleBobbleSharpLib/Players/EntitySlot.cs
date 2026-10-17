// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Players;

// One of the eight entities: a player or an enemy. Each field is the byte the 6502 keeps in the table named in the
// conversion notes. The members return refs so a slot's field reads and writes in place.
internal sealed class EntitySlot
{
    private byte _state;
    private byte _x;
    private byte _y;
    private byte _frame;
    private byte _animationTimer;
    private byte _bubbleTimer;
    private byte _riseCounter;
    private byte _fallCounter;
    private byte _leftFlag;
    private byte _rightFlag;
    private byte _groundState;
    private byte _colour;
    private byte _mode;
    private byte _holdTimer;
    private byte _flashTimer;
    private byte _heading;
    private byte _spriteBase;
    private byte _turnTimer;
    private byte _turnInterval;
    private byte _frameCount;
    private byte _frameMask;
    private byte _leapFlag;
    private byte _climbFlag;
    private byte _attackTimer;

    internal ref byte State => ref _state;

    internal ref byte X => ref _x;

    internal ref byte Y => ref _y;

    internal ref byte Frame => ref _frame;

    internal ref byte AnimationTimer => ref _animationTimer;

    internal ref byte BubbleTimer => ref _bubbleTimer;

    internal ref byte RiseCounter => ref _riseCounter;

    internal ref byte FallCounter => ref _fallCounter;

    internal ref byte LeftFlag => ref _leftFlag;

    internal ref byte RightFlag => ref _rightFlag;

    internal ref byte GroundState => ref _groundState;

    internal ref byte Colour => ref _colour;

    internal ref byte Mode => ref _mode;

    internal ref byte HoldTimer => ref _holdTimer;

    internal ref byte FlashTimer => ref _flashTimer;

    internal ref byte Heading => ref _heading;

    internal ref byte SpriteBase => ref _spriteBase;

    internal ref byte TurnTimer => ref _turnTimer;

    internal ref byte TurnInterval => ref _turnInterval;

    internal ref byte FrameCount => ref _frameCount;

    internal ref byte FrameMask => ref _frameMask;

    internal ref byte LeapFlag => ref _leapFlag;

    internal ref byte ClimbFlag => ref _climbFlag;

    internal ref byte AttackTimer => ref _attackTimer;
}
