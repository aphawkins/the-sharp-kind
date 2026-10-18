// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

public class MonoAlphabeticSymmetricTests
{
    public static TheoryData<string, string, string> Data => new()
    {
        { "ABC|BAC", "ABC", "BAC" },
        { "ABCD|BADC", "ABCD", "BADC" },
        { "ABC|BCA", "ABC", "BCA" },
        { "ABC|ABC", "ABC", "ABC" },
        { "ABCD|ABCD", "Å", "Å" },
        { "ABCD|BADC", "AB CD", "BA DC" },
    };

    [Theory]
    [InlineData("")] // No key
    [InlineData("|ABC")] // No character set
    [InlineData("ABC|")] // No substitutions
    [InlineData("ABC|AB")] // Too few subs
    [InlineData("ABC|ABCA")] // Too many subs
    [InlineData(" ABCD|ABCD")] // Character set spacing
    [InlineData("AB CD|ABCD")] // Character set spacing
    [InlineData("ABCD |ABCD")] // Character set spacing
    [InlineData("ABCD| ABCD")] // Subs spacing
    [InlineData("ABCD|ABCD ")] // Subs spacing
    [InlineData("ABCD|AB CD")] // Subs spacing
    [InlineData("ABCD|aBCD")] // Subs incorrect case
    [InlineData("ABCD|AACD")] // Incorrect subs letters
    [InlineData("ABCD")] // Incorrect number of parts
    [InlineData("ABCD|ABCD|")] // Incorrect number of parts
    public void KeyInvalid(string value)
    {
        byte[] keyBytes = Encoding.Unicode.GetBytes(value);
        using SymmetricAlgorithm symmetric = new MonoAlphabeticSymmetric();

        Assert.Throws<ArgumentException>(nameof(value), () => symmetric.Key = keyBytes);
    }

    [Theory]
    [InlineData("ABC|ABC")]
    [InlineData("ABCD|BADC")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ|BADCEFGHIJKLMNOPQRSTUVWXYZ")]
    [InlineData("ØA|ØA")]
    public void KeyValid(string keyString)
    {
        byte[] key = Encoding.Unicode.GetBytes(keyString);
        using SymmetricAlgorithm symmetric = new MonoAlphabeticSymmetric
        {
            Key = key,
        };
        Assert.Equal(key, symmetric.Key);
    }

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ|BADCFEHGIJKLMNOPQRSTUVWXYZ", "HELLOWORLD", "GfLlOwOrLc")]
    public void Decrypt(string key, string plaintext, string ciphertext)
    {
        using SymmetricAlgorithm cipher = new MonoAlphabeticSymmetric
        {
            Key = Encoding.Unicode.GetBytes(key),
        };

        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ|BADCFEHGIJKLMNOPQRSTUVWXYZ", "HeLlOwOrLd", "GFLLOWORLC")]
    public void Encrypt(string key, string plaintext, string ciphertext)
    {
        using SymmetricAlgorithm cipher = new MonoAlphabeticSymmetric
        {
            Key = Encoding.Unicode.GetBytes(key),
        };
        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, plaintext));
    }

    [Fact]
    public void IvGenerateCorrectness()
    {
        using SymmetricAlgorithm cipher = new MonoAlphabeticSymmetric();
        cipher.GenerateIV();
        Assert.Equal([], cipher.IV);
    }

    [Fact]
    public void IvSet()
    {
        using SymmetricAlgorithm cipher = new MonoAlphabeticSymmetric
        {
            IV = Encoding.Unicode.GetBytes("A"),
        };
        Assert.Equal([], cipher.IV);
    }

    [Fact]
    public void KeyGenerateCorrectness()
    {
        using SymmetricAlgorithm cipher = new MonoAlphabeticSymmetric();
        for (int i = 0; i < 100; i++)
        {
            cipher.GenerateKey();
            string[] parts = Encoding.Unicode.GetString(cipher.Key).Split('|');

            // The substitutions are a permutation of the character set.
            Assert.Equal(parts[0], new string([.. parts[1].Order()]));
        }
    }

    [Fact]
    public void KeyGenerateRandomness()
    {
        bool diff = false;

        using (SymmetricAlgorithm cipher = new MonoAlphabeticSymmetric())
        {
            byte[] key;
            byte[] newKey;

            cipher.GenerateKey();
            newKey = cipher.Key;
            key = newKey;

            for (int i = 0; i < 10; i++)
            {
                if (!newKey.SequenceEqual(key))
                {
                    diff = true;
                    break;
                }

                key = newKey;
                cipher.GenerateKey();
                newKey = cipher.Key;
            }
        }

        Assert.True(diff);
    }

    [Fact]
    public void Name()
    {
        using SymmetricAlgorithm cipher = new MonoAlphabeticSymmetric();
        Assert.Equal("MonoAlphabetic", cipher.ToString());
    }
}
