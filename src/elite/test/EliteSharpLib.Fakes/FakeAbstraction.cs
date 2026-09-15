// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Abstraction;
using SharpKind.Audio;
using SharpKind.Fakes.Audio;
using SharpKind.Fakes.Input;
using SharpKind.Graphics;
using SharpKind.Graphics.Fakes;
using SharpKind.Input;

namespace EliteSharpLib.Fakes;

internal sealed class FakeAbstraction(IGraphics graphics, ScreenLayout layout) : IAbstraction
{
    // A plain square screen for the callers that only need one to exist:
    // EliteDraw derives its layout from these, and a 0x0 fake screen
    // produces negative ranges that blow up star generation. A caller that
    // cares what the frame looks like passes the rendition's own size
    // instead, as HeadlessGameHarness does.
    public FakeAbstraction()
        : this(new RecordingGraphics(512, 512), new(512, 512))
    {
    }

    public IGraphics Graphics { get; } = graphics;

    public ScreenLayout Layout { get; } = layout;

    public ISound Sound { get; } = new FakeSound();

    public IKeyboard Keyboard { get; } = new FakeKeyboard();

    public IGamepad Gamepad { get; } = new FakeGamepad();
}
