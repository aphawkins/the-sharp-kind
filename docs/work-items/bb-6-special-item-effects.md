# Bubble Bobble: the other special-item effects

[BubbleBobbleSharp] Step **6** of section 5 of [bb-port-plan.md](../bb-port-plan.md). The plan holds the
detail — addresses, what is already done, and how the step is proved. Read it
there and update it as the step lands.

Items 1, 2, 6–8, 11 and 15–34 in `$2D65`. Verify: a capture per effect.

## What is left, and what the game does (2026-09-29, read from the game run on `SharpKind.Emulation`)

Effects 7, 29 and 32 are the only ones still `Untranslated`. Each is a subsystem of its own, so each wants a plan and sign-off
before code (AGENTS.md). Song 11 (`Song.SuperBonus`) goes with 32. The rebb64 comments and labels for all three are wrong.

**Effect 7 (`$A653`), the giant bubble.** The effect only writes `$58FF` = 2 to 5 (`$E9EA` and 3, plus 2): the sweeps to make.
`$0A3B` calls `$1AB6` while `$58FF` is not 0 and `$593F` is 0:

1. `$1AB6`-`$1B19`: pause (`$37`); object slots 0 and 1 (the players' bubbles) that hold a type `$18`-`$23` (a caught enemy) save it in
   `$AA42` and every one of them becomes type `$3A` (pops); wait, a frame at a time (`$E90E`, `$1844`, `$1B2E`), until both slots
   are free; put the level's characters back from the backup (`$1B29`); flash the border four frames; slots 0 and 1 become type
   `$46` at row 2 and column `$16` + a roll and 7; `$593F` = 4; unpause.
2. Every pass after (`$198A`-`$1A5D` in the render pass, `L_19CF`): the giant goes one cell left and one down (slot 0's `$DC` down,
   `$EE` up); each of the 16 first object slots with a caught enemy (type `$18`-`$23`) within `$30` in each direction of it is
   popped (`$38`) and its enemy becomes an item (`$1A6F`, state `$0B`); at row `$16` slots 0 and 1 become `$38` then `$36`
   and go. Then `$593F` is 0 and `$58FF` counts down one, and the next sweep starts.
3. It is drawn from characters (`$3F28`), which needs a view of its own.

Seen in the game: a 3-sweep effect ran 22 passes to 84 frames a sweep, the giant one cell in about 3 frames.

**Effect 29 (`$35B0`).** Six enemy slots (2-7) become state `$14`: a giant made of the six sprites in a 3 by 2 block (X `$78`, `$90`
`$A8`; Y `$7C`, `$91`), which walks about (X `$54`, `$84`, `$B4`; Y `$67`), and at frame 120 the six are three items (state `$0B`).
`$4A` (the enemy count) goes to 1 and then 0. `D_859B..` set Y `$F9` down as it comes in.

**Effect 32 (`$3621`), the super bonus room.** A room of its own on the item rooms' collection loop (`$3193`, `$31A2`, which
`ItemRoom` already runs): the screen is cleared and a made map drawn (the walls and platforms of `$A660`'s table, two
blocks by `$344A`, the tiles `$46`/`$42`), 18 items are placed from the tables `$A710` (columns) and `$A722` (rows) and their
mirrors (`$1E` minus the column), the players stand on the floor (X `$2C` and `$EC`, Y `$9D`), the song is `Song.SuperBonus`, and
the hook `$3771` sends a player at X `$8C` down to Y `$DD` once the count `$597F` (`$25` at the start) is below 14, and, at count 1
and Y `$DD`, ends the room with a 16 frame flash and `$21` down. Not a copy of `ItemRoom`: the map, placement and exit differ.
