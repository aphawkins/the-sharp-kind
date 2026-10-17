# Bubble Bobble: a fixed-timestep loop in place of the IRQ phases

[BubbleBobbleSharpLib] `GameLoop` models the C64's main loop and IRQ
(`TicksPerCycle`, `IrqOnlyPhase`), and re-enters a pass through a `Resume` enum
and `IGameHold`. `GameLoop` is 868 lines and `BubbleBobbleMain` 684.

Fix direction: a fixed-timestep update, and the sequenced rooms and waits as
state machines or iterators in place of `Resume`. **[DECISION]** first: this
changes the 16.7 a second pace the main loop counts at, which the golden
traces measure.
