# Bubble Bobble: the port's level map is not the game's for about 45 levels

[BubbleBobbleSharpLib] Found 2026-09-29 by `SolidMapOracleTests` (explicit), which compares the port's `SolidMap` for
every level with the game's `$8500` map, cell by cell, as the game holds it at the level's first pass.

Levels 3-6, 8, 10, 12, 16, 18-21, 23, 27, 28, 32, 33, 35, 36, 39, 45, 48, 49, 52, 55-57, 66, 70, 72-76, 78-88, 94, 96 and 99
differ (level 1, 2 and the rest agree). Some differ by a few cells' bubble current, and levels 72-87 by hundreds.

The game builds the map in three steps:

1. `$E1D1` fills each row with `$8B03 + 4 x row` and 3: the bubble current in the level's right-hand wall byte.
2. `$E239` fills every zone rectangle with its type (0-3), overwriting **whole bytes**, so a rectangle also clears a
   solid bit. The mirror command (`$E24C`) copies the left half to the right, flipping bit 1 of a cell whose bit 0 is set.
3. `$3998` (in `D_392A`) then ORs `$80` into the map cell for every screen cell whose character is `$10` to `$1D`. The
   solid bit therefore comes from the tile drawn, after the rectangles, not from the bitmap alone.

The port builds it as: row defaults, solid bits from the bitmap, then rectangles that change only the current. To fix it,
translate the three steps (the zone data and its redirects are in `zone-data.bin`), or find which levels draw tiles that
are not `$10` to `$1D`. The rows 0 and 24 openings differ too (level 73's bottom row has current 2 in its openings).

Effects: which way bubbles drift, which cells the spawner and the dispatcher treat as solid. Verify: the explicit test
passes.
