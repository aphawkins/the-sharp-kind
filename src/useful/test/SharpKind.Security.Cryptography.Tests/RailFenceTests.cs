// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class RailFenceTests
{
    [Theory]
    [InlineData("WEAREDISCOVEREDFLEEATONCE", "WECRLTEERDSOEEFEAOCAIVDEN", 3)]
    [InlineData("HELLOWORLD", "HLOOLELWRD", 2)]
    [InlineData("HELLO, WORLD!", "HLOOL, ELWRD!", 2)] // Non-letters stay where they are
    [InlineData("ABC", "ABC", 25)] // More rails than letters reads straight down
    public void EncryptDecrypt(string plaintext, string ciphertext, int rails)
    {
        RailFence cipher = new(new RailFenceSettings() { Rails = rails });
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(26)]
    public void RailsOutOfRange(int rails)
        => Assert.Throws<ArgumentOutOfRangeException>("Rails", () => new RailFenceSettings() { Rails = rails });

    [Fact]
    public void GenerateSettings()
    {
        RailFence cipher = new(new RailFenceSettings());

        const int testsCount = 10;
        int[] rails = new int[testsCount];
        for (int i = 0; i < testsCount; i++)
        {
            cipher.GenerateSettings();
            rails[i] = cipher.Settings.Rails;
        }

        Assert.True(rails.Distinct().Count() > 1);
        Assert.All(rails, rail => Assert.InRange(rail, 2, RailFenceSettings.MaxRails));
    }

    [Fact]
    public void Name()
    {
        RailFence cipher = new(new RailFenceSettings());
        Assert.Equal("Rail Fence", cipher.CipherName);
        Assert.Equal("Rail Fence", cipher.ToString());
    }
}
