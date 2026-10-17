# Bubble Bobble: retire the column views (3/3 of entity records)

Move callers from `table.X[slot]` to `var e = table.Slot(slot); e.X`, so each
method works on one entity, then delete `SlotColumn<TSlot>` and the column
properties. `EntityTable.ClimbNext` is still a separate `bool[]`: make it a
`bool` on `EntitySlot`. `BubbleGlide.Bubbles` and the sprite model take
`ToArray()` copies of columns meanwhile; they go with the views.
