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
    // Both viewports are wider than tall; the widest ring reaches their corners.
    public static TheoryData<string, float, float, float, float> Renditions { get; } = new()
    {
        { "16-bit", 640, 512, 128, 2 },
        { "8-bit", 320, 256, 56, 1 },
    };

    [Theory]
    [MemberData(nameof(Renditions))]
    public void TheWidestRingReachesTheCorners(
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

        // Last drawn frame carries the widest ring the animation reaches.
        pattern.Reset();
        for (int i = 0; i < 20; i++)
        {
            pattern.Update(1);
            pattern.Draw();
        }

        Assert.NotEmpty(graphics.Circles);

        foreach ((_, float radius, _) in graphics.Circles)
        {
            Assert.True(radius > 0, $"A ring has radius {radius}.");
        }

        // The widest ring passes through the bottom-right pixel, so it fills the corners.
        Vector2 corner = new(layout.ViewportRight, layout.ViewportBottom);
        float widest = graphics.Circles.Max(c => c.Radius);
        Assert.Equal(Vector2.Distance(layout.ViewportCentre, corner), widest, 3);
    }
}
