// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using SharpKind.Assets;
using SharpKind.Assets.Palettes;

namespace SharpKind.Graphics.Rendering;

// Outline-only rendering with hidden-line removal. Backface culling alone can't do this - ship
// models aren't convex (a fin's double-sided plate always survives a cull) - so every surface
// writes to the depth buffer without drawing, and outlines then draw depth-tested against it.
public sealed class WireframeRenderer : IPolygonRenderer
{
    private readonly FastColor _colorWhite;
    private readonly IGraphics _graphics;
    private PolygonData[] _polys = new PolygonData[PolygonBuffer.InitialCapacity];
    private int _totalPolys;

    public WireframeRenderer(IGraphics graphics, IAssetLocator assetLocator)
    {
        ArgumentNullException.ThrowIfNull(assetLocator);

        _graphics = graphics;
        _colorWhite = PaletteReader.Read(assetLocator.PalettePath)["White"];
    }

    // dither is ignored: an outline has no fill to dither.
    public void Submit(Vector2[] points, float[] depths, FastColor color, float z)
        => Submit(points, depths, color, z, dither: null);

    // An outline draws white whatever colour arrives, so this flattening only affects the depth-only surfaces, never drawn.
    public void Submit(Vector2[] points, float[] depths, FastColor[] colours, float z, IColourQuantiser? quantiser)
        => Submit(points, depths, VertexColours.Flatten(colours, quantiser), z, dither: null);

    public void Submit(Vector2[] points, float[] depths, FastColor color, float z, IColourQuantiser? dither)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(depths);

        PolygonBuffer.EnsureCapacity(ref _polys, _totalPolys + 1);

        int x = _totalPolys;
        _totalPolys++;

        _polys[x].Color = color;
        _polys[x].PointList = new Vector2[points.Length];
        _polys[x].Depths = new float[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            _polys[x].PointList[i] = points[i];
            _polys[x].Depths[i] = depths[i];
        }
    }

    public void StartFrame()
    {
        _totalPolys = 0;
        _graphics.ClearDepth();
    }

    public void EndFrame()
    {
        // Every surface occludes and is tagged so its own edges recognise it. A 2-point detail
        // line isn't a surface, so it takes its host's id rather than one of its own.
        for (int i = 0; i < _totalPolys; i++)
        {
            if (_polys[i].PointList.Length > 2)
            {
                _graphics.FillDepth(_polys[i].PointList, _polys[i].Depths, SurfaceId(i));
            }
        }

        for (int i = 0; i < _totalPolys; i++)
        {
            DrawOutline(_polys[i].PointList, _polys[i].Depths, SurfaceId(i));
        }
    }

    // Ids are 1-based: 0 means "no surface", which never matches.
    private static int SurfaceId(int polyIndex) => polyIndex + 1;

    private void DrawOutline(Vector2[] points, float[] depths, int surfaceId)
    {
        if (points.Length == 2)
        {
            DrawEdge(points[0], points[1], depths[0], depths[1], surfaceId);
            return;
        }

        for (int i = 0; i < points.Length - 1; i++)
        {
            DrawEdge(points[i], points[i + 1], depths[i], depths[i + 1], surfaceId);
        }

        DrawEdge(points[^1], points[0], depths[^1], depths[0], surfaceId);
    }

    // No depth bias: the id settles the tie by identity instead. A bias large enough to cover a
    // near edge-on face's gradient would also let genuinely hidden edges leak through.
    private void DrawEdge(Vector2 start, Vector2 end, float depthStart, float depthEnd, int surfaceId)
        => _graphics.DrawLineDepth(start, end, depthStart, depthEnd, _colorWhite, surfaceId);
}
