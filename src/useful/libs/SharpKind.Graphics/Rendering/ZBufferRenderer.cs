// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;

namespace SharpKind.Graphics.Rendering;

// The per-pixel test decides occlusion on its own, so polygons draw in submission order and the
// flat z sort key is ignored - unlike PainterRenderer, which is nothing but that order. Coplanar
// surfaces still resolve by submission order since the depth test lets the later draw win a tie;
// the caller biases them if interpolation across differently shaped triangles makes the tie inexact.
public sealed class ZBufferRenderer(IGraphics graphics) : IPolygonRenderer
{
    private readonly IGraphics _graphics = graphics;
    private PolygonData[] _polys = new PolygonData[PolygonBuffer.InitialCapacity];
    private int _totalPolys;

    public void Submit(Vector2[] points, float[] depths, FastColor color, float z)
        => Submit(points, depths, color, z, dither: null);

    public void Submit(Vector2[] points, float[] depths, FastColor color, float z, IColourQuantiser? dither)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(depths);

        PolygonBuffer.EnsureCapacity(ref _polys, _totalPolys + 1);

        int x = _totalPolys;
        _totalPolys++;

        _polys[x].Color = color;
        _polys[x].Dither = dither;
        _polys[x].VertexColors = null;
        _polys[x].PointList = new Vector2[points.Length];
        _polys[x].Depths = new float[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            _polys[x].PointList[i] = points[i];
            _polys[x].Depths[i] = depths[i];
        }
    }

    // The per-pixel depth test is already per pixel, so blending a colour
    // across the face costs this strategy nothing it was not already doing.
    public void Submit(Vector2[] points, float[] depths, FastColor[] colours, float z, IColourQuantiser? quantiser)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(depths);
        ArgumentNullException.ThrowIfNull(colours);

        PolygonBuffer.EnsureCapacity(ref _polys, _totalPolys + 1);

        int x = _totalPolys;
        _totalPolys++;

        // A 2-point detail line has no interior to blend across, so it takes
        // the flat stand-in and the whole-face quantisation that goes with it.
        _polys[x].Color = VertexColours.Flatten(colours, quantiser);

        // Every quantiser is a per-pixel one here, dithering or not: see the
        // Submit contract.
        _polys[x].Dither = quantiser;

        FastColor[] vertexColors = new FastColor[points.Length];
        _polys[x].VertexColors = vertexColors;
        _polys[x].PointList = new Vector2[points.Length];
        _polys[x].Depths = new float[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            _polys[x].PointList[i] = points[i];
            _polys[x].Depths[i] = depths[i];
            vertexColors[i] = colours[i];
        }
    }

    public void StartFrame()
    {
        _totalPolys = 0;
        _graphics.ClearDepth();
    }

    public void EndFrame()
    {
        for (int i = 0; i < _totalPolys; i++)
        {
            if (_polys[i].PointList.Length == 2)
            {
                _graphics.DrawLineDepth(
                    _polys[i].PointList[0],
                    _polys[i].PointList[1],
                    _polys[i].Depths[0],
                    _polys[i].Depths[1],
                    _polys[i].Color,
                    surfaceId: 0);
                continue;
            }

            FastColor[]? vertexColors = _polys[i].VertexColors;
            if (vertexColors == null)
            {
                _graphics.DrawPolygonFilledDepth(_polys[i].PointList, _polys[i].Depths, _polys[i].Color, _polys[i].Dither);
                continue;
            }

            _graphics.DrawPolygonFilledDepth(_polys[i].PointList, _polys[i].Depths, vertexColors, _polys[i].Dither);
        }
    }
}
