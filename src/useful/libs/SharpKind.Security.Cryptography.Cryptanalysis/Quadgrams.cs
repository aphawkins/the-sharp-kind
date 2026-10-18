// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.IO.Compression;

namespace SharpKind.Security.Cryptography.Cryptanalysis;

/// <summary>
/// Quadgram statistics for a language, used to score how much a run of letters looks like it.
/// </summary>
/// <remarks>
/// The English counts are from practicalcryptography.com. The Italian counts are from Manzoni's
/// <i>I promessi sposi</i> (Project Gutenberg 45334), with the accents removed. Each score is the base 10 log of the
/// quadgram's probability. A quadgram that never occurs scores as if it occurred 0.01 times.
/// The letter frequencies are derived from the same counts, by the first letter of each quadgram.
/// </remarks>
internal sealed class Quadgrams
{
    private const int Size = EnglishLetters.Count * EnglishLetters.Count * EnglishLetters.Count * EnglishLetters.Count;

    private readonly Lazy<(float[] Scores, double[] LetterFrequencies)> _tables;

    private Quadgrams(string resource) => _tables = new(() => Load(resource));

    /// <summary>
    /// Gets the English statistics.
    /// </summary>
    internal static Quadgrams English { get; } = new("english_quadgrams.txt.gz");

    /// <summary>
    /// Gets the Italian statistics.
    /// </summary>
    internal static Quadgrams Italian { get; } = new("italian_quadgrams.txt.gz");

    /// <summary>
    /// Gets the relative frequency, as a percentage, of each letter A-Z in the language.
    /// </summary>
    internal IReadOnlyList<double> LetterFrequencies => _tables.Value.LetterFrequencies;

    /// <summary>
    /// Gets the statistics for a language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>The statistics.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The language has no statistics.</exception>
    internal static Quadgrams For(Language language)
        => language switch
        {
            Language.English => English,
            Language.Italian => Italian,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown language."),
        };

    /// <summary>
    /// Scores a run of letters.
    /// </summary>
    /// <param name="letters">The letters, each a zero based position in the alphabet.</param>
    /// <returns>The sum of the log probabilities of every quadgram. Higher is more like the language.</returns>
    internal double Score(ReadOnlySpan<int> letters)
    {
        float[] scores = _tables.Value.Scores;
        double score = 0;

        for (int i = 0; i + 3 < letters.Length; i++)
        {
            int index = letters[i];
            index = (index * EnglishLetters.Count) + letters[i + 1];
            index = (index * EnglishLetters.Count) + letters[i + 2];
            index = (index * EnglishLetters.Count) + letters[i + 3];
            score += scores[index];
        }

        return score;
    }

    private static (float[] Scores, double[] LetterFrequencies) Load(string name)
    {
        long[] counts = new long[Size];
        long[] letterCounts = new long[EnglishLetters.Count];
        long total = 0;

        using (Stream resource = typeof(Quadgrams).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("The quadgram resource is missing."))
        using (GZipStream gzip = new(resource, CompressionMode.Decompress))
        using (StreamReader reader = new(gzip))
        {
            while (reader.ReadLine() is string line)
            {
                int space = line.IndexOf(' ', StringComparison.Ordinal);
                int index = 0;

                for (int i = 0; i < space; i++)
                {
                    index = (index * EnglishLetters.Count) + EnglishLetters.IndexOf(line[i]);
                }

                long count = long.Parse(line.AsSpan(space + 1), CultureInfo.InvariantCulture);
                counts[index] = count;
                letterCounts[EnglishLetters.IndexOf(line[0])] += count;
                total += count;
            }
        }

        float[] scores = new float[Size];
        float floor = (float)Math.Log10(0.01 / total);

        for (int i = 0; i < Size; i++)
        {
            scores[i] = counts[i] == 0 ? floor : (float)Math.Log10((double)counts[i] / total);
        }

        double[] letterFrequencies = [.. letterCounts.Select(count => 100.0 * count / total)];

        return (scores, letterFrequencies);
    }
}
