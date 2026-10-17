// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

public class BookCipherSymmetricTests
{
    [Fact]
    public void Ctor()
    {
        using SymmetricAlgorithm cipher = new BookCipherSymmetric();
        Assert.Equal(Encoding.Unicode.GetBytes("A"), cipher.Key);
        Assert.Equal([], cipher.IV);
    }

    [Fact]
    public void KeyValid()
    {
        byte[] key = Encoding.Unicode.GetBytes("Hello, World!");
        using SymmetricAlgorithm cipher = new BookCipherSymmetric { Key = key };
        Assert.Equal(key, cipher.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123 !?")]
    public void KeyInvalid(string value)
    {
        byte[] key = Encoding.Unicode.GetBytes(value);
        using SymmetricAlgorithm cipher = new BookCipherSymmetric();
        Assert.Throws<ArgumentException>(nameof(value), () => cipher.Key = key);
    }

    [Fact]
    public void EncryptDecrypt()
    {
        using SymmetricAlgorithm cipher = new BookCipherSymmetric { Key = Encoding.Unicode.GetBytes("Hello, World!") };

        Assert.Equal("1 2 3 3 5", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "HELLO"));
        Assert.Equal("HELLO", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, "1 2 3 3 5"));
    }

    [Fact]
    public void Name()
    {
        using SymmetricAlgorithm cipher = new BookCipherSymmetric();
        Assert.Equal("Book", cipher.ToString());
    }
}
