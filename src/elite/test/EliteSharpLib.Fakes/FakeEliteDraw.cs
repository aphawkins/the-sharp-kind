// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Graphics;
using EliteSharpLib.Ships;
using SharpKind;
using SharpKind.Assets.Palettes;
using SharpKind.Fakes;
using SharpKind.Fakes.Assets;
using SharpKind.Graphics;
using SharpKind.Graphics.Fakes;
using SharpKind.Graphics.Rendering;

namespace EliteSharpLib.Fakes;

internal class FakeEliteDraw : IEliteDraw
{
    public ViewLayout Layout { get; set; } = new(512, 512, new(512, 129), 2);

    public string Rendition { get; set; } = "16-bit";

    // Settable so a test can widen or narrow the field of view: the real
    // EliteDraw derives this from the screen height and the chosen angle.
    public float Focus { get; set; } = 512;

    // No jitter at all, rather than a seeded stream: the shimmer is
    // cosmetic, and a test asserting where a laser bolt lands wants the
    // geometry, not a random offset it would have to model to predict.
    // Settable so a test that does care can force a spread.
    public IRandomSource Jitter { get; set; } = new FakeRandomSource();

    public IGraphics Graphics { get; set; } = new RecordingGraphics();

    public IPaletteCollection Palette => new FakePalette();

    public ShipColours Ships { get; set; } = new(
        FakeColor.TestColor,
        FakeColor.TestColor,
        FakeColor.TestColor,
        FakeColor.TestColor,
        FakeColor.TestColor);

    public List<(Vector2[] Points, float[] Depths, FastColor FaceColor, float Z)> DrawnPolygons { get; } = [];

    public List<(Vector2[] Points, float[] Depths, FastColor[] CornerColors, float Z)> DrawnShadedPolygons { get; } = [];

    // Flat by default, so the existing tests see the flat path. A test of the
    // per-corner path turns this on.
    public bool ShadesPerVertex { get; set; }

    public void DrawObject(IObject obj)
    {
    }

    public void DrawPolygonFilled(Vector2[] points, float[] depths, FastColor faceColor, float z)
        => DrawnPolygons.Add((points, depths, faceColor, z));

    public void DrawPolygonFilled(Vector2[] points, float[] depths, FastColor[] cornerColors, float z)
        => DrawnShadedPolygons.Add((points, depths, cornerColors, z));

    // Unlit, so a test asserting on a drawn face's colour sees the model's own.
    public FastColor ShadeFace(FastColor faceColour, Vector3 cameraNormal, byte fullyLit) => faceColour;

    // Lambert against a light straight down -Z, so a test can predict a
    // corner's colour from the normal it was handed.
    public FastColor ShadeVertex(FastColor faceColour, Vector3 cameraNormal, byte fullyLit)
        => LambertShading.Shade(faceColour, cameraNormal, new(0, 0, -1), LambertShading.DefaultAmbient, fullyLit);

    public void RenderEnd()
    {
    }

    public void RenderStart()
    {
    }

    public void SetFullScreenClipRegion()
    {
    }
}
