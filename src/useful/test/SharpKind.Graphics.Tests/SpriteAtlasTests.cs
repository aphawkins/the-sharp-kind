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
    public void NamesTheSpriteItCannotFind()
    {
        SpriteAtlas atlas = new(new Dictionary<string, SpriteRect> { ["one"] = new(0, 0, 8, 8) });

        SharpKindException ex = Assert.Throws<SharpKindException>(() => atlas.Sprite("two"));

        Assert.Contains("two", ex.Message, StringComparison.Ordinal);
    }
}
