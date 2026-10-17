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

        entities.Slot(slot).Frame++;

        if ((entities.Slot(slot).Frame & entities.Slot(slot).FrameMask) != 0)
        {
            return;
        }

        entities.Slot(slot).Frame -= entities.Slot(slot).FrameCount;
    }

    internal static void Toggle(EntityTable entities, int slot)
    {
        ArgumentNullException.ThrowIfNull(entities);

        if (Due(entities, slot))
        {
            entities.Slot(slot).Frame ^= 0x01;
        }
    }

    private static bool Due(EntityTable entities, int slot)
    {
        entities.Slot(slot).AnimationTimer++;

        if (entities.Slot(slot).AnimationTimer < Period)
        {
            return false;
        }

        entities.Slot(slot).AnimationTimer = 0;
        return true;
    }
}
