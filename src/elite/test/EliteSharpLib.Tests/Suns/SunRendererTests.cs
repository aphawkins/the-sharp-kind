// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using EliteSharp.Abstractions.Views.Suns;
using EliteSharp.Renditions.SixteenBit;
using Moq;
using SharpKind;
using SharpKind.Assets.Palettes;
using SharpKind.Graphics;

namespace EliteSharpLib.Tests.Suns;

// What each style draws, taken off a real rendition the way the game takes
// it. The renderer is handed screen terms directly, so there is no projection
// in the way of what these say.
public sealed class SunRendererTests
{
    private const float Radius = 100;

    // A real palette rather than the fake one, which answers every name with
    // the same colour - the banded sun would come out flat.
    private static readonly Palette s_palette = new(new Dictionary<string, FastColor>(StringComparer.Ordinal)
    {
        ["White"] = new(0xFFFFFFFFu),
        ["LightYellow"] = new(0xFFFFFFBBu),
        ["LightOrange"] = new(0xFFFFBB77u),
        ["Orange"] = new(0xFFFF7733u),
        ["DarkOrange"] = new(0xFFEE6622u),
    });

    [Fact]
    public void WireframeFillsAPlainDisc()
    {
        // Arrange: the wireframe world's sun is filled, not outlined - an
        // outline alone reads as a planet.
        Mock<IGraphics> graphics = new();

        // Act
        Renderer(graphics, SunStyle.Wireframe).Draw(new(new(256, 256), Radius));

        // Assert
        graphics.Verify(x => x.DrawCircleFilled(
            It.IsAny<Vector2>(),
            It.IsAny<float>(),
            It.Is<FastColor>(c => c == s_palette["White"])));
    }

    [Fact]
    public void SolidDrawsTheDiscAsScanlines()
    {
        // Arrange: it is built line by line rather than as a circle, because
        // each line is stretched past the edge to make the rim flare.
        Mock<IGraphics> graphics = new();

        // Act
        Renderer(graphics, SunStyle.Solid).Draw(new(new(256, 256), Radius));

        // Assert
        graphics.Verify(
            x => x.DrawLine(
                It.IsAny<Vector2>(),
                It.IsAny<Vector2>(),
                It.Is<FastColor>(c => c == s_palette["White"])),
            Times.AtLeastOnce);
    }

    [Fact]
    public void GradientPaintsItsBands()
    {
        // Arrange
        Mock<IGraphics> graphics = new();

        // Act
        Renderer(graphics, SunStyle.Gradient).Draw(new(new(256, 256), Radius));

        // Assert
        graphics.Verify(x => x.DrawPixel(It.IsAny<Vector2>(), It.IsAny<FastColor>()), Times.AtLeastOnce);
    }

    [Fact]
    public void GradientUsesMoreThanOneBand()
    {
        // Arrange: a white core out through the rings to a dithered rim - if
        // the bands collapsed to one colour the sun would be a flat disc.
        HashSet<FastColor> painted = [];
        Mock<IGraphics> graphics = new();
        graphics
            .Setup(x => x.DrawPixel(It.IsAny<Vector2>(), It.IsAny<FastColor>()))
            .Callback<Vector2, FastColor>((_, colour) => painted.Add(colour));

        // Act
        Renderer(graphics, SunStyle.Gradient).Draw(new(new(256, 256), Radius));

        // Assert
        Assert.True(painted.Count > 1);
    }

    // Built through the rendition, exactly as the game builds it.
    private static ISunRenderer Renderer(Mock<IGraphics> graphics, SunStyle style)
    {
        Mock<IViewSurface> surface = new();
        surface.SetupGet(x => x.Graphics).Returns(graphics.Object);
        surface.SetupGet(x => x.Layout).Returns(new ViewLayout(512, 512, 128, 2));
        surface.SetupGet(x => x.Palette).Returns(s_palette);

        Random random = new(0);

        return new SixteenBitRendition().CreateSunRenderer(surface.Object, new(style, new RandomSource(random)));
    }
}
