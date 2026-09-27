# Elite: test with a rendition of arbitrary size

[EliteSharpLib] Split 2026-09-10 from "Elite at non-512x512 resolutions". The
survey found `ViewLayout`, `Stars`, `WorldProjection`, `ShipBase.IsWithinView`,
`ViewFrustum.FromViewport` and the planet/sun renderers all derive from screen
size or radius — this item makes that checked rather than read.

[FakeAbstraction.cs:17-22](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/test/EliteSharpLib.Fakes/FakeAbstraction.cs)
hardcodes 512x512 "to match `SDLProgram`" — stale, since renditions now decide
resolution. Give the fakes a size that is neither square nor a shipped
rendition's (say 800x480), and assert the derived layout, projection and
starfield bounds against it. Lands before
[elite-remove-square-fov-test.md](elite-remove-square-fov-test.md), turning it
into a failing test.
