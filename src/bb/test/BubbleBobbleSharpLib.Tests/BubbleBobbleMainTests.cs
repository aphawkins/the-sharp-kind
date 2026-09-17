// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using BubbleBobbleSharp.Renditions.EightBit;
using BubbleBobbleSharpLib.Fakes;
using BubbleBobbleSharpLib.Levels;
using SharpKind.Fakes.Assets;
using SharpKind.Fakes.Input;
using SharpKind.Graphics;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests;

public sealed class BubbleBobbleMainTests
{
    // Twelve two-row blocks down each edge plus the last row's top half, both edges.
    private const int SidebarCharacters = 100;

    // The sheets the two views draw from. The game repaints them in the level's colours before it
    // draws, so the surface has to be holding them by the time a frame is composed.
    private static readonly string[] s_sheets = ["LevelTiles", "TileEdges", "Sidebars"];

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    [Fact]
    public void NewGameIsRunning()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());

        Assert.True(game.IsRunning);
    }

    [Fact]
    public void EscapeStopsTheGame()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Game(abstraction);
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.Escape, ConsoleModifiers.None);

        game.Update();

        Assert.False(game.IsRunning);
    }

    // There is no front end yet, so the game opens on the first level rather than being asked for one.
    [Fact]
    public void OpensOnTheFirstLevel()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());

        Assert.Equal(1, game.CurrentLevel);
    }

    // Two bands: the level trimmed to the 28 columns the decoration leaves, and the decoration over
    // the whole 32. Both are the full height of the screen.
    [Fact]
    public void ComposesTheLevelAndItsDecorationAsTwoLayers()
    {
        RecordingGraphics graphics = Draw();

        Assert.Equal(
            [(new Vector2(16, 0), 224f, 200f), (new Vector2(0, 0), 256f, 200f)],
            graphics.ClipRegions);
    }

    // The level is drawn first and the decoration over it, which is the order the reference works in:
    // setup_level_screen writes the level, and draw_border then writes over its outermost columns.
    //
    // Which sheet each comes from cannot tell them apart here: level 1 has no sidebar design, so its
    // decoration repeats its header tile out of the same tile sheet the level is drawn from. Where
    // they land does - the decoration is only ever at the level's outermost two columns each side.
    [Fact]
    public void DrawsTheLevelBeforeItsDecoration()
    {
        RecordingGraphics graphics = Draw();

        float[] edges = [0f, 8f, 240f, 248f];

        Assert.Equal(
            edges,
            graphics.ImageParts.TakeLast(SidebarCharacters).Select(x => x.Position.X).Distinct().Order());

        // And the level itself was drawn before them, inside those edges.
        Assert.Contains(
            graphics.ImageParts.SkipLast(SidebarCharacters),
            x => !edges.Contains(x.Position.X));
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "SetImage takes the bitmap on, and the graphics disposes what it holds.")]
    private static RecordingGraphics Draw()
    {
        RecordingGraphics graphics = new(320, 200);

        foreach (string sheet in s_sheets)
        {
            graphics.SetImage(sheet, new FastBitmap(4, 8));
        }

        Game(new FakeAbstraction(graphics, new(320, 200))).Draw();

        return graphics;
    }

    private static BubbleBobbleMain Game(FakeAbstraction abstraction)
        => new(abstraction, new FakeAssetLocator(), new EightBitRendition(), s_levels);
}
