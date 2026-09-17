// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views.Stars;
using EliteSharpLib.Fakes;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests;

public class StarsTests
{
    [Fact]
    public void NormalSpaceSimulatesTheRenditionsOwnStarCount()
    {
        // Each rendition tunes its own star count for its own resolution; the fake renderer's 27 stands in.
        Stars stars = CreateStars(out FakeStarfieldRenderer renderer, normalSpaceStarCount: 27);

        stars.CreateNewStars();
        stars.FrontStarfield();

        // The starfield pass moves stars and collects marks; Draw hands them to the rendition.
        stars.Draw();

        Assert.Equal(27, renderer.LastDrawCount);
    }

    [Fact]
    public void WitchspaceSimulatesTheRenditionsOwnStarCount()
    {
        // Original NOSTM: 18 particles in normal space, dropped to 3 in witchspace for a visibly emptier void.
        Stars stars = CreateStars(out FakeStarfieldRenderer renderer, witchspaceStarCount: 5);

        stars.CreateNewWitchspaceStars();
        stars.FrontStarfield();
        stars.Draw();

        Assert.Equal(5, renderer.LastDrawCount);
    }

    // Recycle bounds are in star space, sized by the focal length. A wide field of view shortens
    // the focal length and stretches the view further across star space, so fixed bounds fell
    // inside the view once it widened, leaving an empty band top and bottom.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RearAndSideStarfieldsFillAWidenedView(bool rear)
    {
        // Half the classic focal length: the view reaches 191 units either side of centre.
        const int frames = 200;
        const int starCount = 64;

        Stars stars = CreateStars(
            out FakeStarfieldRenderer renderer,
            out PlayerShip ship,
            normalSpaceStarCount: starCount,
            focus: 256);

        ship.Speed = 20;
        stars.CreateNewStars();

        for (int i = 0; i < frames; i++)
        {
            if (rear)
            {
                stars.RearStarfield();
            }
            else
            {
                stars.RightStarfield();
            }

            stars.Draw();
        }

        // Pins the fix: nearly every star should be on screen most frames. Fixed bounds thinned
        // the field (93%/69% surviving) since a star recycled to an edge inside the view spent
        // its life off-screen; matching the focal length restores ~98% for both.
        Assert.True(
            renderer.AllMarks.Count > (int)(frames * starCount * 0.95),
            $"only {renderer.AllMarks.Count} of {frames * starCount} star marks were drawn");

        // And nothing is drawn outside the view itself.
        Assert.All(renderer.AllMarks, mark => Assert.InRange(mark.Position.Y, 0, 383));
    }

    private static Stars CreateStars(
        out FakeStarfieldRenderer renderer,
        int normalSpaceStarCount = 18,
        int witchspaceStarCount = 3)
        => CreateStars(out renderer, out _, normalSpaceStarCount, witchspaceStarCount);

    private static Stars CreateStars(
        out FakeStarfieldRenderer renderer,
        out PlayerShip ship,
        int normalSpaceStarCount = 18,
        int witchspaceStarCount = 3,
        float focus = 512)
    {
        // A real stream, not the fixed fake: a recycled star's reappearance point has to vary, or they all stack on one spot.
        FakeEliteDraw draw = new() { Focus = focus, Jitter = new RenderRandom(new Random(1)) };
        renderer = new(normalSpaceStarCount, witchspaceStarCount);

        ScreenManager<Screen, IScreenController> views = new(new FakeKeyboard());
        GameState gameState = new(views, TestMissions.Registry());
        ship = new(gameState);
        return new(gameState, draw, ship, renderer);
    }

    private sealed class FakeStarfieldRenderer(int normalSpaceStarCount, int witchspaceStarCount) : IStarfieldRenderer
    {
        public int NormalSpaceStarCount { get; } = normalSpaceStarCount;

        public int WitchspaceStarCount { get; } = witchspaceStarCount;

        internal int LastDrawCount { get; private set; }

        internal List<StarMark> AllMarks { get; } = [];

        public void Draw(IReadOnlyList<StarMark> stars)
        {
            LastDrawCount = stars.Count;
            AllMarks.AddRange(stars);
        }
    }
}
