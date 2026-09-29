// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Tests.Oracle;

// The port's tables as the game holds them in RAM, so a capture's bytes can be loaded into the port and the port's
// result compared with the bytes the game left.
internal static class RamImage
{
    private static readonly (int Address, Func<ObjectTable, Span<byte>> Field)[] s_object =
    [
        (0x00CA, o => o.Type),
        (0x00DC, o => o.Column),
        (0x00EE, o => o.Row),
        (0xA9B2, o => o.State),
        (0xA9C4, o => o.SubX),
        (0xA9D6, o => o.SubY),
        (0xA9E8, o => o.Behaviour),
        (0xA9FA, o => o.Flags),
        (0xAA0C, o => o.X),
        (0xAA1E, o => o.Y),
        (0xAA30, o => o.Variant),
        (0xAA42, o => o.EnemyType),
        (0x0193, o => o.Direction),
    ];

    private static readonly (int Address, Func<EntityTable, Span<byte>> Field)[] s_entity =
    [
        (0x00B2, e => e.State),
    ];

    internal static void Load(ReadOnlySpan<byte> ram, ObjectTable objects)
    {
        foreach ((int address, Func<ObjectTable, Span<byte>> field) in s_object)
        {
            ram.Slice(address, ObjectTable.Capacity).CopyTo(field(objects));
        }
    }

    internal static void Load(ReadOnlySpan<byte> ram, EntityTable entities)
    {
        foreach ((int address, Func<EntityTable, Span<byte>> field) in s_entity)
        {
            ram.Slice(address, EntityTable.Capacity).CopyTo(field(entities));
        }
    }

    // Every object byte, slot by slot, for comparing.
    internal static string Objects(ObjectTable objects) => Convert.ToHexString(Bytes(objects));

    internal static string Objects(ReadOnlySpan<byte> ram)
    {
        List<byte> bytes = [];

        foreach ((int address, _) in s_object)
        {
            bytes.AddRange(ram.Slice(address, ObjectTable.Capacity));
        }

        return Convert.ToHexString([.. bytes]);
    }

    // Which slots and fields of two object images differ, by address, for a failing assertion's message.
    internal static string Difference(string expected, string actual)
    {
        List<string> differences = [];

        for (int i = 0; i < expected.Length; i += 2)
        {
            if (expected.AsSpan(i, 2).SequenceEqual(actual.AsSpan(i, 2)))
            {
                continue;
            }

            int index = i / 2;
            int field = index / ObjectTable.Capacity;

            string slot = $"${s_object[field].Address:X4}+{index % ObjectTable.Capacity}";

            differences.Add($"{slot}: game {expected.Substring(i, 2)}, port {actual.Substring(i, 2)}");
        }

        return string.Join("; ", differences);
    }

    private static byte[] Bytes(ObjectTable objects)
    {
        List<byte> bytes = [];

        foreach ((_, Func<ObjectTable, Span<byte>> field) in s_object)
        {
            bytes.AddRange(field(objects));
        }

        return [.. bytes];
    }
}
