// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Tests;

// The tables' fields a column at a time, for the tests that load a captured image or compare a whole field. The game
// works on one slot at a time (`table.Slot(i)`).
internal static class TableColumns
{
    extension(ObjectTable objects)
    {
        internal SlotColumn<ObjectSlot> Type => Column(objects, static slot => ref slot.Type);

        internal SlotColumn<ObjectSlot> X => Column(objects, static slot => ref slot.X);

        internal SlotColumn<ObjectSlot> Y => Column(objects, static slot => ref slot.Y);

        internal SlotColumn<ObjectSlot> SubX => Column(objects, static slot => ref slot.SubX);

        internal SlotColumn<ObjectSlot> SubY => Column(objects, static slot => ref slot.SubY);

        internal SlotColumn<ObjectSlot> Column => Column(objects, static slot => ref slot.Column);

        internal SlotColumn<ObjectSlot> Row => Column(objects, static slot => ref slot.Row);

        internal SlotColumn<ObjectSlot> Flags => Column(objects, static slot => ref slot.Flags);

        internal SlotColumn<ObjectSlot> Behaviour => Column(objects, static slot => ref slot.Behaviour);

        internal SlotColumn<ObjectSlot> State => Column(objects, static slot => ref slot.State);

        internal SlotColumn<ObjectSlot> Variant => Column(objects, static slot => ref slot.Variant);

        internal SlotColumn<ObjectSlot> EnemyType => Column(objects, static slot => ref slot.EnemyType);

        internal SlotColumn<ObjectSlot> Direction => Column(objects, static slot => ref slot.Direction);
    }

    extension(EntityTable entities)
    {
        internal SlotColumn<EntitySlot> State => Column(entities, static slot => ref slot.State);

        internal SlotColumn<EntitySlot> X => Column(entities, static slot => ref slot.X);

        internal SlotColumn<EntitySlot> Y => Column(entities, static slot => ref slot.Y);

        internal SlotColumn<EntitySlot> Frame => Column(entities, static slot => ref slot.Frame);

        internal SlotColumn<EntitySlot> AnimationTimer => Column(entities, static slot => ref slot.AnimationTimer);

        internal SlotColumn<EntitySlot> BubbleTimer => Column(entities, static slot => ref slot.BubbleTimer);

        internal SlotColumn<EntitySlot> RiseCounter => Column(entities, static slot => ref slot.RiseCounter);

        internal SlotColumn<EntitySlot> FallCounter => Column(entities, static slot => ref slot.FallCounter);

        internal SlotColumn<EntitySlot> LeftFlag => Column(entities, static slot => ref slot.LeftFlag);

        internal SlotColumn<EntitySlot> RightFlag => Column(entities, static slot => ref slot.RightFlag);

        internal SlotColumn<EntitySlot> GroundState => Column(entities, static slot => ref slot.GroundState);

        internal SlotColumn<EntitySlot> Colour => Column(entities, static slot => ref slot.Colour);

        internal SlotColumn<EntitySlot> Mode => Column(entities, static slot => ref slot.Mode);

        internal SlotColumn<EntitySlot> HoldTimer => Column(entities, static slot => ref slot.HoldTimer);

        internal SlotColumn<EntitySlot> FlashTimer => Column(entities, static slot => ref slot.FlashTimer);

        internal SlotColumn<EntitySlot> Heading => Column(entities, static slot => ref slot.Heading);

        internal SlotColumn<EntitySlot> SpriteBase => Column(entities, static slot => ref slot.SpriteBase);

        internal SlotColumn<EntitySlot> TurnTimer => Column(entities, static slot => ref slot.TurnTimer);

        internal SlotColumn<EntitySlot> TurnInterval => Column(entities, static slot => ref slot.TurnInterval);

        internal SlotColumn<EntitySlot> FrameCount => Column(entities, static slot => ref slot.FrameCount);

        internal SlotColumn<EntitySlot> FrameMask => Column(entities, static slot => ref slot.FrameMask);

        internal SlotColumn<EntitySlot> LeapFlag => Column(entities, static slot => ref slot.LeapFlag);

        internal SlotColumn<EntitySlot> ClimbFlag => Column(entities, static slot => ref slot.ClimbFlag);

        internal SlotColumn<EntitySlot> AttackTimer => Column(entities, static slot => ref slot.AttackTimer);
    }

    private static SlotColumn<ObjectSlot> Column(ObjectTable objects, SlotField<ObjectSlot> field)
        => new([.. Enumerable.Range(0, ObjectTable.Capacity).Select(objects.Slot)], field);

    private static SlotColumn<EntitySlot> Column(EntityTable entities, SlotField<EntitySlot> field)
        => new([.. Enumerable.Range(0, EntityTable.Capacity).Select(entities.Slot)], field);
}
