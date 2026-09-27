// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Input;

namespace BubbleBobbleSharpLib.Players;

internal sealed class Input
{
    internal const byte Up = 0x01;
    internal const byte Down = 0x02;
    internal const byte Left = 0x04;
    internal const byte Right = 0x08;
    internal const byte Fire = 0x10;

    internal const byte Idle = 0xFF;

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

    private static Keys Player1 { get; }
        = new(ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.LeftArrow, ConsoleKey.RightArrow, ConsoleKey.Spacebar);

    private static Keys Player2 { get; }
        = new(ConsoleKey.W, ConsoleKey.S, ConsoleKey.A, ConsoleKey.D, ConsoleKey.Q);

    internal ReadOnlySpan<byte> Read()
    {
        _ports[0] = (byte)(Port(Player1) & Pad());
        _ports[1] = Port(Player2);

        return _ports;
    }

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

    private readonly record struct Keys(
        ConsoleKey UpKey,
        ConsoleKey DownKey,
        ConsoleKey LeftKey,
        ConsoleKey RightKey,
        ConsoleKey FireKey);
}
