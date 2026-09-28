# Bubble Bobble: level clear

[BubbleBobbleSharp] Step **4c** of section 5 of [bb-port-plan.md](../bb-port-plan.md). The plan holds the
detail — addresses, what is already done, and how the step is proved. Read it
there and update it as the step lands.

The last enemy gone, `$16F7`-`$1772`, the tail of `$1578`.

- `$16F7`: with `$4A` (enemies left) zero: `$58` (the food base seed,
  `LevelItems.FoodBase`, see `$1719`) is set from `$E9EA` (and 3) or `$2A + 8`, by
  `$58`'s sign; `$5E` is made `$FF`, or `$53` if that is already negative; `$58FF`
  is cleared.
- `$171B`-`$1750`: every object slot that is not `$44`/`$46`/`$4A` is saved to
  `$AA42` and becomes type `$3A` (a pop), or `$4C` for the `$38`-`$41` range while
  `$68` is set.
- `$1751`-`$1772`: `$1E2E` if `$69`, then `$2B` to `$32`, `$2D` to `$FF`, `$2A` to 9.
  `$3517` if `$68` is set (the bonus round: step 7).

Verify: a VICE capture with `$4A` written to zero (an
intervention; the enemies gone by hand do not end a level, `$4A` does).
