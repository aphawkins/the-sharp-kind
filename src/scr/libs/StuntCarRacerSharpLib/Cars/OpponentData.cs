// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

// Generated from the original Opponent Behaviour.cpp tables. Do not hand-edit the arrays.
namespace StuntCarRacerSharpLib.Cars;

internal static class OpponentData
{
    // Original opp_track_speed_values: standard league occupies the first 32 bytes, super league the next 32.
    internal static byte[] TrackSpeedValues { get; } =
    [
        0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
        0x41, 0x3a, 0x3e, 0x41, 0x48, 0x51, 0x48, 0x4f,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x48, 0x41, 0x45, 0x48, 0x4f, 0x58, 0x4f, 0x56,

        // Super league
        0x07, 0x03, 0x03, 0x03, 0x03, 0x03, 0x07, 0x03,
        0x66, 0x57, 0x57, 0x59, 0x59, 0x69, 0x62, 0x64,
        0x07, 0x03, 0x03, 0x03, 0x03, 0x01, 0x03, 0x03,
        0x61, 0x55, 0x53, 0x56, 0x58, 0x5b, 0x5a, 0x62,
    ];

    // Half the x distance that the opponent's rear wheels span, indexed
    // by wheel height difference.
    internal static int[] XSpans { get; } =
    [
        27, 27, 27, 27, 27, 26, 26, 26, 25, 25, 25, 24, 23, 23, 22, 21,
        20, 19, 18, 17, 15, 14, 11, 9, 7, 7, 7, 7, 7, 7, 7, 7,
    ];

    // Random steering shift table (original TAB5be34).
    internal static int[] SteeringTable { get; } =
    [
        32, 80, 96, 112, 112, 96, 80, 32,
        -32, -80, -96, -112, -112, -96, -80, -32,
    ];
}
