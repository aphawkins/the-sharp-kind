// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharpLib.Audio;
using EliteSharpLib.Graphics;
using EliteSharpLib.Ships;
using SharpKind.Assets.Models;
using SharpKind.Audio;
using SharpKind.Maths;

namespace EliteSharpLib;

/// <summary>
/// The auto-pilot code.  Used for docking computers and for flying other ships to and from the space station.
/// </summary>
internal sealed class Pilot
{
    private readonly IEliteDraw _draw;
    private readonly AudioController _audio;
    private readonly PlayerShip _ship;
    private readonly GameState _gameState;
    private readonly Universe _universe;

    internal Pilot(IEliteDraw draw, AudioController audio, Universe universe, PlayerShip ship, GameState gameState)
    {
        _gameState = gameState;
        _draw = draw;
        _audio = audio;
        _universe = universe;
        _ship = ship;
    }

    internal bool IsAutoPilotOn { get; private set; }

    internal void AutoDock()
    {
        ShipBase ship = new(_draw)
        {
            Rotmat = VectorMaths.GetLeftHandedBasisMatrix,
            Location = Vector4.Zero,
            Acceleration = 0,
            Model = ModelReader.None,
            Velocity = _ship.Speed,
            Bravery = 0,
            RotZ = 0,
            RotX = 0,
        };

        Matrix4x4 rotmat = ship.Rotmat;
        rotmat.M11 = -1;
        rotmat.M33 = 1;
        ship.Rotmat = rotmat;

        AutoPilotShip(ship);

        ApplyAutoDockSpeed(ship);
        ApplyAutoDockPitch(ship);
        ApplyAutoDockRoll(ship);
    }

    /// <summary>
    /// Fly a ship to the planet or to the space station and dock it.
    /// </summary>
    internal void AutoPilotShip(IShip ship)
    {
        if (ship.Flags.HasFlag(ShipProperties.FlyToPlanet) || !_universe.IsStationPresent)
        {
            FlyToPlanet(ship);
            return;
        }

        Vector4 diff = ship.Location - _universe.StationOrSun!.Location;

        float dist = MathF.Sqrt((diff.X * diff.X) + (diff.Y * diff.Y) + (diff.Z * diff.Z));

        if (dist < 160)
        {
            ship.Flags |= ShipProperties.Remove;       // Ship has docked.
            return;
        }

        Vector4 vec = VectorMaths.UnitVector(diff);
        float dir = VectorMaths.VectorDotProduct(_universe.StationOrSun.Rotmat.GetRow(2), vec);

        if (dir < 0.9722)
        {
            FlyToStationFront(ship);
            return;
        }

        dir = VectorMaths.VectorDotProduct(ship.Rotmat.GetRow(2), vec);

        if (dir < -0.9444)
        {
            FlyToDockingBay(ship);
            return;
        }

        FlyToStation(ship);
    }

    internal void DisengageAutoPilot()
    {
        if (IsAutoPilotOn)
        {
            IsAutoPilotOn = false;
            _audio.StopMusic();
        }
    }

    internal void EngageAutoPilot()
    {
        if (!IsAutoPilotOn)
        {
            IsAutoPilotOn = true;
            _audio.PlayMusic(nameof(MusicType.BlueDanube), true);
        }
    }

    internal void Reset() => IsAutoPilotOn = false;

    /// <summary>
    /// Fly to a given point in space.
    /// </summary>
    private static void FlyToVector(IShip ship, Vector4 vec)
    {
        Vector4 nvec;
        float direction;
        float dir;
        int rat;
        float rat2;
        float cnt2;

        rat = 3;
        rat2 = 0.1666f;
        cnt2 = 0.8055f;

        nvec = VectorMaths.UnitVector(vec);
        direction = VectorMaths.VectorDotProduct(nvec, ship.Rotmat.GetRow(2));

        if (direction < -0.6666)
        {
            rat2 = 0;
        }

        dir = VectorMaths.VectorDotProduct(nvec, ship.Rotmat.GetRow(1));

        if (direction < -0.861)
        {
            ship.RotX = (dir < 0) ? 7 : -7;
            ship.RotZ = 0;
            return;
        }

        SetFlyToRotation(ship, nvec, dir, rat, rat2);

        if (direction <= -0.167)
        {
            ship.Acceleration = -1;
            return;
        }

        if (direction >= cnt2)
        {
            ship.Acceleration = 3;
        }
    }

    /// <summary>
    /// Roll and pitch the player's ship into line with the docking bay. Returns
    /// false while it is still lining up, having set the ship's rotation and
    /// slowed it to a crawl.
    /// </summary>
    private static bool IsLinedUpWithDockingBay(IShip ship, Vector4 vec)
    {
        ship.RotZ = 1;
        if (((vec.X >= 0) && (vec.Y >= 0)) ||
            ((vec.X < 0) && (vec.Y < 0)))
        {
            ship.RotZ = -ship.RotZ;
        }

        if (MathF.Abs(vec.X) >= 0.0625f)
        {
            ship.Acceleration = 0;
            ship.Velocity = 1;
            return false;
        }

        if (MathF.Abs(vec.Y) > 0.002436f)
        {
            ship.RotX = (vec.Y < 0) ? -1 : 1;
        }

        if (MathF.Abs(vec.Y) >= 0.0625f)
        {
            ship.Acceleration = 0;
            ship.Velocity = 1;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Point the ship at the target vector, pitching and then rolling towards it.
    /// </summary>
    private static void SetFlyToRotation(IShip ship, Vector4 nvec, float dir, int rat, float rat2)
    {
        ship.RotX = 0;

        if ((MathF.Abs(dir) * 2) >= rat2)
        {
            ship.RotX = (dir < 0) ? rat : -rat;
        }

        if (MathF.Abs(ship.RotZ) >= 16)
        {
            return;
        }

        dir = VectorMaths.VectorDotProduct(nvec, ship.Rotmat.GetRow(0));
        ship.RotZ = 0;

        if ((MathF.Abs(dir) * 2) >= rat2)
        {
            ship.RotZ = (dir < 0) ? rat : -rat;

            if (ship.RotX < 0)
            {
                ship.RotZ = -ship.RotZ;
            }
        }
    }

    private void ApplyAutoDockSpeed(ShipBase ship)
    {
        _ship.Speed = ship.Velocity > 22 ? 22 : ship.Velocity;

        if (ship.Acceleration > 0)
        {
            _ship.Speed += _gameState.Clock.Ticks;
            if (_ship.Speed > 22)
            {
                _ship.Speed = 22;
            }
        }

        if (ship.Acceleration < 0)
        {
            _ship.Speed -= _gameState.Clock.Ticks;
            if (_ship.Speed < 1)
            {
                _ship.Speed = 1;
            }
        }
    }

    private void ApplyAutoDockPitch(ShipBase ship)
    {
        if ((int)ship.RotX == 0)
        {
            _ship.Pitch = 0;
        }

        if (ship.RotX < 0)
        {
            _ship.IncreasePitch();

            if (ship.RotX < -1)
            {
                _ship.IncreasePitch();
            }
        }

        if (ship.RotX > 0)
        {
            _ship.DecreasePitch();

            if (ship.RotX > 1)
            {
                _ship.DecreasePitch();
            }
        }
    }

    private void ApplyAutoDockRoll(ShipBase ship)
    {
        if ((int)ship.RotZ == 127)
        {
            _ship.Roll = -14;
        }
        else if ((int)ship.RotZ == 0)
        {
            _ship.Roll = 0;
        }
        else if (ship.RotZ > 0)
        {
            _ship.IncreaseRoll();

            if (ship.RotZ > 1)
            {
                _ship.IncreaseRoll();
            }
        }
        else if (ship.RotZ < 0)
        {
            _ship.DecreaseRoll();

            if (ship.RotZ < -1)
            {
                _ship.DecreaseRoll();
            }
        }
    }

    /// <summary>
    /// Final stage of docking. Fly into the docking bay.
    /// </summary>
    private void FlyToDockingBay(IShip ship)
    {
        Vector4 diff = ship.Location - _universe.StationOrSun!.Location;
        Vector4 vec = VectorMaths.UnitVector(diff);
        ship.RotX = 0;

        if (ship.Id == ObjectIds.None && !IsLinedUpWithDockingBay(ship, vec))
        {
            // still edging into line with the slot, so hold station this tick
            return;
        }

        ship.RotZ = 0;

        float dir = VectorMaths.VectorDotProduct(ship.Rotmat.GetRow(0), _universe.StationOrSun.Rotmat.GetRow(1));

        if (MathF.Abs(dir) >= 0.9166f)
        {
            ship.Acceleration++;
            ship.RotZ = 127;
            return;
        }

        ship.Acceleration = 0;
        ship.RotZ = 0;
    }

    /// <summary>
    /// Fly towards the planet.
    /// </summary>
    private void FlyToPlanet(IShip ship)
    {
        if (_universe.Planet == null)
        {
            return;
        }

        Vector4 vec = _universe.Planet.Location - ship.Location;
        FlyToVector(ship, vec);
    }

    /// <summary>
    /// Fly towards the space station.
    /// </summary>
    private void FlyToStation(IShip ship)
    {
        Vector4 vec = _universe.StationOrSun!.Location - ship.Location;
        FlyToVector(ship, vec);
    }

    /// <summary>
    /// Fly to a point in front of the station docking bay. Done prior to the final stage of docking.
    /// </summary>
    private void FlyToStationFront(IShip ship)
    {
        Vector4 vec = _universe.StationOrSun!.Location - ship.Location;
        Vector4 stationNose = _universe.StationOrSun.Rotmat.GetRow(2);

        vec.X += stationNose.X * 768;
        vec.Y += stationNose.Y * 768;
        vec.Z += stationNose.Z * 768;

        FlyToVector(ship, vec);
    }
}
