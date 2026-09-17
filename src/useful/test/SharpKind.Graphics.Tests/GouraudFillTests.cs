// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using Moq;
using SharpKind.Assets;
using SharpKind.Graphics.Rendering;

namespace SharpKind.Graphics.Tests;

public class GouraudFillTests
{
    private static readonly FastColor s_black = new(255, 0, 0, 0);
    private static readonly FastColor s_white = new(255, 255, 255, 255);

    // A flat-topped triangle, black along the top edge and white at the
    // bottom point, so the shade should climb steadily down the screen.
    [Fact]
    public void ColourIsBlendedDownTheFace()
    {
        FastBitmap? frame = Fill(
            [new(10, 10), new(90, 10), new(50, 90)],
            [100f, 100f, 100f],
            [s_black, s_black, s_white],
            quantiser: null);

        Assert.NotNull(frame);

        // Sampled down the middle, where all three corners contribute.
        int near = frame.GetPixel(50, 20).R;
        int middle = frame.GetPixel(50, 50).R;
        int far = frame.GetPixel(50, 80).R;

        Assert.True(near < middle, $"expected {near} < {middle}");
        Assert.True(middle < far, $"expected {middle} < {far}");
    }

    // Black at one corner, white at the other two: the blend must run left-right as well as top-bottom, unlike a flat fill.
    [Fact]
    public void ColourIsBlendedAcrossAScanline()
    {
        FastBitmap? frame = Fill(
            [new(10, 10), new(90, 10), new(50, 90)],
            [100f, 100f, 100f],
            [s_black, s_white, s_white],
            quantiser: null);

        Assert.NotNull(frame);

        Assert.True(frame.GetPixel(25, 15).R < frame.GetPixel(75, 15).R);
    }

    [Fact]
    public void AFaceWhoseCornersAgreeFillsFlat()
    {
        FastBitmap? frame = Fill(
            [new(10, 10), new(90, 10), new(50, 90)],
            [100f, 100f, 100f],
            [s_white, s_white, s_white],
            quantiser: null);

        Assert.NotNull(frame);

        for (int x = 30; x <= 70; x++)
        {
            Assert.Equal(s_white, frame.GetPixel(x, 40));
        }
    }

    // The blend is a different colour at every pixel, so the quantiser has to
    // be asked at every pixel - not once for the face, as a flat fill can.
    [Fact]
    public void EveryPixelIsQuantised()
    {
        FastBitmap? frame = Fill(
            [new(10, 10), new(90, 10), new(50, 90)],
            [100f, 100f, 100f],
            [s_black, s_black, s_white],
            new ChannelGridQuantiser(1));

        Assert.NotNull(frame);

        // One bit per channel leaves only 0 and 255, so every drawn pixel has
        // to be one or the other - and both must appear, or nothing blended.
        bool sawBlack = false;
        bool sawWhite = false;

        for (int y = 11; y < 89; y++)
        {
            FastColor pixel = frame.GetPixel(50, y);
            Assert.True(pixel.R is 0 or 255, $"unquantised {pixel} at y={y}");
            sawBlack |= pixel.R == 0;
            sawWhite |= pixel.R == 255;
        }

        Assert.True(sawBlack && sawWhite);
    }

    // Depth still decides what draws: the flat path's test is not skipped
    // just because the colour varies.
    [Fact]
    public void TheDepthTestStillApplies()
    {
        Mock<IAssetLocator> assets = new();
        SetupEmptyAssets(assets);

        FastBitmap? frame = null;
        using SoftwareGraphics graphics = SoftwareGraphics.Create(100, 100, f => frame = f, assets.Object);
        graphics.ClearDepth();

        Vector2[] points = [new(10, 10), new(90, 10), new(50, 90)];

        // Nearer, drawn first, then a farther face over the top of it.
        graphics.DrawPolygonFilledDepth(points, [10f, 10f, 10f], [s_white, s_white, s_white], null);
        graphics.DrawPolygonFilledDepth(points, [500f, 500f, 500f], [s_black, s_black, s_black], null);
        graphics.ScreenUpdate();

        Assert.NotNull(frame);
        Assert.Equal(s_white, frame.GetPixel(50, 40));
    }

    private static FastBitmap? Fill(Vector2[] points, float[] depths, FastColor[] colours, IColourQuantiser? quantiser)
    {
        Mock<IAssetLocator> assets = new();
        SetupEmptyAssets(assets);

        FastBitmap? frame = null;
        using SoftwareGraphics graphics = SoftwareGraphics.Create(100, 100, f => frame = f, assets.Object);
        graphics.ClearDepth();
        graphics.DrawPolygonFilledDepth(points, depths, colours, quantiser);
        graphics.ScreenUpdate();

        return frame;
    }

    private static void SetupEmptyAssets(Mock<IAssetLocator> assets)
    {
        assets.SetupGet(x => x.Rendition).Returns("Test");
        assets.SetupGet(x => x.Colours).Returns(new AssetColourLimits());
        assets.Setup(a => a.ImagePaths).Returns(new Dictionary<string, string>());
        assets.Setup(a => a.FontBitmaps).Returns(new Dictionary<string, BitmapFontAsset>());
        assets.Setup(a => a.FontFons).Returns(new Dictionary<string, FonFontAsset>());
    }
}
