// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Renditions;
using EliteSharp.Abstractions.Views.Suns;
using EliteSharpLib.Graphics;
using EliteSharpLib.Ships;
using SharpKind.Graphics.Rendering;

namespace EliteSharpLib.Suns;

internal static class SunFactory
{
    // As PlanetFactory: the game picks the sun style, the rendition draws it.
    internal static IObject Create(GameState gameState, IEliteDraw draw, IRendition rendition, RNG rng)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(rendition);

        SunStyle sunStyle = gameState.Config.Engine.Graphics.FillMode == FillMode.Wireframe
            ? SunStyle.Wireframe
            : gameState.Config.Game.SunStyle switch
            {
                SunType.Solid => SunStyle.Solid,
                SunType.Gradient => SunStyle.Gradient,
                _ => throw new EliteException(),
            };

        // The flaring rim uses the game's one entropy source, handed over rather than replaced.
        return new Sun(draw, rendition.CreateSunRenderer(draw, new(sunStyle, rng)));
    }
}
