// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Abstraction;
using SharpKind.Audio;
using SharpKind.Fakes.Audio;
using SharpKind.Fakes.Input;
using SharpKind.Graphics;
using SharpKind.Graphics.Fakes;
using SharpKind.Input;

namespace BubbleBobbleSharpLib.Fakes;

public sealed class FakeAbstraction(IGraphics graphics, ScreenLayout layout) : IAbstraction
{
    public FakeAbstraction()
        : this(new RecordingGraphics(), new(0, 0))
    {
    }

    public IGraphics Graphics { get; } = graphics;

    public ScreenLayout Layout { get; } = layout;

    public ISound Sound { get; } = new FakeSound();

    public IKeyboard Keyboard { get; } = new FakeKeyboard();

    public IGamepad Gamepad { get; } = new FakeGamepad();
}
