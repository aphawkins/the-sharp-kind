# Elite 8-bit: the "sounds during music" option

[EliteSharpLib] C64 Elite's `MUSILLY` option lets sound effects play while
music does. The port plays C64 Elite's default: while the title or docking
music plays, effects are silent (`SidSound`, `SidMachine.IdleFrame`).

Fix direction: a game setting that makes `SidSound` run the effect engine on
music frames too, as the C64's interrupt handler does when `MUSILLY` is set.
