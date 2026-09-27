# Stunt Car Racer: golden-trace harness for the physics

[StuntCarRacerSharpLib.Tests] Float physics step 1 of 4.

Float physics conversion (split 2026-07-14): ~4,130 lines of 68000-style
scaled-integer code — `CarPhysics` 2,733 lines over four partials,
`OpponentPhysics` 1,397 over two. Do strictly in order; the golden-trace
harness is the safety net for everything after it. Sequence after the Super
League physics item, which edits the same files.

Drive the existing integer physics with scripted `CarInput` sequences on two or
three tracks, record car/opponent state per physics tick (position, speeds,
angles, damage, boost) to committed baseline files, and add a test that replays
and compares. Pure test code — no physics changes. Exact-integer unit-test
assertions stay untouched until each class converts.
