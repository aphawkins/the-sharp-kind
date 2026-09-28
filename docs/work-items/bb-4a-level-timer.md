# Bubble Bobble: level timer

[BubbleBobbleSharp] Step **4a** of section 5 of [bb-port-plan.md](../bb-port-plan.md). The plan holds the
detail — addresses, what is already done, and how the step is proved. Read it
there and update it as the step lands.

The level's clock: `$2A` (seconds left), `$2B` (frames to the next second),
`$2C` (the level's start value), `$2D` (hurry-up state) and `$A9B1`.

- `$392A`-`$393C`: `$2A` and `$2C` take the level's time: 30 s, 10 s when SUBFLG
  is `$37`, 20 s from SUBFLG `$38`. `$09FD` sets `$2B` to `$32`.
- `$06AB`-`$06C6` (IRQ): `$2B` counts down once a frame unless it is `$FF`;
  at zero it reloads `$32`, decrements `$2A`, and decrements `$A9B1` if that is
  above zero. The port's 25-pass `Items.Tick` (`$5D`/`$5E`) is the same tick.
- `$0504`-`$0512`: a respawn restarts `$2A`-`$2D` (`PlayerRespawn` has a comment
  where this goes).
- The pause handler (`$7E80`) saves and restores them; not needed.

Verify: a VICE capture of `$2A`, `$2B`, `$2D` and `$A9B1` a pass at a time over
a level and a respawn.
