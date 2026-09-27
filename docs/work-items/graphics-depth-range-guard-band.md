# Normalised depth and guard band in clip space

[SharpKind.Graphics] Part 6 of 6 of the clip-space run. **Blocked by**
[elite-clip-space-path.md](elite-clip-space-path.md) and
[scr-clip-space-path.md](scr-clip-space-path.md).

With both games in clip space, `z / w` is a normalised depth, so the z-buffer
can store that instead of camera z — an `IPolygonRenderer` signature change,
since `Submit`'s `depths` are camera-space today — and a guard band is a side
plane pushed outwards rather than a special case.

Retire the `Math.Max`/`Min` span clamps in `DrawTriangleFilled` only if a
measurement says the clip now pays for itself; the 2026-08-05 benchmark in
[wont-do.md](wont-do.md) (frustum side-plane clipping) says an off-screen face
is cheap today. This item is what closes that entry.
