// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics.Tests;

public class TgaReaderTests
{
    private const byte ColourMapped = 1;
    private const byte TrueColour = 2;
    private const byte Greyscale = 3;
    private const byte RunLengthColourMapped = 9;
    private const byte RunLengthTrueColour = 10;

    // Descriptor bit 5: rows top to bottom. Bit 4: each row right to left.
    private const byte TopToBottom = 0x20;
    private const byte RightToLeft = 0x10;

    [Fact]
    public void Decodes24BitTrueColour()
    {
        // Stored blue, green, red.
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(1, 1, TrueColour, 24, [0x00, 0x00, 0xFF]));

        FastBitmap bitmap = TgaReader.Read(file.Path);

        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 0));
    }

    [Fact]
    public void Decodes32BitTrueColourKeepingItsAlpha()
    {
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(1, 1, TrueColour, 32, [0x00, 0x00, 0xFF, 0x80]));

        FastBitmap bitmap = TgaReader.Read(file.Path);

        Assert.Equal(FastColor.FromUInt32(0x80FF0000), bitmap.GetPixel(0, 0));
    }

    [Fact]
    public void Decodes16BitTrueColourAtFullScale()
    {
        // All five bits of red set. Expanding to eight has to reach 0xFF, not 0xF8.
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(1, 1, TrueColour, 16, [0x00, 0x7C]));

        FastBitmap bitmap = TgaReader.Read(file.Path);

        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 0));
    }

    [Fact]
    public void DecodesGreyscaleAsAllThreeChannels()
    {
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(1, 1, Greyscale, 8, [0x40]));

        FastBitmap bitmap = TgaReader.Read(file.Path);

        Assert.Equal(FastColor.FromUInt32(0xFF404040), bitmap.GetPixel(0, 0));
    }

    [Fact]
    public void DecodesAColourMappedImage()
    {
        // Two entries, blue-green-red each: black then red.
        byte[] colourMap = [0x00, 0x00, 0x00, 0x00, 0x00, 0xFF];
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(2, 1, ColourMapped, 8, [0x00, 0x01], colourMap));

        FastBitmap bitmap = TgaReader.Read(file.Path);

        Assert.Equal(FastColor.FromUInt32(0xFF000000), bitmap.GetPixel(0, 0));
        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(1, 0));
    }

    [Fact]
    public void ReadsRowsBottomUpByDefault()
    {
        // TGA's origin is the bottom-left, so the first row in the file is the last on screen.
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(
            1,
            2,
            TrueColour,
            24,
            [0x00, 0x00, 0xFF, 0x00, 0xFF, 0x00]));

        FastBitmap bitmap = TgaReader.Read(file.Path);

        Assert.Equal(FastColor.FromUInt32(0xFF00FF00), bitmap.GetPixel(0, 0));
        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 1));
    }

    [Fact]
    public void ReadsRowsTopDownWhenTheDescriptorSaysSo()
    {
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(
            1,
            2,
            TrueColour,
            24,
            [0x00, 0x00, 0xFF, 0x00, 0xFF, 0x00],
            descriptor: TopToBottom));

        FastBitmap bitmap = TgaReader.Read(file.Path);

        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 0));
        Assert.Equal(FastColor.FromUInt32(0xFF00FF00), bitmap.GetPixel(0, 1));
    }

    [Fact]
    public void ReadsColumnsRightToLeftWhenTheDescriptorSaysSo()
    {
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(
            2,
            1,
            TrueColour,
            24,
            [0x00, 0x00, 0xFF, 0x00, 0xFF, 0x00],
            descriptor: TopToBottom | RightToLeft));

        FastBitmap bitmap = TgaReader.Read(file.Path);

        Assert.Equal(FastColor.FromUInt32(0xFF00FF00), bitmap.GetPixel(0, 0));
        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(1, 0));
    }

    [Fact]
    public void ExpandsARunLengthPacket()
    {
        // 0x82 is a run of three, then the one pixel to repeat.
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(3, 1, RunLengthTrueColour, 24, [0x82, 0x00, 0x00, 0xFF]));

        FastBitmap bitmap = TgaReader.Read(file.Path);

        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 0));
        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(1, 0));
        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(2, 0));
    }

    [Fact]
    public void ExpandsALiteralPacket()
    {
        // 0x01 is two pixels that follow one after another.
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(
            2,
            1,
            RunLengthTrueColour,
            24,
            [0x01, 0x00, 0x00, 0xFF, 0x00, 0xFF, 0x00]));

        FastBitmap bitmap = TgaReader.Read(file.Path);

        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 0));
        Assert.Equal(FastColor.FromUInt32(0xFF00FF00), bitmap.GetPixel(1, 0));
    }

    [Fact]
    public void ExpandsARunLengthColourMappedImage()
    {
        byte[] colourMap = [0x00, 0x00, 0x00, 0x00, 0x00, 0xFF];
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(2, 1, RunLengthColourMapped, 8, [0x81, 0x01], colourMap));

        FastBitmap bitmap = TgaReader.Read(file.Path);

        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(0, 0));
        Assert.Equal(FastColor.FromUInt32(0xFFFF0000), bitmap.GetPixel(1, 0));
    }

    [Fact]
    public void ReturnsAnEmptyBitmapForAnEmptyFile()
    {
        using TempImageFile file = TempImageFile.From([]);

        FastBitmap bitmap = TgaReader.Read(file.Path);

        Assert.Equal(0, bitmap.Width);
        Assert.Equal(0, bitmap.Height);
    }

    [Fact]
    public void RejectsAnUnsupportedImageType()
    {
        // 32 is not a type the format defines.
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(1, 1, 32, 24, [0x00, 0x00, 0x00]));

        Assert.Throws<SharpKindException>(() => TgaReader.Read(file.Path));
    }

    [Fact]
    public void RejectsPixelDataThatRunsPastTheEndOfTheFile()
    {
        // The header claims four pixels and the file carries one.
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(2, 2, TrueColour, 24, [0x00, 0x00, 0xFF]));

        Assert.Throws<SharpKindException>(() => TgaReader.Read(file.Path));
    }

    [Fact]
    public void RejectsRunLengthDataDescribingMorePixelsThanTheImageHas()
    {
        // A run of 128 into a one-pixel image.
        using TempImageFile file = TempImageFile.From(TgaBuilder.Build(1, 1, RunLengthTrueColour, 24, [0xFF, 0x00, 0x00, 0xFF]));

        Assert.Throws<SharpKindException>(() => TgaReader.Read(file.Path));
    }
}
