// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The CIA timer byte $E9EA mixes in, one value for each roll, in order.
internal sealed class ScriptedRandomSource : IRandomSource
{
    private readonly Queue<int> _timer;

    internal ScriptedRandomSource(IEnumerable<int> timer) => _timer = new(timer);

    // The timer bytes that make rolls come out as the game's did, from the state $26 and $27 held when they began.
    public static ScriptedRandomSource For(byte low, byte high, IEnumerable<byte> results)
    {
        List<int> timer = [];

        foreach (byte result in results)
        {
            int found = -1;

            for (int candidate = 0; candidate < 0x100 && found < 0; candidate++)
            {
                BbRandom probe = new(new SingleRandomSource(candidate)) { Low = low, High = high };

                if (probe.Next() == result)
                {
                    found = candidate;
                    low = probe.Low;
                    high = probe.High;
                }
            }

            timer.Add(found >= 0 ? found : throw new InvalidOperationException($"no timer byte rolls ${result:X2}"));
        }

        return new(timer);
    }

    public int NextInt() => throw new NotSupportedException();

    public int Random(int toExclusive) => _timer.Count > 0 ? _timer.Dequeue() : 0;

    public int Random(int fromInclusive, int toExclusive) => throw new NotSupportedException();

    public bool TrueOrFalse() => throw new NotSupportedException();

    public int GaussianRandom(int min, int max) => throw new NotSupportedException();

    private sealed class SingleRandomSource(int value) : IRandomSource
    {
        public int NextInt() => throw new NotSupportedException();

        public int Random(int toExclusive) => value;

        public int Random(int fromInclusive, int toExclusive) => throw new NotSupportedException();

        public bool TrueOrFalse() => throw new NotSupportedException();

        public int GaussianRandom(int min, int max) => throw new NotSupportedException();
    }
}
