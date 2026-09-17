// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharp.Abstractions.Views.Stars;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The starfield in this rendition's white, with the original's threshold for
/// a star being near enough to draw a pixel wider. At this tier's higher
/// resolution a flat white square reads as a blocky dot rather than a bright
/// point, so the growing pixels around a near star's core are drawn a shade
/// dimmer than the core itself - a soft halo standing in for true
/// anti-aliasing, which the shared <see cref="SharpKind.Graphics.IGraphics"/>
/// pixel API has no alpha-blended path for.
/// </summary>
internal sealed class StarfieldRenderer16Bit : StarfieldRendererBase
{
    internal StarfieldRenderer16Bit(IViewSurface surface)
        : base(surface)
    {
        Colour = surface.Palette["White"];
        HaloColour = surface.Palette["LighterGrey"];
    }

    // Denser than the 8-bit tier's 18; that sparse looks empty over four times the area.
    public override int NormalSpaceStarCount => 55;

    public override int WitchspaceStarCount => 9;

    protected override FastColor Colour { get; }

    protected override FastColor HaloColour { get; }

    protected override float WideDistance => 192;

    protected override float BlockDistance => 144;
}
