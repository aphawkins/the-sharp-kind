# Bubble Bobble: item effect 32, the super bonus room

[BubbleBobbleSharp] Part of step **6** of section 5 of [bb-port-plan.md](../bb-port-plan.md). Effect 32 (`$3621`) is still `Untranslated`.
Song 11 (`Song.SuperBonus`) goes with it. Wants a plan and sign-off before code (AGENTS.md).

A room of its own on the item rooms' collection loop (`$3193`, `$31A2`, which `ItemRoom` already runs): the screen is cleared and a
made map drawn (the walls and platforms of `$A660`'s table, two blocks by `$344A`, the tiles `$46`/`$42`), 18 items are placed from
the tables `$A710` (columns) and `$A722` (rows) and their mirrors (`$1E` minus the column), the players stand on the floor (X `$2C`
and `$EC`, Y `$9D`), the song is `Song.SuperBonus`, and the hook `$3771` sends a player at X `$8C` down to Y `$DD` once the count
`$597F` (`$25` at the start) is below 14, and, at count 1 and Y `$DD`, ends the room with a 16 frame flash and `$21` down. Not a
copy of `ItemRoom`: the map, placement and exit differ. The rebb64 comments and labels here are wrong.
Verify: a capture of the room.
