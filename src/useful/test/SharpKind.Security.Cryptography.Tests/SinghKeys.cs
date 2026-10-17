// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;

namespace SharpKind.Security.Cryptography.Tests;

/// <summary>
/// The keys of Simon Singh's 'The Code Book' cipher challenge, shared by the cipher and stream tests.
/// </summary>
internal static class SinghKeys
{
    /// <summary>
    /// Gets the stage 1 character set.
    /// </summary>
    internal const string Stage1Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// Gets the stage 1 substitutions: the ciphertext letter for each letter of <see cref="Stage1Alphabet"/>.
    /// </summary>
    internal const string Stage1Substitutions = "MFANXIWPBSHGLTCQKVRJUDYZEO";

    /// <summary>
    /// Gets the stage 2 shift.
    /// </summary>
    internal const int Stage2Shift = 7;

    /// <summary>
    /// Gets the stage 4 keyword.
    /// </summary>
    internal const string Stage4Keyword = "SCUBA";

    /// <summary>
    /// Gets Fermat's marginal note, which is the key text of stage 5. The letters are counted, not the words.
    /// </summary>
    internal const string Stage5KeyText =
        "Cubem autem in duos cubos, aut quadratoquadratum in duos quadratoquadratos, et generaliter nullam " +
            "in infinitum ultra quadratum potestatem in duos eiusdem nominis fas est dividere. " +
            "Cuius rei demonstrationem mirabilem sane detexi hanc marginis exiguitas non caperet.";

    /// <summary>
    /// Gets the stage 6 keyword, which is the key square read row by row: NKEVI RHCTO GBALU PDMFS ZQWXY.
    /// </summary>
    internal const string Stage6Keyword = "NKEVIRHCTOGBALUPDMFSZQWXY";

    /// <summary>
    /// Gets the stage 7 labels. Their order only names the rows and columns of the square.
    /// </summary>
    internal const string Stage7Labels = "CEMOPRTU";

    /// <summary>
    /// Gets the stage 7 square, row by row, which holds both cases of the letters, the digits, a space and a full stop.
    /// Eight cells are in no ciphertext digraph, so their characters are a guess.
    /// </summary>
    internal const string Stage7Square = "5QTIcH6OVstWyFbwaXj1kf.lJSKAZY0iUMmLnGoxEzr4Dh7PevB2ug83dN9qCRp ";

    /// <summary>
    /// Gets a stage 7 keyword that gives Singh's column order. His own keyword is not known.
    /// </summary>
    internal const string Stage7Keyword = "BCDEFGHA";

    /// <summary>
    /// Gets the stage 8 reflector.
    /// </summary>
    internal const string Stage8Reflector = "B";

    /// <summary>
    /// Gets the stage 8 wheels, left to right. Singh's figure shows the first two the wrong way round.
    /// </summary>
    internal const string Stage8Wheels = "III II I";

    /// <summary>
    /// Gets the stage 8 ring positions, in the order of <see cref="Stage8Wheels"/>.
    /// </summary>
    internal const string Stage8Rings = "01 06 01";

    /// <summary>
    /// Gets the stage 8 plugboard pairs.
    /// </summary>
    internal const string Stage8Plugs = "AS EI JN KL MU OT";

    /// <summary>
    /// Gets the stage 8 message key: the wheel settings at the start, in the order of <see cref="Stage8Wheels"/>.
    /// </summary>
    internal const string Stage8MessageKey = "A L L";

    /// <summary>
    /// Gets the wheel settings at the end of the stage 8 message, in the order of <see cref="Stage8Wheels"/>.
    /// </summary>
    internal const string Stage8FinalKey = "A A R";

    /// <summary>
    /// Gets the stage 9 DES key, as hexadecimal. The Cipher Challenge solvers found it by brute force.
    /// </summary>
    internal const string Stage9DesKey = "d3cd1694cba126fe";

    /// <summary>
    /// Gets the stage 10 RSA public exponent.
    /// </summary>
    internal const long Stage10Exponent = 3735928559;

    /// <summary>
    /// Gets the stage 10 DES key, as hexadecimal. It is the first half of the key that the RSA message holds.
    /// </summary>
    internal const string Stage10DesKey = "462C6BAD381031AD";

    /// <summary>
    /// Gets the stage 10 RSA modulus, which is in the stage 9 plaintext.
    /// </summary>
    internal const string Modulus = """""
1074278829126656590717841127994211661266392179475329458
8877817210355464150980121879033832926235281090750672083
504941996433143425558334401855808989426892463
""""";

    /// <summary>
    /// Gets one factor of the stage 10 RSA modulus. Singh's solvers found it by factoring the modulus.
    /// </summary>
    internal const string Factor = """""
1284420516538103149166225902897755319896498432391586436
8216177647043137765477
""""";

    /// <summary>
    /// Gets the stage 3 key. Each letter has the one symbol, apart from A, which has six, and the space is X.
    /// The key was recovered by lining the ciphertext up with Dante's text.
    /// </summary>
    internal static IReadOnlyDictionary<char, IReadOnlyList<string>> Stage3Homophones { get; } = new Dictionary<char, IReadOnlyList<string>>
    {
        [' '] = ["X"],
        ['A'] = ["M", "B", "C", "G", "N", "Z"],
        ['B'] = ["A"],
        ['C'] = ["S"],
        ['D'] = ["L"],
        ['E'] = ["Q"],
        ['F'] = ["D"],
        ['G'] = ["J"],
        ['H'] = ["O"],
        ['I'] = ["F"],
        ['L'] = ["*"],
        ['M'] = ["W"],
        ['N'] = ["T"],
        ['O'] = ["I"],
        ['P'] = ["Y"],
        ['Q'] = ["R"],
        ['R'] = ["V"],
        ['S'] = ["E"],
        ['T'] = ["U"],
        ['U'] = ["P"],
        ['V'] = ["K"],
        ['Z'] = ["H"],
    };

    /// <summary>
    /// Gets the stage 8 Enigma settings, built from the strings above.
    /// </summary>
    /// <returns>The settings.</returns>
    internal static EnigmaSettings Stage8Enigma()
    {
        EnigmaRotorPosition[] positions = [EnigmaRotorPosition.Third, EnigmaRotorPosition.Second, EnigmaRotorPosition.Fastest];
        string[] wheels = Stage8Wheels.Split(' ');
        string[] rings = Stage8Rings.Split(' ');
        string[] starts = Stage8MessageKey.Split(' ');
        Dictionary<EnigmaRotorPosition, IEnigmaRotor> rotors = [];

        for (int i = 0; i < positions.Length; i++)
        {
            rotors[positions[i]] = new EnigmaRotor()
            {
                RotorNumber = Enum.Parse<EnigmaRotorNumber>(wheels[i]),
                RingPosition = int.Parse(rings[i], CultureInfo.InvariantCulture),
                CurrentSetting = starts[i][0],
            };
        }

        List<EnigmaPlugboardPair> plugs =
            [.. Stage8Plugs.Split(' ').Select(static pair => new EnigmaPlugboardPair() { From = pair[0], To = pair[1] })];

        return new EnigmaSettings()
        {
            Reflector = new EnigmaReflector() { ReflectorNumber = Enum.Parse<EnigmaReflectorNumber>(Stage8Reflector) },
            Rotors = new EnigmaRotors() { Rotors = rotors },
            Plugboard = new EnigmaPlugboard(plugs),
        };
    }
}
