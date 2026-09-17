// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using Moq;
using SharpKind.Assets;
using SharpKind.Graphics.Rendering;

namespace SharpKind.Graphics.Tests;

public class TriangleFillTests
{
    [Fact]
    public void SteepEdgeCrossingScanlineDoesNotSpike()
    {
        // A tiny fractional-y edge across an integer scanline used to produce a huge interpolation slope and paint a horizontal spike.
        Mock<IAssetLocator> assets = new();
        SetupEmptyAssets(assets);
        using SoftwareGraphics graphics = SoftwareGraphics.Create(100, 100, DoAssert, assets.Object);

        Vector2 a = new(40, 49.99f);
        Vector2 b = new(42, 50.01f);
        Vector2 c = new(41, 60f);

        graphics.DrawTriangleFilled(a, b, c, BaseColors.White);
        graphics.ScreenUpdate();

        static void DoAssert(FastBitmap bmp)
        {
            // nothing may be drawn outside the triangle's x range (39-43)
            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    if (x is < 39 or > 43)
                    {
                        Assert.Equal(BaseColors.Black, bmp.GetPixel(x, y));
                    }
                }
            }
        }
    }

    [Fact]
    public void FilledPixelsStayWithinVertexBounds()
    {
        // Try a spread of awkward triangles; no pixel may fall outside the
        // triangle's bounding box.
        Mock<IAssetLocator> assets = new();
        SetupEmptyAssets(assets);

        (Vector2 A, Vector2 B, Vector2 C)[] triangles =
        [
            (new(10.7f, 20.99f), new(30.2f, 21.01f), new(20.5f, 80.5f)),
            (new(50, 10), new(50.4f, 90), new(49.6f, 90)), // sliver
            (new(5, 5), new(95, 5.5f), new(5, 6.4f)), // near-horizontal
            (new(80, 30), new(20, 30), new(20, 31)), // flat top
            (new(33.3f, 70f), new(66.6f, 70f), new(50f, 40.2f)), // flat bottom
        ];

        foreach ((Vector2 a, Vector2 b, Vector2 c) in triangles)
        {
            FastBitmap? frame = null;
            using SoftwareGraphics graphics = SoftwareGraphics.Create(100, 100, f => frame = f, assets.Object);
            graphics.DrawTriangleFilled(a, b, c, BaseColors.White);
            graphics.ScreenUpdate();

            Assert.NotNull(frame);
            int minX = (int)MathF.Floor(Math.Min(a.X, Math.Min(b.X, c.X)));
            int maxX = (int)MathF.Ceiling(Math.Max(a.X, Math.Max(b.X, c.X)));
            int minY = (int)MathF.Floor(Math.Min(a.Y, Math.Min(b.Y, c.Y)));
            int maxY = (int)MathF.Ceiling(Math.Max(a.Y, Math.Max(b.Y, c.Y)));

            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    if (x < minX || x > maxX || y < minY || y > maxY)
                    {
                        Assert.Equal(BaseColors.Black, frame.GetPixel(x, y));
                    }
                }
            }
        }
    }

    [Fact]
    public void SolidTriangleIsFilledWithoutHoles()
    {
        Mock<IAssetLocator> assets = new();
        SetupEmptyAssets(assets);
        using SoftwareGraphics graphics = SoftwareGraphics.Create(100, 100, DoAssert, assets.Object);

        graphics.DrawTriangleFilled(new(10, 10), new(90, 10), new(50, 90), BaseColors.White);
        graphics.ScreenUpdate();

        static void DoAssert(FastBitmap bmp)
        {
            // a horizontal band through the middle must be continuous
            for (int x = 30; x <= 70; x++)
            {
                Assert.Equal(BaseColors.White, bmp.GetPixel(x, 40));
            }
        }
    }

    // Resolves a dither's sixteen answers once for the face; every pixel must still hold exactly what the quantiser would have returned.
    [Fact]
    public void DitheredFillMatchesAskingTheQuantiserPerPixel()
    {
        FastColor[] greys =
        [
            new(0xFF000000), new(0xFF333333), new(0xFF666666),
            new(0xFF999999), new(0xFFCCCCCC), new(0xFFFFFFFF),
        ];

        OrderedDitherQuantiser quantiser = new(new PaletteQuantiser(greys));

        // A colour between two palette entries, so the dither actually
        // alternates rather than landing on one entry everywhere.
        FastColor colour = new(0xFF7A7A7A);

        Mock<IAssetLocator> assets = new();
        SetupEmptyAssets(assets);
        using SoftwareGraphics graphics = SoftwareGraphics.Create(100, 100, DoAssert, assets.Object);

        graphics.DrawPolygonFilled([new(20, 20), new(80, 20), new(80, 80), new(20, 80)], colour, quantiser);
        graphics.ScreenUpdate();

        void DoAssert(FastBitmap bmp)
        {
            // Well inside the quad, clear of the edge rule.
            for (int y = 25; y <= 75; y++)
            {
                for (int x = 25; x <= 75; x++)
                {
                    Assert.Equal(quantiser.Quantise(colour, x, y), bmp.GetPixel(x, y));
                }
            }
        }
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
