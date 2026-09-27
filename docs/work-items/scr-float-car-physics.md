# Stunt Car Racer: convert CarPhysics to float

[StuntCarRacerSharpLib] Float physics step 3 of 4. After
[scr-float-trig.md](scr-float-trig.md).

Float physics conversion (split 2026-07-14): ~4,130 lines of 68000-style
scaled-integer code — `CarPhysics` 2,733 lines over four partials,
`OpponentPhysics` 1,397 over two. Do strictly in order; the golden-trace
harness is the safety net for everything after it. Sequence after the Super
League physics item, which edits the same files.

Convert `CarPhysics` — [CarPhysics.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Cars/CarPhysics.cs),
`.Motion`, `.Road` and the crane/chain-recovery `.Chains` — to float: fields,
locals, and the `>> LogPrecision` rescales. Hunt the traps: arithmetic
right-shift on negatives rounds toward -infinity while integer division
truncates toward zero, and any deliberate short/int overflow wrap needs an
explicit equivalent. Validate against the golden traces with tolerances; rework
`CarPhysicsTests`' exact assertions as tolerance-based in the same session.
