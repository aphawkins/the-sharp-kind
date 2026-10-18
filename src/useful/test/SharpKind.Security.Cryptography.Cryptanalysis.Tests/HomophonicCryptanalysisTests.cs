// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Security.Cryptography.Tests;
using Xunit;

namespace SharpKind.Security.Cryptography.Cryptanalysis.Tests;

public class HomophonicCryptanalysisTests
{
    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    public void CrackTooShortDecrypts(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        IHomophonicSettings settings = HomophonicCryptanalysis.Crack(ciphertext, Language.English);
        Homophonic cipher = new(settings);

        Assert.Equal(ciphertext.Length, cipher.Decrypt(ciphertext).Length);
    }

    [Fact]
    public void CrackIsRepeatable()
    {
        IHomophonicSettings first = HomophonicCryptanalysis.Crack(SinghCiphertexts.Stage3, Language.Italian);
        IHomophonicSettings second = HomophonicCryptanalysis.Crack(SinghCiphertexts.Stage3, Language.Italian);

        Assert.Equal(first, second);
    }
}
