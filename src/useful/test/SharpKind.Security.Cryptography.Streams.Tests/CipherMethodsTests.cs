// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace SharpKind.Security.Cryptography.Streams.Tests;

public class CipherMethodsTests
{
    [Fact]
    public void StreamsAreLeftOpen()
    {
        using SymmetricAlgorithm cipher = new AtbashSymmetric();
        using MemoryStream input = new(Encoding.Unicode.GetBytes("ABC"));
        using MemoryStream output = new();

        CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, input, output);

        Assert.True(input.CanRead);
        Assert.True(output.CanWrite);
        Assert.Equal("ZYX", Encoding.Unicode.GetString(output.ToArray()));
    }

    [Fact]
    public void SurrogatePairAcrossReads()
    {
        // The stream is read 4096 characters at a time, so a read ends between the two halves of the pair.
        string text = new string('A', 4095) + "\U0001F600B";
        using SymmetricAlgorithm cipher = new AtbashSymmetric();
        using MemoryStream input = new(Encoding.Unicode.GetBytes(text));
        using MemoryStream output = new();

        CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, input, output);

        Assert.Equal(new string('Z', 4095) + "\U0001F600Y", Encoding.Unicode.GetString(output.ToArray()));
    }

    [Fact]
    public void Utf8InputWithByteOrderMark()
    {
        using SymmetricAlgorithm cipher = new AtbashSymmetric();
        using MemoryStream input = new([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("ABC")]);
        using MemoryStream output = new();

        CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, input, output);

        Assert.Equal("ZYX", Encoding.Unicode.GetString(output.ToArray()));
    }

    [Fact]
    public void StringKeepsLeadingByteOrderMarkCharacter()
    {
        using SymmetricAlgorithm cipher = new AtbashSymmetric();
        Assert.Equal("﻿Z", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "﻿A"));
    }

    [Fact]
    public void RejectsNull()
    {
        using SymmetricAlgorithm cipher = new AtbashSymmetric();
        using MemoryStream stream = new();

        Assert.Throws<ArgumentNullException>(
            "input",
            () => CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, null!, stream));
        Assert.Throws<ArgumentNullException>(
            "output",
            () => CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, stream, null!));
        Assert.Throws<ArgumentNullException>(
            "input",
            () => CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, null!));
    }
}
