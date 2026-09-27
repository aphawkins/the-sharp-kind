# Won't do

Investigated and deliberately not done. Kept so the same ground isn't
re-covered. Each entry says what would make it worth revisiting; if that
happens, move it back into [backlog-roadmap.md](../backlog-roadmap.md) as a
work item.

## Defects that aren't

### Flight doesn't pause on focus-loss keys while piloting (Elite)

**Authentic, not a defect.** Pressing F5-F11 in space switches the screen and
the universe keeps moving behind it: `SetView` is all the key does, and
`Simulate`
([EliteMain.cs:183](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/EliteMain.cs))
runs `MoveUniverse` and `UpdateInFlight` on every tick that is not docked,
whatever screen is up. The original does the same — in space the BBC main loop
iterates all six parts every time round, and the view (`QQ11`) only selects
what is *drawn*
([elite-source-flight.asm:24376-25007](https://github.com/markmoxon/elite-source-code-bbc-micro-disc/blob/main/1-source-files/main-sources/elite-source-flight.asm)).
A pause would be a deliberate deviation. The existing P/R pause could not be
reused anyway — a paused tick returns before `Compose`. Revisit only as a
feature, if a playability deviation is wanted on purpose.

### Buying more than 255g of Gold/Platinum doesn't work (Elite)

Authentic to the original ("broken as designed"); documented, not fixed.

### Intro2 parade shows 29 of ~33 ship models (Elite)

[ShipFactory.cs:98-129](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Ships/ShipFactory.cs)
— Cougar, Constrictor and the Lone variants are mission-specific ships,
deliberately excluded from the parade; confirmed intentional.

### Equipment tech-level gating (Elite)

E.C.M., Fuel Scoops, Escape Pod, Energy Bomb, Energy Unit, Docking Computer and
Galactic Hyperdrive — **not a bug.** A 2026-08-04 re-derivation of the
original's positional formula (item at `PRXS` position *N* shows once
`planet_tech >= N - 2`, from the `EQL1` loop at
[elite-source-docked.asm:16371-16408](https://github.com/markmoxon/elite-source-code-bbc-micro-disc/blob/main/1-source-files/main-sources/elite-source-docked.asm))
matches every item's `TechLevel` exactly. A prior sweep had mis-derived it.

### Docking-bay roll-alignment threshold (Elite)

`FlyToDockingBay`, `Pilot.cs:341`, `MathF.Abs(dir) >= 0.9166f` — **not a bug.**
A prior sweep computed the original's `CPX #66` threshold as `33/96`, but
`TAS4`'s dot product is right-shifted by 8, capping its high byte at
`96*96/256 = 36`. The correct normalisation is `33/36 ≈ 0.9167` — already in the
port, and symmetric as the original's comment describes.

### Missing 80-point splash damage from a nearby missile (Elite)

Original `TA35`/`TA87`
([elite-source-flight.asm:9219-9245](https://github.com/markmoxon/elite-source-code-bbc-micro-disc/blob/main/1-source-files/main-sources/elite-source-flight.asm))
applies 80 damage whenever a missile is destroyed near the player;
`TryGetMissileHeading` (`Conflict/Combat.cs:1147-1194`) has no such path.
**Blocked, not abandoned (2026-08-04).** TA35's proximity test only examines
each axis's *low* byte (`x_lo OR y_lo OR z_lo`), which read literally fires on
~1-in-256³ ticks. Left unfixed rather than guess a distance convention. Revisit
if someone can resolve what that check is really testing.

### Selecting an 8-bit rendition in SCR does nothing

Not a throw (re-checked 2026-08-09): SCR has one asset set, and
`AssetLocator.Create(engine.Rendition)` reads it whatever the name says (see
[asset-structure.md](../asset-structure.md)). Cosmetic — the setting reads back
a rendition SCR is not drawing as. Short of authoring a real 8-bit set (below),
the honest fix is for SCR to say the value is ignored.

## Graphics pipeline

### Frustum side-plane clipping

**Profiled, not worth fixing.** `NearPlaneClip` clips one plane; the sides are
handled after projection by `SetClipRegion` and the span clamps in
`DrawTriangleFilled`. Output was never wrong. `OffScreenTriangleBenchmarks`
(2026-08-05, 512×512, i5-14600K): a full-height face entirely off to the left
costs **1.79 µs**, straddling the right edge **1.78 µs**, against **93.7 µs**
for one full-screen face; off the top or bottom is **8 ns**. It would take
~9,000 wasted faces per frame to spend a 60fps budget. Closed properly by
[graphics-depth-range-guard-band.md](graphics-depth-range-guard-band.md).

### Fan triangulation of concave polygons

**Not reachable with current assets.** `DrawPolygonFilled` fans from vertex 0,
wrong for a concave polygon, but a 2026-07-31 sweep of both model sets found
zero concave faces, and clipping a convex polygon yields a convex one. Revisit
only if hand-authored models are added.

### Programmable shading, stencil, fog, anti-aliasing, post-processing, instancing

All absent, all deliberately so (2026-07-31 gap analysis). They would make
either game stop looking like its 1984/1989 original rather than merely look
better, and none has a use in a flat-shaded space sim or racer.

### Software rasteriser throughput

Per-pixel `SetPixel`, insertion-sorted painter chain, no spans/SIMD. The
2026-09-06 profile ([performance-notes.md](../performance-notes.md)) puts bare
rasterisation at **0.42 ns/pixel** — 1 % of a 60fps budget for a full-screen
face. The painter chain is moot: `DepthSort` defaults to `ZBuffer`.

### Shared text/HUD-panel helper for both games

**Surveyed 2026-08-19, nothing to lift.** Left/centre/right text is already
shared on `IGraphics`. Elite's chrome is viewport-relative and per tier, already
shared through `IBaseView`; SCR has no chrome, and its text uses three
different conventions. A helper would have one caller per shape. Revisit only if
SCR grows real panel chrome (race-pause or race-result screens).

## Assets and repo

### SCR's 8-bit asset set

Not for now (2026-07-31). Needs a 16-colour palette, an 8-bit manifest, an 8x8
font, a chosen 8-bit resolution, and hand-authored `atlas.bmp` and `menu.bmp` —
the bitmaps being the blocker, since downscaling gives filtered 16-bit imagery
rather than 8-bit-era art. Revisit if the art gets authored.

### Remaining code-complexity rules from issue #5

`S1541`/`S3776` (102 sites) and `S107` (20 sites) stay `severity = none` — the
survivors are mostly ported 6502/Amiga methods, and `CA1502`/`CA1506` cover the
same ground. `S109` (magic numbers, 4087 sites) likewise stays off. Revisit only
if a specific project is scoped for it.

### SCR remake's Windows-only infrastructure

DXUT registry prefs, clipboard, DirectSound, `MessageBox` dialogs — not ported;
see [scr-readme.md](../scr-readme.md).

### ptitSeb's remaining debug toggles

Not ported (2026-07-19 parity audit): F1 test key, F2 vertex-buffer toggle, the
'Z' reposition key, the disabled action-replay/Amiga-recording harness, the
French-keyboard digit remaps, and the SDL command-line video flags. (F5 stats
and F6/F7 freezes were ported, 2026-08-19.) `Chime.wav` is unused by both code
bases.
