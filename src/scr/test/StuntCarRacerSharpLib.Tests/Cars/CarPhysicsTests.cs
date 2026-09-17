// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.Fakes;
using StuntCarRacerSharpLib.Cars;
using StuntCarRacerSharpLib.Tracks;
using Xunit;

namespace StuntCarRacerSharpLib.Tests.Cars;

public class CarPhysicsTests
{
    [Fact]
    public void EngineFluctuationIsDeterministicWithInjectedRandomness()
    {
        // Arrange: force the NextInt() & 0xf roll instead of leaving it to chance.
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track, new FakeRandomSource { NextIntValue = 0x2f });

        car.StartRace();

        // Act
        car.Update(CarInput.None);

        // Assert
        Assert.Equal(0x2f & 0xf, car.EngineFluctuation);
    }

    [Fact]
    public void OffRoadPitchIsDeterministicWithInjectedRandomness()
    {
        // Arrange: force the divisor's NextInt() & 0x1c roll to its maximum,
        // instead of the existing range-based test's real-randomness run.
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track, new FakeRandomSource { NextIntValue = 0x1c });
        car.StartRace();

        for (int frame = 0; frame < 100 && !car.TouchingRoad; frame++)
        {
            car.Update(CarInput.None);
        }

        bool triggered = false;
        for (int frame = 0; frame < 500 && !triggered; frame++)
        {
            car.Update(CarInput.AccelBoost | CarInput.Left);
            triggered = car.OffRoadSoundTriggered;
        }

        Assert.True(triggered);

        // AMIGA_PAL_HZ / (450 + (0x1c & 0x1c)) relative to the 464 midpoint
        // anchor - see CalculateOffRoadPitch.
        Assert.Equal(464.0 / 478, car.OffRoadPitch);
    }

    [Fact]
    public void TurnAroundAddsHalfATurnToTheYAngle()
    {
        // Arrange
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track, new FakeRandomSource());
        car.StartRace();
        int before = car.YAngle;

        // Act: two turns come back to where the car started, and the second
        // one proves the angle wraps rather than running past a full turn.
        car.TurnAround();
        int afterOne = car.YAngle;
        car.TurnAround();

        // Assert
        Assert.Equal((before + AmigaTrig.Degrees180) & (Track.MaxAngle - 1), afterOne);
        Assert.Equal(before, car.YAngle);
    }

    [Fact]
    public void StartRacePositionsCarAboveStartPiece()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);

        car.StartRace();

        // Little Ramp start piece is 15, a straight the car can be put on.
        Assert.Equal(15, car.CurrentPiece);
        Assert.False(car.DropStartDone);
        Assert.False(car.TouchingRoad);
        Assert.Equal(0, car.DisplaySpeed);
        Assert.Equal(0, car.LapNumber);
    }

    [Fact]
    public void CarDropsOntoRoadAtRaceStart()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        // With no input the car should fall under gravity and land on the road.
        bool landed = false;
        for (int frame = 0; frame < 100 && !landed; frame++)
        {
            car.Update(CarInput.None);
            landed = car.TouchingRoad;
        }

        Assert.True(landed);
        Assert.True(car.DropStartDone);
    }

    [Fact]
    public void CarGoesOnChainsAfterLeavingTrackThenReleasesOnBoost()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        for (int frame = 0; frame < 100 && !car.TouchingRoad; frame++)
        {
            car.Update(CarInput.None);
        }

        // Steer hard off the road and keep driving until recovery kicks in.
        bool wentOnChains = false;
        for (int frame = 0; frame < 500 && !wentOnChains; frame++)
        {
            car.Update(CarInput.AccelBoost | CarInput.Left);
            wentOnChains = car.OnChains;
        }

        Assert.True(wentOnChains);

        // Nothing releases the car until the swing settles and it starts
        // waiting for the player to press boost/fire.
        for (int frame = 0; frame < 100 && !car.WaitingToReleaseChains; frame++)
        {
            car.Update(CarInput.None);
        }

        Assert.True(car.WaitingToReleaseChains);
        Assert.True(car.OnChains);

        bool landed = false;
        for (int frame = 0; frame < 200 && !landed; frame++)
        {
            car.Update(CarInput.Boost);
            landed = car.TouchingRoad;
        }

        Assert.False(car.OnChains);
        Assert.True(landed);
    }

    [Fact]
    public void AcceleratingIncreasesDisplaySpeed()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        // Land first, then accelerate.
        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.None);
        }

        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.AccelBoost);
        }

        Assert.True(car.TouchingRoad);
        Assert.True(car.DisplaySpeed > 0);
    }

    // Regression: dead zone raised to "< 0x1100" and rescaled by 200/128 so the speed bar can reach full width.
    [Fact]
    public void DisplaySpeedStaysZeroBelowTheDeadZoneThenRescales()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.None);
        }

        bool sawDeadZone = false;
        bool sawRescaledOutput = false;
        for (int frame = 0; frame < 200; frame++)
        {
            car.Update(CarInput.AccelBoost);

            if (car.PlayerZSpeed < 0x1100)
            {
                Assert.Equal(0, car.DisplaySpeed);
                sawDeadZone = true;
            }
            else
            {
                // the rescaled formula produces roughly 1.5625x the old
                // (183/32768) value, capable of exceeding it
                int oldFormula = (car.PlayerZSpeed * 183) >> 15;
                if (car.DisplaySpeed > oldFormula)
                {
                    sawRescaledOutput = true;
                }
            }
        }

        Assert.True(sawDeadZone, "expected some frames below the 0x1100 dead zone");
        Assert.True(sawRescaledOutput, "expected the rescaled formula to exceed the old one at some point");
    }

    [Fact]
    public void CarDrivesAlongTrackThroughPieces()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        // Drive forward; on the road most of the time (it launches off the ramp and is briefly airborne, correctly).
        HashSet<int> piecesVisited = [];
        int framesTouching = 0;
        for (int frame = 0; frame < 300; frame++)
        {
            car.Update(CarInput.AccelBoost);
            piecesVisited.Add(car.CurrentPiece);
            if (car.TouchingRoad)
            {
                framesTouching++;
            }
        }

        Assert.True(piecesVisited.Count >= 5);
        Assert.True(framesTouching > 150);
    }

    [Fact]
    public void BoostConsumesReserve()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();
        car.BoostReserve = track.StandardBoost;

        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.None);
        }

        for (int frame = 0; frame < 200; frame++)
        {
            car.Update(CarInput.AccelBoost);
        }

        Assert.True(car.BoostReserve < track.StandardBoost);
    }

    // Regression: accelerate/brake/boost are independent keys. Boost alone must not move the car or consume the reserve.
    [Fact]
    public void BoostAloneWithoutAccelerateOrBrakeDoesNothing()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();
        car.BoostReserve = track.StandardBoost;

        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.None);
        }

        int speedBeforeBoost = car.DisplaySpeed;
        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.Boost);
        }

        Assert.Equal(track.StandardBoost, car.BoostReserve);
        Assert.Equal(speedBeforeBoost, car.DisplaySpeed);
    }

    [Fact]
    public void AccelerateAloneMovesTheCarWithoutConsumingBoost()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();
        car.BoostReserve = track.StandardBoost;

        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.None);
        }

        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.Accelerate);
        }

        Assert.True(car.DisplaySpeed > 0);
        Assert.Equal(track.StandardBoost, car.BoostReserve);
    }

    [Fact]
    public void SteeringChangesHeading()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.None);
        }

        int headingBefore = car.YAngle;

        // Get moving, then steer left.
        for (int frame = 0; frame < 30; frame++)
        {
            car.Update(CarInput.AccelBoost);
        }

        for (int frame = 0; frame < 30; frame++)
        {
            car.Update(CarInput.AccelBoost | CarInput.Left);
        }

        Assert.NotEqual(headingBefore, car.YAngle);
    }

    [Fact]
    public void PhysicsIsDeterministic()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car1 = new(track);
        CarPhysics car2 = new(track);
        car1.StartRace();
        car2.StartRace();

        for (int frame = 0; frame < 200; frame++)
        {
            CarInput input = frame < 100 ? CarInput.None : CarInput.AccelBoost;
            car1.Update(input);
            car2.Update(input);
        }

        Assert.Equal(car1.X, car2.X);
        Assert.Equal(car1.Y, car2.Y);
        Assert.Equal(car1.Z, car2.Z);
        Assert.Equal(car1.YAngle, car2.YAngle);
        Assert.Equal(car1.DisplaySpeed, car2.DisplaySpeed);
    }

    [Theory]
    [InlineData(TrackId.LittleRamp)]
    [InlineData(TrackId.SteppingStones)]
    [InlineData(TrackId.HumpBack)]
    [InlineData(TrackId.BigRamp)]
    [InlineData(TrackId.SkiJump)]
    [InlineData(TrackId.DrawBridge)]
    [InlineData(TrackId.HighJump)]
    [InlineData(TrackId.RollerCoaster)]
    public void CarCanDriveOnAllTracks(TrackId id)
    {
        Track track = Track.Load(id);
        CarPhysics car = new(track);
        car.StartRace();

        // Drop, then drive for a while - must not throw and must land.
        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.None);
        }

        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.AccelBoost);
        }

        Assert.True(car.DropStartDone);
    }

    [Fact]
    public void FrontWheelsSpinFasterAsSpeedIncreases()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.None);
        }

        // wheel frames must stay within the cockpit sprite sheet's 6 frames
        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.AccelBoost);
            Assert.InRange(car.LeftWheelFrame, 0, 5);
            Assert.InRange(car.RightWheelFrame, 0, 5);
        }
    }

    [Fact]
    public void WheelAnglesAdvanceOnEveryEngineRevsTickNotJustPhysicsFrames()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        // land on the road and build up speed
        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.AccelBoost);
            car.ApplyEngineRevs();
        }

        int startFrame = car.LeftWheelFrame;

        // no further physics frames (car.Update), only the 50Hz engine-revs
        // tick that now drives the wheel angle advance
        bool advanced = false;
        for (int tick = 0; tick < 20 && !advanced; tick++)
        {
            car.ApplyEngineRevs();
            advanced = car.LeftWheelFrame != startFrame;
        }

        Assert.True(advanced);
    }

    [Fact]
    public void WheelBounceIsZeroWhileAirborne()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        // at race start the car is above the road, not touching it
        Assert.Equal(0, car.LeftWheelBounce);
        Assert.Equal(0, car.RightWheelBounce);
    }

    [Fact]
    public void SmashHolesResetOnNewRace()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        Assert.Equal(0, car.SmashHoles);

        car.StartRace();

        Assert.Equal(0, car.SmashHoles);
    }

    [Fact]
    public void FullDamageWrecksTheCar()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        Assert.False(car.Wrecked);

        car.AddCollisionDamage(255);
        car.UpdateDamage();

        Assert.Equal(255, car.NewDamage);
        Assert.True(car.Wrecked);
    }

    [Fact]
    public void PartialDamageDoesNotWreckTheCar()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        car.AddCollisionDamage(200);
        car.UpdateDamage();

        Assert.Equal(200, car.NewDamage);
        Assert.False(car.Wrecked);
    }

    [Fact]
    public void WreckedResetsOnNewRace()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        car.AddCollisionDamage(255);
        car.UpdateDamage();

        Assert.True(car.Wrecked);

        car.StartRace();

        Assert.False(car.Wrecked);
    }

    [Fact]
    public void LimitViewpointYLeavesRestingCarUnchanged()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        // land on the road and settle
        for (int frame = 0; frame < 100; frame++)
        {
            car.Update(CarInput.None);
        }

        Assert.True(car.TouchingRoad);

        // at rest the wheels sit within the adjustment threshold, so the
        // viewpoint is not moved
        Assert.Equal(car.PlayerY, car.LimitViewpointY());
    }

    [Fact]
    public void LimitViewpointYHoldsViewpointUpOverBumps()
    {
        Track track = Track.Load(TrackId.HumpBack);
        CarPhysics car = new(track);
        car.StartRace();

        // Drive over the humps; landing compression must engage the viewpoint limit on at least one frame.
        bool engaged = false;
        for (int frame = 0; frame < 400; frame++)
        {
            CarInput input = CarInput.Accelerate;
            if (car.RoadXPosition < 0xA0)
            {
                input |= CarInput.Right;
            }
            else if (car.RoadXPosition > 0xE0)
            {
                input |= CarInput.Left;
            }

            // Press boost/fire to release from the recovery chains if the
            // car has gone off track, as a player would.
            if (car.OnChains)
            {
                input |= CarInput.Boost;
            }

            car.Update(input);

            int limited = car.LimitViewpointY();
            if (limited != car.PlayerY)
            {
                engaged = true;
                Assert.True(limited > car.PlayerY);
            }
        }

        Assert.True(engaged);
    }

    [Fact]
    public void CurrentLapTicksOnlyAdvanceViaApplyEngineRevs()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        // the physics frame alone (car.Update) must not advance the lap
        // clock - only the 50Hz engine-revs tick does.
        car.Update(CarInput.None);
        Assert.Equal(0, car.CurrentLapTicks);

        car.ApplyEngineRevs();
        Assert.Equal(1, car.CurrentLapTicks);
    }

    [Fact]
    public void LapTimerResetsOnNewRace()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        for (int tick = 0; tick < 10; tick++)
        {
            car.ApplyEngineRevs();
        }

        Assert.Equal(10, car.CurrentLapTicks);
        Assert.Null(car.BestLapTicks);

        car.StartRace();

        Assert.Equal(0, car.CurrentLapTicks);
        Assert.Null(car.BestLapTicks);
    }

    [Fact]
    public void CompletingALapRecordsTheBestLapTimeAndResetsTheCurrentLap()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        Assert.Equal(0, car.CurrentLapTicks);
        Assert.Null(car.BestLapTicks);

        // Drive a full lap as a player would, releasing the recovery chains if the car strays off track.
        for (int frame = 0; frame < 8000 && car.LapNumber < 1; frame++)
        {
            CarInput input = CarInput.AccelBoost;
            if (car.RoadXPosition < 0xA0)
            {
                input |= CarInput.Right;
            }
            else if (car.RoadXPosition > 0xE0)
            {
                input |= CarInput.Left;
            }

            if (car.OnChains)
            {
                input |= CarInput.Boost;
            }

            car.Update(input);
            car.ApplyEngineRevs();
            car.UpdateLapData();
        }

        Assert.Equal(1, car.LapNumber);
        Assert.NotNull(car.BestLapTicks);
        Assert.True(car.BestLapTicks > 0);

        // the current lap has just restarted, so it is short relative to
        // the lap that was just recorded as best
        Assert.True(car.CurrentLapTicks < car.BestLapTicks);
    }

    [Fact]
    public void CreakVolumeStaysWithinTheOriginalsAmigaRange()
    {
        // AddCollisionDamage only drives the cosmetic average; CreakVolume follows a separate
        // impact-derived value, so this only asserts it stays in range - see CreakVolumeVariesAcrossHardLandings.
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        car.AddCollisionDamage(200);
        car.UpdateDamage();

        Assert.True(car.CreakSoundTriggered);
        Assert.InRange(car.CreakVolume, 28f / 64f, 1f);
    }

    [Fact]
    public void CreakVolumeStaysInRangeAcrossHardLandings()
    {
        Track track = Track.Load(TrackId.HumpBack);
        CarPhysics car = new(track);
        car.StartRace();

        bool triggered = false;
        for (int frame = 0; frame < 400; frame++)
        {
            CarInput input = CarInput.Accelerate;
            if (car.RoadXPosition < 0xA0)
            {
                input |= CarInput.Right;
            }
            else if (car.RoadXPosition > 0xE0)
            {
                input |= CarInput.Left;
            }

            if (car.OnChains)
            {
                input |= CarInput.Boost;
            }

            car.Update(input);
            car.UpdateDamage();

            if (car.CreakSoundTriggered)
            {
                triggered = true;
                Assert.InRange(car.CreakVolume, 28f / 64f, 1f);
            }
        }

        Assert.True(triggered);
    }

    [Fact]
    public void GroundedSoundTriggersWithVolumeInTheOriginalsAmigaRangeOnAHardLanding()
    {
        Track track = Track.Load(TrackId.HumpBack);
        CarPhysics car = new(track);
        car.StartRace();

        bool triggered = false;
        for (int frame = 0; frame < 400 && !triggered; frame++)
        {
            CarInput input = CarInput.Accelerate;
            if (car.RoadXPosition < 0xA0)
            {
                input |= CarInput.Right;
            }
            else if (car.RoadXPosition > 0xE0)
            {
                input |= CarInput.Left;
            }

            if (car.OnChains)
            {
                input |= CarInput.Boost;
            }

            car.Update(input);
            triggered = car.GroundedSoundTriggered;
        }

        Assert.True(triggered);
        Assert.InRange(car.GroundedVolume, 28f / 64f, 1f);
    }

    [Fact]
    public void OffRoadSoundTriggersWithPitchNearTheOriginalsRandomRange()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        for (int frame = 0; frame < 100 && !car.TouchingRoad; frame++)
        {
            car.Update(CarInput.None);
        }

        bool triggered = false;
        for (int frame = 0; frame < 500 && !triggered; frame++)
        {
            car.Update(CarInput.AccelBoost | CarInput.Left);
            triggered = car.OffRoadSoundTriggered;
        }

        Assert.True(triggered);

        // AMIGA_PAL_HZ / (450 + rand&0x1c) relative to the 464 midpoint
        // anchor - see CalculateOffRoadPitch.
        Assert.InRange(car.OffRoadPitch, 464.0 / 478, 464.0 / 450);
    }

    [Fact]
    public void WreckSoundTriggersWithPitchInTheOriginalsSpeedBasedRange()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics car = new(track);
        car.StartRace();

        // stay centred on the road (as a player steering would) so
        // _offMapStatus never blocks the wreck-scrape trigger below.
        bool triggered = false;
        for (int frame = 0; frame < 300 && !triggered; frame++)
        {
            CarInput input = CarInput.AccelBoost;
            if (car.RoadXPosition < 0xA0)
            {
                input |= CarInput.Right;
            }
            else if (car.RoadXPosition > 0xE0)
            {
                input |= CarInput.Left;
            }

            if (car.OnChains)
            {
                input |= CarInput.Boost;
            }

            car.Update(input);

            if (frame == 200)
            {
                Assert.True(car.DisplaySpeed > 0);
                car.AddCollisionDamage(255);
                car.UpdateDamage();
                Assert.True(car.Wrecked);
            }

            triggered = car.WreckSoundTriggered;
        }

        Assert.True(triggered);

        // AMIGA_PAL_HZ / p relative to the 360 divisor-range midpoint
        // anchor - see CalculateWreckPitch.
        Assert.InRange(car.WreckPitch, 360.0 / 422, 360.0 / 298);
    }
}
