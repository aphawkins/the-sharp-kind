// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Globalization;

namespace EliteSharpLib.Tests.GoldenTrace;

// Describes the first divergence rather than dumping two thousand lines at the reader.
//
// The tolerance exists because the frame-rate rework's later items turn ported integer constants
// into per-second rates, whose arithmetic stops matching exactly. Discrete fields (screen, docked
// flag, ship type/flags) never get a tolerance - those either match or a different path is a regression.
internal static class TraceComparer
{
    internal static string? FindFirstDifference(
        IReadOnlyList<TraceSample> expected,
        IReadOnlyList<TraceSample> actual,
        float tolerance)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        if (expected.Count != actual.Count)
        {
            return $"tick count: expected {expected.Count}, got {actual.Count}";
        }

        for (int i = 0; i < expected.Count; i++)
        {
            string? difference = CompareSample(expected[i], actual[i], tolerance);
            if (difference is not null)
            {
                return $"tick {expected[i].Tick}: {difference}";
            }
        }

        return null;
    }

    private static string? CompareSample(TraceSample expected, TraceSample actual, float tolerance)
        => Exact("screen", expected.Screen, actual.Screen)
            ?? Exact("docked", expected.IsDocked, actual.IsDocked)
            ?? Exact("gameOver", expected.IsGameOver, actual.IsGameOver)
            ?? Exact("mcount", expected.MCount, actual.MCount)
            ?? Near("messageCount", expected.MessageCount, actual.MessageCount, tolerance)
            ?? Near("laserTemp", expected.LaserTemp, actual.LaserTemp, tolerance)
            ?? Near("roll", expected.Roll, actual.Roll, tolerance)
            ?? Near("pitch", expected.Pitch, actual.Pitch, tolerance)
            ?? Near("speed", expected.Speed, actual.Speed, tolerance)
            ?? Near("energy", expected.Energy, actual.Energy, tolerance)
            ?? Near("shieldFront", expected.ShieldFront, actual.ShieldFront, tolerance)
            ?? Near("shieldRear", expected.ShieldRear, actual.ShieldRear, tolerance)
            ?? Near("fuel", expected.Fuel, actual.Fuel, tolerance)
            ?? Near("cabinTemp", expected.CabinTemperature, actual.CabinTemperature, tolerance)
            ?? Near("altitude", expected.Altitude, actual.Altitude, tolerance)
            ?? CompareObjects(expected.Objects, actual.Objects, tolerance);

    private static string? CompareObjects(
        IReadOnlyList<TraceObject> expected,
        IReadOnlyList<TraceObject> actual,
        float tolerance)
    {
        if (expected.Count != actual.Count)
        {
            return $"universe holds {actual.Count} object(s), expected {expected.Count}"
                + $" (expected {Describe(expected)}, got {Describe(actual)})";
        }

        for (int i = 0; i < expected.Count; i++)
        {
            TraceObject e = expected[i];
            TraceObject a = actual[i];
            string at = $"slot {e.Slot} ({e.Type})";

            string? difference = Exact($"{at} slot", e.Slot, a.Slot)
                ?? Exact($"{at} type", e.Type, a.Type)
                ?? Exact($"{at} flags", e.Flags, a.Flags)
                ?? Near($"{at} expDelta", e.ExpDelta, a.ExpDelta, tolerance)
                ?? Near($"{at} x", e.X, a.X, tolerance)
                ?? Near($"{at} y", e.Y, a.Y, tolerance)
                ?? Near($"{at} z", e.Z, a.Z, tolerance)
                ?? Near($"{at} rotX", e.RotX, a.RotX, tolerance)
                ?? Near($"{at} rotZ", e.RotZ, a.RotZ, tolerance);

            if (difference is not null)
            {
                return difference;
            }
        }

        return null;
    }

    private static string Describe(IReadOnlyList<TraceObject> objects)
        => objects.Count == 0 ? "nothing" : string.Join(", ", objects.Select(o => $"{o.Slot}:{o.Type}"));

    private static string? Exact<T>(string field, T expected, T actual)
        where T : notnull
        => expected.Equals(actual) ? null : $"{field}: expected {expected}, got {actual}";

    private static string? Near(string field, float expected, float actual, float tolerance)
    {
        float difference = MathF.Abs(expected - actual);
        return difference <= tolerance
            ? null
            : $"{field}: expected {F(expected)}, got {F(actual)} (off by {F(difference)}, tolerance {F(tolerance)})";
    }

    private static string F(float value) => value.ToString("0.0000", CultureInfo.InvariantCulture);
}
