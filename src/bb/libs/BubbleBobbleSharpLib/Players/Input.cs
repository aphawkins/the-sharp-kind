// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Input;

namespace BubbleBobbleSharpLib.Players;

// $1CBD, joystick-input.s: the first thing the game does with a frame is read both joystick ports
// and keep the two bytes. It writes $7F to port A and $FF to port B to put the ports in the state a
// stick can be read in, then stores what it reads, raw, one byte per player.
//
// Raw is the point. The byte this produces is a CIA port reading, bit for bit, because that is what
// the rest of the port reads: the movement and animation routines shift the byte and branch on the
// carry rather than asking a question about a direction, so a tidier layout here would have to be
// untidied again at every one of them. The bit layout is the contract - see the constants below.
//
// Two things about the C64 do not survive, and neither is a bit the game reads. Bits 5 to 7 of port
// A carry a keyboard matrix row rather than the stick, and are left high here. And the port write is
// gone with the ports: nothing needs putting into a readable state when the reading is a keyboard
// poll.
internal sealed class Input
{
    // The CIA's joystick bits. Each is a switch to ground, so a bit reads 0 while its direction is
    // pushed and 1 while it is not - every test in the reference is written that way round.
    internal const byte Up = 0x01;
    internal const byte Down = 0x02;
    internal const byte Left = 0x04;
    internal const byte Right = 0x08;
    internal const byte Fire = 0x10;

    // What a port reads with nothing touching it: every line pulled high.
    internal const byte Idle = 0xFF;

    // How far a stick has to be pushed before it counts as a direction. A joystick port has no
    // position to report, so an analog stick has to be turned back into the switch the game expects,
    // and half travel is where SharpKind's own control map draws that line.
    private const float AxisThreshold = 0.5f;

    private readonly IKeyboard _keyboard;
    private readonly IGamepad _gamepad;
    private readonly byte[] _ports = [Idle, Idle];

    internal Input(IKeyboard keyboard, IGamepad gamepad)
    {
        ArgumentNullException.ThrowIfNull(keyboard);
        ArgumentNullException.ThrowIfNull(gamepad);

        _keyboard = keyboard;
        _gamepad = gamepad;
    }

    // Player 1's keys. Nothing in the reference chooses these - the C64 had a stick in each port and
    // no keyboard alternative at all - so they are this port's own.
    private static Keys Player1 { get; }
        = new(ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.LeftArrow, ConsoleKey.RightArrow, ConsoleKey.Spacebar);

    // Player 2's, on the other side of the keyboard so that two people can sit at one.
    private static Keys Player2 { get; }
        = new(ConsoleKey.W, ConsoleKey.S, ConsoleKey.A, ConsoleKey.D, ConsoleKey.Q);

    // $1CBD. The two port readings, indexed by player: player 1 is port 2 ($DC00, read first) and
    // player 2 is port 1 ($DC01), which is the pairing the C64 game is played with.
    //
    // A held read rather than a pressed one, throughout. The 6502 reads the ports afresh every frame
    // and sees a held stick held, where IKeyboard.IsPressed would report one key-down and then let go
    // of a key still being leaned on.
    internal ReadOnlySpan<byte> Read()
    {
        // The pad is player 1's alone, because IGamepad answers for one active device at a time: a
        // second stick is attached, but it cannot be read alongside the first.
        _ports[0] = (byte)(Port(Player1) & Pad());
        _ports[1] = Port(Player2);

        return _ports;
    }

    // Clears a bit for every direction a key is holding, leaving the rest high.
    private static byte Clear(byte port, byte bit, bool pushed) => pushed ? (byte)(port & ~bit) : port;

    private byte Port(in Keys keys)
    {
        byte port = Idle;

        port = Clear(port, Up, _keyboard.IsHeld(keys.UpKey));
        port = Clear(port, Down, _keyboard.IsHeld(keys.DownKey));
        port = Clear(port, Left, _keyboard.IsHeld(keys.LeftKey));
        port = Clear(port, Right, _keyboard.IsHeld(keys.RightKey));
        return Clear(port, Fire, _keyboard.IsHeld(keys.FireKey));
    }

    // The same, off whatever pad is attached. The hat and the stick both answer, so a pad with only
    // one of them still plays, and a pad with both has either drive the same bit.
    private byte Pad()
    {
        if (!_gamepad.IsConnected)
        {
            return Idle;
        }

        float x = _gamepad.Axis(GamepadAxis.LeftX);
        float y = _gamepad.Axis(GamepadAxis.LeftY);

        byte port = Idle;

        port = Clear(port, Up, _gamepad.IsHeld(GamepadButton.DPadUp) || y <= -AxisThreshold);
        port = Clear(port, Down, _gamepad.IsHeld(GamepadButton.DPadDown) || y >= AxisThreshold);
        port = Clear(port, Left, _gamepad.IsHeld(GamepadButton.DPadLeft) || x <= -AxisThreshold);
        port = Clear(port, Right, _gamepad.IsHeld(GamepadButton.DPadRight) || x >= AxisThreshold);
        return Clear(port, Fire, _gamepad.IsHeld(GamepadButton.A));
    }

    // One player's five keys, in the order the port's bits run. Named apart from the bits above
    // because a key is not a bit, and the two would otherwise read as the same thing.
    private readonly record struct Keys(
        ConsoleKey UpKey,
        ConsoleKey DownKey,
        ConsoleKey LeftKey,
        ConsoleKey RightKey,
        ConsoleKey FireKey);
}
