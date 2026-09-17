// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Renditions;
using EliteSharp.Abstractions.Views;
using EliteSharp.Abstractions.Views.Planets;
using EliteSharp.Abstractions.Views.Stars;
using EliteSharp.Abstractions.Views.Suns;
using EliteSharpLib.Fakes;
using EliteSharpLib.Renditions;
using SharpKind.Abstraction.Renditions;
using SharpKind.UI;

namespace EliteSharpLib.Tests.Views;

public class RenditionRegistryTests
{
    [Fact]
    public void RefusesAPackThatIsAScreenShort()
    {
        // Arrange: a rendition that draws everything but the equip-ship
        // screen. The commander must not find that out by opening it.
        ShortRendition rendition = new(typeof(EquipmentModel));

        // Act & Assert
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => new RenditionRegistry(rendition, new FakeEliteDraw()));

        Assert.Contains(nameof(EquipmentModel), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NamesEveryMissingScreenAtOnce()
    {
        // Arrange: naming one at a time would mean starting the game once per
        // missing screen to find out what is needed.
        ShortRendition rendition = new(typeof(EquipmentModel), typeof(QuitModel));

        // Act
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => new RenditionRegistry(rendition, new FakeEliteDraw()));

        // Assert
        Assert.Contains(nameof(EquipmentModel), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(QuitModel), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HandsBackTheViewTheScreenAskedFor()
    {
        // Arrange
        RenditionRegistry registry = new(new ShortRendition(), new FakeEliteDraw());

        // Act
        IView<EquipmentModel> view = registry.View<EquipmentModel>();

        // Assert
        Assert.NotNull(view);
    }

    [Fact]
    public void RefusesTwoViewsForOneScreen()
    {
        // Arrange: one of the two would never draw, so it is a mistake rather
        // than an override.
        ViewSet views = new();
        views.Add(new NothingView<EquipmentModel>());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => views.Add(new NothingView<EquipmentModel>()));
    }

    // Draws every screen except the ones it was told to leave out.
    private sealed class ShortRendition(params Type[] omitted) : IRendition
    {
        // Modern, because it is the one of the three no shipped rendition
        // claims - this stub is standing in for a rendition, not for either
        // of Elite's.
        public Rendition Rendition => Rendition.Modern;

        public int ScreenWidth => 320;

        public int ScreenHeight => 256;

        public int ConsoleHeight => 56;

        public int DesignScale => 1;

        public IBaseView CreateBaseView(IViewSurface surface) => new NothingBaseView();

        public IMissionBriefingView CreateMissionBriefingView(IViewSurface surface) => new NothingBriefingView();

        public IPlanetRenderer CreatePlanetRenderer(IViewSurface surface, PlanetLook look)
            => throw new NotSupportedException();

        public ISunRenderer CreateSunRenderer(IViewSurface surface, SunLook look)
            => throw new NotSupportedException();

        public IStarfieldRenderer CreateStarfieldRenderer(IViewSurface surface)
            => new NothingStarfield();

        public ShipColours CreateShipColours(IViewSurface surface)
            => new(default, default, default, default, default);

        public SettingsListStyle CreateSettingsListStyle(IViewSurface surface)
        {
            ControlColors nothing = new(default, default);
            ControlStyle style = new("Small", nothing, nothing, nothing);

            return new(style, style, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        public MarketListStyle CreateMarketListStyle(IViewSurface surface)
        {
            ControlColors nothing = new(default, default);
            ControlStyle style = new("Small", nothing, nothing, nothing);

            return new(style, style, [], [], 0, 0, 0, 0, 0, default, default);
        }

        public InventoryListStyle CreateInventoryListStyle(IViewSurface surface)
        {
            ControlColors nothing = new(default, default);
            ControlStyle style = new("Small", nothing, nothing, nothing);

            return new(style, style, [], default, default, default, default, 0, 0, 0, 0, 0);
        }

        public ViewSet CreateViews(IViewSurface surface)
        {
            ViewSet views = new();
            Add<CommanderStatusModel>(views);
            Add<CreditsModel>(views);
            Add<EquipmentModel>(views);
            Add<EscapeCapsuleModel>(views);
            Add<GalacticChartModel>(views);
            Add<GameOverModel>(views);
            Add<Intro1Model>(views);
            Add<Intro2Model>(views);
            Add<LoadCommanderModel>(views);
            Add<OptionsModel>(views);
            Add<PilotModel>(views);
            Add<PlanetDataModel>(views);
            Add<QuitModel>(views);
            Add<SaveCommanderModel>(views);
            Add<ScannerModel>(views);
            Add<ShortRangeChartModel>(views);

            return views;
        }

        private void Add<TModel>(ViewSet views)
        {
            if (!omitted.Contains(typeof(TModel)))
            {
                views.Add(new NothingView<TModel>());
            }
        }
    }

    private sealed class NothingStarfield : IStarfieldRenderer
    {
        public int NormalSpaceStarCount => 18;

        public int WitchspaceStarCount => 3;

        public void Draw(IReadOnlyList<StarMark> stars)
        {
        }
    }

    private sealed class NothingView<TModel> : IView<TModel>
    {
        public void Draw(TModel model)
        {
        }
    }

    private sealed class NothingBriefingView : IMissionBriefingView
    {
        public Vector4 ShipLocation => Vector4.Zero;

        public void Draw(MissionBriefingModel model)
        {
        }
    }

    private sealed class NothingBaseView : IBaseView
    {
        public SharpKind.Graphics.IGraphics Graphics => throw new NotSupportedException();

        public ViewLayout Layout => throw new NotSupportedException();

        public void DrawFps(int fps)
        {
        }

        public void DrawHyperspaceCountdown(int countdown)
        {
        }

        public void DrawInfoMessage(string message)
        {
        }

        public void DrawTextPretty(Vector2 position, float width, string text)
        {
        }

        public void DrawViewHeader(string title)
        {
        }
    }
}
