// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Graphics;
using SharpKind.Assets;
using SharpKind.Assets.Models;

namespace EliteSharpLib.Ships;

internal sealed class ShipFactory : IShipFactory
{
    private readonly Dictionary<string, IShip> _ships;
    private readonly RNG _rng;

    private ShipFactory(Dictionary<string, IShip> ships, RNG rng)
    {
        _ships = ships;
        _rng = rng;
    }

    // rng is the game's: factory rolls (asteroid vs Rock Hermit, which trader) are game decisions. See RenderRandom.
    public static ShipFactory Create(IAssetLocator assetLocator, IEliteDraw draw, RNG rng)
        => Create(assetLocator, draw, rng, ShipTable.Load());

    public IShip CreateShip(string shipName)
    => _ships.TryGetValue(shipName, out IShip? ship)
        ? (IShip)ship.Clone()
        : throw new EliteException($"Ship model '{shipName}' not found.");

    public IShip CreateAsteroid() => _rng.Random(256) > 253 ? CreateShip("RockHermit") : CreateShip("Asteroid");

    public IShip CreateLoneWolf()
    {
        int rnd = _rng.Random(256);
        int index = (rnd & 3) + (rnd > 127 ? 1 : 0);
        return index switch
        {
            0 => CreateShip("CobraMk3Lone"),
            1 => CreateShip("AspMk2"),
            2 => CreateShip("PythonLone"),
            3 => CreateShip("FerDeLance"),
            4 => CreateShip("Moray"),
            _ => throw new EliteException($"Unexpected lone wolf index '{index}' (rnd '{rnd}')."),
        };
    }

    public IShip CreatePackHunter()
    {
        // Original mt1: AND two random bytes together so each bit has a 25%
        // chance of being set, biasing the result toward smaller indices.
        int first = _rng.Random(256);
        int second = _rng.Random(256);
        int rnd = first & second & 7;
        return rnd switch
        {
            0 => CreateShip("Sidewinder"),
            1 => CreateShip("Mamba"),
            2 => CreateShip("Krait"),
            3 => CreateShip("Adder"),
            4 => CreateShip("Gecko"),
            5 => CreateShip("CobraMk1"),
            6 => CreateShip("Worm"),
            7 => CreateShip("CobraMk3"),
            _ => throw new EliteException($"Unexpected pack hunter roll '{rnd}'."),
        };
    }

    public IShip CreatePirate()
    {
        int rnd = _rng.Random(4);
        return rnd switch
        {
            0 => CreateShip("Sidewinder"),
            1 => CreateShip("Mamba"),
            2 => CreateShip("Krait"),
            3 => CreateShip("Adder"),
            _ => throw new EliteException($"Unexpected pirate roll '{rnd}'."),
        };
    }

    public IShip CreateTrader()
    {
        int rnd = _rng.Random(4);
        return rnd switch
        {
            0 => CreateShip("CobraMk3"),
            1 => CreateShip("Python"),
            2 => CreateShip("Boa"),
            3 => CreateShip("Anaconda"),
            _ => throw new EliteException($"Unexpected trader roll '{rnd}'."),
        };
    }

    public List<IShip> CreateParade() => new()
    {
        { CreateShip("Missile") },
        { CreateShip("Coriolis") },
        { CreateShip("EscapeCapsule") },
        { CreateShip("Alloy") },
        { CreateShip("CargoCannister") },
        { CreateShip("Boulder") },
        { CreateShip("Asteroid") },
        { CreateShip("RockSplinter") },
        { CreateShip("Shuttle") },
        { CreateShip("Transporter") },
        { CreateShip("CobraMk3") },
        { CreateShip("Python") },
        { CreateShip("Boa") },
        { CreateShip("Anaconda") },
        { CreateShip("RockHermit") },
        { CreateShip("Viper") },
        { CreateShip("Sidewinder") },
        { CreateShip("Mamba") },
        { CreateShip("Krait") },
        { CreateShip("Adder") },
        { CreateShip("Gecko") },
        { CreateShip("CobraMk1") },
        { CreateShip("Worm") },
        { CreateShip("AspMk2") },
        { CreateShip("FerDeLance") },
        { CreateShip("Moray") },
        { CreateShip("Thargoid") },
        { CreateShip("Tharglet") },
        { CreateShip("DodecStation") },
    };

    // The table the ships are built from, handed in so a test can state its
    // own rather than install a file.
    internal static ShipFactory Create(
        IAssetLocator assetLocator,
        IEliteDraw draw,
        RNG rng,
        IReadOnlyList<ShipDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(assetLocator);
        ArgumentNullException.ThrowIfNull(definitions);

        string? unknown = assetLocator.ModelPaths.Keys.FirstOrDefault(
            x => !definitions.Any(d => d.Id == x));

        if (unknown != null)
        {
            throw new EliteException($"Ship type '{unknown}' could not be found.");
        }

        // Every ship the manifest supplies a model for, including the variants
        // that borrow their parent's model.
        Dictionary<string, IShip> ships = [];
        foreach (ShipDefinition definition in definitions)
        {
            if (assetLocator.ModelPaths.TryGetValue(definition.Model, out string? modelPath))
            {
                ships[definition.Id] = Build(definition, draw, modelPath);
            }
        }

        return new(ships, rng);
    }

    // Clone has always returned a ShipBase whatever class it was called on, so building all prototypes as ShipBase changes nothing.
    private static ShipBase Build(ShipDefinition definition, IEliteDraw draw, string modelPath)
        => new(draw)
        {
            Model = ModelReader.Read(modelPath, draw.Palette),
            Id = definition.Id,
            Flags = definition.Flags,
            Traits = definition.Traits,
            Name = definition.Name,
            ScoopedType = definition.ScoopedType,
            Bounty = definition.Bounty,
            EnergyMax = definition.EnergyMax,
            LaserFront = definition.LaserFront,
            LaserStrength = definition.LaserStrength,
            LootMax = definition.LootMax,
            MinDistance = definition.MinDistance,
            MissilesMax = definition.MissilesMax,
            Size = definition.Size,
            VanishPoint = definition.VanishPoint,
            VelocityMax = definition.VelocityMax,
        };
}
