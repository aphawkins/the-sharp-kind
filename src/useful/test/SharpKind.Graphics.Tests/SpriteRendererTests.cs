// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using SharpKind.Graphics.Fakes;

namespace SharpKind.Graphics.Tests;

public class SpriteRendererTests
{
    private static readonly SpriteSheet s_sheet = new(
        "Atlas",
        new(new Dictionary<string, SpriteRect> { ["bubble"] = new(16, 32, 8, 12) }),
        new(64, 64));

    [Fact]
    public void DrawsTheNamedSpriteAtItsOwnSize()
    {
        RecordingGraphics graphics = new();
        SpriteRenderer renderer = new(graphics, s_sheet);

        renderer.Draw("bubble", new(100, 50));

        (string imageType, Vector2 position, Vector2 size, Vector2 sourcePosition, Vector2 sourceSize) =
            Assert.Single(graphics.ImageParts);

        Assert.Equal("Atlas", imageType);
        Assert.Equal(new Vector2(100, 50), position);
        Assert.Equal(new Vector2(8, 12), size);
        Assert.Equal(new Vector2(16, 32), sourcePosition);
        Assert.Equal(new Vector2(8, 12), sourceSize);
    }

    [Fact]
    public void MirrorsWithANegativeSourceWidth()
    {
        RecordingGraphics graphics = new();
        SpriteRenderer renderer = new(graphics, s_sheet);

        renderer.Draw("bubble", new(100, 50), true);

        Assert.Equal(new Vector2(-8, 12), Assert.Single(graphics.ImageParts).SourceSize);
    }

    [Fact]
    public void FaultsOnASpriteTheSheetDoesNotHave()
    {
        RecordingGraphics graphics = new();
        SpriteRenderer renderer = new(graphics, s_sheet);

        Assert.Throws<SharpKindException>(() => renderer.Draw("missing", Vector2.Zero));
        Assert.Empty(graphics.ImageParts);
    }
}
