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
    internal const int EnemyCount = 0x4A;
    internal const int ClimbNext = 0x86B0;

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
        (0x00BA, e => e.X),
        (0x00C2, e => e.Y),
        (0x8520, e => e.Frame),
        (0x8548, e => e.Colour),
        (0x8598, e => e.SpriteBase),
        (0x85C0, e => e.Mode),
        (0x85E8, e => e.Heading),
        (0x8610, e => e.AnimationTimer),
        (0x8638, e => e.HoldTimer),
        (0x8660, e => e.TurnTimer),
        (0x8688, e => e.TurnInterval),
        (0x86D8, e => e.LeapFlag),
        (0x8700, e => e.ClimbFlag),
        (0x8728, e => e.FlashTimer),
        (0x8750, e => e.FrameCount),
        (0x8778, e => e.FrameMask),
        (0x87A0, e => e.RiseCounter),
        (0x87C8, e => e.FallCounter),
        (0x87F0, e => e.GroundState),
        (0x8818, e => e.BubbleTimer),
        (0x8840, e => e.LeftFlag),
        (0x8868, e => e.RightFlag),
        (0x8890, e => e.AttackTimer),
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

        for (int slot = 0; slot < EntityTable.Capacity; slot++)
        {
            entities.ClimbNext[slot] = ram[ClimbNext + slot] != 0;
        }

        entities.EnemyCount = ram[EnemyCount];
    }

    // The blow, the rings and the lives, as the game holds them: `$A824` (the time to the next blow), `$ACCB` (which way a
    // blow faces), `$A77B` to `$A784` (what a blow makes, and the thunder shots left) and `$61` to `$66` (the rings).
    internal static void Load(ReadOnlySpan<byte> ram, PlayerTable players)
    {
        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            players.Reload[player] = ram[0xA824 + player];
            players.BlowFacing[player] = ram[0xACCB + player];
            players.BlowReload[player] = ram[0xA77B + player];
            players.BlowState[player] = ram[0xA77D + player];
            players.BlowVariant[player] = ram[0xA77F + player];
            players.BlowType[player] = ram[0xA781 + player];
            players.Shots[player] = ram[0xA783 + player];
            players.DriftRing[player] = ram[0x61 + player];
            players.WalkRing[player] = ram[0x63 + player];
            players.BlowRing[player] = ram[0x65 + player];
        }
    }

    // Every object byte, slot by slot, for comparing.
    internal static string Objects(ObjectTable objects)
    {
        List<byte> bytes = [];

        foreach ((_, Func<ObjectTable, Span<byte>> field) in s_object)
        {
            bytes.AddRange(field(objects));
        }

        return Convert.ToHexString([.. bytes]);
    }

    internal static string Objects(ReadOnlySpan<byte> ram)
    {
        List<byte> bytes = [];

        foreach ((int address, _) in s_object)
        {
            bytes.AddRange(ram.Slice(address, ObjectTable.Capacity));
        }

        return Convert.ToHexString([.. bytes]);
    }

    // Every entity byte, slot by slot, and the enemy count.
    internal static string Entities(EntityTable entities)
    {
        List<byte> bytes = [];

        foreach ((_, Func<EntityTable, Span<byte>> field) in s_entity)
        {
            bytes.AddRange(field(entities));
        }

        bytes.Add(entities.EnemyCount);

        return Convert.ToHexString([.. bytes]);
    }

    internal static string Entities(ReadOnlySpan<byte> ram)
    {
        List<byte> bytes = [];

        foreach ((int address, _) in s_entity)
        {
            bytes.AddRange(ram.Slice(address, EntityTable.Capacity));
        }

        bytes.Add(ram[EnemyCount]);

        return Convert.ToHexString([.. bytes]);
    }

    // Which slots and fields of two object images differ, by address, for a failing assertion's message.
    internal static string Difference(string expected, string actual)
        => Compare(expected, actual, [.. s_object.Select(f => f.Address)], ObjectTable.Capacity);

    // As `Difference`, for two entity images.
    internal static string EntityDifference(string expected, string actual)
        => Compare(expected, actual, [.. s_entity.Select(f => f.Address), EnemyCount], EntityTable.Capacity);

    private static string Compare(string expected, string actual, int[] addresses, int width)
    {
        List<string> differences = [];

        for (int i = 0; i < expected.Length; i += 2)
        {
            if (expected.AsSpan(i, 2).SequenceEqual(actual.AsSpan(i, 2)))
            {
                continue;
            }

            int index = i / 2;
            int field = Math.Min(index / width, addresses.Length - 1);
            int slot = index >= (addresses.Length - 1) * width && addresses[^1] == EnemyCount ? 0 : index % width;

            differences.Add($"${addresses[field]:X4}+{slot}: game {expected.Substring(i, 2)}, port {actual.Substring(i, 2)}");
        }

        return string.Join("; ", differences);
    }
}
