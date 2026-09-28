# Bubble Bobble: the hurry-up

[BubbleBobbleSharp] Step **4e** of section 5 of [bb-port-plan.md](../bb-port-plan.md). The plan holds the
detail — addresses, what is already done, and how the step is proved. Read it
there and update it as the step lands.

The timer running out with enemies left, `$15E1`-`$15FA`, `$3AB8`.

- The first expiry: `$2D` set, `$3AB8` (the HURRY UP scroll: drawing) and `$2A` to
  `$0A`. `$1694`-`$16D8` copies the Baron's banner cells to both screens and
  counts `$A9B1`.
- `$16D9`-`$16F6`: while `$2D` is set, with one enemy left, `$16E4` (anger) runs.
  `PlayerRespawn.Calm` is the same routine with `$16EF` raised.
- The level 99 special (`$16C5`-`$16D8`, `$5AFF`) is step 7's; `PlayerDeath` needs `$5AFF`.

Blocked by 4a. Verify: a VICE capture of the expiry, and the angry flash on
each enemy.
