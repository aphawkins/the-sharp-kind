// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes.Input;
using SharpKind.Input;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// What matters about this class is the byte it hands on, because every routine Phase 4 brings over
// shifts that byte rather than asking it a question. So the tests are all about the layout: which
// bit each direction owns, which way round a pushed bit reads, and that one player's stick is not
// wired to the other's.
public sealed class InputTests
{
    [Fact]
    public void ReadsOneByteForEachPlayer()
    {
        Input input = Build(out _, out _);

        Assert.Equal(PlayerTable.Capacity, input.Read().Length);
    }

    // Nothing pushed is every line high, which is the reading the reference tests against: a
    // direction is taken when its bit is clear, so an idle port must not offer a single clear bit.
    [Fact]
    public void ReadsEveryLineHighWhenNothingIsTouched()
    {
        Input input = Build(out _, out _);

        ReadOnlySpan<byte> ports = input.Read();

        Assert.Equal(Input.Idle, ports[0]);
        Assert.Equal(Input.Idle, ports[1]);
    }

    // The bit layout itself, one direction at a time. A switch pulls its line low, so a pushed
    // direction clears its own bit and leaves the other four alone.
    [Theory]
    [InlineData(ConsoleKey.UpArrow, Input.Up)]
    [InlineData(ConsoleKey.DownArrow, Input.Down)]
    [InlineData(ConsoleKey.LeftArrow, Input.Left)]
    [InlineData(ConsoleKey.RightArrow, Input.Right)]
    [InlineData(ConsoleKey.Spacebar, Input.Fire)]
    public void ClearsOnlyItsOwnBitWhilePlayerOneHoldsADirection(ConsoleKey key, byte bit)
    {
        Input input = Build(out FakeKeyboard keyboard, out _);
        keyboard.KeyDown(key, ConsoleModifiers.None);

        Assert.Equal((byte)(Input.Idle & ~bit), input.Read()[0]);
    }

    [Theory]
    [InlineData(ConsoleKey.W, Input.Up)]
    [InlineData(ConsoleKey.S, Input.Down)]
    [InlineData(ConsoleKey.A, Input.Left)]
    [InlineData(ConsoleKey.D, Input.Right)]
    [InlineData(ConsoleKey.Q, Input.Fire)]
    public void ClearsOnlyItsOwnBitWhilePlayerTwoHoldsADirection(ConsoleKey key, byte bit)
    {
        Input input = Build(out FakeKeyboard keyboard, out _);
        keyboard.KeyDown(key, ConsoleModifiers.None);

        Assert.Equal((byte)(Input.Idle & ~bit), input.Read()[1]);
    }

    // A stick can be pushed and the button held at once, and the port says so in one byte.
    [Fact]
    public void ClearsEveryBitThatIsBeingHeldAtOnce()
    {
        Input input = Build(out FakeKeyboard keyboard, out _);
        keyboard.KeyDown(ConsoleKey.RightArrow, ConsoleModifiers.None);
        keyboard.KeyDown(ConsoleKey.UpArrow, ConsoleModifiers.None);
        keyboard.KeyDown(ConsoleKey.Spacebar, ConsoleModifiers.None);

        Assert.Equal((byte)(Input.Idle & ~(Input.Right | Input.Up | Input.Fire)), input.Read()[0]);
    }

    // Two ports, not one shared byte, or a second player would move whenever the first did.
    [Fact]
    public void KeepsTheTwoPlayersPortsApart()
    {
        Input input = Build(out FakeKeyboard keyboard, out _);
        keyboard.KeyDown(ConsoleKey.LeftArrow, ConsoleModifiers.None);
        keyboard.KeyDown(ConsoleKey.D, ConsoleModifiers.None);

        ReadOnlySpan<byte> ports = input.Read();

        Assert.Equal((byte)(Input.Idle & ~Input.Left), ports[0]);
        Assert.Equal((byte)(Input.Idle & ~Input.Right), ports[1]);
    }

    // The 6502 reads the ports afresh every frame and sees a stick still being leaned on, so a held
    // key has to keep reading as held rather than being consumed by the first frame that sees it.
    [Fact]
    public void KeepsReadingAKeyThatIsStillHeld()
    {
        Input input = Build(out FakeKeyboard keyboard, out _);
        keyboard.KeyDown(ConsoleKey.RightArrow, ConsoleModifiers.None);

        _ = input.Read();

        Assert.Equal((byte)(Input.Idle & ~Input.Right), input.Read()[0]);
    }

    [Fact]
    public void LetsGoOfABitWhenTheKeyIsReleased()
    {
        Input input = Build(out FakeKeyboard keyboard, out _);
        keyboard.KeyDown(ConsoleKey.RightArrow, ConsoleModifiers.None);
        _ = input.Read();

        keyboard.KeyUp(ConsoleKey.RightArrow, ConsoleModifiers.None);

        Assert.Equal(Input.Idle, input.Read()[0]);
    }

    [Theory]
    [InlineData(GamepadButton.DPadUp, Input.Up)]
    [InlineData(GamepadButton.DPadDown, Input.Down)]
    [InlineData(GamepadButton.DPadLeft, Input.Left)]
    [InlineData(GamepadButton.DPadRight, Input.Right)]
    [InlineData(GamepadButton.A, Input.Fire)]
    public void ClearsOnlyItsOwnBitWhileThePadHoldsADirection(GamepadButton button, byte bit)
    {
        Input input = Build(out _, out FakeGamepad gamepad);
        gamepad.Connected("Pad");
        gamepad.ButtonDown(button);

        Assert.Equal((byte)(Input.Idle & ~bit), input.Read()[0]);
    }

    // An analog stick has a position where the port has a switch, so half travel is where the switch
    // closes - and anything short of it leaves the port idle.
    [Theory]
    [InlineData(GamepadAxis.LeftX, -1f, Input.Left)]
    [InlineData(GamepadAxis.LeftX, 1f, Input.Right)]
    [InlineData(GamepadAxis.LeftY, -1f, Input.Up)]
    [InlineData(GamepadAxis.LeftY, 1f, Input.Down)]
    public void ClearsADirectionWhenThePadsStickIsPushedFully(GamepadAxis axis, float position, byte bit)
    {
        Input input = Build(out _, out FakeGamepad gamepad);
        gamepad.Connected("Pad");
        gamepad.AxisMoved(axis, position);

        Assert.Equal((byte)(Input.Idle & ~bit), input.Read()[0]);
    }

    [Fact]
    public void LeavesThePortIdleWhileThePadsStickIsBarelyMoved()
    {
        Input input = Build(out _, out FakeGamepad gamepad);
        gamepad.Connected("Pad");
        gamepad.AxisMoved(GamepadAxis.LeftX, 0.25f);

        Assert.Equal(Input.Idle, input.Read()[0]);
    }

    // The pad is player 1's alone, because only one device can be read at a time. Player 2 is on the
    // keys whatever is plugged in.
    [Fact]
    public void LeavesPlayerTwoAloneWhenThePadIsPushed()
    {
        Input input = Build(out _, out FakeGamepad gamepad);
        gamepad.Connected("Pad");
        gamepad.ButtonDown(GamepadButton.DPadLeft);

        Assert.Equal(Input.Idle, input.Read()[1]);
    }

    // Keys and pad drive the same five bits, so holding both is one push, not two.
    [Fact]
    public void ReadsTheKeysAndThePadIntoTheOnePort()
    {
        Input input = Build(out FakeKeyboard keyboard, out FakeGamepad gamepad);
        gamepad.Connected("Pad");
        keyboard.KeyDown(ConsoleKey.LeftArrow, ConsoleModifiers.None);
        gamepad.ButtonDown(GamepadButton.A);

        Assert.Equal((byte)(Input.Idle & ~(Input.Left | Input.Fire)), input.Read()[0]);
    }

    // A stick unplugged mid-game leaves no direction stuck down: with nothing attached the port
    // reads idle whatever the device was last seen holding.
    [Fact]
    public void ReadsEveryLineHighOnceThePadIsUnplugged()
    {
        Input input = Build(out _, out FakeGamepad gamepad);
        gamepad.Connected("Pad");
        gamepad.ButtonDown(GamepadButton.DPadLeft);
        _ = input.Read();

        gamepad.Disconnected();

        Assert.Equal(Input.Idle, input.Read()[0]);
    }

    private static Input Build(out FakeKeyboard keyboard, out FakeGamepad gamepad)
    {
        keyboard = new FakeKeyboard();
        gamepad = new FakeGamepad();
        return new Input(keyboard, gamepad);
    }
}
