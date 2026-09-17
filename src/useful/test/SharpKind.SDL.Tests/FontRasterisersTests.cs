// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Graphics;

namespace SharpKind.SDL.Tests;

// Loading the kinds themselves needs assets on disk and an initialised SDL_ttf, so this checks only the rule that decides whether a kind is offered.
public class FontRasterisersTests
{
    // Declaring nothing for a kind is a choice - the sheets stand in for it.
    [Fact]
    public void AcceptsAKindDeclaredForEveryFontTypeTheSheetsCover()
        => FontRasterisers.RequireEveryFontType(
            ["Small", "Large"],
            ["Small", "Large"],
            FontKind.Fon,
            "Test");

    // Extra types are the rendition's business: the game only asks for the
    // ones its sheets name.
    [Fact]
    public void AcceptsAKindDeclaringMoreThanTheSheetsCover()
        => FontRasterisers.RequireEveryFontType(
            ["Small"],
            ["Small", "Large"],
            FontKind.TrueType,
            "Test");

    // Declaring some of a kind is a mistake, and one that would otherwise
    // wait until a screen drew text in the type left out.
    [Fact]
    public void RefusesAKindMissingAFontTypeTheSheetsCover()
    {
        // Act
        SharpKindException exception = Assert.Throws<SharpKindException>(
            () => FontRasterisers.RequireEveryFontType(["Small", "Large"], ["Small"], FontKind.Fon, "8-bit"));

        // Assert - the message names the rendition, the kind and what is missing.
        Assert.Contains("8-bit", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Fon", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Large", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NamesEveryMissingFontType()
    {
        SharpKindException exception = Assert.Throws<SharpKindException>(
            () => FontRasterisers.RequireEveryFontType(["Small", "Large"], [], FontKind.TrueType, "16-bit"));

        Assert.Contains("Large, Small", exception.Message, StringComparison.Ordinal);
    }
}
