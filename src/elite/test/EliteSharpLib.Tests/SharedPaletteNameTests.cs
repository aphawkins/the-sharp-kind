// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Assets;
using SharpKind.Assets.Palettes;

namespace EliteSharpLib.Tests;

// A palette name looked up from shared code is an assertion that every
// rendition defines it, and nothing checks that at compile time - a name only
// one palette has is a crash at startup, on the tier nobody was testing.
//
// Almost all of these went away when the drawing moved into the renditions,
// where a lookup is against that rendition's own palette and cannot be wrong.
// What is left is the handful the game still draws itself, so this holds the
// line at that handful.
public class SharedPaletteNameTests
{
    // Every name EliteSharpLib looks up through IEliteDraw.Palette. Grep for
    // Palette[" in the library to check this is still the whole list.
    private static readonly string[] s_sharedNames = ["White"];

    // The renditions the game ships with. A third would have to define these
    // too, which is the point of keeping the list short.
    public static TheoryData<string> Renditions => ["EightBit", "SixteenBit"];

    [Theory]
    [MemberData(nameof(Renditions))]
    public void EveryRenditionDefinesTheNamesSharedCodeUses(string rendition)
    {
        IPaletteCollection palette = PaletteReader.Read(PaletteOf(rendition));

        string[] missing = [.. s_sharedNames.Where(name => !palette.ContainsKey(name))];

        Assert.Empty(missing);
    }

    // Renditions are free to disagree about everything else: the 8-bit palette is sixteen web colour names against the 16-bit ramp of twenty-nine.
    [Fact]
    public void TheRenditionsDoNotOtherwiseShareAPalette()
    {
        IPaletteCollection eightBit = PaletteReader.Read(PaletteOf("EightBit"));
        IPaletteCollection sixteenBit = PaletteReader.Read(PaletteOf("SixteenBit"));

        Assert.NotEqual(eightBit.Keys.Order(StringComparer.Ordinal), sixteenBit.Keys.Order(StringComparer.Ordinal));
    }

    // A rendition keeps its palette with the rest of its assets, in the folder
    // its assembly was loaded from.
    private static string PaletteOf(string rendition) => AssetLocator
        .CreateFrom(Path.Combine(AppContext.BaseDirectory, "Renditions", $"EliteSharp.Renditions.{rendition}"), rendition)
        .PalettePath;
}
