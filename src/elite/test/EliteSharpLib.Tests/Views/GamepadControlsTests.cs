// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Views;
using SharpKind.Fakes.Input;
using SharpKind.Input;

namespace EliteSharpLib.Tests.Views;

// What is left here now the flight controls are bindings: the "press fire
// to continue" prompts. Everything else moved to ControlMapTests.
public class GamepadControlsTests
{
    [Fact]
    public void AnIdlePadAnswersNoPrompt()
        => Assert.False(GamepadControls.WasFirePressed(new FakeGamepad()));

    // A stick the bindings file has no entry for should still get past the
    // title screen, so any button answers.
    [Theory]
    [InlineData(GamepadButton.A)]
    [InlineData(GamepadButton.B)]
    [InlineData(GamepadButton.X)]
    [InlineData(GamepadButton.Y)]
    [InlineData(GamepadButton.Start)]
    public void AnyButtonAnswersThePrompt(GamepadButton button)
    {
        FakeGamepad pad = new();
        pad.ButtonDown(button);

        Assert.True(GamepadControls.WasFirePressed(pad));
    }

    // One-shot: held, it would skip straight through the screen behind it.
    [Fact]
    public void AHeldButtonAnswersOnlyOnce()
    {
        FakeGamepad pad = new();
        pad.ButtonDown(GamepadButton.A);

        Assert.True(GamepadControls.WasFirePressed(pad));
        Assert.False(GamepadControls.WasFirePressed(pad));
    }

    // Every button is read rather than stopping at the first to answer, or
    // an unread press would fire on the next update.
    [Fact]
    public void NoPressIsLeftInHandForTheNextUpdate()
    {
        FakeGamepad pad = new();
        pad.ButtonDown(GamepadButton.A);
        pad.ButtonDown(GamepadButton.Start);

        Assert.True(GamepadControls.WasFirePressed(pad));
        Assert.False(GamepadControls.WasFirePressed(pad));
    }
}
