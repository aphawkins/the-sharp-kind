// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Renditions;
using EliteSharp.Abstractions.Views;
using EliteSharp.Abstractions.Views.Planets;
using EliteSharp.Abstractions.Views.Stars;
using EliteSharp.Abstractions.Views.Suns;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit tier: a 640x512 canvas, a proportional font with no character
/// grid, and a 29-entry colour ramp whose names the 8-bit palette does not
/// share. Every screen here looks up only names this tier's palette defines.
/// </summary>
public sealed class SixteenBitRendition : IRendition
{
    public string Name => "16-bit";

    public int ScreenWidth => 640;

    public int ScreenHeight => 512;

    // Rows of HUD art below where the console's own frame starts.
    public int ConsoleHeight => 128;

    public int DesignScale => 2;

    // 640x512 doubled is 1280x1024; no room above that on a common display.
    public IReadOnlyList<int> WindowScales => [1, 2];

    public int DefaultWindowScale => 2;

    // 4096-colour palette, so a lit face can take any tone the DAC reaches.
    public bool ShadesShips => true;

    public IBaseView CreateBaseView(IViewSurface surface) => new BaseView16Bit(surface);

    public IMissionBriefingView CreateMissionBriefingView(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        return new MissionBriefingView16Bit(surface);
    }

    public ViewSet CreateViews(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        ViewSet views = new ViewSet()
            .Add(new CommanderStatusView16Bit(surface))
            .Add(new CreditsView16Bit(surface))
            .Add(new EquipmentView16Bit(surface))
            .Add(new EscapeCapsuleView16Bit(surface))
            .Add(new GalacticChartView16Bit(surface))
            .Add(new GameOverView16Bit(surface))
            .Add(new Intro1View16Bit(surface))
            .Add(new Intro2View16Bit(surface));

        return AddFlightViews(views, surface);
    }

    public IPlanetRenderer CreatePlanetRenderer(IViewSurface surface, PlanetLook look)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(look);

        return look.Style switch
        {
            PlanetStyle.Wireframe => new WireframePlanetRenderer16Bit(surface, look.HasCrater),
            PlanetStyle.Solid => new SolidPlanetRenderer16Bit(surface),
            PlanetStyle.Striped => new StripedPlanetRenderer16Bit(surface),
            PlanetStyle.Fractal => new FractalPlanetRenderer16Bit(surface, look.Random),
            _ => throw new ArgumentOutOfRangeException(nameof(look)),
        };
    }

    public ISunRenderer CreateSunRenderer(IViewSurface surface, SunLook look)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(look);

        return look.Style switch
        {
            SunStyle.Wireframe => new WireframeSunRenderer16Bit(surface),
            SunStyle.Solid => new SolidSunRenderer16Bit(surface, look.Random),
            SunStyle.Gradient => new GradientSunRenderer16Bit(surface, look.Random),
            _ => throw new ArgumentOutOfRangeException(nameof(look)),
        };
    }

    public IStarfieldRenderer CreateStarfieldRenderer(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        return new StarfieldRenderer16Bit(surface);
    }

    public ShipColours CreateShipColours(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        return Ships(surface);
    }

    public SettingsListStyle CreateSettingsListStyle(IViewSurface surface)
        => SettingsListStyle16Bit.Create(surface);

    public MarketListStyle CreateMarketListStyle(IViewSurface surface)
        => MarketListStyle16Bit.Create(surface);

    public InventoryListStyle CreateInventoryListStyle(IViewSurface surface)
        => InventoryListStyle16Bit.Create(surface);

    // Split from CreateViews to stay under CA1506's coupling limit.
    private static ViewSet AddFlightViews(ViewSet views, IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(views);

        return views
            .Add(new LoadCommanderView16Bit(surface))
            .Add(new OptionsView16Bit(surface))
            .Add(new PilotView16Bit(surface))
            .Add(new PlanetDataView16Bit(surface))
            .Add(new QuitView16Bit(surface))
            .Add(new SaveCommanderView16Bit(surface))
            .Add(new ScannerView16Bit(surface, Ships(surface)))
            .Add(new ShortRangeChartView16Bit(surface));
    }

    // One definition, shared by the scanner and the ship's laser beam, so the two cannot disagree.
    private static ShipColours Ships(IViewSurface surface) => new(
        Default: surface.Palette["White"],
        Station: surface.Palette["Green"],
        Missile: surface.Palette["Lilac"],
        Police: surface.Palette["Purple"],
        Hostile: surface.Palette["Yellow"]);
}
