// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Cars;

// Movement: speeds, accelerations, collision, steering and rotation
// (from Car Behaviour.cpp).
public sealed partial class CarPhysics
{
    // Original WorldOffset. Local axes are the physics ones: x right, y up, z forward.
    public Coord3D RotateToWorld(int x, int y, int z)
    {
        _trig.CalculateYXZ(PlayerXAngle, PlayerYAngle, PlayerZAngle);

        return new(
            ((x * _trig.XX) + (y * _trig.YX) + (z * _trig.ZX)) / AmigaTrig.Precision,
            ((x * _trig.XY) + (y * _trig.YY) + (z * _trig.ZY)) / AmigaTrig.Precision,
            ((x * _trig.XZ) + (y * _trig.YZ) + (z * _trig.ZZ)) / AmigaTrig.Precision);
    }

    // Original LimitViewpointY: prevents seeing under the track when the car bottoms out, by
    // holding a too-low front wheel at the threshold. Only the rendered viewpoint uses the result.
    public int LimitViewpointY()
    {
        int savedPlayerZSpeed = PlayerZSpeed;

        _trig.CalculateYXZ(PlayerXAngle, PlayerYAngle, PlayerZAngle);

        // prevent CalculateRoadWheelHeight from averaging current and
        // previous heights
        PlayerZSpeed = 0xA00;

        CalculateWheelXZOffsets();
        CalculateRoadWheelHeights();
        CalculateActualWheelHeights();

        PlayerZSpeed = savedPlayerZSpeed;

        int sinX = AmigaTrig.Sin(PlayerXAngle);
        int sinZ = AmigaTrig.Sin(PlayerZAngle);

        int rightY = 0;
        if (_frontRightRoadHeight - _frontRightActualHeight > YAdjustmentThreshold)
        {
            rightY = (_frontRightRoadHeight - YAdjustmentThreshold) << 8;
            rightY += sinZ << (3 + 15 - Track.LogPrecision);
            rightY -= sinX << (4 + 15 - Track.LogPrecision);
        }

        int leftY = 0;
        if (_frontLeftRoadHeight - _frontLeftActualHeight > YAdjustmentThreshold)
        {
            leftY = (_frontLeftRoadHeight - YAdjustmentThreshold) << 8;
            leftY -= sinZ << (3 + 15 - Track.LogPrecision);
            leftY -= sinX << (4 + 15 - Track.LogPrecision);
        }

        if (rightY != 0)
        {
            // use the average when both wheels need adjusting
            return leftY != 0 ? (rightY + leftY) / 2 : rightY;
        }

        return leftY != 0 ? leftY : PlayerY;
    }

    // Clamp a body angle to +/- limit, stopping rotation pushed past it. Angles in PC format (0-360 degrees).
    private static int ClampBodyAngle(int bodyAngle, int limit, ref int rotationSpeed)
    {
        // get angle in Amiga StuntCarRacerSharp format (i.e. correct sign)
        int angle = bodyAngle < AmigaTrig.Degrees180 ? bodyAngle : bodyAngle - AmigaTrig.Degrees360;

        if (Math.Abs(angle) <= limit)
        {
            return bodyAngle;
        }

        angle = angle >= 0 ? limit : -limit;

        if ((rotationSpeed >= 0 && angle < 0) || (rotationSpeed < 0 && angle >= 0))
        {
            rotationSpeed = 0; // values have different signs
        }

        return angle > 0 ? angle : angle + AmigaTrig.Degrees360;
    }

    // Calculate the height (y value) of each car wheel.
    private void CalculateActualWheelHeights()
    {
        int sinX = AmigaTrig.Sin(PlayerXAngle);
        int sinZ = AmigaTrig.Sin(PlayerZAngle);

        _rearActualHeight = PlayerY;
        _rearActualHeight -= sinX << (4 + 15 - Track.LogPrecision);
        _rearActualHeight >>= 8;

        _frontRightActualHeight = PlayerY;
        _frontRightActualHeight += sinX << (4 + 15 - Track.LogPrecision);
        _frontRightActualHeight -= sinZ << (3 + 15 - Track.LogPrecision);
        _frontRightActualHeight >>= 8;

        _frontLeftActualHeight = PlayerY;
        _frontLeftActualHeight += sinX << (4 + 15 - Track.LogPrecision);
        _frontLeftActualHeight += sinZ << (3 + 15 - Track.LogPrecision);
        _frontLeftActualHeight >>= 8;
    }

    // Original SetOneWheelRotationSpeed (physics rate): faster when touching the road, decaying otherwise.
    private void SetWheelRotationSpeed()
    {
        _frontLeftWheelSpeed = CalculateWheelRotationSpeed(_frontLeftAmountBelowRoad, _frontLeftWheelSpeed);
        _frontRightWheelSpeed = CalculateWheelRotationSpeed(_frontRightAmountBelowRoad, _frontRightWheelSpeed);
    }

    // Original FramesWheelsEngine, called every 50Hz tick so the wheels spin at full speed. Right
    // wheel accumulates from the left's already-updated angle, matching the original's literal source.
    private void AdvanceWheelAngles()
    {
        LeftWheelAngle = (LeftWheelAngle + _frontLeftWheelSpeed) & WheelAngleMask;
        RightWheelAngle = (LeftWheelAngle + _frontRightWheelSpeed) & WheelAngleMask;
    }

    private int CalculateWheelRotationSpeed(int wheelAmountBelowRoad, int wheelRotationSpeed)
    {
        if (wheelAmountBelowRoad == 0)
        {
            // not touching the road: decay by a quarter
            return wheelRotationSpeed - (wheelRotationSpeed / 4);
        }

        int speed = Math.Abs(PlayerZSpeed);
        if (speed < WheelSpeedLowThreshold)
        {
            return speed * 8;
        }

        int result = (speed * 2) + WheelSpeedHighOffset;
        return result > WheelSpeedMax ? WheelSpeedMaxClamped : result;
    }

    // Calculate player's actual X/Z speeds by rotating world speed values.
    private void CalculateXZSpeeds()
    {
        _playerXSpeed = (_playerWorldXSpeed * _trig.XX) >> Track.LogPrecision;
        _playerXSpeed += (_playerWorldYSpeed * _trig.XY) >> Track.LogPrecision;
        _playerXSpeed += (_playerWorldZSpeed * _trig.XZ) >> Track.LogPrecision;

        _playerYSpeed = 0; // zero for current implementation

        PlayerZSpeed = (_playerWorldXSpeed * _trig.ZX) >> Track.LogPrecision;
        PlayerZSpeed += (_playerWorldYSpeed * _trig.ZY) >> Track.LogPrecision;
        PlayerZSpeed += (_playerWorldZSpeed * _trig.ZZ) >> Track.LogPrecision;
    }

    // Gravity acts on the Y axis only, therefore only Y components are used.
    private void CalculateGravityAcceleration()
    {
        _gravityXAcceleration = (-GravityAcceleration * _trig.XY) >> Track.LogPrecision;
        _gravityYAcceleration = (-GravityAcceleration * _trig.YY) >> Track.LogPrecision;
        _gravityZAcceleration = (-GravityAcceleration * _trig.ZY) >> Track.LogPrecision;
    }

    // Calculate car acceleration caused by collision with the road.
    private void CarCollisionDetection()
    {
        _groundedCount = 0;
        _damageValue = 0;
        Damaged = false;

        int frontLeftDamage = FrontLeftDamage;
        CalculateWheelCollision(
            _frontLeftRoadHeight,
            _frontLeftActualHeight,
            out _frontLeftHeightDifference,
            ref _oldFrontLeftDifference,
            ref _frontLeftAmountBelowRoad,
            ref frontLeftDamage);
        FrontLeftDamage = frontLeftDamage;

        int frontRightDamage = FrontRightDamage;
        CalculateWheelCollision(
            _frontRightRoadHeight,
            _frontRightActualHeight,
            out _frontRightHeightDifference,
            ref _oldFrontRightDifference,
            ref _frontRightAmountBelowRoad,
            ref frontRightDamage);
        FrontRightDamage = frontRightDamage;

        int rearDamage = RearDamage;
        CalculateWheelCollision(
            _rearRoadHeight,
            _rearActualHeight,
            out _rearHeightDifference,
            ref _oldRearDifference,
            ref _rearAmountBelowRoad,
            ref rearDamage);
        RearDamage = rearDamage;

        int averageFrontAmountBelowRoad = (_frontLeftAmountBelowRoad + _frontRightAmountBelowRoad) >> 1;
        int averageAmountBelowRoad = (averageFrontAmountBelowRoad + _rearAmountBelowRoad) >> 1;

        CalculateCarCollisionAcceleration(averageAmountBelowRoad);

        int difference = (_frontLeftAmountBelowRoad - _frontRightAmountBelowRoad) * 3;
        if (difference > 0x1000)
        {
            difference = 0x1000;
        }

        if (difference < -0x1000)
        {
            difference = -0x1000;
        }

        _frontDifferenceBelowRoad = difference;

        difference = averageFrontAmountBelowRoad - _rearAmountBelowRoad;
        _overallDifferenceBelowRoad = difference;

        TouchingRoad = averageAmountBelowRoad != 0;

        ApplyAirborneAttitude();

        _carToRoadCollisionZAcceleration = _carCollisionZAcceleration;

        CarToCarCollision?.Invoke();

        // Grounded sound, volume scaled by damage (see CalculateDamageVolume).
        if (_groundedDelay > 0)
        {
            --_groundedDelay;
        }

        if (_groundedCount != 0 && _groundedDelay == 0)
        {
            GroundedSoundTriggered = true;
            GroundedVolume = CalculateDamageVolume();
            _groundedDelay = 5;
        }
    }

    // While airborne, nose the car back down towards the road. The ski jump and
    // roller coaster are the only tracks where this also applies nose-down.
    private void ApplyAirborneAttitude()
    {
        if (TouchingRoad || OnChains)
        {
            return;
        }

        // get angle in Amiga StuntCarRacerSharp format (i.e. correct sign)
        int angle = PlayerXAngle < AmigaTrig.Degrees180 ? PlayerXAngle : PlayerXAngle - AmigaTrig.Degrees360;

        if (angle < 0 && _track.Id is not TrackId.RollerCoaster and not TrackId.SkiJump)
        {
            return;
        }

        int difference = -128;

        if (angle < 0 && _track.Id == TrackId.SkiJump)
        {
            difference = -8;
        }

        if (angle >= 0x1000)
        {
            difference = -256;
        }

        difference -= _overallDifferenceBelowRoad;
        if (difference < 0 && _playerXRotationSpeed >= -256)
        {
            _overallDifferenceBelowRoad = difference;
        }
    }

    private void CalculateWheelCollision(
        int roadHeight,
        int actualHeight,
        out int heightDifference,
        ref int oldDifference,
        ref int amountBelowRoadInOut,
        ref int damageInOut)
    {
        heightDifference = roadHeight - actualHeight - _wreckWheelHeightReduction;

        int newDifference = heightDifference;
        if (newDifference > 0x1400)
        {
            newDifference = 0x1400;
        }
        else if (newDifference < -0x300)
        {
            newDifference = -0x300;
        }

        int amountBelowRoad = newDifference - oldDifference;
        amountBelowRoad = ((amountBelowRoad * Increase) >> 8) + newDifference;

        if (amountBelowRoad >= 0)
        {
            int oldAmountBelowRoad = amountBelowRoadInOut;
            amountBelowRoadInOut = amountBelowRoad;

            if (amountBelowRoad >= 0x400 && oldAmountBelowRoad < 0x200)
            {
                _groundedCount++; // wheel grounded
            }

            int damage = amountBelowRoadInOut - (RoadCushionValue * 256);
            if (damage >= 0x700)
            {
                ApplyWheelDamage(damage, ref amountBelowRoadInOut, ref damageInOut);
            }
            else
            {
                _damagedCount = 0;
            }
        }
        else
        {
            amountBelowRoadInOut = 0;
            _damagedCount = 0;
        }

        oldDifference = newDifference;
    }

    // A wheel has hit the road hard enough to hurt: record the worst impact of
    // the frame and add to that wheel's damage.
    private void ApplyWheelDamage(int damage, ref int amountBelowRoadInOut, ref int damageInOut)
    {
        if (damage > _damageValue)
        {
            _damageValue = damage;
        }

        damage -= 0x600;

        // The original also checks fourteen_frames_elapsed here, but it
        // is never set (always 0), so the check is dropped.
        _damagedCount++;
        if (_damagedCount < DamagedLimit)
        {
            damage /= 256;
            damage &= 0xff;
            damage += damage / 2;
            damage += damageInOut;
            if (damage > 0xff)
            {
                damage = 0xff;
            }

            damageInOut = damage;
            Damaged = true;
        }

        if (amountBelowRoadInOut >= 0x1200)
        {
            amountBelowRoadInOut = 0x11ff;
        }
    }

    private void CalculateCarCollisionAcceleration(int averageAmountBelowRoad)
    {
        // averageAmountBelowRoad is the force exerted by the road on the car.
        // Force is directed through the Y axis of the road surface.
        const int logCarLengthFactor = 4;
        const int logCarWidthFactor = 3; // length is twice the width

        // y inclination to road is zero because road exists in X and Z planes only
        int frontHeightDifference = (_frontLeftHeightDifference + _frontRightHeightDifference) >> 1;
        int roadInclinationX = (frontHeightDifference - _rearHeightDifference) >> logCarLengthFactor;

        CalculateInclinationSinCos(roadInclinationX, out int surfaceSinX, out int surfaceCosX);

        int roadInclinationZ = (_frontLeftHeightDifference - _frontRightHeightDifference) >> logCarWidthFactor;

        CalculateInclinationSinCos(roadInclinationZ, out int surfaceSinZ, out int surfaceCosZ);

        int surfaceCosXCosZ = (surfaceCosX * surfaceCosZ) >> 8;
        int surfaceCosXSinZ = (surfaceCosX * surfaceSinZ) >> 8;

        // X acceleration = force * -cosx.sinz
        int surfaceValue = roadInclinationZ < 0 ? -surfaceCosXSinZ : surfaceCosXSinZ;
        _carCollisionXAcceleration = (averageAmountBelowRoad * surfaceValue) >> 8;

        // Y acceleration = force * cosx.cosz (y inclination never negative at the moment)
        _carCollisionYAcceleration = (averageAmountBelowRoad * surfaceCosXCosZ) >> 8;

        // Z acceleration = force * sinx
        surfaceValue = roadInclinationX < 0 ? surfaceSinX : -surfaceSinX;
        _carCollisionZAcceleration = (averageAmountBelowRoad * surfaceValue) >> 8;
    }

    private void CalculateTotalAcceleration()
    {
        _playerYAcceleration = _gravityYAcceleration + _carCollisionYAcceleration;

        // reduce engine z acceleration if car is accelerating and not travelling
        // backwards - simulates wind resistance / reduced ability to accelerate
        int reduction = ((_engineZAcceleration >> 8) | (PlayerZSpeed >> 8)) & 0xff;
        if ((reduction & 0x80) != 0x80 && (_engineZAcceleration & 0xff) != 0)
        {
            _engineZAcceleration -= reduction;
        }

        // limit engine z acceleration to (2 * car collision y acceleration) -
        // possibly prevents accelerating without enough grip
        int twiceY = GetTwiceCollisionYAcceleration();
        if (Math.Abs(_engineZAcceleration) >= twiceY)
        {
            if (_engineZAcceleration < 0)
            {
                twiceY = -twiceY;
            }

            _engineZAcceleration = twiceY;
        }

        _playerZAcceleration = _engineZAcceleration + _gravityZAcceleration + _carCollisionZAcceleration;

        CalculateXAcceleration();
    }

    private int GetTwiceCollisionYAcceleration()
        => TouchingRoad ? _carCollisionYAcceleration * 2 : 0;

    private void CalculateXAcceleration()
    {
        int acceleration = _gravityXAcceleration + _carCollisionXAcceleration;
        int speedDiff = acceleration - _playerXSpeed;

        int twiceY = GetTwiceCollisionYAcceleration();
        if (Math.Abs(speedDiff) >= twiceY)
        {
            if (_playerXSpeed < 0)
            {
                twiceY = -twiceY;
            }

            acceleration -= twiceY;
            _playerXAcceleration = acceleration;
        }
        else
        {
            _playerXAcceleration = _carCollisionXAcceleration - _playerXSpeed;
        }
    }

    // Allow the car to steer (affects player y angle and y rotation acceleration).
    private void CalculateSteering()
    {
        // find the piece that the car is currently on
        IdentifyPiece(PlayerX, PlayerZ, ref _steeringPiece);
        CurrentPiece = _steeringPiece;
        TrackPiece piece = _track.Pieces[_steeringPiece];

        int sectionSteeringAmount = piece.SteeringAmount;

        bool leftHandBend = CalculateYAngleDifference(piece);

        _differenceAngle = _yAngleDifference;
        _posDifferenceAngle = Math.Abs(_yAngleDifference);

        // save a scaled positive difference angle ranging from 0 to 0x7fff
        int scaledPosDifferenceAngle = _posDifferenceAngle < 0x800 ? _posDifferenceAngle << 4 : 0x7fff;

        if (_leftRightValue != 0)
        {
            SteerCar(piece, sectionSteeringAmount, leftHandBend, scaledPosDifferenceAngle);
        }
        else
        {
            CoastCar(piece, sectionSteeringAmount, leftHandBend);
        }
    }

    // Difference between car's and road's y angle, normalised to -90..90 so the track can be driven either direction.
    private bool CalculateYAngleDifference(TrackPiece piece)
    {
        bool backwards = false;

        // calculate car x/z position relative to the piece
        CalcXZRelativeToPiece(PlayerX, PlayerZ, _steeringPiece, out int rx, out int rz);

        // calculate y angle of piece at the point where the centre of the car lies
        int sectionYAngle = CalcSectionYAngle(_steeringPiece, rx, rz);

        // temporarily reverse section y angle
        sectionYAngle = -sectionYAngle & (Track.MaxAngle - 1);

        // difference between the section and player's y angle:
        // increasingly negative turning right, increasingly positive turning left
        _yAngleDifference = sectionYAngle - PlayerYAngle;

        // make the difference range from -180 to 180 degrees
        if (_yAngleDifference > AmigaTrig.Degrees180)
        {
            _yAngleDifference -= AmigaTrig.Degrees360;
        }

        if (_yAngleDifference < -AmigaTrig.Degrees180)
        {
            _yAngleDifference += AmigaTrig.Degrees360;
        }

        // extra logic to allow car to drive round track in either direction
        // (backwards flag only applies to curves)
        if (_yAngleDifference > AmigaTrig.Degrees90)
        {
            _yAngleDifference -= AmigaTrig.Degrees180;
            backwards = true;
        }

        if (_yAngleDifference < -AmigaTrig.Degrees90)
        {
            _yAngleDifference += AmigaTrig.Degrees180;
            backwards = true;
        }

        // if player is on a curved section then adjust the difference angle
        bool leftHandBend = false;
        if (piece.Type is 0x80 or 0xc0)
        {
            // correctly identify left/right hand bend when driving backwards
            if ((piece.Type == 0x80) ^ piece.OppositeDirection ^ backwards)
            {
                // right hand bend
                _yAngleDifference += 217;
            }
            else
            {
                // left hand bend
                _yAngleDifference -= 217;
                leftHandBend = true;
            }
        }

        return leftHandBend;
    }

    // The player is steering.
    private void SteerCar(TrackPiece piece, int sectionSteeringAmount, bool leftHandBend, int scaledPosDifferenceAngle)
    {
        // work out if the positive difference angle is going to increase,
        // i.e. whether the car is trying to keep in line with the track
        bool increasing = (_differenceAngle < 0) ^ (_leftRightValue < 0);

        int steeringAmount;
        if (piece.Type is 0x80 or 0xc0)
        {
            // curve
            if ((_leftRightValue >= 0) ^ leftHandBend)
            {
                // steering into the bend
                steeringAmount = sectionSteeringAmount + 45;
            }
            else
            {
                // steering away from bend
                steeringAmount = sectionSteeringAmount - 35;

                // left/right value below just used as +'ve/-'ve flag
                _leftRightValue = leftHandBend ? -1 : 1;

                // ensure steering assistance is not done
                increasing = true;
            }
        }
        else
        {
            // straight
            steeringAmount = sectionSteeringAmount;
        }

        if (!increasing)
        {
            // add current difference (between player and road) onto steering
            // amount to assist steering when keeping in line with track
            steeringAmount += scaledPosDifferenceAngle >> 8;
        }

        CalculateSteeringAcceleration(steeringAmount);
    }

    // The player is not steering.
    private void CoastCar(TrackPiece piece, int sectionSteeringAmount, bool leftHandBend)
    {
        _yAngleDifference = 0; // zero steering acceleration

        if (piece.Type is 0x00 or 0x40)
        {
            // straight
            AlignCarWithRoad();
            AdjustSteeringAcceleration();
        }
        else
        {
            // curve: left/right value below just used as +'ve/-'ve flag
            _leftRightValue = leftHandBend ? -1 : 1;

            // give effect of centrifugal force?
            CalculateSteeringAcceleration(sectionSteeringAmount);
        }
    }

    private void CalculateSteeringAcceleration(int steeringAmount)
    {
        // steering acceleration increases with speed and is
        // calculated in a slightly odd way, to match Amiga StuntCarRacerSharp
        int steeringAcceleration = (PlayerZSpeed * steeringAmount) >> 8;

        if (_leftRightValue < 0)
        {
            steeringAcceleration = -steeringAcceleration; // steering left
        }

        steeringAcceleration >>= 3;

        _yAngleDifference = steeringAcceleration;

        if (_posDifferenceAngle >= 30 * 256)
        {
            AlignCarWithRoad();
        }

        AdjustSteeringAcceleration();
    }

    private void AlignCarWithRoad()
    {
        // gradually bring the car back in line with the road
        int adjust = _posDifferenceAngle;

        if (adjust >= 256)
        {
            adjust -= 30 * 256;
            if (adjust >= 0)
            {
                // large adjustment for when the car is very out of line
                // (e.g. sideways with respect to road)
                if (_differenceAngle >= 0)
                {
                    PlayerYAngle += adjust;
                }
                else
                {
                    PlayerYAngle -= adjust;
                }

                return;
            }

            adjust = 255; // set adjustment amount to maximum
        }

        // adjustment of player's Y angle increases as player's speed increases
        int speed = Math.Abs(PlayerZSpeed) + 0xa00;
        if (speed > 0x7f00)
        {
            speed = 0x7f00;
        }

        adjust = (adjust * speed) >> 15;

        if (adjust == 0)
        {
            adjust = 1; // at least do some adjusting
        }

        if (_differenceAngle >= 0)
        {
            PlayerYAngle += adjust;
        }
        else
        {
            PlayerYAngle -= adjust;
        }
    }

    private void AdjustSteeringAcceleration()
    {
        int acceleration = _yAngleDifference - _playerYRotationSpeed;

        // steering is disabled when not touching the road
        _playerYRotationAcceleration = TouchingRoad ? acceleration : 0;
    }

    // Adds components of the player's (rotated) X, Y and Z accelerations
    // to give world acceleration values.
    private void CalculateWorldAcceleration()
    {
        _totalWorldXAcceleration = (_playerXAcceleration * _trig.XX) >> Track.LogPrecision;
        _totalWorldXAcceleration += (_playerYAcceleration * _trig.YX) >> Track.LogPrecision;
        _totalWorldXAcceleration += (_playerZAcceleration * _trig.ZX) >> Track.LogPrecision;

        _totalWorldYAcceleration = (_playerXAcceleration * _trig.XY) >> Track.LogPrecision;
        _totalWorldYAcceleration += (_playerYAcceleration * _trig.YY) >> Track.LogPrecision;
        _totalWorldYAcceleration += (_playerZAcceleration * _trig.ZY) >> Track.LogPrecision;

        _totalWorldZAcceleration = (_playerXAcceleration * _trig.XZ) >> Track.LogPrecision;
        _totalWorldZAcceleration += (_playerYAcceleration * _trig.YZ) >> Track.LogPrecision;
        _totalWorldZAcceleration += (_playerZAcceleration * _trig.ZZ) >> Track.LogPrecision;
    }

    private void ReduceWorldAcceleration()
    {
        int amount = 0;
        int factor = 1; // set maximum reduction factor
        bool normalSituation = true;

        if (TouchingRoad || OnChains)
        {
            amount = Math.Abs(_carToRoadCollisionZAcceleration >> 8);

            if (amount >= 3 || _offMapStatus != 0 || Wrecked || OnChains)
            {
                // collision z acceleration large, off map, wrecked or on chains
                if (Wrecked || OnChains)
                {
                    factor = 3; // set medium reduction factor
                }

                amount = 0x6000;
                normalSituation = false;
            }
        }

        // normal case - car not on chains, little Z collision with road
        if (normalSituation)
        {
            amount = CalculateDragAmount();
            factor = 5; // set minimum reduction factor
        }

        // reduce acceleration values using current speed values
        int reductionX = (_playerWorldXSpeed * amount) >> 16;
        int reductionY = (_playerWorldYSpeed * amount) >> 16;
        int reductionZ = (_playerWorldZSpeed * amount) >> 16;
        _totalWorldXAcceleration -= reductionX >> factor;
        _totalWorldYAcceleration -= reductionY >> factor;
        _totalWorldZAcceleration -= reductionZ >> factor;
    }

    // Drag in the normal case, taken from the greatest of the car's three
    // speeds and eased off when slipstreaming the opponent.
    private int CalculateDragAmount()
    {
        // reduce accelerations depending upon car speed:
        // get greatest of player's x, y and z speeds
        int amount = Math.Abs(_playerXSpeed);

        int speedY = Math.Abs(_playerYSpeed);
        if (speedY > amount)
        {
            amount = speedY;
        }

        int speedZ = Math.Abs(PlayerZSpeed);
        if (speedZ > amount)
        {
            amount = speedZ;
        }

        // Slipstream: if player and opponent are in line left to right
        // and the opponent is in front, there is less drag on the player.
        if (PlayerCloseToOpponent && !OpponentBehindPlayer)
        {
            amount -= 20 * 128;
            if (amount < 0)
            {
                amount = 0;
            }
        }

        return amount;
    }

    // Damp the car X and Z angles to keep the car level with the road.
    private void CalculateXZRotationAcceleration()
    {
        // overall difference below road is effectively car X inclination;
        // front difference below road is effectively car Z inclination
        _playerXRotationAcceleration = _overallDifferenceBelowRoad - (_playerXRotationSpeed >> 4);
        if (TouchingRoad)
        {
            // lifts the car up at the front during forwards acceleration and,
            // vice versa, dips the front during backwards acceleration
            _playerXRotationAcceleration += _playerZAcceleration >> 2;
        }

        _playerZRotationAcceleration = _frontDifferenceBelowRoad - (_playerZRotationSpeed >> 4);
    }

    private void UpdatePlayersRotationSpeed()
    {
        _playerXRotationSpeed += (_playerXRotationAcceleration * Reduction) >> 8;
        _playerYRotationSpeed += (_playerYRotationAcceleration * Reduction) >> 8;
        _playerZRotationSpeed += (_playerZRotationAcceleration * Reduction) >> 8;
    }

    private void CalculateFinalRotationSpeed()
    {
        int sinX = AmigaTrig.Sin(PlayerXAngle);
        int sinZ = AmigaTrig.Sin(PlayerZAngle);
        int cosZ = AmigaTrig.Cos(PlayerZAngle);

        _playerFinalXRotationSpeed = (_playerXRotationSpeed * cosZ) >> Track.LogPrecision;
        _playerFinalXRotationSpeed += (_playerYRotationSpeed * -sinZ) >> Track.LogPrecision;

        _playerFinalYRotationSpeed = (_playerXRotationSpeed * sinZ) >> Track.LogPrecision;
        _playerFinalYRotationSpeed += (_playerYRotationSpeed * cosZ) >> Track.LogPrecision;

        // final Z rotation speed: rotate Y rotation speed about the X axis
        // and add it onto the Z rotation speed
        _playerFinalZRotationSpeed = _playerZRotationSpeed;
        _playerFinalZRotationSpeed += (_playerFinalYRotationSpeed * sinX) >> Track.LogPrecision;
    }

    private void UpdatePlayersWorldSpeed()
    {
        _playerWorldXSpeed += (_totalWorldXAcceleration * Reduction) >> 8;
        _playerWorldYSpeed += (_totalWorldYAcceleration * Reduction) >> 8;
        _playerWorldZSpeed += (_totalWorldZAcceleration * Reduction) >> 8;
    }

    private void UpdatePlayersPosition()
    {
        // set player's new position
        PlayerX += _playerWorldXSpeed * Reduction * Track.PcFactor;
        PlayerY += (_playerWorldYSpeed * Reduction) >> 1;
        PlayerZ += _playerWorldZSpeed * Reduction * Track.PcFactor;

        if (PlayerY >= 0x10000000)
        {
            PlayerY = 0x10000000;
        }

        // set player's new angles
        PlayerXAngle += (_playerFinalXRotationSpeed * Reduction) >> 8;
        PlayerYAngle += (_playerFinalYRotationSpeed * Reduction) >> 8;
        PlayerZAngle += (_playerFinalZRotationSpeed * Reduction) >> 8;

        // limit to valid range as no longer stored as words
        PlayerXAngle &= Track.MaxAngle - 1;
        PlayerYAngle &= Track.MaxAngle - 1;
        PlayerZAngle &= Track.MaxAngle - 1;

        // check player's X angle
        int limit;
        if (_atSideByte == 0xe0 && _smallerLimitRequired)
        {
            // all wheels off road and car on ground
            limit = 11 * 256;
        }
        else
        {
            limit = 45 * 256;
        }

        PlayerXAngle = ClampBodyAngle(PlayerXAngle, limit, ref _playerXRotationSpeed);

        // check player's Z angle
        PlayerZAngle = ClampBodyAngle(PlayerZAngle, limit, ref _playerZRotationSpeed);
    }

    // Engine revs calculation, used for the engine sound pitch (tested
    // against Amiga in the original).
    private void UpdateEngineRevs()
    {
        int c;

        if (!TouchingRoad)
        {
            // not touching road, so test if joystick is held forwards or backwards
            c = 0;
            if (_accelerate || _brake)
            {
                c = 0x9000;
            }
        }
        else
        {
            c = PlayerZSpeed & ~0xf; // zero low four bits
            if (c < 0)
            {
                c = -c;
            }
        }

        c += 0x580;
        c >>= 3;
        if (EngineRevs < 192)
        {
            // if engine revs are low then increase them slowly (e.g. at race start)
            c = 2;
        }
        else
        {
            // otherwise calculate revs change depending on current engine revs
            c -= EngineRevs;
            c >>= 3;
        }

        _engineRevsChange = c;

        // now adjust revs change
        if (_engineRevsChange >= 0x100)
        {
            _engineRevsChange = 0x100;
        }
        else if (_engineRevsChange < 0)
        {
            // minimum change is -0x100 when touching the road, -0x20 otherwise
            int minimum = TouchingRoad ? -0x100 : -0x20;
            if (_engineRevsChange < minimum)
            {
                _engineRevsChange = minimum;
            }
        }

        EngineFluctuation = _randomSource.NextInt() & 0xf;
    }
}
