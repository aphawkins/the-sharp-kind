// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;

namespace SharpKind.Graphics.Rendering;

// The back-to-front chain alone decides occlusion, unlike ZBufferRenderer's per-pixel test. Solid
// polygons only; wireframe is a separate WireframeRenderer selected instead at DI-registration time.
public sealed class PainterRenderer(IGraphics graphics) : IPolygonRenderer
{
    private readonly IGraphics _graphics = graphics;
    private PolygonData[] _polyChain = new PolygonData[PolygonBuffer.InitialCapacity];
    private int _startPoly;
    private int _totalPolys;

    // depths is unused: the chain order alone decides occlusion here.
    public void Submit(Vector2[] points, float[] depths, FastColor color, float z)
        => Submit(points, depths, color, z, dither: null);

    // A plain fill has no per-pixel hook to blend through, so a Gouraud face
    // stands down to the one colour nearest what blending would have given.
    public void Submit(Vector2[] points, float[] depths, FastColor[] colours, float z, IColourQuantiser? quantiser)
        => Submit(
            points,
            depths,
            VertexColours.Flatten(colours, quantiser),
            z,
            quantiser?.IsPositionDependent == true ? quantiser : null);

    public void Submit(Vector2[] points, float[] depths, FastColor color, float z, IColourQuantiser? dither)
    {
        ArgumentNullException.ThrowIfNull(points);

        int i;

        PolygonBuffer.EnsureCapacity(ref _polyChain, _totalPolys + 1);

        int x = _totalPolys;
        _totalPolys++;

        _polyChain[x].Color = color;
        _polyChain[x].Dither = dither;
        _polyChain[x].Z = z;
        _polyChain[x].Next = -1;
        _polyChain[x].PointList = new Vector2[points.Length];

        for (i = 0; i < points.Length; i++)
        {
            _polyChain[x].PointList[i] = points[i];
        }

        if (x == 0)
        {
            return;
        }

        if (z > _polyChain[_startPoly].Z)
        {
            _polyChain[x].Next = _startPoly;
            _startPoly = x;
            return;
        }

        for (i = _startPoly; _polyChain[i].Next != -1; i = _polyChain[i].Next)
        {
            int nx = _polyChain[i].Next;

            if (z > _polyChain[nx].Z)
            {
                _polyChain[i].Next = x;
                _polyChain[x].Next = nx;
                return;
            }
        }

        _polyChain[i].Next = x;
    }

    public void StartFrame()
    {
        _startPoly = 0;
        _totalPolys = 0;
    }

    public void EndFrame()
    {
        if (_totalPolys == 0)
        {
            return;
        }

        for (int i = _startPoly; i != -1; i = _polyChain[i].Next)
        {
            if (_polyChain[i].PointList.Length == 2)
            {
                _graphics.DrawLine(_polyChain[i].PointList[0], _polyChain[i].PointList[1], _polyChain[i].Color);
                continue;
            }

            _graphics.DrawPolygonFilled(_polyChain[i].PointList, _polyChain[i].Color, _polyChain[i].Dither);
        }
    }
}
