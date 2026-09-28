// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// A player running out of lives against the real thing.
//
// Captures/gameover-level-1.txt was read out of VICE running rebb64, on 2026-09-28, on level 1. Two exec
// checkpoints bracket $045C: its entry and the rts of $052A ($05AC), which it ends in. Eight calls.
//
// INTERVENTIONS. Before each call the enemies ($B4-$B9) were zeroed, $4A too, the dying player or players
// were made state $0F with no lives and a junk place, the other player was made state 1 with three lives,
// and SUBFLG ($10), $0409 and $040A were set to junk.
//
// Each line is the bytes before and after: $0409-$040A, $045A-$045B, $B2-$B3, $BA-$BB, $C2-$C3, $5B7F, $4A,
// SUBFLG, then screen A ($5139-$5167, $5251-$527F) and screen B ($5539-$5567, $5651-$567F), the two rows
// of each player's lives and the row under them. The screen is not modelled: the HUD says GAME OVER for any
// lives below zero (HudView8BitTests), where the C64 writes it once and leaves it. It is here to show where
// $7BE8 puts "GAME" and "OVER": column 35, on the lives row and the one below, in screen codes $27 $21 $2D
// $25 and $2F $36 $25 $32.
[Trait("Level", "Integration")]
public sealed class PlayerGameOverGoldenTests
{
    private const int Round = 0;
    private const int Lives = Round + 2;
    private const int States = Lives + 2;
    private const int Xs = States + 2;
    private const int Ys = Xs + 2;
    private const int Undying = Ys + 2;
    private const int EnemyCount = Undying + 1;
    private const int Subflg = EnemyCount + 1;
    private const int Screen = Subflg + 1;
    private const int Length = Screen + (4 * 47);

    private const int TextColumn = 35 - 33;

    public static TheoryData<int> GameOvers()
    {
        TheoryData<int> lines = [];

        for (int i = 0; i < Capture().Count; i++)
        {
            lines.Add(i);
        }

        return lines;
    }

    [Theory]
    [MemberData(nameof(GameOvers))]
    public void EndsThePlayerExactlyAsTheGameDoes(int line)
    {
        (byte[] before, byte[] after) = Capture()[line];
        Assert.Equal(Length, before.Length);

        Rig rig = new(before);

        rig.Respawn.Update(before[Subflg]);

        Assert.Equal(Convert.ToHexString(after[..Subflg]), Convert.ToHexString(rig.Save()[..Subflg]));
    }

    // Where $7BE8 wrote: "GAME" on the lives row and "OVER" on the row under it, from column 35.
    [Theory]
    [MemberData(nameof(GameOvers))]
    public void WritesGameOverWhereTheHudDrawsIt(int line)
    {
        (byte[] before, byte[] after) = Capture()[line];

        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            if ((sbyte)before[Lives + player] >= 0 && (sbyte)after[Lives + player] < 0)
            {
                // Each span starts at the row's column 33, and the row under it is 40 cells on.
                int game = Screen + (player * 47) + TextColumn;

                Assert.Equal("27212D25", Convert.ToHexString(after[game..(game + 4)]));
                Assert.Equal("2F362532", Convert.ToHexString(after[(game + 40)..(game + 44)]));
            }
        }
    }

    [Fact]
    public void CoversOnePlayerOutAndBothOut()
    {
        List<(byte[] Before, byte[] After)> overs = Capture();

        Assert.Equal(8, overs.Count);
        Assert.Contains(overs, o => (sbyte)o.After[Lives] < 0 && (sbyte)o.After[Lives + 1] >= 0);
        Assert.Contains(overs, o => (sbyte)o.After[Lives] >= 0 && (sbyte)o.After[Lives + 1] < 0);
        Assert.Contains(overs, o => (sbyte)o.After[Lives] < 0 && (sbyte)o.After[Lives + 1] < 0);
    }

    private static List<(byte[] Before, byte[] After)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Players", "Captures", "gameover-level-1.txt");

        return [.. File.ReadAllLines(path)
            .Select(line =>
            {
                string[] parts = line.Split(' ');
                return (Convert.FromHexString(parts[1]), Convert.FromHexString(parts[2]));
            })];
    }

    private sealed class Rig
    {
        internal Rig(byte[] b)
        {
            Items = new(Entities, new Scores(), new(Players, Entities), new(new FakeRandomSource()));
            Respawn = new(Entities, Players, Items, Timer);

            b.AsSpan(Round, PlayerTable.Capacity).CopyTo(Players.Round);
            b.AsSpan(Lives, PlayerTable.Capacity).CopyTo(Players.Lives);
            b.AsSpan(States, PlayerTable.Capacity).CopyTo(Entities.State);
            b.AsSpan(Xs, PlayerTable.Capacity).CopyTo(Entities.X);
            b.AsSpan(Ys, PlayerTable.Capacity).CopyTo(Entities.Y);
            Items.Undying = b[Undying];
            Entities.EnemyCount = b[EnemyCount];
        }

        internal EntityTable Entities { get; } = new();

        internal PlayerTable Players { get; } = new();

        internal LevelItems Items { get; }

        internal LevelTimer Timer { get; } = new();

        internal PlayerRespawn Respawn { get; }

        internal byte[] Save()
        {
            byte[] b = new byte[Length];

            Players.Round.CopyTo(b.AsSpan(Round));
            Players.Lives.CopyTo(b.AsSpan(Lives));
            Entities.State.CopyTo(b.AsSpan(States));
            Entities.X.CopyTo(b.AsSpan(Xs));
            Entities.Y.CopyTo(b.AsSpan(Ys));
            b[Undying] = Items.Undying;
            b[EnemyCount] = Entities.EnemyCount;

            return b;
        }
    }
}
