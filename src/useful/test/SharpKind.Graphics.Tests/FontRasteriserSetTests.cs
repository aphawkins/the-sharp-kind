// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;

namespace SharpKind.Graphics.Tests;

// Choosing between the kinds a rendition offers, and standing in for the ones
// it does not.
public class FontRasteriserSetTests
{
    [Fact]
    public void DrawsWithTheKindAskedFor()
    {
        // Arrange
        StubRasteriser sheets = new(10);
        StubRasteriser strikes = new(20);
        using FontRasteriserSet set = new(
            new() { { FontKind.Bitmap, sheets }, { FontKind.Fon, strikes } },
            FontKind.Fon);

        // Act & Assert
        Assert.Equal(FontKind.Fon, set.Kind);
        Assert.Equal(new(20, 20), set.Measure("A", "Small"));
    }

    // A missing declared kind draws with the rendition's own sheets rather than failing: it's a reason to fall back, not to stop.
    [Fact]
    public void FallsBackToTheSheetsForAKindTheRenditionLacks()
    {
        // Arrange
        using FontRasteriserSet set = new(new() { { FontKind.Bitmap, new StubRasteriser(10) } }, FontKind.TrueType);

        // Act & Assert
        Assert.Equal(FontKind.Bitmap, set.Kind);
        Assert.False(set.Has(FontKind.TrueType));
        Assert.Equal(new(10, 10), set.Measure("A", "Small"));
    }

    // Reading the kind back has to say what is actually being drawn with, or
    // a settings screen would show a font that is not on the screen.
    [Fact]
    public void SelectingReportsTheKindActuallyInUse()
    {
        // Arrange
        using FontRasteriserSet set = new(
            new() { { FontKind.Bitmap, new StubRasteriser(10) }, { FontKind.Fon, new StubRasteriser(20) } },
            FontKind.Bitmap);

        // Act & Assert
        Assert.Equal(FontKind.Fon, set.Select(FontKind.Fon));
        Assert.Equal(FontKind.Fon, set.Kind);
        Assert.Equal(FontKind.Bitmap, set.Select(FontKind.TrueType));
        Assert.Equal(FontKind.Bitmap, set.Kind);
    }

    // The sheets are what everything else falls back to, so a set without
    // them has no answer to give for a kind it lacks.
    [Fact]
    public void RefusesASetWithoutTheRenditionsOwnSheets()
    {
        // Act
        SharpKindException exception = Assert.Throws<SharpKindException>(
            () => new FontRasteriserSet(new() { { FontKind.Fon, new StubRasteriser(10) } }, FontKind.Fon));

        // Assert
        Assert.Contains("falls back", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DisposesEveryKindItHolds()
    {
        // Arrange
        StubRasteriser sheets = new(10);
        StubRasteriser strikes = new(20);
        using FontRasteriserSet set = new(
            new() { { FontKind.Bitmap, sheets }, { FontKind.Fon, strikes } },
            FontKind.Bitmap);

        // Act - disposed again below by the using, since the renderer holding
        // a set and whatever built it may both let it go.
        set.Dispose();

        // Assert
        Assert.Equal(1, sheets.Disposals);
        Assert.Equal(1, strikes.Disposals);
    }

    // Measures and draws everything as a square of its own size, so which
    // rasteriser answered is visible in the result.
    private sealed class StubRasteriser(int size) : IFontRasteriser, IDisposable
    {
        public int Disposals { get; private set; }

        public FastBitmap Rasterise(string text, string fontType, FastColor color) => new(size, size);

        public Vector2 Measure(string text, string fontType) => new(size, size);

        public void Dispose() => Disposals++;
    }
}
