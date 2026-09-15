// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using SharpKind.Graphics;

namespace EliteSharp.Abstractions.Views;

/// <summary>
/// The chrome and text helpers every screen shares, in a tier-specific
/// implementation. Nothing that puts pixels on screen lives in a single
/// class serving both tiers: each tier authors its own spacing, fonts and
/// colours, so these are per-tier too.
/// </summary>
public interface IBaseView
{
    public IGraphics Graphics { get; }

    public ViewLayout Layout { get; }

    /// <summary>
    /// Draws the measured frame rate in the top-right, opposite the
    /// hyperspace countdown.
    /// </summary>
    public void DrawFps(int fps);

    /// <summary>
    /// Draws the hyperspace countdown digit in the top-left.
    /// </summary>
    public void DrawHyperspaceCountdown(int countdown);

    /// <summary>
    /// Draws the transient in-flight message, low and centred.
    /// </summary>
    public void DrawInfoMessage(string message);

    /// <summary>
    /// Word-wraps <paramref name="text"/> within the given width.
    /// </summary>
    public void DrawTextPretty(Vector2 position, float width, string text);

    /// <summary>
    /// Draws a screen's title and the rules framing the view area.
    /// </summary>
    public void DrawViewHeader(string title);
}
