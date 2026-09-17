// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Renditions;
using EliteSharp.Abstractions.Views;
using EliteSharp.Abstractions.Views.Planets;
using EliteSharp.Abstractions.Views.Stars;
using EliteSharp.Abstractions.Views.Suns;
using SharpKind.Abstraction.Renditions;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit tier: a 320x256 canvas, a fixed 8x8 font and sixteen colours
/// under their web names. Every screen is authored fresh for that canvas
/// rather than scaled down from the 16-bit one, so this rendition shares no layout
/// with the other and needs none.
/// </summary>
public sealed class EightBitRendition : IRendition
{
    public Rendition Rendition => Rendition.EightBit;

    public int ScreenWidth => 320;

    public int ScreenHeight => 256;

    // The bottom 56 rows of the HUD art, which leaves the viewport a whole
    // 40 by 25 of this tier's 8x8 character cells.
    public int ConsoleHeight => 56;

    public int DesignScale => 1;

    // Quadrupled (1280x1024) is the largest window a common display still shows whole.
    public IReadOnlyList<int> WindowScales => [1, 2, 4];

    public int DefaultWindowScale => 4;

    // Shading snaps to the nearest of sixteen colours; the six greys give a real ramp, reds and blues far fewer steps.
    public bool ShadesShips => true;

    public IBaseView CreateBaseView(IViewSurface surface) => new BaseView8Bit(surface);

    public IMissionBriefingView CreateMissionBriefingView(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        return new MissionBriefingView8Bit(surface);
    }

    public ViewSet CreateViews(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        ViewSet views = new ViewSet()
            .Add(new CommanderStatusView8Bit(surface))
            .Add(new CreditsView8Bit(surface))
            .Add(new EquipmentView8Bit(surface))
            .Add(new EscapeCapsuleView8Bit(surface))
            .Add(new GalacticChartView8Bit(surface))
            .Add(new GameOverView8Bit(surface))
            .Add(new Intro1View8Bit(surface))
            .Add(new Intro2View8Bit(surface));

        return AddFlightViews(views, surface);
    }

    public IPlanetRenderer CreatePlanetRenderer(IViewSurface surface, PlanetLook look)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(look);

        return look.Style switch
        {
            PlanetStyle.Wireframe => new WireframePlanetRenderer8Bit(surface, look.HasCrater),
            PlanetStyle.Solid => new SolidPlanetRenderer8Bit(surface),
            PlanetStyle.Striped => new StripedPlanetRenderer8Bit(surface),
            PlanetStyle.Fractal => new FractalPlanetRenderer8Bit(surface, look.Random),
            _ => throw new ArgumentOutOfRangeException(nameof(look)),
        };
    }

    public ISunRenderer CreateSunRenderer(IViewSurface surface, SunLook look)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(look);

        return look.Style switch
        {
            SunStyle.Wireframe => new WireframeSunRenderer8Bit(surface),
            SunStyle.Solid => new SolidSunRenderer8Bit(surface, look.Random),
            SunStyle.Gradient => new GradientSunRenderer8Bit(surface, look.Random),
            _ => throw new ArgumentOutOfRangeException(nameof(look)),
        };
    }

    public IStarfieldRenderer CreateStarfieldRenderer(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        return new StarfieldRenderer8Bit(surface);
    }

    public ShipColours CreateShipColours(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        return Ships(surface);
    }

    public SettingsListStyle CreateSettingsListStyle(IViewSurface surface)
        => SettingsListStyle8Bit.Create(surface);

    public MarketListStyle CreateMarketListStyle(IViewSurface surface)
        => MarketListStyle8Bit.Create(surface);

    public InventoryListStyle CreateInventoryListStyle(IViewSurface surface)
        => InventoryListStyle8Bit.Create(surface);

    // Split from CreateViews to keep each method under CA1506's coupling
    // limit, which naming every screen in one place goes past.
    private static ViewSet AddFlightViews(ViewSet views, IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(views);

        return views
            .Add(new LoadCommanderView8Bit(surface))
            .Add(new OptionsView8Bit(surface))
            .Add(new PilotView8Bit(surface))
            .Add(new PlanetDataView8Bit(surface))
            .Add(new QuitView8Bit(surface))
            .Add(new SaveCommanderView8Bit(surface))
            .Add(new ScannerView8Bit(surface, Ships(surface)))
            .Add(new ShortRangeChartView8Bit(surface));
    }

    // Shared by the scanner and the beam a ship fires, so the two agree.
    // Police are cyan not purple here - this palette's one purple is already the missile.
    private static ShipColours Ships(IViewSurface surface) => new(
        Default: surface.Palette["White"],
        Station: surface.Palette["Green"],
        Missile: surface.Palette["Purple"],
        Police: surface.Palette["Cyan"],
        Hostile: surface.Palette["Yellow"]);
}
