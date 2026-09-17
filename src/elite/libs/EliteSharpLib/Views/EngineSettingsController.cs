// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Globalization;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Config;
using EliteSharpLib.Graphics;
using EliteSharpLib.Renditions;
using SharpKind.Abstraction;
using SharpKind.Audio;
using SharpKind.Config;
using SharpKind.Graphics;
using SharpKind.Graphics.Rendering;
using SharpKind.Input;
using SharpKind.UI;

namespace EliteSharpLib.Views;

// The non-Elite settings; the game's own are on SettingsController.
internal sealed class EngineSettingsController : SettingsListController
{
    // The original's 2*atan(0.5), rounded for display; selecting it clears the setting so the projection stays exact.
    private const int ClassicFieldOfView = 53;

    // Not a device name, so it can never collide with one a driver reports.
    private const string KeyboardOnly = "Keyboard only";

    internal EngineSettingsController(
        GameState gameState,
        IKeyboard keyboard,
        Space space,
        AudioController audio,
        IConfigWriter<EliteConfig> configWriter,
        InstalledRenditions renditions,
        IGamepad gamepad,
        IBaseView baseView,
        IEliteDraw draw,
        SettingsListStyle style)
        : base(
            gameState,
            keyboard,
            baseView,
            draw,
            style,
            "ENGINE SETTINGS",
            BuildSettings(gameState, space, audio, configWriter, renditions, gamepad, draw),
            "* Applies when the game is restarted")
    {
    }

    private static IReadOnlyList<ISetting> BuildSettings(
        GameState gameState,
        Space space,
        AudioController audio,
        IConfigWriter<EliteConfig> configWriter,
        InstalledRenditions renditions,
        IGamepad gamepad,
        IEliteDraw draw)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(space);
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(configWriter);
        ArgumentNullException.ThrowIfNull(renditions);
        ArgumentNullException.ThrowIfNull(draw);

        EliteConfig config = gameState.Config;
        void Save() => configWriter.WriteConfig(config);

        return
        [
            new SavedSetting(
                new EnumSetting<FillMode>(
                    "Fill Mode:",
                    [(FillMode.Wireframe, "Wireframe"), (FillMode.Solid, "Solid")],
                    () => config.Engine.Graphics.FillMode,
                    value =>
                    {
                        config.Engine.Graphics.FillMode = value;

                        // Planet and sun styles only apply in a solid world, so rebuild both when this flips.
                        space.RefreshPlanetStyle();
                        space.RefreshSunStyle();
                    }),
                Save),
            new SavedSetting(
                new EnumSetting<DepthSort>(
                    "Depth Sort:",
                    [(DepthSort.Painter, "Painter"), (DepthSort.ZBuffer, "ZBuffer")],
                    () => config.Engine.Graphics.DepthSort,
                    value => config.Engine.Graphics.DepthSort = value),
                Save),

            // Shown even when the rendition doesn't shade; hiding a set row would be dishonest.
            new SavedSetting(
                new EnumSetting<ShadingModelKind>(
                    "Shading:",
                    [
                        (ShadingModelKind.Unlit, "Unlit"),
                        (ShadingModelKind.Lambert, "Lambert"),
                        (ShadingModelKind.Gouraud, "Gouraud"),
                    ],
                    () => config.Engine.Graphics.Shading,
                    value => config.Engine.Graphics.Shading = value),
                Save),
            new SavedSetting(
                new EnumSetting<Quantisation>(
                    "Quantisation:",
                    [(Quantisation.Nearest, "Nearest"), (Quantisation.Ordered, "Ordered")],
                    () => config.Engine.Graphics.Quantisation,
                    value => config.Engine.Graphics.Quantisation = value),
                Save),

            // No asterisk: every kind was loaded at launch, so the switch shows next frame.
            new SavedSetting(
                new EnumSetting<FontKind>(
                    "Font:",
                    [
                        (FontKind.Bitmap, "Bitmap"),
                        (FontKind.Fon, "FON"),
                        (FontKind.TrueType, "TrueType"),
                    ],
                    () => config.Engine.Graphics.FontKind,
                    value =>
                    {
                        config.Engine.Graphics.FontKind = value;
                        draw.Graphics.FontKind = value;
                    }),
                Save),
            new SavedSetting(
                new ToggleSetting(
                    "Music:",
                    "Off",
                    "On",
                    () => config.Engine.Sound.Music,
                    value =>
                    {
                        config.Engine.Sound.Music = value;
                        audio.MusicOn = value;

                        // Silence what's already playing rather than leaving it running until the next screen change.
                        if (!value)
                        {
                            audio.StopMusic();
                        }
                    }),
                Save),
            new SavedSetting(
                new ToggleSetting(
                    "Effects:",
                    "Off",
                    "On",
                    () => config.Engine.Sound.Effects,
                    value =>
                    {
                        config.Engine.Sound.Effects = value;
                        audio.EffectsOn = value;
                    }),
                Save),

            // Both read before the game is built, so they're saved now and taken up on next launch.
            new SavedSetting(
                new EnumSetting<Backend>(
                    "Backend *:",
                    [(Backend.Software, "Software"), (Backend.Hardware, "Hardware")],
                    () => config.Engine.Backend,
                    value => config.Engine.Backend = value),
                Save),

            // Scales read live off the Rendition row below, not renditions.Chosen, since a Rendition
            // change doesn't take effect until restart and this row would otherwise offer stale scales.
            new SavedSetting(
                new NumberSetting(
                    "Window Scale *:",
                    () => renditions.Find(config.Engine.Rendition).WindowScales,
                    scale => scale.ToString(CultureInfo.InvariantCulture) + "x",
                    () => config.Engine.WindowScale ?? renditions.Find(config.Engine.Rendition).DefaultWindowScale,
                    value => config.Engine.WindowScale = value),
                Save),

            // 53 is the original's own projection (2*atan(0.5) = 53.13deg); selects null so the
            // classic view stays exact. Bare degrees: the bitmap fonts carry no degree glyph.
            new SavedSetting(
                new NumberSetting(
                    "Field of View:",
                    () => [53, 65, 75, 90, 105],
                    fov => fov.ToString(CultureInfo.InvariantCulture),
                    () => config.Engine.FieldOfView ?? ClassicFieldOfView,
                    value => config.Engine.FieldOfView = value == ClassicFieldOfView ? null : value),
                Save),

            // List read live, since a commander can plug a stick in while this screen is up.
            new SavedSetting(
                new ChoiceSetting(
                    "Controller:",
                    () => [KeyboardOnly, .. gamepad.AttachedDevices],
                    () => string.IsNullOrWhiteSpace(config.Engine.ActiveController)
                        ? KeyboardOnly
                        : config.Engine.ActiveController,
                    value => config.Engine.ActiveController
                        = string.Equals(value, KeyboardOnly, StringComparison.Ordinal) ? string.Empty : value),
                Save),

            // Offers only installed renditions, by the name each calls itself.
            new SavedSetting(
                new ChoiceSetting(
                    "Rendition *:",
                    renditions.Names,
                    () => config.Engine.Rendition,
                    value => config.Engine.Rendition = value),
                Save),
        ];
    }
}
