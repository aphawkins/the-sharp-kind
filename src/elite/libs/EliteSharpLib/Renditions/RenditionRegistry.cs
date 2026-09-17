// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Renditions;
using EliteSharp.Abstractions.Views;
using EliteSharp.Abstractions.Views.Stars;

namespace EliteSharpLib.Renditions;

/// <summary>
/// What a rendition drew, checked against what the game actually has screens
/// for. The check happens once, here, before a frame is composed: a rendition that
/// is a screen short is a startup failure naming the screen rather than a
/// screen that turns out to be blank when the commander opens it.
/// </summary>
internal sealed class RenditionRegistry
{
    // Every screen with a controller of its own, the HUD included. Mission briefing is excluded: the rendition hands it over separately.
    private static readonly Type[] s_requiredModels =
    [
        typeof(CommanderStatusModel),
        typeof(CreditsModel),
        typeof(EquipmentModel),
        typeof(EscapeCapsuleModel),
        typeof(GalacticChartModel),
        typeof(GameOverModel),
        typeof(Intro1Model),
        typeof(Intro2Model),
        typeof(LoadCommanderModel),
        typeof(OptionsModel),
        typeof(PilotModel),
        typeof(PlanetDataModel),
        typeof(QuitModel),
        typeof(SaveCommanderModel),
        typeof(ScannerModel),
        typeof(ShortRangeChartModel),
    ];

    private readonly ViewSet _views;

    internal RenditionRegistry(IRendition rendition, IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(rendition);

        BaseView = rendition.CreateBaseView(surface);
        MissionBriefingView = rendition.CreateMissionBriefingView(surface);
        Starfield = rendition.CreateStarfieldRenderer(surface);
        SettingsListStyle = rendition.CreateSettingsListStyle(surface);
        MarketListStyle = rendition.CreateMarketListStyle(surface);
        InventoryListStyle = rendition.CreateInventoryListStyle(surface);
        _views = rendition.CreateViews(surface);

        string[] missing = [.. s_requiredModels
            .Except(_views.ModelTypes)
            .Select(model => model.Name)
            .Order(StringComparer.Ordinal)];

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"{rendition.Name} draws no screen for: {string.Join(", ", missing)}.");
        }
    }

    /// <summary>
    /// Gets the tier's shared chrome, which every screen draws its border
    /// through and EliteMain the hyperspace countdown.
    /// </summary>
    internal IBaseView BaseView { get; }

    /// <summary>
    /// Gets how the settings screens look in this tier. They have no view of
    /// their own - the game owns their controls, because a setting's value is
    /// not a rendition's to hold - so the tier contributes colours and
    /// positions instead.
    /// </summary>
    internal SettingsListStyle SettingsListStyle { get; }

    /// <summary>
    /// Gets how the market screen looks in this tier. Like the settings
    /// screens it has no view of its own, and for one further reason: the
    /// goods are a plugin, so a rendition declares how many rows it has room
    /// for rather than being written against a known number of them.
    /// </summary>
    internal MarketListStyle MarketListStyle { get; }

    /// <summary>
    /// Gets how the inventory screen looks in this tier. Like the market it has
    /// no view of its own: what a commander can be carrying depends on the
    /// goods set, so the tier declares how many rows it has room for.
    /// </summary>
    internal InventoryListStyle InventoryListStyle { get; }

    /// <summary>
    /// Gets the screen every mission's messages are drawn on.
    /// </summary>
    internal IMissionBriefingView MissionBriefingView { get; }

    /// <summary>
    /// Gets the starfield renderer. It is not a screen - the game draws it
    /// behind whatever screen is showing - so it comes off the rendition
    /// directly rather than out of the set.
    /// </summary>
    internal IStarfieldRenderer Starfield { get; }

    /// <summary>
    /// Gets the view for one screen.
    /// </summary>
    /// <typeparam name="TModel">The model the screen draws.</typeparam>
    /// <returns>The view for that model.</returns>
    internal IView<TModel> View<TModel>() => _views.Get<TModel>();
}
