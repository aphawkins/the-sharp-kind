// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Security.Cryptography.Tests;
using Xunit;

namespace SharpKind.Security.Cryptography.Cryptanalysis.Tests;

/// <summary>
/// Simon Singh's 'The Code Book' cipher challenge, cracked with no key.
/// </summary>
/// <remarks>
/// Stages 1 to 3 are cracked. A cracked key cannot recover letters absent from the plaintext,
/// so the stages are checked by their decryption, not by their key.
/// </remarks>
public class SinghCipherChallengeCryptanalysisTests
{
    [Fact]
    public void Stage1MonoAlphabetic()
    {
        IMonoAlphabeticSettings settings = MonoAlphabeticCryptanalysis.Crack(SinghCiphertexts.Stage1);
        MonoAlphabetic cipher = new(settings);

        Assert.Equal(SinghPlaintexts.Stage1, cipher.Decrypt(SinghCiphertexts.Stage1));
    }

    [Fact]
    public void Stage2Caesar()
    {
        (int bestShift, IReadOnlyDictionary<int, string> allDecryptions) = CaesarCryptanalysis.Crack(SinghCiphertexts.Stage2);

        Assert.Equal(SinghKeys.Stage2Shift, bestShift);
        Assert.Equal(SinghPlaintexts.Stage2, allDecryptions[bestShift]);
    }

    [Fact]
    public void Stage3Homophonic()
    {
        IHomophonicSettings settings = HomophonicCryptanalysis.Crack(SinghCiphertexts.Stage3, Language.Italian);
        Homophonic cipher = new(settings);

        Assert.Equal(SinghPlaintexts.Stage3, cipher.Decrypt(SinghCiphertexts.Stage3));
    }
}
