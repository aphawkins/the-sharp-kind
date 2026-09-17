// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Renditions;
using EliteSharpLib.Audio;
using EliteSharpLib.Conflict;
using EliteSharpLib.Graphics;
using EliteSharpLib.Planets;
using EliteSharpLib.Ships;
using EliteSharpLib.Suns;
using EliteSharpLib.Trader;
using EliteSharpLib.Types;
using EliteSharpLib.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpKind.Audio;
using SharpKind.Maths;

namespace EliteSharpLib;

/// <summary>
/// This module handles all the flight system and management of the space universe.
/// </summary>
internal sealed class Space
{
    /// <summary>
    /// The explosion cloud's age: lit at <see cref="ExplosionStart"/>, grown
    /// by <see cref="ExplosionStep"/> a tick, and past <see
    /// cref="ExplosionEnd"/> the wreck is flagged for removal. The renderer
    /// reads the age to size the cloud; only <see cref="AgeExplosion"/>
    /// changes it.
    /// </summary>
    private const int ExplosionStart = 18;

    /// <inheritdoc cref="ExplosionStart"/>
    private const int ExplosionStep = 4;

    /// <inheritdoc cref="ExplosionStart"/>
    private const int ExplosionEnd = 251;

    /// <summary>
    /// The fastest an object can be left spinning. A spin pegged here does
    /// not wind down - see <see cref="DecayTowardsZero"/>.
    /// </summary>
    private const float MaxSpin = 127;

    // This tick's view-space clones, in the order they were moved, which is
    // the order they are drawn in.
    private readonly List<IObject> _toDraw = [];

    private readonly AudioController _audio;
    private readonly Combat _combat;
    private readonly IEliteDraw _draw;
    private readonly IRendition _rendition;
    private readonly GameState _gameState;
    private readonly ILogger<Space> _logger;
    private readonly Pilot _pilot;
    private readonly PlanetController _planet;
    private readonly PlayerShip _ship;
    private readonly Stars _stars;
    private readonly Trade _trade;
    private readonly Universe _universe;
    private readonly RNG _rng;

    /// <inheritdoc cref="MoveUniverse"/>
    private bool _universeVisible;

    private GalaxySeed _destinationPlanet = new();
    private float _hyperDistance;

    internal Space(
        GameState gameState,
        AudioController audio,
        Pilot pilot,
        Combat combat,
        Trade trade,
        PlayerShip ship,
        PlanetController planet,
        Stars stars,
        Universe universe,
        IEliteDraw draw,
        IRendition rendition,
        RNG rng,
        ILogger<Space>? logger = null)
    {
        _gameState = gameState;
        _audio = audio;
        _pilot = pilot;
        _combat = combat;
        _trade = trade;
        _ship = ship;
        _planet = planet;
        _stars = stars;
        _universe = universe;
        _draw = draw;
        _rendition = rendition;
        _rng = rng;
        _logger = logger ?? NullLogger<Space>.Instance;
    }

    internal int HyperCountdown { get; private set; }

    internal bool HyperGalactic { get; private set; }

    internal string HyperName { get; private set; } = string.Empty;

    internal bool IsHyperspaceReady { get; set; }

    internal void CountdownHyperspace()
    {
        if (HyperCountdown == 0)
        {
            CompleteHyperspace();
            return;
        }

        HyperCountdown--;
    }

    /// <summary>
    /// Dock the player into the space station.
    /// </summary>
    internal void DockPlayer()
    {
        _pilot.DisengageAutoPilot();
        _gameState.IsDocked = true;
        _gameState.Reset();
        _ship.Reset();
        _combat.ResetWeapons();
    }

    /// <summary>
    /// Engage the docking computer. For the moment we just do an instant dock if we are in the safe zone.
    /// </summary>
    internal void EngageDockingComputer()
    {
        if (_universe.IsStationPresent)
        {
            _gameState.SetView(Screen.Docking);
        }
    }

    internal void JumpWarp()
    {
        foreach (IObject obj in _universe.GetAllObjects())
        {
            // Anything with enough mass to hold the jump up; the table says it as a flag.
            if (obj.Traits.HasFlag(ShipTraits.MassLocks))
            {
                _gameState.InfoMessage("Mass Locked");
                return;
            }
        }

        if (_universe.Planet is null
            || _universe.StationOrSun is null
            || (_universe.Planet.Location.Length() < 75001)
            || (_universe.StationOrSun.Location.Length() < 75001))
        {
            _gameState.InfoMessage("Mass Locked");
            return;
        }

        float jump = _universe.Planet.Location.Length() < _universe.StationOrSun.Location.Length() ?
            _universe.Planet.Location.Length() - 75000 : _universe.StationOrSun.Location.Length() - 75000;

        if (jump > 1024)
        {
            jump = 1024;
        }

        foreach (IObject obj in _universe.GetAllObjects())
        {
            if (obj.Id != ObjectIds.None)
            {
                obj.Location = new(obj.Location.X, obj.Location.Y, obj.Location.Z - jump, 0);
            }
        }

        _stars.WarpStars = true;
        _gameState.MCount &= 63;
        _combat.InBattle = false;
    }

    internal void LaunchPlayer()
    {
        _ship.Speed = 12;

        // Rotate in the same direction that the station is spinning
        _ship.Roll = 15;
        _ship.Pitch = 0;
        _ship.Yaw = 0;
        _gameState.Cmdr.LegalStatus |= _trade.IsCarryingContraband();
        _stars.CreateNewStars();

        IObject planet = PlanetFactory.Create(
            _gameState.Config.Engine.Graphics.FillMode,
            _gameState.Config.Game.PlanetStyle,
            _draw,
            _rendition,
            (_gameState.DockedPlanet.A * 251) + _gameState.DockedPlanet.B,
            _gameState.CurrentPlanetData.TechLevel);
        if (!_universe.AddNewShip(planet, new(0, 0, 65536, 0), VectorMaths.GetLeftHandedBasisMatrix, 0, 0))
        {
            LogMessages.FailedToCreateShip(_logger, "Planet");
        }

        Matrix4x4 rotmat = VectorMaths.GetRightHandedBasisMatrix;
        _universe.AddNewStation(_gameState.CurrentPlanetData.TechLevel, new(0, 0, -256, 0), rotmat);

        _gameState.IsDocked = false;
    }

    // Rebuild the current planet in place so a Planet Style change shows on the
    // next frame rather than only at the next launch or hyperspace jump.
    internal void RefreshPlanetStyle()
    {
        if (_universe.Planet is not IObject oldPlanet)
        {
            return;
        }

        IObject planet = PlanetFactory.Create(
            _gameState.Config.Engine.Graphics.FillMode,
            _gameState.Config.Game.PlanetStyle,
            _draw,
            _rendition,
            (_gameState.DockedPlanet.A * 251) + _gameState.DockedPlanet.B,
            _gameState.CurrentPlanetData.TechLevel);

        _universe.RemoveShip(oldPlanet);
        if (!_universe.AddNewShip(planet, oldPlanet.Location, oldPlanet.Rotmat, 0, 0))
        {
            LogMessages.FailedToCreateShip(_logger, "Planet");
        }
    }

    // As RefreshPlanetStyle, for the sun. Does nothing when the slot holds a
    // station rather than a sun.
    internal void RefreshSunStyle()
    {
        if (_universe.StationOrSun is not { Id: ObjectIds.Sun } oldSun)
        {
            return;
        }

        IObject sun = SunFactory.Create(_gameState, _draw, _rendition, _rng);

        _universe.RemoveShip(oldSun);
        if (!_universe.AddNewShip(sun, oldSun.Location, oldSun.Rotmat, 0, 0))
        {
            LogMessages.FailedToCreateShip(_logger, "Sun");
        }
    }

    internal void StartGalacticHyperspace()
    {
        if (IsHyperspaceReady)
        {
            return;
        }

        if (!_ship.HasGalacticHyperdrive)
        {
            return;
        }

        IsHyperspaceReady = true;
        HyperCountdown = 2;
        HyperGalactic = true;
        _pilot.DisengageAutoPilot();
    }

    internal void StartHyperspace()
    {
        if (IsHyperspaceReady)
        {
            return;
        }

        _hyperDistance = PlanetController.CalculateDistanceToPlanet(_gameState.DockedPlanet, _gameState.HyperspacePlanet);

        if (((int)_hyperDistance == 0) || (_hyperDistance > _ship.Fuel))
        {
            return;
        }

        _destinationPlanet = new(_gameState.HyperspacePlanet);
        HyperName = _planet.NamePlanet(_destinationPlanet).CapitaliseFirstLetter();
        IsHyperspaceReady = true;
        HyperCountdown = 15;
        HyperGalactic = false;

        _pilot.DisengageAutoPilot();
    }

    internal void UpdateAltitude()
    {
        _ship.Altitude = PlayerShip.AltitudeMax;

        if (_gameState.InWitchspace)
        {
            return;
        }

        if (_universe.Planet == null)
        {
            return;
        }

        Vector4 vec = Vector4.Abs(_universe.Planet.Location);

        if (vec == Vector4.Zero ||
            vec.X > 65535 ||
            vec.Y > 65535 ||
            vec.Z > 65535)
        {
            return;
        }

        vec /= 256;
        vec *= vec;

        float dist = vec.X + vec.Y + vec.Z;

        if (dist > 65535)
        {
            return;
        }

        dist -= 9472;
        if (dist < 1)
        {
            _ship.Altitude = PlayerShip.AltitudeMin;
            _gameState.GameOver();
            return;
        }

        dist = MathF.Sqrt(dist);
        if (dist < 1)
        {
            _ship.Altitude = PlayerShip.AltitudeMin;
            _gameState.GameOver();
            return;
        }

        _ship.Altitude = Math.Clamp(dist * PlayerShip.AltitudeStep, PlayerShip.AltitudeMin, PlayerShip.AltitudeMax);
    }

    internal void UpdateCabinTemp()
    {
        _ship.CabinTemperature = PlayerShip.AmbientTemperature;

        if (_gameState.InWitchspace)
        {
            return;
        }

        if (_universe.IsStationPresent)
        {
            return;
        }

        if (_universe.StationOrSun == null)
        {
            return;
        }

        Vector4 vec = Vector4.Abs(_universe.StationOrSun.Location);

        if (vec == Vector4.Zero ||
            vec.X > 65535 ||
            vec.Y > 65535 ||
            vec.Z > 65535)
        {
            return;
        }

        vec /= 256;
        vec *= vec;

        // The original truncated the distance to whole units before flipping
        // it, so the quantisation is kept here.
        float dist = (int)((vec.X + vec.Y + vec.Z) / 256) * PlayerShip.TemperatureStep;

        if (dist >= PlayerShip.TemperatureMax)
        {
            return;
        }

        // Close to the sun reads hot, so the temperature is the inverse of the
        // distance, sitting on top of the ambient reading.
        _ship.CabinTemperature = PlayerShip.TemperatureMax - dist + PlayerShip.AmbientTemperature;

        if (_ship.CabinTemperature > PlayerShip.TemperatureMax)
        {
            _ship.CabinTemperature = PlayerShip.TemperatureMax;
            _gameState.GameOver();
            return;
        }

        if ((_ship.CabinTemperature < PlayerShip.ScoopTemperature) || (!_ship.HasFuelScoop))
        {
            return;
        }

        _ship.Fuel += _ship.Speed / 80;
        if (_ship.Fuel > _ship.MaxFuel)
        {
            _ship.Fuel = _ship.MaxFuel;
        }

        _gameState.InfoMessage("Fuel Scoop On");
    }

    /// <summary>
    /// Move every object in the universe on by a tick, collecting the
    /// view-space copies that <see cref="DrawUniverse"/> will draw.
    /// </summary>
    /// <remarks>
    /// This used to draw each object as it moved it. Composing a frame is
    /// now a separate pass, so what the two share is the clone: the game
    /// works out where everything is and what it looks like from here, and
    /// the frame is painted from that. A clone is a snapshot, so drawing it
    /// later cannot see a position the tick has since changed.
    /// </remarks>
    internal void MoveUniverse(float ticks)
    {
        _toDraw.Clear();

        // Asked once and remembered for the compose pass: the screen can change part way through a
        // tick (docking, hyperspace), and the frame built belongs to the screen the tick started on.
        _universeVisible = _gameState.ShowsUniverse;
        int i = -1;

        foreach (IObject obj in _universe.GetAllObjects())
        {
            i++;
            UpdateUniverseObject(obj, i, ticks);
        }

        _gameState.DetonateBomb = false;
    }

    /// <summary>
    /// Draw what <see cref="MoveUniverse"/> put in front of the camera.
    /// </summary>
    internal void DrawUniverse()
    {
        if (!_universeVisible)
        {
            return;
        }

        _draw.RenderStart();

        foreach (IObject obj in _toDraw)
        {
            _draw.DrawObject(obj);
        }

        _draw.RenderEnd();
    }

    private static int RotateByteLeft(int x) => ((x << 1) | (x >> 7)) & 255;

    // Winds down by one unit a tick unless pegged at the limit. Tests against magnitude rather than
    // an exact match, since a fraction-of-a-tick spin can sit just inside the peg and never equal it.
    private static float DecayTowardsZero(float spin, float ticks)
        => MathF.Abs(spin) >= MaxSpin
            ? spin
            : spin < 0 ? MathF.Min(spin + ticks, 0) : MathF.Max(spin - ticks, 0);

    // Original's small-angle rotation: ~1/19 radian turn with a 1/512 correction pulling the basis
    // back towards unit length. Scaled by ticks to make the turn rate a speed rather than a step.
    private static (Vector4 A, Vector4 B) RotateXFirst(Vector4 a, Vector4 b, float direction, float ticks)
    {
        Vector4 fx = a;
        Vector4 ux = b;

        // Divided first, scaled second - not the same as scaling the divisor, since a nineteenth
        // is not exactly representable. This way a whole tick reproduces the original arithmetic to the bit.
        Vector4 shrinkX = fx / 512 * ticks;
        Vector4 shrinkU = ux / 512 * ticks;
        Vector4 turnX = fx / 19 * ticks;
        Vector4 turnU = ux / 19 * ticks;

        if (direction < 0)
        {
            a = fx - shrinkX + turnU;
            b = ux - shrinkU - turnX;
        }
        else
        {
            a = fx - shrinkX - turnU;
            b = ux - shrinkU + turnX;
        }

        return (a, b);
    }

    /// <summary>
    /// Move a ship along its nose vector and apply any pending acceleration.
    /// </summary>
    private static Vector4 ApplyShipVelocity(IShip ship, Vector4 position, float ticks)
    {
        if ((int)ship.Velocity != 0)
        {
            position += ship.Rotmat.GetRow(2) * ship.Velocity * 1.5f * ticks;
        }

        if (ship.Acceleration != 0)
        {
            ship.Velocity += ship.Acceleration;
            ship.Acceleration = 0;
            if (ship.Velocity > ship.VelocityMax)
            {
                ship.Velocity = ship.VelocityMax;
            }

            if (ship.Velocity <= 0)
            {
                ship.Velocity = 1;
            }
        }

        return position;
    }

    /// <summary>
    /// Apply an object's own pitch and roll, damping each back towards zero
    /// unless it is pegged at the maximum rate.
    /// </summary>
    private static void SpinUniverseObject(IObject obj, float ticks)
    {
        float rotx = obj.RotX;
        float rotz = obj.RotZ;

        // If necessary rotate the object around the X axis...
        if ((int)rotx != 0)
        {
            (Vector4 nose, Vector4 roof) = RotateXFirst(obj.Rotmat.GetRow(2), obj.Rotmat.GetRow(1), rotx, ticks);
            obj.Rotmat = obj.Rotmat.WithRow(2, nose).WithRow(1, roof);

            obj.RotX = DecayTowardsZero(rotx, ticks);
        }

        // If necessary rotate the object around the Z axis...
        if ((int)rotz != 0)
        {
            (Vector4 side, Vector4 roof) = RotateXFirst(obj.Rotmat.GetRow(0), obj.Rotmat.GetRow(1), rotz, ticks);
            obj.Rotmat = obj.Rotmat.WithRow(0, side).WithRow(1, roof);

            obj.RotZ = DecayTowardsZero(rotz, ticks);
        }
    }

    /// <summary>
    /// Update and render a single object in the universe.
    /// </summary>
    private void UpdateUniverseObject(IObject obj, int i, float ticks)
    {
        if (obj.Id == ObjectIds.None)
        {
            return;
        }

        if (obj.Flags.HasFlag(ShipProperties.Remove))
        {
            RemoveUniverseObject(obj);
            return;
        }

        if (IsDestroyedByBomb(obj))
        {
            _audio.PlayEffect(nameof(SoundEffect.Explode));
            obj.Flags |= ShipProperties.Dead;
        }

        if (NeedsTactics(obj))
        {
            _combat.Tactics((IShip)obj, i);
        }

        MoveUniverseObject(obj, ticks);
        IObject flip = obj.Clone();
        SwitchToView(flip);

        if (obj.Traits.HasFlag(ShipTraits.Stellar))
        {
            DrawStellarObject(obj, flip);
            return;
        }

        if (obj.Location.Length() < 170)
        {
            DockOrScoop(obj);
            return;
        }

        if (obj.Location.Length() > 57344)
        {
            _combat.RemoveShip(obj);
            return;
        }

        AgeExplosion((IShip)flip);
        _toDraw.Add(flip);
        obj.Flags = flip.Flags;
        ((IShip)obj).ExpDelta = ((IShip)flip).ExpDelta;
        obj.Flags &= ~ShipProperties.Firing;

        if (obj.Flags.HasFlag(ShipProperties.Dead))
        {
            return;
        }

        _combat.CheckTarget((IShip)obj, flip);
    }

    /// <summary>
    /// Light a dead ship's explosion and age it one tick, flagging the wreck
    /// for removal once the cloud has spread as far as it goes.
    /// </summary>
    /// <remarks>
    /// This used to live in <c>EliteDraw.DrawObject</c>/<c>DrawExplosion</c>,
    /// which meant the renderer owned a piece of game state and advanced it
    /// once per drawn frame. Here it advances once per tick, whatever the
    /// frame rate does.
    /// <para>
    /// The screen test is what the renderer applied before it would touch
    /// anything, so a wreck on a chart screen still does not burn down. The
    /// seed-then-advance order is the original's too: a cloud is lit at 18
    /// and immediately aged to 22, which is the first size ever drawn.
    /// </para>
    /// </remarks>
    private void AgeExplosion(IShip ship)
    {
        if (!_universeVisible)
        {
            return;
        }

        if (ship.Flags.HasFlag(ShipProperties.Dead) && !ship.Flags.HasFlag(ShipProperties.Explosion))
        {
            ship.Flags |= ShipProperties.Explosion;
            ship.ExpDelta = ExplosionStart;
        }

        if (!ship.Flags.HasFlag(ShipProperties.Explosion))
        {
            return;
        }

        if (ship.ExpDelta > ExplosionEnd)
        {
            ship.Flags |= ShipProperties.Remove;
            return;
        }

        ship.ExpDelta += ExplosionStep * _gameState.Clock.Ticks;
    }

    /// <summary>
    /// Take a ship that has flagged itself for removal out of the universe,
    /// paying out any bounty it was carrying.
    /// </summary>
    private void RemoveUniverseObject(IObject obj)
    {
        if (obj.Id == ObjectIds.Viper)
        {
            _gameState.Cmdr.LegalStatus |= 64;
        }

        float bounty = ((IShip)obj).Bounty;

        if (((int)bounty != 0) && (!_gameState.InWitchspace))
        {
            _trade.Credits += bounty;
            _gameState.InfoMessage($"{_trade.Credits:N1} Credits");
        }

        _combat.RemoveShip(obj);
    }

    private void DrawStellarObject(IObject obj, IObject flip)
    {
        if (obj.Id == ObjectIds.Planet &&
            !_universe.IsStationPresent &&
            (obj.Location.Length() < 65792 /* was 49152 */))
        {
            MakeStationAppear();
        }

        _toDraw.Add(flip);
    }

    private void DockOrScoop(IObject obj)
    {
        if (obj.Flags.HasFlag(ShipProperties.Station))
        {
            CheckDocking(obj);
        }
        else
        {
            _combat.ScoopItem((IShip)obj);
        }
    }

    private bool IsDestroyedByBomb(IObject obj)
        => _gameState.DetonateBomb &&
            (!obj.Flags.HasFlag(ShipProperties.Dead)) &&
            !obj.Traits.HasFlag(ShipTraits.Stellar) &&
            !obj.Flags.HasFlag(ShipProperties.Station);

    private bool NeedsTactics(IObject obj)
        => (_gameState.CurrentScreen is
                not Screen.IntroOne and
                not Screen.IntroTwo and
                not Screen.GameOver and
                not Screen.EscapeCapsule) &&
            !obj.Traits.HasFlag(ShipTraits.Stellar) &&
            !obj.Flags.HasFlag(ShipProperties.Dead) &&
            !obj.Flags.HasFlag(ShipProperties.Inactive);

    private void SwitchToView(IObject flip)
    {
        if (_gameState.CurrentScreen is Screen.RearView or Screen.GameOver)
        {
            flip.Location = new(-flip.Location.X, flip.Location.Y, -flip.Location.Z, 0);

            Matrix4x4 rotmat = flip.Rotmat;

            rotmat.M11 = -rotmat.M11;
            rotmat.M13 = -rotmat.M13;

            rotmat.M21 = -rotmat.M21;
            rotmat.M23 = -rotmat.M23;

            rotmat.M31 = -rotmat.M31;
            rotmat.M33 = -rotmat.M33;

            flip.Rotmat = rotmat;
            return;
        }

        if (_gameState.CurrentScreen == Screen.LeftView)
        {
            float tmp = flip.Location.X;
            flip.Location = new(flip.Location.Z, flip.Location.Y, -tmp, 0);

            if (flip.Traits.HasFlag(ShipTraits.Stellar))
            {
                return;
            }

            Matrix4x4 rotmat = flip.Rotmat;

            tmp = rotmat.M11;
            rotmat.M11 = rotmat.M13;
            rotmat.M13 = -tmp;

            tmp = rotmat.M21;
            rotmat.M21 = rotmat.M23;
            rotmat.M23 = -tmp;

            tmp = rotmat.M31;
            rotmat.M31 = rotmat.M33;
            rotmat.M33 = -tmp;

            flip.Rotmat = rotmat;
            return;
        }

        if (_gameState.CurrentScreen == Screen.RightView)
        {
            float tmp = flip.Location.X;
            flip.Location = new(-flip.Location.Z, flip.Location.Y, tmp, 0);

            if (flip.Traits.HasFlag(ShipTraits.Stellar))
            {
                return;
            }

            Matrix4x4 rotmat = flip.Rotmat;

            tmp = rotmat.M11;
            rotmat.M11 = -rotmat.M13;
            rotmat.M13 = tmp;

            tmp = rotmat.M21;
            rotmat.M21 = -rotmat.M23;
            rotmat.M23 = tmp;

            tmp = rotmat.M31;
            rotmat.M31 = -rotmat.M33;
            rotmat.M33 = tmp;

            flip.Rotmat = rotmat;
        }
    }

    private void CheckDocking(IObject obj)
    {
        if (_gameState.IsDocked)
        {
            return;
        }

        if (IsDocking(obj))
        {
            _gameState.SetView(Screen.Docking);
            return;
        }

        if (_ship.Speed >= 5)
        {
            _gameState.GameOver();
            return;
        }

        _ship.Speed = 1;
        _ship.DamageShip(5, obj.Location.Z > 0);
        _audio.PlayEffect(nameof(SoundEffect.Crash));
    }

    private void CompleteHyperspace()
    {
        IsHyperspaceReady = false;
        _gameState.InWitchspace = false;

        if (HyperGalactic)
        {
            _ship.HasGalacticHyperdrive = false;
            HyperGalactic = false;
            EnterNextGalaxy();
            _gameState.Cmdr.LegalStatus = 0;
        }
        else
        {
            _ship.Fuel -= _hyperDistance;
            _gameState.Cmdr.LegalStatus /= 2;

            if ((_rng.Random(256) >= 253) || (_ship.Pitch >= _ship.MaxPitch))
            {
                EnterWitchspace();
                return;
            }

            _gameState.DockedPlanet = new(_destinationPlanet);
        }

        _trade.MarketRandomiser = _rng.Random(256);
        _gameState.CurrentPlanetData = PlanetController.GeneratePlanetData(_gameState.DockedPlanet);
        _trade.GenerateStockMarket();

        _ship.Speed = 12;
        _ship.Roll = 0;
        _ship.Pitch = 0;
        _ship.Yaw = 0;
        _stars.CreateNewStars();
        _combat.Reset();
        _universe.ClearUniverse();
        Vector4 position = new()
        {
            Z = ((_gameState.DockedPlanet.B & 7) + 7) / 2f,
        };
        position.X = position.Z / 2;
        position.Y = position.X;

        position.X *= 65536;
        position.Y *= 65536;
        position.Z *= 65536;

        if ((_gameState.DockedPlanet.B & 1) == 0)
        {
            position.X = -position.X;
            position.Y = -position.Y;
        }

        IObject planet = PlanetFactory.Create(
            _gameState.Config.Engine.Graphics.FillMode,
            _gameState.Config.Game.PlanetStyle,
            _draw,
            _rendition,
            (_gameState.DockedPlanet.A * 251) + _gameState.DockedPlanet.B,
            _gameState.CurrentPlanetData.TechLevel);
        if (!_universe.AddNewShip(planet, position, VectorMaths.GetLeftHandedBasisMatrix, 0, 0))
        {
            LogMessages.FailedToCreateShip(_logger, "Planet");
        }

        position.Z = -(((_gameState.DockedPlanet.D & 7) | 1) << 16);
        position.X = ((_gameState.DockedPlanet.F & 3) << 16) | ((_gameState.DockedPlanet.F & 3) << 8);

        IObject sun = SunFactory.Create(_gameState, _draw, _rendition, _rng);
        if (!_universe.AddNewShip(sun, position, VectorMaths.GetLeftHandedBasisMatrix, 0, 0))
        {
            LogMessages.FailedToCreateShip(_logger, "Sun");
        }

        _gameState.SetView(Screen.Hyperspace);
    }

    private void EnterNextGalaxy()
    {
        _gameState.Cmdr.GalaxyNumber++;
        _gameState.Cmdr.GalaxyNumber &= 7;

        _gameState.Cmdr.Galaxy = new()
        {
            A = RotateByteLeft(_gameState.Cmdr.Galaxy.A),
            B = RotateByteLeft(_gameState.Cmdr.Galaxy.B),
            C = RotateByteLeft(_gameState.Cmdr.Galaxy.C),
            D = RotateByteLeft(_gameState.Cmdr.Galaxy.D),
            E = RotateByteLeft(_gameState.Cmdr.Galaxy.E),
            F = RotateByteLeft(_gameState.Cmdr.Galaxy.F),
        };

        _gameState.DockedPlanet = _planet.FindPlanet(_gameState.Cmdr.Galaxy, new(0x60, 0x60));
        _gameState.HyperspacePlanet = new(_gameState.DockedPlanet);
    }

    private void EnterWitchspace()
    {
        _gameState.InWitchspace = true;
        _gameState.DockedPlanet.B ^= 31;
        _combat.InBattle = true;

        _ship.Speed = 12;
        _ship.Roll = 0;
        _ship.Pitch = 0;
        _ship.Yaw = 0;
        _stars.CreateNewWitchspaceStars();
        _combat.Reset();
        _universe.ClearUniverse();

        for (int i = 0; i < 4; i++)
        {
            _combat.CreateThargoid();
        }

        _gameState.SetView(Screen.Hyperspace);
    }

    /// <summary>
    /// Check if we are correctly aligned to dock.
    /// </summary>
    private bool IsDocking(IObject ship)
    {
        // Don't want it to kill anyone!
        if (_pilot.IsAutoPilotOn)
        {
            return true;
        }

        float fz = ship.Rotmat.M33;

        if (fz > -0.90)
        {
            return false;
        }

        Vector4 vec = VectorMaths.UnitVector(ship.Location);

        if (vec.Z < 0.927)
        {
            return false;
        }

        float ux = ship.Rotmat.M21;
        if (ux < 0)
        {
            ux = -ux;
        }

        return ux >= 0.84;
    }

    private void MakeStationAppear()
    {
        Vector4 location = _universe.Planet!.Location;
        Vector4 vec = new(_rng.Random(-16384, 16384), _rng.Random(-16384, 16384), _rng.Random(32768), 0);
        vec = VectorMaths.UnitVector(vec);
        Vector4 position = location - (vec * 65792);

        ////  VectorMaths.set_init_matrix (rotmat);
        Matrix4x4 rotmat = new(
            1,
            0,
            0,
            0,
            vec.X,
            vec.Z,
            -vec.Y,
            0,
            vec.X,
            vec.Y,
            vec.Z,
            0,
            0,
            0,
            0,
            0);

        Matrix4x4 orthonormalized = VectorMaths.OrthonormalizeBasis(rotmat);

        _universe.AddNewStation(_gameState.CurrentPlanetData.TechLevel, position, orthonormalized);
    }

    /// <summary>
    /// Update an objects location in the universe.
    /// </summary>
    private void MoveUniverseObject(IObject obj, float ticks)
    {
        // The player does not move; the universe moves past. Roll and pitch shear every object's
        // position and orientation, and speed slides the whole lot towards the camera.
        float alpha = _ship.Roll / 256 * ticks;
        float beta = _ship.Pitch / 256 * ticks;
        float gamma = _ship.Yaw / 256 * ticks;

        Vector4 position = obj.Location;
        if (obj is IShip shipEx &&
            !obj.Flags.HasFlag(ShipProperties.Dead) &&
            !obj.Traits.HasFlag(ShipTraits.Stellar))
        {
            position = ApplyShipVelocity(shipEx, position, ticks);
        }

        float k2 = position.Y - (alpha * position.X);
        position.Z += beta * k2;
        position.Y = k2 - (position.Z * beta);
        position.X += alpha * position.Y;

        // Yaw is the third shear, about Y - not the original's, which rolls and pitches only.
        position.X -= gamma * position.Z;
        position.Z += gamma * position.X;

        position.Z -= _ship.Speed * ticks;

        obj.Location = position;

        // Original MV45: the sun returns here, before rotating its own orientation or spin.
        if (obj.Id == ObjectIds.Sun)
        {
            return;
        }

        if (obj.Id == ObjectIds.Planet)
        {
            beta = 0.0f;
            gamma = 0.0f;
        }

        obj.Rotmat = VectorMaths.RotateVector(obj.Rotmat, alpha, beta, gamma);

        if (obj.Flags.HasFlag(ShipProperties.Dead))
        {
            return;
        }

        SpinUniverseObject(obj, ticks);

        // Orthonormalize the rotation matrix...
        obj.Rotmat = VectorMaths.OrthonormalizeBasis(obj.Rotmat);
    }
}
