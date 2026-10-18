// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

public class ReflectorSymmetricTests
{
    public static TheoryData<string, string, string> Data => new()
    {
        { "ABC|ABC", "ABC", "ABC" }, // Default settings
        { "ABC|BAC", "ABC", "BAC" }, // Simple substitution
        { "ABCD|BADC", "ABCD", "BADC" }, // All characters subtituted
        { "ABC|ABC", "Å", "Å" }, // Invalid character
        { "ABC|BAC", "AB C", "BA C" }, // Space
    };

    [Fact]
    public void CtorSettings()
    {
        byte[] defaultKey = Encoding.Unicode.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ|ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        using ReflectorSymmetric cipher = new();
        Assert.Equal(defaultKey, cipher.Key);
        Assert.Equal([], cipher.IV);
    }

    [Fact]
    public void KeySet()
    {
        using ReflectorSymmetric cipher = new();
        byte[] key = Encoding.Unicode.GetBytes("ABC|ABC");
        cipher.Key = key;
        Assert.Equal(key, cipher.Key);
    }

    [Fact]
    public void IvSet()
    {
        using ReflectorSymmetric cipher = new();
        cipher.IV = Encoding.Unicode.GetBytes("A");
        Assert.Equal([], cipher.IV);
    }

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ|BADCFEHGIJKLMNOPQRSTUVWXYZ", "HELLOWORLD", "GfLlOwOrLc")]
    public void Decrypt(string key, string plaintext, string ciphertext)
    {
        using SymmetricAlgorithm cipher = new ReflectorSymmetric
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
        using SymmetricAlgorithm cipher = new ReflectorSymmetric
        {
            Key = Encoding.Unicode.GetBytes(key),
        };
        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, plaintext));
    }

    [Fact]
    public void IvGenerateCorrectness()
    {
        using ReflectorSymmetric cipher = new();
        cipher.GenerateIV();
        Assert.Equal([], cipher.IV);
    }

    [Theory]
    [InlineData("")] // No key
    [InlineData("ABC")] // Incorrect number of parts
    [InlineData("ABC|ABC|")] // Incorrect number of parts
    [InlineData("ABC|AB")] // Too few subs
    [InlineData("ABC|BCA")] // Subs non-reflective
    public void KeyInvalid(string value)
    {
        byte[] key = Encoding.Unicode.GetBytes(value);
        using ReflectorSymmetric cipher = new();
        Assert.Throws<ArgumentException>(nameof(value), () => cipher.Key = key);
    }

    [Fact]
    public void KeyGenerateCorrectness()
    {
        using ReflectorSymmetric cipher = new();
        for (int i = 0; i < 100; i++)
        {
            cipher.GenerateKey();
            string[] parts = Encoding.Unicode.GetString(cipher.Key).Split('|');

            // Every letter swaps with another, and the swap goes both ways.
            for (int letter = 0; letter < parts[0].Length; letter++)
            {
                char substitution = parts[1][letter];
                Assert.NotEqual(parts[0][letter], substitution);
                Assert.Equal(parts[0][letter], parts[1][parts[0].IndexOf(substitution, StringComparison.Ordinal)]);
            }
        }
    }

    [Fact]
    public void KeyGenerateRandomness()
    {
        bool diff = false;

        using (ReflectorSymmetric cipher = new())
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
        using ReflectorSymmetric cipher = new();
        Assert.Equal("Reflector", cipher.ToString());
    }
}
