// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.Assets;
using StuntCarRacerSharpLib.Cars;
using StuntCarRacerSharpLib.Rendering;
using StuntCarRacerSharpLib.Tracks;
using Xunit;

namespace StuntCarRacerSharpLib.Tests.Rendering;

public class PlayerRendererTests
{
    // The same mesh the opponent gets, so the same quad count, placed at the
    // player's own position rather than on measured wheel positions.
    [Fact]
    public void AppendProducesTheCarMeshAtThePlayersPosition()
    {
        CarPhysics car = new(Track.Load(TrackId.LittleRamp));
        car.StartRace();
        PlayerRenderer renderer = new(car, new CarMesh(new ScrPalette(AssetLocator.Create())));
        List<WorldPolygon> polygons = [];

        renderer.AppendWorldPolygons(polygons);

        Assert.Equal(10, polygons.Count);

        int x = car.PlayerX >> Track.LogPrecision;
        int z = car.PlayerZ >> Track.LogPrecision;

        // every point sits within a car's length of the car's centre
        foreach (WorldPolygon polygon in polygons)
        {
            foreach (Coord3D point in polygon.Points)
            {
                Assert.InRange(point.X, x - CarPhysics.CarLength, x + CarPhysics.CarLength);
                Assert.InRange(point.Z, z - CarPhysics.CarLength, z + CarPhysics.CarLength);
            }
        }
    }

    // The bug this guards: the car's centre drops below the road when suspension bottoms out, drawing it half-buried.
    [Fact]
    public void CarStaysOnTopOfTheRoadWhileDriving()
    {
        CarPhysics car = new(Track.Load(TrackId.LittleRamp));
        car.StartRace();
        PlayerRenderer renderer = new(car, new CarMesh(new ScrPalette(AssetLocator.Create())));
        List<WorldPolygon> polygons = [];

        for (int frame = 0; frame < 400; frame++)
        {
            car.Update(CarInput.Accelerate);

            polygons.Clear();
            renderer.AppendWorldPolygons(polygons);

            int lowest = int.MaxValue;
            foreach (WorldPolygon polygon in polygons)
            {
                foreach (Coord3D point in polygon.Points)
                {
                    lowest = Math.Min(lowest, point.Y);
                }
            }

            int road = Math.Min(Math.Min(car.FrontLeftRoadY, car.FrontRightRoadY), car.RearRoadY);

            // a unit of slack for the integer rounding in the lift
            Assert.True(lowest >= road - 1, $"frame {frame}: car at {lowest}, road at {road}");
        }
    }

    // The mesh has to turn with the car, not just move with it: after a half
    // turn the corners swap sides.
    [Fact]
    public void MeshFollowsTheCarsOrientation()
    {
        CarPhysics car = new(Track.Load(TrackId.LittleRamp));
        car.StartRace();
        PlayerRenderer renderer = new(car, new CarMesh(new ScrPalette(AssetLocator.Create())));

        List<WorldPolygon> before = [];
        renderer.AppendWorldPolygons(before);

        car.TurnAround();

        List<WorldPolygon> after = [];
        renderer.AppendWorldPolygons(after);

        Assert.NotEqual(before[0].Points[0], after[0].Points[0]);
    }
}
