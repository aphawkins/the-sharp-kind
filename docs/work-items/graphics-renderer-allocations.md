# Remove per-frame allocations in the polygon renderers

[SharpKind.Graphics] `Submit` does `new Vector2[points.Length]` (plus
`new float[...]` in the z-buffer path) for every polygon every frame, and
`ShipBase.BuildFacePolygon` returns a fresh `Vector2[]` — undoing the care
`ShipBase` takes to pool `_pointList`/`_cameraList` and stackalloc its clip
buffers. A persistent vertex/index buffer is the modern shape.

Not a measured bottleneck (see [performance-notes.md](../performance-notes.md)),
so do it for tidiness or not at all. Sequence after
[graphics-depth-range-guard-band.md](graphics-depth-range-guard-band.md), which
changes `Submit`'s signature.
