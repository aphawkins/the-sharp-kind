# Bubble Bobble: the super bonus room's own picture and tile

[BubbleBobbleSharp] Left out of item effect 32 (`SuperBonusRoom`, `$3621`), part of step **6** of section 5 of
[bb-port-plan.md](../bb-port-plan.md).

The room plays, but `BubbleBobbleMain.ShowSuperRoom` draws it as `Playfield.Build(SuperBonusRoom.Map(level))`: the room's
platforms in the level's own tile, with the usual edges and shadows. The game draws it in characters of its own:

- `$3639` copies all 256 bytes of `$A660` over characters `$60`-`$7F` (`$4300`), and `$3644` makes `$76`, `$77`, `$78` plain
  patterns. `$3660` puts `$A660`'s first 50 bytes in `$40A8` (characters `$15`-`$1B`), which `$392A` copies on to `$4080`
  (`$10`-`$14`): the room's tile is none of the hundred levels'.
- The picture: `decompress_level_data` with SUBFLG at `$4C` and `$E2C3` from `$8C00` draw the chambers in the middle (`$65`-`$6F`)
  and the door at the bottom (`$70`-`$75`); `$36A2` fills rows 17 to 21 with `$77` and `$78`; `$370C` puts two blocks of `$46`
  at (2, 2) and (28, 2).
- `$36FF`-`$370A` write `$74`, `$35` and `$06` to `$BC`, `$BD` and `$C0`, and `$370C` calls `$2BBD` and `$2C32`: probably the
  room's sprites. Not looked into.
- The room comes in with `$37C9`'s scroll, and goes after the flash on a cleared screen (`$37A9`) before the level after scrolls in.

Fix direction: export the room's characters (from `$A660`, as the other sheets are from the game's data) and give the room a
picture the 8-bit rendition draws from its screen codes, read from the game's screen (`$5400` at `$31A2`, as
`SuperBonusRoomOracleTests` does for the map).
Verify: the room's screen codes against the game's at `$31A2`, and a screenshot.
