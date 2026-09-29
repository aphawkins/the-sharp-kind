// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharp.Renditions.EightBit;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// House chrome: 56 lines above the C64's 200, which the reference does not have.
public sealed class BannerTests
{
    [Fact]
    public void ThePlayfieldStartsBelowTheBanner()
    {
        BbViewLayout layout = new(320, 256);

        Assert.Equal(56f, layout.BannerHeight);
        Assert.Equal(new Vector2(0, 56), layout.Screen.Cell(0, 0));
        Assert.Equal(new Vector2(0, 56), layout.Playfield.Cell(0, 0));
    }

    [Fact]
    public void AScreenOfTwoHundredLinesHasNoBanner() => Assert.Equal(0f, new BbViewLayout(320, 200).BannerHeight);

    [Fact]
    public void TheBannerIsDrawnAtTheTopOfTheScreen()
    {
        RecordingGraphics graphics = new(320, 256);

        new EightBitRendition().CreateBannerView(new TestSurface(graphics, "Banner")).Draw(new BannerModel(56));

        (string image, Vector2 position, Vector2 size, _, _) = Assert.Single(graphics.ImageParts);

        Assert.Equal("Banner", image);
        Assert.Equal(Vector2.Zero, position);
        Assert.Equal(new Vector2(320, 56), size);
    }

    [Fact]
    public void NothingIsDrawnWithNoRoomForIt()
    {
        RecordingGraphics graphics = new(320, 200);

        new EightBitRendition().CreateBannerView(new TestSurface(graphics, "Banner")).Draw(new BannerModel(0));

        Assert.Empty(graphics.ImageParts);
    }
}
