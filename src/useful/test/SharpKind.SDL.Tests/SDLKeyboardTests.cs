// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Fakes.Input;
using SharpKind.Input;

namespace SharpKind.SDL.Tests;

public class SDLKeyboardTests
{
    [Fact]
    public void InitialStateDefaults()
    {
        FakeInput fakeInput = new();
        SoftwareKeyboard kb = new(fakeInput);

        Assert.False(kb.IsPressed(ConsoleKey.A));
        Assert.False(kb.IsPressed(ConsoleModifiers.Shift));
        (ConsoleKey key, ConsoleModifiers modifiers) = kb.LastPressed();
        Assert.Equal(ConsoleKey.None, key);
        Assert.Equal(ConsoleModifiers.None, modifiers);
    }

    // A modifier that only decides what another key means must be readable without being taken away.
    [Fact]
    public void IsHeldReportsAModifierWithoutConsumingIt()
    {
        FakeInput fakeInput = new();
        SoftwareKeyboard kb = new(fakeInput);

        kb.KeyDown(ConsoleKey.None, ConsoleModifiers.Control);
        kb.KeyDown(ConsoleKey.M, ConsoleModifiers.None);

        Assert.True(kb.IsHeld(ConsoleModifiers.Control));
        Assert.True(kb.IsHeld(ConsoleModifiers.Control));
        Assert.False(kb.IsHeld(ConsoleModifiers.Shift));

        // and the key it modifies is still there for its handler
        Assert.True(kb.IsPressed(ConsoleKey.M));

        kb.KeyUp(ConsoleKey.None, ConsoleModifiers.Control);
        Assert.False(kb.IsHeld(ConsoleModifiers.Control));
    }

    [Fact]
    public void KeyDownLastPressedReturnsAndClears()
    {
        FakeInput fakeInput = new();
        SoftwareKeyboard kb = new(fakeInput);

        kb.KeyDown(ConsoleKey.A, ConsoleModifiers.Shift);

        (ConsoleKey key, ConsoleModifiers modifiers) = kb.LastPressed();
        Assert.Equal(ConsoleKey.A, key);
        Assert.Equal(ConsoleModifiers.Shift, modifiers);

        // subsequent LastPressed is cleared
        (ConsoleKey key1, ConsoleModifiers modifiers1) = kb.LastPressed();
        Assert.Equal(ConsoleKey.None, key1);
        Assert.Equal(ConsoleModifiers.None, modifiers1);
    }

    [Fact]
    public void KeyDownIsPressedKeyConsumedThenNotPressed()
    {
        FakeInput fakeInput = new();
        SoftwareKeyboard kb = new(fakeInput);

        kb.KeyDown(ConsoleKey.B, ConsoleModifiers.None);

        // first call consumes the last-key and returns true
        Assert.True(kb.IsPressed(ConsoleKey.B));

        // second call returns false (consumed / cleared)
        Assert.False(kb.IsPressed(ConsoleKey.B));
    }

    [Fact]
    public void KeyDownIsPressedModifierConsumedThenNotPressed()
    {
        FakeInput fakeInput = new();
        SoftwareKeyboard kb = new(fakeInput);

        kb.KeyDown(ConsoleKey.C, ConsoleModifiers.Control);

        // first call consumes the last-modifier and returns true
        Assert.True(kb.IsPressed(ConsoleModifiers.Control));

        // second call returns false (consumed / cleared)
        Assert.False(kb.IsPressed(ConsoleModifiers.Control));
    }

    // Regression: driving/movement controls need continuous physical state, not IsPressed's one-shot consumption.
    [Fact]
    public void IsHeldStaysTrueAcrossRepeatedPollsWhileKeyRemainsDown()
    {
        FakeInput fakeInput = new();
        SoftwareKeyboard kb = new(fakeInput);

        kb.KeyDown(ConsoleKey.UpArrow, ConsoleModifiers.None);

        Assert.True(kb.IsHeld(ConsoleKey.UpArrow));
        Assert.True(kb.IsHeld(ConsoleKey.UpArrow));
        Assert.True(kb.IsHeld(ConsoleKey.UpArrow));

        kb.KeyUp(ConsoleKey.UpArrow, ConsoleModifiers.None);

        Assert.False(kb.IsHeld(ConsoleKey.UpArrow));
    }

    // Regression: holding a second key must not affect an already-held key's IsHeld state.
    [Fact]
    public void IsHeldTracksMultipleKeysIndependently()
    {
        FakeInput fakeInput = new();
        SoftwareKeyboard kb = new(fakeInput);

        kb.KeyDown(ConsoleKey.UpArrow, ConsoleModifiers.None);
        Assert.True(kb.IsHeld(ConsoleKey.UpArrow));

        // pressing (and polling) a second key must not clear the first
        kb.KeyDown(ConsoleKey.LeftArrow, ConsoleModifiers.None);
        Assert.True(kb.IsHeld(ConsoleKey.LeftArrow));
        Assert.True(kb.IsHeld(ConsoleKey.UpArrow));

        kb.KeyUp(ConsoleKey.LeftArrow, ConsoleModifiers.None);
        Assert.False(kb.IsHeld(ConsoleKey.LeftArrow));
        Assert.True(kb.IsHeld(ConsoleKey.UpArrow));
    }

    [Fact]
    public void KeyUpRemovesPressed()
    {
        FakeInput fakeInput = new();
        SoftwareKeyboard kb = new(fakeInput);

        kb.KeyDown(ConsoleKey.D, ConsoleModifiers.Alt);

        // remove it
        kb.KeyUp(ConsoleKey.D, ConsoleModifiers.Alt);

        // Should not be reported pressed (last-key was set by KeyDown, but IsPressed will check pressed map too)
        Assert.False(kb.IsPressed(ConsoleKey.D));
        Assert.False(kb.IsPressed(ConsoleModifiers.Alt));
    }

    [Fact]
    public void ClearPressedResetsAllState()
    {
        FakeInput fakeInput = new();
        SoftwareKeyboard kb = new(fakeInput);

        kb.KeyDown(ConsoleKey.E, ConsoleModifiers.Shift | ConsoleModifiers.Control);
        Assert.True(kb.IsPressed(ConsoleKey.E) || kb.IsPressed(ConsoleModifiers.Shift) || kb.IsPressed(ConsoleModifiers.Control));

        kb.ClearPressed();

        Assert.False(kb.IsPressed(ConsoleKey.E));
        Assert.False(kb.IsPressed(ConsoleModifiers.Shift));
        Assert.False(kb.IsPressed(ConsoleModifiers.Control));
        (ConsoleKey key, ConsoleModifiers modifiers) = kb.LastPressed();
        Assert.Equal(ConsoleKey.None, key);
        Assert.Equal(ConsoleModifiers.None, modifiers);
    }
}
