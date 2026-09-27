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

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    public void TheRingsHaveTheRenditionsSides(int sides)
    {
        ViewLayout layout = new(320, 256, 56, 1);
        RecordingGraphics graphics = new(320, 256);
        FakeEliteDraw draw = new() { Layout = layout, Graphics = graphics, RingSides = sides };
        BreakPattern pattern = new(draw);

        pattern.Reset();
        for (int i = 0; i < 20; i++)
        {
            pattern.Update(1);
            pattern.Draw();
        }

        Assert.Empty(graphics.Circles);
        Assert.NotEmpty(graphics.Polygons);

        foreach ((Vector2[] points, _) in graphics.Polygons)
        {
            Assert.Equal(sides, points.Length);

            // Every corner is on the ring's circle, so the polygon is regular.
            float radius = Vector2.Distance(layout.ViewportCentre, points[0]);
            Assert.All(points, p => Assert.Equal(radius, Vector2.Distance(layout.ViewportCentre, p), 3));

            // A corner, not an edge, at the right, bottom, left and top.
            foreach (Vector2 d in (Vector2[])[Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX, -Vector2.UnitY])
            {
                Assert.Contains(points, p => Vector2.Distance(p, layout.ViewportCentre + (radius * d)) < 0.001f);
            }
        }

        Vector2 corner = new(layout.ViewportRight, layout.ViewportBottom);
        float widest = graphics.Polygons.Max(p => Vector2.Distance(layout.ViewportCentre, p.Points[0]));
        Assert.Equal(Vector2.Distance(layout.ViewportCentre, corner), widest, 3);
    }
}
