// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using EliteSharp.Renditions.EightBit;
using Moq;
using SharpKind;
using SharpKind.Assets.Palettes;
using SharpKind.Graphics.Fakes;

namespace EliteSharpLib.Tests.Renditions;

public class GalacticChartView8BitTests
{
    private const float Fuel = 7;

    private static readonly FastColor s_orange = new(0xFFFF8000u);

    private static readonly Palette s_palette = new(new Dictionary<string, FastColor>(StringComparer.Ordinal)
    {
        ["Green"] = new(0xFF00FF00u),
        ["Orange"] = s_orange,
        ["White"] = new(0xFFFFFFFFu),
        ["Yellow"] = new(0xFFFFFF00u),
    });

    private static readonly Vector2 s_docked = new(100, 100);

    // A planet at the edge of the fuel range, straight across or straight down, lies on the ring.
    // A light year is 2.5 units of D and 5 of B, so the chart must plot B at half D's scale.
    [Theory]
    [InlineData(Fuel * 2.5f, 0)]
    [InlineData(0, Fuel * 5)]
    public void TheFuelCirclePassesThroughAPlanetAtTheEdgeOfTheRange(float dx, float dy)
    {
        RecordingGraphics graphics = new(320, 256);
        Vector2 edge = s_docked + new Vector2(dx, dy);

        CreateView(graphics).Draw(new("GALACTIC CHART 1", [], s_docked, Fuel, edge, string.Empty, string.Empty));

        // The cross is drawn centred on the edge planet's screen position.
        (Vector2 start, Vector2 end, _) = graphics.Lines.First(line => line.Colour == s_orange);
        Vector2 planet = (start + end) / 2;
        (Vector2 centre, float radius, _) = Assert.Single(graphics.Circles);

        Assert.Equal(radius, Vector2.Distance(centre, planet), 3);
    }

    private static GalacticChartView8Bit CreateView(RecordingGraphics graphics)
    {
        Mock<IViewSurface> surface = new();
        surface.SetupGet(x => x.Graphics).Returns(graphics);
        surface.SetupGet(x => x.Layout).Returns(new ViewLayout(320, 256, 0, 1));
        surface.SetupGet(x => x.Palette).Returns(s_palette);

        return new GalacticChartView8Bit(surface.Object);
    }
}
