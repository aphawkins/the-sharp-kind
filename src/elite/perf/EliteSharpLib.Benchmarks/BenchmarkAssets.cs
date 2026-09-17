// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Renditions;
using SharpKind.Assets;

namespace EliteSharpLib.Benchmarks;

// A bare FakeAssetLocator has no images, so constructing EliteDraw against it throws looking up "Hud"; benchmarks build the real composition instead.
internal static class BenchmarkAssets
{
    private const string Rendition = "16-bit";

    // Where the benchmark project's build drops the rendition, which is the
    // same arrangement the app ships.
    private static readonly string s_renditionFolder =
        Path.Combine("Renditions", "EliteSharp.Renditions.SixteenBit");

    internal static IAssetLocator Locator() => new RenditionAssets(
        AssetLocator.CreateFrom(Path.Combine(AppContext.BaseDirectory, s_renditionFolder), Rendition),
        AssetLocator.Create(Rendition));
}
