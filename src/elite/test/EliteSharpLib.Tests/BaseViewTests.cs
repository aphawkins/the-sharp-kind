// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Graphics;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;
using SharpKind.Graphics.Fakes;
using SharpKind.Graphics.Rendering;

namespace EliteSharpLib.Tests;

public class BaseViewTests
{
    [Fact]
    public void DrawTextPrettyHardBreaksWordLongerThanLineWidth()
    {
        BaseView16Bit baseView = new(Draw(out _));

        // No break point exists in the text; guards against underflowing past index 0.
        string unbreakableText = new('a', 200);

        Exception? exception = Record.Exception(() => baseView.DrawTextPretty(new(0, 0), 64, unbreakableText));

        Assert.Null(exception);
    }

    // Unchosen means the original's focal length of exactly one screen height, not 53 degrees reconstructed (0.3% off).
    [Fact]
    public void FocusWithNoChosenFieldOfViewIsTheScreenHeight()
    {
        EliteDraw draw = Draw(out GameState gameState);

        Assert.Null(gameState.Config.Engine.FieldOfView);
        Assert.Equal(512, draw.Layout.ScreenHeight);
        Assert.Equal(draw.Layout.ScreenHeight, draw.Focus);
    }

    [Theory]
    [InlineData(65)]
    [InlineData(90)]
    [InlineData(105)]
    public void AWiderFieldOfViewShortensTheFocalLength(int fieldOfView)
    {
        EliteDraw draw = Draw(out GameState gameState);
        float classic = draw.Focus;

        gameState.Config.Engine.FieldOfView = fieldOfView;

        Assert.True(draw.Focus < classic);
    }

    [Fact]
    public void ANarrowerFieldOfViewLengthensTheFocalLength()
    {
        EliteDraw draw = Draw(out GameState gameState);
        float classic = draw.Focus;

        gameState.Config.Engine.FieldOfView = 40;

        Assert.True(draw.Focus > classic);
    }

    private static EliteDraw Draw(out GameState gameState)
    {
        // Focus derives from screen height; a zero-sized fake would make Focus assertions vacuously true.
        RecordingGraphics graphics = new(640, 512);
        gameState = new(new ScreenManager<Screen, IScreenController>(new FakeKeyboard()), TestMissions.Registry());
        ZBufferRenderer shipRenderer = new(graphics);
        RenderRandom rng = new(new Random(0));
        return new EliteDraw(gameState, graphics, graphics.Layout, TestAssets.Locator(), new SixteenBitRendition(), shipRenderer, rng);
    }
}
