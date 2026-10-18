// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class AffineTests
{
    [Theory]
    [InlineData("AFFINECIPHER", "IHHWVCSWFRCP", 5, 8)]
    [InlineData("HELLO WORLD!", "HELLO WORLD!", 1, 0)]
    [InlineData("ABC", "DEF", 1, 3)] // A multiplier of one is a Caesar shift
    public void EncryptDecrypt(string plaintext, string ciphertext, int multiplier, int shift)
    {
        Affine cipher = new(new AffineSettings() { Multiplier = multiplier, Shift = shift });
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(13)]
    [InlineData(26)]
    public void MultiplierNotCoprime(int multiplier)
        => Assert.Throws<ArgumentOutOfRangeException>(
            "Multiplier",
            () => new AffineSettings() { Multiplier = multiplier });

    [Theory]
    [InlineData(-1)]
    [InlineData(26)]
    public void ShiftOutOfRange(int shift)
        => Assert.Throws<ArgumentOutOfRangeException>("Shift", () => new AffineSettings() { Shift = shift });

    [Fact]
    public void GenerateSettings()
    {
        Affine cipher = new(new AffineSettings());

        const int testsCount = 10;
        (int, int)[] keys = new (int, int)[testsCount];
        for (int i = 0; i < testsCount; i++)
        {
            cipher.GenerateSettings();
            keys[i] = (cipher.Settings.Multiplier, cipher.Settings.Shift);
        }

        Assert.True(keys.Distinct().Count() > 1);
        Assert.All(keys, key => Assert.Contains(key.Item1, AffineSettings.ValidMultipliers));
    }

    [Fact]
    public void Name()
    {
        Affine cipher = new(new AffineSettings());
        Assert.Equal("Affine", cipher.CipherName);
        Assert.Equal("Affine", cipher.ToString());
    }
}
