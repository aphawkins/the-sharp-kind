// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

public class CaesarSymmetricTests
{
    public static TheoryData<string, string, int> Data => new()
    {
        { "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", 0 },
        { "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "DEFGHIJKLMNOPQRSTUVWXYZABC", 3 },
        { ">?@ [\\]", ">?@ [\\]", 3 },
        { "Å", "Å", 3 },
    };

    [Theory]
    [InlineData(1)]
    public void Ctor(int rightShift)
    {
        byte[] key = Encoding.Unicode.GetBytes($"{rightShift}");
        using SymmetricAlgorithm cipher = new CaesarSymmetric();
        Assert.Equal(key, cipher.Key);
        Assert.Equal([], cipher.IV);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(25)]
    public void KeyInRange(int rightShift)
    {
        byte[] key = Encoding.Unicode.GetBytes($"{rightShift}");
        using SymmetricAlgorithm cipher = new CaesarSymmetric
        {
            Key = key,
        };

        Assert.Equal(key, cipher.Key);
        Assert.Equal([], cipher.IV);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("26")]
    public void KeyOutOfRange(string rightShift)
    {
        byte[] key = Encoding.Unicode.GetBytes(rightShift);
        using SymmetricAlgorithm cipher = new CaesarSymmetric();
        Assert.Throws<ArgumentOutOfRangeException>("key", () => cipher.Key = key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Hello World")]
    public void KeyIncorrectFormat(string rightShift)
    {
        byte[] key = Encoding.Unicode.GetBytes(rightShift);
        using SymmetricAlgorithm cipher = new CaesarSymmetric();
        Assert.Throws<ArgumentException>("key", () => cipher.Key = key);
    }

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "defghijklmnopqrstuvwxyzabc", 3)]
    public void Decrypt(string plaintext, string ciphertext, int rightShift)
    {
        using SymmetricAlgorithm cipher = new CaesarSymmetric
        {
            Key = Encoding.Unicode.GetBytes($"{rightShift}"),
        };
        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("abcdefghijklmnopqrstuvwxyz", "DEFGHIJKLMNOPQRSTUVWXYZABC", 3)]
    public void Encrypt(string plaintext, string ciphertext, int rightShift)
    {
        using SymmetricAlgorithm cipher = new CaesarSymmetric
        {
            Key = Encoding.Unicode.GetBytes($"{rightShift}"),
        };
        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, plaintext));
    }

    [Fact]
    public void IvGenerateCorrectness()
    {
        using SymmetricAlgorithm cipher = new CaesarSymmetric();
        cipher.GenerateIV();
        Assert.Equal([], cipher.IV);
    }

    [Fact]
    public void IvSet()
    {
        using SymmetricAlgorithm cipher = new CaesarSymmetric
        {
            IV = Encoding.Unicode.GetBytes("A"),
        };
        Assert.Equal([], cipher.IV);
    }

    [Fact]
    public void KeyGenerateCorrectness()
    {
        using SymmetricAlgorithm cipher = new CaesarSymmetric();
        string keyString;
        for (int i = 0; i < 100; i++)
        {
            cipher.GenerateKey();
            keyString = Encoding.Unicode.GetString(cipher.Key);
            Assert.True(int.TryParse(keyString, out int key));
            Assert.True(key is >= 0 and < 26);
        }
    }

    [Fact]
    public void KeyGenerateRandomness()
    {
        bool diff = false;

        using (SymmetricAlgorithm cipher = new CaesarSymmetric())
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
    public void KeySet()
    {
        using SymmetricAlgorithm cipher = new CaesarSymmetric();
        byte[] key = Encoding.Unicode.GetBytes("7");
        cipher.Key = key;
        Assert.Equal(key, cipher.Key);
    }

    [Fact]
    public void Name()
    {
        using SymmetricAlgorithm cipher = new CaesarSymmetric();
        Assert.Equal("Caesar", cipher.ToString());
    }
}
