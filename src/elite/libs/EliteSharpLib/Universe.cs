// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharpLib.Ships;
using SharpKind.Maths;

namespace EliteSharpLib;

internal sealed class Universe
{
    /// <summary>
    /// The most ships space holds at once, not counting the planet and the
    /// station or sun, which have slots of their own.
    /// </summary>
    internal const int MaxUniverseObjects = 20;
    private readonly IShipFactory _shipFactory;
    private readonly List<IObject> _objects = [];
    private readonly Dictionary<string, int> _shipCount = new(StringComparer.Ordinal);
    private readonly RNG _rng;

    internal Universe(IShipFactory shipFactory, RNG rng)
    {
        _shipFactory = shipFactory;
        _rng = rng;
        ClearUniverse();
    }

    // A station is any ship flagged Station; it shares the StationOrSun slot with the sun.
    internal bool IsStationPresent
        => StationOrSun?.Flags.HasFlag(ShipProperties.Station) == true;

    internal IObject? Planet { get; private set; }

    internal IObject? FirstShip => _objects.Count > 0 ? _objects[0] : StationOrSun;

    internal int PoliceCount => ShipCount(ObjectIds.Viper);

    internal IObject? StationOrSun { get; private set; }

    internal bool AddNewShip(IObject newObj, Vector4 location, Matrix4x4 rotmat, float rotx, float rotz)
    {
        if (_objects.Count >= MaxUniverseObjects)
        {
            return false;
        }

        newObj.Location = location;
        newObj.Rotmat = rotmat;
        if (newObj is IShip newShip)
        {
            newShip.RotX = rotx;
            newShip.RotZ = rotz;
            newShip.Energy = newShip.EnergyMax;
            newShip.Missiles = newShip.MissilesMax;
        }

        Count(newObj, 1);

        if (newObj.Flags.HasFlag(ShipProperties.Station) || newObj.Id == ObjectIds.Sun)
        {
            StationOrSun = newObj;
        }
        else if (newObj.Id == ObjectIds.Planet)
        {
            Planet = newObj;
        }
        else
        {
            _objects.Add(newObj);
        }

        return true;
    }

    internal bool AddNewShip(IShip ship)
    {
        Vector4 position = new()
        {
            X = 1000 + _rng.Random(8192),
            Y = 1000 + _rng.Random(8192),
            Z = 12000,
        };

        if (_rng.Random(256) > 127)
        {
            position.X = -position.X;
        }

        if (_rng.Random(256) > 127)
        {
            position.Y = -position.Y;
        }

        return AddNewShip(ship, position, VectorMaths.GetLeftHandedBasisMatrix, 0, 0);
    }

    internal void AddNewStation(int planetTechLevel, Vector4 position, Matrix4x4 rotmat)
    {
        IShip station = planetTechLevel >= 10 ? _shipFactory.CreateShip("DodecStation") : _shipFactory.CreateShip("Coriolis");
        AddNewShip(station, position, rotmat, 0, -127);
    }

    internal void ClearUniverse()
    {
        Planet = null;
        StationOrSun = null;
        _objects.Clear();

        _shipCount.Clear();
    }

    internal IEnumerable<IObject> GetAllObjects()
    {
        if (Planet != null)
        {
            yield return Planet;
        }

        if (StationOrSun != null)
        {
            yield return StationOrSun;
        }

        foreach (IObject obj in _objects.ToList())
        {
            yield return obj;
        }
    }

    internal void RemoveShip(IObject ship)
    {
        Count(ship, -1);

        if (ReferenceEquals(StationOrSun, ship))
        {
            StationOrSun = null;
        }
        else if (ReferenceEquals(Planet, ship))
        {
            Planet = null;
        }
        else
        {
            _objects.Remove(ship);
        }
    }

    internal int ShipCount(string shipId) => _shipCount.GetValueOrDefault(shipId);

    // The planet and sun are not ships and are not counted, matching the original.
    private void Count(IObject obj, int delta)
    {
        if (obj.Id is not ObjectIds.None and not ObjectIds.Planet and not ObjectIds.Sun)
        {
            _shipCount[obj.Id] = _shipCount.GetValueOrDefault(obj.Id) + delta;
        }
    }
}
