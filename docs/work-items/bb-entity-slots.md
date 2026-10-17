# Bubble Bobble: EntityTable as an array of typed slots (2/3 of entity records)

[BubbleBobbleSharpLib/Players/EntityTable.cs] The same change as `ObjectTable`
got (`ObjectSlot`, `ObjectColumn`, `Slot(i)`) for the 25 arrays of 8, with 989
test accesses. `IRowTails` (`Tail(row, index)`) becomes a field switch on the
slot. `ClimbNext` is a `bool` column, so it needs its own view. The test
`RamImage` loader already maps RAM addresses to `ObjectField` selectors; do
the same for `EntityTable`.
