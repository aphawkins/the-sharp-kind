# Bubble Bobble: the Baron

[BubbleBobbleSharp] Step **4f** of section 5 of [bb-port-plan.md](../bb-port-plan.md). The plan holds the
detail — addresses, what is already done, and how the step is proved. Read it
there and update it as the step lands.

The second expiry (`$2D` above zero at `$2A` zero), `$1621`-`$1693`.

- `$1AE8` (objects 0-1 saved and made `$3A`, a pop), `$1B02` (the loop over
  `$E90E`, `$1844` and `$1805` until both are free again), `$1B19`-`$1B3F` (the
  background restored from its backup).
- `$1633`-`$1678`: for each object slot 0-1, a random column (`$A777`) and row
  (`$A779`); the type `$2E`/`$30` for each slot whose player is in the game (the
  Baron; the port's `Baron` at `$1473` runs it).
- `$1681`-`$1693`: `$2D` to 0, `$2A` from `$2C`.

Blocked by 4e. Verify: a VICE capture of the spawn: the Baron's place, type and
first moves.
