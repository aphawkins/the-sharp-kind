// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using SharpKind.Graphics;

namespace EliteSharp.Abstractions.Views;

/// <summary>
/// The tier's screen metrics, in one place, for the views to lay out against.
/// Everything but the four inputs is derived rather than stored: a screen
/// size, the console band's height and the coordinate scale determine the rest, so
/// no two of these can disagree.
/// <para>
/// There is one region here, the <b>viewport</b>: everything above the HUD,
/// where the universe and every screen's content is drawn. It starts at the
/// screen's own origin, so a view's position is its position - no border
/// inset and no HUD edge enter the arithmetic, and the 8-bit tier's viewport
/// is exactly 40 by 25 of its 8x8 character cells. The border is drawn over
/// the viewport's outer edge rather than reserved out of it, so changing the
/// border's width never moves a single view.
/// </para>
/// </summary>
/// <param name="ScreenWidth">The render width in pixels.</param>
/// <param name="ScreenHeight">The render height in pixels.</param>
/// <param name="ConsoleHeight">
/// The height of the console band at the foot of the screen - the dials,
/// the radar and the compass - and the only place the HUD enters the layout:
/// it is what the viewport stops short of. Stated by the rendition rather
/// than measured off the HUD art, which is a full-screen overlay and so has
/// nothing to measure (the 8-bit band is 56 pixels against the 16-bit 128).
/// </param>
/// <param name="DesignScale">
/// The design-space scale: chrome positions and sizes are authored in the
/// original's 256x256-ish space, and this is what multiplies them up to
/// this rendition's pixels. It is a unit conversion, not a transform - it
/// never enters the projection, which has its own focal length.
/// Kept whole, per the pixel-doubling rule in docs/decisions.md - a
/// fractional value would put HUD text and ship vertices on half-pixels.
/// </param>
public sealed record ViewLayout(float ScreenWidth, float ScreenHeight, float ConsoleHeight, float DesignScale)
    : ScreenLayout(ScreenWidth, ScreenHeight)
{
    // Left/Top/Right/Bottom are inclusive pixel bounds - Right and Bottom are
    // the last pixel that is still inside - while Width/Height are the extents
    // one past them, which is what a clip region and a rectangle want.
    // The viewport starts at the screen's own origin - that is the whole
    // point of it - so these are fixed rather than derived.
    public float ViewportLeft { get; }

    public float ViewportTop { get; }

    public float ViewportWidth => ScreenWidth;

    public float ViewportHeight => ScreenHeight - ConsoleHeight;

    public float ViewportRight => ViewportWidth - 1;

    public float ViewportBottom => ViewportHeight - 1;

    public Vector2 ViewportCentre => new(ViewportWidth / 2, ViewportHeight / 2);
}
