// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Graphics.Tests;

public class SpriteAtlasTests
{
    [Fact]
    public void ReadsEveryEntryFromJson()
    {
        const string Json = /*lang=json,strict*/ """
            {
              "bub-walk-0": { "X": 0, "Y": 0, "Width": 16, "Height": 16 },
              "bubble": { "X": 16, "Y": 32, "Width": 8, "Height": 8 }
            }
            """;

        using TempImageFile file = TempImageFile.From(Encoding.UTF8.GetBytes(Json));

        SpriteAtlas atlas = SpriteAtlas.Read(file.Path);

        Assert.Equal(2, atlas.Count);
        Assert.Equal(new SpriteRect(0, 0, 16, 16), atlas.Sprite("bub-walk-0"));
        Assert.Equal(new SpriteRect(16, 32, 8, 8), atlas.Sprite("bubble"));
    }

    [Fact]
    public void KnowsWhichNamesItHas()
    {
        SpriteAtlas atlas = new(new Dictionary<string, SpriteRect> { ["one"] = new(0, 0, 8, 8) });

        Assert.True(atlas.Has("one"));
        Assert.False(atlas.Has("One"));
        Assert.Equal(["one"], atlas.Names);
    }

    [Fact]
    public void BuildsAGridInReadingOrder()
    {
        SpriteAtlas atlas = SpriteAtlas.Grid("tile", 8, 8, 5, 3);

        Assert.Equal(15, atlas.Count);
        Assert.Equal(new SpriteRect(0, 0, 8, 8), atlas.Sprite("tile-0"));
        Assert.Equal(new SpriteRect(32, 0, 8, 8), atlas.Sprite("tile-4"));
        Assert.Equal(new SpriteRect(0, 8, 8, 8), atlas.Sprite("tile-5"));
        Assert.Equal(new SpriteRect(32, 16, 8, 8), atlas.Sprite("tile-14"));
        Assert.False(atlas.Has("tile-15"));
    }

    [Theory]
    [InlineData(0, 8, 2, 2)]
    [InlineData(8, 0, 2, 2)]
    [InlineData(8, 8, 0, 2)]
    [InlineData(8, 8, 2, 0)]
    public void RefusesAGridThatDescribesNothing(int cellWidth, int cellHeight, int columns, int rows)
        => Assert.Throws<SharpKindException>(
            () => SpriteAtlas.Grid("tile", cellWidth, cellHeight, columns, rows));

    [Fact]
    public void NamesTheSpriteItCannotFind()
    {
        SpriteAtlas atlas = new(new Dictionary<string, SpriteRect> { ["one"] = new(0, 0, 8, 8) });

        SharpKindException ex = Assert.Throws<SharpKindException>(() => atlas.Sprite("two"));

        Assert.Contains("two", ex.Message, StringComparison.Ordinal);
    }
}
