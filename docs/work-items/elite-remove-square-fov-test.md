# Elite: delete the square field-of-vision test

[EliteSharpLib] After
[elite-arbitrary-size-test-rendition.md](elite-arbitrary-size-test-rendition.md).

[EliteDraw.DrawObject](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Graphics/EliteDraw.cs)
tests `MathF.Abs(obj.Location.X) > obj.Location.Z` (and the same for Y) — a
hardcoded 90° cone from the original, the one surviving place that assumes the
viewport. Below 90° it is redundant (`ShipBase.Draw` culls against the real
frustum straight after), but a viewport wide enough to see past 45° loses ships
genuinely on screen. Explosions, planets and suns return before it, so ships are
its whole reach. Nothing is wrong on either shipped rendition.

In passing: `IsWithinView` passes `ViewportWidth`/`ViewportHeight` where
`FromViewport` documents right and bottom *edges* — one pixel generous; make
them agree.
