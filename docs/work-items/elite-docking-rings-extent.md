# Elite: docking rings don't reach the viewport's side edges

**Bug.** Reported by the maintainer, 2026-09-18. Split from the docking-rings
[LARGE] item; do this before [the shape fix](elite-docking-rings-shape.md).

While docking, the break-pattern rings should expand to fill the viewport —
reaching left and right as well as top and bottom — but currently only reach
the top/bottom edges.

[BreakPattern.cs:27-28](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/BreakPattern.cs)
sizes `RingStep` from `MathF.Min(ViewportCentre.X, ViewportCentre.Y)`, capping
the largest ring's radius at the viewport's shorter half-extent — on a
landscape viewport that is the vertical half-extent, so the rings meet the
top/bottom edges and stop well short of the left and right ones. Its own
comment ("meets the near edges rather than running off the far ones") records
this as deliberate, but it contradicts the intent: the rings should reach the
*farthest* edges (grow from `Max`, or size X and Y independently as an
ellipse).

Small and self-contained, with an obvious regression test.

**Related off-by-one (noted 2026-09-15).** The widest ring reaches exactly
`ViewportHeight` — one row past `ViewportBottom` — so on the 8-bit tier it
would put 29 pixels into the console band if drawn without the window region's
clip. It is drawn with that clip, so nothing is wrong today. Correcting it
shrinks all twenty rings slightly and moves the 16-bit golden frames; since
this item changes the ring sizes anyway, settle it here with the maintainer
rather than silently.
