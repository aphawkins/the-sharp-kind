// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using SharpKind.Graphics.Fakes;

namespace SharpKind.Graphics.Tests;

public class LayerRunnerTests
{
    [Fact]
    public void EachLayerDrawsUnderItsOwnClipRegion()
    {
        RecordingGraphics graphics = new(512, 512);
        SpyLayer window = new(graphics);
        SpyLayer hud = new(graphics);
        LayerRunner runner = new(
            graphics,
            new RenderLayer(Vector2.Zero, 512, 384, window),
            new RenderLayer(Vector2.Zero, 512, 512, hud));

        runner.Draw();

        Assert.Equal(
            [(Vector2.Zero, 512, 384), (Vector2.Zero, 512, 512)],
            graphics.ClipRegions);

        // Each layer ran after its own clip region was set, and before the
        // next one replaced it.
        Assert.Equal(1, window.ClipRegionsWhenDrawn);
        Assert.Equal(2, hud.ClipRegionsWhenDrawn);
    }

    [Fact]
    public void LayersDrawInTheOrderTheyWereGiven()
    {
        RecordingGraphics graphics = new(512, 512);
        List<string> order = [];
        LayerRunner runner = new(
            graphics,
            new RenderLayer(Vector2.Zero, 512, 384, new NamedLayer("stars", order)),
            new RenderLayer(Vector2.Zero, 512, 384, new NamedLayer("universe", order)),
            new RenderLayer(Vector2.Zero, 512, 512, new NamedLayer("hud", order)));

        runner.Draw();

        Assert.Equal(["stars", "universe", "hud"], order);
    }

    [Fact]
    public void DepthContentIsFlushedBeforeTheNextLayerChangesTheClipRegion()
    {
        RecordingGraphics graphics = new(512, 512);
        SpyLayer window = new(graphics);
        SpyLayer hud = new(graphics);
        LayerRunner runner = new(
            graphics,
            new RenderLayer(Vector2.Zero, 512, 384, window),
            new RenderLayer(Vector2.Zero, 512, 512, hud));

        runner.Draw();

        // The universe layer's flush had already happened by the time the HUD
        // layer drew, so a compositing backend trims that layer's depth-tested
        // content to 512x384 rather than to the HUD's full screen.
        Assert.Equal(0, window.FlushesWhenDrawn);
        Assert.Equal(1, hud.FlushesWhenDrawn);
        Assert.Equal(2, graphics.FlushDepthCount);
    }

    [Fact]
    public void ALayerThatDrawsNothingStillFlushesAndStillHoldsItsPlace()
    {
        RecordingGraphics graphics = new(512, 512);
        SpyLayer hud = new(graphics);
        LayerRunner runner = new(
            graphics,
            new RenderLayer(Vector2.Zero, 512, 384, new SilentLayer()),
            new RenderLayer(Vector2.Zero, 512, 512, hud));

        runner.Draw();

        Assert.Equal(2, graphics.ClipRegions.Count);
        Assert.Equal(1, hud.FlushesWhenDrawn);
    }

    [Fact]
    public void NoLayersDrawsNothing()
    {
        RecordingGraphics graphics = new(512, 512);
        LayerRunner runner = new(graphics);

        runner.Draw();

        Assert.Empty(graphics.ClipRegions);
        Assert.Equal(0, graphics.FlushDepthCount);
    }

    [Fact]
    public void TheRunnerNeedsGraphicsAndALayerList()
    {
        Assert.Throws<ArgumentNullException>(() => new LayerRunner(null!));
        Assert.Throws<ArgumentNullException>(() => new LayerRunner(new RecordingGraphics(), null!));
    }

    // Records what the graphics had already been asked to do at the moment
    // this layer was drawn, which is how the ordering above is asserted.
    private sealed class SpyLayer(RecordingGraphics graphics) : ILayerDrawer
    {
        public int ClipRegionsWhenDrawn { get; private set; }

        public int FlushesWhenDrawn { get; private set; }

        public void Draw()
        {
            ClipRegionsWhenDrawn = graphics.ClipRegions.Count;
            FlushesWhenDrawn = graphics.FlushDepthCount;
        }
    }

    private sealed class NamedLayer(string name, List<string> order) : ILayerDrawer
    {
        public void Draw() => order.Add(name);
    }

    private sealed class SilentLayer : ILayerDrawer
    {
        public void Draw()
        {
        }
    }
}
