// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Security.Cryptography.Tests;
using Xunit;

namespace SharpKind.Security.Cryptography.Cryptanalysis.Tests;

public class MonoAlphabeticCryptanalysisTests
{
    [Fact]
    public void CrackSinghStage1()
    {
        IMonoAlphabeticSettings settings = MonoAlphabeticCryptanalysis.Crack(SinghCiphertexts.Stage1);
        MonoAlphabetic cipher = new(settings);

        Assert.Equal(SinghPlaintexts.Stage1, cipher.Decrypt(SinghCiphertexts.Stage1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("!!! ...")]
    [InlineData("ABC")]
    public void CrackTooShortGivesIdentity(string ciphertext)
    {
        IMonoAlphabeticSettings settings = MonoAlphabeticCryptanalysis.Crack(ciphertext);

        Assert.Equal(settings.CharacterSet, settings.Substitutions);
    }

    [Fact]
    public void CrackIsRepeatable()
    {
        IMonoAlphabeticSettings first = MonoAlphabeticCryptanalysis.Crack(SinghCiphertexts.Stage1);
        IMonoAlphabeticSettings second = MonoAlphabeticCryptanalysis.Crack(SinghCiphertexts.Stage1);

        Assert.Equal(first.Substitutions, second.Substitutions);
    }
}
