# Add a projection matrix and a w

[SharpKind.Graphics] Part 2 of 6 of the clip-space run. After
[graphics-viewport-stage.md](graphics-viewport-stage.md).

Build a `Matrix4x4` from focus, aspect, near and far, so a camera-space point
transforms to clip space and the divide by `w` reproduces today's projector.
Library only, no caller changes. The test that matters compares the matrix path
against `PerspectiveProjector` over a sweep of points, to a tolerance.
