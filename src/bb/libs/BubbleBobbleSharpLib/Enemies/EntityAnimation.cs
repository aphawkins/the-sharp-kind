// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $EB0F and $EE4A in entity-system.s: the two ways an enemy's frame steps on.
//
// $EB0F is where nearly every enemy path ends. Most of the walker's branches finish with `jmp
// L_EB0F`, and $1E87 falls back to it for any state it does not name, so an enemy held by its spawn
// delay still walks on the spot.
internal static class EntityAnimation
{
    // $EB16. A step every second call.
    private const byte Period = 0x02;

    // $EB0F. The frame steps on, and when the step leaves it a whole cycle along, the count is taken
    // back off. A thing facing left, whose frames start at the count, stays in the second block.
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

    // $EE4A. The same counter, and bit 0 of the frame flipped instead.
    internal static void Toggle(EntityTable entities, int slot)
    {
        ArgumentNullException.ThrowIfNull(entities);

        if (Due(entities, slot))
        {
            entities.Frame[slot] ^= 0x01;
        }
    }

    // $EB0F to $EB1B. The `cmp` is unsigned, so a counter that somehow passed two resets on the next
    // call rather than running round.
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
