// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

namespace StuntCarRacerSharpLib.Cars;

// Following ptitSeb's stuntcarremake KEY_P1_* definitions, which made accelerate/brake/boost
// independent keys rather than the original fluffyfreak remake's combined ones.
[Flags]
public enum CarInput
{
    None = 0,

    Left = 1,

    Right = 2,

    Accelerate = 4,

    Brake = 8,

    Boost = 16,

    // Convenience combinations for driving tests ("floor it").
    AccelBoost = Accelerate | Boost,

    BrakeBoost = Brake | Boost,
}
