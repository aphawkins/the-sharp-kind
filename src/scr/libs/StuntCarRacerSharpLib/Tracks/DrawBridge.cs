// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using StuntCarRacerSharpLib.Cars;

namespace StuntCarRacerSharpLib.Tracks;

// Animates the moving bridge section (original MoveDrawBridge/ResetDrawBridge in Track.cpp): pieces 51/52 and 54/55 rise and fall
// with a triangle wave while no car is on them; the opponent's speeds for approaching (48-50) and climbing (51-52) match the height.
public sealed class DrawBridge
{
    private const int NumYValues = 15;

    // Opponent speed values for driving up the bridge (one per height).
    private static readonly byte[] s_climbSpeeds =
    [
        0xd2, 0xbb, 0xb7, 0xb3, 0xb1, 0xad, 0xab, 0xa7,
        0xa6, 0xa4, 0xa2, 0xa1, 0x9f, 0x9f, 0x9f, 0x9e,
    ];

    // Opponent speed values for approaching the bridge.
    private static readonly byte[] s_approachSpeeds =
    [
        0xf7, 0xf7, 0xf6, 0xf6, 0xf5, 0xf5, 0xf6, 0xf7,
        0xf8, 0xf9, 0xfb, 0xfd, 0xff, 0x02, 0x05, 0xfd,
    ];

    private readonly Track _track;
    private readonly int[] _yList = new int[NumYValues];

    private int _onBridgeOffset;
    private int _frameCount;

    public DrawBridge(Track track)
    {
        ArgumentNullException.ThrowIfNull(track);
        _track = track;
    }

    public void Reset(OpponentPhysics opponent)
    {
        _onBridgeOffset = 0;
        _frameCount = 0;

        // the original sets both cars to the start piece so the bridge moves
        Move(_track.PlayersStartPiece, _track.PlayersStartPiece, opponent);
    }

    public void Move(int playerPiece, int opponentPiece, OpponentPhysics opponent)
    {
        ArgumentNullException.ThrowIfNull(opponent);

        if (_track.Id != TrackId.DrawBridge)
        {
            return;
        }

        // bridge doesn't move while a car is on it (51-55), or while the opponent approaches (48-50) after a car has been on it
        bool onBridge = playerPiece is >= 51 and < 56
            || opponentPiece is >= 51 and < 56
            || (_onBridgeOffset != 0 && opponentPiece >= 48 && opponentPiece < 56);

        int approachFrame;
        if (onBridge)
        {
            _onBridgeOffset = 12;
            approachFrame = _onBridgeOffset + _frameCount;
        }
        else
        {
            _frameCount++;
            _onBridgeOffset = 0;

            // height between 0 and 15 (triangle wave)
            int height = (_frameCount & 0x1f) - 0x10;
            if (height < 0)
            {
                height = Math.Abs(height) - 1;
            }

            // opponent's required speeds for driving up the bridge
            opponent.SetSpeedValue(51, s_climbSpeeds[height]);
            opponent.SetSpeedValue(52, s_climbSpeeds[height]);

            // populate the y value list for the current height
            int increment = (height + 4) << 5;
            int y = increment;
            for (int i = 0; i < NumYValues; i++)
            {
                _yList[i] = y;
                y += increment;
            }

            UpdateYCoords(51, 1, 8, 0, 1);
            UpdateYCoords(52, 0, 7, 7, 1);
            UpdateYCoords(54, 1, 8, NumYValues - 1, -1);
            UpdateYCoords(55, 0, 7, NumYValues - 1 - 7, -1);

            if (opponentPiece != 47)
            {
                return;
            }

            approachFrame = _frameCount;
        }

        // approach speeds: values with the top bit set cause double acceleration for a quicker speed change
        int index = (approachFrame & 0x1f) >> 1;
        int speed = 0xc6;
        for (int i = 0; i < 3; i++)
        {
            speed += s_approachSpeeds[index];
            opponent.SetSpeedValue(48 + i, (byte)speed);
        }
    }

    private void UpdateYCoords(int piece, int firstCoord, int lastCoord, int firstYIndex, int direction)
    {
        TrackPiece trackPiece = _track.Pieces[piece];

        for (int i = firstCoord, j = firstYIndex; i <= lastCoord; i++, j += direction)
        {
            int y = _yList[j];
            Coord3D topLeft = trackPiece.Coords[i * 4];
            Coord3D topRight = trackPiece.Coords[(i * 4) + 1];
            trackPiece.Coords[i * 4] = topLeft with { Y = (y + trackPiece.LeftYShift) * Track.PcFactor };
            trackPiece.Coords[(i * 4) + 1] = topRight with { Y = (y + trackPiece.RightYShift) * Track.PcFactor };
        }
    }
}
