// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Diagnostics;
using System.Numerics;
using EliteSharp.Abstractions.Renditions;
using EliteSharp.Abstractions.Ships;
using EliteSharpLib.Audio;
using EliteSharpLib.Graphics;
using EliteSharpLib.Missions;
using EliteSharpLib.Ships;
using EliteSharpLib.Suns;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpKind.Audio;
using SharpKind.Maths;

namespace EliteSharpLib.Conflict;

internal sealed partial class Combat
{
    private readonly AudioController _audio;
    private readonly GameState _gameState;
    private readonly Pilot _pilot;
    private readonly PlayerShip _ship;
    private readonly Trade _trade;
    private readonly Universe _universe;
    private readonly IEliteDraw _draw;
    private readonly IRendition _rendition;
    private readonly IShipFactory _shipFactory;
    private readonly MissionRunner _missions;
    private readonly RNG _rng;
    private readonly TacticsSchedule _tactics = new();
    private readonly ILogger<Combat> _logger;
    private bool _isEcmOurs;
    private float _laserCounter;
    private int _laserStrength;
    private LaserType _laserType;

    internal Combat(
        GameState gameState,
        AudioController audio,
        PlayerShip ship,
        Trade trade,
        Pilot pilot,
        Universe universe,
        IEliteDraw draw,
        IRendition rendition,
        IShipFactory shipFactory,
        RNG rng,
        MissionRunner missions,
        ILogger<Combat>? logger = null)
    {
        _gameState = gameState;
        _audio = audio;
        _ship = ship;
        _trade = trade;
        _pilot = pilot;
        _universe = universe;
        _draw = draw;
        _rendition = rendition;
        _shipFactory = shipFactory;
        _rng = rng;
        _missions = missions;
        _logger = logger ?? NullLogger<Combat>.Instance;
    }

    internal bool InBattle { get; set; }

    internal bool IsMissileArmed { get; private set; }

    internal IObject? MissileTarget { get; private set; }

    internal void ActivateECM(bool ours)
    {
        if (_ship.EcmActive == 0)
        {
            _ship.EcmActive = 32;
            _isEcmOurs = ours;
            _audio.PlayEffect(nameof(SoundEffect.Ecm));
        }
    }

    internal void ArmMissile()
    {
        if (_ship.MissileCount != 0)
        {
            IsMissileArmed = true;
        }
    }

    internal void CheckTarget(IShip obj, IObject flip)
    {
        if (!IsInTarget(obj, flip.Location))
        {
            return;
        }

        TryLockMissile(obj);

        if (_laserStrength <= 0)
        {
            return;
        }

        _audio.PlayEffect(nameof(SoundEffect.HitEnemy));
        ApplyLaserDamage(obj);

        if (obj.Energy <= 0)
        {
            DestroyTarget(obj);
        }

        MakeAngry(obj);
    }

    internal void CreateThargoid()
    {
        IShip thargoid = _shipFactory.CreateShip("Thargoid");
        if (_universe.AddNewShip(thargoid))
        {
            thargoid.Flags = ShipProperties.Angry | ShipProperties.HasECM;
            thargoid.Bravery = 113;

            if (_rng.Random(256) > 64
                && !LaunchEnemy(thargoid, _shipFactory.CreateShip("Tharglet"), ShipProperties.Angry | ShipProperties.HasECM, 96))
            {
                LogMessages.FailedToCreateShip(_logger, "Tharglet");
            }
        }
        else
        {
            LogMessages.FailedToCreateShip(_logger, "Thargoid");
        }
    }

    internal void ExplodeObject(IObject obj)
    {
        _gameState.Cmdr.Score++;

        if ((_gameState.Cmdr.Score & 255) == 0)
        {
            _gameState.InfoMessage("Right On Commander!");
        }

        _audio.PlayEffect(nameof(SoundEffect.Explode));
        obj.Flags |= ShipProperties.Dead;

        _missions.ShipDestroyed(obj.Id);
    }

    internal bool FireLaser()
    {
        if (_gameState.IsDocked ||
            _gameState.DrawLasers ||
            _laserCounter > 0 ||
            _gameState.LaserTemp >= GameState.LaserTempOverheated)
        {
            return false;
        }

        _laserStrength = _gameState.CurrentScreen switch
        {
            Screen.FrontView => _ship.LaserFront.Strength,
            Screen.RearView => _ship.LaserRear.Strength,
            Screen.RightView => _ship.LaserRight.Strength,
            Screen.LeftView => _ship.LaserLeft.Strength,
            _ => 0,
        };

        if (_laserStrength == 0)
        {
            return false;
        }

        _laserType = _gameState.CurrentScreen switch
        {
            Screen.FrontView => _ship.LaserFront.Type,
            Screen.RearView => _ship.LaserRear.Type,
            Screen.RightView => _ship.LaserRight.Type,
            Screen.LeftView => _ship.LaserLeft.Type,
            _ => LaserType.None,
        };

        _laserCounter = _laserStrength > 127 ? 0 : _laserStrength & 250;
        _laserStrength &= 127;

        _audio.PlayEffect(nameof(SoundEffect.Pulse));
        _gameState.LaserTemp += GameState.LaserTempPerShot;
        if (_ship.Energy > PlayerShip.EnergyStep)
        {
            _ship.DecreaseEnergy(-1);
        }

        return true;
    }

    internal void FireMissile()
    {
        if (MissileTarget == null || !IsMissileArmed)
        {
            return;
        }

        Matrix4x4 rotmat = VectorMaths.GetRightHandedBasisMatrix;
        rotmat.M11 = -1;

        IShip missile = _shipFactory.CreateShip("Missile");
        if (!_universe.AddNewShip(missile, new(0, -28, 14, 0), rotmat, 0, 0))
        {
            _gameState.InfoMessage("Missile Jammed");
            return;
        }

        missile.Velocity = _ship.Speed * 2;
        missile.Flags = ShipProperties.Angry;
        missile.Target = MissileTarget;

        if (MissileTarget.Traits.HasFlag(ShipTraits.Crewed))
        {
            MissileTarget.Flags |= ShipProperties.Angry;
        }

        _ship.MissileCount--;
        IsMissileArmed = false;
        MissileTarget = null;

        _audio.PlayEffect(nameof(SoundEffect.Missile));
    }

    internal void RandomEncounter()
    {
        if (_universe.IsStationPresent)
        {
            return;
        }

        if (_rng.Random(256) == 136)
        {
            if (((int)_universe.Planet!.Location.Z & 0x3e) != 0)
            {
                CreateThargoid();
            }
            else
            {
                CreateCougar();
            }

            return;
        }

        if (_rng.Random(8) == 0)
        {
            CreateTrader();
            return;
        }

        CheckForAsteroids();

        CheckForPolice();

        if (_universe.PoliceCount != 0)
        {
            return;
        }

        if (InBattle)
        {
            return;
        }

        // The game rolls the odds, so a mission needs no randomness of its own.
        if (_missions.Ambush() is { } ambush && _rng.Random(256) < ambush.ChanceInTwoFiftySix)
        {
            CreateMissionShip(ambush.ShipName);
        }

        CheckForOthers();
    }

    internal void RemoveShip(IObject obj)
    {
        if (obj.Id == ObjectIds.None)
        {
            return;
        }

        CheckMissiles(obj);

        if (obj.Flags.HasFlag(ShipProperties.Station))
        {
            Vector4 position = obj.Location;
            position.Y = (int)position.Y & 0xFFFF;
            position.Y = (int)position.Y | 0x60000;
            IObject sun = SunFactory.Create(_gameState, _draw, _rendition, _rng);
            _universe.AddNewShip(sun, position, VectorMaths.GetLeftHandedBasisMatrix, 0, 0);
        }

        _universe.RemoveShip(obj);
    }

    internal void Reset()
    {
        InBattle = false;
        _tactics.Reset();
    }

    internal void ResetWeapons()
    {
        _gameState.LaserTemp = GameState.LaserTempMin;
        _laserCounter = 0;
        _laserStrength = 0;
        _laserType = LaserType.None;
        _ship.EcmActive = 0;
        MissileTarget = null;
    }

    internal void ScoopItem(IShip obj)
    {
        if (obj.Flags.HasFlag(ShipProperties.Dead))
        {
            return;
        }

        if (obj.Id == ObjectIds.Missile)
        {
            return;
        }

        if (!_ship.HasFuelScoop || obj.Location.Y >= 0 || _trade.TotalCargoTonnage() == _ship.CargoCapacity)
        {
            ExplodeObject(obj);
            if (!_ship.HasFuelScoop || obj.Location.Y >= 0)
            {
                _ship.DamageShip(128 + (obj.Energy / 2), obj.Location.Z > 0);
            }

            return;
        }

        if (obj.Id == ObjectIds.CargoCannister)
        {
            // One draw over the same width as the original's 1..9 range, so the dice land the same.
            ScoopCargo(_trade.DroppedByShips[_rng.Random(_trade.DroppedByShips.Count)], obj);
            return;
        }

        if (obj.ScoopedType != null)
        {
            ScoopCargo(_trade[obj.ScoopedType], obj);
            return;
        }

        ExplodeObject(obj);
        _ship.DamageShip(obj.Energy / 2f, obj.Location.Z > 0);
    }

    internal void Tactics(IShip ship, int un)
    {
        if (ship.Id == ObjectIds.Missile)
        {
            if (ship.Flags.HasFlag(ShipProperties.Angry))
            {
                MissileTactics(ship);
            }

            return;
        }

        if (!_tactics.ShouldThink(un, _gameState.MCount))
        {
            return;
        }

        if (ship.Flags.HasFlag(ShipProperties.Station))
        {
            StationTactics(ship);
            return;
        }

        if (ship.Id == ObjectIds.RockHermit)
        {
            HermitTactics(ship);
            return;
        }

        if (ship.Energy < ship.EnergyMax)
        {
            ship.Energy++;
        }

        if (ship.Id == ObjectIds.Tharglet && _universe.ShipCount(ObjectIds.Thargoid) == 0)
        {
            ship.Flags = 0;
            ship.Velocity /= 2;
            return;
        }

        ShipTactics(ship);
    }

    // Counted in ticks, not updates, so a burst's length and cost don't shrink at 60fps.
    internal void TimeECM()
    {
        if (_ship.EcmActive <= 0)
        {
            return;
        }

        float ticks = MathF.Min(_gameState.Clock.Ticks, _ship.EcmActive);
        _ship.EcmActive -= ticks;

        if (_isEcmOurs)
        {
            _ship.DecreaseEnergy(-ticks);
        }
    }

    internal void UnarmMissile()
    {
        MissileTarget = null;
        IsMissileArmed = false;
        _audio.PlayEffect(nameof(SoundEffect.Boop));
    }

    private static bool IsInTarget(IShip ship, Vector4 position)
        => position.Z >= 0 && (position.X * position.X) + (position.Y * position.Y) <= ship.Size;

    private static void MakeAngry(IShip ship)
    {
        if (ship.Flags.HasFlag(ShipProperties.Inactive))
        {
            return;
        }

        if (ship.Flags.HasFlag(ShipProperties.Station))
        {
            ship.Flags |= ShipProperties.Angry;
            return;
        }

        if (ship.Traits.HasFlag(ShipTraits.Crewed))
        {
            ship.RotX = 4;
            ship.Acceleration = 2;
            ship.Flags |= ShipProperties.Angry;
        }
    }

    private static void TrackObject(IShip ship, float direction, Vector4 nvec)
    {
        const int rat = 3;
        const float rat2 = 0.111f;
        float dir = VectorMaths.VectorDotProduct(nvec, ship.Rotmat.GetRow(1));

        if (direction < -0.861)
        {
            ship.RotX = dir < 0 ? 7 : -7;
            ship.RotZ = 0;
            return;
        }

        ship.RotX = 0;

        if (MathF.Abs(dir) * 2 >= rat2)
        {
            ship.RotX = dir < 0 ? rat : -rat;
        }

        if (MathF.Abs(ship.RotZ) < 16)
        {
            dir = VectorMaths.VectorDotProduct(nvec, ship.Rotmat.GetRow(0));

            ship.RotZ = 0;

            if (MathF.Abs(dir) * 2 > rat2)
            {
                ship.RotZ = dir < 0 ? rat : -rat;

                if (ship.RotX < 0)
                {
                    ship.RotZ = -ship.RotZ;
                }
            }
        }
    }

    // Ordinary ship tactics: excludes stations, hermits and missiles, handled elsewhere.
    private void ShipTactics(IShip ship)
    {
        ShipProperties flags = ship.Flags;

        if (flags.HasFlag(ShipProperties.Slow) &&
            _rng.Random(256) > 50)
        {
            return;
        }

        if ((flags.HasFlag(ShipProperties.Police) && _gameState.Cmdr.LegalStatus >= 64) ||
            (flags.HasFlag(ShipProperties.BountyHunter) && _gameState.Cmdr.LegalStatus >= 40))
        {
            flags |= ShipProperties.Angry;
            ship.Flags = flags;
        }

        if (!flags.HasFlag(ShipProperties.Angry))
        {
            if (flags.HasFlag(ShipProperties.FlyToPlanet) || flags.HasFlag(ShipProperties.FlyToStation))
            {
                _pilot.AutoPilotShip(ship);
            }

            return;
        }

        AttackTactics(ship, flags);
    }

    private void TryLockMissile(IShip obj)
    {
        if (MissileTarget == null && IsMissileArmed && !obj.Traits.HasFlag(ShipTraits.Stellar))
        {
            MissileTarget = obj;
            _gameState.InfoMessage("Target Locked");
            _audio.PlayEffect(nameof(SoundEffect.Beep));
        }
    }

    private void ApplyLaserDamage(IShip obj)
    {
        if (obj.Flags.HasFlag(ShipProperties.Station))
        {
            return;
        }

        if (obj.Id is ObjectIds.Constrictor or ObjectIds.Cougar)
        {
            // only a military laser can hurt these two
            if (_laserType == LaserType.Military)
            {
                obj.Energy -= _laserStrength / 4;
            }
        }
        else
        {
            obj.Energy -= _laserStrength;
        }
    }

    // Same three steps whether a canister was opened or a ship left one thing behind.
    private void ScoopCargo(StockItem stock, IShip obj)
    {
        stock.CurrentCargo++;
        _gameState.InfoMessage(stock.Good.Name);
        RemoveShip(obj);
    }

    private void DestroyTarget(IShip obj)
    {
        ExplodeObject(obj);

        if (obj.Id == ObjectIds.Asteroid && _laserType == LaserType.Mining)
        {
            LaunchLoot(obj, ObjectIds.RockSplinter);
        }

        LaunchLoot(obj, ObjectIds.Alloy);
        LaunchLoot(obj, ObjectIds.CargoCannister);
    }

    private void StationTactics(IShip ship)
    {
        if (!ship.Flags.HasFlag(ShipProperties.Angry))
        {
            LaunchShuttle();
            return;
        }

        if (_rng.Random(256) < 240)
        {
            return;
        }

        if (_universe.PoliceCount >= 4)
        {
            return;
        }

        if (!LaunchEnemy(ship, _shipFactory.CreateShip("Viper"), ShipProperties.Angry | ShipProperties.HasECM, 113))
        {
            LogMessages.FailedToCreateShip(_logger, "Police");
        }
    }

    private void HermitTactics(IShip ship)
    {
        if (_rng.Random(256) <= 200)
        {
            return;
        }

        IShip pirate = _shipFactory.CreatePirate();
        if (!LaunchEnemy(ship, pirate, ShipProperties.Angry | ShipProperties.HasECM, 113))
        {
            LogMessages.FailedToCreateShip(_logger, "Hermit Pirate");
        }

        ship.Flags |= ShipProperties.Inactive;
    }

    private void AttackTactics(IShip ship, ShipProperties flags)
    {
        if (_universe.IsStationPresent && !flags.HasFlag(ShipProperties.Bold))
        {
            ship.Bravery = 0;
        }

        if (ship.Id == ObjectIds.Anaconda && _rng.Random(256) > 200)
        {
            IShip anacondaHunter = _rng.Random(256) > 100 ? _shipFactory.CreateShip("Worm") : _shipFactory.CreateShip("Sidewinder");
            if (!LaunchEnemy(ship, anacondaHunter, ShipProperties.Angry | ShipProperties.HasECM, 113))
            {
                LogMessages.FailedToCreateShip(_logger, "Anaconda Hunter");
            }

            return;
        }

        if (_rng.Random(256) >= 250)
        {
            ship.RotZ = _rng.Random(256) | 0x68;
            if (ship.RotZ > 127)
            {
                ship.RotZ = -((int)ship.RotZ & 127);
            }
        }

        if (ship.Energy < ship.EnergyMax / 2 && LowEnergyTactics(ship))
        {
            return;
        }

        Vector4 nvec = VectorMaths.UnitVector(ship.Location);
        float direction = VectorMaths.VectorDotProduct(nvec, ship.Rotmat.GetRow(2));

        // Original CPX #160 (X <= -32 of magnitude 36): also gates Firing.
        if (ship.Location.Length() < 8192 &&
            direction <= -0.889 &&
            ship.LaserStrength != 0)
        {
            FiringTactics(ship, direction, nvec);
            return;
        }

        ManoeuvreTactics(ship, direction, nvec);
    }

    /// <summary>
    /// Low energy responses - escape capsule or missile launch.
    /// </summary>
    /// <returns><c>true</c> if the ship has acted and no further tactics apply.</returns>
    private bool LowEnergyTactics(IShip ship)
    {
        if (ship.Energy < ship.EnergyMax / 8 && _rng.Random(256) > 230 && ship.Id != ObjectIds.Thargoid)
        {
            ship.Flags &= ~ShipProperties.Angry;
            ship.Flags |= ShipProperties.Inactive;
            if (!LaunchEnemy(ship, _shipFactory.CreateShip("EscapeCapsule"), 0, 126))
            {
                LogMessages.FailedToCreateShip(_logger, "Escape Capsule");
            }

            return true;
        }

        if (ship.Missiles != 0 && _ship.EcmActive == 0 && ship.Missiles >= _rng.Random(32))
        {
            ship.Missiles--;
            if (ship.Id == ObjectIds.Thargoid)
            {
                if (!LaunchEnemy(ship, _shipFactory.CreateShip("Tharglet"), ShipProperties.Angry, ship.Bravery))
                {
                    LogMessages.FailedToCreateShip(_logger, "Tharglet");
                }
            }
            else
            {
                if (!LaunchEnemy(ship, _shipFactory.CreateShip("Missile"), ShipProperties.Angry, 126))
                {
                    LogMessages.FailedToCreateShip(_logger, "Missile");
                }

                _gameState.InfoMessage("INCOMING MISSILE");
            }

            return true;
        }

        return false;
    }

    private void FiringTactics(IShip ship, float direction, Vector4 nvec)
    {
        ship.Flags |= ShipProperties.Firing | ShipProperties.Hostile;

        if (direction <= -0.972)
        {
            _ship.DamageShip(ship.LaserStrength, ship.Location.Z >= 0.0);
            ship.Acceleration--;
            if ((ship.Location.Z >= 0.0 && _ship.ShieldFront <= PlayerShip.ShieldMin) ||
                (ship.Location.Z < 0.0 && _ship.ShieldRear <= PlayerShip.ShieldMin))
            {
                _audio.PlayEffect(nameof(SoundEffect.IncomingFire2));
            }
            else
            {
                _audio.PlayEffect(nameof(SoundEffect.IncomingFire1));
            }
        }
        else
        {
            nvec.X = -nvec.X;
            nvec.Y = -nvec.Y;
            nvec.Z = -nvec.Z;
            direction = -direction;
            TrackObject(ship, direction, nvec);
        }

        ////      if ((fabs(ship.location.z) < 768) && (ship.bravery <= ((random.rand255() & 127) | 64)))
        if (MathF.Abs(ship.Location.Z) < 768f)
        {
            ship.RotX = _rng.Random(136);
            if (ship.RotX > 127)
            {
                ship.RotX = -((int)ship.RotX & 127);
            }

            ship.Acceleration = 3;
            return;
        }

        ship.Acceleration = ship.Location.Length() < 8192 ? -1 : 3;
    }

    private void ManoeuvreTactics(IShip ship, float direction, Vector4 nvec)
    {
        const float cnt2 = 0.223f;
        int attacking = 0;

        if ((MathF.Abs(ship.Location.Z) >= 768 ||
            MathF.Abs(ship.Location.X) >= 512 ||
            MathF.Abs(ship.Location.Y) >= 512) &&
            ship.Bravery > _rng.Random(128))
        {
            attacking = 1;
            nvec.X = -nvec.X;
            nvec.Y = -nvec.Y;
            nvec.Z = -nvec.Z;
            direction = -direction;
        }

        TrackObject(ship, direction, nvec);

        if (attacking == 1 && ship.Location.Length() < 2048)
        {
            if (direction >= cnt2)
            {
                ship.Acceleration = -1;
                return;
            }

            if (ship.Velocity < 6)
            {
                ship.Acceleration = 3;
            }
            else if (_rng.Random(256) >= 200)
            {
                ship.Acceleration = -1;
            }

            return;
        }

        if (direction <= -0.167)
        {
            ship.Acceleration = -1;
            return;
        }

        if (direction >= cnt2)
        {
            ship.Acceleration = 3;
            return;
        }

        if (ship.Velocity < 6)
        {
            ship.Acceleration = 3;
        }
        else if (_rng.Random(256) >= 200)
        {
            ship.Acceleration = -1;
        }
    }

    /// <summary>
    /// Check for a random asteroid encounter.
    /// </summary>
    private void CheckForAsteroids()
    {
        if (_rng.Random(256) >= 35 || _universe.ShipCount(ObjectIds.Asteroid) >= 3)
        {
            return;
        }

        IShip asteroid = _shipFactory.CreateAsteroid();
        if (_universe.AddNewShip(asteroid))
        {
            //// space.universe[newship].velocity = (random.rand255() & 31) | 16;
            asteroid.Velocity = 8;
            asteroid.RotZ = _rng.TrueOrFalse() ? -127 : 127;
            asteroid.RotX = 16;
        }
        else
        {
            LogMessages.FailedToCreateShip(_logger, "Asteroid");
        }
    }

    private void CheckForOthers()
    {
        int gov = _gameState.CurrentPlanetData.Government;
        int rnd = _rng.Random(256);

        if (gov != 0 && (rnd >= 90 || (rnd & 7) < gov))
        {
            return;
        }

        if (_rng.Random(256) < 100)
        {
            CreateLoneWolf();
            return;
        }

        Vector4 position = new()
        {
            Z = 12000,
            X = 1000 + _rng.Random(8192),
            Y = 1000 + _rng.Random(8192),
        };

        if (_rng.TrueOrFalse())
        {
            position.X = -position.X;
        }

        if (_rng.TrueOrFalse())
        {
            position.Y = -position.Y;
        }

        rnd = _rng.Random(4);

        for (int i = 0; i <= rnd; i++)
        {
            IShip packHunter = _shipFactory.CreatePackHunter();
            if (_universe.AddNewShip(packHunter, position, VectorMaths.GetLeftHandedBasisMatrix, 0, 0))
            {
                packHunter.Flags = ShipProperties.Angry;
                if (_rng.Random(256) > 245)
                {
                    packHunter.Flags |= ShipProperties.HasECM;
                }

                packHunter.Bravery = ((_rng.Random(256) * 2) | 64) & 127;
                InBattle = true;
            }
            else
            {
                LogMessages.FailedToCreateShip(_logger, "Pack Hunter");
            }
        }
    }

    private void CheckForPolice()
    {
        int offense = _trade.IsCarryingContraband() * 2;
        if (_universe.PoliceCount != 0)
        {
            offense |= _gameState.Cmdr.LegalStatus;
        }

        if (_rng.Random(256) >= offense)
        {
            return;
        }

        IShip police = _shipFactory.CreateShip("Viper");

        if (_universe.AddNewShip(police))
        {
            police.Flags = ShipProperties.Angry;
            if (_rng.Random(256) > 245)
            {
                police.Flags |= ShipProperties.HasECM;
            }

            police.Bravery = ((_rng.Random(256) * 2) | 64) & 127;
        }
        else
        {
            LogMessages.FailedToCreateShip(_logger, "Police");
        }
    }

    private void CheckMissiles(IObject obj)
    {
        if (MissileTarget == obj)
        {
            MissileTarget = null;
            IsMissileArmed = false;
            _gameState.InfoMessage("Target Lost");
        }

        foreach (IObject universeObj in _universe.GetAllObjects())
        {
            if (universeObj.Id == ObjectIds.Missile && ((IShip)universeObj).Target == obj)
            {
                universeObj.Flags |= ShipProperties.Dead;
            }
        }
    }

    private void CreateCougar()
    {
        if (_universe.ShipCount(ObjectIds.Cougar) != 0)
        {
            return;
        }

        IShip cougar = _shipFactory.CreateShip("Cougar");
        if (_universe.AddNewShip(cougar))
        {
            cougar.Flags = ShipProperties.HasECM; //// | FLG_CLOAKED;
            cougar.Bravery = 121;
            cougar.Velocity = 18;
        }
        else
        {
            LogMessages.FailedToCreateShip(_logger, "Cougar");
        }
    }

    private void CreateLoneWolf()
    {
        // The mission cannot see the universe, so its uniqueness check is done here.
        IShip loneWolf = _missions.LoneWolfSubstitute() is { } substitute
            && (!substitute.Unique
                || _universe.ShipCount(substitute.ShipName) == 0)
            ? _shipFactory.CreateShip(substitute.ShipName)
            : _shipFactory.CreateLoneWolf();

        if (_universe.AddNewShip(loneWolf))
        {
            // A bounty hunter waits for a bad enough record (FIST >= 40).
            if (!loneWolf.Flags.HasFlag(ShipProperties.BountyHunter) || _gameState.Cmdr.LegalStatus >= 40)
            {
                loneWolf.Flags |= ShipProperties.Angry;
            }

            if (_rng.Random(256) > 200 || loneWolf.Id == ObjectIds.Constrictor)
            {
                loneWolf.Flags |= ShipProperties.HasECM;
            }

            loneWolf.Bravery = ((_rng.Random(256) * 2) | 64) & 127;
            InBattle = true;
        }
        else
        {
            LogMessages.FailedToCreateShip(_logger, "Lone Hunter");
        }
    }

    private void CreateTrader()
    {
        IShip trader = _shipFactory.CreateTrader();

        if (_universe.AddNewShip(trader))
        {
            Matrix4x4 rotmat = trader.Rotmat;
            rotmat.M33 = -1.0f;
            trader.Rotmat = rotmat;

            trader.RotZ = _rng.Random(8);
            trader.Velocity = _rng.Random(32) | 16;
            trader.Bravery = _rng.Random(128);

            if (_rng.TrueOrFalse())
            {
                trader.Flags |= ShipProperties.HasECM;
            }

            //// if (rnd & 2)
            ////   space.universe[newship].flags |= FLG.FLG_ANGRY;
        }
        else
        {
            LogMessages.FailedToCreateShip(_logger, "Trader");
        }
    }

    private bool LaunchEnemy(IShip sourceShip, IShip newShip, ShipProperties flags, int bravery)
    {
        if (!_universe.AddNewShip(newShip, sourceShip.Location, sourceShip.Rotmat, sourceShip.RotX, sourceShip.RotZ))
        {
            return false;
        }

        if (sourceShip.Flags.HasFlag(ShipProperties.Station))
        {
            newShip.Velocity = 32;
            newShip.Location += newShip.Rotmat.GetRow(2) * 2;
        }

        newShip.Flags |= flags;
        newShip.RotZ /= 2;
        newShip.RotZ *= 2;
        newShip.Bravery = bravery;

        if (newShip.Id is ObjectIds.CargoCannister or ObjectIds.Alloy or ObjectIds.RockSplinter)
        {
            newShip.RotZ = ((_rng.Random(256) * 2) & 255) - 128;
            newShip.RotX = ((_rng.Random(256) * 2) & 255) - 128;
            newShip.Velocity = _rng.Random(16);
        }

        return true;
    }

    // lootId is the ship table id the wreckage is filed under, not a case built in.
    private void LaunchLoot(IShip ship, string lootId)
    {
        int count;

        if (lootId == ObjectIds.RockSplinter)
        {
            count = _rng.Random(4);
        }
        else
        {
            count = _rng.Random(256);
            if (count >= 128)
            {
                return;
            }

            count &= ship.LootMax;
            count &= 15;
        }

        for (int i = 0; i < count; i++)
        {
            IShip loot = _shipFactory.CreateShip(lootId);

            if (!LaunchEnemy(ship, loot, 0, 0))
            {
                LogMessages.FailedToCreateShip(_logger, "Loot");
            }
        }
    }

    private void LaunchShuttle()
    {
        if (_universe.ShipCount(ObjectIds.Transporter) != 0 ||
            _universe.ShipCount(ObjectIds.Shuttle) != 0 ||
            _rng.Random(256) < 253
            || _pilot.IsAutoPilotOn)
        {
            return;
        }

        IObject? station = _universe.StationOrSun;
        if (station == null)
        {
            return;
        }

        Debug.Assert(station.Flags.HasFlag(ShipProperties.Station), "Shuttle must be launched from a station");
        IShip shuttle = _rng.TrueOrFalse() ? _shipFactory.CreateShip("Shuttle") : _shipFactory.CreateShip("Transporter");
        if (!LaunchEnemy((IShip)station, shuttle, ShipProperties.HasECM | ShipProperties.FlyToPlanet, 113))
        {
            LogMessages.FailedToCreateShip(_logger, "Shuttle");
        }
    }

    private void MissileTactics(IShip missile)
    {
        if (_ship.EcmActive != 0)
        {
            _audio.PlayEffect(nameof(SoundEffect.Explode));

            missile.Flags |= ShipProperties.Dead;
            return;
        }

        if (!TryGetMissileHeading(missile, out Vector4 vec))
        {
            return;
        }

        Vector4 nvec = VectorMaths.UnitVector(vec);
        float direction = VectorMaths.VectorDotProduct(nvec, missile.Rotmat.GetRow(2));
        nvec.X = -nvec.X;
        nvec.Y = -nvec.Y;
        nvec.Z = -nvec.Z;
        direction = -direction;

        TrackObject(missile, direction, nvec);
        SetMissileAcceleration(missile, direction);
    }

    // Returns false when the missile has already resolved itself this tick
    // (detonated or jammed by the target's ECM).
    private bool TryGetMissileHeading(IShip missile, out Vector4 vec)
    {
        if (missile.Target == null)
        {
            if (missile.Location.Length() < 256)
            {
                missile.Flags |= ShipProperties.Dead;
                _audio.PlayEffect(nameof(SoundEffect.Explode));
                _ship.DamageShip(250, missile.Location.Z >= 0.0);
                vec = default;
                return false;
            }

            vec = missile.Location;
            return true;
        }

        vec = missile.Location - missile.Target.Location;

        // Close enough is each axis within 256 (a box), not distance within 256 (a
        // sphere), matching the 6502's x_hi/y_hi/z_hi tests and The New Kind.
        if (MathF.Abs(vec.X) < 256 && MathF.Abs(vec.Y) < 256 && MathF.Abs(vec.Z) < 256)
        {
            missile.Flags |= ShipProperties.Dead;

            if (!missile.Target.Flags.HasFlag(ShipProperties.Station))
            {
                ExplodeObject(missile.Target);
            }
            else
            {
                _audio.PlayEffect(nameof(SoundEffect.Explode));
            }

            return false;
        }

        if (_rng.Random(256) < 16 && missile.Target.Flags.HasFlag(ShipProperties.HasECM))
        {
            ActivateECM(false);
            return false;
        }

        return true;
    }

    // Missiles think every update, not one count in eight, so acceleration is scaled
    // by ticks or they'd gain speed 4.5x too fast at 60fps.
    private void SetMissileAcceleration(IShip missile, float direction)
    {
        const float cnt2 = 0.223f;

        float ticks = _gameState.Clock.Ticks;

        if (direction <= -0.167)
        {
            missile.Acceleration = -2 * ticks;
            return;
        }

        if (direction >= cnt2)
        {
            missile.Acceleration = 3 * ticks;
            return;
        }

        if (missile.Velocity < 6)
        {
            missile.Acceleration = 3 * ticks;
        }
        else if (_rng.Random(256) >= 200)
        {
            missile.Acceleration = -2 * ticks;
        }
    }
}
