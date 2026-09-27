// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal static class EntityAnimation
{
    private const byte Period = 0x02;

    internal static void Step(EntityTable entities, int slot)
    {
        ArgumentNullException.ThrowIfNull(entities);

        if (!Due(entities, slot))
        {
            return;
        }

        entities.Frame[slot]++;

        if ((entities.Frame[slot] & entities.FrameMask[slot]) != 0)
        {
            return;
        }

        entities.Frame[slot] -= entities.FrameCount[slot];
    }

    internal static void Toggle(EntityTable entities, int slot)
    {
        ArgumentNullException.ThrowIfNull(entities);

        if (Due(entities, slot))
        {
            entities.Frame[slot] ^= 0x01;
        }
    }

    private static bool Due(EntityTable entities, int slot)
    {
        entities.AnimationTimer[slot]++;

        if (entities.AnimationTimer[slot] < Period)
        {
            return false;
        }

        entities.AnimationTimer[slot] = 0;
        return true;
    }
}
