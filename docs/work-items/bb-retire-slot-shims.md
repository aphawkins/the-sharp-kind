# Bubble Bobble: retire the column views (3/3 of entity records)

Move callers from `table.X[slot]` to `var e = table.Slot(slot); e.X`, so each
method works on one entity, then delete `ObjectColumn` and the column
properties. Blocked by entity records 2/3.
