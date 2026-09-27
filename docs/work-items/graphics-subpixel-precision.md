# Sub-pixel rasterisation precision

[SharpKind.Graphics] **[DECISION]** — a deliberate departure from the
originals; the maintainer decides whether modernity wins here.

Triangle edges snap to integer scanlines (`MathF.Ceiling`/`Floor` in
`DrawTriangleFilled` and its variants), so geometry jitters as it moves rather
than sliding smoothly. Fixing it means carrying fractional edge coverage through
all four triangle routines — worth it only alongside anti-aliasing, which is
declined in [wont-do.md](wont-do.md).
