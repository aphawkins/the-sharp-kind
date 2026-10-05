// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;

namespace SharpKind.Graphics;

/// <summary>
/// One band of a frame: a clip rectangle, and the drawing that goes into it.
/// <para>
/// Layers are drawn furthest first, so a later layer covers an earlier one.
/// The rectangle is the layer's own, not the screen's: a game draws its
/// backdrop and its universe into the opening its cockpit art leaves, and
/// its cockpit over the whole screen.
/// </para>
/// </summary>
/// <param name="Position">The top-left corner of the clip rectangle.</param>
/// <param name="Width">The clip rectangle's width in pixels.</param>
/// <param name="Height">The clip rectangle's height in pixels.</param>
/// <param name="Drawer">The drawing that goes into the rectangle.</param>
public sealed record RenderLayer(Vector2 Position, float Width, float Height, ILayerDrawer Drawer)
{
    /// <summary>
    /// Gets how far the layer's drawing is moved this frame, asked each time it is drawn, or null for not at all. The clip
    /// rectangle stays where it is, so drawing moved out of it is cut off.
    /// </summary>
    public Func<Vector2>? Offset { get; init; }
}
