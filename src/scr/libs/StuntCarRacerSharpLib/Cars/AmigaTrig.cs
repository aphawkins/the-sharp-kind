// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Cars;

// From the original 3D Engine.cpp. Angles are internal format (65536 = 360 degrees); results are scaled by 16384.
internal static class AmigaTrig
{
    internal const int Precision = 1 << Track.LogPrecision;

    internal const int Degrees360 = Track.MaxAngle;

    internal const int Degrees270 = 3 * Track.MaxAngle / 4;

    internal const int Degrees180 = Track.MaxAngle / 2;

    internal const int Degrees90 = Track.MaxAngle / 4;

    // Table extends past 360 degrees to make use of the sine/cosine overlap.
    private static readonly short[] s_sinCos = CreateSinCosTable();

    internal static short Sin(int angle) => s_sinCos[angle];

    internal static short Cos(int angle) => s_sinCos[angle + Degrees90];

    private static short[] CreateSinCosTable()
    {
        short[] table = new short[Track.MaxAngle + Degrees90];
        const double step = 2 * Math.PI / Track.MaxAngle;
        for (int i = 0; i < table.Length; i++)
        {
            table[i] = (short)(Math.Sin(i * step) * Precision);
        }

        return table;
    }
}
