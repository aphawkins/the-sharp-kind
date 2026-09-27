# Stunt Car Racer: convert angles and trig to float

[StuntCarRacerSharpLib] Float physics step 2 of 4. After
[scr-golden-trace-harness.md](scr-golden-trace-harness.md).

Float physics conversion (split 2026-07-14): ~4,130 lines of 68000-style
scaled-integer code — `CarPhysics` 2,733 lines over four partials,
`OpponentPhysics` 1,397 over two. Do strictly in order; the golden-trace
harness is the safety net for everything after it. Sequence after the Super
League physics item, which edits the same files.

Replace `AmigaTrig`'s 16384-scaled short table and `TrigCoefficients` with
float equivalents; decide the angle unit (keeping 0..65536 as a float unit is
least churn) and replace the `& (MaxAngle - 1)` wraps with a float wrap helper.

Decide the rendering boundary too: `Scene3D.TransformPoint` consumes
`TrigCoefficients` and `Track.LogPrecision`
([Scene3D.cs:102-113](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Rendering/Scene3D.cs))
— convert its fixed-point view transform in the same step or leave a shim. This
is where SCR's last non-`System.Numerics` piece (issue #3) is resolved, and it
is the prerequisite for [scr-clip-space-path.md](scr-clip-space-path.md).
