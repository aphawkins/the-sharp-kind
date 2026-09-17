// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Diagnostics;
using System.Numerics;
using Microsoft.Extensions.Logging;
using SDL;
using SharpKind.Assets;
using SharpKind.Graphics;
using SharpKind.Graphics.Rendering;
using static SDL.SDL3;

namespace SharpKind.SDL;

public sealed unsafe partial class SDLGraphics : IGraphics, IDisposable
{
    private const int CircleSegments = 32;

    private readonly SDLRenderer _renderer;
    private readonly Dictionary<(string FontType, string Text, uint Color), TextTextureEntry> _textTextures = [];

    // Shared with the software backend so text reads the same on both.
    private FontRasteriserSet _fontRasterisers = FontRasteriserSet.Only(new BitmapFontRasteriser([]));
    private Dictionary<string, FastBitmap> _images = [];
    private Dictionary<string, nint> _imageTextures = [];
    private bool _isDisposed;

    // CPU-rasterised, per-pixel depth-tested layer for DrawPolygonFilledDepth
    // / DrawPolygonTexturedDepth: SDL's accelerated 2D renderer has no depth
    // buffer, so depth-tested polygons would otherwise pop/flicker in
    // submission order. Composited onto the renderer as one blit
    // (FlushDepthLayer) so it lands in the right place in the draw order and
    // inherits the clip active when it was rasterised.
    //
    // DrawLine also routes through this layer while a depth pass is open, so
    // a line submitted between two depth-tested faces stays in submission
    // order with a single flush at the end, rather than needing every later
    // face re-flushed around it.
    private FastBitmap? _depthLayer;

    // GPU counterpart of _depthLayer, created once and reused via streaming
    // + SDL_UpdateTexture to avoid a fresh GPU allocation and sync upload per
    // frame. Blend mode is set explicitly: SDL_CreateTexture (unlike
    // SDL_CreateTextureFromSurface) does not infer it from pixel alpha.
    private nint _depthTexture;

    private float[]? _depthBuffer;

    // The surface currently occupying each pixel, 0 = none; see
    // SoftwareGraphics._surfaceIds.
    private int[]? _surfaceIds;
    private bool _depthLayerDirty;
    private bool _depthPassOpen;

    // All drawing targets this persistent off-screen texture rather than the
    // swap-chain backbuffer directly: present rate outpaces the game's tick
    // rate, so a multi-buffered swap chain would otherwise flicker between
    // stale buffers. ScreenUpdate() blits this onto the backbuffer fresh on
    // every present instead.
    private nint _frameTarget;

    private SDLGraphics(SDLRenderer renderer, float screenWidth, float screenHeight)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        _renderer = renderer;
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
    }

    // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~SDLGraphics()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public float ScreenHeight { get; }

    public float ScreenWidth { get; }

    public FontKind FontKind
    {
        get => _fontRasterisers.Kind;

        set
        {
            if (_isDisposed)
            {
                return;
            }

            FontKind previous = _fontRasterisers.Kind;

            // A kind change makes every cached texture's rasterised text stale.
            if (_fontRasterisers.Select(value) != previous)
            {
                ClearTextTextures();
            }
        }
    }

    private SDL_Renderer* NativeRenderer => (SDL_Renderer*)(nint)_renderer;

    public static SDLGraphics Create(SDLRenderer renderer, float screenWidth, float screenHeight, IAssetLocator assetLocator)
        => Create(renderer, screenWidth, screenHeight, assetLocator, FontKind.Bitmap, null);

    public static SDLGraphics Create(
        SDLRenderer renderer,
        float screenWidth,
        float screenHeight,
        IAssetLocator assetLocator,
        FontKind fontKind,
        ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(assetLocator);

        // Shared managed decoder, not SDL_LoadBMP, so both backends see the same pixels.
        AssetSet assets = AssetSet.Load(assetLocator, logger);

        SDLGraphics graphics = new(renderer, screenWidth, screenHeight)
        {
            _images = assets.Images,

            _fontRasterisers = FontRasterisers.Load(assets, assetLocator, fontKind),
        };

        // Created once, not per-draw: a texture upload is synchronous and images are static.
        graphics._imageTextures = graphics._images.ToDictionary(
            x => x.Key,
            x => CreateImageTexture(graphics.NativeRenderer, x.Value));

        graphics._frameTarget = SDLGuard.Execute(() => (nint)SDL_CreateTexture(
            graphics.NativeRenderer,
            SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888,
            SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET,
            (int)screenWidth,
            (int)screenHeight));

        // Nearest, not SDL's default linear, so magnifying duplicates pixels rather than filtering them.
        SDLGuard.Execute(() => SDL_SetTextureScaleMode(
            (SDL_Texture*)graphics._frameTarget,
            SDL_ScaleMode.SDL_SCALEMODE_NEAREST));

        // SDL's default blend mode is NONE, which ignores alpha entirely.
        SDLGuard.Execute(() => SDL_SetRenderDrawBlendMode(
            graphics.NativeRenderer,
            SDL_BlendMode.SDL_BLENDMODE_BLEND));

        // Drawing targets _frameTarget from here on; ScreenUpdate() switches to the window briefly to blit it.
        SDLGuard.Execute(() => SDL_SetRenderTarget(graphics.NativeRenderer, (SDL_Texture*)graphics._frameTarget));

        return graphics;
    }

    public void Clear()
    {
        if (_isDisposed)
        {
            return;
        }

        FlushDepthLayer();
        EvictStaleTextTextures();

        SetRenderDrawColor(BaseColors.Black);

        SDLGuard.Execute(() => SDL_RenderClear(NativeRenderer));
    }

    public void ClearDepth()
    {
        if (_isDisposed)
        {
            return;
        }

        _depthLayer ??= new FastBitmap((int)ScreenWidth, (int)ScreenHeight);
        _depthBuffer ??= new float[(int)ScreenWidth * (int)ScreenHeight];
        _surfaceIds ??= new int[(int)ScreenWidth * (int)ScreenHeight];

        if (_depthTexture == nint.Zero)
        {
            _depthTexture = SDLGuard.Execute(() => (nint)SDL_CreateTexture(
                NativeRenderer,
                SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888,
                SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                _depthLayer.Width,
                _depthLayer.Height));

            SDLGuard.Execute(() => SDL_SetTextureBlendMode((SDL_Texture*)_depthTexture, SDL_BlendMode.SDL_BLENDMODE_BLEND));
        }

        _depthLayer.Clear(BaseColors.TransparentBlack);
        Array.Clear(_depthBuffer);
        Array.Clear(_surfaceIds);
        _depthLayerDirty = false;
        _depthPassOpen = true;
    }

    // Callers changing the clip region must flush first: an unflushed depth layer blits under the new clip, not the one it was rasterised under.
    public void FlushDepth() => FlushDepthLayer();

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void DrawCircle(Vector2 centre, float radius, FastColor color)
    {
        if (_isDisposed)
        {
            return;
        }

        Vector2 previous = centre + new Vector2(radius, 0);

        for (int i = 1; i <= CircleSegments; i++)
        {
            float angle = i * MathF.Tau / CircleSegments;
            Vector2 next = centre + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
            DrawLine(previous, next, color);
            previous = next;
        }
    }

    public void DrawCircleFilled(Vector2 centre, float radius, FastColor color)
    {
        if (_isDisposed)
        {
            return;
        }

        // No filled-circle primitive without the removed gfx dependency, so build a triangle fan sharing the centre.
        Vector2 previous = centre + new Vector2(radius, 0);

        for (int i = 1; i <= CircleSegments; i++)
        {
            float angle = i * MathF.Tau / CircleSegments;
            Vector2 next = centre + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
            DrawTriangleFilled(centre, previous, next, color);
            previous = next;
        }
    }

    public void DrawImage(string imageType, Vector2 position)
    {
        if (_isDisposed)
        {
            return;
        }

        FlushDepthLayer();

        FastBitmap image = _images[imageType];
        nint texturePtr = _imageTextures[imageType];

        SDLGuard.Execute(() =>
        {
            SDL_FRect dest = new()
            {
                x = position.X,
                y = position.Y,
                w = image.Width,
                h = image.Height,
            };

            return SDL_RenderTexture(NativeRenderer, (SDL_Texture*)texturePtr, null, &dest);
        });
    }

    public void DrawImageCentre(string imageType, float y)
    {
        if (_isDisposed)
        {
            return;
        }

        float x = (ScreenWidth - _images[imageType].Width) / 2;
        DrawImage(imageType, new(x, y));
    }

    public Vector2 ImageSize(string imageType)
    {
        FastBitmap image = _images[imageType];
        return new(image.Width, image.Height);
    }

    public void DrawImagePart(string imageType, Vector2 position, Vector2 size, Vector2 sourcePosition, Vector2 sourceSize)
    {
        if (_isDisposed)
        {
            return;
        }

        FlushDepthLayer();

        nint texturePtr = _imageTextures[imageType];

        SDL_FlipMode flip = sourceSize.X < 0 ? SDL_FlipMode.SDL_FLIP_HORIZONTAL : SDL_FlipMode.SDL_FLIP_NONE;

        SDLGuard.Execute(() =>
        {
            SDL_FRect source = new()
            {
                x = sourcePosition.X,
                y = sourcePosition.Y,
                w = MathF.Abs(sourceSize.X),
                h = sourceSize.Y,
            };

            SDL_FRect dest = new()
            {
                x = position.X,
                y = position.Y,
                w = size.X,
                h = size.Y,
            };

            return SDL_RenderTextureRotated(NativeRenderer, (SDL_Texture*)texturePtr, &source, &dest, 0, null, flip);
        });
    }

    public void DrawLine(Vector2 lineStart, Vector2 lineEnd, FastColor color)
    {
        if (_isDisposed)
        {
            return;
        }

        if (_depthPassOpen && _depthLayer != null)
        {
            DrawLineToDepthLayer(lineStart, lineEnd, color);
            _depthLayerDirty = true;
            return;
        }

        FlushDepthLayer();
        SetRenderDrawColor(color);

        SDLGuard.Execute(() => SDL_RenderLine(NativeRenderer, lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y));
    }

    public void DrawLineDepth(Vector2 lineStart, Vector2 lineEnd, float depthStart, float depthEnd, FastColor color, int surfaceId)
    {
        if (_isDisposed || _depthLayer == null)
        {
            return;
        }

        DrawLineDepthToLayer(lineStart, lineEnd, depthStart, depthEnd, color, surfaceId);
        _depthLayerDirty = true;
    }

    public void DrawPixel(Vector2 position, FastColor color)
    {
        if (_isDisposed)
        {
            return;
        }

        FlushDepthLayer();
        SetRenderDrawColor(color);

        SDLGuard.Execute(() => SDL_RenderPoint(NativeRenderer, position.X, position.Y));
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

    // dither is ignored: this backend fills on the GPU and has no per-pixel
    // hook to ask it at. Dithering is a Software-backend feature.
    public void DrawPolygonFilled(Vector2[] points, FastColor faceColor, IColourQuantiser? dither)
    {
        if (points == null)
        {
            return;
        }

        // SDL_RenderGeometry only renders triangles and quads?
        // Create triangles of which each share the first vertex
        for (int i = 1; i < points.Length - 1; i++)
        {
            DrawTriangleFilled(points[0], points[i], points[i + 1], faceColor);
        }
    }

    public void DrawPolygonFilledDepth(Vector2[] points, float[] depths, FastColor faceColor)
        => DrawPolygonFilledDepth(points, depths, faceColor, dither: null);

    // As DrawPolygonFilled: no per-pixel hook on this backend.
    public void DrawPolygonFilledDepth(Vector2[] points, float[] depths, FastColor faceColor, IColourQuantiser? dither)
        => FillPolygonDepth(points, depths, faceColor, writeColor: true, surfaceId: 0);

    public void FillDepth(Vector2[] points, float[] depths, int surfaceId)
        => FillPolygonDepth(points, depths, BaseColors.Black, writeColor: false, surfaceId);

    public void DrawPolygonTextured(Vector2[] points, Vector2[] textureCoords, FastBitmap texture)
    {
        if (points == null || textureCoords == null || texture == null || textureCoords.Length < points.Length)
        {
            return;
        }

        // SDL can't sample a FastBitmap directly, so approximate with a flat fill at the average texture coordinate.
        Vector2 averageUv = Vector2.Zero;
        for (int i = 0; i < points.Length; i++)
        {
            averageUv += textureCoords[i];
        }

        averageUv /= points.Length;

        int x = Math.Clamp((int)(averageUv.X * texture.Width), 0, texture.Width - 1);
        int y = Math.Clamp((int)(averageUv.Y * texture.Height), 0, texture.Height - 1);
        DrawPolygonFilled(points, texture.GetPixel(x, y));
    }

    public void DrawPolygonTexturedDepth(Vector2[] points, float[] depths, Vector2[] textureCoords, FastBitmap texture)
    {
        if (_isDisposed ||
            points == null ||
            depths == null ||
            textureCoords == null ||
            texture == null ||
            depths.Length < points.Length ||
            textureCoords.Length < points.Length ||
            _depthLayer == null)
        {
            return;
        }

        for (int i = 1; i < points.Length - 1; i++)
        {
            DrawTriangleTexturedDepthToLayer(
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

        _depthLayerDirty = true;
    }

    public void DrawRectangle(Vector2 position, float width, float height, FastColor color)
    {
        if (_isDisposed)
        {
            return;
        }

        FlushDepthLayer();
        SetRenderDrawColor(color);

        float x = position.X;
        float y = position.Y;

        SDLGuard.Execute(() =>
        {
            SDL_FRect rectangle = new()
            {
                x = x,
                y = y,
                w = width + 1,
                h = height + 1,
            };

            return SDL_RenderRect(NativeRenderer, &rectangle);
        });
    }

    public void DrawRectangleCentre(float y, float width, float height, FastColor color)
        => DrawRectangle(new((ScreenWidth - width) / 2, y), width, height, color);

    public void DrawRectangleFilled(Vector2 position, float width, float height, FastColor color)
    {
        if (_isDisposed)
        {
            return;
        }

        FlushDepthLayer();
        SetRenderDrawColor(color);

        float x = position.X;
        float y = position.Y;

        SDLGuard.Execute(() =>
        {
            SDL_FRect rectangle = new()
            {
                x = x,
                y = y,
                w = width + 1,
                h = height + 1,
            };

            return SDL_RenderFillRect(NativeRenderer, &rectangle);
        });
    }

    // No texture is created for text that is only being measured - the
    // rasteriser sizes it without drawing it.
    public Vector2 MeasureText(string text, string fontType)
        => _isDisposed ? new(0, 0) : _fontRasterisers.Measure(text, fontType);

    public void DrawTextCentre(float y, string text, string fontType, FastColor color)
    {
        if (_isDisposed || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        FlushDepthLayer();
        TextTextureEntry entry = GetOrCreateTextTexture(fontType, text, color);
        float destX = (ScreenWidth / 2) - (entry.Width / 2);
        float destY = y;

        SDLGuard.Execute(() =>
        {
            SDL_FRect dest = new() { x = destX, y = destY, w = entry.Width, h = entry.Height };
            return SDL_RenderTexture(NativeRenderer, (SDL_Texture*)entry.Texture, null, &dest);
        });
    }

    public void DrawTextLeft(Vector2 position, string text, string fontType, FastColor color)
    {
        if (_isDisposed || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        FlushDepthLayer();
        TextTextureEntry entry = GetOrCreateTextTexture(fontType, text, color);
        float destX = position.X;
        float destY = position.Y;

        SDLGuard.Execute(() =>
        {
            SDL_FRect dest = new() { x = destX, y = destY, w = entry.Width, h = entry.Height };
            return SDL_RenderTexture(NativeRenderer, (SDL_Texture*)entry.Texture, null, &dest);
        });
    }

    public void DrawTextRight(Vector2 position, string text, string fontType, FastColor color)
    {
        if (_isDisposed || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        FlushDepthLayer();
        TextTextureEntry entry = GetOrCreateTextTexture(fontType, text, color);
        float destX = position.X - entry.Width;
        float destY = position.Y;

        SDLGuard.Execute(() =>
        {
            SDL_FRect dest = new() { x = destX, y = destY, w = entry.Width, h = entry.Height };
            return SDL_RenderTexture(NativeRenderer, (SDL_Texture*)entry.Texture, null, &dest);
        });
    }

    public void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, FastColor color)
    {
        DrawLine(a, b, color);
        DrawLine(b, c, color);
        DrawLine(c, a, color);
    }

    public void DrawTriangleFilled(Vector2 a, Vector2 b, Vector2 c, FastColor color)
    {
        if (_isDisposed)
        {
            return;
        }

        FlushDepthLayer();

        SDLGuard.Execute(() =>
        {
            SDL_Vertex* vertices = stackalloc SDL_Vertex[3];
            vertices[0] = ConvertVertex(a, color);
            vertices[1] = ConvertVertex(b, color);
            vertices[2] = ConvertVertex(c, color);

            return SDL_RenderGeometry(NativeRenderer, null, vertices, 3, null, 0);
        });
    }

    public void ScreenUpdate()
    {
        if (_isDisposed)
        {
            return;
        }

        FlushDepthLayer();

        // Blit the persistent frame texture onto whichever swap-chain buffer is current; see _frameTarget's field comment.
        SDLGuard.Execute(() => SDL_SetRenderTarget(NativeRenderer, null));
        SDLGuard.Execute(() =>
        {
            SDL_FRect dest = new() { x = 0, y = 0, w = ScreenWidth, h = ScreenHeight };
            return SDL_RenderTexture(NativeRenderer, (SDL_Texture*)_frameTarget, null, &dest);
        });
        SDLGuard.Execute(() => SDL_RenderPresent(NativeRenderer));
        SDLGuard.Execute(() => SDL_SetRenderTarget(NativeRenderer, (SDL_Texture*)_frameTarget));
    }

    public void SaveScreen(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_isDisposed)
        {
            return;
        }

        FlushDepthLayer();

        int width = (int)ScreenWidth;
        int height = (int)ScreenHeight;

        nint rawSurfacePtr = SDLGuard.Execute(() =>
        {
            SDL_Rect rect = new() { x = 0, y = 0, w = width, h = height };
            return (nint)SDL_RenderReadPixels(NativeRenderer, &rect);
        });

        // SDL_RenderReadPixels does not guarantee a particular pixel format,
        // so convert explicitly to match FastBitmap's tightly packed ARGB8888 layout.
        nint convertedSurfacePtr = SDLGuard.Execute(
            () => (nint)SDL_ConvertSurface((SDL_Surface*)rawSurfacePtr, SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888));
        SDL_DestroySurface((SDL_Surface*)rawSurfacePtr);

        SDL_Surface* converted = (SDL_Surface*)convertedSurfacePtr;
        Debug.Assert(converted->pitch == width * 4, "Converted surface is not tightly packed.");

        using FastBitmap bitmap = new(width, height);
        long byteCount = (long)width * height * 4;
        Buffer.MemoryCopy((void*)converted->pixels, (void*)bitmap.BitmapHandle, byteCount, byteCount);
        SDL_DestroySurface(converted);

        BitmapWriter.Write(bitmap, path);
    }

    public void SetClipRegion(Vector2 position, float width, float height)
    {
        if (_isDisposed)
        {
            return;
        }

        int x = (int)position.X;
        int y = (int)position.Y;
        int w = (int)width;
        int h = (int)height;

        SDLGuard.Execute(() =>
        {
            SDL_Rect rectangle = new() { x = x, y = y, w = w, h = h };
            return SDL_SetRenderClipRect(NativeRenderer, &rectangle);
        });
    }

    // Uploads a decoded bitmap as a static texture. Blend mode is set explicitly since SDL_CreateTexture, unlike SDL_CreateTextureFromSurface, does not infer it from pixel alpha.
    private static nint CreateImageTexture(SDL_Renderer* renderer, FastBitmap image)
    {
        nint texture = SDLGuard.Execute(() => (nint)SDL_CreateTexture(
            renderer,
            SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888,
            SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC,
            image.Width,
            image.Height));

        SDLGuard.Execute(() => SDL_UpdateTexture((SDL_Texture*)texture, null, image.BitmapHandle, image.Width * 4));
        SDLGuard.Execute(() => SDL_SetTextureBlendMode((SDL_Texture*)texture, SDL_BlendMode.SDL_BLENDMODE_BLEND));

        return texture;
    }

    private static SDL_Vertex ConvertVertex(Vector2 point, in FastColor color) => new()
    {
        position = new() { x = point.X, y = point.Y },
        tex_coord = new() { x = 0.0f, y = 0.0f },
        color = ToSDLFColor(color),
    };

    // SDL_Vertex colours are normalised floats (0..1), not bytes.
    private static SDL_FColor ToSDLFColor(in FastColor color) => new()
    {
        r = color.R / 255f,
        g = color.G / 255f,
        b = color.B / 255f,
        a = color.A / 255f,
    };

    // Interpolation parameter of edge p0-p1 at scanline y (p0.Y <= p1.Y); 0 for a horizontal/degenerate edge. Mirrors SoftwareGraphics's EdgeT.
    private static float EdgeT(Vector2 p0, Vector2 p1, float y)
    {
        float dy = p1.Y - p0.Y;
        return dy <= 0 ? 0f : Math.Clamp((y - p0.Y) / dy, 0f, 1f);
    }

    // Sample the texture at a [0,1] coordinate, clamping at the edges.
    private static FastColor SampleDepthLayerTexture(FastBitmap texture, Vector2 uv)
    {
        int x = Math.Clamp((int)(uv.X * texture.Width), 0, texture.Width - 1);
        int y = Math.Clamp((int)(uv.Y * texture.Height), 0, texture.Height - 1);
        return texture.GetPixel(x, y);
    }

    // Cached by (font, text, colour): a glyph texture is a CPU render plus a synchronous GPU upload. Entries unused since the last Clear() are evicted there.
    private TextTextureEntry GetOrCreateTextTexture(string fontType, string text, in FastColor color)
    {
        (string FontType, string Text, uint Color) key = (fontType, text, color.Argb);

        if (_textTextures.TryGetValue(key, out TextTextureEntry? cached))
        {
            cached.UsedThisFrame = true;
            return cached;
        }

        // The same pixels the software backend would blit, uploaded as a
        // texture instead - which is what makes the two backends agree.
        using FastBitmap bitmap = _fontRasterisers.Rasterise(text, fontType, color);

        TextTextureEntry entry = new()
        {
            Texture = CreateImageTexture(NativeRenderer, bitmap),
            Width = bitmap.Width,
            Height = bitmap.Height,
            UsedThisFrame = true,
        };

        _textTextures[key] = entry;
        return entry;
    }

    private void ClearTextTextures()
    {
        foreach (KeyValuePair<(string, string, uint), TextTextureEntry> entry in _textTextures)
        {
            SDL_DestroyTexture((SDL_Texture*)entry.Value.Texture);
        }

        _textTextures.Clear();
    }

    private void EvictStaleTextTextures()
    {
        List<(string, string, uint)> stale = [];

        foreach (KeyValuePair<(string, string, uint), TextTextureEntry> pair in _textTextures)
        {
            if (!pair.Value.UsedThisFrame)
            {
                SDL_DestroyTexture((SDL_Texture*)pair.Value.Texture);
                stale.Add(pair.Key);
                continue;
            }

            pair.Value.UsedThisFrame = false;
        }

        foreach ((string, string, uint) key in stale)
        {
            _textTextures.Remove(key);
        }
    }

    // Composites the accumulated depth layer as one texture blit; a no-op once flushed. Called at
    // the top of every other draw method (and ScreenUpdate/SaveScreen) so it lands between
    // whatever was drawn before and after it.
    private void FlushDepthLayer()
    {
        if (!_depthLayerDirty || _depthLayer == null || _depthTexture == nint.Zero)
        {
            return;
        }

        _depthLayerDirty = false;
        _depthPassOpen = false;

        SDLGuard.Execute(() => SDL_UpdateTexture(
            (SDL_Texture*)_depthTexture,
            null,
            _depthLayer.BitmapHandle,
            _depthLayer.Width * 4));

        SDLGuard.Execute(() =>
        {
            SDL_FRect dest = new() { x = 0, y = 0, w = ScreenWidth, h = ScreenHeight };
            return SDL_RenderTexture(NativeRenderer, (SDL_Texture*)_depthTexture, null, &dest);
        });

        // Cheap insurance against a double flush within one pass repainting already-on-screen pixels.
        _depthLayer.Clear(BaseColors.TransparentBlack);
    }

    // Bresenham line into the CPU depth layer, matching SoftwareGraphics's DrawLineInt. No depth test, for callers with no depth for the line; others use DrawLineDepthToLayer.
    private void DrawLineToDepthLayer(Vector2 lineStart, Vector2 lineEnd, in FastColor color)
    {
        int x0 = (int)MathF.Floor(lineStart.X);
        int y0 = (int)MathF.Floor(lineStart.Y);
        int x1 = (int)MathF.Floor(lineEnd.X);
        int y1 = (int)MathF.Floor(lineEnd.Y);

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int width = (int)ScreenWidth;
        int height = (int)ScreenHeight;

        while (true)
        {
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
            {
                _depthLayer!.SetPixel(x0, y0, color);
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

    // Depth-tested Bresenham line into the CPU depth layer: inverse depth
    // (1/z) is interpolated along the walk by its fraction of the major
    // axis. Mirrors SoftwareGraphics.DrawLineIntDepth.
    private void DrawLineDepthToLayer(
        Vector2 lineStart,
        Vector2 lineEnd,
        float depthStart,
        float depthEnd,
        in FastColor color,
        int surfaceId)
    {
        int x0 = (int)MathF.Floor(lineStart.X);
        int y0 = (int)MathF.Floor(lineStart.Y);
        int x1 = (int)MathF.Floor(lineEnd.X);
        int y1 = (int)MathF.Floor(lineEnd.Y);

        float inverseStart = 1f / depthStart;
        float inverseEnd = 1f / depthEnd;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int steps = Math.Max(dx, dy);

        for (int step = 0; step <= steps; step++)
        {
            float t = steps == 0 ? 0f : (float)step / steps;
            PlotDepthTestedLayerPixel(x0, y0, inverseStart + ((inverseEnd - inverseStart) * t), color, surfaceId);

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

    private void PlotDepthTestedLayerPixel(int x, int y, float inverseDepth, in FastColor color, int surfaceId)
    {
        if (x < 0 || x >= (int)ScreenWidth || y < 0 || y >= (int)ScreenHeight)
        {
            return;
        }

        if (DepthTestLayer(x, y, inverseDepth, surfaceId))
        {
            _depthLayer!.SetPixel(x, y, color);
        }
    }

    private void FillPolygonDepth(Vector2[] points, float[] depths, in FastColor faceColor, bool writeColor, int surfaceId)
    {
        if (_isDisposed || points == null || depths == null || depths.Length < points.Length || _depthLayer == null)
        {
            return;
        }

        for (int i = 1; i < points.Length - 1; i++)
        {
            DrawTriangleFilledDepthToLayer(
                points[0],
                points[i],
                points[i + 1],
                depths[0],
                depths[i],
                depths[i + 1],
                faceColor,
                writeColor,
                surfaceId);
        }

        _depthLayerDirty = true;
    }

    // Depth-tested triangle fill into the CPU depth layer; inverse depth (1/z) interpolated
    // linearly (perspective-correct). writeColor false primes the buffer for a hidden-line pass
    // without drawing. Mirrors SoftwareGraphics.DrawTriangleFilledDepth.
    private void DrawTriangleFilledDepthToLayer(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        float za,
        float zb,
        float zc,
        in FastColor color,
        bool writeColor,
        int surfaceId)
    {
        if (za <= 0 || zb <= 0 || zc <= 0)
        {
            return;
        }

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

        int firstY = Math.Max((int)MathF.Ceiling(a.Y), 0);
        int lastY = Math.Min((int)MathF.Floor(c.Y), (int)ScreenHeight - 1);

        for (int y = firstY; y <= lastY; y++)
        {
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

            DrawSpanFilledDepthToLayer(y, x0, x1, i0, i1, color, writeColor, surfaceId);
        }
    }

    // Draw one depth-tested scanline of a flat-shaded triangle into the depth
    // layer, interpolating inverse depth from i0 at x0 to i1 at x1.
    private void DrawSpanFilledDepthToLayer(
        int y,
        float x0,
        float x1,
        float i0,
        float i1,
        in FastColor color,
        bool writeColor,
        int surfaceId)
    {
        int start = Math.Max((int)MathF.Floor(x0), 0);
        int end = Math.Min((int)MathF.Floor(x1), (int)ScreenWidth - 1);
        float span = x1 - x0;

        for (int x = start; x <= end; x++)
        {
            float t = span <= 0 ? 0f : Math.Clamp((x - x0) / span, 0f, 1f);
            if (DepthTestLayer(x, y, i0 + ((i1 - i0) * t), surfaceId) && writeColor)
            {
                _depthLayer!.SetPixel(x, y, color);
            }
        }
    }

    // Depth-tested, perspective-correct textured triangle fill into the CPU
    // depth layer. Mirrors SoftwareGraphics.DrawTriangleTexturedDepth.
    private void DrawTriangleTexturedDepthToLayer(
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

        int firstY = Math.Max((int)MathF.Ceiling(a.Y), 0);
        int lastY = Math.Min((int)MathF.Floor(c.Y), (int)ScreenHeight - 1);

        for (int y = firstY; y <= lastY; y++)
        {
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

            DrawSpanTexturedDepthToLayer(y, x0, x1, i0, i1, uv0, uv1, texture);
        }
    }

    // Draw one depth-tested scanline of a textured triangle into the depth
    // layer. The texture coordinates arrive already divided by depth and are
    // recovered per pixel.
    private void DrawSpanTexturedDepthToLayer(
        int y,
        float x0,
        float x1,
        float i0,
        float i1,
        Vector2 uv0,
        Vector2 uv1,
        FastBitmap texture)
    {
        int start = Math.Max((int)MathF.Floor(x0), 0);
        int end = Math.Min((int)MathF.Floor(x1), (int)ScreenWidth - 1);
        float span = x1 - x0;

        for (int x = start; x <= end; x++)
        {
            float t = span <= 0 ? 0f : Math.Clamp((x - x0) / span, 0f, 1f);
            float inverseDepth = i0 + ((i1 - i0) * t);
            if (DepthTestLayer(x, y, inverseDepth, surfaceId: 0))
            {
                Vector2 uv = Vector2.Lerp(uv0, uv1, t) / inverseDepth;
                _depthLayer!.SetPixel(x, y, SampleDepthLayerTexture(texture, uv));
            }
        }
    }

    // Test-and-set a depth-layer pixel's inverse depth: the draw passes when
    // at least as near as what is already there, so later draws win ties
    // (matching SoftwareGraphics.DepthTest and the original Direct3D
    // LESSEQUAL depth test).
    private bool DepthTestLayer(int x, int y, float inverseDepth, int surfaceId)
    {
        if (x < 0 || y < 0 || x >= (int)ScreenWidth || y >= (int)ScreenHeight)
        {
            return false;
        }

        int index = (y * (int)ScreenWidth) + x;
        if (inverseDepth < _depthBuffer![index])
        {
            return surfaceId != 0 && _surfaceIds![index] == surfaceId;
        }

        _depthBuffer[index] = inverseDepth;
        _surfaceIds![index] = surfaceId;
        return true;
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (disposing)
            {
                // dispose managed state (managed objects)
                _depthLayer?.Dispose();
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null

            // Fonts
            _fontRasterisers.Dispose();

            // Images
            foreach (KeyValuePair<string, FastBitmap> image in _images)
            {
                image.Value.Dispose();
            }

            foreach (KeyValuePair<string, nint> texture in _imageTextures)
            {
                SDL_DestroyTexture((SDL_Texture*)texture.Value);
            }

            // Cleared as well as destroyed: a handle left in the dictionary is
            // one a later pass over it would free a second time.
            ClearTextTextures();

            if (_depthTexture != nint.Zero)
            {
                SDL_DestroyTexture((SDL_Texture*)_depthTexture);
            }

            if (_frameTarget != nint.Zero)
            {
                SDL_DestroyTexture((SDL_Texture*)_frameTarget);
            }
        }
    }

    private void SetRenderDrawColor(in FastColor color)
    {
        // Copied out of the 'in' parameter: a lambda cannot capture a by-reference one.
        FastColor drawColor = color;
        SDLGuard.Execute(() => SDL_SetRenderDrawColor(NativeRenderer, drawColor.R, drawColor.G, drawColor.B, drawColor.A));
    }

    private sealed class TextTextureEntry
    {
        public required nint Texture { get; init; }

        public required float Width { get; init; }

        public required float Height { get; init; }

        public bool UsedThisFrame { get; set; }
    }
}
