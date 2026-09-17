// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Ships;
using SharpKind;

namespace EliteSharp.Abstractions.Views;

/// <summary>
/// The laser beams and crosshairs. The geometry is shared - it is already
/// derived from the tier's scale and viewport edges, which is why this sits
/// with the contracts rather than in either rendition - but the beam colours
/// are each tier's own, so those are left to the subclass.
/// <para>
/// It reads no game state: whether the beams are outlined and where they
/// converge both arrive on the model, the same as everything else a view
/// draws.
/// </para>
/// </summary>
public abstract class LaserDrawBase
{
    protected LaserDrawBase(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        Surface = surface;
    }

    /// <summary>
    /// Gets what this draws on.
    /// </summary>
    protected IViewSurface Surface { get; }

    /// <summary>
    /// Draws the two beams converging ahead of the ship.
    /// </summary>
    /// <param name="laserType">The mount that is firing.</param>
    /// <param name="aim">
    /// Where the beams meet, relative to the viewport centre and in the
    /// original's coordinates - the frame's jitter, which the game rolls
    /// rather than the view.
    /// </param>
    /// <param name="wireframe">Whether the beams are outlined rather than filled.</param>
    public void DrawLaserLines(LaserType laserType, Vector2 aim, bool wireframe)
    {
        FastColor color = BeamColor(laserType);
        float scale = Surface.Layout.DesignScale;

        Vector2 target = Surface.Layout.ViewportCentre + (aim * scale);

        Vector2 leftA = new(Surface.Layout.ViewportLeft + (32 * scale), Surface.Layout.ViewportBottom);
        Vector2 leftB = new(Surface.Layout.ViewportLeft + (48 * scale), Surface.Layout.ViewportBottom);

        Vector2 rightA = new(Surface.Layout.ViewportRight - (32 * scale), Surface.Layout.ViewportBottom);
        Vector2 rightB = new(Surface.Layout.ViewportRight - (48 * scale), Surface.Layout.ViewportBottom);

        if (wireframe)
        {
            Surface.Graphics.DrawTriangle(leftA, target, leftB, color);
            Surface.Graphics.DrawTriangle(rightA, target, rightB, color);
        }
        else
        {
            Surface.Graphics.DrawTriangleFilled(leftA, target, leftB, color);
            Surface.Graphics.DrawTriangleFilled(rightA, target, rightB, color);
        }
    }

    /// <summary>
    /// Draws the crosshair sprite for a laser type, centred on the view. Each
    /// type has its own.
    /// </summary>
    /// <param name="laserType">The mount to draw the crosshair for.</param>
    public void DrawLaserSights(LaserType laserType)
    {
        if (laserType == LaserType.None)
        {
            return;
        }

        string image = CrosshairImage(laserType);
        Surface.Graphics.DrawImage(image, Surface.Layout.ViewportCentre - (Surface.Graphics.ImageSize(image) / 2));
    }

    /// <summary>
    /// The beam colour for a laser type. Beam and mining match their crosshair
    /// sprite's shade; pulse and military share the default.
    /// </summary>
    /// <param name="laserType">The mount that is firing.</param>
    /// <returns>The beam colour, in this tier's palette.</returns>
    protected abstract FastColor BeamColor(LaserType laserType);

    private static string CrosshairImage(LaserType laserType) => laserType switch
    {
        LaserType.Pulse => nameof(ImageType.LaserPulse),
        LaserType.Beam => nameof(ImageType.LaserBeam),
        LaserType.Military => nameof(ImageType.LaserMilitary),
        LaserType.Mining => nameof(ImageType.LaserMining),
        _ => throw new ArgumentOutOfRangeException(nameof(laserType)),
    };
}
