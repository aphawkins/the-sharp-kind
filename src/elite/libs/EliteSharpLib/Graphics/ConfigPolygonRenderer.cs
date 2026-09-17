// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using SharpKind;
using SharpKind.Assets;
using SharpKind.Graphics;
using SharpKind.Graphics.Rendering;

namespace EliteSharpLib.Graphics;

// Picks the strategy from the live config once per frame, so a Settings change takes effect next frame.
// Fixed for the frame's duration so Submit/EndFrame always reach the renderer that started it.
internal sealed class ConfigPolygonRenderer : IPolygonRenderer
{
    private readonly GameState _gameState;
    private readonly IPolygonRenderer _wireframe;
    private readonly IPolygonRenderer _painter;
    private readonly IPolygonRenderer _zBuffer;
    private IPolygonRenderer _current;

    internal ConfigPolygonRenderer(GameState gameState, IGraphics graphics, IAssetLocator assetLocator)
    {
        _gameState = gameState;
        _wireframe = new WireframeRenderer(graphics, assetLocator);
        _painter = new PainterRenderer(graphics);
        _zBuffer = new ZBufferRenderer(graphics);
        _current = Selected;
    }

    private IPolygonRenderer Selected => _gameState.Config.Engine.Graphics.FillMode == FillMode.Wireframe
        ? _wireframe
        : _gameState.Config.Engine.Graphics.DepthSort == DepthSort.Painter
            ? _painter
            : _zBuffer;

    public void Submit(Vector2[] points, float[] depths, FastColor color, float z)
        => Submit(points, depths, color, z, dither: null);

    public void Submit(Vector2[] points, float[] depths, FastColor color, float z, IColourQuantiser? dither)
        => _current.Submit(points, depths, color, z, dither);

    public void Submit(Vector2[] points, float[] depths, FastColor[] colours, float z, IColourQuantiser? quantiser)
        => _current.Submit(points, depths, colours, z, quantiser);

    public void StartFrame()
    {
        _current = Selected;
        _current.StartFrame();
    }

    public void EndFrame() => _current.EndFrame();
}
