# Bubble Bobble: the main loop's pass rate

Open question in [bb-port-plan.md](../bb-port-plan.md) §6: in VICE the main loop runs a pass every three IRQ
frames (level 1, 260 passes, always 3), not two. The port runs 25 passes a
second, so routines counting passes rather than seconds (`$13BE`'s `$A9FA`,
the bubble timers) may run half again faster than the C64. `LevelTimer`
already keeps wall time. Find which counters matter, measure them in VICE,
and fix the rate or the counters. Possibly a bug.
