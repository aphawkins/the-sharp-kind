// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views.Suns;
using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Graphics;
using EliteSharpLib.Suns;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Views;
using SharpKind;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;
using SharpKind.Graphics;
using SharpKind.Graphics.Rendering;

namespace EliteSharpLib.Tests;

public class WireframeSunTests
{
    private const int ScreenSize = 512;

    // Projects to the centre of the view at the radius the filled suns use: 6291456 / distance, scaled by Focus / 256.
    private const float Distance = 123456;

    private const float ExpectedRadius = 6291456f / Distance * (ScreenSize / 256f);

    [Fact]
    public void DrawFillsADiscOfTheExpectedSize()
    {
        (FastBitmap frame, Vector2 centre, FastColor white) = DrawSun();

        // A filled disc, not an outline: the centre and a point most of the
        // way out to the edge are both lit.
        Assert.Equal(white, frame.GetPixel((int)centre.X, (int)centre.Y));
        Assert.Equal(white, frame.GetPixel((int)(centre.X + (ExpectedRadius * 0.8f)), (int)centre.Y));
        Assert.Equal(white, frame.GetPixel((int)centre.X, (int)(centre.Y - (ExpectedRadius * 0.8f))));

        // ... and it stops at the edge rather than flaring past it, as the
        // solid and gradient suns do.
        Assert.NotEqual(white, frame.GetPixel((int)(centre.X + (ExpectedRadius * 1.2f)), (int)centre.Y));

        // Area within a few percent of a circle's, which an outline or a
        // square would miss by far more.
        int lit = CountPixels(frame, white);
        const float area = MathF.PI * ExpectedRadius * ExpectedRadius;
        Assert.InRange(lit, area * 0.9f, area * 1.1f);
    }

    // The one colour it draws in is white, whatever the palette holds for
    // the filled suns' flare.
    [Fact]
    public void DrawUsesWhiteOnly()
    {
        (FastBitmap frame, _, FastColor white) = DrawSun();

        HashSet<FastColor> colors = [];
        for (int y = 0; y < frame.Height; y++)
        {
            for (int x = 0; x < frame.Width; x++)
            {
                colors.Add(frame.GetPixel(x, y));
            }
        }

        Assert.Equal(2, colors.Count);
        Assert.Contains(white, colors);
    }

    private static (FastBitmap Frame, Vector2 Centre, FastColor White) DrawSun()
    {
        FastBitmap? lastFrame = null;
        using SoftwareGraphics graphics = SoftwareGraphics.Create(ScreenSize, ScreenSize, b => lastFrame = b, TestAssets.Locator());
        GameState gameState = new(new ScreenManager<Screen, IScreenController>(new FakeKeyboard()), TestMissions.Registry());
        RenderRandom rng = new(new Random(0));
        EliteDraw draw = new(
            gameState,
            graphics,
            new(ScreenSize, ScreenSize),
            TestAssets.Locator(),
            new SixteenBitRendition(),
            new ZBufferRenderer(graphics),
            rng);
        Sun sun = new(draw, new SixteenBitRendition().CreateSunRenderer(draw, new(SunStyle.Wireframe, rng)))
        {
            Location = new(0, 0, Distance, 0),
        };

        graphics.Clear();
        sun.Draw();
        graphics.ScreenUpdate();

        Assert.NotNull(lastFrame);
        return (lastFrame, draw.Layout.ViewportCentre, draw.Palette["White"]);
    }

    private static int CountPixels(FastBitmap bitmap, in FastColor color)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) == color)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
