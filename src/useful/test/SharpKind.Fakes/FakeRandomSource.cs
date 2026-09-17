// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Fakes;

// Every member returns a fixed, test-set value, so a test can force an exact branch without hunting for a seed that produces it.
public sealed class FakeRandomSource : IRandomSource
{
    public int NextIntValue { get; set; }

    public int RandomValue { get; set; }

    public bool TrueOrFalseValue { get; set; }

    public int GaussianRandomValue { get; set; }

    public int NextInt() => NextIntValue;

    public int Random(int toExclusive) => RandomValue;

    public int Random(int fromInclusive, int toExclusive) => RandomValue;

    public bool TrueOrFalse() => TrueOrFalseValue;

    public int GaussianRandom(int min, int max) => GaussianRandomValue;
}
