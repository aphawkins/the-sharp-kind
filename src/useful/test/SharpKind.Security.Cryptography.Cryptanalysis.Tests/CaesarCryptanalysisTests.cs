// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Security.Cryptography.Tests;
using Xunit;

namespace SharpKind.Security.Cryptography.Cryptanalysis.Tests;

public class CaesarCryptanalysisTests
{
    [Theory]
    [InlineData("AAAAAAAABBCCCDDDDEEEEEEEEEEEEEFFGGHHHHHHIIIIIIIKLLLLMMNNNNNNNOOOOOOOOPPRRRRRRSSSSSSSSSTTTTTTTTTUUUVWWYY", 0)]
    [InlineData("YMJHFJXFWHNUMJWNXTSJTKYMJJFWQNJXYPSTBSFSIXNRUQJXYHNUMJWX", 5)] // http://practicalcryptography.com/cryptanalysis/stochastic-searching/cryptanalysis-caesar-cipher/
    [InlineData(SinghCiphertexts.Stage2, 7)] // Singh Code Book
    [InlineData("QFM", 12)]
    public void Crack(string ciphertext, int shift)
    {
        (int bestShift, IReadOnlyDictionary<int, string> allDecryptions) = CaesarCryptanalysis.Crack(ciphertext);
        Assert.Equal(shift, bestShift);
        Assert.Equal(26, allDecryptions.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("!!! ...")]
    public void CrackWithoutLetters(string ciphertext)
    {
        (int bestShift, IReadOnlyDictionary<int, string> _) = CaesarCryptanalysis.Crack(ciphertext);
        Assert.Equal(0, bestShift);
    }

    [Fact]
    public void CrackIgnoresSpacingWhenScoring()
    {
        const string spaced = "MHILY LZA ZBHL XBPZXBL MVYABUHL HWWPBZ JSHBKPBZ JHLJBZ KPJABT HYJHUBT LZA ULBAYVU";

        // Frequencies are scored over the letters alone, so removing the spaces cannot change
        // the answer.
        string unspaced = spaced.Replace(" ", string.Empty, StringComparison.Ordinal);

        (int spacedShift, IReadOnlyDictionary<int, string> _) = CaesarCryptanalysis.Crack(spaced);
        (int unspacedShift, IReadOnlyDictionary<int, string> _) = CaesarCryptanalysis.Crack(unspaced);

        Assert.Equal(spacedShift, unspacedShift);
    }
}
