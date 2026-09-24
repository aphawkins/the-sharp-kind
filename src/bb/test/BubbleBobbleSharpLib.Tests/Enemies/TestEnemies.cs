// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// $1CBD's enemy arm with every handler behind it, wired the one way the game wires them.
internal static class TestEnemies
{
    internal static EnemyFrame Frame(EntityTable entities, ObjectTable objects, BbRandom random)
    {
        EnemyChase chase = new(entities);
        PlayerDescent descent = new(entities);

        return new(
            entities,
            new(entities, chase, new(entities), descent),
            new(entities, objects, random),
            new(entities, chase, descent, random),
            new(entities),
            new(entities, objects, descent, random));
    }
}
