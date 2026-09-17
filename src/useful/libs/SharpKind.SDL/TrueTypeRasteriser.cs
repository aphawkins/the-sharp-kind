// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Diagnostics;
using System.Numerics;
using SDL;
using SharpKind.Assets;
using SharpKind.Graphics;
using static SDL.SDL3;
using static SDL.SDL3_ttf;

namespace SharpKind.SDL;

/// <summary>
/// Draws text from TrueType faces, through SDL_ttf. The only font kind whose
/// rasteriser needs SDL, which is why it lives here rather than beside the
/// other two in <c>SharpKind.Graphics</c> - that library draws every backend's
/// pixels and must not depend on any one of them.
/// </summary>
/// <remarks>
/// Glyphs are rendered solid, without antialiasing, so the result is two
/// colours like the bitmap and .fon kinds are. A face blended against the
/// background would put colours on screen that the rendition's palette never
/// declared, and the 8-bit renditions in particular have no shades to spare
/// for softened edges.
/// </remarks>
public sealed unsafe class TrueTypeRasteriser : IFontRasteriser, IDisposable
{
    private readonly Dictionary<string, nint> _fonts;
    private bool _isDisposed;

    private TrueTypeRasteriser(Dictionary<string, nint> fonts) => _fonts = fonts;

    /// <summary>
    /// Opens every TrueType face a rendition declares. SDL_ttf must already be
    /// initialised, which <see cref="SDLWindow"/> does when it is built.
    /// </summary>
    /// <param name="fonts">The faces to open, by font type.</param>
    /// <returns>A rasteriser over those faces.</returns>
    public static TrueTypeRasteriser Load(IDictionary<string, TrueTypeFontAsset> fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);

        return new(fonts.ToDictionary(x => x.Key, x => LoadFont(x.Value)));
    }

    public FastBitmap Rasterise(string text, string fontType, FastColor color)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (_isDisposed || string.IsNullOrWhiteSpace(text))
        {
            return new(1, 1);
        }

        SDL_Color colour = ToSDLColor(color);
        nint renderedPtr = SDLGuard.Execute(
            () => (nint)TTF_RenderText_Solid((TTF_Font*)_fonts[fontType], text, 0, colour));

        // Solid rendering gives an 8-bit paletted surface; converting resolves indices through the palette into FastBitmap's ARGB8888 layout.
        nint convertedPtr = SDLGuard.Execute(
            () => (nint)SDL_ConvertSurface((SDL_Surface*)renderedPtr, SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888));
        SDL_DestroySurface((SDL_Surface*)renderedPtr);

        SDL_Surface* converted = (SDL_Surface*)convertedPtr;
        Debug.Assert(converted->pitch == converted->w * 4, "Converted surface is not tightly packed.");

        FastBitmap bitmap = new(converted->w, converted->h);
        long byteCount = (long)converted->w * converted->h * 4;
        Buffer.MemoryCopy((void*)converted->pixels, (void*)bitmap.BitmapHandle, byteCount, byteCount);
        SDL_DestroySurface(converted);

        return bitmap;
    }

    // TTF measures the string itself, so this asks the face rather than
    // rendering: nothing is drawn for text that is only being measured.
    public Vector2 Measure(string text, string fontType)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (_isDisposed || string.IsNullOrWhiteSpace(text))
        {
            return new(0, _isDisposed ? 0 : TTF_GetFontHeight((TTF_Font*)_fonts[fontType]));
        }

        // Called directly, not through SDLGuard: the out parameters are pointers, which a lambda can't capture.
        int width;
        int height;
        if (!TTF_GetStringSize((TTF_Font*)_fonts[fontType], text, 0, &width, &height))
        {
            SDLHelper.Throw(nameof(TTF_GetStringSize));
        }

        return new(width, height);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        foreach (KeyValuePair<string, nint> font in _fonts)
        {
            TTF_CloseFont((TTF_Font*)font.Value);
        }

        _fonts.Clear();
        _isDisposed = true;
    }

    private static nint LoadFont(TrueTypeFontAsset font)
    {
        Debug.Assert(File.Exists(font.Path), $"Font file '{font.Path}' does not exist.");
        Debug.Assert(
            string.Equals(Path.GetExtension(font.Path), ".ttf", StringComparison.OrdinalIgnoreCase),
            $"Font file '{font.Path}' must be a TTF file.");
        Debug.Assert(font.PointSize > 0, $"Font '{font.Path}' must have a positive point size.");

        return SDLGuard.Execute(() => (nint)TTF_OpenFont(font.Path, font.PointSize));
    }

    // ARGB, matching FastColor's decoding - not RGBA.
    private static SDL_Color ToSDLColor(in FastColor color) => new()
    {
        r = color.R,
        g = color.G,
        b = color.B,
        a = color.A,
    };
}
