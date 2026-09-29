# Bubble Bobble: the port's level map is not the game's for 21 levels

[BubbleBobbleSharpLib] Found 2026-09-29 by `SolidMapOracleTests`, which compares the port's `SolidMap` for every level
with the game's `$8500` map, cell by cell, as the game holds it at the level's first pass. `SolidMap` now runs the game's
command list in order (mirror included), and the zone data is exported from the game's encoded bytes, which cut the
levels that differ from about 45 to 21. The test skips the remaining ones (`s_gaps`): 36, 48, 52, 70 and 72-87.

Levels 36, 48, 52 and 70 differ by a few cells' bubble current. Levels 72-87 differ because the game's first-pass map
lacks some platforms; level 76's platforms are drawn in characters `$C0` and up, which `$3998` does not count. The cause
is not found.

The game builds the map in three steps:

1. `$E1D1` fills each row with `$8B03 + 4 x row` and 3: the bubble current in the level's right-hand wall byte.
2. `$E239` fills every zone rectangle with its type (0-3), overwriting **whole bytes**, so a rectangle also clears a
   solid bit. The mirror command (`$E24C`) copies the left half to the right, flipping bit 1 of a cell whose bit 0 is set.
3. `$3998` (in `D_392A`) then ORs `$80` into the map cell for every screen cell whose character is `$10` to `$1D`. The
   solid bit therefore comes from the tile drawn, after the rectangles, not from the bitmap alone.

Steps 1 and 2 are translated. Step 3 is still the bitmap's solid bits; finding what `$3998` does for levels 72-87 is what is left.

Effects: which way bubbles drift, which cells the spawner and the dispatcher treat as solid. Verify: `SolidMapOracleTests` passes with `s_gaps` empty.
