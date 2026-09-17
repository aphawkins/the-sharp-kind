// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.Text;
using System.Text.Json;
using Moq;
using SharpKind.Assets;
using SharpKind.Fakes.Assets;

namespace SharpKind.Graphics.Tests;

public class AssetSetTests
{
    // Stand-ins for what a rendition declares about itself, since these are no longer fixed game-side facts.
    private static AssetColourLimits EightBit => new()
    {
        MaxColours = 16,
        PaletteNamesEveryColour = true,
        ChannelBits = 8,
    };

    private static AssetColourLimits SixteenBit => new()
    {
        MaxColours = 4096,
        PaletteNamesEveryColour = false,
        ChannelBits = 4,
    };

    [Fact]
    public void CountsDistinctOpaqueColoursAcrossTheWholeSet()
    {
        // Arrange: two images sharing one colour, so the union is 3 rather
        // than the 4 a per-image count would give.
        using TempImageFile red = TempImageFile.From(Bmp(0xFFFF0000, 0xFF00FF00));
        using TempImageFile blue = TempImageFile.From(Bmp(0xFF0000FF, 0xFF00FF00));

        // Act
        AssetSet assets = AssetSet.Load(Locator(SixteenBit, ("Red", red), ("Blue", blue)));

        // Assert
        Assert.Equal(3, assets.Budget.ColourCount);
        Assert.Equal(2, assets.Budget.PerAsset["Red"]);
        Assert.Equal(2, assets.Budget.PerAsset["Blue"]);
    }

    [Fact]
    public void ExcludesFullyTransparentPixelsFromTheCount()
    {
        // Arrange: one opaque colour and one transparent pixel.
        using TempImageFile image = TempImageFile.From(Bmp(0xFFFF0000, 0x00123456));

        // Act
        AssetSet assets = AssetSet.Load(Locator(SixteenBit, ("Image", image)));

        // Assert
        Assert.Equal(1, assets.Budget.ColourCount);
    }

    [Fact]
    public void CountsBitmapFontsAgainstTheBudgetToo()
    {
        // Arrange: the sheet is part of the tier's set whatever font kind is
        // selected to draw with.
        using TempImageFile image = TempImageFile.From(Bmp(0xFFFF0000, 0xFFFF0000));
        using TempImageFile font = TempImageFile.From(FontBmp(0xFF00FF00, 0xFF0000FF));

        // Act
        AssetSet assets = AssetSet.Load(Locator(SixteenBit, [("Image", image)], [("Small", font)]));

        // Assert
        Assert.Equal(3, assets.Budget.ColourCount);
        Assert.Equal(2, assets.Budget.PerAsset["Small"]);
    }

    [Fact]
    public void ThrowsOnPixelsWhoseAlphaIsNeitherFullyOpaqueNorFullyTransparent()
    {
        // Arrange: the renderer has no middle ground for alpha, so a
        // half-transparent pixel is an authoring mistake, not a style.
        using TempImageFile image = TempImageFile.From(Bmp(0x80FF0000, 0xFF00FF00));
        IAssetLocator locator = Locator(SixteenBit, ("Image", image));

        // Act / Assert
        Assert.Throws<SharpKindException>(() => AssetSet.Load(locator));
    }

    [Fact]
    public void ThrowsWhenAnEightBitSetExceedsSixteenColours()
    {
        // Arrange: 17 distinct colours against the 8-bit cap of 16.
        uint[] colours = [.. Enumerable.Range(0, 17).Select(i => 0xFF000000u | (uint)i)];
        using TempImageFile image = TempImageFile.From(Bmp(colours));
        IAssetLocator locator = Locator(EightBit, ("Image", image));

        // Act / Assert
        Assert.Throws<SharpKindException>(() => AssetSet.Load(locator));
    }

    [Fact]
    public void ReportsAnEightBitSetOfSixteenColoursAsWithinBudget()
    {
        // Arrange: exactly at the cap, which must pass.
        uint[] colours = [.. Enumerable.Range(0, 16).Select(i => 0xFF000000u | (uint)i)];
        using TempImageFile image = TempImageFile.From(Bmp(colours));

        // Act
        AssetSet assets = AssetSet.Load(Locator(EightBit, ("Image", image)));

        // Assert
        Assert.True(assets.Budget.IsWithinBudget);
    }

    [Fact]
    public void AllowsTheSameSetUnderTheRoomierSixteenBitCap()
    {
        // 17 colours break the 8-bit cap but are fine here, confirming the failure above is the cap, not the loader.
        using TempImageFile image = TempImageFile.From(Bmp(OnGrid(17)));

        // Act
        AssetSet assets = AssetSet.Load(Locator(SixteenBit, ("Image", image)));

        // Assert
        Assert.Single(assets.Images);
        Assert.Equal(17, assets.Budget.ColourCount);
    }

    [Fact]
    public void ThrowsWhenAnEightBitBitmapUsesAColourThePaletteDoesNotName()
    {
        // 8-bit was indexed-colour: a non-palette pixel is a colour the machine could not show, well inside the 16-colour cap.
        using TempImageFile palette = Palette(("Red", 0xFFFF0000));
        using TempImageFile image = TempImageFile.From(Bmp(0xFFFF0000, 0xFF00FF00));
        IAssetLocator locator = Locator(EightBit, palette, ("Image", image));

        // Act
        SharpKindException exception = Assert.Throws<SharpKindException>(() => AssetSet.Load(locator));

        // Assert: the message has to name the file and the colour, or the fix
        // is a hunt through every bitmap in the tier.
        Assert.Contains("Image", exception.Message, StringComparison.Ordinal);
        Assert.Contains("FF00FF00", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AllowsAnEightBitSetWhoseColoursThePaletteAllNames()
    {
        // Arrange: every pixel is a palette entry, and the palette names one
        // colour no bitmap uses - the rule is a subset test, not equality.
        using TempImageFile palette = Palette(("Red", 0xFFFF0000), ("Green", 0xFF00FF00), ("Blue", 0xFF0000FF));
        using TempImageFile image = TempImageFile.From(Bmp(0xFFFF0000, 0xFF00FF00));

        // Act
        AssetSet assets = AssetSet.Load(Locator(EightBit, palette, ("Image", image)));

        // Assert
        Assert.True(assets.Budget.IsWithinPalette);
        Assert.Empty(assets.Budget.OutsidePalette);
    }

    [Fact]
    public void IgnoresTransparentPixelsWhenCheckingAgainstThePalette()
    {
        // Arrange: a transparent pixel carries no colour, so the palette has
        // no reason to name whatever ARGB it happens to hold.
        using TempImageFile palette = Palette(("Red", 0xFFFF0000));
        using TempImageFile image = TempImageFile.From(Bmp(0xFFFF0000, 0x00123456));

        // Act
        AssetSet assets = AssetSet.Load(Locator(EightBit, palette, ("Image", image)));

        // Assert
        Assert.True(assets.Budget.IsWithinPalette);
    }

    [Fact]
    public void ReportsButAllowsColoursOutsideTheSixteenBitPalette()
    {
        // 16-bit is direct-colour: the palette is only names the geometry draws with, bitmaps are free of it.
        using TempImageFile palette = Palette(("Red", 0xFFFF0000));
        using TempImageFile image = TempImageFile.From(Bmp(0xFFFF0000, 0xFF00FF00));

        // Act
        AssetSet assets = AssetSet.Load(Locator(SixteenBit, palette, ("Image", image)));

        // Assert
        Assert.True(assets.Budget.IsWithinPalette);
        Assert.Equal([0xFF00FF00], assets.Budget.OutsidePalette["Image"]);
    }

    [Fact]
    public void ChecksBitmapFontsAgainstThePaletteToo()
    {
        // Arrange: the font is part of the tier's set, so its colours have to
        // be palette colours like any other asset's.
        using TempImageFile palette = Palette(("Red", 0xFFFF0000));
        using TempImageFile image = TempImageFile.From(Bmp(0xFFFF0000, 0xFFFF0000));
        using TempImageFile font = TempImageFile.From(FontBmp(0xFFFF0000, 0xFF00FF00));
        IAssetLocator locator = Locator(EightBit, palette, [("Image", image)], [("Small", font)]);

        // Act
        SharpKindException exception = Assert.Throws<SharpKindException>(() => AssetSet.Load(locator));

        // Assert
        Assert.Contains("Small", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0xFF000000u, true)]
    [InlineData(0xFFFFFFFFu, true)]
    [InlineData(0xFF112233u, true)]
    [InlineData(0xFFEE6622u, true)]
    [InlineData(0xFF808080u, false)] // the midpoint no 4-bit channel can hold
    [InlineData(0xFFF0F0F0u, false)] // a left shift rather than a replication
    [InlineData(0xFF11FF34u, false)] // one stray channel is enough
    [InlineData(0x80112233u, true)] // alpha is not a channel of the DAC
    public void PlacesAColourOnTheTwelveBitGridOnlyWhenEveryChannelIsALevel(uint argb, bool expected)
        => Assert.Equal(expected, AssetColourBudget.IsOnGrid(argb, 4));

    [Fact]
    public void ThrowsWhenASixteenBitColourSitsBetweenTheTiersLevels()
    {
        // Arrange: 0x808080 is the classic mid grey, and the one no 12-bit DAC
        // can produce - 0x77 and 0x88 are the levels either side of it.
        using TempImageFile image = TempImageFile.From(Bmp(0xFF112233, 0xFF808080));
        IAssetLocator locator = Locator(SixteenBit, ("Image", image));

        // Act
        SharpKindException exception = Assert.Throws<SharpKindException>(() => AssetSet.Load(locator));

        // Assert: naming the file and the colour is what makes the failure
        // actionable rather than a hunt through the tier's art.
        Assert.Contains("Image", exception.Message, StringComparison.Ordinal);
        Assert.Contains("FF808080", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AllowsASixteenBitSetWhoseColoursAllSitOnTheGrid()
    {
        // Arrange
        using TempImageFile image = TempImageFile.From(Bmp(0xFF112233, 0xFFEE6622));

        // Act
        AssetSet assets = AssetSet.Load(Locator(SixteenBit, ("Image", image)));

        // Assert
        Assert.True(assets.Budget.IsOnColourGrid);
        Assert.Empty(assets.Budget.OffGrid);
    }

    [Fact]
    public void ChecksTheNamedPaletteAgainstTheGridAsWellAsTheBitmaps()
    {
        // The bitmap is clean here, so only the palette can be at fault.
        using TempImageFile palette = Palette(("Grey", 0xFF808080));
        using TempImageFile image = TempImageFile.From(Bmp(0xFF112233, 0xFF112233));
        IAssetLocator locator = Locator(SixteenBit, palette, ("Image", image));

        // Act
        SharpKindException exception = Assert.Throws<SharpKindException>(() => AssetSet.Load(locator));

        // Assert
        Assert.Contains("Palette", exception.Message, StringComparison.Ordinal);
        Assert.Contains("FF808080", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LeavesTheEightBitTierFreeOfTheChannelDepthRule()
    {
        // Arrange: the same colour that fails on 16-bit, on the tier whose
        // limit is its 16-entry palette rather than its channel depth.
        using TempImageFile image = TempImageFile.From(Bmp(0xFF808080, 0xFF808080));

        // Act
        AssetSet assets = AssetSet.Load(Locator(EightBit, ("Image", image)));

        // Assert
        Assert.True(assets.Budget.IsOnColourGrid);
    }

    [Fact]
    public void ChecksBitmapFontsAgainstTheGridToo()
    {
        // Arrange: the font is part of the tier's set, so its channels have to
        // land on the tier's levels like any other asset's.
        using TempImageFile image = TempImageFile.From(Bmp(0xFF112233, 0xFF112233));
        using TempImageFile font = TempImageFile.From(FontBmp(0xFF112233, 0xFF808080));
        IAssetLocator locator = Locator(SixteenBit, [("Image", image)], [("Small", font)]);

        // Act
        SharpKindException exception = Assert.Throws<SharpKindException>(() => AssetSet.Load(locator));

        // Assert
        Assert.Contains("Small", exception.Message, StringComparison.Ordinal);
    }

    // Distinct colours that all sit on the 12-bit grid. One channel only
    // carries 16 of them, so the count spills into the next.
    [Fact]
    public void LoadsTheStrikesADeclaredFonHolds()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.fon");
        File.WriteAllBytes(path, FonFileBuilder.Build(new FontSpec(0x0200, 8, false, 'A', 'A', [Block(4, 8)])));

        try
        {
            // Act
            AssetSet assets = AssetSet.Load(LocatorWithFon(path, 8));

            // Assert
            Assert.Equal(8, assets.FonFonts["Small"].PixelHeight);
            Assert.Equal(4, assets.FonFonts["Small"].Glyph('A')!.Width);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // A .fon carries no colours of its own - a strike is one bit per pixel, coloured when drawn.
    [Fact]
    public void CountsNoColoursForAFon()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.fon");
        File.WriteAllBytes(path, FonFileBuilder.Build(new FontSpec(0x0200, 8, false, 'A', 'A', [Block(4, 8)])));

        try
        {
            // Act
            AssetSet assets = AssetSet.Load(LocatorWithFon(path, 8));

            // Assert
            Assert.Equal(0, assets.Budget.ColourCount);
            Assert.DoesNotContain("Small", assets.Budget.PerAsset.Keys);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // Missing files are reported together, and a .fon must be among them.
    [Fact]
    public void ReportsAMissingFonWithTheRest()
    {
        // Arrange
        string missing = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.fon");

        // Act
        SharpKindException exception =
            Assert.Throws<SharpKindException>(() => AssetSet.Load(LocatorWithFon(missing, 8)));

        // Assert
        Assert.Contains(Path.GetFileName(missing), exception.Message, StringComparison.Ordinal);
    }

    private static uint[] OnGrid(int count)
        => [.. Enumerable.Range(0, count).Select(i => 0xFF000000u | ((uint)(i % 16) * 0x11u) | ((uint)(i / 16) * 0x1100u))];

    private static TempImageFile Palette(params (string Name, uint Argb)[] colours)
        => TempImageFile.From(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
            colours.ToDictionary(x => x.Name, x => x.Argb.ToString("X8", CultureInfo.InvariantCulture)))));

    private static byte[] Bmp(params uint[] colours)
    {
        byte[] pixels = new byte[colours.Length * 4];

        for (int i = 0; i < colours.Length; i++)
        {
            pixels[i * 4] = (byte)(colours[i] & 0xFF);
            pixels[(i * 4) + 1] = (byte)((colours[i] >> 8) & 0xFF);
            pixels[(i * 4) + 2] = (byte)((colours[i] >> 16) & 0xFF);
            pixels[(i * 4) + 3] = (byte)(colours[i] >> 24);
        }

        return BmpBuilder.Build(colours.Length, 1, 32, pixels);
    }

    // A .fon is loaded alongside the sheets, so a rendition that declares one
    // has its strikes ready before anything asks to draw with them.
    private static string[] Block(int width, int height)
        => [.. Enumerable.Repeat(new string('#', width), height)];

    private static IAssetLocator LocatorWithFon(string path, int pixelHeight)
    {
        Mock<IAssetLocator> locator = new();
        locator.SetupGet(x => x.Rendition).Returns("Test");
        locator.SetupGet(x => x.Colours).Returns(SixteenBit);
        locator.SetupGet(x => x.PalettePath).Returns(string.Empty);
        locator.SetupGet(x => x.ImagePaths).Returns(new Dictionary<string, string>());
        locator.SetupGet(x => x.FontBitmaps).Returns(new Dictionary<string, BitmapFontAsset>());
        Dictionary<string, FonFontAsset> fons = new() { { "Small", new(path, pixelHeight) } };
        locator.SetupGet(x => x.FontFons).Returns(fons);

        return locator.Object;
    }

    // BitmapFont only accepts the 513x193 sheet the real fonts use, so a
    // font fixture has to be that size whatever colours it carries.
    private static byte[] FontBmp(uint first, uint rest)
    {
        const int width = 513;
        const int height = 193;
        byte[] pixels = new byte[width * height * 4];

        for (int i = 0; i < width * height; i++)
        {
            uint colour = i == 0 ? first : rest;
            pixels[i * 4] = (byte)(colour & 0xFF);
            pixels[(i * 4) + 1] = (byte)((colour >> 8) & 0xFF);
            pixels[(i * 4) + 2] = (byte)((colour >> 16) & 0xFF);
            pixels[(i * 4) + 3] = (byte)(colour >> 24);
        }

        return BmpBuilder.Build(width, height, 32, pixels);
    }

    private static IAssetLocator Locator(AssetColourLimits limits, params (string Name, TempImageFile File)[] images)
        => Locator(limits, null, images, []);

    private static IAssetLocator Locator(
        AssetColourLimits limits,
        TempImageFile palette,
        params (string Name, TempImageFile File)[] images)
        => Locator(limits, palette, images, []);

    private static IAssetLocator Locator(
        AssetColourLimits limits,
        (string Name, TempImageFile File)[] images,
        (string Name, TempImageFile File)[] fonts)
        => Locator(limits, null, images, fonts);

    private static IAssetLocator Locator(
        AssetColourLimits limits,
        TempImageFile? palette,
        (string Name, TempImageFile File)[] images,
        (string Name, TempImageFile File)[] fonts)
    {
        Mock<IAssetLocator> locator = new();
        locator.SetupGet(x => x.Rendition).Returns("Test");
        locator.SetupGet(x => x.Colours).Returns(limits);
        locator.SetupGet(x => x.PalettePath).Returns(palette?.Path ?? string.Empty);
        locator.SetupGet(x => x.ImagePaths).Returns(images.ToDictionary(x => x.Name, x => x.File.Path));
        locator.SetupGet(x => x.FontBitmaps).Returns(fonts.ToDictionary(
            x => x.Name,
            x => new BitmapFontAsset(
                x.File.Path,
                new BitmapFontEntry { File = x.File.Path, CellWidth = 32, CellHeight = 32, Columns = 16 })));
        locator.SetupGet(x => x.FontFons).Returns(new Dictionary<string, FonFontAsset>());
        return locator.Object;
    }
}
