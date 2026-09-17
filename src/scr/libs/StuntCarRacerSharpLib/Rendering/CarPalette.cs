// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind;
using SharpKind.Assets.Palettes;
using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Rendering;

// Track.ScrBaseColour is currently a fixed offset, so this is safe to resolve once, at CarMesh construction time.
internal static class CarPalette
{
    private const int WheelOffset = 0;
    private const int BottomOffset = 9;
    private const int EndOffset = 10;
    private const int SideOffset = 12;
    private const int TopOffset = 15;

    internal static IPaletteCollection Colours(ScrPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);

        return new Palette(new Dictionary<string, FastColor>
        {
            ["Wheel"] = palette.Colour(Track.ScrBaseColour + WheelOffset),
            ["Bottom"] = palette.Colour(Track.ScrBaseColour + BottomOffset),
            ["End"] = palette.Colour(Track.ScrBaseColour + EndOffset),
            ["Side"] = palette.Colour(Track.ScrBaseColour + SideOffset),
            ["Top"] = palette.Colour(Track.ScrBaseColour + TopOffset),
        });
    }
}
