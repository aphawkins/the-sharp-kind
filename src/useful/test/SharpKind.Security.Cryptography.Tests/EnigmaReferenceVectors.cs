// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

/// <summary>
/// Enigma settings and the text they give, from a separate implementation of the machine. The settings make the
/// rotors step in the awkward ways: a middle rotor that starts on its notch, double steps, notches at Z and M,
/// and the slowest rotor stepping when the middle rotor wraps from Z to A. Shared by the cipher and stream tests.
/// </summary>
#pragma warning disable CA1515 // xUnit's MemberData needs a public member.
public static class EnigmaReferenceVectors
{
    /// <summary>
    /// Gets the reflector, wheels, rings, plugs, start, plaintext and ciphertext of each machine.
    /// </summary>
    public static TheoryData<string, string, string, string, string, string, string> Vectors => new()
    {
        // Middle rotor II starts on its notch
        {
            "B",
            "I II III",
            "01 01 01",
            string.Empty,
            "A E A",
            "DKQQUDHTTRNZSRXYYPYYSOHATCDJDOAVPVKGMILLZMXQUCXKCRRJJOEUWWSJ",
            "MBNMWNLJFDFIEVCQOSCMKEECVAVSSPDSYAUQGPPQKZPILNLESDVANBUFGYHL"
        },

        // Middle rotor I starts on its notch
        {
            "B",
            "III I II",
            "03 09 20",
            "AC",
            "K Q M",
            "AWLLONCMSRPDNQZYTPMQINSPQQZASHYEBWQVDWTNPEPHDUWOVPWRIRMVJRZU",
            "NAIIJRKLKOGVOBASNMAOYOIWEKJNYNOXUMTYEMNOLOFGTVAYEMLCYHDKCWXB"
        },

        // Middle rotor IV starts on its notch
        {
            "C",
            "II IV I",
            "05 12 26",
            string.Empty,
            "R J T",
            "QWPZMNGJIABEUTOZQPLGIWPOQYPZNRVBJKOZDOWWZQIOFNXPCUIPLDGXNEOA",
            "PUTGLWEWMGLNLZVJUOFDBFZLBDWBKHAWMODIJYTURJBSCTAJVRPGXIUIDASN"
        },

        // Middle rotor V starts on its notch
        {
            "B",
            "I V III",
            "01 01 01",
            string.Empty,
            "A Z A",
            "CAOSRCTZGHDTBYRIVYULEAKVSSYPWXERHPSETWQJCNVFEZZSJQNMZEQZGMAD",
            "KGKULQGNWJSSNSQVGXTDHEQEEEQZOFCMUJLMNIMUTFIXXHBZAPAPXGVTYFVU"
        },

        // Middle rotor VI starts on a notch at Z
        {
            "B",
            "II VI I",
            "07 14 21",
            string.Empty,
            "C Z H",
            "MCBLULYAAQPKVJOKDPOSTZFESQGNTKOTHVWCRMBGUFBURBOYUGPWVAEUQGZQ",
            "DILQNUQSSMZUROETXAGYHKZWAHDLZCGYXWAXHOSLIHMYXOTUEAUUWUFKIVAH"
        },

        // Middle rotor VII starts on a notch at M
        {
            "C",
            "IV VII V",
            "02 03 04",
            string.Empty,
            "D M N",
            "EFVFMINRNCMJFLFIOPYNJOFKQPODQZQZIDGHACOUKNRZVFALFLRVSZRGDTWI",
            "SDGQBBXKJDUCQJZKQGEDHTCCESUECJHQWUNKTBUMASCMQEZXXGLYOBQSSBHO"
        },

        // Classic double step
        {
            "B",
            "I II III",
            "01 01 01",
            string.Empty,
            "A D U",
            "RYSXGSJSIVREDZIDNFDVBYLXZFUUDQKISCJKFIICANYINABDHAJDDVRSLKYO",
            "FXGLEPXXGBVWFPSRFXNRIQEBUJOKXLIOTLZSLYBJJREPTRVXCLTLQXBVIRDB"
        },

        // Double step with rings
        {
            "B",
            "III II IV",
            "08 17 03",
            string.Empty,
            "P D H",
            "TUUWTBMKTGTONVDVLQVIMJLBXWNAJWKJXYZDJYNKSARKGRBXDFTDDBEQLTUU",
            "DLBDGXHHIKUWSZBFCPEKZSBJFKOQXFUBGPUJHJGDVSFHFFVVQXFZZTIEDQFT"
        },

        // Fastest rotor V double steps across Z
        {
            "B",
            "I IV V",
            "01 01 01",
            string.Empty,
            "A I Y",
            "JANSELNMMHYNVHYEFRAVWRVRMIZEHAAIRPNIKEUSCFRBUQIKUQYXKBVAUHPB",
            "RBVNAVMVVGJIEDGSGISISTLKSUWTFQHVPGFUZMYVFVDLXXFBVXCTQUQIZECZ"
        },

        // Fastest rotor VI double steps across Z
        {
            "B",
            "III IV VI",
            "04 05 06",
            string.Empty,
            "A I Y",
            "HVQEXMWLZHRIKGLLOUORZPZVSJGFUNEPALQWNZBUFCJLRXIEWXDARGWSJSBM",
            "XJPCKNPQVKOWHYPOLVXMJFYHGVIPSPAKTIOEPPRQXEVDQEGDVTVODTNNHAOU"
        },

        // Fastest rotor VII passes both notches
        {
            "C",
            "I II VII",
            "01 01 01",
            string.Empty,
            "A D K",
            "MRQZUVFQDCEXMKRIJBXSVZGFJJRJLEMGXUATUPNLZYYRFXQAFKREFADQVWUY",
            "QWHEGKGKHKVTGVDPLLUWXFLXERSSWCKZKSIIGTMFDHOFGWUETTDYRKYIHZAM"
        },

        // Fastest rotor VIII near M
        {
            "B",
            "II I VIII",
            "09 09 09",
            string.Empty,
            "A O K",
            "ALCLJGRETTHEDKPDXCMITJUDLPPDRQHLVOSLCCYCZPYJIYVHFKNQRKKXVPMK",
            "NSXULNLMNZFHZALTGKAUPQOIXFBYDSXNIWMGJKXMOSQKKPUIKQWRAWYBPFHX"
        },

        // Slowest rotor steps after middle V passes Z
        {
            "B",
            "II V III",
            "01 01 01",
            string.Empty,
            "A Y U",
            "FGZOHWLYZLNZSMGEOXSVXENIKDKIOIERUIHKERYDIJSOUMMARWKVNKDVYPVU",
            "TSSIQXDXTPZYIENANSYUWAOYZMUCEJMQAEVEBFHYSDYCBCGWEGFPGAZXEKDP"
        },

        // Slowest rotor steps after middle VIII passes Z
        {
            "C",
            "I VIII III",
            "06 11 16",
            string.Empty,
            "A Y U",
            "VYRQGNLUKBMXFAIRWIEKSUTNYZPPKABYHUDVIKAYGZOQITULCRWGRYADEKHO",
            "JHEPLMPSHVIALVPTBRDJISMRTLEXNTYHGGQFAZDVIKKYTVRBLWGREEKWPAMU"
        },

        // Slowest rotor steps after middle VI passes M
        {
            "B",
            "IV VI I",
            "01 01 01",
            string.Empty,
            "A L P",
            "GTVFWJCSNWQVSREDAINBFRXQHPVVKKLKTPIPKIBUPEAPZLIYOGLIQXMJGSBN",
            "HDYZZBGOYPXSLOSZKBFDMDYLOUOGPGHIJJJNZHDKIBZWYKTPLZZSTFNWCYCB"
        },

        // Rotors I II III with six plugs
        {
            "B",
            "I II III",
            "01 01 01",
            "AS EI JN KL MU OT",
            "A A A",
            "CXXUVQVRYJPTCQQTKPKVKLFCRZFYIQQMUDTGAOHGNQQEQGZIYWPERVKLUMRT",
            "QARTLFAOGHQHQELVBLURTHDNTIEBXDHKMUBHIWSPTNLSFFHCVQWQLNBSEPLE"
        },

        // Rotors V IV II reflector C with plugs
        {
            "C",
            "V IV II",
            "13 02 24",
            "BQ CR DI EJ",
            "Q C W",
            "XRGETRWEXEEZZVSBZVBIZJFEIRWLEOFFWBYFVENUTPRAJHQLRCOVXNEKEWQB",
            "RAURGFYNOLUUTLYXRZOWPBRACQSHHHVSLOOPMZOHLSIRKJNDHBPXVXUYWCPL"
        },

        // Rotors VIII VII VI reflector B
        {
            "B",
            "VIII VII VI",
            "26 26 26",
            string.Empty,
            "Z Z Z",
            "PABDLSWWIKYAFQGCNVNGFHHVPGLFRNXNOLWGJUXORTPCGXGDYCLWVTWFDFGU",
            "AMXFRGTQUFRWSRNFBNRFHEBLIKTJVRORMBDDCJKWCCNKHAPGAHUOUKNDCSLV"
        },
    };
}
