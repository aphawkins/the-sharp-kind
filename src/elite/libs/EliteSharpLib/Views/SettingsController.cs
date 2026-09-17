// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharpLib.Config;
using EliteSharpLib.Planets;
using EliteSharpLib.Suns;
using SharpKind.Config;
using SharpKind.Input;
using SharpKind.UI;

namespace EliteSharpLib.Views;

// The "game" half of the config file: how Elite itself looks and plays. The
// shared engine settings have their own screen - see EngineSettingsController.
internal sealed class SettingsController : SettingsListController
{
    internal SettingsController(
        GameState gameState,
        IKeyboard keyboard,
        Space space,
        IConfigWriter<EliteConfig> configWriter,
        IBaseView baseView,
        IViewSurface surface,
        SettingsListStyle style)
        : base(
            gameState,
            keyboard,
            baseView,
            surface,
            style,
            "GAME SETTINGS",
            BuildSettings(gameState, space, configWriter))
    {
    }

    // Each setting names what it stores, how it's shown and what happens when it changes, all in one place.
    private static IReadOnlyList<ISetting> BuildSettings(
        GameState gameState,
        Space space,
        IConfigWriter<EliteConfig> configWriter)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(space);
        ArgumentNullException.ThrowIfNull(configWriter);

        EliteConfig config = gameState.Config;
        void Save() => configWriter.WriteConfig(config);

        // Planet Style is PlanetType, not the rendition's PlanetStyle: whether the world draws solid is the engine's Graphic Style.
        return
        [
            new SavedSetting(
                new EnumSetting<PlanetType>(
                    "Planet Style:",
                    [
                        (PlanetType.Solid, "Solid"),
                        (PlanetType.Striped, "Striped"),
                        (PlanetType.Fractal, "Fractal"),
                    ],
                    () => config.Game.PlanetStyle,
                    value =>
                    {
                        config.Game.PlanetStyle = value;
                        space.RefreshPlanetStyle();
                    }),
                Save),
            new SavedSetting(
                new EnumSetting<SunType>(
                    "Sun Style:",
                    [(SunType.Solid, "Solid"), (SunType.Gradient, "Gradient")],
                    () => config.Game.SunStyle,
                    value =>
                    {
                        config.Game.SunStyle = value;
                        space.RefreshSunStyle();
                    }),
                Save),
            new SavedSetting(
                new EnumSetting<PlanetDescriptions>(
                    "Planet Desc.:",
                    [(PlanetDescriptions.TreeGrubs, "BBC"), (PlanetDescriptions.HoopyCasinos, "MSX")],
                    () => config.Game.PlanetDescriptions,
                    value => config.Game.PlanetDescriptions = value),
                Save),
            new SavedSetting(
                new ToggleSetting(
                    "Instant Dock:",
                    "Off",
                    "On",
                    () => config.Game.InstantDock,
                    value => config.Game.InstantDock = value),
                Save),
        ];
    }
}
