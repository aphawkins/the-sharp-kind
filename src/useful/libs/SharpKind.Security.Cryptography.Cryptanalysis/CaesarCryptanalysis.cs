// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography.Cryptanalysis;

/// <summary>
/// The Caesar cipher cryptanalysis.
/// </summary>
public static class CaesarCryptanalysis
{
    /// <summary>
    /// The relative frequency, as a percentage, of each letter A-Z in English text.
    /// Taken from Robert Lewand, "Cryptological Mathematics" (2000).
    /// </summary>
    private static readonly double[] s_letterFrequencies
        = [8.2,
            1.5,
            2.8,
            4.3,
            13.0,
            2.2,
            2.0,
            6.1,
            7.0,
            0.15,
            0.77,
            4.0,
            2.4,
            6.7,
            7.5,
            1.9,
            0.095,
            6.0,
            9.3,
            9.1,
            2.8,
            0.98,
            2.4,
            0.15,
            2.0,
            0.074];

    /// <summary>
    /// Calculates the optimal settings.
    /// </summary>
    /// <remarks>
    /// The ciphertext is assumed to be English, as the letter frequencies it is scored against
    /// are those of English text.
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
        double[] cipherFrequencies = new double[EnglishLetters.Count];
        int letterCount = 0;

        // Totals for each letter
        foreach (char letter in ciphertext)
        {
            int index = EnglishLetters.IndexOf(letter);

            if (index >= 0)
            {
                cipherFrequencies[index]++;
                letterCount++;
            }
        }

        if (letterCount == 0)
        {
            return 0;
        }

        // Frequencies for each letter, as a percentage of the letters present rather than of the
        // whole ciphertext, so that spacing and punctuation don't deflate every frequency.
        for (int i = 0; i < EnglishLetters.Count; i++)
        {
            cipherFrequencies[i] = 100.0 * cipherFrequencies[i] / letterCount;
        }

        double bestDifference = double.MaxValue;
        int bestShift = 0;

        // Test all the shifts to find the best difference
        for (int shift = 0; shift < EnglishLetters.Count; shift++)
        {
            double difference = 0.0;

            for (int i = 0; i < EnglishLetters.Count; i++)
            {
                difference += Math.Abs(s_letterFrequencies[i] - cipherFrequencies[(i + shift) % EnglishLetters.Count]);
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
