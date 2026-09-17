// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics.Tests;

public class BitmapReaderTests
{
    [Theory]
    [InlineData("2x2redtopleft.bmp", 2, 2)]
    public void LoadBitmapOrientation(string filename, int width, int height)
    {
        // Arrange
        string path = Path.Combine("golden", filename);

        // Act
        FastBitmap bitmap = BitmapReader.Read(path);

        // Assert
        Assert.Equal(width, bitmap.Width);
        Assert.Equal(height, bitmap.Height);
        Assert.Equal(32, bitmap.BitsPerPixel);
        Assert.Equal(BaseColors.Red, bitmap.GetPixel(0, 0));
        Assert.Equal(BaseColors.TransparentBlack, bitmap.GetPixel(0, 1));
        Assert.Equal(BaseColors.TransparentBlack, bitmap.GetPixel(1, 0));
        Assert.Equal(BaseColors.TransparentBlack, bitmap.GetPixel(1, 1));
    }

    [Fact]
    public void Reads24BppRowsIncludingTheirPaddingToAFourByteBoundary()
    {
        // Arrange: 3 pixels of BGR is 9 bytes, padded out to a 12-byte row.
        byte[] pixels = [0x00, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00];
        using TempImageFile file = TempImageFile.From(BmpBuilder.Build(3, 1, 24, pixels));

        // Act
        FastBitmap bitmap = BitmapReader.Read(file.Path);

        // Assert
        Assert.Equal(3, bitmap.Width);
        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 0));
        Assert.Equal(FastColor.FromUInt32(0xFF00FF00), bitmap.GetPixel(1, 0));
        Assert.Equal(FastColor.FromUInt32(0xFF0000FF), bitmap.GetPixel(2, 0));
    }

    [Theory]
    [InlineData(8, new byte[] { 0x00, 0x01, 0x02, 0x00 })]
    [InlineData(4, new byte[] { 0x01, 0x20, 0x00, 0x00 })]
    public void ReadsPalettisedPixelsAsOpaqueColours(short bitsPerPixel, byte[] pixels)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(pixels);
        uint[] palette = [0xFF0000, 0x00FF00, 0x0000FF];
        using TempImageFile file = TempImageFile.From(BmpBuilder.Build(3, 1, bitsPerPixel, pixels, palette));

        // Act
        FastBitmap bitmap = BitmapReader.Read(file.Path);

        // Assert
        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 0));
        Assert.Equal(FastColor.FromUInt32(0xFF00FF00), bitmap.GetPixel(1, 0));
        Assert.Equal(FastColor.FromUInt32(0xFF0000FF), bitmap.GetPixel(2, 0));
    }

    [Fact]
    public void ReadsOneBitPixelsMostSignificantBitFirst()
    {
        // Arrange: 0xA0 is 1, 0, 1 across the first three bits.
        uint[] palette = [0x000000, 0xFFFFFF];
        using TempImageFile file = TempImageFile.From(BmpBuilder.Build(3, 1, 1, [0xA0, 0x00, 0x00, 0x00], palette));

        // Act
        FastBitmap bitmap = BitmapReader.Read(file.Path);

        // Assert
        Assert.Equal(FastColor.FromUInt32(0xFFFFFFFF), bitmap.GetPixel(0, 0));
        Assert.Equal(FastColor.FromUInt32(0xFF000000), bitmap.GetPixel(1, 0));
        Assert.Equal(FastColor.FromUInt32(0xFFFFFFFF), bitmap.GetPixel(2, 0));
    }

    [Theory]
    [InlineData(2, 0xFFFFFFFFu)]
    [InlineData(-2, 0xFFFF0000u)]
    public void HonoursRowOrderFromTheSignOfTheHeight(int signedHeight, uint expectedTopLeft)
    {
        // Positive height means bottom-up, so red lands at the bottom; negative means top-down, leaving red at the top.
        byte[] pixels = [0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        using TempImageFile file = TempImageFile.From(BmpBuilder.Build(1, signedHeight, 32, pixels));

        // Act
        FastBitmap bitmap = BitmapReader.Read(file.Path);

        // Assert
        Assert.Equal(2, bitmap.Height);
        Assert.Equal(FastColor.FromUInt32(expectedTopLeft), bitmap.GetPixel(0, 0));
    }

    [Fact]
    public void ReadsPixelDataFromTheOffsetTheHeaderDeclares()
    {
        // Arrange: BmpBuilder puts pixel data at 54, not the 150 every
        // committed asset happens to use.
        using TempImageFile file = TempImageFile.From(BmpBuilder.Build(1, 1, 32, [0x00, 0x00, 0xFF, 0xFF]));

        // Act
        FastBitmap bitmap = BitmapReader.Read(file.Path);

        // Assert
        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 0));
    }

    [Fact]
    public void ThrowsOnAnUnsupportedBitDepth()
    {
        // Arrange
        using TempImageFile file = TempImageFile.From(BmpBuilder.Build(1, 1, 16, [0x00, 0x00]));

        // Act / Assert
        Assert.Throws<SharpKindException>(() => BitmapReader.Read(file.Path));
    }

    [Fact]
    public void ThrowsOnACompressedBitmap()
    {
        // Arrange: BI_RLE8.
        using TempImageFile file = TempImageFile.From(BmpBuilder.Build(1, 1, 8, [0x00, 0x00, 0x00, 0x00], [0xFF0000], 1));

        // Act / Assert
        Assert.Throws<SharpKindException>(() => BitmapReader.Read(file.Path));
    }

    [Fact]
    public void ThrowsWhenPixelDataIsTruncated()
    {
        // Arrange: a 4x4 image with a single row of pixels.
        using TempImageFile file = TempImageFile.From(BmpBuilder.Build(4, 4, 32, new byte[16]));

        // Act / Assert
        Assert.Throws<SharpKindException>(() => BitmapReader.Read(file.Path));
    }
}
