// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Diagnostics;
using System.Numerics;
using Microsoft.Extensions.Logging;
using SharpKind.Assets;
using SharpKind.Graphics.Rendering;

namespace SharpKind.Graphics;

public sealed partial class SoftwareGraphics : IGraphics, IDisposable
{
    // Bounded LRU cache of rendered text bitmaps, keyed by (font, colour,
    // text). Elite draws ever-changing strings (bounties, countdowns) that
    // would otherwise accumulate forever; capping the entry count and
    // evicting the least-recently-used bitmap keeps memory bounded while
    // keeping frequently redrawn text (HUD labels) warm.
    private const int TextCacheCapacity = 256;

    private readonly FastBitmap _screen;
    private readonly Action<FastBitmap> _screenUpdate;
    private readonly Dictionary<string, LinkedListNode<(string Key, FastBitmap Bitmap)>> _textCache = [];
    private readonly LinkedList<(string Key, FastBitmap Bitmap)> _textCacheOrder = new();

    // How text becomes pixels, in each kind the rendition offers. This
    // renderer neither knows nor cares how any of them work - only that the
    // one in use turns a string into a bitmap it can blit.
    private readonly FontRasteriserSet _fontRasterisers;

    // Inverse depth (1/z) per pixel, 0 = infinitely far; allocated on first
    // use so purely 2D rendering pays nothing.
    private float[]? _depth;

    // The surface currently occupying each pixel, 0 = none. Only the
    // hidden-line pass tags its fills, so this stays all-zero otherwise.
    private int[]? _surfaceIds;
    private bool _isDisposed;

    // Clip rectangle in whole pixels - left and top inclusive, right and
    // bottom exclusive - defaulting to the whole screen so callers that
    // never call SetClipRegion see no change in behaviour. Always kept
    // clamped to the screen bounds, so clamping a loop to these bounds
    // keeps it both inside the clip region and inside the framebuffer: the
    // fills, rectangles and images below narrow their ranges to them once
    // and then write pixels without testing anything. Held as int so no
    // pixel path converts them to float.
    private int _clipLeft;
    private int _clipTop;
    private int _clipRight;
    private int _clipBottom;

    internal SoftwareGraphics(
        float screenWidth,
        float screenHeight,
        Action<FastBitmap> screenUpdate,
        Dictionary<string, FastBitmap> images,
        FontRasteriserSet fontRasterisers)
    {
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
        _screen = new((int)screenWidth, (int)screenHeight);
        _screenUpdate = screenUpdate;
        Images = images;
        _fontRasterisers = fontRasterisers;
        _clipRight = (int)screenWidth;
        _clipBottom = (int)screenHeight;
        Clear();
    }

    public float ScreenHeight { get; }

    public float ScreenWidth { get; }

    public FontKind FontKind
    {
        get => _fontRasterisers.Kind;

        set
        {
            FontKind previous = _fontRasterisers.Kind;

            // Every cached bitmap was drawn in the kind that was in use when
            // it was cached, so a change makes the lot of them stale.
            if (_fontRasterisers.Select(value) != previous)
            {
                ClearTextCache();
            }
        }
    }

    internal Dictionary<string, FastBitmap> Images { get; }

    public static SoftwareGraphics Create(
        float screenWidth,
        float screenHeight,
        Action<FastBitmap> screenUpdate,
        IAssetLocator assetLocator)
        => Create(screenWidth, screenHeight, screenUpdate, assetLocator, null);

    public static SoftwareGraphics Create(
        float screenWidth,
        float screenHeight,
        Action<FastBitmap> screenUpdate,
        IAssetLocator assetLocator,
        ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(screenUpdate);
        ArgumentNullException.ThrowIfNull(assetLocator);

        AssetSet assets = AssetSet.Load(assetLocator, logger);

        return Create(
            screenWidth,
            screenHeight,
            screenUpdate,
            assets,
            FontRasteriserSet.Only(new BitmapFontRasteriser(assets.BitmapFonts)));
    }

    /// <summary>
    /// Builds a renderer over an already-loaded asset set and the font kinds
    /// it is to be drawn with. The overloads above load the assets themselves
    /// and can only offer the rendition's own sheets, since one of the other
    /// kinds needs SDL - a caller wanting all three composes the set where SDL
    /// is available and passes it in here.
    /// </summary>
    /// <param name="screenWidth">The framebuffer width.</param>
    /// <param name="screenHeight">The framebuffer height.</param>
    /// <param name="screenUpdate">What to do with a composed frame.</param>
    /// <param name="assets">The rendition's loaded assets.</param>
    /// <param name="fontRasterisers">The font kinds available, with one selected.</param>
    /// <returns>A renderer drawing that rendition.</returns>
    public static SoftwareGraphics Create(
        float screenWidth,
        float screenHeight,
        Action<FastBitmap> screenUpdate,
        AssetSet assets,
        FontRasteriserSet fontRasterisers)
    {
        ArgumentNullException.ThrowIfNull(screenUpdate);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(fontRasterisers);

        return new(screenWidth, screenHeight, screenUpdate, assets.Images, fontRasterisers);
    }

    public void Clear() => _screen.Clear();

    public void ClearDepth()
    {
        _depth ??= new float[(int)ScreenWidth * (int)ScreenHeight];
        _surfaceIds ??= new int[(int)ScreenWidth * (int)ScreenHeight];
        Array.Clear(_depth);
        Array.Clear(_surfaceIds);
    }

    // Nothing to do: every draw goes straight into the one shared buffer and
    // is clipped as it rasterises, so depth-tested content is already on the
    // screen and already trimmed to the clip region that was active when it
    // was drawn. The method exists for the backends that composite instead.
    public void FlushDepth()
    {
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void DrawCircle(Vector2 centre, float radius, FastColor color)
    {
        float diameter = radius * 2;
        float x = MathF.Floor(radius);
        float y = 0;
        float tx = 1;
        float ty = 1;
        float error = tx - diameter;

        while (x >= y)
        {
            DrawPixel(new(centre.X + x, centre.Y + y), color);
            DrawPixel(new(centre.X + x, centre.Y - y), color);
            DrawPixel(new(centre.X - x, centre.Y + y), color);
            DrawPixel(new(centre.X - x, centre.Y - y), color);
            DrawPixel(new(centre.X + y, centre.Y + x), color);
            DrawPixel(new(centre.X + y, centre.Y - x), color);
            DrawPixel(new(centre.X - y, centre.Y + x), color);
            DrawPixel(new(centre.X - y, centre.Y - x), color);

            if (error <= 0)
            {
                y++;
                error += ty;
                ty += 2;
            }

            if (error > 0)
            {
                x--;
                tx += 2;
                error += tx - diameter;
            }
        }
    }

    public void DrawCircleFilled(Vector2 centre, float radius, FastColor color)
    {
        float diameter = MathF.Floor(radius) * 2;
        float x = MathF.Floor(radius);
        float y = 0;
        float tx = 1;
        float ty = 1;
        float error = tx - diameter;

        while (x >= y)
        {
            // Top of top half
            DrawLine(new(centre.X - y, centre.Y - x), new(centre.X + y, centre.Y - x), color);

            // Bottom of top half
            DrawLine(new(centre.X - x, centre.Y - y), new(centre.X + x, centre.Y - y), color);

            // Top of bottom half
            DrawLine(new(centre.X - x, centre.Y + y), new(centre.X + x, centre.Y + y), color);

            // Bottom of bottom half
            DrawLine(new(centre.X - y, centre.Y + x), new(centre.X + y, centre.Y + x), color);

            if (error <= 0)
            {
                y++;
                error += ty;
                ty += 2;
            }

            if (error > 0)
            {
                x--;
                tx += 2;
                error += tx - diameter;
            }
        }
    }

    public void DrawImage(string imageType, Vector2 position)
    {
        Debug.Assert(Images.ContainsKey(imageType), "Image has not been loaded");

        FastBitmap bitmap = Images[imageType];
        DrawImage(bitmap, position);
    }

    public void DrawImageCentre(string imageType, float y)
    {
        float x = (ScreenWidth - Images[imageType].Width) / 2;
        DrawImage(imageType, new(x, y));
    }

    public void DrawImagePart(string imageType, Vector2 position, Vector2 size, Vector2 sourcePosition, Vector2 sourceSize)
    {
        Debug.Assert(Images.ContainsKey(imageType), "Image has not been loaded");

        FastBitmap bitmap = Images[imageType];
        bool mirrorX = sourceSize.X < 0;
        float sourceWidth = Math.Abs(sourceSize.X);
        float sourceHeight = sourceSize.Y;
        int destWidth = (int)MathF.Round(size.X);
        int destHeight = (int)MathF.Round(size.Y);
        if (destWidth <= 0 || destHeight <= 0 || sourceWidth <= 0 || sourceHeight <= 0)
        {
            return;
        }

        for (int dy = 0; dy < destHeight; dy++)
        {
            int y = (int)(position.Y + dy);
            if (y < _clipTop || y >= _clipBottom)
            {
                continue;
            }

            // nearest-neighbour sample from the centre of each destination pixel
            int sy = (int)(sourcePosition.Y + ((dy + 0.5f) * sourceHeight / destHeight));
            sy = Math.Clamp(sy, 0, bitmap.Height - 1);

            DrawImagePartRow(bitmap, position.X, y, destWidth, sourcePosition.X, sourceWidth, mirrorX, sy);
        }
    }

    public Vector2 ImageSize(string imageType)
    {
        Debug.Assert(Images.ContainsKey(imageType), "Image has not been loaded");

        FastBitmap bitmap = Images[imageType];
        return new(bitmap.Width, bitmap.Height);
    }

    public void DrawLineDepth(
        Vector2 lineStart,
        Vector2 lineEnd,
        float depthStart,
        float depthEnd,
        FastColor color,
        int surfaceId)
        => DrawLineIntDepth(
            (int)MathF.Floor(lineStart.X),
            (int)MathF.Floor(lineStart.Y),
            (int)MathF.Floor(lineEnd.X),
            (int)MathF.Floor(lineEnd.Y),
            1f / depthStart,
            1f / depthEnd,
            color,
            surfaceId);

    public void DrawLine(Vector2 lineStart, Vector2 lineEnd, FastColor color)
        => DrawLineInt(
            (int)MathF.Floor(lineStart.X),
            (int)MathF.Floor(lineStart.Y),
            (int)MathF.Floor(lineEnd.X),
            (int)MathF.Floor(lineEnd.Y),
            color);

    public void DrawPixel(Vector2 position, FastColor color)
    {
        if (position.X < _clipLeft || position.Y < _clipTop || position.X >= _clipRight || position.Y >= _clipBottom)
        {
            return;
        }

        StorePixel((int)position.X, (int)position.Y, color);
    }

    public void DrawPolygon(Vector2[] points, FastColor lineColor)
    {
        if (points == null)
        {
            return;
        }

        for (int i = 0; i < points.Length - 1; i++)
        {
            DrawLine(points[i], points[i + 1], lineColor);
        }

        DrawLine(points[0], points[^1], lineColor);
    }

    public void DrawPolygonFilled(Vector2[] points, FastColor faceColor)
        => DrawPolygonFilled(points, faceColor, dither: null);

    public void DrawPolygonFilled(Vector2[] points, FastColor faceColor, IColourQuantiser? dither)
    {
        if (points == null)
        {
            return;
        }

        // The face is one colour, so the dither has only sixteen answers for
        // it; resolve them once here rather than per pixel.
        DitherCells cells = new(stackalloc FastColor[DitherCells.Count], faceColor, dither);

        // Create triangles of which each share the first vertex
        for (int i = 1; i < points.Length - 1; i++)
        {
            DrawTriangleFilledCells(points[0], points[i], points[i + 1], in cells);
        }
    }

    public void DrawPolygonFilledDepth(Vector2[] points, float[] depths, FastColor faceColor)
        => DrawPolygonFilledDepth(points, depths, faceColor, dither: null);

    public void DrawPolygonFilledDepth(Vector2[] points, float[] depths, FastColor faceColor, IColourQuantiser? dither)
        => FillPolygonDepth(
            points,
            depths,
            new(stackalloc FastColor[DitherCells.Count], faceColor, dither),
            writeColor: true,
            surfaceId: 0);

    public void FillDepth(Vector2[] points, float[] depths, int surfaceId)
        => FillPolygonDepth(
            points,
            depths,
            new(stackalloc FastColor[DitherCells.Count], BaseColors.Black, null),
            writeColor: false,
            surfaceId);

    public void DrawPolygonTextured(Vector2[] points, Vector2[] textureCoords, FastBitmap texture)
    {
        if (points == null || textureCoords == null || texture == null || textureCoords.Length < points.Length)
        {
            return;
        }

        // Create triangles of which each share the first vertex
        for (int i = 1; i < points.Length - 1; i++)
        {
            DrawTriangleTextured(
                points[0],
                points[i],
                points[i + 1],
                textureCoords[0],
                textureCoords[i],
                textureCoords[i + 1],
                texture);
        }
    }

    public void DrawPolygonTexturedDepth(Vector2[] points, float[] depths, Vector2[] textureCoords, FastBitmap texture)
    {
        if (points == null ||
            depths == null ||
            textureCoords == null ||
            texture == null ||
            depths.Length < points.Length ||
            textureCoords.Length < points.Length)
        {
            return;
        }

        // Create triangles of which each share the first vertex
        for (int i = 1; i < points.Length - 1; i++)
        {
            DrawTriangleTexturedDepth(
                points[0],
                points[i],
                points[i + 1],
                depths[0],
                depths[i],
                depths[i + 1],
                textureCoords[0],
                textureCoords[i],
                textureCoords[i + 1],
                texture);
        }
    }

    public void DrawRectangle(Vector2 position, float width, float height, FastColor color)
        => DrawRectangleInt(
            (int)MathF.Floor(position.X),
            (int)MathF.Floor(position.Y),
            (int)MathF.Floor(width),
            (int)MathF.Floor(height),
            color);

    public void DrawRectangleCentre(float y, float width, float height, FastColor color)
        => DrawRectangle(new((ScreenWidth - width) / 2, y), width, height, color);

    public void DrawRectangleFilled(Vector2 position, float width, float height, FastColor color)
        => DrawRectangleFilledInt(
            (int)MathF.Floor(position.X),
            (int)MathF.Floor(position.Y),
            (int)MathF.Floor(width),
            (int)MathF.Floor(height),
            color);

    public Vector2 MeasureText(string text, string fontType) => _fontRasterisers.Measure(text, fontType);

    public void DrawTextCentre(float y, string text, string fontType, FastColor color)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        FastBitmap bitmapText = GenerateTextBitmap(text, fontType, color);
        int x = (int)((ScreenWidth / 2) - (bitmapText.Width / 2));
        DrawImage(bitmapText, new(x, y));
    }

    public void DrawTextLeft(Vector2 position, string text, string fontType, FastColor color)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        FastBitmap bitmapText = GenerateTextBitmap(text, fontType, color);
        DrawImage(bitmapText, position);
    }

    public void DrawTextRight(Vector2 position, string text, string fontType, FastColor color)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        FastBitmap bitmapText = GenerateTextBitmap(text, fontType, color);
        DrawImage(bitmapText, position - new Vector2(bitmapText.Width, 0));
    }

    public void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, FastColor color)
    {
        DrawLine(a, b, color);
        DrawLine(b, c, color);
        DrawLine(c, a, color);
    }

    public void DrawTriangleFilled(Vector2 a, Vector2 b, Vector2 c, FastColor color)
        => DrawTriangleFilled(a, b, c, color, dither: null);

    public void DrawTriangleFilled(Vector2 a, Vector2 b, Vector2 c, in FastColor color, IColourQuantiser? dither)
        => DrawTriangleFilledCells(a, b, c, new DitherCells(stackalloc FastColor[DitherCells.Count], color, dither));

    public void ScreenUpdate() => _screenUpdate(_screen);

    public void SaveScreen(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        BitmapWriter.Write(_screen, path);
    }

    public void SetClipRegion(Vector2 position, float width, float height)
    {
        _clipLeft = (int)Math.Clamp(position.X, 0, ScreenWidth);
        _clipTop = (int)Math.Clamp(position.Y, 0, ScreenHeight);
        _clipRight = (int)Math.Clamp(position.X + width, 0, ScreenWidth);
        _clipBottom = (int)Math.Clamp(position.Y + height, 0, ScreenHeight);
    }

    // Textured variant of DrawTriangleFilled: texture coordinates are
    // interpolated affinely in screen space (no perspective correction,
    // which is fine for the small triangles a scene decomposes into) and
    // sampled bilinearly, from whichever mip level best matches this
    // triangle's texel-to-pixel ratio, with edge clamping.
    internal void DrawTriangleTextured(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 ta,
        Vector2 tb,
        Vector2 tc,
        FastBitmap texture)
    {
        FastBitmap[] mipChain = GetMipChain(texture);
        FastBitmap mip = SelectMip(mipChain, a, b, c, ta, tb, tc);

        // Sort the points so that a.Y <= b.Y <= c.Y, keeping each texture
        // coordinate paired with its point
        if (b.Y < a.Y)
        {
            (a, b, ta, tb) = (b, a, tb, ta);
        }

        if (c.Y < a.Y)
        {
            (a, c, ta, tc) = (c, a, tc, ta);
        }

        if (c.Y < b.Y)
        {
            (b, c, tb, tc) = (c, b, tc, tb);
        }

        // Clamp Y range to screen bounds
        int firstY = Math.Max((int)MathF.Ceiling(a.Y), _clipTop);
        int lastY = Math.Min((int)MathF.Floor(c.Y), _clipBottom - 1);

        // As DrawTriangleFilled: evaluate the two edges crossing each
        // scanline directly, with the interpolation parameter clamped to the
        // edge's endpoints, carrying the texture coordinates along
        for (int y = firstY; y <= lastY; y++)
        {
            // the long edge a-c, and either a-b (above b) or b-c (below)
            float t0 = EdgeT(a, c, y);
            float x0 = a.X + ((c.X - a.X) * t0);
            Vector2 uv0 = Vector2.Lerp(ta, tc, t0);

            float x1;
            Vector2 uv1;
            if (y < b.Y)
            {
                float t1 = EdgeT(a, b, y);
                x1 = a.X + ((b.X - a.X) * t1);
                uv1 = Vector2.Lerp(ta, tb, t1);
            }
            else
            {
                float t1 = EdgeT(b, c, y);
                x1 = b.X + ((c.X - b.X) * t1);
                uv1 = Vector2.Lerp(tb, tc, t1);
            }

            if (x0 > x1)
            {
                (x0, x1) = (x1, x0);
                (uv0, uv1) = (uv1, uv0);
            }

            int start = Math.Max((int)MathF.Floor(x0), _clipLeft);
            int end = Math.Min((int)MathF.Floor(x1), _clipRight - 1);
            float span = x1 - x0;

            for (int x = start; x <= end; x++)
            {
                float t = span <= 0 ? 0f : Math.Clamp((x - x0) / span, 0f, 1f);
                StorePixel(x, y, SampleTexture(mip, Vector2.Lerp(uv0, uv1, t)));
            }
        }
    }

    // Depth-tested variant of DrawTriangleFilled: inverse depth (1/z) is
    // interpolated linearly in screen space (which is perspective-correct
    // for depth) and each pixel only draws when it passes the depth test.
    // writeColor false runs the depth test and its writes but draws nothing,
    // which is how a hidden-line pass primes the buffer.
    internal void DrawTriangleFilledDepth(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        float za,
        float zb,
        float zc,
        in DitherCells cells,
        bool writeColor = true,
        int surfaceId = 0)
    {
        if (za <= 0 || zb <= 0 || zc <= 0)
        {
            return;
        }

        // Sort the points so that a.Y <= b.Y <= c.Y, keeping each depth
        // paired with its point
        if (b.Y < a.Y)
        {
            (a, b, za, zb) = (b, a, zb, za);
        }

        if (c.Y < a.Y)
        {
            (a, c, za, zc) = (c, a, zc, za);
        }

        if (c.Y < b.Y)
        {
            (b, c, zb, zc) = (c, b, zc, zb);
        }

        float ia = 1f / za;
        float ib = 1f / zb;
        float ic = 1f / zc;

        // Clamp Y range to screen bounds
        int firstY = Math.Max((int)MathF.Ceiling(a.Y), _clipTop);
        int lastY = Math.Min((int)MathF.Floor(c.Y), _clipBottom - 1);

        // As DrawTriangleFilled: evaluate the two edges crossing each
        // scanline directly, carrying the inverse depth along
        for (int y = firstY; y <= lastY; y++)
        {
            // the long edge a-c, and either a-b (above b) or b-c (below)
            float t0 = EdgeT(a, c, y);
            float x0 = a.X + ((c.X - a.X) * t0);
            float i0 = ia + ((ic - ia) * t0);

            float x1;
            float i1;
            if (y < b.Y)
            {
                float t1 = EdgeT(a, b, y);
                x1 = a.X + ((b.X - a.X) * t1);
                i1 = ia + ((ib - ia) * t1);
            }
            else
            {
                float t1 = EdgeT(b, c, y);
                x1 = b.X + ((c.X - b.X) * t1);
                i1 = ib + ((ic - ib) * t1);
            }

            if (x0 > x1)
            {
                (x0, x1) = (x1, x0);
                (i0, i1) = (i1, i0);
            }

            DrawSpanFilledDepth(y, x0, x1, i0, i1, in cells, writeColor, surfaceId);
        }
    }

    // Depth-tested variant of DrawTriangleTextured: texture coordinates are
    // interpolated divided by depth and recovered per pixel (perspective
    // correct, unlike the affine DrawTriangleTextured), since the road
    // polygons near the viewpoint cover large parts of the screen.
    internal void DrawTriangleTexturedDepth(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        float za,
        float zb,
        float zc,
        Vector2 ta,
        Vector2 tb,
        Vector2 tc,
        FastBitmap texture)
    {
        if (za <= 0 || zb <= 0 || zc <= 0)
        {
            return;
        }

        FastBitmap[] mipChain = GetMipChain(texture);

        // Sort the points so that a.Y <= b.Y <= c.Y, keeping each depth and
        // texture coordinate paired with its point
        if (b.Y < a.Y)
        {
            (a, b, za, zb, ta, tb) = (b, a, zb, za, tb, ta);
        }

        if (c.Y < a.Y)
        {
            (a, c, za, zc, ta, tc) = (c, a, zc, za, tc, ta);
        }

        if (c.Y < b.Y)
        {
            (b, c, zb, zc, tb, tc) = (c, b, zc, zb, tc, tb);
        }

        float ia = 1f / za;
        float ib = 1f / zb;
        float ic = 1f / zc;
        Vector2 ua = ta * ia;
        Vector2 ub = tb * ib;
        Vector2 uc = tc * ic;

        // Clamp Y range to screen bounds
        int firstY = Math.Max((int)MathF.Ceiling(a.Y), _clipTop);
        int lastY = Math.Min((int)MathF.Floor(c.Y), _clipBottom - 1);

        for (int y = firstY; y <= lastY; y++)
        {
            // the long edge a-c, and either a-b (above b) or b-c (below)
            float t0 = EdgeT(a, c, y);
            float x0 = a.X + ((c.X - a.X) * t0);
            float i0 = ia + ((ic - ia) * t0);
            Vector2 uv0 = Vector2.Lerp(ua, uc, t0);

            float x1;
            float i1;
            Vector2 uv1;
            if (y < b.Y)
            {
                float t1 = EdgeT(a, b, y);
                x1 = a.X + ((b.X - a.X) * t1);
                i1 = ia + ((ib - ia) * t1);
                uv1 = Vector2.Lerp(ua, ub, t1);
            }
            else
            {
                float t1 = EdgeT(b, c, y);
                x1 = b.X + ((c.X - b.X) * t1);
                i1 = ib + ((ic - ib) * t1);
                uv1 = Vector2.Lerp(ub, uc, t1);
            }

            if (x0 > x1)
            {
                (x0, x1) = (x1, x0);
                (i0, i1) = (i1, i0);
                (uv0, uv1) = (uv1, uv0);
            }

            DrawSpanTexturedDepth(y, x0, x1, i0, i1, uv0, uv1, mipChain);
        }
    }

    // The x position of the edge p0-p1 at scanline y, clamped to the
    // edge's endpoints (p0.Y must not be greater than p1.Y).
    private static float EdgeX(Vector2 p0, Vector2 p1, float y)
        => p0.X + ((p1.X - p0.X) * EdgeT(p0, p1, y));

    // The interpolation parameter of the edge p0-p1 at scanline y, clamped
    // to the edge's endpoints (p0.Y must not be greater than p1.Y). A
    // horizontal or degenerate edge yields 0.
    private static float EdgeT(Vector2 p0, Vector2 p1, float y)
    {
        float dy = p1.Y - p0.Y;
        return dy <= 0 ? 0f : Math.Clamp((y - p0.Y) / dy, 0f, 1f);
    }

    private static (Vector2 A, Vector2 B, Vector2 C) SortPointsByY(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2[] sorted = [a, b, c];
        Array.Sort(sorted, (i, j) => i.Y.CompareTo(j.Y));
        return (sorted[0], sorted[1], sorted[2]);
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
                _screen?.Dispose();
                _fontRasterisers?.Dispose();
                ClearTextCache();
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            _isDisposed = true;
        }
    }

    private void DrawImage(FastBitmap bitmap, Vector2 position)
    {
        // Clipped once here rather than per pixel: the loops are narrowed to
        // the part of the bitmap landing inside the clip rectangle, which is
        // inside the framebuffer by construction, so the pixel path tests
        // nothing.
        int left = Math.Max(0, _clipLeft - (int)position.X);
        int top = Math.Max(0, _clipTop - (int)position.Y);
        int right = Math.Min(bitmap.Width, _clipRight - (int)position.X);
        int bottom = Math.Min(bitmap.Height, _clipBottom - (int)position.Y);

        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                FastColor color = bitmap.GetPixel(x, y);
                if (color.A != 0)
                {
                    StorePixel((int)(position.X + x), (int)(position.Y + y), color);
                }
            }
        }
    }

    // Draw one destination row of a scaled image part, sampling row sy of the
    // source bitmap with nearest-neighbour filtering.
    private void DrawImagePartRow(
        FastBitmap bitmap,
        float destX,
        int y,
        int destWidth,
        float sourceX,
        float sourceWidth,
        bool mirrorX,
        int sy)
    {
        for (int dx = 0; dx < destWidth; dx++)
        {
            int x = (int)(destX + dx);
            if (x < _clipLeft || x >= _clipRight)
            {
                continue;
            }

            float fx = (dx + 0.5f) * sourceWidth / destWidth;
            int sx = (int)(sourceX + (mirrorX ? sourceWidth - fx : fx));
            sx = Math.Clamp(sx, 0, bitmap.Width - 1);

            FastColor color = bitmap.GetPixel(sx, sy);
            if (color.A != 0)
            {
                StorePixel(x, y, color);
            }
        }
    }

    private void DrawTriangleFilledCells(Vector2 a, Vector2 b, Vector2 c, in DitherCells cells)
    {
        // Sort the points so that a.Y <= b.Y <= c.Y
        (a, b, c) = SortPointsByY(a, b, c);

        // Clamp Y range to screen bounds
        int firstY = Math.Max((int)MathF.Ceiling(a.Y), _clipTop);
        int lastY = Math.Min((int)MathF.Floor(c.Y), _clipBottom - 1);

        // Evaluate the two edges crossing each scanline directly; the
        // interpolation parameter is clamped to the edge's endpoints, so
        // steep or near-horizontal edges can never overshoot.
        for (int y = firstY; y <= lastY; y++)
        {
            // the long edge a-c, and either a-b (above b) or b-c (below)
            float x0 = EdgeX(a, c, y);
            float x1 = y < b.Y ? EdgeX(a, b, y) : EdgeX(b, c, y);

            if (x0 > x1)
            {
                (x0, x1) = (x1, x0);
            }

            int start = Math.Max((int)MathF.Floor(x0), _clipLeft);
            int end = Math.Min((int)MathF.Floor(x1), _clipRight - 1);

            for (int x = start; x <= end; x++)
            {
                // As DrawSpanFilledDepth: the face's colours are already
                // resolved, so the pixel only picks one of them.
                StorePixel(x, y, cells[x, y]);
            }
        }
    }

    // Draw one depth-tested scanline of a flat-shaded triangle, interpolating
    // inverse depth from i0 at x0 to i1 at x1.
    private void FillPolygonDepth(
        Vector2[] points,
        float[] depths,
        in DitherCells cells,
        bool writeColor,
        int surfaceId)
    {
        if (points == null || depths == null || depths.Length < points.Length)
        {
            return;
        }

        // Create triangles of which each share the first vertex
        for (int i = 1; i < points.Length - 1; i++)
        {
            DrawTriangleFilledDepth(
                points[0],
                points[i],
                points[i + 1],
                depths[0],
                depths[i],
                depths[i + 1],
                in cells,
                writeColor,
                surfaceId);
        }
    }

    private void DrawSpanFilledDepth(
        int y,
        float x0,
        float x1,
        float i0,
        float i1,
        in DitherCells cells,
        bool writeColor,
        int surfaceId)
    {
        int start = Math.Max((int)MathF.Floor(x0), _clipLeft);
        int end = Math.Min((int)MathF.Floor(x1), _clipRight - 1);
        float span = x1 - x0;

        for (int x = start; x <= end; x++)
        {
            float t = span <= 0 ? 0f : Math.Clamp((x - x0) / span, 0f, 1f);
            if (DepthTest(x, y, i0 + ((i1 - i0) * t), surfaceId) && writeColor)
            {
                // The face's sixteen possible colours are already resolved,
                // so the pixel only picks the one its dither cell holds;
                // without a dither every cell holds the same colour.
                StorePixel(x, y, cells[x, y]);
            }
        }
    }

    // Draw one depth-tested scanline of a textured triangle. The texture
    // coordinates arrive already divided by depth and are recovered per
    // pixel. The mip level is chosen once for the whole span, from how many
    // texels the recovered UV covers at each end versus how many pixels the
    // span covers - this is what keeps distant, receding faces (SCR's road,
    // seen nearly edge-on) sampling a pre-averaged level instead of aliasing
    // against the full-resolution texture.
    private void DrawSpanTexturedDepth(
        int y,
        float x0,
        float x1,
        float i0,
        float i1,
        Vector2 uv0,
        Vector2 uv1,
        FastBitmap[] mipChain)
    {
        int start = Math.Max((int)MathF.Floor(x0), _clipLeft);
        int end = Math.Min((int)MathF.Floor(x1), _clipRight - 1);
        if (start > end)
        {
            return;
        }

        float span = x1 - x0;
        FastBitmap mip = SelectMip(mipChain, x0, x1, start, end, i0, i1, uv0, uv1);

        for (int x = start; x <= end; x++)
        {
            float t = span <= 0 ? 0f : Math.Clamp((x - x0) / span, 0f, 1f);
            float inverseDepth = i0 + ((i1 - i0) * t);
            if (DepthTest(x, y, inverseDepth, surfaceId: 0))
            {
                Vector2 uv = Vector2.Lerp(uv0, uv1, t) / inverseDepth;
                StorePixel(x, y, SampleTexture(mip, uv));
            }
        }
    }

    private void DrawLineInt(int x0, int y0, int x1, int y1, in FastColor color)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            // The walk runs over the whole line, so which pixels it picks
            // never depends on where it was clipped; only the write is gated,
            // and by the clip rectangle alone - that rectangle is inside the
            // framebuffer, so the one test is the whole of the bounds check.
            if (x0 >= _clipLeft && x0 < _clipRight && y0 >= _clipTop && y0 < _clipBottom)
            {
                StorePixel(x0, y0, color);
            }

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    // Depth-tested variant of DrawLineInt: inverse depth (1/z) is
    // interpolated along the Bresenham walk by its fraction of the major
    // axis, matching how DrawSpanFilledDepth interpolates across a span.
    private void DrawLineIntDepth(int x0, int y0, int x1, int y1, float inverseStart, float inverseEnd, in FastColor color, int surfaceId)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int steps = Math.Max(dx, dy);

        for (int step = 0; step <= steps; step++)
        {
            float t = steps == 0 ? 0f : (float)step / steps;
            PlotDepthTestedPixel(x0, y0, inverseStart + ((inverseEnd - inverseStart) * t), color, surfaceId);

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private void PlotDepthTestedPixel(int x, int y, float inverseDepth, in FastColor color, int surfaceId)
    {
        if (x < _clipLeft || x >= _clipRight || y < _clipTop || y >= _clipBottom)
        {
            return;
        }

        if (DepthTest(x, y, inverseDepth, surfaceId))
        {
            StorePixel(x, y, color);
        }
    }

    // The one place a colour reaches the framebuffer. A translucent one is
    // composited with what is already there; the opaque case, which is nearly
    // every draw, skips the read.
    private void StorePixel(int x, int y, in FastColor color)
        => _screen.SetPixel(x, y, color.A == 255 ? color : FastColor.Blend(color, _screen.GetPixel(x, y)));

    // Test-and-set a pixel's inverse depth: the draw passes when at least
    // as near as what is already there, so later draws win ties (as the
    // original Direct3D LESSEQUAL depth test). A non-zero surfaceId also
    // passes against itself - the caller is drawing the same surface that
    // already owns the pixel, so there is nothing to hide it behind - and
    // that case leaves the stored depth alone rather than pushing it back.
    private bool DepthTest(int x, int y, float inverseDepth, int surfaceId)
    {
        _depth ??= new float[(int)ScreenWidth * (int)ScreenHeight];
        _surfaceIds ??= new int[(int)ScreenWidth * (int)ScreenHeight];

        int index = (y * (int)ScreenWidth) + x;
        if (inverseDepth < _depth[index])
        {
            return surfaceId != 0 && _surfaceIds[index] == surfaceId;
        }

        _depth[index] = inverseDepth;
        _surfaceIds[index] = surfaceId;
        return true;
    }

    private void DrawRectangleFilledInt(int startX, int startY, int width, int height, in FastColor color)
    {
        startX = Math.Min(Math.Max(startX, _clipLeft), _clipRight - 1);
        startY = Math.Min(Math.Max(startY, _clipTop), _clipBottom - 1);
        int endX = Math.Min(Math.Max(startX + width - 1, _clipLeft), _clipRight - 1);
        int endY = Math.Min(Math.Max(startY + height - 1, _clipTop), _clipBottom - 1);

        // Draw horizontal lined
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                StorePixel(x, y, color);
            }
        }
    }

    private void DrawRectangleInt(int startX, int startY, int width, int height, in FastColor color)
    {
        startX = Math.Min(Math.Max(startX, _clipLeft), _clipRight - 1);
        startY = Math.Min(Math.Max(startY, _clipTop), _clipBottom - 1);
        int endX = Math.Min(Math.Max(startX + width - 1, _clipLeft), _clipRight - 1);
        int endY = Math.Min(Math.Max(startY + height - 1, _clipTop), _clipBottom - 1);

        // Draw horizontal lines
        for (int x = startX; x <= endX; x++)
        {
            StorePixel(x, startY, color);
            StorePixel(x, endY, color);
        }

        for (int y = startY + 1; y <= endY - 1; y++)
        {
            StorePixel(startX, y, color);
            StorePixel(endX, y, color);
        }
    }

    private void ClearTextCache()
    {
        foreach ((string _, FastBitmap bitmap) in _textCacheOrder)
        {
            bitmap.Dispose();
        }

        _textCacheOrder.Clear();
        _textCache.Clear();
    }

    private FastBitmap GenerateTextBitmap(string text, string fontType, in FastColor color)
    {
        string key = $"{fontType}_{color}_{text}";

        if (_textCache.TryGetValue(key, out LinkedListNode<(string Key, FastBitmap Bitmap)>? cacheNode))
        {
            _textCacheOrder.Remove(cacheNode);
            _textCacheOrder.AddFirst(cacheNode);
            return cacheNode.Value.Bitmap;
        }

        FastBitmap bitmap = _fontRasterisers.Rasterise(text, fontType, color);

        if (_textCacheOrder.Count >= TextCacheCapacity)
        {
            LinkedListNode<(string Key, FastBitmap Bitmap)> lru = _textCacheOrder.Last!;
            _textCacheOrder.RemoveLast();
            _textCache.Remove(lru.Value.Key);
            lru.Value.Bitmap.Dispose();
        }

        LinkedListNode<(string Key, FastBitmap Bitmap)> node = _textCacheOrder.AddFirst((key, bitmap));
        _textCache.Add(key, node);
        return bitmap;
    }
}
