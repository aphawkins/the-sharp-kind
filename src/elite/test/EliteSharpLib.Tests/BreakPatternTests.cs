// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Fakes;
using SharpKind.Graphics.Fakes;

namespace EliteSharpLib.Tests;

public class BreakPatternTests
{
    // The two renditions that ship: both viewports are wider than they are
    // tall, which is what the rings have to fit inside.
    public static TheoryData<string, float, float, float, float> Renditions { get; } = new()
    {
        { "16-bit", 640, 512, 128, 2 },
        { "8-bit", 320, 256, 56, 1 },
    };

    [Theory]
    [MemberData(nameof(Renditions))]
    public void EveryRingFitsTheViewport(
        string rendition,
        float screenWidth,
        float screenHeight,
        float consoleHeight,
        float scale)
    {
        ViewLayout layout = new(screenWidth, screenHeight, consoleHeight, scale);
        RecordingGraphics graphics = new(screenWidth, screenHeight);
        FakeEliteDraw draw = new() { Layout = layout, Rendition = rendition, Graphics = graphics };
        BreakPattern pattern = new(draw);

        // One tick per ring, drawing after each: the last drawn frame carries
        // the widest ring the animation ever reaches.
        pattern.Reset();
        for (int i = 0; i < 20; i++)
        {
            pattern.Update(1);
            pattern.Draw();
        }

        Assert.NotEmpty(graphics.Circles);

        foreach ((Vector2 centre, float radius, _) in graphics.Circles)
        {
            Assert.True(radius > 0, $"A ring has radius {radius}.");
            Assert.True(
                centre.Y >= radius && centre.Y + radius <= layout.ViewportHeight,
                $"A ring of radius {radius} runs off a viewport {layout.ViewportHeight} tall.");
        }

        // The widest ring fills the viewport's shorter axis rather than
        // stopping short of it.
        Assert.Equal(layout.ViewportCentre.Y, graphics.Circles.Max(c => c.Radius), 3);
    }
}
