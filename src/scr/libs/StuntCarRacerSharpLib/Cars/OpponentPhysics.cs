// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind;
using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Cars;

// Not a full physics simulation: rides the road surface at scripted per-piece speeds, with randomized steering and spring dynamics.
public sealed partial class OpponentPhysics
{
    internal const int NumOpponents = 11;

    private const int RearLeft = 0;
    private const int RearRight = 1;
    private const int Front = 2;
    private const int NumWheels = 3;

    private const int Reduction = 238; // (238/256)
    private const int Increase = 276; // (276/256)

    // Opponent attribute flags.
    private const int ObstructsPlayer = 2;
    private const int Wheelie = 4;
    private const int DrivesNearEdge = 8;
    private const int PushPlayer = 32;

    private static readonly string[] s_names =
    [
        "Hot Rod",
        "Whizz Kid",
        "Bad Guy",
        "The Dodger",
        "Big Ed",
        "Max Boost",
        "Dare Devil",
        "High Flyer",
        "Bully Boy",
        "Jumping Jack",
        "Road Hog",
    ];

    private static readonly byte[] s_attributes =
    [
        PushPlayer | ObstructsPlayer, // Hot Rod
        PushPlayer, // Whizz Kid
        64 | PushPlayer | ObstructsPlayer, // Bad Guy
        PushPlayer, // The Dodger
        PushPlayer | 16 | DrivesNearEdge | Wheelie | ObstructsPlayer, // Big Ed
        Wheelie, // Max Boost
        PushPlayer | 16, // Dare Devil
        16 | Wheelie, // High Flyer
        64 | DrivesNearEdge | ObstructsPlayer, // Bully Boy
        16, // Jumping Jack
        DrivesNearEdge, // Road Hog
    ];

    private readonly Track _track;
    private readonly CarPhysics _player;
    private readonly IRandomSource _randomSource;
    private readonly int[] _speedValueOverrides;
    private readonly int[] _actualHeight = new int[NumWheels];
    private readonly int[] _ySpeed = new int[NumWheels];
    private readonly int[] _yAcceleration = new int[NumWheels];
    private readonly int _enginePower = 236; // (236 standard, 314 super)

    // Steering state (original B1bbbe); the third value is the random steering count.
    private readonly int[] _steeringShift = new int[3];

    // Wheel positions on/above the road (world track units; y in Amiga height units).
    private Coord3D _rearLeftRoadPos;
    private Coord3D _rearRightRoadPos;
    private int _frontRoadPosY;
    private Coord3D _frontLeftRoadPos;
    private Coord3D _frontRightRoadPos;

    private Coord3D _shadowRearLeft;
    private Coord3D _shadowRearRight;
    private Coord3D _shadowFrontLeft;
    private Coord3D _shadowFrontRight;

    private int _oldRearLeftDifference;
    private int _oldRearRightDifference;
    private int _oldFrontDifference;
    private int _smallestDifference;

    private int _engineZAcceleration;
    private int _maxSpeed;
    private int _zSpeed;
    private bool _requiredZSpeedReached;
    private int _speedValuePiece = -1;
    private int _speedValue;
    private int _byteCount;

    private int _distanceIntoSection;
    private int _roadXPosition;

    // Steering state (original B1bb9d/B1bbc2/B1bbbd).
    private int _lastPieceType;
    private int _steeringTableShift;
    private int _steeringAdjust;

    // Player interaction state.
    private int _differenceBetweenPlayers;
    private int _smallestDistanceBetweenPlayers;
    private int _xDifference;
    private bool _playerToRight;
    private int _suggestedRoadXPosition;
    private int _collisionCountdown; // original B1bbc3
    private int _collidedFlag; // original B1bbeb
    private int _carsCollidedDelay;
    private bool _carsCollided;
    private int _carToCarXAcceleration;
    private int _carToCarYAcceleration;
    private int _carToCarZAcceleration;
    private int[]? _distancesAroundRoad;
    private int _totalRoadDistance;

    // Lap data.
    private bool _carOnFirstHalfOfLap;

    public OpponentPhysics(Track track, CarPhysics player)
        : this(track, player, new RandomSource(Random.Shared))
    {
    }

    public OpponentPhysics(Track track, CarPhysics player, IRandomSource randomSource)
    {
        ArgumentNullException.ThrowIfNull(track);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(randomSource);

        _track = track;
        _player = player;
        _randomSource = randomSource;

        // per-piece required speed overrides (set by the draw bridge animation)
        _speedValueOverrides = new int[track.NumPieces];
        Array.Fill(_speedValueOverrides, -1);

        // wire the car-to-car collision transfer into the player physics
        _player.CarToCarCollision = TransferCollisionToPlayer;
    }

    public int OpponentId { get; private set; } = -1;

    public string Name => OpponentId >= 0 ? s_names[OpponentId] : string.Empty;

    public int CurrentPiece { get; private set; }

    public int LapNumber { get; private set; }

    public bool TouchingRoad { get; private set; }

    // Render-space outputs (as output by the original OpponentBehaviour).
    public int X { get; private set; }

    public int Y { get; private set; }

    public int Z { get; private set; }

    // Corner positions for drawing the opponent, in track units with the
    // display y conversion already applied (original y values are halved).
    public Coord3D VisualRearLeft { get; private set; }

    public Coord3D VisualRearRight { get; private set; }

    public Coord3D VisualFrontLeft { get; private set; }

    public Coord3D VisualFrontRight { get; private set; }

    public bool ShadowVisible { get; private set; }

    public Coord3D ShadowRearLeft => Shadow(_shadowRearLeft);

    public Coord3D ShadowRearRight => Shadow(_shadowRearRight);

    public Coord3D ShadowFrontLeft => Shadow(_shadowFrontLeft);

    public Coord3D ShadowFrontRight => Shadow(_shadowFrontRight);

    // Set for one frame when the car-to-car collision sound should play.
    internal bool HitCarSoundTriggered { get; private set; }

    // Leave the race without another one starting: the reference sets
    // `opponentsID = NO_OPPONENT` when M returns to the track menu
    // (`StuntCarRacer.cpp:1731-1741`), which is what stops the opponent
    // being drawn there.
    public void Clear() => OpponentId = -1;

    // Start a new race: reset and place the opponent at the start piece.
    public void StartRace()
    {
        OpponentId = _randomSource.NextInt() % NumOpponents;

        _oldRearLeftDifference = 0;
        _oldRearRightDifference = 0;
        _oldFrontDifference = 0;

        for (int i = 0; i < NumWheels; i++)
        {
            _ySpeed[i] = 0;
        }

        _zSpeed = 0;
        _requiredZSpeedReached = false;

        _player.PlayerCloseToOpponent = false;
        _player.OpponentBehindPlayer = false;

        CurrentPiece = _track.PlayersStartPiece;
        _distanceIntoSection = 0x400; // half way into section
        _roadXPosition = 0x4c;

        // initialise opponent data: position a random amount above the road
        CalculateRoadWheelPositions();
        int r = (_randomSource.NextInt() & 0x7f) + 0x68;
        _actualHeight[RearLeft] = _rearLeftRoadPos.Y + r;
        _actualHeight[RearRight] = _rearRightRoadPos.Y + r;
        _actualHeight[Front] = _frontRoadPosY + r;

        // set opponent max speed
        int trackIndex = (int)_track.Id;
        int s = _randomSource.NextInt() & OpponentData.TrackSpeedValues[trackIndex];
        s += OpponentData.TrackSpeedValues[trackIndex + 8];
        _maxSpeed = s;

        ResetLapData();
    }

    public void ResetLapData()
    {
        LapNumber = 0;
        _carOnFirstHalfOfLap = false;
    }

    // One frame of opponent behaviour (original OpponentBehaviour).
    public void Update() => Update(false);

    public void Update(bool paused)
    {
        HitCarSoundTriggered = false;

        _player.CalculateRoadPosition();

        if (!paused)
        {
            Movement();
            CalculateDistancesBetweenPlayers();
            PlayerInteraction();
        }
        else
        {
            CalculateDistancesBetweenPlayers();
        }

        CalculateRoadWheelPositions();

        // calculate the opponent's new centre point; use the higher of road
        // and actual heights for display (the original NEW_OPP_METHOD)
        int visRearLeftY = Math.Max(_rearLeftRoadPos.Y, _actualHeight[RearLeft]);
        int visRearRightY = Math.Max(_rearRightRoadPos.Y, _actualHeight[RearRight]);
        int visFrontY = Math.Max(_frontRoadPosY, _actualHeight[Front]);

        int rearY = (visRearLeftY + visRearRightY) / 2;
        int opponentY = (rearY + visFrontY) / 2;

        // raise the opponent slightly (stops them sinking into the road)
        opponentY += 20;

        int opponentX = (_frontLeftRoadPos.X + _frontRightRoadPos.X + _rearLeftRoadPos.X + _rearRightRoadPos.X) / 4;
        int opponentZ = (_frontLeftRoadPos.Z + _frontRightRoadPos.Z + _rearLeftRoadPos.Z + _rearRightRoadPos.Z) / 4;

        X = opponentX << Track.LogPrecision;
        Y = -(opponentY << (Track.LogPrecision - 3)) * 4;
        Z = opponentZ << Track.LogPrecision;

        // corner positions for the renderer (display y = Amiga y / 2)
        VisualRearLeft = new(_rearLeftRoadPos.X, visRearLeftY / 2, _rearLeftRoadPos.Z);
        VisualRearRight = new(_rearRightRoadPos.X, visRearRightY / 2, _rearRightRoadPos.Z);
        VisualFrontLeft = new(_frontLeftRoadPos.X, visFrontY / 2, _frontLeftRoadPos.Z);
        VisualFrontRight = new(_frontRightRoadPos.X, visFrontY / 2, _frontRightRoadPos.Z);
    }

    public void UpdateLapData()
    {
        int startFinishPiece = _track.StartLinePiece + 1 < _track.NumPieces ? _track.StartLinePiece + 1 : 0;

        if (_carOnFirstHalfOfLap)
        {
            if (CurrentPiece == _track.HalfALapPiece)
            {
                _carOnFirstHalfOfLap = false;
            }
        }
        else if (CurrentPiece == startFinishPiece)
        {
            _carOnFirstHalfOfLap = true;
            LapNumber++;
        }
    }

    // Returns negative if the player is winning (original CalculateIfWinning).
    public int CalculateIfWinning()
    {
        int startFinishPiece = _track.StartLinePiece + 1 < _track.NumPieces ? _track.StartLinePiece + 1 : 0;

        int result = LapNumber - _player.LapNumber;
        if (result != 0)
        {
            return result; // on different laps
        }

        int p = _player.CurrentPiece - startFinishPiece;
        if (p < 0)
        {
            p += _track.NumPieces;
        }

        int o = CurrentPiece - startFinishPiece;
        if (o < 0)
        {
            o += _track.NumPieces;
        }

        result = o - p;
        if (result != 0)
        {
            return result; // on different pieces
        }

        return _differenceBetweenPlayers;
    }

    // Distance between opponent and player for display (original
    // CalculateOpponentsDistance; negative when the opponent is behind).
    public int DistanceToPlayer()
    {
        int dist = _smallestDistanceBetweenPlayers;
        dist += dist >> 2;
        dist >>= 2;

        return _player.OpponentBehindPlayer ? -dist : dist;
    }

    private static Coord3D Shadow(Coord3D position)
        => new(position.X, 7 + (position.Y / 2), position.Z);

    private static int CalcSurfacePosition(out bool nextSegment, int distance, int shiftZ)
    {
        int surfacePosition = distance & 0xff;

        nextSegment = false;
        surfacePosition += shiftZ;
        if (surfacePosition >= 256)
        {
            nextSegment = true;
            surfacePosition &= 0xff;
        }

        return surfacePosition;
    }

    // Height of the surface at an x,z position using linear interpolation.
    private static int RoadWheelHeight(in SurfaceCorners c, int sx, int sz)
    {
        // first do x interpolation
        int sya = c.Y1 + ((sx * (c.Y4 - c.Y1)) >> 8);
        int syb = c.Y2 + ((sx * (c.Y3 - c.Y2)) >> 8);

        // now do z interpolation
        int y = (syb << 8) + (sz * (sya - syb));

        return y >> 9;
    }

    private void CalculateRoadWheelPositions()
    {
        int piece = CurrentPiece;
        bool drawShadow = true;

        // rear wheel position
        int distance = _distanceIntoSection - 64;
        if (distance < 0)
        {
            // DIRECTION DEPENDANT - go to previous piece
            piece--;
            if (piece < 0)
            {
                piece = _track.NumPieces - 1;
            }

            distance += _track.Pieces[piece].NumSegments * 256;
        }

        // fetch the 4 surface coords surrounding the rear wheels
        int segment = distance >> 8;
        SurfaceCorners corners = GetSurfaceCorners(piece, segment);

        // don't draw the opponent's shadow on black road segments
        if (_track.Pieces[piece].RoadColours[segment] == Track.ScrBaseColour)
        {
            drawShadow = false;
        }

        // calculate segment left side x,z at the rear wheel distance
        int surfacePosition = CalcSurfacePosition(out bool nextSegment, distance, _steeringShift[0]);
        int leftSideX;
        int leftSideZ;
        if (!nextSegment)
        {
            leftSideX = corners.X2 + ((surfacePosition * (corners.X1 - corners.X2)) >> 8);
            leftSideZ = corners.Z2 + ((surfacePosition * (corners.Z1 - corners.Z2)) >> 8);
        }
        else
        {
            // use other end's value as base (as Amiga StuntCarRacerSharp, though
            // it should really use the next segment's values)
            leftSideX = corners.X1 + ((surfacePosition * (corners.X1 - corners.X2)) >> 8);
            leftSideZ = corners.Z1 + ((surfacePosition * (corners.Z1 - corners.Z2)) >> 8);
        }

        // calculate segment right side x,z at the rear wheel distance
        surfacePosition = CalcSurfacePosition(out nextSegment, distance, _steeringShift[2]);
        int rightSideX;
        int rightSideZ;
        if (!nextSegment)
        {
            rightSideX = corners.X3 + ((surfacePosition * (corners.X4 - corners.X3)) >> 8);
            rightSideZ = corners.Z3 + ((surfacePosition * (corners.Z4 - corners.Z3)) >> 8);
        }
        else
        {
            rightSideX = corners.X4 + ((surfacePosition * (corners.X4 - corners.X3)) >> 8);
            rightSideZ = corners.Z4 + ((surfacePosition * (corners.Z4 - corners.Z3)) >> 8);
        }

        // half the x distance that the opponent's rear wheels span
        int heightIndex = Math.Abs(_actualHeight[RearLeft] - _actualHeight[RearRight]) >> 4;
        if (heightIndex >= OpponentData.XSpans.Length)
        {
            heightIndex = OpponentData.XSpans.Length - 1;
        }

        int spanX = OpponentData.XSpans[heightIndex];

        // shadow spans are reduced to account for the greater width of
        // sloped segments
        int xd = rightSideX - leftSideX;
        int yd = (corners.Y3 - corners.Y2) / 4;
        int zd = rightSideZ - leftSideZ;
        double baseWidth = Math.Sqrt(((double)xd * xd) + ((double)zd * zd));
        double slopeWidth = Math.Sqrt((baseWidth * baseWidth) + ((double)yd * yd));
        int shadowSpanX = slopeWidth < 1 ? spanX : (int)(spanX * baseWidth / slopeWidth);

        int sz = distance & 0xff; // z position of rear wheels

        // rear left road coordinate
        int sx = _roadXPosition - spanX;
        _rearLeftRoadPos = new(
            leftSideX + ((sx * xd) >> 8),
            RoadWheelHeight(corners, sx, sz),
            leftSideZ + ((sx * zd) >> 8));

        // rear left shadow coordinate
        sx = _roadXPosition - shadowSpanX;
        _shadowRearLeft = new(
            leftSideX + ((sx * xd) >> 8),
            RoadWheelHeight(corners, sx, sz),
            leftSideZ + ((sx * zd) >> 8));

        // rear right road coordinate
        sx = _roadXPosition + spanX;
        _rearRightRoadPos = new(
            leftSideX + ((sx * xd) >> 8),
            RoadWheelHeight(corners, sx, sz),
            leftSideZ + ((sx * zd) >> 8));

        // rear right shadow coordinate
        sx = _roadXPosition + shadowSpanX;
        _shadowRearRight = new(
            leftSideX + ((sx * xd) >> 8),
            RoadWheelHeight(corners, sx, sz),
            leftSideZ + ((sx * zd) >> 8));

        // position rear coordinates within the world
        int pieceX = _track.Pieces[piece].CubeX << (Track.CubeSizeLog2 - Track.LogPrecision);
        int pieceZ = _track.Pieces[piece].CubeZ << (Track.CubeSizeLog2 - Track.LogPrecision);
        _rearLeftRoadPos = _rearLeftRoadPos with { X = _rearLeftRoadPos.X + pieceX, Z = _rearLeftRoadPos.Z + pieceZ };
        _rearRightRoadPos = _rearRightRoadPos with { X = _rearRightRoadPos.X + pieceX, Z = _rearRightRoadPos.Z + pieceZ };
        _shadowRearLeft = _shadowRearLeft with { X = _shadowRearLeft.X + pieceX, Z = _shadowRearLeft.Z + pieceZ };
        _shadowRearRight = _shadowRearRight with { X = _shadowRearRight.X + pieceX, Z = _shadowRearRight.Z + pieceZ };

        // front wheels: car length is 1.5 times width
        int diff = _rearRightRoadPos.X - _rearLeftRoadPos.X;
        int xdiff = diff + (diff >> 1);
        diff = _rearRightRoadPos.Z - _rearLeftRoadPos.Z;
        int zdiff = diff + (diff >> 1);
        _frontLeftRoadPos = new(_rearLeftRoadPos.X - zdiff, 0, _rearLeftRoadPos.Z + xdiff);
        _frontRightRoadPos = new(_rearRightRoadPos.X - zdiff, 0, _rearRightRoadPos.Z + xdiff);

        diff = _shadowRearRight.X - _shadowRearLeft.X;
        xdiff = diff + (diff >> 1);
        diff = _shadowRearRight.Z - _shadowRearLeft.Z;
        zdiff = diff + (diff >> 1);
        _shadowFrontLeft = new(_shadowRearLeft.X - zdiff, 0, _shadowRearLeft.Z + xdiff);
        _shadowFrontRight = new(_shadowRearRight.X - zdiff, 0, _shadowRearRight.Z + xdiff);

        // add 128 to get z of the opponent's front
        distance += 128;
        if (distance >= _track.Pieces[piece].NumSegments * 256)
        {
            // DIRECTION DEPENDANT - go to next piece
            distance -= _track.Pieces[piece].NumSegments * 256;
            piece++;
            if (piece >= _track.NumPieces)
            {
                piece = 0;
            }
        }

        // fetch the 4 surface coords surrounding the front wheels
        segment = distance >> 8;
        corners = GetSurfaceCorners(piece, segment);
        if (_track.Pieces[piece].RoadColours[segment] == Track.ScrBaseColour)
        {
            drawShadow = false;
        }

        sz = distance & 0xff;
        sx = _roadXPosition & 0xff;

        _frontRoadPosY = RoadWheelHeight(corners, sx, sz);

        // front left and right road y coordinates
        sx = _roadXPosition - spanX;
        _frontLeftRoadPos = _frontLeftRoadPos with { Y = RoadWheelHeight(corners, sx, sz) };
        sx = _roadXPosition + spanX;
        _frontRightRoadPos = _frontRightRoadPos with { Y = RoadWheelHeight(corners, sx, sz) };

        // front shadow y coordinates
        sx = _roadXPosition - shadowSpanX;
        _shadowFrontLeft = _shadowFrontLeft with { Y = RoadWheelHeight(corners, sx, sz) };
        sx = _roadXPosition + shadowSpanX;
        _shadowFrontRight = _shadowFrontRight with { Y = RoadWheelHeight(corners, sx, sz) };

        ShadowVisible = drawShadow;
    }

    private SurfaceCorners GetSurfaceCorners(int piece, int segment)
    {
        IList<Coord3D> coords = _track.Pieces[piece].Coords;

        Coord3D c2 = coords[segment * 4];
        Coord3D c3 = coords[(segment * 4) + 1];
        segment++;
        Coord3D c1 = coords[segment * 4];
        Coord3D c4 = coords[(segment * 4) + 1];

        return new(c1.X, c1.Y, c1.Z, c2.X, c2.Y, c2.Z, c3.X, c3.Y, c3.Z, c4.X, c4.Y, c4.Z);
    }

    private void Movement()
    {
        if (!_player.DropStartDone)
        {
            return;
        }

        UpdateActualWheelHeights();
        RandomizeSteering();
        GetEngineAcceleration();
        AdjustEngineAcceleration();
        UpdateZSpeed();

        // advance the opponent's distance into the section using z speed
        // scaled by the piece's length reduction
        int value = (_zSpeed * (_track.Pieces[CurrentPiece].LengthReduction << 7)) << 1;
        value >>= 16;
        value *= Reduction;
        value >>= 8;
        value <<= 3;
        int b = value & 0xff;
        value >>= 8;
        _byteCount += b;
        if (_byteCount > 0xff)
        {
            ++value;
            _byteCount &= 0xff;
        }

        _distanceIntoSection += value;

        if (_distanceIntoSection >= _track.Pieces[CurrentPiece].NumSegments * 256)
        {
            // DIRECTION DEPENDANT - go to next piece
            _distanceIntoSection -= _track.Pieces[CurrentPiece].NumSegments * 256;

            int piece = CurrentPiece + 1;
            CurrentPiece = piece >= _track.NumPieces ? 0 : piece;
        }
    }

    private void UpdateActualWheelHeights()
    {
        _smallestDifference = -32768;

        // increase collision when on a curve
        int heightAdjust = (_track.Pieces[CurrentPiece].Type & 0x80) != 0 ? 124 : 40;

        int touching = 0;

        CalculateWheelDifference(
            _rearLeftRoadPos.Y,
            _actualHeight[RearLeft],
            heightAdjust,
            ref _oldRearLeftDifference,
            out int newRearLeftDifference,
            ref touching);
        CalculateWheelDifference(
            _rearRightRoadPos.Y,
            _actualHeight[RearRight],
            heightAdjust,
            ref _oldRearRightDifference,
            out int newRearRightDifference,
            ref touching);
        CalculateWheelDifference(
            _frontRoadPosY,
            _actualHeight[Front],
            heightAdjust,
            ref _oldFrontDifference,
            out int newFrontDifference,
            ref touching);

        TouchingRoad = touching != 0;

        // accelerations from 6 parts of the wheel difference in question and
        // 1 part of the other two wheels
        int totalDiff = newRearLeftDifference + newRearRightDifference + newFrontDifference;

        _yAcceleration[RearLeft] = (totalDiff + newRearLeftDifference + (newRearLeftDifference << 2)) >> 3;
        _yAcceleration[RearRight] = (totalDiff + newRearRightDifference + (newRearRightDifference << 2)) >> 3;
        _yAcceleration[Front] = (totalDiff + newFrontDifference + (newFrontDifference << 2)) >> 3;

        // randomly make the opponent do a wheelie (if they have that attribute)
        if ((s_attributes[OpponentId] & Wheelie) != 0)
        {
            // if the front of the car isn't moving much vertically
            int i = _ySpeed[Front] | _yAcceleration[Front];
            if ((i & 0xfffc) == 0 && (_randomSource.NextInt() & 0xf) == 0)
            {
                _ySpeed[Front] = 160; // make the opponent do a wheelie
            }
        }

        // update wheel y speeds and heights
        for (int wheel = 0; wheel < NumWheels; wheel++)
        {
            _ySpeed[wheel] += (_yAcceleration[wheel] * Reduction) >> 8;
            _actualHeight[wheel] += (_ySpeed[wheel] * Reduction) >> 9;
        }

        // limit movement of the opponent's wheels
        int diff = LimitWheels(296, RearLeft, RearRight);

        if (diff < 0)
        {
            // use rear right wheel (because this is higher than rear left)
            LimitWheels(368, RearRight, Front);
        }
        else
        {
            LimitWheels(368, RearLeft, Front);
        }
    }

    private void CalculateWheelDifference(
        int roadHeight,
        int actualHeight,
        int heightAdjust,
        ref int oldDifference,
        out int newDifferenceOut,
        ref int touchingRoad)
    {
        int newDifference = roadHeight - actualHeight;
        if (newDifference > _smallestDifference)
        {
            _smallestDifference = newDifference;
        }

        newDifference += heightAdjust;
        if (newDifference < -96)
        {
            newDifference = -96; // maximum amount above road
        }

        int amountBelowRoad = newDifference - oldDifference;
        amountBelowRoad = ((amountBelowRoad * Increase) >> 8) + newDifference;

        if (amountBelowRoad < 0)
        {
            amountBelowRoad = 0;
        }

        if (amountBelowRoad > 1023)
        {
            amountBelowRoad = 1023;
        }

        touchingRoad |= amountBelowRoad;

        amountBelowRoad -= heightAdjust;
        newDifferenceOut = amountBelowRoad;
        oldDifference = newDifference;
    }

    // Adjusts wheel heights and y speeds to limit the car's x and z angle,
    // especially important in the air on extreme tracks (e.g. Roller Coaster).
    private int LimitWheels(int maxDifference, int wheel1, int wheel2)
    {
        int diff = _actualHeight[wheel1] - _actualHeight[wheel2];

        int drop = maxDifference - Math.Abs(diff);
        if (drop < 0)
        {
            // drop highest wheel
            if (diff >= 0)
            {
                _actualHeight[wheel1] += drop;
            }
            else
            {
                _actualHeight[wheel2] += drop;
            }

            if (wheel2 != Front)
            {
                AverageWheelYSpeeds(RearLeft, RearRight);
                return diff;
            }

            AverageWheelYSpeeds(RearLeft, RearRight);
            AverageWheelYSpeeds(Front, RearRight);
            AverageWheelYSpeeds(RearLeft, RearRight);
        }

        if (wheel2 != Front)
        {
            return diff; // finish if first call
        }

        if (TouchingRoad)
        {
            return diff;
        }

        // adjust wheel y speeds when in the air, possibly to pitch forwards
        if (_ySpeed[wheel1] - _ySpeed[Front] < 16)
        {
            _ySpeed[RearLeft] += 4;
            _ySpeed[RearRight] += 4;
            _ySpeed[Front] -= 4;
        }

        return diff;
    }

    private void AverageWheelYSpeeds(int wheel1, int wheel2)
    {
        int average = (_ySpeed[wheel1] + _ySpeed[wheel2]) >> 1;
        _ySpeed[wheel1] = average;
        _ySpeed[wheel2] = average;
    }

    private void RandomizeSteering()
    {
        if (!TouchingRoad)
        {
            return;
        }

        int side = 0;
        _steeringShift[0] = 0;
        _steeringShift[1] = 0;
        _steeringAdjust = 0;

        int count = _steeringShift[2]; // random steering count
        if (count != 0)
        {
            _steeringShift[2]--;

            count += _steeringTableShift;
            count &= 0xf;
            int index = count;
            int shift = OpponentData.SteeringTable[index];
            if (shift < 0)
            {
                shift = -shift;
                side++;
            }

            _steeringShift[side] = shift;

            index += 5;
            index &= 0xf;
            _steeringAdjust = OpponentData.SteeringTable[index];
        }
        else if (!_player.OpponentBehindPlayer &&
            (_track.Pieces[CurrentPiece].Type & 0x80) == 0 &&
            (_lastPieceType & 0x80) != 0)
        {
            // just left a curved piece onto a straight
            _steeringTableShift = (_lastPieceType & 0x40) != 0 ? 16 : 8;

            int value = _randomSource.NextInt() & 0x1f;
            if (OpponentId >= value)
            {
                _steeringShift[2] = 16; // random steering count
            }
        }

        int type = _track.Pieces[CurrentPiece].OppositeDirection ? 0x40 : 0;
        _lastPieceType = type ^ _track.Pieces[CurrentPiece].Type;
    }

    private void GetEngineAcceleration()
    {
        int power = _enginePower;

        if (_steeringShift[2] != 0)
        {
            power -= 25; // steering randomly
        }

        _engineZAcceleration = TouchingRoad ? power : 0;
    }

    private void AdjustEngineAcceleration()
    {
        if (!TouchingRoad)
        {
            return;
        }

        int speedValue = SpeedValue(CurrentPiece);
        int speed = speedValue;
        if ((speed & 0x80) == 0 && speed > _maxSpeed)
        {
            speed = _maxSpeed;
        }

        int requiredZSpeed = speed & 0x7f;

        speed = _zSpeed >> 8;
        speed -= requiredZSpeed;
        if (speed == 0)
        {
            _requiredZSpeedReached = true;
            return;
        }

        if (speed > 0)
        {
            // speed is greater than required speed
            _requiredZSpeedReached = true;
            _engineZAcceleration = -_engineZAcceleration;
            if (speed < 14)
            {
                return;
            }
        }

        if ((speedValue & 0x80) != 0 || speed >= 0 || !_requiredZSpeedReached)
        {
            _engineZAcceleration <<= 1;
            return;
        }

        if (speed >= -2)
        {
            return;
        }

        _requiredZSpeedReached = false;
        _engineZAcceleration <<= 1;
    }

    private void UpdateZSpeed()
    {
        int accelerationAdjust = 0;

        if (_zSpeed >= 0)
        {
            int s = _zSpeed >> 7;

            // reduce speed value if the opponent is close behind the player
            if (_player.PlayerCloseToOpponent && _player.OpponentBehindPlayer)
            {
                s -= 20;
                if (s < 0)
                {
                    s = 0;
                }
            }

            // a fraction of the square of the speed is subtracted from
            // acceleration (this only has a small effect)
            accelerationAdjust = ((_zSpeed >> 8) * s) >> 6;

            // reduce the acceleration further if on the road
            if (TouchingRoad && _engineZAcceleration >= 0)
            {
                // subtract fraction of speed from acceleration
                s = _zSpeed >> 8;
                int adjusted = _engineZAcceleration - s;

                if ((_track.Pieces[CurrentPiece].Type & 0x80) != 0)
                {
                    // subtract again when on a curve, then reduce further
                    adjusted -= s;
                    adjusted -= 35;
                }

                _engineZAcceleration = adjusted;
            }
        }

        int a = _engineZAcceleration - accelerationAdjust;
        if (TouchingRoad)
        {
            a += CalculatePitchAcceleration();
        }

        _zSpeed += (a * Reduction) >> 8;
        if (_zSpeed < 0)
        {
            _zSpeed = 0;
        }
    }

    // Acceleration is reduced when pitched backwards, increased when pitched
    // forwards (i.e. the effect of gravity).
    private int CalculatePitchAcceleration()
    {
        int d = (_rearLeftRoadPos.Y + _rearRightRoadPos.Y) >> 1;
        d -= _frontRoadPosY;

        // d is negative when pitched backwards, positive when forwards
        int pitch = Math.Abs(d);
        if (pitch >= 512)
        {
            pitch = 510;
        }

        pitch >>= 1;
        int adjust = pitch + (pitch >> 2); // (5 * pitch) / 8

        return d < 0 ? -adjust : adjust;
    }

    private readonly record struct SurfaceCorners(
        int X1,
        int Y1,
        int Z1,
        int X2,
        int Y2,
        int Z2,
        int X3,
        int Y3,
        int Z3,
        int X4,
        int Y4,
        int Z4);
}
