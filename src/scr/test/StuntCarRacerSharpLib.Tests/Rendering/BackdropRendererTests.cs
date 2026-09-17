// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using System.Numerics;
using SharpKind;
using SharpKind.Assets;
using SharpKind.Fakes.Assets;
using SharpKind.Graphics;
using StuntCarRacerSharpLib.Rendering;
using StuntCarRacerSharpLib.Tracks;
using Xunit;

namespace StuntCarRacerSharpLib.Tests.Rendering;

public class BackdropRendererTests
{
    private const int Width = 640;

    // The app window is 16:10, not the original's 4:3 - the aspect ratio at
    // which the horizon and track projections used to disagree.
    private const int Height = 400;

    // The projection of the ground plane at 2 * 0x10000 track units (original uses z = 0x10000, viewpoint height halved).
    private const int GroundLineDistance = 2 * 0x00010000;

    // Regression test for the floating track bug: a mismatch makes the track appear to float above the ground when the camera pitches.
    [Theory]
    [InlineData(500, 8000)] // shallow pitch down
    [InlineData(2000, 8000)] // steep pitch down
    [InlineData(1000, 20000)] // near-level, high up
    public void GroundLineMatchesTrackProjectionWhenPitchedDown(int cameraHeight, int targetDistance)
    {
        ScrPalette palette = new(AssetLocator.Create());
        FastBitmap? frame = null;
        using SoftwareGraphics graphics = SoftwareGraphics.Create(Width, Height, b => frame = b, new FakeAssetLocator());
        BackdropRenderer backdrop = new(graphics, new(Width, Height), palette);
        SceneCamera camera = new();

        // camera above the world centre looking down at a ground point ahead
        const long centre = (long)Track.TrackCubes * Track.CubeSize / 2;
        camera.LookAt(
            centre,
            -((long)cameraHeight << Track.LogPrecision),
            centre,
            centre,
            0,
            centre + ((long)targetDistance << Track.LogPrecision));

        graphics.Clear();
        backdrop.DrawHorizonOnly(camera);
        graphics.ScreenUpdate();
        Assert.NotNull(frame);

        // where the track renderer would project the ground plane at the
        // ground line's distance, directly ahead
        Scene3D scene = new();
        scene.SetView(camera, Width, Height);
        Vector2 projected = scene.ProjectPoint(
            scene.TransformPoint(camera.X, 0, camera.Z + GroundLineDistance));

        float groundLine = FindSkyToGroundBoundary(frame, palette);
        Assert.True(
            Math.Abs(groundLine - projected.Y) <= 3,
            $"Ground line at y={groundLine} but the track projects the ground plane to y={projected.Y}.");
    }

    // Finds the sky/ground fill boundary: the topmost ground pixel in a
    // column whose pixel above is sky (skipping columns covered by scenery).
    private static float FindSkyToGroundBoundary(FastBitmap frame, ScrPalette palette)
    {
        FastColor sky = palette.Colour(Track.ScrBaseColour + 7);
        FastColor ground = palette.Colour(Track.ScrBaseColour + 13);

        for (int x = 0; x < Width; x++)
        {
            for (int y = 1; y < Height; y++)
            {
                if (frame.GetPixel(x, y) == ground)
                {
                    if (frame.GetPixel(x, y - 1) == sky)
                    {
                        return y;
                    }

                    break; // scenery above the ground here; try the next column
                }
            }
        }

        Assert.Fail("No sky/ground boundary found in the frame.");
        return 0;
    }
}
