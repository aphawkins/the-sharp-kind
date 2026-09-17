// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Fakes.Assets;
using Xunit;

namespace SharpKind.Assets.Tests;

// Fonts are built rather than shipped as files: no single real font exercises every case (several strikes, both FNT versions, proportional vs fixed pitch).
public class FonFontReaderTests
{
    [Fact]
    public void ReadsFixedPitchGlyphs()
    {
        // Arrange - 'A' as a 4x5 box, in a fixed-pitch 2.0 font.
        string[] glyph =
        [
            "####",
            "#..#",
            "####",
            "#..#",
            "#..#",
        ];

        string path = WriteFont(new FontSpec(0x0200, 5, false, 'A', 'A', [glyph]));

        try
        {
            // Act
            FonFont font = FonFontReader.Read(path, 5);

            // Assert
            Assert.Equal(5, font.PixelHeight);
            Assert.False(font.IsProportional);
            AssertGlyph(glyph, font.Glyph('A'));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadsProportionalGlyphsOfDifferingWidths()
    {
        // Arrange - an 'i' narrower than the 'm' beside it.
        string[] i = ["#", "#", "#"];
        string[] j = ["#.#", "#.#", "###"];

        string path = WriteFont(new FontSpec(0x0200, 3, true, 'i', 'j', [i, j]));

        try
        {
            // Act
            FonFont font = FonFontReader.Read(path, 3);

            // Assert
            Assert.True(font.IsProportional);
            Assert.Equal(3, font.MaxWidth);
            Assert.Equal(1, font.Glyph('i')!.Width);
            Assert.Equal(3, font.Glyph('j')!.Width);
            AssertGlyph(i, font.Glyph('i'));
            AssertGlyph(j, font.Glyph('j'));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // 3.0 widened the glyph offset and moved the character table down; both versions must decode to the same glyph.
    [Fact]
    public void ReadsVersionThreeGlyphs()
    {
        // Arrange
        string[] glyph = ["#.#", ".#.", "#.#"];

        string path = WriteFont(new FontSpec(0x0300, 3, false, 'X', 'X', [glyph]));

        try
        {
            // Act
            FonFont font = FonFontReader.Read(path, 3);

            // Assert
            AssertGlyph(glyph, font.Glyph('X'));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // Spans two byte-columns of the packing; the second column's bits must land in the right pixels.
    [Fact]
    public void ReadsGlyphsWiderThanOneByte()
    {
        // Arrange
        string[] glyph =
        [
            "#########.#",
            "..........#",
            "#########.#",
        ];

        string path = WriteFont(new FontSpec(0x0200, 3, false, 'W', 'W', [glyph]));

        try
        {
            // Act
            FonFont font = FonFontReader.Read(path, 3);

            // Assert
            Assert.Equal(11, font.Glyph('W')!.Width);
            AssertGlyph(glyph, font.Glyph('W'));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]

    // Exact matches, and heights between strikes going to the nearer one.
    [InlineData(8, 8)]
    [InlineData(16, 16)]
    [InlineData(9, 8)]
    [InlineData(15, 16)]

    // Outside the range the file holds: the nearest is all there is.
    [InlineData(3, 8)]
    [InlineData(40, 16)]

    // Exactly between two strikes goes to the smaller.
    [InlineData(12, 8)]
    public void TakesTheNearestStrike(int wanted, int expected)
    {
        // Arrange
        string path = WriteFont(
            new FontSpec(0x0200, 8, false, 'A', 'A', [Block(4, 8)]),
            new FontSpec(0x0200, 16, false, 'A', 'A', [Block(4, 16)]));

        try
        {
            // Act
            FonFont font = FonFontReader.Read(path, wanted);

            // Assert
            Assert.Equal(expected, font.PixelHeight);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReportsEveryStrikeHeight()
    {
        // Arrange
        string path = WriteFont(
            new FontSpec(0x0200, 8, false, 'A', 'A', [Block(4, 8)]),
            new FontSpec(0x0200, 16, false, 'A', 'A', [Block(4, 16)]));

        try
        {
            // Act
            IReadOnlyList<int> heights = FonFontReader.StrikeHeights(path);

            // Assert
            Assert.Equal([8, 16], heights);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // A character outside the declared range has no glyph, not a guessed one.
    [Fact]
    public void HasNoGlyphOutsideTheDeclaredRange()
    {
        // Arrange
        string path = WriteFont(new FontSpec(0x0200, 3, false, 'a', 'b', [Block(2, 3), Block(2, 3)]));

        try
        {
            // Act
            FonFont font = FonFontReader.Read(path, 3);

            // Assert
            Assert.NotNull(font.Glyph('a'));
            Assert.NotNull(font.Glyph('b'));
            Assert.Null(font.Glyph('c'));
            Assert.Null(font.Glyph(' '));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // A height left out of the manifest binds as zero; nearest-strike would silently answer that as readily as any number.
    [Theory]
    [InlineData(0)]
    [InlineData(-8)]
    public void RefusesToBeAskedForAStrikeThatIsNotASize(int pixelHeight)
    {
        // Arrange
        string path = WriteFont(
            new FontSpec(0x0200, 8, false, 'A', 'A', [Block(4, 8)]),
            new FontSpec(0x0200, 16, false, 'A', 'A', [Block(4, 16)]));

        try
        {
            // Act
            SharpKindException exception = Assert.Throws<SharpKindException>(
                () => FonFontReader.Read(path, pixelHeight));

            // Assert
            Assert.Contains("real size", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RejectsAFileThatIsNotAnExecutable()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.fon");
        File.WriteAllBytes(path, [.. Enumerable.Repeat((byte)0x00, 256)]);

        try
        {
            // Act
            SharpKindException exception = Assert.Throws<SharpKindException>(() => FonFontReader.Read(path, 8));

            // Assert
            Assert.Contains("not an executable", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RejectsAFileThatEndsPartWayThroughAFont()
    {
        // Arrange
        string full = WriteFont(new FontSpec(0x0200, 8, false, 'A', 'A', [Block(4, 8)]));
        string truncated = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.fon");
        byte[] bytes = File.ReadAllBytes(full);
        File.WriteAllBytes(truncated, bytes[..(bytes.Length / 2)]);
        File.Delete(full);

        try
        {
            // Act
            SharpKindException exception = Assert.Throws<SharpKindException>(() => FonFontReader.Read(truncated, 8));

            // Assert
            Assert.Contains("ends part way through", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(truncated);
        }
    }

    private static string[] Block(int width, int height)
        => [.. Enumerable.Repeat(new string('#', width), height)];

    private static void AssertGlyph(string[] expected, FonGlyph? glyph)
    {
        Assert.NotNull(glyph);
        Assert.Equal(expected.Length, glyph.Height);
        Assert.Equal(expected[0].Length, glyph.Width);

        for (int y = 0; y < glyph.Height; y++)
        {
            for (int x = 0; x < glyph.Width; x++)
            {
                Assert.Equal(expected[y][x] == '#', glyph[x, y]);
            }
        }
    }

    private static string WriteFont(params FontSpec[] fonts)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.fon");
        File.WriteAllBytes(path, FonFileBuilder.Build(fonts));

        return path;
    }
}
