// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics.Tests;

public class SpriteSheetTests
{
    [Fact]
    public void AcceptsRectanglesInsideTheSheet()
    {
        SpriteAtlas atlas = new(new Dictionary<string, SpriteRect>
        {
            ["top-left"] = new(0, 0, 16, 16),
            ["bottom-right"] = new(48, 48, 16, 16),
        });

        SpriteSheet sheet = new("Atlas", atlas, new(64, 64));

        Assert.Equal("Atlas", sheet.ImageType);
        Assert.Equal(2, sheet.Atlas.Count);
    }

    [Theory]
    [InlineData(-1, 0, 16, 16)]
    [InlineData(0, -1, 16, 16)]
    [InlineData(49, 0, 16, 16)]
    [InlineData(0, 49, 16, 16)]
    [InlineData(0, 0, 0, 16)]
    [InlineData(0, 0, 16, 0)]
    public void RejectsRectanglesOutsideTheSheet(int x, int y, int width, int height)
    {
        SpriteAtlas atlas = new(new Dictionary<string, SpriteRect> { ["stray"] = new(x, y, width, height) });

        SharpKindException ex = Assert.Throws<SharpKindException>(() => new SpriteSheet("Atlas", atlas, new(64, 64)));

        Assert.Contains("stray", ex.Message, StringComparison.Ordinal);
        Assert.Contains("64x64", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsEveryOffenderAtOnce()
    {
        SpriteAtlas atlas = new(new Dictionary<string, SpriteRect>
        {
            ["good"] = new(0, 0, 8, 8),
            ["wide"] = new(60, 0, 8, 8),
            ["tall"] = new(0, 60, 8, 8),
        });

        SharpKindException ex = Assert.Throws<SharpKindException>(() => new SpriteSheet("Atlas", atlas, new(64, 64)));

        Assert.Contains("2 sprite(s)", ex.Message, StringComparison.Ordinal);
        Assert.Contains("tall", ex.Message, StringComparison.Ordinal);
        Assert.Contains("wide", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("good", ex.Message, StringComparison.Ordinal);
    }
}
