// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Config;
using SharpKind.Abstraction.Renditions;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Config;

public sealed class BbConfigTests
{
    [Fact]
    public void DefaultsToTheOnlyRenditionThisGameShips()
    {
        // The engine defaults to 16-bit; this game has art for the 8-bit tier alone.
        BbConfig config = new();

        Assert.Equal(RenditionNames.EightBit, config.Engine.Rendition);
    }

    [Fact]
    public void RepairsARenditionThatIsNotOneOfTheThree()
    {
        // Repaired to this game's fallback, not the engine's 16-bit, which it has no art for.
        BbConfig config = new();
        config.Engine.Rendition = "Psychedelic";

        bool repaired = config.Repair();

        Assert.True(repaired);
        Assert.Equal(RenditionNames.EightBit, config.Engine.Rendition);
    }

    [Fact]
    public void KeepsARenditionThatIsOneOfTheThree()
    {
        BbConfig config = new();
        config.Engine.Rendition = RenditionNames.Modern;

        config.Repair();

        Assert.Equal(RenditionNames.Modern, config.Engine.Rendition);
    }
}
