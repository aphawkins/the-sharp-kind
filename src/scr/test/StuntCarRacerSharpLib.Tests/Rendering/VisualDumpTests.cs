// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind;
using SharpKind.Assets;
using SharpKind.Graphics;
using StuntCarRacerSharpLib.Cars;
using StuntCarRacerSharpLib.Rendering;
using StuntCarRacerSharpLib.Tracks;
using Xunit;

namespace StuntCarRacerSharpLib.Tests.Rendering;

// Renders frames through the real software rasterizer and saves BMP files
// for visual inspection (found under StuntCarRacerFrames in the temp dir).
public class VisualDumpTests
{
    [Fact]
    public void DumpFrames()
    {
        string outDir = Path.Combine(Path.GetTempPath(), "StuntCarRacerFrames");
        Directory.CreateDirectory(outDir);

        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();
        OpponentPhysics opponent = new(track, car, new RandomSource(new Random(3)));
        opponent.StartRace();

        ScrPalette palette = new(AssetLocator.Create());
        FastBitmap? lastFrame = null;
        using SoftwareGraphics graphics = SoftwareGraphics.Create(640, 400, b => lastFrame = b, AssetLocator.Create());
        ScreenLayout screen = new(640, 400);
        TrackRenderer renderer = new(track, graphics, screen, palette, new(palette));
        BackdropRenderer backdrop = new(graphics, screen, palette);
        OpponentRenderer opponentRenderer = new(opponent, new(palette), palette);
        SceneCamera camera = new();
        List<WorldPolygon> worldPolygons = [];

        void Step(CarInput input)
        {
            car.Update(input);
            opponent.Update();
        }

        void RenderAndSave(string name, bool followCar = true)
        {
            if (followCar)
            {
                camera.FollowCar(car);
            }

            graphics.Clear();
            backdrop.Draw(camera);
            worldPolygons.Clear();
            opponentRenderer.AppendWorldPolygons(worldPolygons);
            renderer.Draw(camera, worldPolygons, car.CurrentPiece, car.CurrentSegment);
            graphics.ScreenUpdate();
            Assert.NotNull(lastFrame);
            BitmapWriter.Write(lastFrame, Path.Combine(outDir, name));
        }

        // frame at race start (car in the air above the start piece)
        RenderAndSave("frame_start.bmp");

        // after dropping onto the road (the opponent should be just ahead)
        for (int i = 0; i < 12; i++)
        {
            Step(CarInput.None);
        }

        RenderAndSave("frame_landed.bmp");

        // a moment later the opponent has pulled ahead into view
        for (int i = 0; i < 10; i++)
        {
            Step(CarInput.None);
        }

        RenderAndSave("frame_opponent.bmp");

        // after accelerating down the straight
        for (int i = 0; i < 60; i++)
        {
            Step(CarInput.AccelBoost);
        }

        RenderAndSave("frame_driving.bmp");

        // approaching / on the ramp
        for (int i = 0; i < 60; i++)
        {
            Step(CarInput.AccelBoost);
        }

        RenderAndSave("frame_ramp.bmp");

        // the driving frame with the in-game cockpit overlay: wheels,
        // engine, a part-grown damage crack, speed bar and read-outs
        HudRenderer hud = new(graphics, screen);
        camera.FollowCar(car);
        graphics.Clear();
        backdrop.Draw(camera);
        worldPolygons.Clear();
        opponentRenderer.AppendWorldPolygons(worldPolygons);
        renderer.Draw(camera, worldPolygons, car.CurrentPiece, car.CurrentSegment);
        hud.Draw(new CockpitState(
            car.LeftWheelFrame,
            car.RightWheelFrame,
            car.LeftWheelBounce,
            car.RightWheelBounce,
            car.BoostActivated != 0,
            120,
            0,
            car.DisplaySpeed,
            2,
            15,
            -340,
            car.OnChains,
            car.WaitingToReleaseChains,
            car.ZAngle,
            car.CurrentLapTicks,
            car.BestLapTicks));
        graphics.ScreenUpdate();
        Assert.NotNull(lastFrame);
        BitmapWriter.Write(lastFrame, Path.Combine(outDir, "frame_hud.bmp"));

        // The track-preview viewpoint: the camera angle that exposed the floating-track bug.
        const long centre = (long)Track.TrackCubes * Track.CubeSize / 2;
        camera.LookAt(
            centre + ((car.X - centre) / 2),
            car.Y - ((long)Track.CubeSize / 2),
            centre + ((car.Z - centre) / 2),
            car.X,
            car.Y,
            car.Z);
        RenderAndSave("frame_preview.bmp", followCar: false);

        // the track menu's authentic menu.png overlay (transparent centre
        // panel over the orbiting world view)
        graphics.Clear();
        backdrop.Draw(camera);
        renderer.Draw(camera);
        graphics.DrawImagePart(
            "Menu",
            new(0, 0),
            screen.ScreenSize,
            new(0, 0),
            new(320, 200));
        graphics.ScreenUpdate();
        Assert.NotNull(lastFrame);
        BitmapWriter.Write(lastFrame, Path.Combine(outDir, "frame_menu.bmp"));
    }
}
