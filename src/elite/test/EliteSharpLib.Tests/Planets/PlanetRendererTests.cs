// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using EliteSharp.Abstractions.Views.Planets;
using EliteSharp.Renditions.SixteenBit;
using Moq;
using SharpKind;
using SharpKind.Assets.Palettes;
using SharpKind.Graphics;

namespace EliteSharpLib.Tests.Planets;

// What each style draws, taken off a real rendition. The renderer is handed screen terms
// directly here rather than through a planet, so a radius is a radius.
public sealed class PlanetRendererTests
{
    // Comfortably above the radius the outlined style stops drawing detail
    // at, which is 6 of the original's units.
    private const float Radius = 100;

    // A real palette: the fake answers every name with the same colour, which would make land and sea come out uniform.
    private static readonly Palette s_palette = new(new Dictionary<string, FastColor>(StringComparer.Ordinal)
    {
        ["White"] = new(0xFFFFFFFFu),
        ["Green"] = new(0xFF00FF00u),
        ["Blue"] = new(0xFF0000FFu),
        ["LightBlue"] = new(0xFF8080FFu),
        ["LightGreen"] = new(0xFF80FF80u),
        ["Purple"] = new(0xFF800080u),
        ["DarkBlue"] = new(0xFF000080u),
        ["LighterGrey"] = new(0xFFEEEEEEu),
        ["Orange"] = new(0xFFFF7700u),
        ["LightOrange"] = new(0xFFFFBB77u),
        ["DarkOrange"] = new(0xFFEE6622u),
        ["Lilac"] = new(0xFFEEAAEEu),
    });

    [Fact]
    public void SolidDrawsAFilledDisc()
    {
        // Arrange
        Mock<IGraphics> graphics = new();

        // Act
        Renderer(graphics, PlanetStyle.Solid).Draw(View());

        // Assert
        graphics.Verify(x => x.DrawCircleFilled(
            It.IsAny<Vector2>(),
            It.IsAny<float>(),
            It.Is<FastColor>(c => c == s_palette["Green"])));
    }

    [Fact]
    public void WireframeDrawsTheOutline()
    {
        // Arrange
        Mock<IGraphics> graphics = new();

        // Act
        Renderer(graphics, PlanetStyle.Wireframe).Draw(View());

        // Assert
        graphics.Verify(x => x.DrawCircle(It.IsAny<Vector2>(), It.IsAny<float>(), It.IsAny<FastColor>()));
    }

    [Fact]
    public void WireframeDrawsAnEquatorAndMeridian()
    {
        // Arrange
        Mock<IGraphics> graphics = new();

        // Act
        Renderer(graphics, PlanetStyle.Wireframe).Draw(View(Matrix4x4.Identity));

        // Assert: two half ellipses of eight segments each
        graphics.Verify(
            x => x.DrawLine(It.IsAny<Vector2>(), It.IsAny<Vector2>(), It.IsAny<FastColor>()),
            Times.Exactly(16));
    }

    [Fact]
    public void WireframeDrawsACraterFacingUs()
    {
        // Arrange: roofv_z is positive, so the crater faces us.
        Mock<IGraphics> graphics = new();
        Matrix4x4 orientation = Matrix4x4.Identity;
        orientation.M22 = 0;
        orientation.M23 = 1;
        orientation.M32 = -1;
        orientation.M33 = 0;

        // Act
        Renderer(graphics, PlanetStyle.Wireframe, hasCrater: true).Draw(View(orientation));

        // Assert: one full ellipse of sixteen segments
        graphics.Verify(
            x => x.DrawLine(It.IsAny<Vector2>(), It.IsAny<Vector2>(), It.IsAny<FastColor>()),
            Times.Exactly(16));
    }

    [Fact]
    public void WireframeHidesACraterOnTheFarSide()
    {
        // Arrange: roofv_z is negative, so the crater is round the back.
        Mock<IGraphics> graphics = new();
        Matrix4x4 orientation = Matrix4x4.Identity;
        orientation.M22 = 0;
        orientation.M23 = -1;
        orientation.M32 = 1;
        orientation.M33 = 0;

        // Act
        Renderer(graphics, PlanetStyle.Wireframe, hasCrater: true).Draw(View(orientation));

        // Assert
        graphics.Verify(
            x => x.DrawLine(It.IsAny<Vector2>(), It.IsAny<Vector2>(), It.IsAny<FastColor>()),
            Times.Never);
    }

    // Scaled by UnitScale, not a pixel count, so a rendition can draw at any resolution and still stop detailing at the same size.
    [Fact]
    public void WireframeDrawsNoDetailBelowTheDetailRadius()
    {
        // Arrange: a radius of 5 against a threshold of 6, at unit scale.
        Mock<IGraphics> graphics = new();

        // Act
        Renderer(graphics, PlanetStyle.Wireframe)
            .Draw(new(new(256, 256), 5, Matrix4x4.Identity, 1));

        // Assert: the outline is still drawn, but nothing on the surface.
        graphics.Verify(x => x.DrawCircle(It.IsAny<Vector2>(), It.IsAny<float>(), It.IsAny<FastColor>()));
        graphics.Verify(
            x => x.DrawLine(It.IsAny<Vector2>(), It.IsAny<Vector2>(), It.IsAny<FastColor>()),
            Times.Never);
    }

    [Theory]
    [InlineData(PlanetStyle.Striped)]
    [InlineData(PlanetStyle.Fractal)]
    public void SurfacedStylesPaintTheSphere(PlanetStyle style)
    {
        // Arrange
        Mock<IGraphics> graphics = new();

        // Act
        Renderer(graphics, style).Draw(View());

        // Assert
        graphics.Verify(x => x.DrawPixel(It.IsAny<Vector2>(), It.IsAny<FastColor>()));
    }

    // Really a test that the renderer takes the seed it's given rather than rolling its own.
    [Fact]
    public void FractalIsTheSameEveryTimeForOneSeed()
        => Assert.Equal(FractalPixels(seed: 12345), FractalPixels(seed: 12345));

    [Fact]
    public void FractalDiffersBetweenSeeds()
        => Assert.NotEqual(FractalPixels(seed: 12345), FractalPixels(seed: 54321));

    private static PlanetView View() => View(Matrix4x4.Identity);

    private static PlanetView View(Matrix4x4 orientation) => new(new(256, 256), Radius, orientation, 1);

    // Built through the rendition, exactly as the game builds it.
    private static IPlanetRenderer Renderer(Mock<IGraphics> graphics, PlanetStyle style, bool hasCrater = false, int seed = 1)
    {
        Mock<IViewSurface> surface = new();
        surface.SetupGet(x => x.Graphics).Returns(graphics.Object);
        surface.SetupGet(x => x.Layout).Returns(new ViewLayout(512, 512, 128, 2));
        surface.SetupGet(x => x.Palette).Returns(s_palette);

        Random random = new(seed);

        return new SixteenBitRendition()
            .CreatePlanetRenderer(surface.Object, new(style, hasCrater, new RandomSource(random)));
    }

    // Every pixel the generated surface paints, in order, which is what
    // "the same planet" means from the outside.
    private static List<(Vector2 Position, FastColor Colour)> FractalPixels(int seed)
    {
        List<(Vector2, FastColor)> painted = [];
        Mock<IGraphics> graphics = new();
        graphics
            .Setup(x => x.DrawPixel(It.IsAny<Vector2>(), It.IsAny<FastColor>()))
            .Callback<Vector2, FastColor>((position, colour) => painted.Add((position, colour)));

        Renderer(graphics, PlanetStyle.Fractal, seed: seed).Draw(View());

        Assert.NotEmpty(painted);
        return painted;
    }
}
