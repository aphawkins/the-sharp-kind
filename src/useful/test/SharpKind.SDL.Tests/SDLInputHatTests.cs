// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SDL;
using SharpKind.Fakes.Input;
using SharpKind.Input;

namespace SharpKind.SDL.Tests;

// SDL sends a hat's whole state on every change, never a separate release, so each direction is compared with what it was.
public class SDLInputHatTests
{
    private const int Device = 1;
    private const byte Centred = (byte)SDL3.SDL_HAT_CENTERED;
    private const byte Up = (byte)SDL3.SDL_HAT_UP;
    private const byte Left = (byte)SDL3.SDL_HAT_LEFT;
    private const byte Corner = Up | Left;

    [Fact]
    public void APushedDirectionGoesDown()
    {
        FakeGamepad pad = new();
        pad.Connected(Device, "hat stick");

        SDLInput.HatDirectionChanged(
            Device, Centred, Up, SDL3.SDL_HAT_UP, GamepadButton.DPadUp, pad);

        Assert.True(pad.IsHeld(GamepadButton.DPadUp));
    }

    // The failure this comparison exists to prevent: a hat returned to
    // centre must release, or the view would keep being selected.
    [Fact]
    public void AReleasedDirectionComesBackUp()
    {
        FakeGamepad pad = new();
        pad.Connected(Device, "hat stick");
        SDLInput.HatDirectionChanged(
            Device, Centred, Up, SDL3.SDL_HAT_UP, GamepadButton.DPadUp, pad);

        SDLInput.HatDirectionChanged(
            Device, Up, Centred, SDL3.SDL_HAT_UP, GamepadButton.DPadUp, pad);

        Assert.False(pad.IsHeld(GamepadButton.DPadUp));
    }

    // A direction that has not changed sends nothing, so a held hat does
    // not re-press on every event the other directions cause.
    [Fact]
    public void AnUnchangedDirectionIsNotPressedAgain()
    {
        FakeGamepad pad = new();
        pad.Connected(Device, "hat stick");
        SDLInput.HatDirectionChanged(
            Device, Centred, Up, SDL3.SDL_HAT_UP, GamepadButton.DPadUp, pad);
        Assert.True(pad.IsPressed(GamepadButton.DPadUp));

        SDLInput.HatDirectionChanged(
            Device, Up, Up, SDL3.SDL_HAT_UP, GamepadButton.DPadUp, pad);

        Assert.False(pad.IsPressed(GamepadButton.DPadUp));
    }

    // A corner is both of its directions at once, which is why the game
    // side picks one view rather than setting two.
    [Fact]
    public void ACornerPressesBothOfItsDirections()
    {
        FakeGamepad pad = new();
        pad.Connected(Device, "hat stick");
        SDLInput.HatDirectionChanged(
            Device, Centred, Corner, SDL3.SDL_HAT_UP, GamepadButton.DPadUp, pad);
        SDLInput.HatDirectionChanged(
            Device, Centred, Corner, SDL3.SDL_HAT_LEFT, GamepadButton.DPadLeft, pad);

        Assert.True(pad.IsHeld(GamepadButton.DPadUp));
        Assert.True(pad.IsHeld(GamepadButton.DPadLeft));
    }

    // Rolling off a corner onto one edge releases only the direction that
    // has gone.
    [Fact]
    public void LeavingACornerReleasesOnlyTheDirectionThatWent()
    {
        FakeGamepad pad = new();
        pad.Connected(Device, "hat stick");
        SDLInput.HatDirectionChanged(
            Device, Centred, Corner, SDL3.SDL_HAT_UP, GamepadButton.DPadUp, pad);
        SDLInput.HatDirectionChanged(
            Device, Centred, Corner, SDL3.SDL_HAT_LEFT, GamepadButton.DPadLeft, pad);

        SDLInput.HatDirectionChanged(Device, Corner, Up, SDL3.SDL_HAT_UP, GamepadButton.DPadUp, pad);
        SDLInput.HatDirectionChanged(Device, Corner, Up, SDL3.SDL_HAT_LEFT, GamepadButton.DPadLeft, pad);

        Assert.True(pad.IsHeld(GamepadButton.DPadUp));
        Assert.False(pad.IsHeld(GamepadButton.DPadLeft));
    }
}
