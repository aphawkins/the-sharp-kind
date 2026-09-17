// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using Moq;
using SharpKind.Assets;

namespace SharpKind.Graphics.Tests;

public class TexturedFillTests
{
    [Fact]
    public void TextureMapsAcrossPolygon()
    {
        Mock<IAssetLocator> assets = new();
        SetupEmptyAssets(assets);
        using SoftwareGraphics graphics = SoftwareGraphics.Create(100, 100, DoAssert, assets.Object);
        using FastBitmap texture = new(2, 1);
        texture.SetPixel(0, 0, BaseColors.Red);
        texture.SetPixel(1, 0, BaseColors.Green);

        // an axis-aligned square with u = 0 on the left and 1 on the right
        Vector2[] points = [new(20, 20), new(80, 20), new(80, 80), new(20, 80)];
        Vector2[] textureCoords = [new(0, 0), new(1, 0), new(1, 0), new(0, 0)];

        graphics.DrawPolygonTextured(points, textureCoords, texture);
        graphics.ScreenUpdate();

        static void DoAssert(FastBitmap bmp)
        {
            // Bilinear blends near the u = 0.5 boundary (x = 50), so only assert the quarter-texel bands nearest each edge (x <= 35, x >= 65).
            for (int y = 25; y <= 75; y += 10)
            {
                for (int x = 25; x <= 35; x += 5)
                {
                    Assert.Equal(BaseColors.Red, bmp.GetPixel(x, y));
                }

                for (int x = 65; x <= 75; x += 5)
                {
                    Assert.Equal(BaseColors.Green, bmp.GetPixel(x, y));
                }
            }
        }
    }

    [Fact]
    public void TexturedPixelsStayWithinVertexBounds()
    {
        Mock<IAssetLocator> assets = new();
        SetupEmptyAssets(assets);

        // The same spread of awkward triangles as the flat fill tests; no
        // pixel may fall outside the triangle's bounding box.
        (Vector2 A, Vector2 B, Vector2 C)[] triangles =
        [
            (new(10.7f, 20.99f), new(30.2f, 21.01f), new(20.5f, 80.5f)),
            (new(50, 10), new(50.4f, 90), new(49.6f, 90)), // sliver
            (new(5, 5), new(95, 5.5f), new(5, 6.4f)), // near-horizontal
            (new(80, 30), new(20, 30), new(20, 31)), // flat top
            (new(33.3f, 70f), new(66.6f, 70f), new(50f, 40.2f)), // flat bottom
        ];

        using FastBitmap texture = new(4, 4);
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                texture.SetPixel(x, y, BaseColors.White);
            }
        }

        foreach ((Vector2 a, Vector2 b, Vector2 c) in triangles)
        {
            FastBitmap? frame = null;
            using SoftwareGraphics graphics = SoftwareGraphics.Create(100, 100, f => frame = f, assets.Object);
            graphics.DrawPolygonTextured([a, b, c], [new(0, 0), new(1, 0), new(0.5f, 1)], texture);
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
    public void OutOfRangeTextureCoordinatesClampInsteadOfThrowing()
    {
        Mock<IAssetLocator> assets = new();
        SetupEmptyAssets(assets);
        using SoftwareGraphics graphics = SoftwareGraphics.Create(100, 100, DoAssert, assets.Object);
        using FastBitmap texture = new(2, 1);
        texture.SetPixel(0, 0, BaseColors.Red);
        texture.SetPixel(1, 0, BaseColors.Green);

        Vector2[] points = [new(20, 20), new(80, 20), new(80, 80), new(20, 80)];
        Vector2[] textureCoords = [new(-2, -1), new(3, -1), new(3, 2), new(-2, 2)];

        graphics.DrawPolygonTextured(points, textureCoords, texture);
        graphics.ScreenUpdate();

        static void DoAssert(FastBitmap bmp)
        {
            // coordinates clamp to the edge texels
            Assert.Equal(BaseColors.Red, bmp.GetPixel(25, 50));
            Assert.Equal(BaseColors.Green, bmp.GetPixel(75, 50));
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
