// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using StuntCarRacerSharpLib.Cars;
using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Rendering;

// Outside view only - in the cockpit view the camera sits inside the car. Mirrors the remake's
// SetCarWorldTransform, orienting CarMesh on four corners rotated out of the car's own frame,
// unlike the opponent's measured wheel positions.
public sealed class PlayerRenderer
{
    // The remake raises the car mesh by VCAR_HEIGHT/3 so wheels are fully visible, and the mesh's
    // wheels reach VCAR_HEIGHT/4 below its origin. Applying the same offset to the corners gives the same result.
    private const int RideHeight = (CarMesh.CarHeight / 3) - (CarMesh.CarHeight / 4);

    private readonly CarPhysics _car;
    private readonly CarMesh _carMesh;

    public PlayerRenderer(CarPhysics car, CarMesh carMesh)
    {
        ArgumentNullException.ThrowIfNull(car);
        ArgumentNullException.ThrowIfNull(carMesh);
        _car = car;
        _carMesh = carMesh;
    }

    // Appends the player's polygons (world track units) for depth-sorted
    // drawing with the track.
    public void AppendWorldPolygons(ICollection<WorldPolygon> polygons)
    {
        ArgumentNullException.ThrowIfNull(polygons);

        int x = _car.PlayerX >> Track.LogPrecision;
        int y = _car.PlayerY >> (Track.LogPrecision - 2);
        int z = _car.PlayerZ >> Track.LogPrecision;

        // The mesh's own footprint, not the physics car's: road clearance below is measured at the corners matching the visible wheels.
        const int halfWidth = CarMesh.CarWidth / 2;
        const int halfLength = CarMesh.CarLength / 2;

        Coord3D rearLeft = Corner(x, y, z, -halfWidth, -halfLength);
        Coord3D rearRight = Corner(x, y, z, halfWidth, -halfLength);
        Coord3D frontLeft = Corner(x, y, z, -halfWidth, halfLength);
        Coord3D frontRight = Corner(x, y, z, halfWidth, halfLength);

        // Lift the whole frame clear, not each corner, so the car stays rigid and keeps its roll
        // and pitch - the player's version of the max(road, actual) heights the opponent uses.
        int lift = Math.Max(
            Math.Max(_car.FrontLeftRoadY - frontLeft.Y, _car.FrontRightRoadY - frontRight.Y),
            Math.Max(_car.RearRoadY - rearLeft.Y, _car.RearRoadY - rearRight.Y));

        if (lift > 0)
        {
            rearLeft.Y += lift;
            rearRight.Y += lift;
            frontLeft.Y += lift;
            frontRight.Y += lift;
        }

        _carMesh.Append(polygons, rearLeft, rearRight, frontLeft, frontRight);
    }

    // A wheel corner in world track units: the car-local offset rotated by
    // the car's orientation, added to its centre.
    private Coord3D Corner(int x, int y, int z, int localX, int localZ)
    {
        Coord3D offset = _car.RotateToWorld(localX, RideHeight, localZ);

        return new(x + offset.X, y + offset.Y, z + offset.Z);
    }
}
