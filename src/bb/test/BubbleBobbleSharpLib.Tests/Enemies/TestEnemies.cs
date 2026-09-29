// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// $1CBD's enemy arm with every handler behind it, wired the one way the game wires them.
internal static class TestEnemies
{
    // The AI of the objects of types $24 to $32, on a food drop and fire strips of their own.
    internal static ObjectAi Ai(ObjectTable objects, EntityTable entities, BbRandom random)
    {
        FoodDrop food = new(entities, new Scores(), random);

        return new(objects, entities, random, new(entities, food), new(objects, entities), food);
    }

    internal static EnemyFrame Frame(EntityTable entities, ObjectTable objects, BbRandom random)
        => Frame(entities, objects, random, new());

    internal static EnemyFrame Frame(EntityTable entities, ObjectTable objects, BbRandom random, Scores scores)
        => Frame(entities, objects, random, scores, new(new()));

    internal static EnemyFrame Frame(EntityTable entities, ObjectTable objects, BbRandom random, Scores scores, LevelFlow flow)
        => Frame(entities, objects, random, scores, flow, new(entities, random));

    internal static EnemyFrame Frame(
        EntityTable entities,
        ObjectTable objects,
        BbRandom random,
        Scores scores,
        LevelFlow flow,
        WaterFlow water)
    {
        EnemyChase chase = new(entities);
        PlayerDescent descent = new(entities);

        return new(
            entities,
            new(entities, chase, new(entities), descent),
            new(entities, objects, random),
            new(entities, chase, descent, random),
            new(entities),
            new(entities, objects, descent, random),
            new(entities, scores, random),
            flow,
            water);
    }
}
