// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using SharpKind.Graphics.Rendering;

namespace SharpKind.Graphics;

public interface IGraphics
{
    /// <summary>
    /// Gets or sets which kind of font text is drawn with. Setting it takes effect on the
    /// next thing drawn, so the commander sees the choice they just made
    /// rather than the one the game launched with. A kind the rendition does
    /// not have leaves its own sheets in use - reading the property back says
    /// which is actually being drawn with.
    /// </summary>
    public FontKind FontKind { get; set; }

    public void Clear();

    /// <summary>
    /// Reset the depth buffer, before drawing a depth-tested scene.
    /// </summary>
    public void ClearDepth();

    /// <summary>
    /// Put any depth-tested drawing on the screen now, under the clip region
    /// that is active at this moment.
    /// <para>
    /// A backend that rasterises depth-tested content aside and composites it
    /// later applies the clip region when it composites, not when it draws.
    /// Its content therefore obeys whichever clip region is active at that
    /// point. Call this before the clip region changes, so a layer's
    /// depth-tested content is trimmed to that layer's own rectangle.
    /// </para>
    /// <para>
    /// A backend that clips each draw as it rasterises has nothing to do here.
    /// </para>
    /// </summary>
    public void FlushDepth();

    public void DrawCircle(Vector2 centre, float radius, FastColor color);

    public void DrawCircleFilled(Vector2 centre, float radius, FastColor color);

    public void DrawImage(string imageType, Vector2 position);

    public void DrawImageCentre(string imageType, float y);

    /// <summary>
    /// Draw a sub-rectangle of a loaded image scaled to the destination
    /// size (sprite-sheet support). A negative sourceSize.X draws the part
    /// mirrored horizontally. Fully transparent pixels are skipped.
    /// </summary>
    public void DrawImagePart(string imageType, Vector2 position, Vector2 size, Vector2 sourcePosition, Vector2 sourceSize);

    /// <summary>
    /// The width and height of a loaded image, for callers that need to
    /// position it relative to its own extent (e.g. centred on a point).
    /// </summary>
    public Vector2 ImageSize(string imageType);

    /// <summary>
    /// A loaded image's pixels, for a caller that needs to make another image
    /// out of them - recolouring a sheet whose pixels stand for palette
    /// entries, say. The bitmap is the backend's own, so a caller that means
    /// to change it copies it first.
    /// </summary>
    public FastBitmap Image(string imageType);

    /// <summary>
    /// Give <paramref name="imageType"/> these pixels from now on, adding the
    /// name if the asset set did not declare it. For images the game makes for
    /// itself rather than loads, which it then draws by name like any other.
    /// <para>
    /// The backend takes the image on, and lets go of whatever was under that
    /// name - so a caller that still wants the old one keeps it under a name
    /// of its own rather than reading it back afterwards.
    /// </para>
    /// <para>
    /// It costs whatever the backend does to take a new image - an upload, for
    /// one that draws through a GPU - so it is for something that changes
    /// rarely, once a level rather than once a frame.
    /// </para>
    /// </summary>
    public void SetImage(string imageType, FastBitmap image);

    public void DrawLine(Vector2 lineStart, Vector2 lineEnd, FastColor color);

    /// <summary>
    /// Draw a line with per-pixel depth testing. The endpoint depths are
    /// camera-space distances as DrawPolygonFilledDepth's, interpolated
    /// along the line, so a line lying on hidden geometry is hidden by it
    /// rather than drawn over everything.
    /// <para>
    /// A non-zero <paramref name="surfaceId"/> also draws the pixel when the
    /// surface already occupying it is the one with that id, however the
    /// depths compare. That is how an edge escapes being hidden by the very
    /// surface it bounds, which it lies exactly on: the two are the same
    /// geometry, so the tie needs deciding by identity, not by a depth bias
    /// that no single value can get right for every viewing angle. Zero
    /// means the line belongs to no surface and is tested on depth alone.
    /// </para>
    /// </summary>
    public void DrawLineDepth(
        Vector2 lineStart,
        Vector2 lineEnd,
        float depthStart,
        float depthEnd,
        FastColor color,
        int surfaceId);

    public void DrawPixel(Vector2 position, FastColor color);

    public void DrawPolygon(Vector2[] points, FastColor lineColor);

    public void DrawPolygonFilled(Vector2[] points, FastColor faceColor);

    /// <summary>
    /// As DrawPolygonFilled, with a quantiser the fill asks per pixel - which
    /// only a dither needs. Null when the caller already resolved the colour.
    /// </summary>
    public void DrawPolygonFilled(Vector2[] points, FastColor faceColor, IColourQuantiser? dither);

    /// <summary>
    /// Fill a polygon with per-pixel depth testing. Depths pair with points
    /// and hold each point's positive camera-space distance (larger is
    /// further away); a pixel only draws when it is at least as near as
    /// what is already drawn there since the last ClearDepth.
    /// </summary>
    public void DrawPolygonFilledDepth(Vector2[] points, float[] depths, FastColor faceColor);

    /// <summary>
    /// As DrawPolygonFilledDepth, with a per-pixel quantiser - see the
    /// DrawPolygonFilled overload.
    /// </summary>
    public void DrawPolygonFilledDepth(Vector2[] points, float[] depths, FastColor faceColor, IColourQuantiser? dither);

    /// <summary>
    /// As DrawPolygonFilledDepth with a colour per point rather than one for
    /// the whole polygon (Gouraud shading), blended across the fill so a
    /// curve drawn as facets shades as a curve.
    /// </summary>
    /// <remarks>
    /// The quantiser is asked per pixel whether or not it dithers, since an
    /// interpolated colour differs at every pixel and cannot be resolved once
    /// by the caller. A backend with no per-pixel hook falls back to a flat
    /// fill - see <see cref="VertexColours.Flatten"/>.
    /// </remarks>
    public void DrawPolygonFilledDepth(
        Vector2[] points,
        float[] depths,
        FastColor[] vertexColors,
        IColourQuantiser? quantiser);

    /// <summary>
    /// Write a polygon's depth without drawing it, tagging every pixel it
    /// wins with <paramref name="surfaceId"/>. Primes the depth buffer so a
    /// later pass can be hidden by geometry that is never itself drawn —
    /// hidden-line removal, where the surfaces occlude but only the edges
    /// are visible — and lets each edge recognise its own surface. Ids are
    /// cleared by ClearDepth along with the depths.
    /// </summary>
    public void FillDepth(Vector2[] points, float[] depths, int surfaceId);

    /// <summary>
    /// Fill a polygon by sampling a texture. Texture coordinates pair with
    /// points and map [0,1] across the texture's width and height.
    /// </summary>
    public void DrawPolygonTextured(Vector2[] points, Vector2[] textureCoords, FastBitmap texture);

    /// <summary>
    /// As DrawPolygonTextured with per-pixel depth testing (depths as
    /// DrawPolygonFilledDepth) and perspective-correct sampling.
    /// </summary>
    public void DrawPolygonTexturedDepth(Vector2[] points, float[] depths, Vector2[] textureCoords, FastBitmap texture);

    public void DrawRectangle(Vector2 position, float width, float height, FastColor color);

    public void DrawRectangleCentre(float y, float width, float height, FastColor color);

    public void DrawRectangleFilled(Vector2 position, float width, float height, FastColor color);

    /// <summary>
    /// The size <paramref name="text"/> would occupy if drawn in
    /// <paramref name="fontType"/>. Lets a caller place text against something
    /// other than the screen - inside a control's own bounds, say - which the
    /// Centre overloads below cannot do, since they measure against the screen
    /// width. Whitespace-only text measures as the font's line height by zero
    /// width, matching what the Draw methods put on screen for it.
    /// </summary>
    public Vector2 MeasureText(string text, string fontType);

    public void DrawTextCentre(float y, string text, string fontType, FastColor color);

    public void DrawTextLeft(Vector2 position, string text, string fontType, FastColor color);

    public void DrawTextRight(Vector2 position, string text, string fontType, FastColor color);

    public void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, FastColor color);

    public void DrawTriangleFilled(Vector2 a, Vector2 b, Vector2 c, FastColor color);

    /// <summary>
    /// Blit the back buffer to the screen.
    /// </summary>
    public void ScreenUpdate();

    /// <summary>
    /// Write the current back buffer to a BMP file, independently of
    /// <see cref="ScreenUpdate"/> - for debug/test frame dumps (e.g. a
    /// live SDL app's F12 key) that need the exact native-resolution
    /// framebuffer rather than a desktop screenshot.
    /// </summary>
    public void SaveScreen(string path);

    public void SetClipRegion(Vector2 position, float width, float height);
}
