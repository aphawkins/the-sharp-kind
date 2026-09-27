# Elite: docking rings should be polygons per rendition

**Bug.** Reported by the maintainer, 2026-09-18. The extent fix it followed
landed 2026-09-27: the widest ring now reaches the viewport's corners.

The rings should not be circles: octagons (8 sides) on the 8-bit rendition,
hexadecagons (16 sides) on the 16-bit rendition, and circles only on a Modern
rendition.

`Draw()` always calls `_draw.Graphics.DrawCircle(...)` — there is no
per-rendition break pattern; `BreakPattern` lives in the shared
`EliteSharpLib` and is used unchanged by `DockingView`
([DockingView.cs:42](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Views/DockingView.cs))
regardless of rendition.

Fixing it needs either a rendition-specific ring drawer (mirroring how
`GalacticChartView8Bit` / `16Bit` draw their own circles) or a shared
`DrawRegularPolygon(centre, radius, sides, colour)` primitive plumbed through
`IEliteDraw`/`EliteDraw`
([IEliteDraw.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Graphics/IEliteDraw.cs),
[EliteDraw.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Graphics/EliteDraw.cs))
that `BreakPattern` calls with 8, 16, or a circle depending on the active
rendition. No Modern rendition exists yet, so the circle case is whatever
`BreakPattern` already does.
