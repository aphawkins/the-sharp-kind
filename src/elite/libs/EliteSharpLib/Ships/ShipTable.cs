// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Text.Json;

namespace EliteSharpLib.Ships;

/// <summary>
/// The ships the game can build, read from the <c>ships.json</c> that ships
/// beside the assembly. Every one of them used to be a class whose whole body
/// was a constructor setting the same ten fields - <see cref="ShipBase"/> was
/// already all the behaviour any of them had, and cloning one returned a
/// <see cref="ShipBase"/> regardless - so what was thirty-three files is one
/// file of rows now.
/// <para>
/// The file follows the goods set's shape: the game reads it rather than
/// naming any of it, and nothing here knows the word "Adder". It is not a
/// plugin yet - a ship still needs a model in the rendition's asset manifest,
/// so the table and the artwork have to agree - but the shape is the one a
/// plugin would come through.
/// </para>
/// </summary>
internal static class ShipTable
{
    /// <summary>
    /// The data file's name, beside the assembly.
    /// </summary>
    internal const string FileName = "ships.json";

    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>
    /// Reads the ship table from beside this assembly.
    /// </summary>
    /// <returns>The ships the factory can build, in the file's order.</returns>
    /// <exception cref="EliteException">As <see cref="LoadFrom(string)"/>.</exception>
    internal static IReadOnlyList<ShipDefinition> Load()
        => LoadFrom(Path.Combine(AssemblyFolder(), FileName));

    /// <summary>
    /// Reads the ship table from a named file, which is how the tests reach one
    /// without installing it.
    /// </summary>
    /// <param name="path">The ship file to read.</param>
    /// <returns>The ships the factory can build, in the file's order.</returns>
    /// <exception cref="EliteException">
    /// The file is missing, unreadable, not the shape a ship file is, describes
    /// no ships, names the same ship twice, names a behaviour the game does
    /// not have, or is based on a ship the file does not describe.
    /// A table the game cannot read is a game with nothing to fly, so this says
    /// which file and why rather than starting an empty universe.
    /// </exception>
    internal static IReadOnlyList<ShipDefinition> LoadFrom(string path)
    {
        ShipFile file = Read(path);
        Dictionary<string, ShipEntry> byId = [];
        List<ShipDefinition> ships = [];

        foreach (ShipEntry entry in file.Ships)
        {
            if (string.IsNullOrWhiteSpace(entry.Id))
            {
                throw new EliteException($"The ship file '{path}' has a ship with no id at position {ships.Count}.");
            }

            if (!byId.TryAdd(entry.Id, entry))
            {
                throw new EliteException($"The ship file '{path}' describes '{entry.Id}' twice.");
            }

            ships.Add(Resolve(path, entry, byId));
        }

        return ships;
    }

    private static string AssemblyFolder()
        => Path.GetDirectoryName(typeof(ShipTable).Assembly.Location) ?? AppContext.BaseDirectory;

    private static ShipFile Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new EliteException($"The ship file '{path}' is not there, so there is nothing to fly.");
        }

        ShipFile? file;

        try
        {
            using FileStream stream = File.OpenRead(path);
            file = JsonSerializer.Deserialize<ShipFile>(stream, s_options);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new EliteException($"The ship file '{path}' could not be read: {ex.Message}", ex);
        }

        return file?.Ships is { Count: > 0 }
            ? file
            : throw new EliteException($"The ship file '{path}' describes no ships.");
    }

    // A variant states only what it changes; anything left unsaid comes from the ship it's based
    // on, which must already be described in the file so rows resolve as read and no cycle is possible.
    private static ShipDefinition Resolve(string path, ShipEntry entry, Dictionary<string, ShipEntry> byId)
    {
        ShipEntry? parent = null;

        if (entry.BasedOn != null && !byId.TryGetValue(entry.BasedOn, out parent))
        {
            throw new EliteException(
                $"The ship file '{path}' bases '{entry.Id}' on '{entry.BasedOn}', which it does not describe before it.");
        }

        ShipEntry ship = Merge(entry, parent);
        string id = ship.Id!;

        return new(
            id,
            ship.Model ?? ship.BasedOn ?? id,
            ParseFlags(path, id, ship.Flags),
            ParseTraits(path, id, ship.Traits),
            ship.Name ?? throw NoName(path, id),
            ship.ScoopedType,
            ship.Bounty ?? 0,
            ship.EnergyMax ?? 0,
            ship.LaserFront ?? 0,
            ship.LaserStrength ?? 0,
            ship.LootMax ?? 0,
            ship.MinDistance ?? 0,
            ship.MissilesMax ?? 0,
            ship.Size ?? 0,
            ship.VanishPoint ?? 0,
            ship.VelocityMax ?? 0);
    }

    // A field the parent doesn't state either stays unsaid, so Resolve still decides what an absent one means.
    private static ShipEntry Merge(ShipEntry entry, ShipEntry? parent)
        => parent == null ? entry : entry with
        {
            Model = entry.Model ?? parent.Model,
            Flags = entry.Flags ?? parent.Flags,
            Traits = entry.Traits ?? parent.Traits,
            Name = entry.Name ?? parent.Name,
            ScoopedType = entry.ScoopedType ?? parent.ScoopedType,
            Bounty = entry.Bounty ?? parent.Bounty,
            EnergyMax = entry.EnergyMax ?? parent.EnergyMax,
            LaserFront = entry.LaserFront ?? parent.LaserFront,
            LaserStrength = entry.LaserStrength ?? parent.LaserStrength,
            LootMax = entry.LootMax ?? parent.LootMax,
            MinDistance = entry.MinDistance ?? parent.MinDistance,
            MissilesMax = entry.MissilesMax ?? parent.MissilesMax,
            Size = entry.Size ?? parent.Size,
            VanishPoint = entry.VanishPoint ?? parent.VanishPoint,
            VelocityMax = entry.VelocityMax ?? parent.VelocityMax,
        };

    private static EliteException NoName(string path, string id)
        => new($"The ship file '{path}' gives '{id}' no name to show.");

    private static ShipTraits ParseTraits(string path, string id, IReadOnlyList<string>? traits)
    {
        ShipTraits parsed = ShipTraits.None;

        foreach (string trait in traits ?? [])
        {
            parsed |= Enum.TryParse(trait, out ShipTraits one)
                ? one
                : throw new EliteException($"The ship file '{path}' gives '{id}' the unknown trait '{trait}'.");
        }

        return parsed;
    }

    private static ShipProperties ParseFlags(string path, string id, IReadOnlyList<string>? flags)
    {
        ShipProperties parsed = ShipProperties.None;

        foreach (string flag in flags ?? [])
        {
            parsed |= Enum.TryParse(flag, out ShipProperties one)
                ? one
                : throw new EliteException($"The ship file '{path}' gives '{id}' the unknown behaviour '{flag}'.");
        }

        return parsed;
    }

    /// <summary>
    /// The file's shape. Separate from <see cref="ShipDefinition"/> so what the
    /// game builds a ship from stays free of anything to do with how the table
    /// happens to be stored. Positional records, so the reader binds through
    /// the constructor; every member is optional because a variant states only
    /// what it changes, and <see cref="Resolve"/> decides which absences are
    /// allowed.
    /// </summary>
    private sealed record ShipFile(string? Name, IReadOnlyList<ShipEntry> Ships);

    /// <inheritdoc cref="ShipFile"/>
    private sealed record ShipEntry(
        string? Id,
        string? BasedOn,
        string? Model,
        IReadOnlyList<string>? Flags,
        IReadOnlyList<string>? Traits,
        string? Name,
        string? ScoopedType,
        float? Bounty,
        int? EnergyMax,
        int? LaserFront,
        int? LaserStrength,
        int? LootMax,
        float? MinDistance,
        int? MissilesMax,
        float? Size,
        int? VanishPoint,
        float? VelocityMax);
}
