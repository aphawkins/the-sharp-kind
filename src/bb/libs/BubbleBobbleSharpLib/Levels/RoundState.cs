// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

// The two bytes a bonus round ($34A0) changes for the rest of the game, which a level's start ($0656-$0664) puts back.
internal sealed class RoundState
{
    internal const byte RestState = 0x10;
    internal const byte ReleasedState = 0x0A;

    // `$5A7F`: the state a player comes back in after a death, and the state a level's end takes to be a player waiting.
    internal byte Respawn { get; set; } = RestState;

    // `$5ABF`: the state an enemy comes out of its bubble in.
    internal byte Release { get; set; } = ReleasedState;

    // `$23DE` and `$0F73`: the last level's player reset (`$7F9B`, `$7FA0`) patches these two places in the code, so that a
    // player marked by the crystal (`PlayerTable.BossBlow`) blows bubbles that turn to thunder. They stay patched until a game
    // begins (`$098F`, `$0994`).
    internal bool ThunderBlow { get; set; }

    internal void Begin()
    {
        Respawn = RestState;
        Release = ReleasedState;
    }
}
