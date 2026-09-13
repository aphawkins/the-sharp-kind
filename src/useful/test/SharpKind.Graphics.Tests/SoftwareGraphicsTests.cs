// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Collections;
using System.Numerics;
using System.Reflection;
using Moq;
using SharpKind.Assets;

namespace SharpKind.Graphics.Tests;

public class SoftwareGraphicsTests
{
    // Without the guard a null set is taken, every non-text draw works, and
    // the first line of text throws where the renderer is rather than where
    // the set should have come from.
    [Fact]
    public void RefusesToBeBuiltWithoutAnyWayToRasteriseText()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        AssetSet assets = AssetSet.Load(moqAssetLocator.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => SoftwareGraphics.Create(5, 5, _ => { }, assets, null!));
    }

    [Fact]
    public void Clear()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawPixel(new(2, 2), BaseColors.White);
        graphics.Clear();
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp) => Assert.Equal(BaseColors.Black, bmp.GetPixel(2, 2));
    }

    [Fact]
    public void DrawCircleFilledInBounds()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawCircleFilled(new(2, 2), 2, BaseColors.White);
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.White, bmp.GetPixel(0, 2));
            Assert.Equal(BaseColors.White, bmp.GetPixel(4, 2));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 0));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 4));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 2));
        }
    }

    [Theory]
    [InlineData(2, 2, 3, 0xFFFFFFFF)]
    [InlineData(2, 2, 9, 0xFFFFFFFF)]
    [InlineData(-9, -9, 3, 0xFF000000)]
    [InlineData(9, 9, 3, 0xFF000000)]
    public void DrawCircleFilledOutOfBounds(float x, float y, float radius, uint centreColor)
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawCircleFilled(new(x, y), radius, BaseColors.White);
        graphics.ScreenUpdate();

        // Assert
        void DoAssert(FastBitmap bmp)
        {
            for (int screenY = 0; screenY < 5; screenY++)
            {
                for (int screenX = 0; screenX < 5; screenX++)
                {
                    Assert.Equal(FastColor.FromUInt32(centreColor), bmp.GetPixel(screenX, screenY));
                }
            }
        }
    }

    [Fact]
    public void DrawCircleInBounds()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawCircle(new(2, 2), 2, BaseColors.White);
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.White, bmp.GetPixel(0, 2));
            Assert.Equal(BaseColors.White, bmp.GetPixel(4, 2));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 0));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 4));
        }
    }

    [Theory]
    [InlineData(2, 2, 3)]
    [InlineData(2, 2, 9)]
    [InlineData(-9, -9, 3)]
    [InlineData(9, 9, 3)]
    public void DrawCircleOutOfBounds(float x, float y, float radius)
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawCircle(new(x, y), radius, BaseColors.White);
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp)
        {
            for (int screenY = 0; screenY < 5; screenY++)
            {
                for (int screenX = 0; screenX < 5; screenX++)
                {
                    Assert.Equal(BaseColors.Black, bmp.GetPixel(screenX, screenY));
                }
            }
        }
    }

    [Fact]
    public void DrawCirclePartialInBounds()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawCircle(new(0, 0), 4, BaseColors.White);
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.White, bmp.GetPixel(0, 4));
            Assert.Equal(BaseColors.White, bmp.GetPixel(4, 0));
        }
    }

    [Theory]
    [InlineData(2, 2, "2x2redtopleft.bmp", 0, 0)]
    [InlineData(4, 4, "2x2redtopleft.bmp", 2, 2)]
    public void DrawImage(int width, int height, string filename, int imageX, int imageY)
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets(filename);
        using SoftwareGraphics graphics = SoftwareGraphics.Create(width, height, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawImage("TestImage", new(imageX, imageY));
        graphics.ScreenUpdate();

        // Assert
        void DoAssert(FastBitmap bmp) => Assert.Equal(BaseColors.Red, bmp.GetPixel(imageX, imageY));
    }

    [Theory]
    [InlineData("2x2redtopleft.bmp", 2, 2)]
    public void DrawImageOutOfBounds(string filename, int imageWidth, int imageHeight)
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets(filename);
        using SoftwareGraphics graphics = SoftwareGraphics.Create(imageWidth, imageHeight, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawImage("TestImage", new(1, 1));
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp) => Assert.Equal(BaseColors.Red, bmp.GetPixel(1, 1));
    }

    [Theory]
    [InlineData("2x2redtopleft.bmp")]
    public void DrawImageTransparent(string filename)
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets(filename);
        using SoftwareGraphics graphics = SoftwareGraphics.Create(2, 2, DoAssert, moqAssetLocator.Object);
        graphics.DrawPixel(new(1, 1), BaseColors.White);

        // Act
        graphics.DrawImage("TestImage", new(0, 0));
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Red, bmp.GetPixel(0, 0));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(0, 1));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(1, 0));
            Assert.Equal(BaseColors.White, bmp.GetPixel(1, 1));
        }
    }

    [Theory]
    [InlineData(0, 0, 4, 4)]
    [InlineData(0, 4, 4, 0)]
    [InlineData(0, 0, 4, 0)]
    [InlineData(0, 0, 0, 4)]
    public void DrawLineInBounds(float startX, float startY, float endX, float endY)
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawLine(new(startX, startY), new(endX, endY), BaseColors.White);
        graphics.ScreenUpdate();

        // Assert
        void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.White, bmp.GetPixel((int)startX, (int)startY));
            Assert.Equal(BaseColors.White, bmp.GetPixel((int)endX, (int)endY));
        }
    }

    // FillDepth occludes without drawing, and an edge tagged with the same
    // surface id draws over it however the depths compare - which is what
    // lets a wireframe outline survive the very face it bounds while a
    // genuinely hidden edge does not.
    [Fact]
    public void FillDepthOccludesEverythingButItsOwnSurface()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);
        Vector2[] quad = [new(0, 0), new(5, 0), new(5, 5), new(0, 5)];

        // Act
        graphics.ClearDepth();
        graphics.FillDepth(quad, [10f, 10f, 10f, 10f], surfaceId: 7);
        graphics.DrawLineDepth(new(0, 1), new(4, 1), 20f, 20f, BaseColors.White, surfaceId: 0);
        graphics.DrawLineDepth(new(0, 2), new(4, 2), 20f, 20f, BaseColors.White, surfaceId: 9);
        graphics.DrawLineDepth(new(0, 3), new(4, 3), 20f, 20f, BaseColors.White, surfaceId: 7);
        graphics.ScreenUpdate();

        // Assert: the surface drew nothing itself; the line behind it
        // belonging to no surface is hidden; so is one belonging to a
        // different surface; only the one belonging to that surface draws.
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Black, bmp.GetPixel(2, 0));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(2, 1));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(2, 2));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 3));
        }
    }

    // A detail line lying on a face turned away from the camera must be
    // hidden by the nearer surface, not drawn straight through it - the
    // whole reason a line needs a depth test at all.
    [Fact]
    public void DrawLineDepthIsHiddenByNearerGeometry()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);
        Vector2[] nearQuad = [new(0, 0), new(5, 0), new(5, 5), new(0, 5)];

        // Act: a near surface covering the view, then a line behind it and
        // an identical one in front of it.
        graphics.ClearDepth();
        graphics.DrawPolygonFilledDepth(nearQuad, [10f, 10f, 10f, 10f], BaseColors.Red);
        graphics.DrawLineDepth(new(0, 1), new(4, 1), 20f, 20f, BaseColors.White, surfaceId: 0);
        graphics.DrawLineDepth(new(0, 3), new(4, 3), 5f, 5f, BaseColors.White, surfaceId: 0);
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Red, bmp.GetPixel(2, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 3));
        }
    }

    // Depth is interpolated along the line, so one that passes from behind
    // a surface to in front of it is hidden over exactly the part behind.
    [Fact]
    public void DrawLineDepthInterpolatesAlongTheLine()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);
        Vector2[] quad = [new(0, 0), new(5, 0), new(5, 5), new(0, 5)];

        // Act
        graphics.ClearDepth();
        graphics.DrawPolygonFilledDepth(quad, [10f, 10f, 10f, 10f], BaseColors.Red);
        graphics.DrawLineDepth(new(0, 2), new(4, 2), 20f, 5f, BaseColors.White, surfaceId: 0);
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Red, bmp.GetPixel(0, 2));
            Assert.Equal(BaseColors.White, bmp.GetPixel(4, 2));
        }
    }

    [Theory]
    [InlineData(0, 0, 5, 5)]
    [InlineData(-1, -1, 5, 5)]
    [InlineData(-1, 5, 5, -1)]
    [InlineData(-1, 2, 5, 2)]
    public void DrawLineOutOfBounds(float startX, float startY, float endX, float endY)
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawLine(new(startX, startY), new(endX, endY), BaseColors.White);
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp) => Assert.Equal(BaseColors.White, bmp.GetPixel(2, 2));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    [InlineData(4, 4)]
    public void DrawPixelInBounds(float x, float y)
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawPixel(new(x, y), BaseColors.White);
        graphics.ScreenUpdate();

        // Assert
        void DoAssert(FastBitmap bmp) => Assert.Equal(BaseColors.White, bmp.GetPixel((int)x, (int)y));
    }

    // A translucent draw mixes with what is already on the screen rather
    // than replacing it, so a half-alpha white over black lands grey and a
    // fully transparent draw leaves the screen alone.
    [Fact]
    public void DrawPixelBlendsATranslucentColourWithTheScreen()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawPixel(new(1, 1), new FastColor(0x80FFFFFF));
        graphics.DrawPixel(new(2, 2), BaseColors.White);
        graphics.DrawPixel(new(2, 2), new FastColor(0x00FF0000));
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(new FastColor(0xFF808080), bmp.GetPixel(1, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 2));
        }
    }

    // Filled polygons go through the same pixel store, so a translucent face
    // shows what it covers.
    [Fact]
    public void DrawPolygonFilledBlendsATranslucentFace()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        Vector2[] tri = [new(1, 1), new(3, 1), new(2, 3)];

        // Act
        graphics.DrawPolygonFilled(tri, BaseColors.White);
        graphics.DrawPolygonFilled(tri, new FastColor(0x80000000));
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp) => Assert.Equal(new FastColor(0xFF7F7F7F), bmp.GetPixel(2, 2));
    }

    [Theory]
    [InlineData(-9, -9)]
    [InlineData(9, 9)]
    public void DrawPixelOutOfBounds(float x, float y)
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, (_) => { }, moqAssetLocator.Object);

        // Act
        graphics.DrawPixel(new(x, y), BaseColors.White);
        graphics.ScreenUpdate();

        // Assert
    }

    [Theory]
    [InlineData("2x2redtopleft.bmp", 2, 2)]
    public void LoadImage(string filename, int width, int height)
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets(filename);
        using SoftwareGraphics graphics = SoftwareGraphics.Create(width, height, (_) => { }, moqAssetLocator.Object);

        // Act
        graphics.ScreenUpdate();

        // Assert
    }

    [Fact]
    public void PropertiesSetAndDimensions()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();

        // Act
        using SoftwareGraphics graphics = SoftwareGraphics.Create(3, 4, (_) => { }, moqAssetLocator.Object);

        // Assert initial state
        Assert.Equal(3f, graphics.ScreenWidth);
        Assert.Equal(4f, graphics.ScreenHeight);
    }

    [Fact]
    public void DrawImageCentrePlacesImageAtCalculatedX()
    {
        // Arrange: use known 2x2 image
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets("2x2redtopleft.bmp");
        using SoftwareGraphics graphics = SoftwareGraphics.Create(4, 4, DoAssert, moqAssetLocator.Object);

        // Act: centre X should be (4 - 2) / 2 = 1
        graphics.DrawImageCentre("TestImage", 1);
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp) => Assert.Equal(BaseColors.Red, bmp.GetPixel(1, 1));
    }

    [Fact]
    public void DrawPolygonDrawsEdges()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        Vector2[] square = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];

        // Act
        graphics.DrawPolygon(square, BaseColors.White);
        graphics.ScreenUpdate();

        // Assert: corners drawn
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.White, bmp.GetPixel(0, 0));
            Assert.Equal(BaseColors.White, bmp.GetPixel(4, 0));
            Assert.Equal(BaseColors.White, bmp.GetPixel(4, 4));
            Assert.Equal(BaseColors.White, bmp.GetPixel(0, 4));
        }
    }

    [Fact]
    public void DrawPolygonFilledFillsTriangle()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        Vector2[] tri = [new(1, 1), new(3, 1), new(2, 3)];

        // Act
        graphics.DrawPolygonFilled(tri, BaseColors.White);
        graphics.ScreenUpdate();

        // Assert: interior pixel set
        static void DoAssert(FastBitmap bmp) => Assert.Equal(BaseColors.White, bmp.GetPixel(2, 2));
    }

    [Fact]
    public void DrawRectangleBehaviour()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(6, 6, DoAssert, moqAssetLocator.Object);

        // Act - outline
        graphics.DrawRectangle(new(1, 1), 3, 3, BaseColors.White);
        graphics.ScreenUpdate();

        // Assert outline
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Black, bmp.GetPixel(1, 0));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(3, 0));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(0, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(1, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(3, 1));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(4, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(1, 2));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(2, 2));
            Assert.Equal(BaseColors.White, bmp.GetPixel(3, 2));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(0, 3));
            Assert.Equal(BaseColors.White, bmp.GetPixel(1, 3));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 3));
            Assert.Equal(BaseColors.White, bmp.GetPixel(3, 3));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(4, 3));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(1, 4));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(3, 4));
        }
    }

    [Fact]
    public void DrawRectangleFilledBehaviour()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(6, 6, DoAssert, moqAssetLocator.Object);

        // Act - filled
        using SoftwareGraphics graphics2 = SoftwareGraphics.Create(6, 6, DoAssert, moqAssetLocator.Object);
        graphics2.DrawRectangleFilled(new(1, 1), 3, 3, BaseColors.White);
        graphics2.ScreenUpdate();

        // Assert - interior should be white
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Black, bmp.GetPixel(1, 0));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(3, 0));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(0, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(1, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(3, 1));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(4, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(1, 2));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 2));
            Assert.Equal(BaseColors.White, bmp.GetPixel(3, 2));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(0, 3));
            Assert.Equal(BaseColors.White, bmp.GetPixel(1, 3));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 3));
            Assert.Equal(BaseColors.White, bmp.GetPixel(3, 3));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(4, 3));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(1, 4));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(3, 4));
        }
    }

    [Fact]
    public void DrawRectangleFilledClampsToScreenHeightOnNonSquareScreen()
    {
        // Arrange - a screen wider than it is tall would previously clamp Y
        // against ScreenWidth, indexing past the bitmap and throwing.
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(10, 4, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawRectangleFilled(new(2, 1), 20, 20, BaseColors.White);
        graphics.ScreenUpdate();

        // Assert - clamped to the last valid row/column, not out of bounds
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Black, bmp.GetPixel(9, 0));
            Assert.Equal(BaseColors.White, bmp.GetPixel(9, 3));
        }
    }

    [Fact]
    public void DrawRectangleClampsToScreenHeightOnNonSquareScreen()
    {
        // Arrange - same as above but for the outline path.
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(10, 4, DoAssert, moqAssetLocator.Object);

        // Act
        graphics.DrawRectangle(new(2, 1), 20, 20, BaseColors.White);
        graphics.ScreenUpdate();

        // Assert - clamped to the last valid row/column, not out of bounds
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Black, bmp.GetPixel(9, 0));
            Assert.Equal(BaseColors.White, bmp.GetPixel(9, 3));
        }
    }

    [Fact]
    public void DrawTextWhitespaceNoChange()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Preserve a pixel
        graphics.DrawPixel(new(2, 2), BaseColors.White);

        // Act - whitespace should do nothing
        graphics.DrawTextLeft(new(0, 0), "   ", "TestFont", BaseColors.White);
        graphics.DrawTextCentre(0, " ", "TestFont", BaseColors.White);
        graphics.DrawTextRight(new(0, 0), Environment.NewLine, "TestFont", BaseColors.White);
        graphics.ScreenUpdate();

        // Assert preserved
        static void DoAssert(FastBitmap bmp) => Assert.Equal(BaseColors.White, bmp.GetPixel(2, 2));
    }

    [Fact]
    public void DrawTriangleNoThrowAndDraws()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        Vector2 a = new(1, 1);
        Vector2 b = new(3, 1);
        Vector2 c = new(2, 3);

        // Act - outline
        graphics.DrawTriangle(a, b, c, BaseColors.White);
        graphics.ScreenUpdate();

        // Assert some outline points exist
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.White, bmp.GetPixel(1, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(3, 1));
        }
    }

    [Fact]
    public void DrawTriangleFilledNoThrowAndDraws()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        Vector2 a = new(1, 1);
        Vector2 b = new(3, 1);
        Vector2 c = new(2, 3);

        // Act - filled
        graphics.DrawTriangleFilled(a, b, c, BaseColors.White);
        graphics.ScreenUpdate();

        static void DoAssert(FastBitmap bmp) => Assert.Equal(BaseColors.White, bmp.GetPixel(2, 2));
    }

    [Fact]
    public void SetClipRegionNoThrow()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, (_) => { }, moqAssetLocator.Object);

        // Act / Assert - should not throw
        graphics.SetClipRegion(new Vector2(1, 1), 2, 2);
    }

    [Fact]
    public void SetClipRegionRestrictsPixelWritesToRegion()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act - clip to the region [1,1]-[3,3), then draw both inside and outside it
        graphics.SetClipRegion(new Vector2(1, 1), 2, 2);
        graphics.DrawPixel(new(0, 0), BaseColors.White);
        graphics.DrawPixel(new(2, 2), BaseColors.White);
        graphics.ScreenUpdate();

        // Assert
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Black, bmp.GetPixel(0, 0));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 2));
        }
    }

    [Fact]
    public void SetClipRegionBackToFullScreenRestoresUnclippedDrawing()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act - narrow the clip, then restore it to the whole screen before drawing
        graphics.SetClipRegion(new Vector2(1, 1), 2, 2);
        graphics.SetClipRegion(new Vector2(0, 0), 5, 5);
        graphics.DrawPixel(new(0, 0), BaseColors.White);
        graphics.ScreenUpdate();

        // Assert - the earlier narrow clip no longer applies
        static void DoAssert(FastBitmap bmp) => Assert.Equal(BaseColors.White, bmp.GetPixel(0, 0));
    }

    [Fact]
    public void SetClipRegionRestrictsRectangleFilledToRegion()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act - clip to the region [1,1]-[3,3), then fill a rectangle spanning the whole screen
        graphics.SetClipRegion(new Vector2(1, 1), 2, 2);
        graphics.DrawRectangleFilled(new(0, 0), 5, 5, BaseColors.White);
        graphics.ScreenUpdate();

        // Assert - only the clip region got painted
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Black, bmp.GetPixel(0, 0));
            Assert.Equal(BaseColors.White, bmp.GetPixel(1, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 2));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(3, 3));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(4, 4));
        }
    }

    [Fact]
    public void SetClipRegionRestrictsTriangleFilledToRegion()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act - clip to the region [1,1]-[3,3), then fill a triangle covering the whole screen
        graphics.SetClipRegion(new Vector2(1, 1), 2, 2);
        graphics.DrawTriangleFilled(new(0, 0), new(8, 0), new(0, 8), BaseColors.White);
        graphics.ScreenUpdate();

        // Assert - only the clip region got painted
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Black, bmp.GetPixel(0, 0));
            Assert.Equal(BaseColors.White, bmp.GetPixel(1, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 2));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(3, 3));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(0, 4));
        }
    }

    [Fact]
    public void SetClipRegionRestrictsPolygonFilledDepthToRegion()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act - clip to the region [1,1]-[3,3), then fill a depth-tested quad
        // covering the whole screen
        graphics.SetClipRegion(new Vector2(1, 1), 2, 2);
        graphics.ClearDepth();
        graphics.DrawPolygonFilledDepth(
            [new(0, 0), new(5, 0), new(5, 5), new(0, 5)],
            [1f, 1f, 1f, 1f],
            BaseColors.White);
        graphics.ScreenUpdate();

        // Assert - only the clip region got painted
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Black, bmp.GetPixel(0, 0));
            Assert.Equal(BaseColors.White, bmp.GetPixel(1, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 2));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(3, 3));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(4, 4));
        }
    }

    [Fact]
    public void SetClipRegionRestrictsLineToRegion()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, DoAssert, moqAssetLocator.Object);

        // Act - clip to the region [1,1]-[3,3), then draw a diagonal crossing
        // the whole screen, entering and leaving the region
        graphics.SetClipRegion(new Vector2(1, 1), 2, 2);
        graphics.DrawLine(new(0, 0), new(4, 4), BaseColors.White);
        graphics.ScreenUpdate();

        // Assert - the line keeps the pixels it would have had, minus those
        // outside the region
        static void DoAssert(FastBitmap bmp)
        {
            Assert.Equal(BaseColors.Black, bmp.GetPixel(0, 0));
            Assert.Equal(BaseColors.White, bmp.GetPixel(1, 1));
            Assert.Equal(BaseColors.White, bmp.GetPixel(2, 2));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(3, 3));
            Assert.Equal(BaseColors.Black, bmp.GetPixel(4, 4));
        }
    }

    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, (_) => { }, moqAssetLocator.Object);

        // Act
        graphics.Dispose();
        graphics.Dispose(); // second call should be safe

        // Assert - no exception thrown (implicit)
    }

    [Fact]
    public void SaveScreenWritesTheCurrentBackBufferIndependentlyOfScreenUpdate()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(5, 5, _ => { }, moqAssetLocator.Object);
        graphics.DrawPixel(new(2, 2), BaseColors.White);

        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bmp");
        try
        {
            // Act
            graphics.SaveScreen(path);

            // Assert - readable back without ScreenUpdate ever being called
            FastBitmap read = BitmapReader.Read(path);
            Assert.Equal(BaseColors.White, read.GetPixel(2, 2));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GenerateTextBitmapCachesAndEvictsLeastRecentlyUsed()
    {
        // Arrange
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssetsWithFont();
        using SoftwareGraphics graphics = SoftwareGraphics.Create(640, 64, (_) => { }, moqAssetLocator.Object);
        const int capacity = 256;

        // Act - draw more distinct strings than the cache can hold, then
        // redraw the very first one (the least-recently-used entry, so it
        // should have been evicted by now).
        for (int i = 0; i < capacity + 10; i++)
        {
            graphics.DrawTextLeft(new(0, 0), $"Text{i}", "TestFont", BaseColors.White);
        }

        graphics.DrawTextLeft(new(0, 0), "Text0", "TestFont", BaseColors.White);
        graphics.ScreenUpdate();

        // Assert - the cache never grew past its capacity
        Assert.Equal(capacity, GetTextCacheCount(graphics));
    }

    private static int GetTextCacheCount(SoftwareGraphics graphics)
    {
        FieldInfo field = typeof(SoftwareGraphics).GetField("_textCache", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_textCache field not found");
        object value = field.GetValue(graphics) ?? throw new InvalidOperationException("_textCache was null");
        return ((ICollection)value).Count;
    }

    private static string GraphicsFilename(string filename)
        => Path.Combine("golden", filename);

    private static Mock<IAssetLocator> ArrangeAssets(string filename = "")
    {
        Mock<IAssetLocator> moqAssetLocator = new();
        moqAssetLocator.SetupGet(x => x.Rendition).Returns("Test");
        moqAssetLocator.SetupGet(x => x.Colours).Returns(new AssetColourLimits());

        if (string.IsNullOrEmpty(filename))
        {
            moqAssetLocator.Setup(x => x.ImagePaths)
                .Returns(new Dictionary<string, string>());
        }
        else
        {
            moqAssetLocator.Setup(x => x.ImagePaths)
                .Returns(new Dictionary<string, string>() { { "TestImage", GraphicsFilename(filename) } });
        }

        moqAssetLocator.Setup(x => x.FontBitmaps)
            .Returns(new Dictionary<string, BitmapFontAsset>());

        moqAssetLocator.Setup(x => x.FontFons)
            .Returns(new Dictionary<string, FonFontAsset>());

        return moqAssetLocator;
    }

    private static Mock<IAssetLocator> ArrangeAssetsWithFont()
    {
        Mock<IAssetLocator> moqAssetLocator = ArrangeAssets();

        moqAssetLocator.Setup(x => x.FontBitmaps).Returns(
            new Dictionary<string, BitmapFontAsset> { { "TestFont", SixteenBitFont(GraphicsFilename("font1.bmp")) } });

        return moqAssetLocator;
    }

    // The committed 16-bit small sheet: 18x17 cells - the widest glyph plus
    // its marker column, the glyph height plus its marker row - 16 columns
    // filling the sheet exactly, and magenta-delimited variable widths.
    private static BitmapFontAsset SixteenBitFont(string path) => new(
        path,
        new BitmapFontEntry
        {
            File = path,
            CellWidth = 18,
            CellHeight = 17,
            Columns = 16,
            IsProportional = true,
        });
}
