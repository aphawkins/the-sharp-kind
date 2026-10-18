// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography.Cryptanalysis;

/// <summary>
/// The monoalphabetic substitution cipher cryptanalysis.
/// </summary>
/// <remarks>
/// A hill climb over decryption keys, scored by English quadgram statistics. Each climb starts
/// from a random key and swaps pairs of letters while the score improves. The best of several
/// climbs wins. The random source has a fixed seed, so the same ciphertext always gives the same key.
/// </remarks>
public static class MonoAlphabeticCryptanalysis
{
    private const int Climbs = 20;

    private const int Seed = 0;

    /// <summary>
    /// Calculates the optimal settings.
    /// </summary>
    /// <remarks>
    /// The ciphertext is assumed to be English. Only letters are scored, so word breaks and
    /// punctuation do not matter. A letter absent from the ciphertext cannot be recovered,
    /// so its substitution is arbitrary.
    /// </remarks>
    /// <param name="ciphertext">The text to crack.</param>
    /// <returns>The settings that best decrypt the ciphertext.</returns>
    public static IMonoAlphabeticSettings Crack(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        int[] cipherLetters = [.. ciphertext.Select(EnglishLetters.IndexOf).Where(index => index >= 0)];
        int[] key = BestKey(cipherLetters);

        // The key maps each ciphertext letter to its plaintext letter. The settings map each
        // plaintext letter to its ciphertext letter.
        char[] substitutions = new char[EnglishLetters.Count];

        for (int cipher = 0; cipher < EnglishLetters.Count; cipher++)
        {
            substitutions[key[cipher]] = (char)('A' + cipher);
        }

        return new MonoAlphabeticSettings { Substitutions = new string(substitutions) };
    }

    private static int[] BestKey(int[] cipherLetters)
    {
        int[] bestKey = [.. Enumerable.Range(0, EnglishLetters.Count)];

        // Fewer than four letters hold no quadgram to score.
        if (cipherLetters.Length < 4)
        {
            return bestKey;
        }

        // A repeatable, non-cryptographic random source is wanted here.
#pragma warning disable CA5394 // Do not use insecure randomness
        Random random = new(Seed);
        double bestScore = double.MinValue;
        int[] plainLetters = new int[cipherLetters.Length];

        for (int climb = 0; climb < Climbs; climb++)
        {
            int[] key = [.. Enumerable.Range(0, EnglishLetters.Count)];
            random.Shuffle(key);
            double score = Climb(key, cipherLetters, plainLetters);

            if (score > bestScore)
            {
                bestScore = score;
                bestKey = key;
            }
        }
#pragma warning restore CA5394

        return bestKey;
    }

    private static double Climb(int[] key, int[] cipherLetters, int[] plainLetters)
    {
        double score = Score(key, cipherLetters, plainLetters);
        bool improved = true;

        while (improved)
        {
            improved = false;

            for (int a = 0; a < EnglishLetters.Count - 1; a++)
            {
                for (int b = a + 1; b < EnglishLetters.Count; b++)
                {
                    (key[a], key[b]) = (key[b], key[a]);
                    double newScore = Score(key, cipherLetters, plainLetters);

                    if (newScore > score)
                    {
                        score = newScore;
                        improved = true;
                    }
                    else
                    {
                        (key[a], key[b]) = (key[b], key[a]);
                    }
                }
            }
        }

        return score;
    }

    private static double Score(int[] key, int[] cipherLetters, int[] plainLetters)
    {
        for (int i = 0; i < cipherLetters.Length; i++)
        {
            plainLetters[i] = key[cipherLetters[i]];
        }

        return Quadgrams.Score(plainLetters);
    }
}
