// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Renditions;
using Microsoft.Extensions.DependencyInjection;
using SharpKind.Assets;

namespace EliteSharpLib;

// Kept in its own class for the same reason the screen registrations are: CA1506's class-coupling
// limit on EliteServiceCollectionExtensions. Public because the app calls it directly, before the container exists.
public static class EliteRenditionAssetsServiceCollectionExtensions
{
    // Registered over the one AddGameEngine put there, which knows only about the executable's own Assets folder.
    public static IServiceCollection AddRenditionAssets(this IServiceCollection services, InstalledRenditions renditions)
    {
        ArgumentNullException.ThrowIfNull(renditions);

        return services.AddSingleton<IAssetLocator>(_ => new RenditionAssets(
            AssetLocator.CreateFrom(renditions.Folder, renditions.Chosen.Name),
            AssetLocator.Create(renditions.Chosen.Name)));
    }
}
