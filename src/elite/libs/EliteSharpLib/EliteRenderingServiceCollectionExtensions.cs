// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Renditions;
using EliteSharpLib.Graphics;
using EliteSharpLib.Ships;
using Microsoft.Extensions.DependencyInjection;
using SharpKind.Assets;
using SharpKind.Graphics;
using SharpKind.Graphics.Rendering;

namespace EliteSharpLib;

// Split out of EliteServiceCollectionExtensions once EliteDraw's ScreenLayout pushed that class over CA1506's class-coupling limit.
internal static class EliteRenderingServiceCollectionExtensions
{
    internal static void AddEliteRendering(this IServiceCollection services)
    {
        services.AddSingleton<IPolygonRenderer>(sp => new ConfigPolygonRenderer(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IGraphics>(),
            sp.GetRequiredService<IAssetLocator>()));

        // The drawing's own entropy, registered beside the surface that
        // carries it and deliberately not the game's stream. See
        // RenderRandom.
        services.AddSingleton(_ => new RenderRandom(new()));
        services.AddSingleton<IEliteDraw>(sp => new EliteDraw(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IGraphics>(),
            sp.GetRequiredService<ScreenLayout>(),
            sp.GetRequiredService<IAssetLocator>(),
            sp.GetRequiredService<IRendition>(),
            sp.GetRequiredService<IPolygonRenderer>(),
            sp.GetRequiredService<RenderRandom>()));
        services.AddSingleton<IShipFactory>(sp => ShipFactory.Create(
            sp.GetRequiredService<IAssetLocator>(),
            sp.GetRequiredService<IEliteDraw>(),
            sp.GetRequiredService<RNG>()));
    }
}
