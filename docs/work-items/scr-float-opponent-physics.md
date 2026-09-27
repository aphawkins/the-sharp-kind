# Stunt Car Racer: convert OpponentPhysics to float

[StuntCarRacerSharpLib] Float physics step 4 of 4. After
[scr-float-car-physics.md](scr-float-car-physics.md).

Float physics conversion (split 2026-07-14): ~4,130 lines of 68000-style
scaled-integer code — `CarPhysics` 2,733 lines over four partials,
`OpponentPhysics` 1,397 over two. Do strictly in order; the golden-trace
harness is the safety net for everything after it. Sequence after the Super
League physics item, which edits the same files.

Convert `OpponentPhysics` (+ `.Interaction`, `OpponentData`) the same way,
including its `_random`-driven speed logic (seedable, so traces stay
reproducible); rework `OpponentPhysicsTests`; then delete the unused integer
trig (`AmigaTrig`, and `TrigCoefficients` if `Scene3D` was converted) and drop
`Track.LogPrecision` from the physics path. Done means racing complete laps on
several tracks, comparing feel against the integer build.
