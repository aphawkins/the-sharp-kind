// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Cars;

// Road location, surface search and interpolation (from Car Behaviour.cpp).
public sealed partial class CarPhysics
{
    // Use the track map to get the piece number that a world x/z point is within.
    // Surface position on a straight or diagonal straight.
    private static void CalcStraightSurfacePosition(
        int x,
        int z,
        int ox,
        int oz,
        int ux,
        int uz,
        int vx,
        int vz,
        out int sx,
        out int sz,
        out int roadX,
        bool wantRoadX)
    {
        roadX = 0;

        // z vector
        ux -= ox;
        uz -= oz;

        // x vector
        vx -= ox;
        vz -= oz;

        // left edge - calculate surface x position
        int v = ((x - ox) * uz) + ((oz - z) * ux);
        int denominator = (uz * vx) - (ux * vz);
        if (denominator == 0)
        {
            sx = 0;
        }
        else
        {
            sx = v * SurfaceSize / denominator;
            if (wantRoadX)
            {
                roadX = v * RoadWidth / denominator;
            }
        }

        if (sx >= SurfaceSize)
        {
            sx = SurfaceSize - 1;
        }

        if (sx < 0)
        {
            sx = 0;
        }

        // top edge - calculate surface z position
        int u = ((x - ox) * vz) + ((oz - z) * vx);
        denominator = (ux * vz) - (uz * vx);
        sz = denominator == 0 ? 0 : u * SurfaceSize / denominator;

        if (sz >= SurfaceSize)
        {
            sz = SurfaceSize - 1;
        }

        if (sz < 0)
        {
            sz = 0;
        }
    }

    private bool GetPieceUsingMap(int x, int z, ref int piece)
    {
        int mapX = x >> Track.CubeSizeLog2;
        int mapZ = z >> Track.CubeSizeLog2;

        if (mapX < 0 || mapX >= Track.TrackCubes || mapZ < 0 || mapZ >= Track.TrackCubes)
        {
            return false; // off the map
        }

        int mapPiece = _track.GetMapPiece(mapX, mapZ);
        if (mapPiece == -1)
        {
            return false; // no piece at this place on the map
        }

        piece = mapPiece;
        return true;
    }

    // Calculate position of world x/z point, relative to required piece.
    private void CalcXZRelativeToPiece(int x, int z, int piece, out int rx, out int rz)
    {
        int pieceX = _track.Pieces[piece].CubeX << Track.CubeSizeLog2;
        int pieceZ = _track.Pieces[piece].CubeZ << Track.CubeSizeLog2;

        rx = (x - pieceX) >> Track.LogPrecision;
        rz = (z - pieceZ) >> Track.LogPrecision;
    }

    // Calculate offsets from player's position (car's centre point).
    private void CalculateWheelXZOffsets()
    {
        // rear wheel is just (0, 0, -CAR_LENGTH/2) split into components
        _rearWheelXOffset = _trig.ZX * (-CarLength / 2) * Track.PcFactor;
        _rearWheelZOffset = _trig.ZZ * (-CarLength / 2) * Track.PcFactor;

        // front left wheel is just (-CAR_WIDTH/2, 0, CAR_LENGTH/2) split into components
        _frontLeftWheelXOffset = _trig.XX * (-CarWidth / 2) * Track.PcFactor;
        _frontLeftWheelXOffset += _trig.ZX * (CarLength / 2) * Track.PcFactor;
        _frontLeftWheelZOffset = _trig.XZ * (-CarWidth / 2) * Track.PcFactor;
        _frontLeftWheelZOffset += _trig.ZZ * (CarLength / 2) * Track.PcFactor;

        // front right wheel is just (CAR_WIDTH/2, 0, CAR_LENGTH/2) split into components
        _frontRightWheelXOffset = _trig.XX * (CarWidth / 2) * Track.PcFactor;
        _frontRightWheelXOffset += _trig.ZX * (CarLength / 2) * Track.PcFactor;
        _frontRightWheelZOffset = _trig.XZ * (CarWidth / 2) * Track.PcFactor;
        _frontRightWheelZOffset += _trig.ZZ * (CarLength / 2) * Track.PcFactor;
    }

    // Calculate the road height (y value) directly below each car wheel.
    private void CalculateRoadWheelHeights()
    {
        _atSideByte = 0;

        Span<(int X, int Z, int Y)> wheels =
        [
            (_frontLeftWheelXOffset + PlayerX, _frontLeftWheelZOffset + PlayerZ, _frontLeftRoadHeight),
            (_frontRightWheelXOffset + PlayerX, _frontRightWheelZOffset + PlayerZ, _frontRightRoadHeight),
            (_rearWheelXOffset + PlayerX, _rearWheelZOffset + PlayerZ, _rearRoadHeight),
        ];

        for (int i = 0; i < wheels.Length; i++)
        {
            CalculateWorldRoadHeight((WheelPosition)i, wheels[i].X, wheels[i].Z, out int height);

            // convert the result to PC StuntCarRacerSharp magnitude
            height = (height / Track.PcFactor) >> (Track.LogPrecision - 3);

            int wheelY = wheels[i].Y;
            CalculateRoadWheelHeight(height, ref wheelY);
            wheels[i].Y = wheelY;

            if ((WheelPosition)i == WheelPosition.Rear)
            {
                _playerDistanceOffRoad = Math.Abs(_distanceOffRoad);
            }
        }

        _frontLeftRoadHeight = wheels[0].Y;
        _frontRightRoadHeight = wheels[1].Y;
        _rearRoadHeight = wheels[2].Y;
    }

    private void CalculateRoadWheelHeight(int height, ref int heightOut)
    {
        if (_wheelOffRoad)
        {
            CalculateIfCarOffRoad(ref height);
        }

        _wheelOffRoad = false;

        // get angle in Amiga StuntCarRacerSharp format (i.e. correct sign)
        int angle = PlayerXAngle < AmigaTrig.Degrees180 ? PlayerXAngle : PlayerXAngle - AmigaTrig.Degrees360;

        // Use the height as-is when moving quickly or pitched steeply, otherwise average with the previous value (lowering onto the road).
        heightOut = Math.Abs(PlayerZSpeed) >= 0xA00 || Math.Abs(angle) >= 0x600
            ? height
            : (height + heightOut) / 2;
    }

    private void CalculateIfCarOffRoad(ref int height)
    {
        // calculate how far the current wheel is off the left or right of the road
        int x = Math.Abs(_distanceOffRoad);

        if (x > 3 * CarWidth / 4)
        {
            // signal whole car is off road
            height = OffRoadHeight;
            _atSideByte = (_atSideByte >> 1) | 0x80;
        }
        else
        {
            // use the amount the wheel is off the road to drop the height of the
            // wheel, making the car fall off the edge gradually
            height -= (x * 16) + 0x100;

            if (height < OffRoadHeight)
            {
                height = OffRoadHeight;
                _atSideByte = (_atSideByte >> 1) | 0x80;
            }
            else
            {
                // store which side the car is falling off
                int w = _distanceOffRoad >> 8;

                if ((w & 0x80) != 0)
                {
                    if (_offLeft)
                    {
                        WhichSideByte = 0x80;
                    }
                    else if (_offRight)
                    {
                        WhichSideByte = 0x40;
                    }
                }
            }
        }
    }

    private void CalculateWorldRoadHeight(WheelPosition wheel, int x, int z, out int heightOut)
    {
        // starts with the piece/surface that was used last time; this avoids
        // locating the wrong map square (e.g. diagonal pieces run into
        // adjacent squares)

        // handle the case when the point has been off the road and returned
        // to an entirely different area of the road
        int thisPiece = 0;
        if (!GetPieceUsingMap(x, z, ref thisPiece))
        {
            if (_surfaceFirstTime)
            {
                _surfacePiece = 0;
                _surfaceSegment = 0;
                GetSurfaceCoords(_surfacePiece, _surfaceSegment);
            }
        }
        else if ((_surfaceFirstTime || Math.Abs(thisPiece - _surfacePiece) > 1) &&
            !(thisPiece == _track.NumPieces - 1 && _surfacePiece == 0) &&
            !(thisPiece == 0 && _surfacePiece == _track.NumPieces - 1))
        {
            // moved by more than one piece, and the move is not from the
            // last to first piece, or vice versa
            _surfacePiece = thisPiece;
            _surfaceSegment = 0;
            GetSurfaceCoords(_surfacePiece, _surfaceSegment);
        }

        _surfaceFirstTime = false;

        // find the surface that the point is located within:
        // first check point is not before or after surface (z direction)
        int rx = 0;
        int rz = 0;

        AdvanceToSurface(x, z, ref rx, ref rz);
        RetreatToSurface(x, z, ref rx, ref rz);

        // now know that point is between start and end edge of surface;
        // find out if point is off left or right of surface
        if (wheel != WheelPosition.Centre)
        {
            CalculateWheelOffRoad(ref rx, ref rz);
        }

        // calculate height of surface at x,z position using linear interpolation
        int calculatedSegment = _surfaceSegment;
        int sx;
        int sz;

        if (wheel != WheelPosition.Centre)
        {
            CalcSurfacePosition(
                _surfacePiece,
                rx,
                rz,
                _sx2,
                _sz2,
                _sx1,
                _sz1,
                _sx3,
                _sz3,
                out sx,
                out sz,
                out _,
                false,
                ref calculatedSegment);

            if (wheel == WheelPosition.Rear)
            {
                // Reduce sx to (0 - 255)
                RearWheelSurfaceX = sx >> (LogSurfaceSize - 8);
            }
        }
        else
        {
            // called by CalculateRoadPosition: set current piece/segment,
            // distance into section and road x position
            CalcSurfacePosition(
                _surfacePiece,
                rx,
                rz,
                _sx2,
                _sz2,
                _sx1,
                _sz1,
                _sx3,
                _sz3,
                out sx,
                out sz,
                out int roadX,
                true,
                ref calculatedSegment);

            CurrentPiece = _surfacePiece;
            CurrentSegment = calculatedSegment;
            DistanceIntoSection = (calculatedSegment * 256) + (sz >> (LogSurfaceSize - 8));
            RoadXPosition = roadX;
        }

        // if the curve calculation output a different segment then the
        // coordinates must be retrieved for the calculated segment
        if (calculatedSegment != _surfaceSegment)
        {
            _surfaceSegment = calculatedSegment;
            GetSurfaceCoords(_surfacePiece, _surfaceSegment);
        }

        // first do x interpolation
        int sya = _sy1 + ((sx * (_sy4 - _sy1)) >> LogSurfaceSize);
        int syb = _sy2 + ((sx * (_sy3 - _sy2)) >> LogSurfaceSize);

        // now do z interpolation
        int y = (syb << LogSurfaceSize) + (sz * (sya - syb));

        heightOut = y << (Track.LogPrecision - LogSurfaceSize);
    }

    /// <summary>
    /// Work out whether the point lies off the left or right edge of the
    /// surface. If it does, rx/rz are moved onto that edge so the road height
    /// is taken at the edge, and the (negative) distance off is recorded.
    /// </summary>
    private void CalculateWheelOffRoad(ref int rx, ref int rz)
    {
        // calculate left dot product => off left
        int xs = _sx2 - _sx1;
        int zs = _sz2 - _sz1;
        int xp = rx - _sx1;
        int zp = rz - _sz1;
        _offLeft = xs * zp < xp * zs;

        // calculate right dot product => off right
        xs = _sx4 - _sx3;
        zs = _sz4 - _sz3;
        xp = rx - _sx3;
        zp = rz - _sz3;
        _offRight = xs * zp < xp * zs;

        _wheelOffRoad = false;
        _distanceOffRoad = 0;
        if (_offLeft || _offRight)
        {
            _wheelOffRoad = true;

            _distanceOffRoad = _offLeft
                ? CalcDistanceOffRoad(rx, rz, _sx2, _sz2, _sx1, _sz1, _sx3, _sz3, out int ex, out int ez)
                : CalcDistanceOffRoad(rx, rz, _sx3, _sz3, _sx4, _sz4, _sx2, _sz2, out ex, out ez);

            rx = ex;
            rz = ez;
        }
    }

    /// <summary>
    /// Steps forwards through segments while the point lies before the current surface.
    /// </summary>
    private void AdvanceToSurface(int x, int z, ref int rx, ref int rz)
    {
        int numPieceChanges = 0;
        bool moving = true;
        while (moving)
        {
            CalcXZRelativeToPiece(x, z, _surfacePiece, out rx, out rz);

            // calculate top dot product => before surface
            int xs = _sx1 - _sx4;
            int zs = _sz1 - _sz4;
            int xp = rx - _sx4;
            int zp = rz - _sz4;
            moving = xs * zp < xp * zs;

            if (moving)
            {
                // DIRECTION DEPENDANT - WHOLE SECTION
                if (_surfaceSegment < _track.Pieces[_surfacePiece].NumSegments - 1)
                {
                    _surfaceSegment++;
                }
                else
                {
                    _surfacePiece++;
                    if (_surfacePiece >= _track.NumPieces)
                    {
                        _surfacePiece = 0;
                    }

                    _surfaceSegment = 0;
                    numPieceChanges++;
                }

                GetSurfaceCoords(_surfacePiece, _surfaceSegment);
            }

            if (numPieceChanges >= _track.NumPieces)
            {
                break; // prevent an infinite loop
            }
        }
    }

    /// <summary>
    /// Steps backwards through segments while the point lies after the current surface.
    /// </summary>
    private void RetreatToSurface(int x, int z, ref int rx, ref int rz)
    {
        int numPieceChanges = 0;
        bool moving = true;
        while (moving)
        {
            CalcXZRelativeToPiece(x, z, _surfacePiece, out rx, out rz);

            // calculate bottom dot product => after surface
            int xs = _sx3 - _sx2;
            int zs = _sz3 - _sz2;
            int xp = rx - _sx2;
            int zp = rz - _sz2;
            moving = xs * zp < xp * zs;

            if (moving)
            {
                // DIRECTION DEPENDANT - WHOLE SECTION
                if (_surfaceSegment > 0)
                {
                    _surfaceSegment--;
                }
                else
                {
                    _surfacePiece--;
                    if (_surfacePiece < 0)
                    {
                        _surfacePiece = _track.NumPieces - 1;
                    }

                    _surfaceSegment = _track.Pieces[_surfacePiece].NumSegments - 1;
                    numPieceChanges++;
                }

                GetSurfaceCoords(_surfacePiece, _surfaceSegment);
            }

            if (numPieceChanges >= _track.NumPieces)
            {
                break; // prevent an infinite loop
            }
        }
    }

    private void GetSurfaceCoords(int piece, int segment)
    {
        IList<Coord3D> coords = _track.Pieces[piece].Coords;

        _sx2 = coords[segment * 4].X;
        _sy2 = coords[segment * 4].Y;
        _sz2 = coords[segment * 4].Z;

        _sx3 = coords[(segment * 4) + 1].X;
        _sy3 = coords[(segment * 4) + 1].Y;
        _sz3 = coords[(segment * 4) + 1].Z;

        segment++;
        _sx1 = coords[segment * 4].X;
        _sy1 = coords[segment * 4].Y;
        _sz1 = coords[segment * 4].Z;

        _sx4 = coords[(segment * 4) + 1].X;
        _sy4 = coords[(segment * 4) + 1].Y;
        _sz4 = coords[(segment * 4) + 1].Z;
    }

    private void CalcSurfacePosition(
        int piece,
        int x,
        int z,
        int ox,
        int oz,
        int ux,
        int uz,
        int vx,
        int vz,
        out int sx,
        out int sz,
        out int roadX,
        bool wantRoadX,
        ref int segmentOut)
    {
        if ((_track.Pieces[piece].Type & 0x80) != 0)
        {
            CalcCurveSurfacePosition(piece, x, z, out sx, out sz, out roadX, wantRoadX, ref segmentOut);
        }
        else
        {
            CalcStraightSurfacePosition(x, z, ox, oz, ux, uz, vx, vz, out sx, out sz, out roadX, wantRoadX);
        }
    }

    // Surface position on a curve, treated as a true circular arc (removes the
    // 'jitter' problem).
    private void CalcCurveSurfacePosition(
        int piece,
        int x,
        int z,
        out int sx,
        out int sz,
        out int roadX,
        bool wantRoadX,
        ref int segmentOut)
    {
        roadX = 0;

        CalcCurveMeasurements(piece, x, z, out int pieceYAngle, out int radius, out double distanceFromCentre);

        // adjust for normal direction of travel
        if (_track.Pieces[piece].OppositeDirection)
        {
            pieceYAngle = (Track.MaxAngle / 8) - pieceYAngle;
        }

        // limit piece y angle to valid range
        if (pieceYAngle < 0)
        {
            pieceYAngle = 0;
        }

        if (pieceYAngle >= Track.MaxAngle / 8)
        {
            pieceYAngle = (Track.MaxAngle / 8) - 1;
        }

        // calculate surface x position
        double d = distanceFromCentre < radius ? radius - distanceFromCentre : distanceFromCentre - radius;

        int surfaceX = (int)(d * SurfaceSize / (RoadWidth * Track.PcFactor));
        if (wantRoadX)
        {
            roadX = (int)(d / Track.PcFactor);
        }

        if (surfaceX >= SurfaceSize)
        {
            surfaceX = SurfaceSize - 1;
        }

        sx = surfaceX;

        // calculate surface z position and output calculated segment
        int numSegments = _track.Pieces[piece].NumSegments;
        int pieceZ = (pieceYAngle << LogSurfaceSize) * numSegments / (Track.MaxAngle / 8);

        sz = pieceZ & (SurfaceSize - 1);
        segmentOut = pieceZ >> LogSurfaceSize;
    }

    private void IdentifyPiece(int x, int z, ref int pieceInOut)
    {
        // find the piece that the point is located within;
        // defaults to the input piece if no piece could be found using the map
        int piece = pieceInOut;
        GetPieceUsingMap(x, z, ref piece);

        GetPieceCoords(piece);

        AdvanceToPiece(x, z, ref piece);
        RetreatToPiece(x, z, ref piece);

        pieceInOut = piece;
    }

    // 'before piece' loop: steps forwards while the point lies before the piece.
    private void AdvanceToPiece(int x, int z, ref int piece)
    {
        int numPieceChanges = 0;
        bool moving = true;
        while (moving)
        {
            CalcXZRelativeToPiece(x, z, piece, out int rx, out int rz);

            // calculate top dot product => before piece
            int xs = _px1 - _px4;
            int zs = _pz1 - _pz4;
            int xp = rx - _px4;
            int zp = rz - _pz4;
            moving = xs * zp < xp * zs;

            if (moving)
            {
                // DIRECTION DEPENDANT - go to next piece
                piece++;
                if (piece >= _track.NumPieces)
                {
                    piece = 0;
                }

                numPieceChanges++;
                GetPieceCoords(piece);
            }

            if (numPieceChanges >= _track.NumPieces)
            {
                break; // prevent an infinite loop
            }
        }
    }

    // 'after piece' loop: steps backwards while the point lies after the piece.
    private void RetreatToPiece(int x, int z, ref int piece)
    {
        int numPieceChanges = 0;
        bool moving = true;
        while (moving)
        {
            CalcXZRelativeToPiece(x, z, piece, out int rx, out int rz);

            // calculate bottom dot product => after piece
            int xs = _px3 - _px2;
            int zs = _pz3 - _pz2;
            int xp = rx - _px2;
            int zp = rz - _pz2;
            moving = xs * zp < xp * zs;

            if (moving)
            {
                // DIRECTION DEPENDANT - go to previous piece
                piece--;
                if (piece < 0)
                {
                    piece = _track.NumPieces - 1;
                }

                numPieceChanges++;
                GetPieceCoords(piece);
            }

            if (numPieceChanges >= _track.NumPieces)
            {
                break; // prevent an infinite loop
            }
        }
    }

    private void GetPieceCoords(int piece)
    {
        IList<Coord3D> coords = _track.Pieces[piece].Coords;
        int numSegments = _track.Pieces[piece].NumSegments;

        _px2 = coords[0].X;
        _pz2 = coords[0].Z;

        _px3 = coords[1].X;
        _pz3 = coords[1].Z;

        _px1 = coords[numSegments * 4].X;
        _pz1 = coords[numSegments * 4].Z;

        _px4 = coords[(numSegments * 4) + 1].X;
        _pz4 = coords[(numSegments * 4) + 1].Z;
    }

    // Calculate the piece y angle at the x/z point (e.g. centre of car).
    // Assumes the provided x/z point is within and relative to the piece.
    private int CalcSectionYAngle(int piece, int x, int z)
    {
        TrackPiece trackPiece = _track.Pieces[piece];
        int sectionYAngle;

        if (trackPiece.Type == 0x00)
        {
            // straight
            sectionYAngle = -trackPiece.RoughPieceAngle;
            return sectionYAngle & (Track.MaxAngle - 1);
        }

        if (trackPiece.Type == 0x40)
        {
            // diagonal straight - always 45 degrees on
            sectionYAngle = -(trackPiece.RoughPieceAngle + (Track.MaxAngle / 8));
            return sectionYAngle & (Track.MaxAngle - 1);
        }

        // must be a curve
        CalcCurveMeasurements(piece, x, z, out sectionYAngle, out _, out _);

        // change sign if right hand curve (default calculation is for left hand)
        if (!trackPiece.CurveToLeft)
        {
            sectionYAngle = -sectionYAngle;
        }

        // add on rough piece angle to get final section y angle
        sectionYAngle -= trackPiece.RoughPieceAngle;

        // adjust for normal direction of travel
        if (trackPiece.OppositeDirection)
        {
            sectionYAngle += Track.MaxAngle / 2; // plus 180 degrees
        }

        return sectionYAngle & (Track.MaxAngle - 1);
    }

    // Y angle within the piece, edge radius, and distance from the piece's circle centre. Assumes x/z relative to the piece.
    private void CalcCurveMeasurements(int piece, int x, int z, out int curveAngle, out int radius, out double distanceFromCentre)
    {
        curveAngle = 0;
        radius = 0;
        distanceFromCentre = 0;

        IList<Coord3D> coords = _track.Pieces[piece].Coords;
        int numSegments = _track.Pieces[piece].NumSegments;

        // get first and last coordinates from inner or outer edge
        const int first = 0;
        int last = numSegments * 4;

        int xf = coords[first].X;
        int zf = coords[first].Z;
        int xl = coords[last].X;
        int zl = coords[last].Z;

        // assumes all curved pieces are 45 degree circular arcs
        radius = Math.Abs(xl - xf) + Math.Abs(zl - zf);

        // a horizontal or vertical edge is needed to calculate the circle centre so
        // check first edge is horizontal/vertical, if not then use last edge
        int xo = coords[first + 1].X;
        int zo = coords[first + 1].Z;
        if (xo != xf && zo != zf)
        {
            // use last edge (variable names now have the opposite meaning)
            xf = coords[last].X;
            zf = coords[last].Z;
            xl = coords[first].X;
            zl = coords[first].Z;

            xo = coords[last + 1].X;
            zo = coords[last + 1].Z;
        }

        // check resulting edge is horizontal/vertical
        if (xo != xf && zo != zf)
        {
            return; // piece has no horizontal or vertical edge
        }

        // calculate coordinate of circle centre, using first coordinate
        // from the other edge for comparison
        int o;
        int a;
        if (xo != xf)
        {
            // piece edge is horizontal
            int xc = xf < xl ? xf + radius : xf - radius;
            int zc = zf;

            o = z - zc;
            a = x - xc;
        }
        else if (zo != zf)
        {
            // piece edge is vertical
            int xc = xf;
            int zc = zf < zl ? zf + radius : zf - radius;

            o = x - xc;
            a = z - zc;
        }
        else
        {
            return; // piece edge is invalid (both ends are same)
        }

        // use inverse tan to calculate basic angle in radians
        // (90 degrees when 'a' is zero, preventing division by zero)
        double radians = a == 0 ? Math.PI / 2 : Math.Atan((double)o / a);

        // convert radians to internal angle (also round up)
        double angle = radians * Track.MaxAngle / (2 * Math.PI);
        curveAngle = angle > 0 ? (int)(angle + 0.5) : (int)(0.5 - angle);

        distanceFromCentre = Math.Sqrt(((double)o * o) + ((double)a * a));
    }

    // Position car above middle of piece, facing correct direction.
    private void PositionCarAbovePiece(int piece)
    {
        // find a section that the car can be lowered onto
        while (true)
        {
            int template = _track.Pieces[piece].AngleAndTemplate & 0xf;
            if ((Track.SectionCarCanBePutOn(template) & 0x80) != 0)
            {
                // go to previous piece
                piece--;
                if (piece < 0)
                {
                    piece = _track.NumPieces - 1;
                }
            }
            else
            {
                break;
            }
        }

        TrackPiece trackPiece = _track.Pieces[piece];

        // calculate x/z position of piece's front left corner, within world
        int pieceX = trackPiece.CubeX << Track.CubeSizeLog2;
        int pieceZ = trackPiece.CubeZ << Track.CubeSizeLog2;

        // set car x/z position to middle of piece
        PlayerX = pieceX + (Track.CubeSize / 2);
        PlayerZ = pieceZ + (Track.CubeSize / 2);

        // set car y position above the road height at that point
        CalculateWorldRoadHeight(WheelPosition.FrontLeft, PlayerX, PlayerZ, out int height);
        height = (height / Track.PcFactor) >> (Track.LogPrecision - 3);
        PlayerY = (height + 0xc00) * 256;

        // clear car x/z angle
        PlayerXAngle = 0;
        PlayerZAngle = 0;

        // set car y angle
        PlayerYAngle = trackPiece.RoughPieceAngle;

        if (trackPiece.OppositeDirection)
        {
            PlayerYAngle += Track.MaxAngle / 2; // plus 180 degrees
        }

        // diagonal straights are always 45 degrees on
        if (trackPiece.Type == 0x40)
        {
            PlayerYAngle += Track.MaxAngle / 8;
        }

        PlayerYAngle &= Track.MaxAngle - 1;

        // player to side of road: shift player by (x = 160, z = 0) rotated
        // about the y axis
        int sinY = AmigaTrig.Sin(PlayerYAngle);
        int cosY = AmigaTrig.Cos(PlayerYAngle);
        PlayerX += 160 * cosY;
        PlayerZ -= 160 * sinY;

        CurrentPiece = piece;
        CurrentSegment = 0;
    }
}
