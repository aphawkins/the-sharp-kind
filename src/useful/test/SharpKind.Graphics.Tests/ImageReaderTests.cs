// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics.Tests;

public class ImageReaderTests
{
    [Fact]
    public void ReadsABmpRegardlessOfItsExtension()
    {
        // Arrange: TempImageFile writes everything as .img, so a match here
        // can only have come from the file's magic bytes.
        using TempImageFile file = TempImageFile.From(BmpBuilder.Build(1, 1, 32, [0x00, 0x00, 0xFF, 0xFF]));

        // Act
        FastBitmap bitmap = ImageReader.Read(file.Path);

        // Assert
        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 0));
    }

    [Fact]
    public void ReadsAPngRegardlessOfItsExtension()
    {
        // Arrange
        using TempImageFile file = TempImageFile.From(PngBuilder.Build(1, 1, 8, 2, [0, 0xFF, 0x00, 0x00]));

        // Act
        FastBitmap bitmap = ImageReader.Read(file.Path);

        // Assert
        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 0));
    }

    [Fact]
    public void ReadsATgaRegardlessOfItsExtension()
    {
        // Arrange: TGA has no signature at the start, so this one is
        // recognised by its header being one a TGA could have had.
        using TempImageFile file = TempImageFile.From(
            TgaBuilder.Build(1, 1, 2, 24, [0x00, 0x00, 0xFF]));

        // Act
        FastBitmap bitmap = ImageReader.Read(file.Path);

        // Assert
        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 0));
    }

    [Fact]
    public void StillReadsABmpAsABmpNowThatTgaIsTried()
    {
        // TGA is recognised by a header that could be one rather than by a
        // signature, so the formats that can answer for certain have to keep
        // winning. A BMP decoded as a TGA would not throw - it would come out
        // as nonsense - so this asserts the pixel, not the absence of an
        // exception.
        // A 24bpp row pads out to a 4-byte boundary, hence the trailing byte.
        using TempImageFile file = TempImageFile.From(BmpBuilder.Build(1, 1, 24, [0x00, 0xFF, 0x00, 0x00]));

        FastBitmap bitmap = ImageReader.Read(file.Path);

        Assert.Equal(FastColor.FromUInt32(0xFF00FF00), bitmap.GetPixel(0, 0));
    }

    [Fact]
    public void ReturnsAnEmptyBitmapForAnEmptyFile()
    {
        // Arrange
        using TempImageFile file = TempImageFile.From([]);

        // Act
        FastBitmap bitmap = ImageReader.Read(file.Path);

        // Assert
        Assert.Equal(0, bitmap.Width);
        Assert.Equal(0, bitmap.Height);
    }

    [Fact]
    public void ThrowsOnAnUnrecognisedFormat()
    {
        // Arrange: a GIF header, which nothing here decodes.
        using TempImageFile file = TempImageFile.From([0x47, 0x49, 0x46, 0x38, 0x39, 0x61]);

        // Act / Assert
        Assert.Throws<SharpKindException>(() => ImageReader.Read(file.Path));
    }
}
