// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Text.Json;
using EliteSharp.Abstractions.Trading;

namespace EliteSharp.Goods.Classic;

/// <summary>
/// The seventeen goods the original traded in, read from the
/// <c>goods.json</c> that ships beside this assembly. Everything the game used
/// to hold about them in four different places - the market table, the starting
/// station's shelf, which of them the police care about, and which a canister
/// can contain - is that one file now.
/// <para>
/// The table is data rather than code, so changing an economy - or writing a
/// different one - is editing a file rather than rebuilding an assembly. The
/// class is only what finds and checks it; nothing here knows the word "Food".
/// The set's own name comes from the file too, so this assembly is not tied to
/// the classic wares beyond the name it is filed under.
/// </para>
/// <para>
/// The file is found beside the assembly, the same way a rendition finds the
/// artwork it brought with it. Only one goods set may be installed at a time -
/// the loader refuses two - so there is no question which file is whose.
/// </para>
/// </summary>
public sealed class ClassicGoodsSet : IGoodsSet
{
    /// <summary>
    /// The data file's name, beside this assembly.
    /// </summary>
    internal const string FileName = "goods.json";

    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly GoodsFile _file;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassicGoodsSet"/> class,
    /// reading the goods from the file beside this assembly.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The file is missing, unreadable, not the shape a goods file is, or
    /// describes no goods at all. A set that cannot say what it trades is not
    /// one the game can start with, so this says which file and why rather than
    /// handing back an empty market.
    /// </exception>
    public ClassicGoodsSet()
        : this(Path.Combine(AssemblyFolder(), FileName))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassicGoodsSet"/> class
    /// from a named file, which is how the tests reach one without installing
    /// it.
    /// </summary>
    /// <param name="path">The goods file to read.</param>
    /// <exception cref="InvalidOperationException">As the default constructor.</exception>
    internal ClassicGoodsSet(string path)
    {
        _file = Read(path);
        Goods = [.. _file.Goods.Select(ToGood)];
    }

    /// <inheritdoc/>
    public string Name => _file.Name;

    /// <inheritdoc/>
    public IReadOnlyList<Good> Goods { get; }

    private static string AssemblyFolder()
        => Path.GetDirectoryName(typeof(ClassicGoodsSet).Assembly.Location) ?? AppContext.BaseDirectory;

    private static GoodsFile Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"The goods file '{path}' is not there, so there is nothing to trade.");
        }

        GoodsFile? file;

        try
        {
            using FileStream stream = File.OpenRead(path);
            file = JsonSerializer.Deserialize<GoodsFile>(stream, s_options);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"The goods file '{path}' could not be read: {ex.Message}", ex);
        }

        if (file?.Goods is not { Count: > 0 })
        {
            throw new InvalidOperationException($"The goods file '{path}' describes no goods.");
        }

        if (string.IsNullOrWhiteSpace(file.Name))
        {
            throw new InvalidOperationException($"The goods file '{path}' does not name the set it describes.");
        }

        for (int i = 0; i < file.Goods.Count; i++)
        {
            Check(path, file.Goods[i], i);
        }

        return file;
    }

    // Only checks what this file can know alone - duplicates and wreck drops depend on what else is installed.
    private static void Check(string path, GoodsEntry entry, int index)
    {
        if (string.IsNullOrWhiteSpace(entry.Id))
        {
            throw new InvalidOperationException($"The goods file '{path}' has a good with no id at position {index}.");
        }

        if (string.IsNullOrWhiteSpace(entry.Name))
        {
            throw new InvalidOperationException($"The goods file '{path}' gives '{entry.Id}' no name to show.");
        }

        if (string.IsNullOrWhiteSpace(entry.Units))
        {
            throw new InvalidOperationException($"The goods file '{path}' gives '{entry.Id}' no units.");
        }
    }

    private static Good ToGood(GoodsEntry entry) => new(
        entry.Id,
        entry.Name,
        entry.BasePrice,
        entry.EconomyAdjust,
        entry.BaseQuantity,
        entry.Mask,
        entry.Units,
        entry.FillsHold,
        entry.OpeningStationStock,
        entry.IsSoldByStations,
        entry.ContrabandWeight,
        entry.IsDroppedByShips);

    /// <summary>
    /// The file's shape. Separate from <see cref="Good"/> so the published
    /// contract stays free of anything to do with how this plugin happens to
    /// store its table. Positional records, so the reader binds through the
    /// constructor and a member the file leaves out is a default rather than a
    /// property nothing was ever seen to assign.
    /// </summary>
    private sealed record GoodsFile(string Name, IReadOnlyList<GoodsEntry> Goods);

    /// <inheritdoc cref="GoodsFile"/>
    private sealed record GoodsEntry(
        string Id,
        string Name,
        float BasePrice,
        int EconomyAdjust,
        int BaseQuantity,
        int Mask,
        string Units,
        bool FillsHold,
        int OpeningStationStock,
        bool IsSoldByStations,
        int ContrabandWeight,
        bool IsDroppedByShips);
}
