// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Missions;
using EliteSharpLib.Planets;
using EliteSharpLib.Types;

namespace EliteSharpLib.Views;

/// <summary>
/// The planet data screen's behaviour: formatting the hyperspace target's
/// stats, and generating its randomised description.
/// </summary>
internal sealed class PlanetDataController : IScreenController
{
    private readonly string[][] _descriptionList =
    [
        ["fabled", "notable", "well known", "famous", "noted"],
        ["very", "mildly", "most", "reasonably", string.Empty],
        ["ancient", "<20>", "great", "vast", "pink"],
        ["<29> <28> plantations", "mountains", "<27>", "<19> forests", "oceans"],
        ["shyness", "silliness", "mating traditions", "loathing of <5>", "love for <5>"],
        ["food blenders", "tourists", "poetry", "discos", "<13>"],
        ["talking tree", "crab", "bat", "lobst", "%R"],
        ["beset", "plagued", "ravaged", "cursed", "scourged"],
        ["<21> civil war", "<26> <23> <24>s", "a <26> disease", "<21> earthquakes", "<21> solar activity"],
        ["its <2> <3>", "the %I <23> <24>", "its inhabitants' <25> <4>", "<32>", "its <12> <13>"],
        ["juice", "brandy", "water", "brew", "gargle blasters"],
        ["%R", "%I <24>", "%I %R", "%I <26>", "<26> %R"],
        ["fabulous", "exotic", "hoopy", "unusual", "exciting"],
        ["cuisine", "night life", "casinos", "sit coms", " <32> "],
        ["%H", "The planet %H", "The world %H", "This planet", "This world"],
        ["n unremarkable", " boring", " dull", " tedious", " revolting"],
        ["planet", "world", "place", "little planet", "dump"],
        ["wasp", "moth", "grub", "ant", "%R"],
        ["poet", "arts graduate", "yak", "snail", "slug"],
        ["tropical", "dense", "rain", "impenetrable", "exuberant"],
        ["funny", "wierd", "unusual", "strange", "peculiar"],
        ["frequent", "occasional", "unpredictable", "dreadful", "deadly"],
        ["<1> <0> for <9>", "<1> <0> for <9> and <9>", "<7> by <8>", "<1> <0> for <9> but <7> by <8>", " a<15> <16>"],
        ["<26>", "mountain", "edible", "tree", "spotted"],
        ["<30>", "<31>", "<6>oid", "<18>", "<17>"],
        ["ancient", "exceptional", "eccentric", "ingrained", "<20>"],
        ["killer", "deadly", "evil", "lethal", "vicious"],
        ["parking meters", "dust clouds", "ice bergs", "rock formations", "volcanoes"],
        ["plant", "tulip", "banana", "corn", "%Rweed"],
        ["%R", "%I %R", "%I <26>", "inhabitant", "%I %R"],
        ["shrew", "beast", "bison", "snake", "wolf"],
        ["leopard", "cat", "monkey", "goat", "fish"],
        ["<11> <10>", "%I <30> <33>", "its <12> <31> <33>", "<34> <35>", "<11> <10>"],
        ["meat", "cutlet", "steak", "burgers", "soup"],
        ["ice", "mud", "Zero-G", "vacuum", "%I ultra"],
        ["hockey", "cricket", "karate", "polo", "tennis"],
    ];

    private readonly string[] _economyType =
    [
        "Rich Industrial",
        "Average Industrial",
        "Poor Industrial",
        "Mainly Industrial",
        "Mainly Agricultural",
        "Rich Agricultural",
        "Average Agricultural",
        "Poor Agricultural",
    ];

    private readonly GameState _gameState;

    private readonly string[] _governmentType =
    [
        "Anarchy",
        "Feudal",
        "Multi-Government",
        "Dictatorship",
        "Communist",
        "Confederacy",
        "Democracy",
        "Corporate State",
    ];

    private readonly PlanetController _planet;
    private readonly RNG _rng;
    private readonly MissionRunner _missions;
    private readonly IView<PlanetDataModel> _view;

    private float _distanceToPlanet;
    private PlanetData _hyperPlanetData = new();

    internal PlanetDataController(
        GameState gameState,
        PlanetController planet,
        RNG rng,
        MissionRunner missions,
        IView<PlanetDataModel> view)
    {
        _gameState = gameState;
        _planet = planet;
        _rng = rng;
        _missions = missions;
        _view = view;
    }

    public void Draw() => _view.Draw(BuildModel());

    public void HandleInput()
    {
    }

    public void Reset()
    {
    }

    public void Update()
    {
        _distanceToPlanet = PlanetController.CalculateDistanceToPlanet(_gameState.DockedPlanet, _gameState.HyperspacePlanet);
        _hyperPlanetData = PlanetController.GeneratePlanetData(_gameState.HyperspacePlanet);
    }

    // Exposed for tests: every formatted field, including the generated
    // description.
    internal PlanetDataModel BuildModel() => new(
        $"DATA ON {_planet.NamePlanet(_gameState.HyperspacePlanet)}",
        _distanceToPlanet > 0 ? $"{_distanceToPlanet:N1} Light Years" : string.Empty,
        _economyType[_hyperPlanetData.Economy],
        _governmentType[_hyperPlanetData.Government],
        $"{_hyperPlanetData.TechLevel + 1}",
        $"{_hyperPlanetData.Population:N1} Billion {_planet.DescribeInhabitants(_gameState.HyperspacePlanet)}",
        $"{_hyperPlanetData.Productivity} Million Credits",
        $"{_hyperPlanetData.Radius} km",
        DescribePlanet(_gameState.HyperspacePlanet));

    private string DescribePlanet(GalaxySeed planet)
    {
        // A rumour is somebody in this station talking, so the mission is only asked about the system it can be heard in.
        if (_missions.DescribePlanet(_planet.FindPlanetNumber(_gameState.Cmdr.Galaxy, planet)) is { } rumour)
        {
            return rumour;
        }

        _rng.Seed.A = planet.C;
        _rng.Seed.B = planet.D;
        _rng.Seed.C = planet.E;
        _rng.Seed.D = planet.F;

        if (_gameState.Config.Game.PlanetDescriptions == PlanetDescriptions.HoopyCasinos)
        {
            _rng.Seed.A ^= planet.A;
            _rng.Seed.B ^= planet.B;
            _rng.Seed.C ^= _rng.Seed.A;
            _rng.Seed.D ^= _rng.Seed.B;
        }

        StringBuilder planet_description = new();

        ExpandDescription("<14> is <22>.", ref planet_description);

        return planet_description.ToString();
    }

    private void ExpandDescription(string source, ref StringBuilder planetDescription)
    {
        int k = 0;
        for (int j = 0; (j + k) < source.Length; j++)
        {
            if (source[j + k] == '<')
            {
                ExpandToken(source, j, ref k, ref planetDescription);
                continue;
            }

            if (source[j + k] == '%')
            {
                k++;
                ExpandEscape(source[j + k], planetDescription);
                continue;
            }

            planetDescription.Append(source[j + k]);
        }
    }

    /// <summary>
    /// Expand a "&lt;n&gt;" token by picking one of description list n's phrases
    /// and expanding that in turn. Advances k past the token.
    /// </summary>
    private void ExpandToken(string source, int j, ref int k, ref StringBuilder planetDescription)
    {
        StringBuilder temp = new();
        k++;

        while (source[j + k] != '>')
        {
            temp.Append(source[j + k]);
            k++;
        }

        int num = Convert.ToInt32(temp.ToString(), CultureInfo.InvariantCulture);
        Debug.Assert(num < _descriptionList.Length, "Number should be within the description range.");

        ExpandDescription(_descriptionList[num][SelectDescriptionOption()], ref planetDescription);
    }

    /// <summary>
    /// Choose which of a description list's five phrases to use.
    /// </summary>
    private int SelectDescriptionOption()
    {
        if (_gameState.Config.Game.PlanetDescriptions == PlanetDescriptions.HoopyCasinos)
        {
            return _rng.GenMSXRandomNumber();
        }

        int rnd = _rng.GenerateRandomNumber();
        int option = 0;
        if (rnd >= 0x33)
        {
            option++;
        }

        if (rnd >= 0x66)
        {
            option++;
        }

        if (rnd >= 0x99)
        {
            option++;
        }

        if (rnd >= 0xCC)
        {
            option++;
        }

        return option;
    }

    /// <summary>
    /// Expand a "%x" escape: the planet's name, its adjective, or a random name.
    /// </summary>
    private void ExpandEscape(char code, StringBuilder planetDescription)
    {
        switch (code)
        {
            case 'H':
                planetDescription.Append(_planet.NamePlanet(_gameState.HyperspacePlanet).CapitaliseFirstLetter());
                break;

            case 'I':
                planetDescription
                    .Append(_planet.NamePlanet(_gameState.HyperspacePlanet).CapitaliseFirstLetter())
                    .Append("ian");
                break;

            case 'R':
                AppendRandomName(planetDescription);
                break;
        }
    }

    /// <summary>
    /// Append a made-up name built from one to four digram pairs.
    /// </summary>
    private void AppendRandomName(StringBuilder planetDescription)
    {
        int len = _rng.GenerateRandomNumber() & 3;
        for (int i = 0; i <= len; i++)
        {
            int x = _rng.GenerateRandomNumber() & 62;
            if (i == 0)
            {
                planetDescription.Append(_planet.Digrams[x]);
            }
            else
            {
                planetDescription.Append(char.ToLowerInvariant(_planet.Digrams[x]));
            }

            planetDescription.Append(char.ToLowerInvariant(_planet.Digrams[x + 1]));
        }
    }
}
