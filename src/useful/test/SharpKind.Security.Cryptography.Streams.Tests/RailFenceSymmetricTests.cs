// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

public class RailFenceSymmetricTests
{
    [Fact]
    public void Ctor()
    {
        using SymmetricAlgorithm cipher = new RailFenceSymmetric();
        Assert.Equal(Encoding.Unicode.GetBytes("2"), cipher.Key);
        Assert.Equal([], cipher.IV);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("25")]
    public void KeyValid(string keyString)
    {
        byte[] key = Encoding.Unicode.GetBytes(keyString);
        using SymmetricAlgorithm cipher = new RailFenceSymmetric { Key = key };
        Assert.Equal(key, cipher.Key);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("26")]
    public void KeyInvalid(string keyString)
    {
        byte[] key = Encoding.Unicode.GetBytes(keyString);
        using SymmetricAlgorithm cipher = new RailFenceSymmetric();
        Assert.Throws<ArgumentOutOfRangeException>("key", () => cipher.Key = key);
    }

    [Fact]
    public void EncryptDecrypt()
    {
        const string plaintext = "WE ARE DISCOVERED. FLEE AT ONCE";
        const string ciphertext = "WE CRL TEERDSOEEF. EAOC AI VDEN";

        using SymmetricAlgorithm cipher = new RailFenceSymmetric { Key = Encoding.Unicode.GetBytes("3") };

        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, plaintext));
        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void GenerateKey()
    {
        using SymmetricAlgorithm cipher = new RailFenceSymmetric();

        cipher.GenerateKey();

        Assert.InRange(int.Parse(Encoding.Unicode.GetString(cipher.Key), System.Globalization.CultureInfo.InvariantCulture), 2, 25);
        string ciphertext = CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "Hello, World!");
        Assert.Equal("HELLO, WORLD!", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void Name()
    {
        using SymmetricAlgorithm cipher = new RailFenceSymmetric();
        Assert.Equal("Rail Fence", cipher.ToString());
    }
}
