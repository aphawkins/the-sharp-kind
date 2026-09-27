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

public class BaseView8BitTests
{
    private static readonly Palette s_palette = new(new Dictionary<string, FastColor>(StringComparer.Ordinal)
    {
        ["White"] = new(0xFFFFFFFFu),
        ["Yellow"] = new(0xFFFFFF00u),
    });

    // The countdown's right edge is the fourth column, so its digits fill columns two and three.
    [Fact]
    public void HyperspaceCountdownEndsAtTheFourthColumn()
    {
        RecordingGraphics graphics = new(320, 256);
        Mock<IViewSurface> surface = new();
        surface.SetupGet(x => x.Graphics).Returns(graphics);
        surface.SetupGet(x => x.Layout).Returns(new ViewLayout(320, 256, 0, 1));
        surface.SetupGet(x => x.Palette).Returns(s_palette);
        BaseView8Bit baseView = new(surface.Object);

        baseView.DrawHyperspaceCountdown(15);

        (Vector2 position, string text, _, _) = Assert.Single(graphics.RightTexts);
        Assert.Equal("15", text);
        Assert.Equal(EightBitGrid.Of(surface.Object).Column(4), position.X);
    }
}
