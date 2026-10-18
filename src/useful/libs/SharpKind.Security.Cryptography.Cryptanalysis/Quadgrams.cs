// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.IO.Compression;

namespace SharpKind.Security.Cryptography.Cryptanalysis;

/// <summary>
/// English quadgram statistics, used to score how English a run of letters looks.
/// </summary>
/// <remarks>
/// The counts are from practicalcryptography.com. Each score is the base 10 log of the
/// quadgram's probability. A quadgram that never occurs scores as if it occurred 0.01 times.
/// The letter frequencies are derived from the same counts, by the first letter of each quadgram.
/// </remarks>
internal static class Quadgrams
{
    private const int Size = EnglishLetters.Count * EnglishLetters.Count * EnglishLetters.Count * EnglishLetters.Count;

    private static readonly Lazy<(float[] Scores, double[] LetterFrequencies)> s_tables = new(Load);

    /// <summary>
    /// Gets the relative frequency, as a percentage, of each letter A-Z in English text.
    /// </summary>
    internal static IReadOnlyList<double> LetterFrequencies => s_tables.Value.LetterFrequencies;

    /// <summary>
    /// Scores a run of letters.
    /// </summary>
    /// <param name="letters">The letters, each a zero based position in the alphabet.</param>
    /// <returns>The sum of the log probabilities of every quadgram. Higher is more English.</returns>
    internal static double Score(ReadOnlySpan<int> letters)
    {
        float[] scores = s_tables.Value.Scores;
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

    private static (float[] Scores, double[] LetterFrequencies) Load()
    {
        long[] counts = new long[Size];
        long[] letterCounts = new long[EnglishLetters.Count];
        long total = 0;

        using (Stream resource = typeof(Quadgrams).Assembly.GetManifestResourceStream("english_quadgrams.txt.gz")
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
