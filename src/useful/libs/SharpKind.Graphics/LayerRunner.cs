// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics;

/// <summary>
/// Draws a frame as an ordered list of <see cref="RenderLayer"/>s. For each
/// layer it sets the clip region, runs the layer, and flushes the depth
/// content, then moves on to the next.
/// <para>
/// Build one and keep it. The set of layers is fixed in a given game, so
/// there is nothing to rebuild per frame - a layer that has nothing to draw
/// this tick draws nothing, rather than leaving the list.
/// </para>
/// </summary>
public sealed class LayerRunner
{
    private readonly IGraphics _graphics;
    private readonly RenderLayer[] _layers;

    public LayerRunner(IGraphics graphics, params RenderLayer[] layers)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(layers);

        _graphics = graphics;
        _layers = layers;
    }

    /// <summary>
    /// Draw every layer, in order, furthest first.
    /// </summary>
    public void Draw()
    {
        foreach (RenderLayer layer in _layers)
        {
            _graphics.SetClipRegion(layer.Position, layer.Width, layer.Height);
            layer.Drawer.Draw();

            // Must run before the next layer's clip region replaces this one, or a depth-compositing backend would trim this layer's content to the next layer's rectangle - see IGraphics.FlushDepth.
            _graphics.FlushDepth();
        }
    }
}
