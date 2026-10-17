// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;
using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class UuDecoderTests
{
    [Theory]
    [InlineData("#0V%T", "Cat")]
    [InlineData("#0V%T\n`\nend", "Cat")] // Terminator
    [InlineData("begin 644 cat.txt\n#0V%T\n`\nend\n", "Cat")] // Header
    [InlineData("#0V%T\r\n", "Cat")] // Windows line ending
    [InlineData("\"0V$`", "Ca")] // Short final group
    [InlineData("!0P  ", "C")] // Short final group
    [InlineData("!0P", "C")] // Trailing spaces lost
    [InlineData("", "")]
    public void Decode(string uuencoded, string expected)
        => Assert.Equal(expected, Encoding.ASCII.GetString(UuDecoder.Decode(uuencoded)));

    [Fact]
    public void DecodeRejectsNull()
        => Assert.Throws<ArgumentNullException>(() => UuDecoder.Decode(null!));
}
