// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind;
using SharpKind.Fakes;
using StuntCarRacerSharpLib.Cars;
using StuntCarRacerSharpLib.Tracks;
using Xunit;

namespace StuntCarRacerSharpLib.Tests.Cars;

public class OpponentPhysicsTests
{
    [Theory]
    [InlineData(3, 3)]
    [InlineData(14, 3)] // 14 % NumOpponents (11) wraps back to 3
    public void StartRacePicksOpponentIdDeterministically(int roll, int expectedOpponentId)
    {
        // Arrange: the OpponentId = NextInt() % NumOpponents roll is forced
        // instead of hunting for a seed that lands on a given opponent.
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics player = new(track);
        OpponentPhysics opponent = new(track, player, new FakeRandomSource { NextIntValue = roll });

        // Act
        opponent.StartRace();

        // Assert
        Assert.Equal(expectedOpponentId, opponent.OpponentId);
    }

    [Fact]
    public void StartRaceInitialisesOpponent()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics player = new(track);
        player.StartRace();
        OpponentPhysics opponent = new(track, player, new RandomSource(new Random(1)));

        opponent.StartRace();

        Assert.InRange(opponent.OpponentId, 0, OpponentPhysics.NumOpponents - 1);
        Assert.False(string.IsNullOrEmpty(opponent.Name));
        Assert.Equal(track.PlayersStartPiece, opponent.CurrentPiece);
        Assert.Equal(0, opponent.LapNumber);
    }

    [Fact]
    public void OpponentDrivesAroundTheTrack()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics player = new(track);
        player.StartRace();
        OpponentPhysics opponent = new(track, player, new RandomSource(new Random(1)));
        opponent.StartRace();

        // Let the player drop (the opponent waits for drop start), then run.
        HashSet<int> piecesVisited = [];
        for (int frame = 0; frame < 1500; frame++)
        {
            player.Update(CarInput.None);
            opponent.Update();
            opponent.UpdateLapData();
            piecesVisited.Add(opponent.CurrentPiece);
        }

        // The opponent drives by itself: it should progress through many pieces.
        Assert.True(piecesVisited.Count > 10);
    }

    [Fact]
    public void OpponentCompletesALap()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics player = new(track);
        player.StartRace();
        OpponentPhysics opponent = new(track, player, new RandomSource(new Random(1)));
        opponent.StartRace();

        for (int frame = 0; frame < 8000 && opponent.LapNumber < 1; frame++)
        {
            player.Update(CarInput.None);
            opponent.Update();
            opponent.UpdateLapData();
        }

        Assert.True(opponent.LapNumber >= 1);
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
    public void OpponentDrivesOnAllTracks(TrackId id)
    {
        Track track = Track.Load(id);
        CarPhysics player = new(track);
        player.StartRace();
        OpponentPhysics opponent = new(track, player, new RandomSource(new Random(2)));
        opponent.StartRace();

        int startPiece = opponent.CurrentPiece;
        for (int frame = 0; frame < 1000; frame++)
        {
            player.Update(CarInput.None);
            opponent.Update();
        }

        Assert.NotEqual(startPiece, opponent.CurrentPiece);
    }

    [Fact]
    public void OpponentIsDeterministicWithSeededRandom()
    {
        Track track = Track.Load(TrackId.LittleRamp);

        CarPhysics player1 = new(track);
        player1.StartRace();
        OpponentPhysics opponent1 = new(track, player1, new RandomSource(new Random(7)));
        opponent1.StartRace();

        CarPhysics player2 = new(track);
        player2.StartRace();
        OpponentPhysics opponent2 = new(track, player2, new RandomSource(new Random(7)));
        opponent2.StartRace();

        for (int frame = 0; frame < 500; frame++)
        {
            player1.Update(CarInput.AccelBoost);
            opponent1.Update();
            player2.Update(CarInput.AccelBoost);
            opponent2.Update();
        }

        Assert.Equal(opponent1.OpponentId, opponent2.OpponentId);
        Assert.Equal(opponent1.CurrentPiece, opponent2.CurrentPiece);
        Assert.Equal(opponent1.X, opponent2.X);
        Assert.Equal(opponent1.Y, opponent2.Y);
        Assert.Equal(opponent1.Z, opponent2.Z);
    }

    [Fact]
    public void SpeedValueIsTenFasterOnSectionsTheCarCanBePutOn()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics player = new(track);
        OpponentPhysics opponent = new(track, player, new RandomSource(new Random(1)));

        // The standard-league random mask is zero, so values are deterministic: track base on sections the car can't be put on, ten faster elsewhere.
        Assert.Equal(0x48 + 10, opponent.SpeedValue(0));
        Assert.Equal(0x48, opponent.SpeedValue(27));
    }

    [Fact]
    public void SpeedValueDoesNotConsumeRandomnessWhileOnTheSamePiece()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics player = new(track);
        Random random = new(5);
        OpponentPhysics opponent = new(track, player, new RandomSource(random));

        // repeated queries for the same piece must not advance the RNG stream
        _ = opponent.SpeedValue(0);
        _ = opponent.SpeedValue(0);
        _ = opponent.SpeedValue(0);

        Random reference = new(5);
        _ = reference.Next(); // the single roll from the first query
        Assert.Equal(reference.Next(), random.Next());
    }

    [Fact]
    public void SpeedValueHonoursDrawBridgeOverrides()
    {
        Track track = Track.Load(TrackId.DrawBridge);
        CarPhysics player = new(track);
        OpponentPhysics opponent = new(track, player, new RandomSource(new Random(1)));

        int computed = opponent.SpeedValue(51);
        opponent.SetSpeedValue(51, 0xd2);

        Assert.Equal(0xd2, opponent.SpeedValue(51));
        Assert.NotEqual(computed, opponent.SpeedValue(51));
    }

    [Fact]
    public void DistanceToPlayerIsPositiveWhenOpponentAhead()
    {
        Track track = Track.Load(TrackId.LittleRamp);
        CarPhysics player = new(track);
        player.StartRace();
        OpponentPhysics opponent = new(track, player, new RandomSource(new Random(1)));
        opponent.StartRace();

        // Leave the player stationary while the opponent drives away; while
        // the opponent is ahead (within half a track) the distance is positive.
        for (int frame = 0; frame < 60; frame++)
        {
            player.Update(CarInput.None);
            opponent.Update();
        }

        Assert.NotEqual(player.CurrentPiece, opponent.CurrentPiece);
        Assert.True(opponent.DistanceToPlayer() > 0);
    }
}
