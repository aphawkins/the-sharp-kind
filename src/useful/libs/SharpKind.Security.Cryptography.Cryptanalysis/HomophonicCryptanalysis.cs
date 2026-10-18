// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography.Cryptanalysis;

/// <summary>
/// The homophonic substitution cipher cryptanalysis, for ciphers with one character per symbol.
/// </summary>
/// <remarks>
/// A hill climb over decryption keys, scored by quadgram statistics for the plaintext language. A key gives each cipher
/// symbol a plaintext letter, and several symbols may share one. Each climb starts from a random key,
/// then changes the letter of one symbol, or swaps the letters of two, while the score improves. The
/// best of several climbs wins. The random source has a fixed seed, so the same ciphertext always
/// gives the same key.
/// </remarks>
public static class HomophonicCryptanalysis
{
    private const int Climbs = 20;

    private const int Seed = 0;

    private const int MaxDoublingRate = 50;

    // Singh stage 3 cracks with any weight from 3 to 6.
    private const double DivergenceWeight = 4;

    /// <summary>
    /// Calculates the optimal settings.
    /// </summary>
    /// <remarks>
    /// Every character that is not white space is a symbol. The most frequent symbol is taken as the
    /// word break if fewer than 1 in 50 of its uses are doubled, and decrypts to a space. Letters absent from the
    /// key have no homophones in the settings.
    /// </remarks>
    /// <param name="ciphertext">The text to crack.</param>
    /// <param name="language">The language of the plaintext.</param>
    /// <returns>The settings that best decrypt the ciphertext.</returns>
    public static IHomophonicSettings Crack(string ciphertext, Language language)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        char[] text = [.. ciphertext.Where(static c => !char.IsWhiteSpace(c)).Select(char.ToUpperInvariant)];
        char? wordBreak = WordBreak(text);
        char[] symbols = [.. text.Where(c => c != wordBreak).Distinct().Order()];
        int[] cipherSymbols = [.. text.Where(c => c != wordBreak).Select(c => Array.IndexOf(symbols, c))];
        int[] key = BestKey(symbols.Length, cipherSymbols, Quadgrams.For(language));

        Dictionary<char, IReadOnlyList<string>> homophones = symbols
            .Select((symbol, index) => (Symbol: symbol.ToString(), Letter: (char)('A' + key[index])))
            .GroupBy(static pair => pair.Letter)
            .ToDictionary(static group => group.Key, static group => (IReadOnlyList<string>)[.. group.Select(static pair => pair.Symbol)]);

        if (wordBreak is char space)
        {
            homophones[' '] = [space.ToString()];
        }

        return homophones.Count == 0 ? new HomophonicSettings() : new HomophonicSettings(homophones);
    }

    private static char? WordBreak(char[] text)
    {
        if (text.Length == 0)
        {
            return null;
        }

        IGrouping<char, char> common = text.GroupBy(static c => c).MaxBy(static group => group.Count())!;
        int doubles = 0;

        for (int i = 1; i < text.Length; i++)
        {
            if (text[i] == common.Key && text[i - 1] == common.Key)
            {
                doubles++;
            }
        }

        // Letters such as L, S and T often double, but a word break almost never does.
        return doubles * MaxDoublingRate < common.Count() ? common.Key : null;
    }

    private static int[] BestKey(int symbolCount, int[] cipherSymbols, Quadgrams quadgrams)
    {
        int[] bestKey = new int[symbolCount];

        // Fewer than four letters hold no quadgram to score.
        if (cipherSymbols.Length < 4)
        {
            return bestKey;
        }

        // A repeatable, non-cryptographic random source is wanted here.
#pragma warning disable CA5394 // Do not use insecure randomness
        Random random = new(Seed);
        double bestScore = double.MinValue;
        int[] plainLetters = new int[cipherSymbols.Length];

        for (int climb = 0; climb < Climbs; climb++)
        {
            int[] key = [.. Enumerable.Range(0, symbolCount).Select(_ => random.Next(EnglishLetters.Count))];
            double score = Climb(key, cipherSymbols, plainLetters, quadgrams);

            if (score > bestScore)
            {
                bestScore = score;
                bestKey = key;
            }
        }
#pragma warning restore CA5394

        return bestKey;
    }

    private static double Climb(int[] key, int[] cipherSymbols, int[] plainLetters, Quadgrams quadgrams)
    {
        double score = Score(key, cipherSymbols, plainLetters, quadgrams);
        bool improved = true;

        while (improved)
        {
            double before = score;
            score = ChangeLetters(key, cipherSymbols, plainLetters, quadgrams, score);
            score = SwapLetters(key, cipherSymbols, plainLetters, quadgrams, score);
            improved = score > before;
        }

        return score;
    }

    private static double ChangeLetters(int[] key, int[] cipherSymbols, int[] plainLetters, Quadgrams quadgrams, double score)
    {
        for (int symbol = 0; symbol < key.Length; symbol++)
        {
            int best = key[symbol];

            for (int letter = 0; letter < EnglishLetters.Count; letter++)
            {
                key[symbol] = letter;
                double newScore = Score(key, cipherSymbols, plainLetters, quadgrams);

                if (newScore > score)
                {
                    score = newScore;
                    best = letter;
                }
            }

            key[symbol] = best;
        }

        return score;
    }

    private static double SwapLetters(int[] key, int[] cipherSymbols, int[] plainLetters, Quadgrams quadgrams, double score)
    {
        for (int a = 0; a < key.Length - 1; a++)
        {
            for (int b = a + 1; b < key.Length; b++)
            {
                (key[a], key[b]) = (key[b], key[a]);
                double newScore = Score(key, cipherSymbols, plainLetters, quadgrams);

                if (newScore > score)
                {
                    score = newScore;
                }
                else
                {
                    (key[a], key[b]) = (key[b], key[a]);
                }
            }
        }

        return score;
    }

    private static double Score(int[] key, int[] cipherSymbols, int[] plainLetters, Quadgrams quadgrams)
    {
        Span<int> counts = stackalloc int[EnglishLetters.Count];

        for (int i = 0; i < cipherSymbols.Length; i++)
        {
            plainLetters[i] = key[cipherSymbols[i]];
            counts[plainLetters[i]]++;
        }

        // Quadgrams alone reward giving every symbol a common letter. Taking off the divergence of the
        // letter frequencies from the language, over the whole text, removes that reward.
        IReadOnlyList<double> letterFrequencies = quadgrams.LetterFrequencies;
        double divergence = 0;

        for (int letter = 0; letter < EnglishLetters.Count; letter++)
        {
            if (counts[letter] > 0)
            {
                double frequency = (double)counts[letter] / cipherSymbols.Length;
                divergence += frequency * Math.Log10(frequency * 100 / letterFrequencies[letter]);
            }
        }

        return quadgrams.Score(plainLetters) - (DivergenceWeight * cipherSymbols.Length * divergence);
    }
}
