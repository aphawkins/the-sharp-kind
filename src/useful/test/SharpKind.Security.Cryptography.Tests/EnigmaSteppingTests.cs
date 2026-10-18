// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

/// <summary>
/// The rotor positions after each key press. The expected positions come from a separate implementation of the
/// machine, and the first row is the double step that is often quoted: ADU, ADV, AEW, BFX.
/// </summary>
public class EnigmaSteppingTests
{
    public static TheoryData<string, string, string> Steps => new()
    {
        { "I II III", "ADU", "ADV AEW BFX BFY BFZ BFA" },
        { "I II III", "AEA", "BFB BFC BFD BFE" },
        { "I II III", "ZDV", "ZEW AFX AFY" },
        { "I IV V", "AIY", "AIZ AJA BKB BKC BKD BKE BKF BKG" },
        { "II V III", "AYV", "AZW BAX BAY BAZ BAA BAB BAC BAD" },
        { "II V III", "AZV", "BAW BAX BAY BAZ" },
        { "I VIII III", "AYV", "AZW BAX BAY BAZ BAA BAB BAC BAD" },
        { "IV VI I", "ALP", "ALQ AMR BNS BNT BNU BNV BNW BNX BNY BNZ BNA BNB" },
        { "II I VII", "ADL", "ADM AEN AEO AEP AEQ AER AES AET" },
        { "III II I", "AAA", "AAB AAC AAD AAE AAF" },
    };

    [Theory]
    [MemberData(nameof(Steps))]
    public void AdvanceRotors(string wheels, string start, string expected)
    {
        ArgumentNullException.ThrowIfNull(wheels);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(expected);

        EnigmaRotors rotors = EnigmaFactory.Rotors(wheels, "01 01 01", start);
        List<string> actual = [];

        for (int i = 0; i < expected.Split(' ').Length; i++)
        {
            rotors.AdvanceRotors();
            actual.Add(string.Concat(
                rotors[EnigmaRotorPosition.Third].CurrentSetting,
                rotors[EnigmaRotorPosition.Second].CurrentSetting,
                rotors[EnigmaRotorPosition.Fastest].CurrentSetting));
        }

        Assert.Equal(expected, string.Join(' ', actual));
    }
}
