// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Cars;

// Opponent interaction with the player: distances, obstruction, pushing
// and car-to-car collision (from Opponent Behaviour.cpp).
public sealed partial class OpponentPhysics
{
    // Adjust a per-piece required speed value (used by the draw bridge animation).
    internal void SetSpeedValue(int piece, byte value) => _speedValueOverrides[piece] = value;

    // Original Opponent_Speed_Value: re-rolled only when the piece changes. Draw bridge pieces are overridden by the bridge animation.
    internal int SpeedValue(int piece)
    {
        if (_speedValueOverrides[piece] >= 0)
        {
            return _speedValueOverrides[piece];
        }

        if (piece != _speedValuePiece)
        {
            int trackIndex = (int)_track.Id;
            int value = _randomSource.NextInt() & OpponentData.TrackSpeedValues[trackIndex + 16];
            value += OpponentData.TrackSpeedValues[trackIndex + 24];

            // the reference's own can-be-put-on test never fires (a signedness
            // slip) - the Amiga's bpl tests bit 7, as MoveToMiddleOnCurves does
            int template = _track.Pieces[piece].AngleAndTemplate & 0xf;
            if ((Track.SectionCarCanBePutOn(template) & 0x80) == 0 && value < 0x7f - 10)
            {
                value += 10;
            }

            _speedValuePiece = piece;
            _speedValue = value;
        }

        return _speedValue;
    }

    private void CalculateDistancesBetweenPlayers()
    {
        if (_distancesAroundRoad == null)
        {
            _distancesAroundRoad = new int[_track.NumPieces];
            int d = 0;
            for (int piece = 0; piece < _track.NumPieces; piece++)
            {
                _distancesAroundRoad[piece] = d << 5;
                d += _track.Pieces[piece].NumSegments;
            }

            _totalRoadDistance = d << 5;
        }

        int diff = (_distanceIntoSection - _player.DistanceIntoSection) >> 3;
        diff += _distancesAroundRoad[CurrentPiece] - _distancesAroundRoad[_player.CurrentPiece];

        // note: only reliable when player and opponent are on the same piece
        _differenceBetweenPlayers = diff;

        int absDiff = Math.Abs(diff);
        int opposite = _totalRoadDistance - absDiff;

        if (absDiff < opposite)
        {
            _smallestDistanceBetweenPlayers = absDiff;
            diff = -diff;
        }
        else
        {
            _smallestDistanceBetweenPlayers = opposite;
        }

        _player.OpponentBehindPlayer = diff > 0;
    }

    private void PlayerInteraction()
    {
        _player.PlayerCloseToOpponent = false;

        int suggested = _roadXPosition & 0xff;
        _suggestedRoadXPosition = suggested;

        int d = suggested - _player.RearWheelSurfaceX;
        bool playerToRight = false;
        if (d < 0)
        {
            d = -d;
            playerToRight = true;
        }

        _xDifference = d;
        _playerToRight = playerToRight;

        bool farAway = false;
        bool collided = false;

        if ((_smallestDistanceBetweenPlayers >> 8) != 0)
        {
            farAway = true;
        }
        else
        {
            HandleNearbyPlayer(ref farAway, ref collided);
        }

        if (collided)
        {
            HandleCollided(ref farAway);
        }

        if (farAway)
        {
            DriveAtOwnSideOfRoad();
        }

        ApplySteering();
    }

    // The player and opponent are within 0x100 of each other.
    private void HandleNearbyPlayer(ref bool farAway, ref bool collided)
    {
        int distance = _smallestDistanceBetweenPlayers;
        if (distance < 64 && (_player.OpponentBehindPlayer || _xDifference < 50))
        {
            // opponent less than 64 behind player, or player less than 64
            // behind and less than 50 to the left or right of opponent
            _player.PlayerCloseToOpponent = true;
        }

        if (distance < 16 && _xDifference < 50 && PlayerNearRoad())
        {
            CarToCarCollisionDetection();
            collided = true;
            return;
        }

        _collisionCountdown = 0; // clear collision values
        _collidedFlag = 0;

        if ((_smallestDistanceBetweenPlayers & 0xff) >= 24)
        {
            ResolveObstruction(ref farAway);
        }
        else
        {
            collided = true;
        }
    }

    // An opponent that drives near the edge holds its line rather than moving
    // aside for the player.
    private void HandleCollided(ref bool farAway)
    {
        if ((s_attributes[OpponentId] & DrivesNearEdge) != 0 &&
            !_player.OpponentBehindPlayer &&
            (_smallestDistanceBetweenPlayers & 0xff) >= 14)
        {
            farAway = true;
            return;
        }

        MoveToOneSide();
        farAway = false;
    }

    // Nothing nearby, so head for this opponent's usual side of the road.
    private void DriveAtOwnSideOfRoad()
    {
        int side = (s_attributes[OpponentId] & DrivesNearEdge) != 0 ? 110 : 64;

        if ((OpponentId & 1) != 0)
        {
            side = 255 - side; // to other side of road
        }

        _suggestedRoadXPosition = side;
        MoveToMiddleOnCurves();
    }

    private bool PlayerNearRoad()
    {
        int d = _player.RoadXPosition >> 8;
        return d < 1 || (d == 1 && (_player.RoadXPosition & 0xff) < 0x80);
    }

    private void ResolveObstruction(ref bool farAway)
    {
        if (_player.OpponentBehindPlayer)
        {
            PushPlayerOff();
            return;
        }

        int d = _smallestDistanceBetweenPlayers & 0xff;
        if (d < 50)
        {
            if ((s_attributes[OpponentId] & ObstructsPlayer) != 0)
            {
                // put opponent at same position as player
                _suggestedRoadXPosition = _player.RearWheelSurfaceX;
                MoveToMiddleOnCurves();
                return;
            }

            PushPlayerOff();
            MoveToMiddleOnCurves();
            return;
        }

        if (d >= 200 || (s_attributes[OpponentId] & PushPlayer) == 0)
        {
            farAway = true;
            return;
        }

        PushPlayerOff();
        MoveToMiddleOnCurves();
    }

    // Move opponent to the middle of the road if approaching or on a piece
    // the car cannot be put on (e.g. curves).
    private void MoveToMiddleOnCurves()
    {
        int piece = CurrentPiece;
        for (int count = 2; count > 0; count--)
        {
            int template = _track.Pieces[piece].AngleAndTemplate & 0xf;
            if ((Track.SectionCarCanBePutOn(template) & 0x80) != 0)
            {
                _suggestedRoadXPosition = 128; // middle of road
            }

            piece++;
            if (piece >= _track.NumPieces)
            {
                piece = 0;
            }
        }
    }

    private void ApplySteering()
    {
        int d = _steeringAdjust;
        int move;

        if (d == 0)
        {
            d = _suggestedRoadXPosition - (_roadXPosition & 0xff);
            if (d == 0)
            {
                return;
            }
        }

        if (d < 0)
        {
            if (d >= -16)
            {
                return;
            }

            move = -9;
        }
        else
        {
            if (d < 16)
            {
                return;
            }

            move = 9;
        }

        move += _roadXPosition & 0xff;

        if (!TouchingRoad)
        {
            return;
        }

        if (move is >= 225 or < 32)
        {
            return;
        }

        _roadXPosition = move;
    }

    // Position opponent on left or right of the player.
    private void MoveToOneSide()
    {
        if (_xDifference >= 56)
        {
            return;
        }

        _suggestedRoadXPosition = _playerToRight ? 32 : 256 - 32;
    }

    // Opponent pushing player off track.
    private void PushPlayerOff()
    {
        if (_xDifference >= 56)
        {
            return;
        }

        int d = _player.RearWheelSurfaceX;

        _suggestedRoadXPosition = _playerToRight
            ? (d < 96 ? 256 - 32 : 32)
            : (d >= 256 - 96 ? 32 : 256 - 32);
    }

    // Calculates opponent collision with player (original CarToCarCollisionDetection).
    private void CarToCarCollisionDetection()
    {
        if (!_player.DropStartDone)
        {
            return;
        }

        if ((!TouchingRoad || !_player.TouchingRoad) && !CalculateAirborneCollisionY())
        {
            return;
        }

        if (_xDifference < 45 && (_smallestDistanceBetweenPlayers & 0xff) <= 8)
        {
            _carToCarXAcceleration = _playerToRight ? 0x800 : -0x800;
        }

        if ((_collidedFlag & 0x80) == 0)
        {
            int sign = 3;
            int d = _zSpeed - _player.PlayerZSpeed;
            if (d < 0)
            {
                sign = -3;
            }

            d >>= 1;
            d += sign;
            _carToCarZAcceleration = d;
        }

        _carsCollided = true;
        _collidedFlag = 0x80;

        // apply collision damage to the player
        int damage = 512;
        damage += Math.Abs(_carToCarXAcceleration);
        damage += Math.Abs(_carToCarYAcceleration);
        damage += Math.Abs(_carToCarZAcceleration);
        damage >>= 8;

        _player.AddCollisionDamage(damage);
    }

    // Returns false when the cars are too far apart vertically to have collided at all.
    private bool CalculateAirborneCollisionY()
    {
        int playersSmallerY = _player.PlayerY >> 11;
        int d = playersSmallerY - _actualHeight[RearLeft];
        int signed = d;
        d += 40;
        d = Math.Abs(d);

        if (d >= 192)
        {
            _collisionCountdown = 3;
            return false;
        }

        if (_collisionCountdown != 0)
        {
            --_collisionCountdown;
            int accel = 256 - d;
            if (signed < 0)
            {
                accel = -accel;
            }

            _carToCarYAcceleration = accel << 4;
        }

        return true;
    }

    // Original CarToCarCollision, called from the player's collision detection.
    private void TransferCollisionToPlayer()
    {
        if (_carsCollidedDelay > 0)
        {
            --_carsCollidedDelay;
        }

        if (!_carsCollided)
        {
            return;
        }

        _carsCollided = false;

        int speed = _zSpeed - _carToCarZAcceleration;
        if (speed < 0)
        {
            speed = 0;
        }

        _zSpeed = speed;

        int d = _carToCarYAcceleration >> 4;
        _ySpeed[RearLeft] -= d;
        _ySpeed[RearRight] -= d;
        _ySpeed[Front] -= d;

        _player.AddCollisionAcceleration(_carToCarXAcceleration, _carToCarYAcceleration, _carToCarZAcceleration);

        _carToCarXAcceleration = 0;
        _carToCarYAcceleration = 0;
        _carToCarZAcceleration = 0;

        // hit-car sound, rate limited as the original
        if (_carsCollidedDelay == 0)
        {
            HitCarSoundTriggered = true;
            _carsCollidedDelay = 5;
        }
    }
}
