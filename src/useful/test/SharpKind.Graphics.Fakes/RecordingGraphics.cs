// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using SharpKind.Graphics.Rendering;

namespace SharpKind.Graphics.Fakes;

/// <summary>
/// An <see cref="IGraphics"/> that draws nothing and remembers what it was
/// asked to draw. The single test double for the interface: a test that only
/// needs somewhere for drawing to go ignores the lists, and one checking
/// rendered output asserts on them.
/// </summary>
/// <param name="screenWidth">The width reported by <see cref="Layout"/>.</param>
/// <param name="screenHeight">The height reported by <see cref="Layout"/>.</param>
public sealed class RecordingGraphics(float screenWidth = 0, float screenHeight = 0) : IGraphics
{
    // Recorded rather than acted on: nothing here rasterises, so which kind
    // of font would have been used is only worth reading back.
    public FontKind FontKind { get; set; }

    public IList<(Vector2[] Points, FastColor Colour)> FilledPolygons { get; } = [];

    public IList<(Vector2[] Points, FastColor[] CornerColours)> ShadedPolygons { get; } = [];

    public IList<(Vector2[] Points, Vector2[] TextureCoords, FastBitmap Texture)> TexturedPolygons { get; } = [];

    public IList<(string ImageType, Vector2 Position, Vector2 Size, Vector2 SourcePosition, Vector2 SourceSize)> ImageParts { get; } = [];

    public IList<(Vector2 Position, float Width, float Height, FastColor Colour)> FilledRectangles { get; } = [];

    public IList<(Vector2 Start, Vector2 End, FastColor Colour)> Lines { get; } = [];

    public IList<(Vector2 Centre, float Radius, FastColor Colour)> Circles { get; } = [];

    public IList<(Vector2 Position, float Width, float Height)> ClipRegions { get; } = [];

    public IDictionary<string, FastBitmap> Images { get; } = new Dictionary<string, FastBitmap>(StringComparer.Ordinal);

    public IList<(Vector2 Position, string Text, string FontType, FastColor Colour)> LeftTexts { get; } = [];

    public IList<(Vector2 Position, string Text, string FontType, FastColor Colour)> RightTexts { get; } = [];

    public IList<(float Y, string Text, string FontType, FastColor Colour)> CentredTexts { get; } = [];

    public int ClearCount { get; private set; }

    public int ClearDepthCount { get; private set; }

    public int FlushDepthCount { get; private set; }

    public int ScreenUpdateCount { get; private set; }

    public IList<string> SavedScreenPaths { get; } = [];

    // The screen size is not IGraphics' to report, so a test that hands this
    // fake to something laying out against the screen passes this alongside.
    public ScreenLayout Layout { get; } = new(screenWidth, screenHeight);

    public void Clear() => ClearCount++;

    public void ClearDepth() => ClearDepthCount++;

    public void FlushDepth() => FlushDepthCount++;

    public void DrawCircle(Vector2 centre, float radius, FastColor color)
        => Circles.Add((centre, radius, color));

    public void DrawCircleFilled(Vector2 centre, float radius, FastColor color)
    {
    }

    public void DrawImage(string imageType, Vector2 position)
    {
    }

    public void DrawImageCentre(string imageType, float y)
    {
    }

    public void DrawImagePart(string imageType, Vector2 position, Vector2 size, Vector2 sourcePosition, Vector2 sourceSize)
        => ImageParts.Add((imageType, position, size, sourcePosition, sourceSize));

    public Vector2 ImageSize(string imageType) => Vector2.Zero;

    // Named images the test put there itself, and the ones the game made for
    // itself while it drew - a recoloured sheet, say, which a test wants to
    // read back rather than watch go past.
    public FastBitmap Image(string imageType) => Images[imageType];

    public void SetImage(string imageType, FastBitmap image) => Images[imageType] = image;

    public void DrawLine(Vector2 lineStart, Vector2 lineEnd, FastColor color)
        => Lines.Add((lineStart, lineEnd, color));

    public void DrawLineDepth(Vector2 lineStart, Vector2 lineEnd, float depthStart, float depthEnd, FastColor color, int surfaceId)
    {
    }

    public void DrawPixel(Vector2 position, FastColor color)
    {
    }

    public void DrawPolygon(Vector2[] points, FastColor lineColor)
    {
    }

    public void DrawPolygonFilled(Vector2[] points, FastColor faceColor)
        => DrawPolygonFilled(points, faceColor, dither: null);

    public void DrawPolygonFilled(Vector2[] points, FastColor faceColor, IColourQuantiser? dither)
        => FilledPolygons.Add((points, faceColor));

    public void DrawPolygonFilledDepth(Vector2[] points, float[] depths, FastColor faceColor)
        => DrawPolygonFilledDepth(points, depths, faceColor, dither: null);

    public void DrawPolygonFilledDepth(Vector2[] points, float[] depths, FastColor faceColor, IColourQuantiser? dither)
        => FilledPolygons.Add((points, faceColor));

    // Recorded against the flat colour, so a test only caring that a face was drawn reads the same either way; corners land in ShadedPolygons.
    public void DrawPolygonFilledDepth(
        Vector2[] points,
        float[] depths,
        FastColor[] vertexColors,
        IColourQuantiser? quantiser)
    {
        ShadedPolygons.Add((points, vertexColors));
        FilledPolygons.Add((points, VertexColours.Flatten(vertexColors, quantiser)));
    }

    public void FillDepth(Vector2[] points, float[] depths, int surfaceId)
    {
    }

    public void DrawPolygonTextured(Vector2[] points, Vector2[] textureCoords, FastBitmap texture)
        => TexturedPolygons.Add((points, textureCoords, texture));

    public void DrawPolygonTexturedDepth(Vector2[] points, float[] depths, Vector2[] textureCoords, FastBitmap texture)
        => TexturedPolygons.Add((points, textureCoords, texture));

    public void DrawRectangle(Vector2 position, float width, float height, FastColor color)
    {
    }

    public void DrawRectangleCentre(float y, float width, float height, FastColor color)
    {
    }

    public void DrawRectangleFilled(Vector2 position, float width, float height, FastColor color)
        => FilledRectangles.Add((position, width, height, color));

    // A notional 8x8 cell per character, so a test can predict a measurement
    // without loading a font sheet.
    public Vector2 MeasureText(string text, string fontType)
        => new(string.IsNullOrWhiteSpace(text) ? 0 : text.Length * 8, 8);

    public void DrawTextCentre(float y, string text, string fontType, FastColor color)
        => CentredTexts.Add((y, text, fontType, color));

    public void DrawTextLeft(Vector2 position, string text, string fontType, FastColor color)
        => LeftTexts.Add((position, text, fontType, color));

    public void DrawTextRight(Vector2 position, string text, string fontType, FastColor color)
        => RightTexts.Add((position, text, fontType, color));

    public void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, FastColor color)
    {
    }

    public void DrawTriangleFilled(Vector2 a, Vector2 b, Vector2 c, FastColor color)
    {
    }

    public void ScreenUpdate() => ScreenUpdateCount++;

    public void SaveScreen(string path) => SavedScreenPaths.Add(path);

    public void SetClipRegion(Vector2 position, float width, float height)
        => ClipRegions.Add((position, width, height));
}
