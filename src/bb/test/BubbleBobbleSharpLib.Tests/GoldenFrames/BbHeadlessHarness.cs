// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Renditions.EightBit;
using BubbleBobbleSharpLib.Fakes;
using BubbleBobbleSharpLib.Levels;
using SharpKind.Assets;
using SharpKind.Fakes.Harness;
using SharpKind.Fakes.Input;

namespace BubbleBobbleSharpLib.Tests.GoldenFrames;

// Drives the real BubbleBobbleMain against a real SoftwareGraphics with no SDL window, on the shipped 8-bit rendition's real
// artwork, as the tests' build output lays it out. The seed replaces the game's shared Random, which every roll of the level's
// enemies and the bubbles' drift draws from, so that no two runs would otherwise compose the same frame.
internal sealed class BbHeadlessHarness : HeadlessGameHarnessBase<int>
{
    private const string Rendition = "BubbleBobbleSharp.Renditions.EightBit";

    public BbHeadlessHarness(int level, int randomSeed)
        : base(320, 200, AssetLocator.CreateFrom(RenditionFolder(), Rendition))
    {
        FakeAbstraction abstraction = new(Graphics, new(320, 200));
        Keyboard = (FakeKeyboard)abstraction.Keyboard;

        string assets = Path.Combine(RenditionFolder(), "Assets", "Levels");

        Game = new(
            abstraction,
            AssetLocator.CreateFrom(RenditionFolder(), Rendition),
            new EightBitRendition(),
            LevelStore.Read(Path.Combine(assets, "levels.json")),
            ZoneStore.Read(Path.Combine(assets, "zones.json")),
            new(),
            new Random(randomSeed));

        Game.ShowLevel(level);
        Game.Play();
    }

    public BubbleBobbleMain Game { get; }

    public override int State => Game.CurrentLevel;

    protected override void UpdateGame() => Game.Update();

    protected override void DrawGame() => Game.Draw();

    private static string RenditionFolder() => Path.Combine(AppContext.BaseDirectory, "Renditions", Rendition);
}
