// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Tests;

public partial class EnigmaSymmetricTests
{
    [Fact]
    public void KeyIvDefault()
    {
        using SymmetricAlgorithm cipher = new EnigmaSymmetric();
        Assert.Equal("B|III II I|01 01 01|", Encoding.Unicode.GetString(cipher.Key));
        Assert.Equal("A A A", Encoding.Unicode.GetString(cipher.IV));
    }

    [Theory]
    [InlineData("", "", "B|III II I|01 01 01|", "A A A")]
    [InlineData("HELLOWORLD", "MFNCZBBFZM", "B|III II I|01 01 01|", "A A K")]
    [InlineData("HELLO WORLD", "MFNCZ BBFZM", "B|III II I|01 01 01|", "A A K")]
    [InlineData("HeLlOwOrLd", "MFNCZBBFZM", "B|III II I|01 01 01|", "A A K")]
    [InlineData("Å", "", "B|III II I|01 01 01|", "A A A")]
    public void EncryptCtor(string plaintext, string ciphertext, string newKey, string newIV)
    {
        using SymmetricAlgorithm target = new EnigmaSymmetric();
        string s = CipherMethods.SymmetricTransform(target, CipherTransformMode.Encrypt, plaintext);
        Assert.Equal(ciphertext, s);
        Assert.Equal(newKey, Encoding.Unicode.GetString(target.Key));
        Assert.Equal(newIV, Encoding.Unicode.GetString(target.IV));
    }

    [Theory]
    [InlineData("", "", "B|III II I|01 01 01|", "A A A")]
    [InlineData("HELLOWORLD", "MFNCZBBFZM", "B|III II I|01 01 01|", "A A K")]
    [InlineData("HELLO WORLD", "MFNCZ BBFZM", "B|III II I|01 01 01|", "A A K")]
    [InlineData("HELLOWORLD", "MfNcZbBfZm", "B|III II I|01 01 01|", "A A K")]
    [InlineData("", "Å", "B|III II I|01 01 01|", "A A A")]
    public void DecryptCtor(string plaintext, string ciphertext, string newKey, string newIV)
    {
        using SymmetricAlgorithm target = new EnigmaSymmetric();
        string s = CipherMethods.SymmetricTransform(target, CipherTransformMode.Decrypt, ciphertext);
        Assert.Equal(plaintext, s);
        Assert.Equal(newKey, Encoding.Unicode.GetString(target.Key));
        Assert.Equal(newIV, Encoding.Unicode.GetString(target.IV));
    }

    [Theory]
    [InlineData("A", "F", "B|III II I|01 01 01|", "A A A", "A A B")] // Default
    [InlineData("A", "G", "B|III II I|01 01 01|", "A A E", "A A F")] // Change Setting
    [InlineData("A", "J", "B|III II I|01 01 12|", "A A A", "A A B")] // Change Ring
    [InlineData("A", "C", "B|III I II|01 01 01|", "A A A", "A A B")] // Change Rotor
    [InlineData("A", "H", "B|II I III|01 01 01|", "A A A", "A A B")] // Change Rotor
    [InlineData("A", "P", "B|III II IV|01 01 01|", "A A A", "A A B")] // Change Rotor
    [InlineData("A", "U", "B|III II V|01 01 01|", "A A A", "A A B")] // Change Rotor
    [InlineData("A", "W", "B|III II VI|01 01 01|", "A A A", "A A B")] // Change Rotor
    [InlineData("A", "B", "B|III II V|01 01 12|", "A A A", "A A B")] // Change Rotor + Ring
    [InlineData("A", "N", "B|III II V|01 01 12|", "A A E", "A A F")] // Change Rotor + Ring + Setting
    [InlineData("HELLOWORLD", "VKHWQLADBN", "B|III II I|02 02 02|", "A A A", "A A K")]
    [InlineData("A", "M", "B|III II I|01 01 01|", "K D Q", "K E R")] // Notch - single step
    [InlineData("A", "H", "B|III II I|01 01 01|", "K E R", "L F S")] // Doublestep the middle rotor here
    [InlineData("A", "J", "B|III II I|01 01 01|", "L F S", "L F T")] // Notch - single step
    [InlineData("HELLOWORLD", "ZFZEFSQZDU", "B|III II I|01 01 01|AB CD EF GH IJ KL MN OP QR ST UV WX YZ", "A A A", "A A K")] // Complex
    [InlineData("B", "I", "B|II V I|23 15 02|HN IU JK LM OP TY", "K K R", "K K S")] // Bugfix
    public void EncryptSettings(string plaintext, string ciphertext, string key, string iv, string newIV)
    {
        using SymmetricAlgorithm target = new EnigmaSymmetric
        {
            Key = Encoding.Unicode.GetBytes(key),
            IV = Encoding.Unicode.GetBytes(iv),
        };
        string s = CipherMethods.SymmetricTransform(target, CipherTransformMode.Encrypt, plaintext);
        Assert.Equal(key, Encoding.Unicode.GetString(target.Key));
        Assert.Equal(newIV, Encoding.Unicode.GetString(target.IV));
        Assert.Equal(ciphertext, s);
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
        const string keyString = "B|II IV V|02 21 12|AV BS CG DL FU HZ IN KM OW RX";
        const string initVector = "B L A";
        const string newIv = "B R S";

        string plaintext = new StringBuilder()
            .Append("AUFKL XABTE ILUNG XVONX KURTI ")
            .Append("NOWAX KURTI NOWAX NORDW ESTLX ")
            .Append("SEBEZ XSEBE ZXUAF FLIEG ERSTR ")
            .Append("ASZER IQTUN GXDUB ROWKI XDUBR ")
            .Append("OWKIX OPOTS CHKAX OPOTS CHKAX ")
            .Append("UMXEI NSAQT DREIN ULLXU HRANG ")
            .Append("ETRET ENXAN GRIFF XINFX RGTX")
            .ToString();

        using SymmetricAlgorithm target = new EnigmaSymmetric
        {
            Key = Encoding.Unicode.GetBytes(keyString),
            IV = Encoding.Unicode.GetBytes(initVector),
        };
        string s = CipherMethods.SymmetricTransform(target, CipherTransformMode.Decrypt, ciphertext);
        Assert.Equal(plaintext, s);
        Assert.Equal(keyString, Encoding.Unicode.GetString(target.Key));
        Assert.Equal(newIv, Encoding.Unicode.GetString(target.IV));
    }

    [Fact]
    public void IvGenerateCorrectness()
    {
        using SymmetricAlgorithm cipher = new EnigmaSymmetric();
        string initVector;
        for (int i = 0; i < 100; i++)
        {
            cipher.GenerateIV();
            initVector = Encoding.Unicode.GetString(cipher.IV);

            // Test IV correctness here
            Assert.NotNull(initVector);
        }
    }

    [Fact]
    public void IvGenerateRandomness()
    {
        bool diff = false;

        using (SymmetricAlgorithm cipher = new EnigmaSymmetric())
        {
            byte[] iv;
            byte[] newIv;

            cipher.GenerateIV();
            newIv = cipher.IV;
            iv = newIv;

            for (int i = 0; i < 10; i++)
            {
                if (!newIv.SequenceEqual(iv))
                {
                    diff = true;
                    break;
                }

                iv = newIv;
                cipher.GenerateIV();
                newIv = cipher.IV;
            }
        }

        Assert.True(diff);
    }

    [Fact]
    public void IvSet()
    {
        using SymmetricAlgorithm cipher = new EnigmaSymmetric();
        byte[] iv = Encoding.Unicode.GetBytes("A B C");
        cipher.IV = iv;
        Assert.Equal(iv, cipher.IV);
    }

    [Fact]
    public void KeyGenerateCorrectness()
    {
        using SymmetricAlgorithm cipher = new EnigmaSymmetric();
        string keyString;
        for (int i = 0; i < 100; i++)
        {
            cipher.GenerateKey();
            keyString = Encoding.Unicode.GetString(cipher.Key);

            // Test key correctness here
            Assert.NotNull(keyString);
        }
    }

    [Fact]
    public void KeyGenerateRandomness()
    {
        bool diff = false;

        using (SymmetricAlgorithm cipher = new EnigmaSymmetric())
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
        using SymmetricAlgorithm cipher = new EnigmaSymmetric();
        byte[] key = Encoding.Unicode.GetBytes("B|I II III|01 02 03|AB CD");
        cipher.Key = key;
        Assert.Equal(key, cipher.Key);
    }

    [Fact]
    public void Name()
    {
        using SymmetricAlgorithm cipher = new EnigmaSymmetric();
        Assert.Equal("Enigma M3", cipher.ToString());
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
        const string keyString = "B|II V I|23 15 02|HN IU JK LM OP TY";
        const string initVector = "K J S";
        const string newIv = "K P G";

        string plaintext = new StringBuilder()
            .Append("THEENIGMACIPHERWASAFIELDCIPHER")
            .Append("USEDBYTHEGERMANSDURINGWORLDWAR")
            .Append("IITHEENIGMAISONEOFTHEBETTERKNO")
            .Append("WNHISTORICALENCRYPTIONMACHINES")
            .Append("ANDITACTUALLYREFERSTOARANGEOFS")
            .Append("IMILARCIPHERMACHINES")
            .ToString();

        using SymmetricAlgorithm target = new EnigmaSymmetric()
        {
            Key = Encoding.Unicode.GetBytes(keyString),
            IV = Encoding.Unicode.GetBytes(initVector),
        };
        string s = CipherMethods.SymmetricTransform(target, CipherTransformMode.Decrypt, ciphertext);
        Assert.Equal(plaintext, s);
        Assert.Equal(keyString, Encoding.Unicode.GetString(target.Key));
        Assert.Equal(newIv, Encoding.Unicode.GetString(target.IV));
    }
}
