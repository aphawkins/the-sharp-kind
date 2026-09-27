# Split the viewport transform into its own stage

[SharpKind.Graphics] Part 1 of 6 of the homogeneous clip-space run (split
2026-09-10). Parts 1-3 are library-only and change no game output.

Split
[PerspectiveProjector.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.Graphics/PerspectiveProjector.cs)
into the perspective divide (camera space to NDC) and a `Viewport` mapping NDC
to pixels, keeping `Project` as the composition of the two. Both games keep
calling `Project` and get the same pixels — a library change with library
tests, and what makes an NDC exist to clip against.

**Preserve:** `Project` returns *pixels* (`Centre + Focus * x / z`) and every
caller expects pixels; renderers take a camera-space depth per point
(`float[] depths` on
[IPolygonRenderer](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.Graphics/Rendering/IPolygonRenderer.cs)).

Background: Elite already transforms by a float `Matrix4x4`, both games share
`PerspectiveProjector`, `ViewFrustum.FromViewport` derives all six planes, and
both clip against `NearPlaneClip`. What is missing is `w`, NDC, and a separate
viewport stage.
