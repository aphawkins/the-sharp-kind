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
public sealed record RenderLayer(Vector2 Position, float Width, float Height, ILayerDrawer Drawer);
