// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind;

namespace EliteSharpLib;

/// <summary>
/// Elite's random-number source: the shared <see cref="RandomSource"/>
/// plus the period-accurate 6502/MSX generators, which carry their own
/// <see cref="RandomSeed"/> state and don't generalise to other games.
/// </summary>
internal sealed class RNG : IRandomSource
{
    private readonly IRandomSource _randomSource;

    internal RNG(Random random)
        : this(new RandomSource(random))
    {
    }

    // Lets tests inject a FakeRandomSource without changing the RNG(Random) constructor the DI container depends on.
    internal RNG(IRandomSource randomSource) => _randomSource = randomSource;

    internal RandomSeed Seed { get; set; } = new();

    public int NextInt() => _randomSource.NextInt();

    public int Random(int toExclusive) => _randomSource.Random(toExclusive);

    public int Random(int fromInclusive, int toExclusive) => _randomSource.Random(fromInclusive, toExclusive);

    public bool TrueOrFalse() => _randomSource.TrueOrFalse();

    public int GaussianRandom(int min, int max) => _randomSource.GaussianRandom(min, max);

    /// <summary>
    /// Generate a random number between 0 and 255.
    /// This is the version used in the 6502 Elites.
    /// </summary>
    /// <returns>A random number between 0 and 255.</returns>
    internal int GenerateRandomNumber()
    {
        int x = (Seed.A * 2) & 0xFF;
        int a = x + Seed.C;
        if (Seed.A > 127)
        {
            a++;
        }

        Seed.A = a & 0xFF;
        Seed.C = x;

        a /= 256;    // a = any carry left from above
        x = Seed.B;
        a = (a + x + Seed.D) & 0xFF;
        Seed.B = a;
        Seed.D = x;
        return a;
    }

    /// <summary>
    /// Generate a random number between 0 and 255.
    /// This is the version used in the MSX and 16bit Elites.
    /// </summary>
    /// <returns>A random number between 0 and 255.</returns>
    internal int GenMSXRandomNumber()
    {
        int a = Seed.A;
        int b = Seed.B;

        Seed.A = Seed.C;
        Seed.B = Seed.D;

        a += Seed.C;
        b = (b + Seed.D) & 255;
        if (a > 255)
        {
            a &= 255;
            b++;
        }

        Seed.C = a;
        Seed.D = b;

        return Seed.C / 52;
    }
}
