// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Cars;

// A crane swings the car back above the road and dangles it until boost/fire is pressed, then
// normal gravity takes back over. Ported in spirit from the original's chain state machine, whose
// 68k fixed-point constants aren't recoverable from either C++ reference port (both skipped
// chains entirely) - this drives position/angle directly rather than through the acceleration
// integrator, reaching the same visual beats: swing settles, dangle, release, land.
public sealed partial class CarPhysics
{
    private static int MoveTowards(int value, int target, int step)
        => value < target ? Math.Min(value + step, target) : Math.Max(value - step, target);

    // Swing magnitude is a small +-44..+-16 pendulum amount (original
    // swing.magnitude); scaled here as degrees of car roll.
    private static int ScaleChainSwingAngle(int magnitude)
        => ((magnitude * Track.MaxAngle / 360) + Track.MaxAngle) % Track.MaxAngle;

    // Put the car back in the air above its current piece and start the
    // chain sequence (original: set.players.restart.position + car.on.chains.countdown = 240).
    private void BeginChainRecovery()
    {
        bool swingFromLeft = WhichSideByte == 0x80;
        int piece = CurrentPiece;

        Reset();
        PositionCarAbovePiece(piece);
        PlayerY += ChainRaiseHeight;

        _chainRaisedY = PlayerY;
        _chainSwingFromLeft = swingFromLeft;
        _chainSwingMagnitude = swingFromLeft ? -ChainSwingFullMagnitude : ChainSwingFullMagnitude;
        _chainCountdown = ChainLiftStart;
        WaitingToReleaseChains = false;

        // Opponent movement/collision only pause for the player's very
        // first drop of the race; mid-race recoveries leave them running.
        DropStartDone = true;
        _offTrackCount = 0;
    }

    // Settle the swing, then dangle until boost/fire is pressed, holding the car at the raised height so gravity does nothing until release.
    private void AnimateChainLift(CarInput input)
    {
        const int SwingStep = ChainSwingFullMagnitude / ChainSwingSettleFrames;

        int target = _chainSwingFromLeft ? -ChainSwingRestMagnitude : ChainSwingRestMagnitude;

        if (_chainCountdown > ChainLiftStart - ChainSwingSettleFrames)
        {
            _chainSwingMagnitude = MoveTowards(_chainSwingMagnitude, target, Math.Max(SwingStep, 1));
            _chainCountdown--;
        }
        else
        {
            WaitingToReleaseChains = true;
            if (input.HasFlag(CarInput.Boost))
            {
                ReleaseFromChains();
                return;
            }
        }

        PlayerY = _chainRaisedY;
        _playerWorldYSpeed = 0;
        PlayerZAngle = ScaleChainSwingAngle(_chainSwingMagnitude);
    }

    private void ReleaseFromChains()
    {
        _chainCountdown = 0;
        _chainSwingMagnitude = 0;
        WaitingToReleaseChains = false;
        PlayerZAngle = 0;
    }
}
