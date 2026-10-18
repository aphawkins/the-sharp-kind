// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography.Cryptanalysis;

/// <summary>
/// The Caesar cipher cryptanalysis.
/// </summary>
public static class CaesarCryptanalysis
{
    /// <summary>
    /// Calculates the optimal settings.
    /// </summary>
    /// <remarks>
    /// The ciphertext is assumed to be English. Each shift's decryption is scored by English
    /// quadgram statistics. With fewer than four letters, English letter frequencies, derived from the same
    /// statistics, are used instead.
    /// Only letters are scored, so word breaks and punctuation do not matter.
    /// </remarks>
    /// <param name="ciphertext">The text to crack.</param>
    /// <returns>The best guess crack, along with the decryption for every possible shift.</returns>
    public static (int BestShift, IReadOnlyDictionary<int, string> AllDecryptions) Crack(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        Dictionary<int, string> shifts = new(EnglishLetters.Count);
        CaesarSettings settings = new();
        Caesar cipher = new(settings);

        for (int i = 0; i < EnglishLetters.Count; i++)
        {
            settings.RightShift = i;
            shifts.Add(i, cipher.Decrypt(ciphertext));
        }

        return (BestShift(ciphertext), shifts);
    }

    private static int BestShift(string ciphertext)
    {
        int[] cipherLetters = [.. ciphertext.Select(EnglishLetters.IndexOf).Where(index => index >= 0)];

        // Fewer than four letters hold no quadgram to score, so fall back to letter frequencies.
        return cipherLetters.Length < 4 ? FrequencyShift(cipherLetters) : QuadgramShift(cipherLetters);
    }

    private static int QuadgramShift(int[] cipherLetters)
    {
        int[] plainLetters = new int[cipherLetters.Length];
        double bestScore = double.MinValue;
        int bestShift = 0;

        for (int shift = 0; shift < EnglishLetters.Count; shift++)
        {
            for (int i = 0; i < cipherLetters.Length; i++)
            {
                plainLetters[i] = (cipherLetters[i] - shift + EnglishLetters.Count) % EnglishLetters.Count;
            }

            double score = Quadgrams.Score(plainLetters);

            if (score > bestScore)
            {
                bestScore = score;
                bestShift = shift;
            }
        }

        return bestShift;
    }

    private static int FrequencyShift(int[] cipherLetters)
    {
        if (cipherLetters.Length == 0)
        {
            return 0;
        }

        // Frequencies for each letter, as a percentage of the letters present.
        double[] cipherFrequencies = new double[EnglishLetters.Count];

        foreach (int letter in cipherLetters)
        {
            cipherFrequencies[letter] += 100.0 / cipherLetters.Length;
        }

        IReadOnlyList<double> letterFrequencies = Quadgrams.LetterFrequencies;
        double bestDifference = double.MaxValue;
        int bestShift = 0;

        // Test all the shifts to find the best difference
        for (int shift = 0; shift < EnglishLetters.Count; shift++)
        {
            double difference = 0.0;

            for (int i = 0; i < EnglishLetters.Count; i++)
            {
                difference += Math.Abs(letterFrequencies[i] - cipherFrequencies[(i + shift) % EnglishLetters.Count]);
            }

            if (difference < bestDifference)
            {
                bestDifference = difference;
                bestShift = shift;
            }
        }

        return bestShift;
    }
}
