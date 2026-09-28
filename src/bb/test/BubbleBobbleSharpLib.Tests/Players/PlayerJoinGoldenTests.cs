// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// A player joining against the real thing.
//
// Captures/join-level-1.txt was read out of VICE running rebb64, on 2026-09-28, on level 1. Two exec
// checkpoints bracket $052A, the join: its entry and its rts ($05AC). Twenty-four calls.
//
// INTERVENTIONS, all to make joins frequent. The enemies ($B4-$B9) were zeroed so the run lived on.
// $0530's `lda $DC00,x` was replaced with `lda #$EF / nop`, so fire is down for both players. After each
// join, the next joiner or joiners (player 1, player 2 or both, in a fixed rotation) were put back in
// state 0, and the scores, lives, $AB-$AF and the credit digits ($53E4, $57E4) were set to junk, with
// $049D put back to its jump. Junk credits went as low as $FF with the jump in place, which the game
// never does, so the three lines that start there cannot be replayed by a port that takes the door's
// state from the credits, and are left out of the comparison.
//
// Each line is the bytes before and after: $0400-$0409, $045A-$045B, $AB-$AF, $B2-$B3, $049D-$049F,
// $53E4, $57E4, then the lives rows in screen A ($5139-$5167, $5251-$527F) and screen B ($5539-$5567,
// $5651-$567F). The screen is not modelled: the C64 blanks the joiner's lives cells, and the port's HUD
// draws from the lives. Colour RAM is not kept, since it flashes with no join.
[Trait("Level", "Integration")]
public sealed class PlayerJoinGoldenTests
{
    private const int ScoreAt = 0;
    private const int Lives = ScoreAt + 10;
    private const int Ab = Lives + 2;
    private const int States = Ab + 5;
    private const int Patch = States + 2;
    private const int Length = Patch + 3 + 2 + (4 * 47);
    private const int Compared = Patch + 3;

    private const byte Jump = 0x4C;
    private const byte Return = 0x60;

    public static TheoryData<int> Joins()
    {
        TheoryData<int> lines = [];

        for (int i = 0; i < Capture().Count; i++)
        {
            if ((sbyte)Capture()[i].Before[Ab] >= 0)
            {
                lines.Add(i);
            }
        }

        return lines;
    }

    [Theory]
    [MemberData(nameof(Joins))]
    public void JoinsExactlyAsTheGameDoes(int line)
    {
        (byte[] before, byte[] after) = Capture()[line];
        Assert.Equal(Length, before.Length);

        Rig rig = new(before);

        rig.Join.Update([unchecked((byte)~Input.Fire), unchecked((byte)~Input.Fire)]);

        // $0406-$0409 are not the join's, and the port does not hold them.
        after.AsSpan(ScoreAt + 6, 4).Clear();

        Assert.Equal(Convert.ToHexString(after[..Compared]), Convert.ToHexString(rig.Save()[..Compared]));
    }

    [Fact]
    public void CoversEveryKindOfJoin()
    {
        List<(byte[] Before, byte[] After)> joins = Capture();

        Assert.Equal(24, joins.Count);
        Assert.Equal(21, joins.Count(j => (sbyte)j.Before[Ab] >= 0));
        Assert.Contains(joins, j => j.Before[States] == 0 && j.Before[States + 1] != 0);
        Assert.Contains(joins, j => j.Before[States] != 0 && j.Before[States + 1] == 0);
        Assert.Contains(joins, j => j.Before[States] == 0 && j.Before[States + 1] == 0);
        Assert.Contains(joins, j => j.After[Patch] == Return && (sbyte)j.Before[Ab] >= 0);
        Assert.Contains(joins, j => j.After[Patch] == Jump);
    }

    private static List<(byte[] Before, byte[] After)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Players", "Captures", "join-level-1.txt");

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
            Join = new(Entities, Players, Score);

            for (int i = 0; i < 6; i++)
            {
                Score.Add(i, b[ScoreAt + i]);
            }

            b.AsSpan(Lives, PlayerTable.Capacity).CopyTo(Players.Lives);
            Players.Credits = b[Ab];
            b.AsSpan(Ab + 1, PlayerTable.Capacity).CopyTo(Players.ExtraLifeStep);
            b.AsSpan(Ab + 3, PlayerTable.Capacity).CopyTo(Players.ExtraLifeByte);
            b.AsSpan(States, PlayerTable.Capacity).CopyTo(Entities.State);
        }

        internal EntityTable Entities { get; } = new();

        internal PlayerTable Players { get; } = new();

        internal Scores Score { get; } = new();

        internal PlayerJoin Join { get; }

        internal byte[] Save()
        {
            byte[] b = new byte[Length];

            Score.Bytes.CopyTo(b.AsSpan(ScoreAt));

            Players.Lives.CopyTo(b.AsSpan(Lives));
            b[Ab] = Players.Credits;
            Players.ExtraLifeStep.CopyTo(b.AsSpan(Ab + 1));
            Players.ExtraLifeByte.CopyTo(b.AsSpan(Ab + 3));
            Entities.State.CopyTo(b.AsSpan(States));

            b[Patch] = (sbyte)Players.Credits < 0 ? Return : Jump;
            b[Patch + 1] = 0x2A;
            b[Patch + 2] = 0x05;
            return b;
        }
    }
}
