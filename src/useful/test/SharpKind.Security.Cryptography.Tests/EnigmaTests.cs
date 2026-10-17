// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;
using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class EnigmaTests
{
    [Fact]
    public void Name()
    {
        IEnigmaSettings settings = new EnigmaSettings();
        Enigma cipher = new(settings);
        Assert.Equal("Enigma M3", cipher.CipherName);
        Assert.Equal("Enigma M3", cipher.ToString());
    }

    [Theory]
    [InlineData("", "", 'A')]
    [InlineData("HELLOWORLD", "MFNCZBBFZM", 'K')]
    [InlineData("HELLO WORLD", "MFNCZ BBFZM", 'K')]
    [InlineData("HeLlOwOrLd", "MFNCZBBFZM", 'K')]
    [InlineData("Å", "", 'A')]
    public void Encrypt(string plaintext, string ciphertext, char newFastestRotorPosition)
    {
        EnigmaSettings settings = new();
        Enigma cipher = new(settings);
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
        Assert.Equal(newFastestRotorPosition, settings.Rotors[EnigmaRotorPosition.Fastest].CurrentSetting);
    }

    [Theory]
    [InlineData("", "", 'A')]
    [InlineData("HELLOWORLD", "MFNCZBBFZM", 'K')]
    [InlineData("HELLO WORLD", "MFNCZ BBFZM", 'K')]
    [InlineData("HELLOWORLD", "MfNcZbBfZm", 'K')]
    [InlineData("", "Å", 'A')]
    public void Decrypt(string plaintext, string ciphertext, char newFastestRotorPosition)
    {
        EnigmaSettings settings = new();
        Enigma cipher = new(settings);
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
        Assert.Equal(newFastestRotorPosition, settings.Rotors[EnigmaRotorPosition.Fastest].CurrentSetting);
    }

    [Fact]
    public void ResetRewindsTheRotors()
    {
        EnigmaSettings settings = new();
        Enigma cipher = new(settings);

        string ciphertext = cipher.Encrypt("HELLOWORLD");
        Assert.Equal('K', settings.Rotors[EnigmaRotorPosition.Fastest].CurrentSetting);

        cipher.Reset();
        Assert.Equal('A', settings.Rotors[EnigmaRotorPosition.Fastest].CurrentSetting);

        // Having rewound the rotors, the same key round trips the message.
        Assert.Equal("HELLOWORLD", cipher.Decrypt(ciphertext));
    }

    [Fact]
    public void ResetReturnsToTheGeneratedRotorPositions()
    {
        Enigma cipher = new(new EnigmaSettings());
        cipher.GenerateSettings();

        List<char> generated = [.. EnigmaRotors.RotorPositions
            .Select(position => cipher.Settings.Rotors[position].CurrentSetting)];

        string ciphertext = cipher.Encrypt("HELLOWORLD");
        cipher.Reset();

        List<char> afterReset = [.. EnigmaRotors.RotorPositions
            .Select(position => cipher.Settings.Rotors[position].CurrentSetting)];

        Assert.Equal(generated, afterReset);
        Assert.Equal("HELLOWORLD", cipher.Decrypt(ciphertext));
    }

    [Fact]
    public void Enigma19410707T1925()
    {
        string ciphertext = new StringBuilder()
            .Append("EDPUD NRGYS ZRCXN UYTPO MRMBO ")
            .Append("FKTBZ REZKM LXLVE FGUEY SIOZV ")
            .Append("EQMIK UBPMM YLKLT TDEIS MDICA ")
            .Append("GYKUA CTCDO MOHWX MUUIA UBSTS ")
            .Append("LRNBZ SZWNR FXWFY SSXJZ VIJHI ")
            .Append("DISHP RKLKA YUPAD TXQSP INQMA ")
            .Append("TLPIF SVKDA SCTAC DPBOP VHJK")
            .ToString();

        // Reflector: B
        // Wheel order: II IV V
        // Ring positions:  02 21 12  (B U L)
        // Plug pairs: AV BS CG DL FU HZ IN KM OW RX
        // Message key: BLA
        // Final key: BRS
        IEnigmaReflector reflector = new EnigmaReflector() { ReflectorNumber = EnigmaReflectorNumber.B };

        EnigmaRotors rotors = new()
        {
            Rotors = new Dictionary<EnigmaRotorPosition, IEnigmaRotor>()
            {
                {
                    EnigmaRotorPosition.Fastest,
                    new EnigmaRotor()
                    {
                        RotorNumber = EnigmaRotorNumber.V,
                        RingPosition = 12,
                        CurrentSetting = 'A',
                    }
                },
                {
                    EnigmaRotorPosition.Second,
                    new EnigmaRotor()
                    {
                        RotorNumber = EnigmaRotorNumber.IV,
                        RingPosition = 21,
                        CurrentSetting = 'L',
                    }
                },
                {
                    EnigmaRotorPosition.Third,
                    new EnigmaRotor()
                    {
                        RotorNumber = EnigmaRotorNumber.II,
                        RingPosition = 2,
                        CurrentSetting = 'B',
                    }
                },
            },
        };

        IList<EnigmaPlugboardPair> plugs = new List<EnigmaPlugboardPair>
        {
            { new EnigmaPlugboardPair() { From = 'A', To = 'V' } },
            { new EnigmaPlugboardPair() { From = 'B', To = 'S' } },
            { new EnigmaPlugboardPair() { From = 'C', To = 'G' } },
            { new EnigmaPlugboardPair() { From = 'D', To = 'L' } },
            { new EnigmaPlugboardPair() { From = 'F', To = 'U' } },
            { new EnigmaPlugboardPair() { From = 'H', To = 'Z' } },
            { new EnigmaPlugboardPair() { From = 'I', To = 'N' } },
            { new EnigmaPlugboardPair() { From = 'K', To = 'M' } },
            { new EnigmaPlugboardPair() { From = 'O', To = 'W' } },
            { new EnigmaPlugboardPair() { From = 'R', To = 'X' } },
        };
        IEnigmaPlugboard plugboard = new EnigmaPlugboard(plugs);

        string plaintext = new StringBuilder()
            .Append("AUFKL XABTE ILUNG XVONX KURTI ")
            .Append("NOWAX KURTI NOWAX NORDW ESTLX ")
            .Append("SEBEZ XSEBE ZXUAF FLIEG ERSTR ")
            .Append("ASZER IQTUN GXDUB ROWKI XDUBR ")
            .Append("OWKIX OPOTS CHKAX OPOTS CHKAX ")
            .Append("UMXEI NSAQT DREIN ULLXU HRANG ")
            .Append("ETRET ENXAN GRIFF XINFX RGTX")
            .ToString();

        IEnigmaSettings settings = new EnigmaSettings() { Reflector = reflector, Rotors = rotors, Plugboard = plugboard };
        Enigma cipher = new(settings);
        string newPlaintext = cipher.Decrypt(ciphertext);
        Assert.Equal(plaintext, newPlaintext);
        Assert.Equal('S', rotors[EnigmaRotorPosition.Fastest].CurrentSetting);
        Assert.Equal('R', rotors[EnigmaRotorPosition.Second].CurrentSetting);
        Assert.Equal('B', rotors[EnigmaRotorPosition.Third].CurrentSetting);
    }

    [Fact]
    public void PracticalCryptography()
    {
        string ciphertext = new StringBuilder()
            .Append("YXBMXADQBDBAAYIMKDODAYIXNBDQZF")
            .Append("JKOLFVEEQBCLUUXDFVQYGKEYBVRHON")
            .Append("JKPJMKUNLYLZUKBKJOAJTWVWMOMDPG")
            .Append("VXEPUKXBVSGHROFOSBCNKEHEHAKWKO")
            .Append("GWTBZFXSYCGSUUPPIZTRTFVCXZVCXT")
            .Append("FLMTPTAQVMREGWSBFZBM")
            .ToString();

        // Reflector: B
        // Wheel order: II V I
        // Ring positions:  23 15 02  (W O B)
        // Plug pairs: PO ML IU KJ NH YT
        // Message key: KJS
        // Final key: KPG
        IEnigmaReflector reflector = new EnigmaReflector() { ReflectorNumber = EnigmaReflectorNumber.B };

        EnigmaRotors rotors = new()
        {
            Rotors = new Dictionary<EnigmaRotorPosition, IEnigmaRotor>()
            {
                {
                    EnigmaRotorPosition.Fastest,
                    new EnigmaRotor()
                    {
                        RotorNumber = EnigmaRotorNumber.I,
                        RingPosition = 2,
                        CurrentSetting = 'S',
                    }
                },
                {
                    EnigmaRotorPosition.Second,
                    new EnigmaRotor()
                    {
                        RotorNumber = EnigmaRotorNumber.V,
                        RingPosition = 15,
                        CurrentSetting = 'J',
                    }
                },
                {
                    EnigmaRotorPosition.Third,
                    new EnigmaRotor()
                    {
                        RotorNumber = EnigmaRotorNumber.II,
                        RingPosition = 23,
                        CurrentSetting = 'K',
                    }
                },
            },
        };

        IList<EnigmaPlugboardPair> plugs = new List<EnigmaPlugboardPair>()
        {
            { new EnigmaPlugboardPair() { From = 'P', To = 'O' } },
            { new EnigmaPlugboardPair() { From = 'M', To = 'L' } },
            { new EnigmaPlugboardPair() { From = 'I', To = 'U' } },
            { new EnigmaPlugboardPair() { From = 'K', To = 'J' } },
            { new EnigmaPlugboardPair() { From = 'N', To = 'H' } },
            { new EnigmaPlugboardPair() { From = 'Y', To = 'T' } },
        };

        IEnigmaPlugboard plugboard = new EnigmaPlugboard(plugs);

        string plaintext = new StringBuilder()
            .Append("THEENIGMACIPHERWASAFIELDCIPHER")
            .Append("USEDBYTHEGERMANSDURINGWORLDWAR")
            .Append("IITHEENIGMAISONEOFTHEBETTERKNO")
            .Append("WNHISTORICALENCRYPTIONMACHINES")
            .Append("ANDITACTUALLYREFERSTOARANGEOFS")
            .Append("IMILARCIPHERMACHINES")
            .ToString();

        IEnigmaSettings settings = new EnigmaSettings() { Reflector = reflector, Rotors = rotors, Plugboard = plugboard };
        Enigma cipher = new(settings);
        string newPlaintext = cipher.Decrypt(ciphertext);
        Assert.Equal(plaintext, newPlaintext);
        Assert.Equal('G', rotors[EnigmaRotorPosition.Fastest].CurrentSetting);
        Assert.Equal('P', rotors[EnigmaRotorPosition.Second].CurrentSetting);
        Assert.Equal('K', rotors[EnigmaRotorPosition.Third].CurrentSetting);
    }
}
