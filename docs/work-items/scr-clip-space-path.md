# Stunt Car Racer: move rendering onto the clip-space path

[StuntCarRacerSharpLib] Part 5 of 6 of the clip-space run. After
[graphics-clip-space-clipper.md](graphics-clip-space-clipper.md) and, strictly,
[scr-float-trig.md](scr-float-trig.md) — until then the view transform is a
fixed-point 3x3 with a `>> Track.LogPrecision` rescale.

Move `Scene3D.TransformPoint`/`ProjectPoint` and `TrackRenderer`'s per-triangle
clip loop
([Scene3D.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Rendering/Scene3D.cs),
[TrackRenderer.cs:200-232](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Rendering/TrackRenderer.cs))
onto the clip-space path. Verify against the physics golden traces and drive a
lap.
