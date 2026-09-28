# Bubble Bobble Conversion Notes

How each class maps to the 6502 in `rebb64/src/`, one section per class,
grouped by folder.
Addresses are the reference's. See [bb-port-plan.md](bb-port-plan.md) for
the plan and the proofs.

## Bubbles

### BubbleBlow

`$22E8` and `$2301` (`entity-interaction.s`): fire, and the six frames after.

- `$22E8` does not make a bubble. It puts `$06` in the bubble timer and
  latches the facing (`$22F4`: bit 2 of the frame, halved, because `$230E`
  adds it to `$ACC4`). It returns if still reloading, or if the timer is not
  negative (a blow running, or the player in a bubble).
- `$2301` runs from `$2165` every frame the timer is not negative. It
  decrements the timer on both arms. At zero (`$2303`) the sprite goes back
  to the plain facing frame and the timer to `$FF`. Otherwise the frame is
  `facing + $ACC4[timer]`, and the bubble is made at timer `$04` (`$231B`).
  So fire and bubble are three frames apart.
- `$2321`–`$23E9`, the spawn. `$232A` works the cell out from the pixel
  position again, not from `$1E6C`'s cell. `$2341`: a player above the
  playfield borrows, and `$2349` abandons the spawn, leaving the four bytes
  already written in a slot that is still free. `$2352` writes type `$16`
  and `$7D` to `$A9FA`/`$A9E8`. `$2364` starts the reload at the bubble, not
  the button, so held fire is a fourteen-frame cycle.
- `$2385`: facing right (frame `$09`) moves the bubble a column right,
  because the mouth is at the sprite's far end; direction `$00`, else `$80`.
  `$2399`: a player off the ground (`$87A0` positive) and low in the cell
  (`FineY` ≥ 4) blows into the row below.
- `$23D7`: the blow ring's ten points.
- `$0A28` (`game-loop.s`) clears the reload, once a frame before the entity
  update, so a reload is a frame shorter than its eight.
- Not translated, gated off by level setup: `$23B8`'s arm on `$A783` (type
  `$44`) and `$23DE`'s arm on `$37C7` (the flag byte).

### BubblePop

`$E90E`, the sprite renderer's pass, and `$E97F` after it. It is the
renderer's, not the AI's. `$E90E` draws the eighteen slots from `$11` down,
and jumps through a vector per type: `jmp ($040C + type)`. Most vectors only
draw. These change a slot:

| Type | Vector | What it does |
|---|---|---|
| `$16` | `$7BD4` | A bubble just blown becomes `$00` before it is drawn. |
| `$18`–`$22` | `$3CB2` | A caught enemy: its own hardware sprite (slot 2 on) moves onto the bubble, with the frame `$1090` kept in `$AA42`. |
| `$34` | `$3D2D` | A touched bubble (`$0D02`). Every bubble within `$18` pixels pops with it, for the same player; then `$3D77`. |
| `$3A` | `$3D77` | A bubble whose time ran out (`$13BE`). |
| `$3C`, `$3E`, `$40`, `$38`, `$36` | `$3CD3`, `$3CDB`, `$3CDF`, `$3DA9`, `$7BDB` | The pop's frames, one a pass. `$36` empties the slot. |

- `$3D2D`: the AI counter tested is the popping slot's, so a bubble still
  being moved pops nothing else. `$3D66` keeps the type the bubble had.
- `$3D77`: a bubble with an enemy in it (`$3D7A`, `$3D7E`: `$18` plus twice
  the enemy) lets the enemy out dead through `$1A6F` (`$3D82`), in the state
  `$1090` kept, and counts it in the player's chain. `$3D91` turns the
  enemy's sprite back on, whichever way `$142E`'s flicker left it. Any other
  bubble goes to `$3DB0`.
- `$3DB0`: ten points if a player popped it, a hundred for types `$06`–`$0B`
  (`$3DB6`–`$3DC0`), a letter of EXTEND for `$0C`–`$15` (`$3DD5`, `$3DD9`),
  then pop frame `$3C`.
- `$3DDD`: one bit per letter (`$AB53` on into `$AB55`), in twos from `$0C`:
  E, X, T, N and D. The fourth bit is the second E, so only `$0C` reaches it,
  and only once the player holds the first.
- `$E97F`: scores each player's chain (`$46`, `$47`) once it stops growing
  (`$48`, `$49` hold it as `$E90E` began): `$AB52` by count, 1000 for one
  enemy, doubling after.
- `$E931`'s `bmi`: nothing at `$80` or above is drawn or stepped.
- Not translated: the special bubbles' own arms at `$3E02` (`$3E2C`,
  `$3E5E`: types `$06`, `$0A`, `$08`). They throw.

**What it drew.** `Drawn` is each slot's cell in `object-sprites.tga`, taken
from the type at the start of the step:

| Types | Vector | Cell |
|---|---|---|
| `$00`–`$14` | `$E779` | `type × 2 + $A9C4` (0–43) |
| `$34`, `$3A`, `$3C` / `$3E`, `$40` | `$3CE5` | 44 + (0 or 4) + `$A9C4` |
| `$28` | `$3EFD` | 58 + `$A9C4`, from `$A826`/`$A82A` |
| `$2A` | `$E752` | 56 + `$A9C4`: `$AA54` entries 56–57 |
| `$2C` | `$3ED4` | 62 + `$A9C4`, from `$AB5B`/`$AB5F` |
| `$2E`, `$30` (the Baron) | `$E767` | 52 + `$E765[slot]` + bit 1 of `$597F`: `$AA54` entries 48–51 |
| `$32` | `$3E77` | 66 |

`DrawnSubY` is OLDTXT: `$A9D6`, but zero for a shot, whose vector clears it.
`Counter` is `$597F`, which `$E90E` counts up every pass. A shot's `$A9C4`
past its table would read the next table, so it throws.

### BubblePush

`$0AAB` (`collision.s`): a player walking into a bubble pushes it. The
reference calls it player-enemy collision; it is not. It walks the eighteen
object slots, player 2 first, then player 1, slot 17 down.

- A player in state 1 (`$0AB0`) with frame 0–3 nudges an object on their
  right four pixels right; frame 4–7, one on their left four pixels left
  (`$0AB7`, `$0ABD`). Only state-0 objects below type `$24` move (`$0ACB`,
  `$0B5D`). The box is one-sided in X and absolute in Y (`$0AD6`, `$0B68`,
  `$0AE7`/`$0B79`).
- `$0ADA`–`$0AE7`: the absolute difference; a borrow leaves the carry clear
  for the `adc #$01`.
- Left arm: `$0AF2`'s `sbc #$03` takes four (carry clear from the `bcc`);
  `$0AFA`'s `sbc #$02` runs with the carry the X subtraction left.
  `$0B24`–`$0B39` read the cell's column down three rows (the first only when
  `SubY` is not zero; one past a forty-byte row is the next row). In a wall,
  `$0B3B` goes back a column and snaps to the grid: `sbc #$14` sets the carry
  `adc #$07` adds, which sets the carry `adc #$14` adds.
- **The left arm loses the player.** `$0B08` parks the player in `$3C` and
  loads the row into Y. A row outside `$04`–`$1C` branches (`$0B0E`, `$0B12`)
  to the loop's `dex`, not `$0B51`, so the rest of the walk, and the `dey`
  that picks the next player, run with the row as the player index. `Peek`
  maps every `$B2,y`, `$BA,y`, `$C2,y`, `$CA,y`, `$DC,y`, `$EE,y` and
  `$8520,y` read to its table; any other read throws.
- Right arm (`$0B56`–`$0BEA`): `$0B83`'s carry is clear from the `bcc`;
  `$0B8B`'s `adc #$02` takes the X addition's carry. It clamps the row
  (`$0BA0`–`$0BAC`). `$0BD4` goes back a column and snaps; `and` keeps the
  carry `sbc #$1C` left.
- An object's row is four more than `SolidMap`'s.

### EntityMover

`$0E23` (`enemy-ai.s`): two pixels in one of four directions, picked by the
low two bits of A, with the cell bookkeeping. The mover every object shares;
found with a store watchpoint on a rising bubble's Y (`$0E30`, `$0E33`). The
eight-pixel shot a bubble opens with is the dispatcher's, at `$0F98`.

- Up (`$0E27`): the row steps when the sub-position comes back to zero.
  `$0E33`'s `and #$07` turns the borrow into a wrap. `$0E3D` tests the row's
  top bit, so the row before zero is `$FF`.
- Down (`$0E75`): the row steps when the sub-position reaches two
  (`$0E90`), a step after the wrap, not on it. The reference's own asymmetry.
- Right (`$0E55`): two pixels, since `$0E52`'s `cmp #$01` leaves the carry set
  for `$0E56`'s `adc #$01`. `$0E6F`'s fall-through into the down arm at a
  column wrap is unreachable and not reproduced.
- Left (`$0EAD`): the sub-position borrows into the column.
- `$0E39`, `$0E95`: a row off either end comes back at the other.
  `$0E41`/`$0E9E`/`$0E45`: a thing that wraps becomes `$38` unless already a
  high type.
- `$0E5F`, `$0EB4`: the horizontal sub-position counts quarters of a cell.

### ObjectTable

The eighteen slots `$CA` indexes: bubbles, items, shots and caught enemies.
`EntityTable` is the other table, eight slots at `$85xx`–`$88xx`.

| Field | Address | Notes |
|---|---|---|
| `Type` | `$CA` | `$FF` is free. Not an "enemy type array". |
| `X`, `Y` | `$AA0C`, `$AA1E` | Pixels, in the players' units. |
| `SubX`, `SubY` | `$A9C4`, `$A9D6` | Past the cell. `$2334` halves X; `$2350` does not halve Y. |
| `Column`, `Row` | `$DC`, `$EE` | Stored, not derived: later routines write them apart. |
| `Flags`, `Behaviour` | `$A9FA`, `$A9E8` | `$7D` at a blow. `$A9E8` is the AI routine index. |
| `State`, `Variant`, `EnemyType` | `$A9B2`, `$AA30`, `$AA42` | Copied from the per-player tables at `$2385`. |
| `Direction` | `$0193` | `$00` or `$80`. |

- `$0620`/`$062B` (`$05C5`): types to `$FF`, `$DC` and `$EE` to zero in one
  `$24`-byte run.
- `FindFree` is `$2321`: slot 17 down, first negative type wins; -1 where
  the 6502 falls out of the loop at `$2329`.

## Enemies

### Baron

`$1473`–`$1577` (`player-sprites.s`): Baron Von Blubba, who comes for a
player who takes too long.

- **Not the routine the reference names.** `special-enemies.s` heads `$10D3`
  and type `$44` as the Baron. In VICE (2026-09-23, level 1, enemies
  cleared) no `$44` appeared: when the hurry-up ran out, `$1677` put type
  `$2E` in object slot 0, and later player 1 was in state `$0E`. `$0D4E`
  sends types `$2E` and `$30` to `$1473`, which the reference calls
  `player_death_handler`. `$44` is a bubble type, written by `$23D5`.
- One Baron per player, in the object slot of the player's own number: slot
  0 is `$2E` (`$148A`) and chases player 1, slot 1 is `$30` (`$148E`). So
  `$1473` reads `$B2`, `$BA` and `$C2` with the object slot as the index.
- He moves one cell a pass, a run along one axis then the other, through
  walls, and pauses `$1E` passes between pairs of runs (`$14C6`).
- `$14A7`: a run over starts the next, and the pause. `$14B0`–`$14C8`:
  `$AA30`'s low four bits count the runs, never to a multiple of sixteen, and
  its top bit flips each time; `$AA42` takes the result as the new run.
  `$14F7`: bit 7 picks the axis, the low seven bits count it.
- `$14D9`, `$14EF`: the player's cell, `$E9B8` without its biases (`$14`,
  `$15`). `$1500`, `$1512`: one cell towards the target; an arrived run ends.
  `$14E4`, `$14E8`: the sprite faces the player (`$00` right, `$02` left).
- `$152B`: catches either player, the second first; the first caught is the
  only one. The caller changes `$152B`'s two compares to `$18` and `$0E`, so
  a player in state `$18`, or any live state below `$0E`, can be caught
  (`$1496`–`$14A2`). The box is `$10` pixels each way (`$1558`, `$1568`),
  from the cell's corner (`$18`, `$15`); the `asl`s leave bit 5 in the carry
  for the `adc`.
- `$1479`: the Baron goes (type `$3A`) when his player dies (`$0E`,
  `$1475`, `$156E`).
- `$A775` (pause) and `$E765` (the facing the composer draws), two bytes
  each, start at `$16`,`$00` and `$02`,`$00` and are never reset, so a Baron
  takes over what the last one in his slot left.
- Not translated: `$1621`, which puts him in play, and `$1490`'s store of
  `$32` in `$2B`. Both are the level timer's.

### BubbleCollision

`$105B` and `$1090` (`enemy-ai.s`): whether a moving object has met an
enemy, and the capture. The dispatcher calls it down each arm of `$0F98`,
after the eight-pixel step and before any terrain probe.

- `$105B` walks `$B4`, `$BC`, `$C4` with Y from 5 down: `EntityTable` slots
  2–7. The last enemy first, and the first in range wins (`$1090` ends in an
  `rts`).
- `$105D`/`$1060`/`$1064`: an empty enemy, or one in `$0B`–`$15`, is passed
  over. `$1077`, `$1088`: a square box of `$10` pixels.
- `$1091`/`$1093`: type becomes `$18` + enemy × 2.
- `$1098`: only an enemy at `$0A`, or `$16` and above, has its three
  counters reset, and then the byte stored to `$AA30` at `$10B1` is its rise
  counter, not its state: `cmp` leaves A as `$1095`'s `lda $B4,y` on the
  short path.
- `$10B4`: the enemy leaves the eight slots and the object loses its AI
  counter. `$10BB`: `$A9FA` gets `$A0`.
- `$10BF`: the caught frame, `$AB81` indexed by the byte just stored to
  `$AA30`. States 8 and 9 are reached, so the read runs on into `$AB89`
  (`FoodDrop`'s animation offsets); those bytes are carried. Beyond them it
  throws. `$3CB2` puts the frame in the enemy's `$8520`.
- `Check` returns the capture. The 6502 infers it: `$0FB4`'s `cmp #$06` on
  the new type sends the move home.

### DiagonalMover

`$EFC0` (state 5, class 3) and `$EEB2` (state 6, class 4) in
`entity-system.s`: things that travel diagonally and bounce.

- One pixel up or down (`$EFC0`) or two (`$EEB2`), and two across (`$EF08`,
  `$EF47`), every pass. `$85E8`: bit 0 is left, bit 2 is up. A solid cell
  turns only the axis that met it (`$EF0D`: EOR 3; `$EF8F`: EOR `$0C`).
  `$EF98` adds the step, so up's `$FF` wraps.
- `$EFC0`'s cell is `$1E6C`'s, for the vertical half only; `$EFD6` calls
  `$E9B8` again before the horizontal half. `$EFC7` saves the heading first.
- `$EF4C` (up) probes only on a row boundary, one row up (`$EF70`: minus
  forty). `$EFA0` (down), two rows down (`$EFBC`), into the same loop at
  `$EF7D`. The third cell counts only off a column boundary.
- **The wrap.** `$EF4C` and `$EFA0` are the only code that reads the wrap
  tables at `$8501` and `$88C1`, `SolidMap`'s rows 0 and 24. At the top
  (`$EF5A`, equality) or bottom (`$EFA6`) edge, an open cell (`$EF66`,
  `$EFB2`, one column in) sends the thing out at the other end without moving
  further; a solid one turns it (`$EF5E`, `$EFAA`).
- The two wrap checks are a pixel apart. The reference says each `sbc` runs
  with the carry clear; both run with it set (a `bne`/`bcc` not taken), so
  `$EF60` takes `$13` and `$EFAC` takes `$14`.
- `$EEEB` (left) and `$EF2A` (right) probe three cells on a column boundary,
  the third only off a row boundary. State 5 enters them with `$60` (`rts`)
  in `$EF15`; state 6 with `$BD`, so a turn also flips the frame
  (`lda $8520,x` / `eor #$04` / `sta`).
- State 6 faces first and moves on the next pass (`$EEDF`, `$EF1E`: frames
  0–3 right, 4 on left, from `$1E6F`'s copy). Its frame steps through
  `$EB0F`, not `$EFEA`.
- `$EFEA`: every second pass, four frames; reached whatever the halves did.
  `$1E87` calls it alone for a thing not free to move.

### Distance

`$0D1A`, `$102B` and `$106F`: subtract, and negate if it borrowed. The
negate is `eor #$FF` then `adc #$01` with the carry clear from the borrow:
a true two's complement, not one off.

### EnemyAiLoop

`$0CF2` (`enemy-ai.s`, `enemy_ai_update`): the walk over all eighteen object
slots once a frame, slot `$11` down (the same direction as `$2321`'s search).
Everything in `ObjectTable` moves through it, bubbles included. The movement
dispatcher at `$0F48` is `EnemyDispatcher`.

- `$0CF4`'s `cmp #$24`: types `$24` up get no AI; `$0D4E` routes them by
  value. Below `$34` the type indexes a jump table, and `$0D52`'s `sbc #$23`
  runs with the carry clear (`$0D50`'s `bcs` fell through), so `$24`–`$33`
  index it as 0–15. Low and high bytes are interleaved one apart, so only the
  even types reach a whole address: `$2E`/`$30` reach `$1473` (the Baron);
  the other six are not translated. `$0D56` gates the table on `$67`, which
  nothing translated writes. At `$34` up, `$44` reaches `$10D3` and `$4C`
  reaches `$0EC5`, neither translated; anything else moves on.
- `$0CFF`: a slot with an AI state goes to `$0F48`.
- `$0D02` (`Caught`): player 2 first; the first within range wins, and the
  loop takes `$0D4A`. Players in state `$0E` or `$0F` are not caught
  (`$0D0A`, `$0D0E`). The box (`$0D04`–`$0D34`) is `$10` pixels, an absolute
  difference in one byte per axis; `$0D14` nudges the player's X two pixels
  right first, in X only. `$0D33` stores the player to `$0193`, the old type,
  and type `$34` (`$0D3F`). `$0193` also holds a blow's facing (`$2352`),
  and `$0F98` tests its top bit, so a caught slot reads as facing right.
- `$0D44`, `$0D48`: no catch falls into `$0D86`.
- `$0D86` (`Wander`): one draw; at `$EA` or above (`$0D8B`) straight to the
  map. Otherwise `$0D8F`–`$0DFE` look for a neighbour (slot 17 down, not
  itself, no AI state, not special, `$10` pixels, no nudge: `$0DAE`,
  `$0DBF`). Only a neighbour makes it move at random; a lone thing falls into
  `$0E00`. The draw is still made.
- `$0DC2`–`$0DFA` (`RandomDirection`): a draw picks the axis, another the
  way, unless near an edge (`$0DCB`, `$0DD4`, `$0DE6`, `$0DEE`: within 4 of
  the top, `$1C` rows, 2 or `$1C` columns), when it turns to the middle.
  `$0DE4`'s `ora #$01` makes a draw sideways; `$0DC9`'s `and #$02` vertical.
- **The axis threshold is the level, not `$1E`.** `$0DC5`'s `cmp #$1E` is
  self-modified: `$F23C` stores A, the level from `$F217`'s `lda SUBFLG`. So
  level 1 always goes sideways, and deeper levels more often vertically. On
  level `$48` (`$F219`, `$F21D`) a fill loop has left `$02` in A.
- `$0E00` (`MapDirection`): the current in the cell one row below and one
  column right (`$0E1F`'s `ldy #$29`), the row clamped to `$04`–`$1C`
  (`$0E02`, `$0E08`). `$AD1E` gives `$8500` for row four (`$0E0E`–`$0E1F`).
  Clamped at the bottom, it reads `$88E8`, which `$3A6C` fills with row 24.

### EnemyChase

`$EE62` (`entity-system.s`): what an enemy may try, from its player's row.
The walker (`$EA08`) and the hopper (`$1F2E`) call it first. Level: it may
leap. Below: nothing. Above (`$EE91`): the leap and the climb take turns,
swapping each time the turn timer runs out. It keeps heading bits 0, 1 and 3
and clears bit 2, the jump up (`$EE70`, `$EE8A`). The target is the slot
`$1CF3` put in `$4B`.

### EnemyDispatcher

`$0F48`, `$0F61`, `$0F91`, `$0F98` and `$100B` (`enemy-ai.s`), with `$7BFE`
(`level-data-part2.s`): a slot that already has an AI state. The
eight-pixel half of movement; `EntityMover` is the two-pixel half.

- `$0F48`: one step, and a second for a slot whose `$AA42` is negative
  (`$0F53`) while it has state left, and whose type is not above `$0C`
  (`$0F4C`). **The index is the state before the decrement:** `$0F61`'s
  `and #$7F` masks A, which `dec` does not touch; the first call's A is from
  `$0CF8`, the second's from `$0F55`.
- `$0F66`: a negative `$AA30` halves the index, held at one, not zero.
- `$0F91`: the type comes from `$ACB6` (seven bytes, `$10 $04 $04 $02 $02
  $02 $00` by index; not a direction table), read on into `$ACBD`'s
  `$00 $00`, since a blown bubble's counter `$88` indexes 8 and 7. Past nine
  it throws.
- `$0F94`: an unblocked move leaves type `$04`. `$0F51`, `$0FE1`: above type
  `$06` it returns before probing.
- `$0F98`: eight pixels left or right (`$0FAC`, `$0FDC`), then probes. The
  arms are not mirrors: the right refuses to pass column `$1C` (`$0FD1`), the
  left has no bound; the left probes `$00`, `$29`, `$50` and the right `$01`,
  `$28`, `$51`. The move comes before the probes, so `$100B` puts it back.
- `$0FB1`, `$0FE0`: `jsr $105B` (`BubbleCollision`). A capture types the
  slot `$18` or more, which `$0FB4`'s test reads as a reason to stop.
- `$0FB8`–`$0FFE`: three probes, the first skipped on a cell boundary.
  `$7BFE` returns `$A9D6`, the vertical sub-position (not a direction).
  `$1001`: a clean move clears the state counter's top bit.
- `$100B`: put back. `$100E`–`$1014` snap X to the grid: subtract `$14`,
  `and #$F8`, `adc #$13` with the carry the subtraction left, so it adds
  twenty. A negative state becomes type `$34` (`$104E`). `$101C` tests
  player 2 only (`$BB`, `$C3`, no indexing), with the `$10` box (`$1030`,
  `$1040`); proximity decides only what goes to `$0193`. `$1052` zeroes
  `$A9C4` and the state counter.
- **The row bias is four.** An entity's row is four more than `SolidMap`'s.
  `$8500`–`$851F` is not tile data but the wrap permission and spawn tables;
  tile rows start at `$8528` (`$AC01` entry five). The wrap block reads solid
  and is `SolidMap`'s row 0, so `SolidMap` row = tile row + 1 = entity row −
  4. Level 1's platforms are on tile rows 8, 13, 18 in play and on `SolidMap`
  rows 9, 14, 19. `EntityMover`'s wrap at row `$1D` is one past the map.

### EnemyFrame

`$1CBD`'s loop over slots 7 to 2, with `$1CA0`, `$1E87` and `$1E6C`. The
6502 runs one loop from 7 to 0, so enemies go before the players
(`PlayerFrame`, slots 1 and 0) in the same frame.

- Per enemy (`$1CDB`–`$1D21`), in order: `Mode` not negative
  (`$1CDB`'s `lda $85C0,x`/`bpl`; a player's is `$FF`) is still dropping in:
  `$1CA0` moves it down two pixels (two `inc`, `$1CA9`) to the row in
  `AttackTimer`, and `$1E87` animates it. The `inc` pair is not a compare, so
  a drop that steps over its row goes round the byte. `$1CB5` leaves
  `AttackTimer` at `$0A`. `FlashTimer` not zero is angry: an extra `$1E6C`
  on frames with bit 1 of the counter clear (`$1D0A`), one and a half times
  as fast. `HoldTimer` not zero is the spawn delay: only `$1E87`. Otherwise
  `$1E6C` dispatches on the state through `$1E3A`.
- `$1CE3`–`$1CF3` (`Target`): the player on the enemy's side of the slot
  numbers if playing (`$1CEB`: state 1), else the other, unchecked.
- `$17BE`, from `$09F7` at level start (`Enter`): each enemy's row is kept as
  the drop's target, it is put at row `$15` (`$17C8`), and `$16` frames
  (`$17D1`) move it down and animate it; `$1CA0` finishes the drop in play.
  The loop ignores the mode, so one already there has its mode taken down on
  every frame left: `$00` to `$EA`.
- `$1E87` (`Idle`): state 4 by `$214A` and state 9 by `$EE4A` (the same
  toggle), state 5 by `$EFEA`, the rest by `$EB0F`.
- `$1E6C` (`Dispatch`): the cell, then the state. States 2–9 are the eight
  classes (walker, class 1, hopper, diagonal, facing diagonal, class 5,
  class 6, runner); `$0B`, `$0C`, `$11`, `$12` are a killed enemy on its way
  to food. `$E9FD` (`Shooter`): the shot first, then the walk unless a shot
  is counting down.
- Not translated: `$1CFB`'s test of `$67`, which skips enemies below state
  `$0B` while set. `$2F74`, an item effect, sets it.

### EnemyHopper

`$1F2E`–`$214A` (`player-animation.s`): state 4, class 2, an enemy that only
hops. Filed with the players' animation, but it runs for enemy slots only.

- `$1F31`: falling. `$1F41`/`$1F5B`: no ground (`$1F47`: the three cells
  under it) starts a fall; above row `$1F` (`$1F43`) it falls without
  looking.
- On the ground it hops every frame. **Up** (`$1F61`–`$1FB0`) when `$EE62`
  allows a climb and there is a platform: rise counter `$1F` (`$1FAB`), and
  the first sixteen frames are a wind-up that turns it every fourth
  (`$201E`). **Across** otherwise (`$1FD3`), rise counter `$0F` (`$1FEA`),
  after turning to face its player on a draw below `$1E` (`$1FB3`, `$1FBB`)
  when a leap is allowed. A turn lands on frame 7 or 3 (`$1FC8`, `$1FCC`,
  `$202D`, `$2031`). `$1FDB` reads `$1E6F`'s frame copy unless `$1FCE`
  replaced it.
- `$1FEF` (`Hop`): A holds the rise counter from before the `dec`.
  **The spring is the frame's low two bits** (`$1FFB`, `$204E`): it does not
  rise until they are below 2 (`$1FF6`: one off each frame). `$2006`: a leap
  rises a pixel and moves across; up rises by `$ACDD`.
- `$2037` (`Fall`): at most `$10` frames (`$2042`); stops when the spring
  bits are 2 or more. `$2070`: a floor is looked for only crossing into a new
  row, from the frame's starting cell, with `$2074`/`$20A7`'s checks; the
  row below (`$2090`) must be solid. `$20A4`.
- `$20B0` (`Land`): the spring counts back to 3, then the hop ends; a frame
  of 0 or 1 is set to 2 in its block.
- `$20D7` (`Across`): two pixels (`$2107`, `$2146`); a wall
  (`$20DC`–`$20FA`, the third cell only part way down a row) turns it and
  flips the facing (`$213E`). Above row `$2D` walls are not looked at
  (`$20DE`, `$2115`), and there is no right limit.
- **A falling hopper animates only as it lands.** `$EBB8` sets up the
  descent at `$EB48` with an `rts` for `$EBB5`'s continuation, which every
  exit but the landing takes. The landing leaves through `$EB94`, and
  `$EBB8` writes only its opcode, a `jmp`, whose operand is `$EB0F`. A
  level 36 capture showed every landing stepping the frame.
- `$2006`, `$2055`: either drift flag not zero is a leap.

### EnemyJump

`$EBC4`–`$EC87` (`entity-system.s`): a walking enemy's jump.

- **Up**: `$EBC4` clears both drift flags and sets the rise counter to `$1F`;
  `$EBD9` runs the first frame at once. Sixteen wind-up frames (`$EBFB`):
  it stays and turns every fourth (low two bits clear). Then it rises by
  `$ACDD` for fifteen frames and falls by it.
- **Across**: `$EAD1` sets a drift flag and the counter to `$0F`, no wind-up;
  it rises a pixel and moves two across (`$EBE7`), then falls by `$ACED`.
- `$EBD9`: A holds the counter from before the `dec`.
- `$EC0C` (`Fall`): the fall counter counts up to `$10` (`$EC17`). `$EC3C`: a
  floor is looked for only crossing into a new row, not above row `$1F`,
  probing from the frame's starting cell: row clear (`$EC4A`), row below
  solid (`$EC5C`), as `$EB48`. `$EC70` snaps to the grid. `$EC7C`: both
  counters back to `$FF`.
- `$EC87` (`Across`): a drift flag not negative is off; `$EAD1` sets `$FF`, a
  wall sets `$01`, so the leap stops moving across and keeps falling. Two
  pixels (`$ECAB`, `$ECD8`); right limit `$F4` (`$ECB6`), none on the left.
  `$EC8B`, `$ECBC`: the cell beside the middle row, or the bottom row part
  way down. `$EC94`, `$ECC3`: only a wall with open space behind it stops
  the thing, so a thing inside a wall is not held.

### EnemyProbes

Shared map reads.

- `$EA20`–`$EA62`, `$1F66`–`$1FA1` (`PlatformAbove`): in the column above,
  within four rows, a solid cell with an open one above it.
- `$ECF7` (`Sidestep`): off a column boundary the thing moves without
  looking; on one, a wall beside it flips both walk bits of the heading.

### EnemyRunner

`$1E9F`–`$1F2B` (`player-animation.s`): state 9, class 7, an enemy that runs
four pixels a step and throws. Headed as the player's bubble shooting, but
it runs for enemy slots only and throws type `$32`, not a bubble.

- It never jumps. It runs the way `$85E8` says (bit 0 left, bit 1 right;
  `$ED12`, `$ED18`: four pixels, the cell beside the middle row) until a wall
  turns it, and falls off a platform's end. `$1EF6` saves the heading first;
  `$1F1D` reads both bits from the copy, so a turn by the first does not stop
  the second.
- `$1E9F`–`$1EF3` (`Throw`): the countdown, a draw below `$0C` (`$1EAC`),
  standing, no throw held, and above `$A8` (`$1EBC`). It takes the first
  free of sixteen object slots from the top (`$1EC0`), two along as for
  `EnemyShot`, and writes type `$32` to the type and enemy type (`$1ECC`), at
  X + `$0C`, Y − `$05` (`$1EDB`, `$1EE6`). `$1EEE`: `$64` frames in
  `AttackTimer`, and the same in `BubbleTimer`. Nothing here counts
  `BubbleTimer` down, and a throw needs it negative.
- `$1F03`, `$1F17`: no ground under it (three cells) starts a fall the same
  frame.
- `$EB34` (`Descend`): the descent with `$EB94` turned into a `BIT`, so a
  landing runs into `$EB97`, and `$EE4A` as the continuation. `$EB97`–`$EBAF`:
  landed, it heads at the player, keeping bit 2; the `cmp` is unsigned, so
  level is left.

### EnemyShot

`$EDCD` and `$ED3B` (`entity-system.s`). States 3, 7 and 8 go to `$E9FD`:
`$EDCD`, then the walker (`$EA08`) or `$ED3B`. So classes 1, 5 and 6 walk as
class 0 does, and shoot.

- `$EDCD` (`Aim`) books a slot; `BubbleTimer` counts seven frames (`$EE39`)
  and the enemy does not walk. `$E9FD`'s test: not negative is booked. It
  needs no jump or fall (`$EDDC`), one chance in four (`$EDE9`), the player
  on its row or, one in thirty-two more (`$EE1F`), above it (`$EE15`), and
  to face the player away from the edge (`$EDF1`–`$EE13`: `$F4`, `$34`). The
  subtraction there runs with `$E9EA`'s carry, so level in X takes either
  arm by that draw.
- `$EE25`–`$EE46` (`Book`): the first free of sixteen slots from the top;
  type `$42`, flags `$09`, reload `$96` (`$EE31`, `$EE3D`, `$EE41`).
- While booked, `BubbleTimer` (`$8818`) is the countdown and `RiseCounter`
  (`$87A0`) the slot. `$ED41`: nothing booked, the countdown restarts from
  one.
- `$ED3B` (`Fire`): `RiseCounter` back to `$FF`. `$ED60`, `$ED6A`: direction
  `$02` right or `$80` left, written whether or not it fires. `$ED73`: a
  wall in either cell beside (`$27`/`$28` left, `$2B`/`$2C` right) frees the
  slot. Otherwise the shot starts two columns out (`$ED64`, `$ED68`), from
  the corner (`$ED4C`, `$ED8A`, byte subtractions that wrap), with
  `SubX`/`SubY` zero.
- `$EDA0`–`$EDC3` (`Load`): class 6 fires `$2A`; class 5 `$2C` and then
  stands still, the slot in `AttackTimer` and `$2C` in `BubbleTimer`;
  others fire `$28` with `SubX` = direction & `$7F`.
- **The object indexes are two along.** The 6502 indexes `$CC`, `$A9FC` and
  the rest with 0–15, which are `$CA`, `$A9FA` two bytes along, so slots 0
  and 1 are never a shot.

### EnemySpawner

`$39D2` (`entity-spawn.s`), with `$1E2E` (`joystick-input.s`) before it: a
level's enemies into slots 2 to 7. `$39A8` walks the spawn data to the
level's list, which `LevelStore` already does.

- `$1E2E` (`Clear`): state, X and Y of the six slots (`ldx #$05`). The zero
  it returns in A is the count, the first slot and the first list byte.
  `$39D3`, `$39D6`: the players' hold timers from the same zero.
- `$39E2`–`$3A63` (`Place`), one three-byte record. `$39F0`: the delay is read
  with `and #$3F` and `asl`, dropping the flag bits. `$39FB`: **the state is
  the class plus two**, stored in `$B4`. `$3A03`/`$3A0B`: X and Y are the cell
  × 8 plus `$14` and `$15`. `$39FF`: the carry out of the column is not
  cleared, so a column of 30 or more would be a pixel lower (no level has
  one). `$3A0F`: the low three row bits one place up, and the move-left bit
  in bit 0. `$3A21`: facing left starts at its second block's first frame.
  `$3A3D`: one draw per enemy.
- **Classes 6 and 7 read past each table.** `$3A2A`–`$3A47` index `$AB61`,
  `$AB69`, `$AB71`, `$AB79` (eight bytes each) by state, and states 8 and 9
  read the next table's first two bytes, to `$AB81`. 133 of the game's 572
  spawns are class 6 or 7.
- Not here: `$3A6C` (`SolidMap`'s), and `$3A89`'s special-bubble setup.

### EnemyWalker

`$EA08` (`entity-system.s`), with `$EE62`, `$EAFA` and the side movers:
state 2, class 0 (level 1's enemy); states 3, 7 and 8 reach it through
`$E9FD`.

- **The facing is the frame:** below `FrameCount` faces right. Every test
  reads the frame `$1E6C` copied into `$25` (`$1E6F`), so a turn earlier in
  the frame does not change a later test. The target is `$1CF3`'s `$4B`.
- `$EA0E`: the heading is copied after `$EE62`, and `$EAFA` reads the copy a
  bit at a time (`$85E8`: bit 0 left, 1 right, 2 up), so one bit's turn does
  not stop the next being read.
- `$EA13`: not on the ground and not jumping is falling. `$EA20`: a climb
  when allowed and `EnemyProbes.PlatformAbove`. `$EA73`: part way down a row
  the ground is not looked at; above row `$1F` (`$EA6D`) it falls without
  looking. `$EA78`: the three cells under it. `$EA8C`: out of `$FF` into a
  fall the same frame.
- `$EA92`–`$EAF2` (`Leap`): on a row boundary with ground, not class 1
  (`$EA9C`), an open cell two rows down beside its feet and a solid one nine
  columns on (`$50 ± 9`). Left leaps from a column boundary; right from six
  pixels in (`$EAAA`), and `$EAB0`/`$EAC4`'s `adc #$08` runs with the carry
  set. Rise counter `$0F` (`$EAE1`, `$EAF4`).
- **The leap reads past the level.** Near a wall the far column is off the
  thirty-two and reads one of the eight entity bytes that end each row of
  `$8500` (`SolidMap`'s row tails). A level 50 capture: a class 5 enemy at
  column 25 did not leap, because the byte was slot 2's `$8840`, zero.
- `$ECDF` (left), `$ED1E` (right): a thing facing the other way turns and
  moves next frame. Two pixels (`$ECF5`, `$ED33`); the cell beside the middle
  row, or the bottom row part way down (`$ECED`, `$ED2B`).
- `$EB3F`: the descent, then `$EB0F` either way.

### EntityAnimation

`$EB0F` and `$EE4A` (`entity-system.s`): how an enemy's frame steps.
`$EB0F` ends nearly every enemy path, and `$1E87` falls back to it, so an
enemy held by its spawn delay walks on the spot.

- `$EB0F`: every second call (`$EB16`) the frame steps; a whole cycle along,
  the count is taken off, so a left-facing thing stays in its second block.
  `$EB0F`–`$EB1B`'s `cmp` is unsigned, so a counter past two resets.
- `$EE4A`: the same counter, flipping bit 0 of the frame.

### EntityTimers

`$13BE` (`player-sprites.s`, `enemy_timer_handler`), called at `$0A51` after
the AI loop: the clock every object slot runs on. **This is what ends a
bubble**: it is made with `$7D` in `$A9FA`, and `$13C0` takes one off every
pass. The AI state counter, `$A9B2`, is not touched.

- `$13C6`: a bubble (`$04`) out of clock becomes `$3A`, the pop (`$13CC`),
  keeping what it was.
- `$13DA` (`Release`): a bubble with an enemy (`$18`–`$23`: `$13D7`,
  `$142E`) out of clock lets the enemy back into its slot (`$13DC`, the
  inverse of `$1091`) in state `$5ABF`, and becomes `$38` (`$1404`).
  `$13E8` sets the row's bottom bit and `$13EE` clears the column's: not
  symmetrical, and kept. `$13FD`: the stashed `$AA30` comes back as the rise
  counter. `$5ABF` lives in unused sprite data; `bonus-round.s` writes it,
  so it is passed in.
- `$140B`: at `$24` up, only `$42` means anything, and frees the slot.
- `$1413` (`Flicker`): below `$11` on even counts (`$1415`). `$1428`: a
  bubble wobbles, `$04` and `$48` in turn (EOR `$4C`, `$1425`; captured at
  frames 195–215). `$142E`: a caught enemy toggles its `$D015` bit instead,
  from `$AB55` (`$04` for the first enemy to `$80` for the sixth).
- `$1449` (`Pressure`): fewer than two slots at `$34` or above (`$144F`,
  `$1457`) pops free bubbles. `$145C` is a `jsr` to the next instruction, so
  `$145F` runs twice: two bubbles a pass. `$145F` pops the highest-numbered
  free bubble (`$1466`).

## Players

### EntityTable

The arrays a player shares with every moving thing, from `$85xx`–`$88xx`.
`sprites2-tables.s` lays that region out as forty-byte rows: thirty-two
bytes of level tiles (`SolidMap`), then eight bytes that are these arrays.
So each array is forty after the last. Eight slots (`$1CBD`: `ldx #$07`),
players 0 and 1, enemies 2 to 7.

| Field | Address | Notes |
|---|---|---|
| `State` | `$B2` | Zero page. Zero is empty. `$105B` walks `$B4`, `$BC`, `$C4` (two along) for the enemies. |
| `X` | `$BA` | Pixels; wraps. |
| `Y` | `$C2` | Pixels; the vertical mover wraps `$F5` to `$15` and `$14` to `$F5`. |
| `Frame` | `$8520` | Sprite, and facing: 0 right, 4 left, bit 0 the walk cycle. |
| `AnimationTimer` | `$8610` | Frames between animation steps. |
| `BubbleTimer` | `$8818` | Negative: not in a bubble. `$05C5` sets `$FF`. |
| `RiseCounter` | `$87A0` | A jump's rise left; `$FF` at level start. |
| `FallCounter` | `$87C8` | Counts up to `$10`. |
| `LeftFlag`, `RightFlag` | `$8840`, `$8868` | Drift while off the ground, negative on. `$222B` latches, `$2483` reads. |
| `Colour` | `$8548` | The sprite's own colour; `$D025`/`$D026` are shared. `$1822` reads it. |
| `FlashTimer` | `$8728` | The flash after a hurt; `$13FA` sets `$FF` on release. |
| `Mode` | `$85C0` | `$1CDB`'s arm: negative is the ordinary update. Players `$FF`, enemies 0; `$10B9` sets `$FF`. |
| `HoldTimer` | `$8638` | `$1D11` skips `$1D21` while it is not zero, and counts it down. |
| `GroundState` | `$87F0` | Negative on the ground. `$1F17`, `$2575` start a fall by incrementing it. |
| `Heading` | `$85E8` | Bit 0 left, 1 right, 2 up; `$EF0D` EOR 3, `$EF8F` EOR `$0C`. |
| `SpriteBase` | `$8598` | Added to `Frame & $1F` for the sprite pointer. |
| `TurnTimer`, `TurnInterval` | `$8660`, `$8688` | `$EE91`'s countdown and reload; `$39D2` starts both at one draw. |
| `FrameCount`, `FrameMask` | `$8750`, `$8778` | A walk cycle's frames, and one less. |
| `LeapFlag`, `ClimbFlag` | `$86D8`, `$8700` | Negative: may leap or climb. `$EE62` sets them. |
| `ClimbNext` | `$86B0` | `$EE91` indexes `$86D8` with the slot or slot + `$28` (`$8700`); true is the latter. |
| `AttackTimer` | `$8890` | The drop's target row (left at `$0A`), then the shot and throw reload. |
| `EnemyCount` | `$4A` | Enemies still to deal with; `$39D2` counts them. |
| `SpriteEnable` | `$D015` | Game state, not `$B2`, decides whether a sprite shows: `$09CD` sets all, `$142E` flickers, `$3D91` restores. |

`IRowTails`: row r of `$8500` ends in the eight bytes at `$8520 + 40r`. Rows
holding arrays not kept here cannot read solid: `$8570` is a colour below
`$10`, `$86B0` a slot. Rows 23 and 24 (`$88B8`, `$88E0`) read zero in every
capture.

### Input

`$1CBD` (`joystick-input.s`) reads both joystick ports first each frame and
keeps the raw bytes, one per player. The movers shift the byte and branch on
the carry, so the CIA bit layout is kept: 0 while pushed. Bits 5–7 (keyboard
rows) read high. Player 1 is port 2 (`$DC00`), player 2 is port 1
(`$DC01`). A held read, since the 6502 reads afresh every frame.

The keys are this port's own; player 2's are on the other side of the
keyboard. The gamepad is player 1's only, since `IGamepad` reads one device.
An analog stick counts at half travel, SharpKind's own threshold; hat and
stick both drive the same bit.

### PlayerCell

`$E9B8` (`convert_entity_pos_to_screen`): a position's cell on the collision
map, and how far past it. The 6502 subtracts `$14` from X and `$15` from Y
(byte subtractions, so above the top wraps high), divides by eight, and
keeps a pointer in `$11`/`$12` for indexed probes: `$51` two rows down and
one across, `$28` the row below, `$00` the cell. Forty bytes a row.

`$AC03` starts the row table at `$FF88`, three rows below `$8500`, and `$11`
is decremented once: the two biases (3 rows, 1 column) are folded in. The
offset is added to the pointer, so a column past forty carries into the row:
`$27` is the row below, one left (`$ED3B`'s shot probe proved it).

### PlayerTable

The two players' own bytes, one array per field, indexed by player.

| Field | Address | Notes |
|---|---|---|
| `Lives` | `$045A`, `$045B` | Two separate bytes in `game-variables.s`. |
| `Reload` | `$A824`, `$A825` | Frames until the next blow; `$2385` sets it, `$0A28` counts down, `$22E8` checks. Called an invincibility timer; only the blow reads it. |
| `BlowFacing` | `$ACCB`, `$ACCC` | `$22E8` stores `$00`/`$02`; `$2301` doubles it. |
| `ExtendLetters` | `$54`, `$55` | One bit per letter from `$AB53`; `$3DF9` sets them. |
| `BlowReload`, `BlowState`, `BlowVariant`, `BlowType` | `$A77B`, `$A77D`, `$A77F`, `$A781` | What a blow makes: `$2364` copies the first to `Reload` and the rest to `$A9B2`, `$AA30`, `$AA42`. `$7F53` sets them; candies `$2DAB`, `$2DB2`, `$2DCD` change them. |
| `DriftRing`, `WalkRing`, `BlowRing` | `$61`, `$63`, `$65` | While not zero, `$2483`, `$22B4` and `$23D7` score ten through `$7C21`. `$2F5F`–`$2F65` set them, `$7F53` clears them. |

### PlayerDeath

`$1D32` (`joystick-input.s`), the end of `$1CBD`: an enemy touching a player
kills them. Headed "player-to-player collision"; it is a player against the
six enemies, after all eight slots have moved. Player 2 first.

- A player in state 1 (`$1D36`) with an enemy (slots 2–7, `$1D49`) in state
  1 to `$0A` (`$1D4F`) within `$0C` pixels both ways (`$1D5E`, `$1D6D`) goes
  to `$0E` (`$1D6F`). Nothing else is written.
- **The box is not centred.** `$1D3B`–`$1D48`: the `cmp #$01` leaves the
  carry set, so `adc #$01` adds two to X, and its carry goes into Y's
  `adc #$02`: X + 2 and Y + 2, or Y + 3 when X + 2 wraps.
- `$1D53`–`$1D5E`: `sbc`, and on a borrow `eor #$FF`, `adc #$01` with the
  carry clear.
- `$1D2A` skips it for `$1D84` while `$5AFF` is not zero (returns at once
  except on level 99). Only untranslated code sets `$5AFF`: the hurry-up at
  `$16CD`, the bonus stage at `$370C`, `$F06D`, `$7EC1`.

### PlayerDescent

`$EB48` (`entity-system.s`): a flat two-pixel fall (`$EB51`) until there is
something underneath. Shared: in VICE it runs for enemy slots; the player
reaches it only from `$2575` when a landing finds no floor (read, not
watched).

- `$EB56`, `$EB5A`: exactly `$F5` wraps to `$15` (equality, so a step over
  `$F5` does not). Above row `$1F` (`$EB5F`) no floor is looked for; only on
  a row boundary from `$2D` (`$EB62`).
- `$EB69`, `$EB7D`: the row it is in clear, the row below solid; one lower
  than the landing's pair, because nothing here calls `$E9B8`, so the probes
  read the frame's starting cell. `$EB91`: back to `$FF`, standing.
- **The self-modifying code is not reproduced.** `$EB34`, `$EB3F` and
  `$EBB8` write different continuations into `$EB94` and `$EBB5` (`$EB0F`,
  `$EE4A` past a `BIT`, or `rts`) and fall into this body. This class stops
  where the continuation begins; `EnemyWalker`, `EnemyHopper` and
  `EnemyRunner` each run their own.

### PlayerDrift

`$2483` (`entity-interaction.s`): sideways movement off the ground, from the
flags `$222B` latched at take-off (`$8840`, `$8868`). One pixel a frame, and
animation every second frame (`$24FA`) for frames below `$08` (`$2510`).
Both halves of the arc come here (`$23FC`, `$243A`); a landing goes to
`$2519`.

- `$2483`: the drift ring's ten points, then the drift. A fall enters at
  `$248D`, having called `$E9B8` at `$2450`, so both have a fresh cell.
- `$248D`: the left flag first and wins, then the right, then the animation.
- `$2490`–`$24C4` (left), `$24C5`–`$24F9` (right): an edge (`$24`/`$25`,
  `$F4`) clears the flag, so the jump carries straight on. Above row `$2D`
  (`$24A0`, `$24D8`) nothing is looked at, nor for a player straddling a
  column. Probes `$28`/`$50` and `$2B`/`$53` (`$2494`, `$24CC`).
- `$24A6`, `$24DE`: the player moves when the cell ahead is open, and also
  when it and the one past it are both solid. Left looks past (`iny`), right
  looks before (`dey`).
- `$2515` leaves for `$25F1` (`PlayerSteer`), reached only from here, so a
  player steers on the frames the drift animates.

### PlayerDying

`$28A5` (`spawn-handlers.s`, "spawn_animation"): state `$0E`, the death. It
runs once a pass, then moves the player to `$0F`, whose handler `$28E8` is a
bare `rts`; the dead player waits for `$045C`.

- `$28AD`: the first call finds `$86D8` not `$12`, sets it, and starts
  `$8610` at `$20` (`$28B6`). `$7F53` (`player_death_respawn`) clears it
  (`PlayerRespawn`); VICE shows every death starting from zero.
- `$8610` counts down. `$1F` to `$00`: the spin, frame 0 or `$0C`–`$0E` from
  bits 1 and 2 (`$28C4`–`$28CE`). A player caught mid-jump or mid-fall
  drops three pixels a call (`$28D1`–`$28D9`), wrapping `$F5` to `$15`
  (`$28DE`–`$28E4`); `$87A0`/`$87F0` are not updated, so a player caught in
  the air falls through floors. `$28C3`'s `bmi` to `$28E9`: `$FF` to `$C8`,
  lying still, frames `$0F`–`$12` (`$28EE`). At `$C8` (`$28F5`) the state
  goes to `$0F`.

### PlayerRespawn

`$045C` (`player-state.s`), once a pass at `$0A1C`: a dead (`$0F`) player
comes back. Player 2 first; the loop carries on after one.

- `$7F53` first: `$A77B`-`$A782` back to `$08`, `$88`, `$04`, `$04`, the
  rings and `$8728` cleared, `$86D8` zeroed so the next death starts from
  `$20`. Level `$63` has its own setup at `$7F77` (throws).
- `$04BB`: the player's `$AB53` bit off `$5B7F`, frame `$A737`, `$87A0`,
  `$87C8`, `$87F0` to `$FF`, then `dec $045A,x`. Negative is the game over
  (`$04F0`, throws). Otherwise Y `$DD`, X `$A735`, `$8688` `$4A`, state
  `$10`, or 0 while `$5A7F` is set (a bonus round; not modelled).
- `$04F0`, the game over (a negative count): `$0409,x` takes SUBFLG (the
  level reached, kept in `PlayerTable.Round` for the front end), `$7BE8`
  writes the text, X and Y go to zero and the state to 0. `$0514` then runs
  as for a respawn. `$7BE8`: colour 1, "GAME" at column 35 on the lives row
  (`$A739,x`: 7 or `$0E`) and "OVER" on the row under it. `$046C` skips a
  negative count, so the text stays until a join blanks it. The port's HUD
  draws it for any count below zero.
- `$0514`: with `$4A` two or more, `$16E4` runs with `$16EF` raised, so its
  `lda #$FF` stores zero: every enemy in state 1-`$0A` loses its anger.
- `$290D`, state `$10`: `$8688` counts down to state 1, the colour EORs 5
  every turn (74 turns, so it ends as it began), and `$2162` plays the turn
  as state 1.

### The HUD's other text

`$7B53` (`credits_display`), at each level start: "CREDITS" in colour 3 at
column 33, row 23, and `$AB` + 1 as one digit at column 36, row 24. A join
counts the digit down in place (`dec $53E4`), so the HUD draws it from
`PlayerTable.Credits`. `display_text_string` (`$E42A`): a byte below `$10`
is a colour, `$10` ends the string (the reference's comments say `$00`),
`$1F` is followed by column and row, and any other byte less `$20` is a
screen code (A is `$21`, digits are their value).

### PlayerJoin

`$052A` (`player-state.s`), the tail of `$045C` through the jump at `$049D`:
a player in state 0 whose fire bit is clear (`$DC00,x`) joins. Player 2
first, then player 1, in the same call.

- Lives `$04` (`$045A,x`), state `$0F`: the next `$045C` takes one life and
  respawns the player, so the join shows three.
- The score's three bytes go to zero (`$AB51,x` is the last byte).
  `$AC,x` (`RIDBS`) goes to zero and `$AE,x` (`RODBE`) to the score's last
  byte less one: `$F1AC`'s extra-life progress, which is not ported.
- `$AB` (`RIDBE`) is the credits less one: 7 in a two-player game, 8 in a
  one-player game (`$0977`-`$0982`). Each join does `dec`, and so do the
  credit digit on both screens (`$53E4`, `$57E4`). A join that leaves it
  negative writes `rts` over the jump at `$049D`, so no one joins again
  until a new game (`$0997`). The rest of that call still runs.
- The 6502 also blanks 7 cells of two rows on both screens, the player's
  lives row (`$0584`-`$05A5`), and `$046C` draws it again one pass on. The
  port's HUD comes from the lives, so it shows four for that one pass.
- The reference calls `$28FA` "`$28FB`". Its table entry for state `$18`
  is `$28FA` (`FA 28` at `$1E6A`): colour EOR 5, then play as state 1,
  the `$290D` tail. Its lines after `jsr $2916` never run, since that ends
  in a jump. Only `$34D3` sets state `$18` (the bonus-round entry, step 6).

### PlayerFall

`$257C` (`entity-interaction.s`): a fall with no jump behind it, after
walking off a ledge. Two pixels a frame (`$2580`), not the arc's `$ACCD`.

- Like `$EB48`: the same probes (`$2597`, `$25AB`: `$51`–`$53` clear,
  `$79`–`$7B` solid, from the starting cell) and the same `$F5`/`$15`
  equality wrap (`$2586`, `$258A`; the wrap is the whole frame). Unlike it:
  no floor above row `$26` (`$258C`; `$EB48` uses `$1F`), a landing squares
  X to an even column (`$25D1`, `$25D8`), and its own tail.
- `$258C`–`$25BE` (`Landed`): only on a row boundary from `$2D` (`$2591`).
- `$25BF` (`Land`): a `dec` back to `$FF`, and squared up by facing. The
  frame sets differ from `$2519`'s: `$04`–`$07` and `$0A` up round down;
  below `$04` and `$08`, `$09` round up.
- `$25E2` falls into `$25F1` (`PlayerSteer`) every second frame, so a
  falling player can steer. There is no frame toggle here; the counter
  increments and resets, and the frame comes from the steer.

### PlayerFrame

`$1CBD`, `$1E6C` and `$2162`: a player between one frame and the next.
`$1CBD` reads the ports and walks the eight slots from 7 down, calling
`$1E6C` for every non-zero state; `$1E6C` works out the cell and dispatches
through `$1E3A`. Only the two player slots are driven here.

- States translated: 1 (`$2162`), `$0E` (`PlayerDying`, `$28A5`, from
  `$1D6F`) and `$0F` (`$28E8`, a bare `rts`; `$28F8`). Any other state
  throws.
- `$1CDB`–`$1D21` (`Slot`): a zero state is an empty slot, skipped. `$85C0`
  is `$FF` for a player, so `$1CA0`'s drop is never taken, and `$1CF7`'s
  `SESSION` test is for slots 2 up. **`$1D03` can run `$1E6C` twice**: a
  slot whose `$8728` is not zero gets an extra call on passes with bit 1 of
  `$08` clear (`$1D0A`). For a player that is item effect 0's flash, and why
  a death's first pass can step twice (`$28A5` clears `$8728` only once
  running). `$1D11`: nothing translated sets a player's `$8638`.
- `$1E6F`'s copy of `$8520` into `$25` is not reproduced; readers read the
  frame itself. The ports are the bytes `$85E8`/`$85E9` hold.
- `$2162` (`Playing`), in order: a bubble timer not negative runs a frame of
  the blow (`$2301`) with the timer as it is now. `$21BD`: a rise counter not
  `$FF` means an arc is running; the jump owns the frame. `$21C5`: off the
  ground without an arc falls. `$21CD`: on a row boundary, no floor under
  (`$51`–`$53`) starts a fall the same frame (`$21E5`). `$21EB`: any stick
  bit pushed walks (`$21EE`: `$1F` is idle).
- `$21F3`–`$2209` (`Idle`): in a bubble the stick still reaches the movers.
  An idle player animates on a period of seven (`$21FB`), `$2159`'s toggle;
  `$2200`'s `cmp` is equality, so a counter past seven runs round.
- Not translated: `$2171`–`$21BA`, the scan that catches an enemy while the
  player pushes up (falls through to `$21BD` with nothing found), and
  `$21F1`'s `inc $B1`.

### PlayerJump

`$23EA` (`entity-interaction.s`): the jump's arc, fall and landing. Headed
"bubble ascent"; a store checkpoint on `$C2` with up held splits it into
`$23FC` rising, `$243A` falling, `$247E` landing.

- `$ACCD`, sixteen deltas, drives both halves: the rise reads it with a
  counter running down, the fall with one running up. So a jump slows at the
  top and speeds up coming down.
- `$23EA` is entered with the rise counter in A: rising while it has not run
  out. `$21BD` only calls it with the counter 0–15; `$05C5` parks it at
  `$FF`. `$222B`, which sets it, is not translated here.
- `$23EA`–`$2400` (`Rise`): `dec` then `tay`, so the delta is the one before
  the decrement. The playfield wraps at `$05` going up (`$F0` step).
  `$23FC` leaves for `$2483` (`PlayerDrift`), which calls `$E9B8` fresh.
- `$2401`–`$243B` (`Fall`): the first frame tidies the sprite (`$2404`–
  `$2422`: frames 2, 3, 6, 7 are masked down; four interleaved compares) and
  zeroes the counter. Then sixteen frames at most (`$2423`), which ends the
  arc like a landing, with the frame's starting cell. It wraps at `$F5`.
- `$243C`–`$2482` (`Land`): not above row `$1F`; only on crossing into a new
  row (the fine Y is the frame's starting one), snapped to eight pixels from
  `$2D` (`$2474`). `$2450` calls `$E9B8`, so the probes read the cell just
  moved into: row `$29`–`$2B` clear (`$2453`), `$51`–`$53` solid (`$2460`).
- `$2480` leaves for `$2519` (`PlayerLanding`) with `$2450`'s cell; a landing
  does not drift. `$2453` and `$2470` leave for `$248D`, the drift.

### PlayerLanding

`$2519` (`entity-interaction.s`): the end of a jump, from `$2480` (landing)
or `$2423` (the fall out of table). Headed "bubble reached top".

- `$2519`: both arc counters back to `$FF`.
- `$2524`–`$2540` (`Square`): X to an even column (`$2530`, `$2538`), by
  facing: frames `$04`–`$09` round down, the rest up. `$2530`'s fall-through
  into the round-up when it stored zero changes nothing and is not
  reproduced. With `$247E`'s Y snap, a landing tidies both axes.
- `$2541`: the probes use the caller's cell, a pixel before the squaring:
  row `$29`–`$2B` clear, `$51`–`$53` solid (`$2555`).
- `$2569`–`$2571`: on `SUBFLG` `$5B` (level 92; the reference's comment says
  91), a player at Y `$DD` stays put.
- `$2575`: nothing underneath starts a fall, and `$2578` leaves for `$EB3F`
  with the same cell.

### PlayerMovement

`$220C`, `$226B` and `$22C3` (`entity-interaction.s`): the stick on the
ground. The reference calls `$226B`/`$22C3` "move left/right in bubble"; a
store watchpoint on `$BA` with right held traps at `$22C2`, after `$22C0`'s
`sta $BA,x`, the tail `L22BB` both fall into. They are the walk.

- `$220C` shifts the port a bit at a time: up, down, left, right, fire; 0 is
  pushed. Up is a `jmp` to `$222B`, so a player pushing up does not walk.
  Down is dropped (`$2213`). `$2223`: fire.
- Each mover writes its delta to `$04` (`$FE`, `$02`), picks its probe
  (`$28`/`$50` left, `$2B`/`$53` right, the second when part way down a row,
  by `$24`), and asks whether the frame already faces that way (`$2277`: a run
  of `cmp`/`beq`; `$22CF`: `cmp`/`bcc` then two `beq`; not mirrors). Then
  the shared tail.
- Not facing: turn (frame 4 left, 0 right), only while the bubble timer is
  negative. Some facing frames reach `$2294` first: the walk cycle steps
  every fourth frame by flipping bit 0 (`$2159`).
- `$22A6`–`$22C2` (`Advance`): above row `$2D`, or off a column boundary, no
  wall check. `$22B4`: the walk ring's ten points on every step.
- `$222B` (`Launch`): rise counter `$0F`, and the stick latched into the
  drift flags (`$FF`), left only from a left-facing frame and right only
  from a right-facing one. `$225C`: the jumping sprite unless the frame is
  already high. `$2268`'s `inc $B1` is not translated.

### PlayerSteer

`$25F1` (`entity-interaction.s`): the stick read again in the air. Reached
from the drift's tail (`$2515`), so only every second frame while the
sprite is below `$08`; holding right through a jump covers about a pixel and
a third a frame. Also reached from `$25E2` (`PlayerFall`).

- `$25F1` calls `$E9B8`, so the cell is fresh. `$25F5`: fire is a `jsr`, so a
  player blows and keeps steering. Left is asked first and wins; both its
  paths end in `rts`.
- `$25FE` and `$263D` are the walk's movers rewritten: one pixel, not two.
  `$2604`: left moves on `$04`–`$07` and `$0A` up, else turns. `$2641`: right
  moves below `$04`, or on `$08`, `$09`. `$2613`, `$2650`: a turn, not while
  in a bubble.
- `$2620`, `$265F`: the edges `$24` and `$F4` are equality tests, so only an
  exact arrival stops.
- `$261E`–`$263C`, `$265D`–`$267A`: a fine Y of zero jumps past the height
  test (`$262C`, `$266B`: `$2D`), so on a row boundary the wall check always
  runs.

## Items

### FoodDrop

An enemy after its bubble pops: it flies, falls, lands as food, and is taken
or goes. It stays in its own slot of the eight, and each step is a `$1E3A`
state, so `EnemyFrame` dispatches to it.

- `$1A6F` (`Kill`, enemy 0–5, slot two along): state `$0B`, a random flight.
  An enemy in `$0A` keeps its class in `RiseCounter`; `$0D` keeps none
  (`$1A71`, `$1A7A`). `$1AA4`, `$1AAA`, `$1AAF`: bubble timer 7, food 9,
  colour `$0E`. `AttackTimer` (`$8890`) is which food, and the pop that calls
  `Kill` then stores its chain count there. `LeftFlag` (`$8840`) keeps the
  class, which offsets the flight's frames (`$AB89`).
- State `$0B`, `$267B` (`Fly`): `RiseCounter` is the sideways speed and
  `GroundState` the way. `FallCounter` is positive going up, with the speed
  in `RightFlag` counting down to the turn; going down it is the fall's
  length with bit 7 set, and `RightFlag` counts up to it. It turns or ends on
  one frame in four (`$2681`). `$26AA`: `bcc` and `beq`, so only a count past
  the length ends it. `$26BF` (`Travel`): to a wall (`$F4`, `$24`: `$26CC`–
  `$26E5`) and back, then up or down, wrapping `$15`/`$F5` (`$26F9`–
  `$270F`). `$2713`: `Heading` is the frame counter; a quarter picks a frame.
- State `$0C`, `$273D` (`Fall`): two pixels (`$2740`); on rows lining up with a
  platform (`$274C`, `$2750`: from `$1F`, grid `$2D`) it looks two rows down
  (`$51`, inside a platform: go on through) and three (`$79`, under: landed)
  (`$2756`–`$277C`), from `$1E6C`'s cell.
- State `$11`, `$2921` (`Land`): becomes food. Only one slot a frame may,
  because it copies the food's picture into the slot's sprite; `$2922`, its
  own `lda #` operand, is the gate, and `$1CC7`'s `stx $2922` reopens it each
  frame. Life `$C8` (`$2931`, `$293A`); sprite base `$F2` (`$2946`:
  `(sprite_data_7C40 + $40 − $4000) / 64`); food by `AttackTimer` from
  `$A790` (entry 0 is the byte before the table, `sprite_data_7C40 + $200`'s
  high byte); colour from `$A8E4`, low three bits.
- State `$12`, `$29BB` (`Wait`): player 2 first, the first within `$10`
  (`$29DB`, `$29EB`) takes it; a dying player still does, a dead one (`$0F`)
  not (`$29C7`, `$29CB`). The score is `$A79A` by `AttackTimer`; the tenth
  byte is `$A7A3`, run into. `$29FF`: `$50` goes in the lowest byte, the rest
  a byte above. `$2A0B` clears the slot.
- Not here: the pop (`$3D2D`, `$3D77`), and `$29FF`'s store to `$B0`.

### ItemEffects

`$2D65` and `$2D88` (`special-item-effects.s`): the special item's effect.
`$2D40` looks the handler up by type (0–34) and calls it with the player in
X; `$2D43`'s type is `$53` with bit 7 clear.

- 3, `$2DAB`: reload 3, not 8. 4, `$2DB2`: `$8C` in `$A9B2` (further) and
  `$FF` in `$AA30` (faster). 5, `$2DCD`: `$FF` as `$AA42`. 10, `$2DC7`: all
  three, falling into `$2DCD`. 9, `$2DBE`: all three and every ring.
- 12, 13, 14 (`$2F5F`, `$2F62`, `$2F65`): the drift, walk and blow rings.
- 0, `final_routine` (`$7FF3`): `$FE` in the player's `$8728`.
- The rest reach untranslated code (level skips, `$2F74`'s freeze, screen
  effects, bonus handlers) and throw with the address. `$2DD2`'s store to
  `$B0` is not here.

### LevelItems

A level's two items: 0, a bonus food, and 1, the special item, as zero-page
pairs.

| Field | Address |
|---|---|
| type (bit 7 hidden, `$FF` gone) | `$52`, `$53` |
| X, Y | `$59`/`$5A`, `$5B`/`$5C` |
| cell (`$1844`'s top-left) | `$4E`/`$50`, `$4F`/`$51`, kept apart from X/Y, which `$2C8C` can wrap |
| colour RAM byte | `$5F`, `$60` |
| seconds | `$5D`, `$5E` |
| food quality | `$58`, set by `$1719` at level end (the level timer) |
| players who have not died | `$5B7F`, set by `$0989` |
| next milestone level | `$5B3F`, `$12` at game start |

- `$2B31` (`Setup`, SUBFLG counting from zero): both hidden, with timers.
  `$2B41`, `$2B78`: cells × 8, `$2C8C` adds `$14` and `$2D`, the Y add taking
  the X add's carry. `$2BB0`: the food is `$58` + up to three; the `adc` takes
  `$E9EA`'s carry, untouched by `and`; `$2E` is the best (`$2BB7`, `$2BBB`).
  `$2C5B`: three to ten seconds before the food, one to sixteen before the
  special item. It copies each item's art (`$A892`, `$A8C1`) from
  `sprites_rom` and colour (`$A8E4`, `$A913`).
- `$2BDB`–`$2C32` (`Special`): every ten levels (`$2BE7`–`$2BF8`) a player
  who has not died gets item `$20`; at `$3A` the milestone jumps to `$4E`
  with item `$21`. Otherwise `$2BFC`: the level's own item, from the RNG
  seeded with the level and put back after, and one level in sixteen `$1E`
  or the next (`$2C1E`, `$2C20`), with `$2C24`.
- `$06C6`: once a second from the IRQ, each timer down.
- `$1578`–`$15C8`: a timer out flips bit 7: the item appears, reloaded with
  eleven seconds (`$1580`), and next time it goes. Going ends the routine
  for the pass (`$158F`'s `rts`).
- `$2CB7` (`Collect`): player 2 first, the special item before the food,
  only while shown (the compare takes bit 7). A player in state `$18`, or
  live below `$0E`, or `$10` (`$2CC9`–`$2CD1`), within `$10` (`$2CFB`,
  `$2CEB`) takes it (`$2CFF`), scores `$A936`/`$A965` (a byte higher from
  types `$0F` and `$18`: `$2D18`, `$2D25`), and the special effect runs. A
  taken item stays until `$1578`, so both can take it. `$2CB9`, `$2CBF`: a
  visible `$18` flashes, EOR 5.
- Not here: the bonus level (`$63`), and `$2D06`'s store to `$B0`.

### Rings

While a ring's byte is not zero, `$7C21` adds 1 (`$7C24`) to the player's
lowest score byte: ten points. Drift (`$2483`, `$61`: a fall joins at
`$248D`, past the test), walk (`$22B4`, `$63`), blow (`$23D7`, `$65`).

### Scores

`$0400`–`$0405` and `$7C26`: two scores of three BCD bytes, most significant
first. `$AB51`/`$AB52` give each player's last byte. `$7C26` adds A at byte
Y in decimal mode and carries towards `$0400`; a carry out of `$0400` is
lost. The 6510's decimal add corrects each nibble by six once past nine,
so `$00 + $0A` is `$10` (`$A79A`'s last entry).

## Levels

### Level, LevelPoint, EnemySpawn

One of the hundred levels, as `rebb64`'s `levels.txt` describes it.
These classes, and `LevelZones` and `ZoneRect`, are public with setters
because they are JSON serialisable.
`Colours` is spelled `colors` in the file, so its name is pinned.
`Bitmap` is 23 rows of 32 characters, `#` solid and `.` open, as
`levels.txt` draws them. A `LevelPoint` is a tile in the 32×23 playfield.
An `EnemySpawn`'s flags byte (`$0`–`$1F` in `levels.txt`) is exported
already split; `Byte1Low` is the low three bits of the record's first byte,
bit 0 meaning move right.

### LevelStore, ZoneStore, LevelZones, ZoneRect

The hundred levels (`levels.txt`) and their zone rectangles
(`zone-data.txt`), read once. Both are asked for by level number, 1 to 100;
`LevelZones.Number` runs 0 to 99, as `zone-data.txt` and SUBFLG count. The
count is checked at load. Zones are a separate file, brought together with
the level only in `SolidMap.Build`.

A `ZoneRect` is a block of the map that carries a current; `Type` is the
two-bit direction (0 up, 1 right, 2 down, 3 left). The export resolves the
reference's mirroring into a second rectangle.

### IRowTails

The eight bytes that end each forty-byte row of `$8500`, past the level's
thirty-two columns: entity arrays (`$8520` on row 0, `$8548` on row 1 and
on). A probe far enough past an edge reads one, bit 7 as solid.

### Playfield

The level half of `setup_level_screen`: `$E000` builds what the renderer
leaves in screen memory.

- `$E014`: tables are indexed by a level counted from zero; the tile sheet
  has one character per level. `$E0CE` splits the colour byte into the two
  background nibbles; the byte crosses whole.
- `$E393` (`clear_color_ram`) fills screen memory with spaces.
- `$E0B1`: each set bit puts the level's tile, and three characters: one to
  its right and two below. `$E0BD` picks them by what the cell holds, so a
  row of tiles is edged once, at its end:

| Character | Address | Where |
|---|---|---|
| `$0A` | `$E0EB` | below a tile, over nothing |
| `$0B` | `$E0F5` | below and right of a tile |
| `$0C` | `$E0DD` | right of a tile, over nothing |
| `$0D` | `$E0EF` | below a tile, over another's shadow |
| `$0E` | `$E0D7` | right of a tile, beside another tile |
| `$0F` | `$E0DA` | right of a tile, over a shadow |

- The renderer writes one column right and one row down, so a last-column
  tile reaches screen column 32 (`draw_border` clears it) and a last-row
  tile the row off the bottom. Both are cropped.
- `$E16F`: the top row's plain right edges become the beside-a-tile edge,
  since nothing above it casts a shadow.

### Sidebars

The sidebar half of `setup_level_screen`: which four characters draw the
border. `$E023`: `levels.txt` packs the index and symmetry flag into a byte
the export splits (`convert-levels.py` rejects bit 7), so `and #$7F` has
nothing to mask. `$E029` compares the index against 100, not the 59 designs:
from 100 up (every such level carries `$7F`) the level repeats its header
tile. `$E014` counts levels from zero.

### SolidMap

`$8500` (`sprites2-tables.s`): which cells are solid, and each cell's
current. The reference keeps forty bytes a row (32 level, 8 entity
tail), bit 7 solid, low two bits a direction (0 up, 1 right, 2 down, 3 left)
that `$0E00`/`$0E23` read. This keeps one byte per cell, from the bit grid
`init_level_renderer` builds; `PlayerCell` turns a position into row and
column.

- 25 rows: the level's 23 between a ceiling (row 0) and floor (row 24).
- `$3A6C` (`entity-spawn.s`): `ldx #$27` copies all forty bytes of row 0 over
  the three rows above (`$8488`, `$84B0`, `$84D8`, which start as `$80`) and
  row 24 over the three below (`$88E8`, `$8910`, `$8938`), once per level. So
  a wrap opening goes on through them. Further off the map reads solid and
  direction 0. Columns 32–39 are the row tail (`IRowTails`); without tails
  they read solid. The copies take the tail as it is now, not as it was at
  `$3A6C`; no probe known reaches them.
- `Number` is 1 to 100, for `$2569`, which branches on the level; the
  reference's byte is one less.
- `Build` is `$E299` plus `$E18B` (`decompress_level_data`), once per level.
  `$E1D1` (`FillRowDefaults`): every row first gets one direction across its
  width (`AND #$03`): rows 0 and 24 from `$8B03`/`$8B63`, which hold the
  `bubbleCurrent` nibble two bits at a time; other rows from the last of the
  four bitmap bytes (columns 30–31) reread as a direction. Confirmed against
  a VICE capture of the transient buffer.
- `$E2E9`: `wrap_openings`, four bits (top left, top right, bottom left,
  bottom right); a set bit opens that half-row's gap. `$E34E` builds each
  half from `$E36E`/`$E371`'s byte pairs (0 solid, 1 left gap, 2 right gap).
  The direction is untouched.
- `$E308`: the bitmap copied a row at a time, with the outer two bits of each
  row's first and last byte set: the two outer columns each side are wall.
- `$E1F3`, `$E239`: each zone rectangle overwrites its cells' direction,
  never the solid bit, clipped to the map. An empty list is a level with no
  zones.

## Game

### GameLoop

`game-loop.s`: a level's start at `$09DC`, one main-loop pass at `$0A07`, and
the IRQ's share. Every translated routine is called from here in the
reference's order.

- **A pass is two 50 Hz frames.** The main loop waits for `$08` to move
  twice, and the IRQ calls `$1CBD` on every second frame, so 25 passes a
  second (`$09FD`), `$1CBD` once each, at the end of the pass. `$0A03` zeroes
  `$08`, so the pass's `$1CBD` frame is odd.
- Level start (`$09DC`–`$0A05`): `$09CD` every sprite on; `$09DF`
  `decompress_level_data`; `$392A` the level display and spawn; `$09E5`
  `$05C5`; `$09F1` `$2B31`; `$09F7` `$17BE`; `$09FD`, `$0A03`.
- `$04BB`, `$05C5`: where a life starts, its facing and colour. `$0620`,
  `$062B`: every object slot free. `$05F5`, all eight slots: the idle counters,
  and a walker's leap and climb off; `$8728`/`$8729`, the players' flash, are
  kept on the stack. `$0656`: the rings last a level. `$451E` copies `$47BD`
  into `$8598` once at boot: `$60` for both players.
- A pass: `$0A07`–`$0A57`, `$06C6` once a second, then `$1CBD`: slots 7–2,
  then 1 and 0, then `$1D32`.
- `$0654`: a released enemy's state outside bonus rounds. `$098A`: a
  one-player game (only player 1 has not died). `$0942`, `$0944`: the first
  level's food quality starts in `$0A`–`$28` (`$093F`–`$0946`). SUBFLG `$63`
  is the bonus level, whose items throw, so level 100 starts without them.
- `$052A` (`PlayerJoin`) runs straight after `$045C`, with the ports. `$0A64` (`IsOver`) is judged after the pass, and holds 75 passes.
- Gaps: `$0BED` (corner bubbles), `$32C1`
  (EXTEND), the level timer and hurry-up, and level completion.

### BbRandom

`$E9EA` (`sprite-composer.s`, `prng_update`). Two bytes of state, `$26`
(named `RESHO`, after the BASIC area it borrows) and `$27`, shifted as one
sixteen-bit value and folded, then EOR'd with `CIA1_TBLO`, a free-running
timer. CIA emulation is out of scope, so that byte is injected
(`Random.Shared`); no capture of a random choice can be matched byte for byte.

- The `adc`'s carry is the one `rol RESHO` left: `$26`'s top bit on the way
  in (the opening `asl` shifted a copy in A). The `eor` and `sta` after leave
  it, and `$EDF1` subtracts with it: `Carry`.
- Nothing seeds it for play. `$2BFC` saves it, seeds both bytes with the
  level number, and puts them back.

### BubbleBobbleMain

The game as the host sees it. One tick is one pass. It opens on level 1 with
one player (no front end yet); `N` steps to the next level (wrapping) until
there is a progression, and F12 dumps the frame. `$0956`: three lives.
`$0969` clears the high score at game start.

The playfield and sidebar are settled once per level (`setup_level_screen`);
sprites (`$1805`), objects (`$E90E`), items (`$1844`, colour from `L_186B`)
and the HUD (`$E3A7`, `$046C`, scores from `$0400`) are built each frame.
Layers, in the reference's drawing order:

| Layer | What |
|---|---|
| 0 | The level, trimmed to the columns the decoration does not cover; the items (`$1844`), which are characters of the level |
| 1 | `$E90E`'s bubbles, pops and shots |
| 2 | `draw_border`, the decoration down both edges |
| 3 | `$1805`'s sprites, in front of the characters as a C64 sprite is |
| 4 | The scores and lives |

### Hosting

- `BubbleBobbleServiceCollectionExtensions`: `BbConfig` is internal, so the
  config file is registered from inside the assembly. The rendition is
  loaded before the container, because the window is made at its size, and
  its asset locator replaces the engine's (the game keeps no assets of its
  own). `levels.json` and `zones.json` sit in the rendition's
  `Assets/Levels`, not in the manifest, as separate files.
- `BbConfig`: the engine defaults to 16-bit, but this game has 8-bit art only,
  so the default and the fallback are 8-bit. `BbConfigSettings` is empty but
  gives the `game` element a type.
- `RenditionLoader`: a rendition is a folder, since it brings its own assets;
  loose DLLs are still read, and get the `Renditions` folder for assets. A
  missing rendition is fatal only if it was the one asked for.
- `BbViewSurface`: the window size is the rendition's.
- `LogMessages`: source-generated logging.

## Views

### BbViewLayout

The C64 screen is 40 × 25 cells of 8 × 8. The level is the leftmost 32
columns, because `setup_level_screen` starts each row at the screen
pointer's offset 0 and stops after 32; `draw_border` clears column 32. The
eight columns right of it are the HUD, written straight into screen memory.
`draw_border` covers the level's outer two columns each side, so the
composition trims the level to what survives. The metrics are settable, for
renditions of other tiers.

### HudModel

- A score is three bytes of packed BCD (`$0400`, `$0403`, high `$0406`),
  six digits, as `add_score` (`$7C24`) keeps it; the model takes the bytes,
  not a number.
- `display_score_digits` (`$E3D9`) hands each digit to `$3FB0`, which writes a
  space for a leading zero, but always the last digit, so no score reads as
  one zero.
- Lives are `$045A`, `$045B`, signed. `$046C` returns without drawing when
  negative (a player who is out gets an empty row), and fills seven cells
  from the right (counting six to zero), stopping when it runs out of cells.

### ItemsModel

`$1844` draws each shown item (`$52`/`$53`, bit 7 clear) as a 2 × 2 block at
its level cell, the art `$2B31` copied in, with one colour RAM byte
(`$5F`/`$60`) on all four cells. A hidden item draws nothing: `$1578` has put
the cell back.

### ObjectsModel

What `$E90E` drew in each object slot this pass, recorded as it draws,
because the AI loop moves the slots before a view runs. `Column` is `$DC`,
`Row` is `$EE`, `SubY` is OLDTXT.

### PlayfieldModel

The level as screen codes, 32 × 25, as `setup_level_screen` leaves them. The
C64's own codes, because the renderer's choices are made in them.
`setup_level_screen` copies the level's tile (one of a hundred, counted from
zero) into one charset slot. The colour byte is `levels.txt`'s: two nibbles,
each one of the sixteen colours the two-bit entries point at.

### SidebarModel

One of 59 designs of four characters, or none. `setup_level_screen` copies
32 bytes (four characters) into the four border slots; `draw_border` lays
them as a 2 × 2 block down both edges. A level with no design repeats its
header tile in all four slots. The decoration is inside the level, so it is
painted from the level's colour byte.

### SpriteModel

What `update_sprite_positions` (`$1805`) leaves in the VIC-II's registers
for the eight slots: `$BA` to X (`$1809`), `$C2` to Y (`$180E`), `$8548` to
the colour register, and `($8520 & $1F) + $8598` to the pointer (`$182F`, a
byte add that wraps). Nothing else lies between a slot's bytes and the
screen, so the position bytes are carried as they are.

- A pointer names 64 bytes of the VIC bank at `$4000`. The game's sprites
  start at `$5800` (pointer `$60`), 97 of them. `$8598` is `$60` for the
  players and `$73`–`$BB` for enemy classes.
- `$182F` also stores the masked frame back to `$8520`; a model does not
  write state, so that is not reproduced.
- `$1817`: a zero state skips the colour work, but position and pointer are
  written anyway, so the state does not say whether a slot is seen. `$D015`
  does: all on during play, except `$142E`'s flicker. An empty slot is
  parked at Y `$15`, in the top border.
- A pointer outside `$5800`'s sprites is not drawn: food's (base `$F2`,
  `$7C80` on), which `$2921` builds at run time.
- `$1822`: a slot below state `$11` (`$1826`) with its flash timer running
  takes `$8570`'s colour (copied from `$47B5` by `$451E`): 5 and 3 for the
  players, 10 (light red) for an enemy.
- The colour register holds four bits.

## 8-bit rendition

### EightBitRendition

The C64: 40 × 25 cells of 8 × 8, sixteen colours. Window scales up to four
(1280 × 800).

### HudView8Bit

All characters from the font: digits are screen codes `$00`–`$09` (so the
charset runs digits first and `BitmapFont` reads them as ASCII), the life
marker is `$1D`, in the sheet's last cell (`LIFE_ICON` in the export).
`update_sprite_animations` (`$E3A7`) writes the scores at `$50E9`, `$5201`,
`$5341` (rows 5, 12, 20, column 33, on the screen at `$5000`); `$046C`'s
lives at `$5139`, `$5251` (rows 7, 14). The string at `$AB93` places the
labels a row above each score and writes their colour RAM, which the digits
then take. None has bit 3 set, so the HUD is hires.

### ItemView8Bit

`$1844`'s 2 × 2 block from `item-chars.tga`. Entry 11 is the item's colour
RAM byte, so each item has its own painted sheet; entry 00 is opaque,
because the characters replace the cell. `$1934` draws the special item
first, then the food.

### MulticolourSheet

A multicolour character is four two-bit entries a row: 00 background, 01
and 10 the two background registers, 11 the cell's colour RAM nibble. The
sheets are exported with the entry as colour 0–3, and repainted once a level
for the level's colour byte. `$E0CE` (`level-renderer.s`): the high nibble
goes to `$1D` and the low to `$1F`, which the split-screen IRQ at `$072E`
writes to the two background registers. `$09B8` (`game-loop.s`) fills colour
RAM with `$0D`: bit 3 is multicolour mode, so entry 11 is colour 5 on the
playfield. Items write their own. Entry 00 is transparent and never
repainted.

### MulticolourSprites

A multicolour sprite resolves entries differently from a character: 00
transparent, 01 and 11 from `$D025`/`$D026` (shared by every sprite), 10 the
sprite's own colour. `$44E5` (`$44E9`, `$44EC`) sets `$D026` to 1 and `$D025`
to 2, and nothing writes them again, so a copy is held per sprite colour.

### ObjectView8Bit

A cell's left edge is `$DC × 8`; the art is already shifted by `$A9C4`
inside it. The bottom is in character row `$EE − 2`, at line
`(OLDTXT + 7) & 7`, and the sixteen-row cell builds upwards. Each sheet pixel
is two on screen.

### PlayfieldView8Bit

Two sheets, as the C64 has: the tile sheet (a hundred tiles, ten a row, one
per level) and the six edge and shadow characters from the charset. Both are
multicolour, drawn at double width, and repainted first. It draws the outer
two columns each side even though the sidebar covers them, so the two views
need not know of each other. An empty cell is the background register,
drawn by nobody.

### SidebarView8Bit

`draw_border`'s 2 × 2 block down the level's outer two columns each side,
at double width; the sheets stay byte for byte rebb64's.
`init_level_renderer` forces those columns solid, so the decoration is always
over wall. `$E10C` walks twelve two-row blocks and then the top half once
more on the last row, covering 25 rows. `$E051` copies a design's four
characters; `$E060` copies the header tile into all four slots when there is
none.

### SpriteView8Bit

A C64 sprite is 24 × 21; multicolour, twelve entries a row, drawn at double
width. The sheet is the 97 sprites of `$5800` in one row, in memory order.
`$1805` writes `$BA`/`$C2` straight into the registers, so the offset is the
VIC-II's: `$18` and `$32` put a sprite at the screen's top-left. `PlayerCell`
works from `$14` and `$15`, four pixels in and five down: the game probes a
point inside the character. `$1805` walks slots 7 to 0; the VIC gives a
lower-numbered sprite priority, which drawing 7 to 0 reproduces.
