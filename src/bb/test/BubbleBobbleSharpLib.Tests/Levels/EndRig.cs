// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind;

namespace BubbleBobbleSharpLib.Tests.Levels;

// A level's end with what it works on, wired the way the game wires it.
internal sealed class EndRig
{
    internal EndRig()
        : this(new ScriptedRandomSource([]))
    {
    }

    internal EndRig(IRandomSource source)
    {
        Random = new(source);
        Items = new(Entities, Scores, new(Players, Entities), Random);
        Pop = new(Objects, Entities, Players, new(Entities, Scores, Random), Scores);
        End = new(Timer, Entities, Objects, Pop, Items, Random);
        Timer.Begin(0);
    }

    internal LevelTimer Timer { get; } = new();

    internal EntityTable Entities { get; } = new();

    internal PlayerTable Players { get; } = new();

    internal ObjectTable Objects { get; } = new();

    internal Scores Scores { get; } = new();

    internal BbRandom Random { get; }

    internal LevelItems Items { get; }

    internal BubblePop Pop { get; }

    internal LevelEnd End { get; }
}
