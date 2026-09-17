// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Input;

namespace SharpKind.Fakes.Input;

// Keys and modifiers are tracked apart, as SoftwareKeyboard tracks them, so a press and its modifier can each be read once.
public sealed class FakeKeyboard : IKeyboard, IKeyboardSink
{
    private readonly HashSet<ConsoleKey> _pressedKeys = [];
    private (ConsoleKey Key, ConsoleModifiers Modifiers)? _last;
    private ConsoleModifiers _pressedModifiers = ConsoleModifiers.None;

    public bool Close { get; set; }

    public void ClearPressed()
    {
        _pressedKeys.Clear();
        _pressedModifiers = ConsoleModifiers.None;
    }

    // One-shot, like the real SoftwareKeyboard: a non-consuming fake would let two handlers act on one press.
    public bool IsPressed(ConsoleKey key) => key != ConsoleKey.None && _pressedKeys.Remove(key);

    public bool IsPressed(ConsoleModifiers modifiers)
    {
        if (!IsHeld(modifiers))
        {
            return false;
        }

        _pressedModifiers &= ~modifiers;
        return true;
    }

    public bool IsHeld(ConsoleKey key) => key != ConsoleKey.None && _pressedKeys.Contains(key);

    public bool IsHeld(ConsoleModifiers modifiers)
        => modifiers != ConsoleModifiers.None && _pressedModifiers.HasFlag(modifiers);

    public void KeyDown(ConsoleKey key, ConsoleModifiers modifiers)
    {
        if (key != ConsoleKey.None)
        {
            _ = _pressedKeys.Add(key);
        }

        _pressedModifiers |= modifiers;
        _last = (key, modifiers);
    }

    public void KeyUp(ConsoleKey key, ConsoleModifiers modifiers)
    {
        _ = _pressedKeys.Remove(key);
        _pressedModifiers &= ~modifiers;
    }

    public (ConsoleKey Key, ConsoleModifiers Modifiers) LastPressed()
        => _last ?? (default(ConsoleKey), default(ConsoleModifiers));

    public void Poll()
    {
        // No-op for the fake. Real implementations may update internal state here.
    }

    public void SetClose(bool value) => Close = value;
}
