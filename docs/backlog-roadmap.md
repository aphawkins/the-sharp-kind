# Backlog and Roadmap — The Sharp Kind

Features, refactors, cleanups and spikes — work that **adds or reshapes**,
not work that fixes. Prioritised with MoSCoW (per
[architecture-principles.md](architecture-principles.md)).

Defects live in [backlog-issues.md](backlog-issues.md), and take priority
over everything here: both games work, so the repo fixes what is broken
before it builds what is missing. Nothing in this file should start while
that file has open Musts.

This file merges the 2026-07-11 architecture/code-quality review, the
business-application practices review, and the retired `issues.md`,
`release-plan.md` and `scr-conversion-plan.md`. It was split from the
defect list on 2026-07-31.

How to use this file:

- Each item is one concern, small enough for a single focused session, and
  self-contained (paths, line numbers, problem, fix direction). Items too
  big for one session are tagged **[LARGE]** — split them in a follow-up
  conversation before starting, not here.
- Definition of done (from the retired SCR plan, now repo-wide): build the
  full solution, run the complete test suite, and smoke-test the affected
  app(s) live if the change touches shared code or either game loop.
- When an item completes, delete it here and record it in
  [CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md). If it's significant enough that a player
  or contributor would want to know about it at a glance, add a line for it
  to [release-notes.md](release-notes.md) too.
- Line numbers date from the review; verify before editing.

## Decisions

Maintainer decisions live in [decisions.md](decisions.md), not here — each
one blocks or reshapes items below; check there before starting an item
that mentions a decision.

## Must

(none open — see [CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md) for completed items)

## Should

### Performance

(none open — see [CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md) for completed items)

Profiled 2026-09-06 (i5-14600K, 512x512) after a frame-rate drop was noticed
while docking, as a Coriolis fills the view. Every item it produced has since
landed; the evidence is kept here because it is where the next performance
item should start rather than re-profiling from nothing. The benchmarks that
produced it are
`StationBenchmarks`/`GraphicsPreset` (a Coriolis drawn through the real
pipeline - `RenderStart`, `DrawObject`, `RenderEnd`) and
`DepthFillBenchmarks`/`QuantiserBenchmarks` (one full-screen depth-tested
face, and the quantisers on their own); re-run them to check any of this.

**The settings decide whether there is a problem at all.** The presets that
ask the quantiser per pixel are the Gouraud pair, and they now cost 1.8-3.3x
the defaults. The figures are 2026-09-12; the column in brackets is the same
preset on 2026-09-06, before the per-face dither table, the cheaper
`Quantise` call and the Gouraud span's own arithmetic:

| Preset          | Z=1000        | Z=250             | vs default |
|-----------------|--------------:|------------------:|-----------:|
| Unlit/Nearest   | 113 us (117)  |   807 us (  804)  |       1.0x |
| Lambert/Ordered | 118 us (567)  |   801 us (4 841)  |       1.0x |
| Gouraud/Nearest | 212 us (519)  | 1 468 us (4 028)  |       1.8x |
| Gouraud/Ordered | 355 us (771)  | 2 624 us (6 397)  |       3.3x |

On the defaults the worst frame spends 807 us on the station - 5 % of the
budget, not something a player would see. **The maintainer confirmed
(2026-09-06) that the drop reproduced in all three of the other presets and
never on the defaults**, which was the table's shape then: the per-pixel
quantiser and nothing else. Lambert/Ordered has since joined the defaults.

**Where the per-pixel time goes** (one full-screen face, 262 144 pixels; the
right column is the cost the row adds over the row above it):

| Ingredient                         |      Total | Adds per pixel |
|------------------------------------|-----------:|---------------:|
| `ClearDepth` (per frame, no draw)  |    24.8 us |              - |
| Rasterise only, no depth buffer    |   109.8 us |        0.42 ns |
| + per-pixel depth test             |   500.9 us |        1.49 ns |
| + per-pixel clip test              |   712.8 us |        1.14 ns |
| + ordered dither, channel grid     |  3 973.9 us |      13.25 ns |
| + ordered dither, 16-entry palette |  6 800.0 us |       24.0 ns |
| Gouraud (interpolate + quantise)   |  3 849.2 us |       12.8 ns |

And the quantisers alone, one full screen of calls with the loop cost removed
(re-measured 2026-09-10): `ChannelGridQuantiser` **0.1 ns** a call - it reads a
256-entry table now, where it rounded to the grid for 5.5 ns before -
`PaletteQuantiser` **15.5 ns**, and the `OrderedDitherQuantiser` wrapper a
further **4.1-4.9 ns** on top of whichever it wraps, down from 8.3-8.7 now the
sixteen nudges are resolved at construction. Against those, resolving a flat
face's sixteen possible dithered answers once and indexing them per pixel costs
**0.15 ns**.

Two things this profile settles rather than opens:

- **The off-screen theory is refuted.** The suspicion was that the station's
  faces overflow the view and their pixels are rasterised then clipped away.
  They are not: the fills clamp before the loop, not per pixel, so an
  off-screen pixel is never visited. The distance sweep shows it directly -
  from Z=500 to Z=250 the station's *unclipped* area grows 4x while the cost
  grows only 1.8x, because the growth lands off-screen and the clamps drop
  it. This agrees with the 2026-08-05 `OffScreenTriangleBenchmarks` figures
  in [backlog-issues.md](backlog-issues.md). Nothing here argues for frustum
  side-plane clipping.
- **`ClearDepth` costs 25 us per frame** zeroing two full-screen arrays,
  drawn or not - the whole of the 28 us floor. That is 0.15 % of a 60fps
  budget, so it is recorded here and deliberately not an item.

The stale "the game is fixed at 13.5fps by design" premise in the
rasteriser-throughput Won't entry was corrected 2026-09-06 in the same pass;
bare rasterisation stays a Won't, now on the 0.42 ns/pixel measurement above.

**Three rows of the per-pixel table are historical.** A flat fill resolves
the dither once per face since 2026-09-10, not once per pixel, so `Dithered`
and `DitheredPalette` measure at the flat fill's own cost - 667 us each
against 678 us - and the 13.25 and 24.0 ns per pixel are gone. The Gouraud
row went with the cheaper `Quantise` call the same day and again on
2026-09-12, when the span stopped lerping from `t` at each pixel: it measures
**1 299 us** now, **2.4 ns** a pixel over a flat fill rather than 12.8. The
four rows above those are still the 2026-09-06 figures and were not
re-measured. What is left of the Gouraud span's cost is the per-pixel
arithmetic itself; resolving the quantiser to a concrete type at the top of
the span was measured on 2026-09-12 and saves only 0.18 ns a pixel, which
does not pay for a copy of the pixel loop per quantiser.

**The clip row is historical as well.** The fills clamp their scanlines and
spans to the clip rectangle rather than to the screen since 2026-09-12, so no
pixel is tested against it and the 1.14 ns is gone. The measurement is
`DepthFillBenchmarks/FlatClipped` against `Flat`: a fill inside Elite's
viewport rectangle costs what its smaller area says it should - 608 us
against the full-screen fill's 680 - where it used to cost more than the
full-screen fill did, at 774 us. `StationBenchmarks` did not move and was not
expected to: it draws with the clip left full-screen, so it never paid the
test the game pays.

### Release engineering (from the retired release plan)

(none open — see [CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md) for completed items)

## Could

### Rendering in three layers - done (2026-09-15)

All four items landed; see
[CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md).
A frame is now a `LayerRunner` over three `RenderLayer`s in both games,
furthest first - the backdrop, the universe or world, and the HUD over both.
The window region is the opening the HUD's art leaves: Elite's viewport in
the shipped tiers, and the whole display in SCR, which is why nothing about
SCR's geometry moved. The kept scoping notes below are the ones a later item
still needs.

**The focal length is not the region's.** Elite derives `Focus` from
`ScreenHeight`, not from the viewport (decided 2026-07-29, so a wider screen
shows more rather than magnifying). Deriving it from the region would take
16-bit from 512 to 383 and change every frame. Leave it alone. The Modern
rendition makes the window region the whole screen and floats the console
over it, so it is the first thing that will exercise a region that is not
the viewport.

**One off-by-one found and deliberately not fixed.** `BreakPattern`'s widest
ring reaches exactly `ViewportHeight` - one row past `ViewportBottom`, the
last pixel inside - so on the 8-bit tier it puts 29 pixels into the console
band if it is ever drawn without the window region's clip. It is drawn with
that clip, so nothing is wrong today, and `RingStep`
([BreakPattern.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/BreakPattern.cs))
derives every ring from the same half-extent - so correcting it would shrink
all twenty rings slightly and move the 16-bit golden frames, which is a
maintainer decision about the animation rather than a defect to fix
silently. The equivalent off-by-one in the 8-bit border and in
`LaserDrawBase` was corrected, because there the clip and the fix produce the
same pixels.

### The debug overlays should be common to both games

Each game grew its own developer furniture, and they have ended up with
opposite halves of the same feature: Elite shows a frame rate and no stats,
SCR shows stats and no frame rate.

- [ ] [SharpKind.Abstraction] Make the debug overlays one thing both games
      draw, most likely as a fourth layer - always closest, always full
      screen, drawn after the HUD so nothing can cover it, and appended to
      each game's existing `LayerRunner`.
      **What is there now.** Elite counts presents into `_framesDrawn` (a
      list of `Stopwatch` timestamps trimmed to the last second) in
      `Draw()`, and draws through `_baseView.DrawFps` inside its HUD layer
      ([EliteMain.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/EliteMain.cs)).
      SCR draws `Race.DrawStats` - the frame gap and the F6/F7 per-car
      freezes - from `StuntCarRacerMain.Draw` after the current screen, in
      no layer at all
      ([Race.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Race.cs)).
      Even the switches disagree: Elite's is the `engine.graphics.showFps`
      config setting, SCR's is an F5 key toggle.
      **Where the seams already are.** `ShowFps` is on the *shared*
      [GraphicsConfigSettings](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.Abstraction/Config/GraphicsConfigSettings.cs)
      and only Elite reads it, so half the plumbing is in place. Frame
      timing belongs lower still: `GameHost.Run`
      ([GameHost.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.Abstraction/GameHost.cs))
      already drives both games' loops and is the one place that knows when
      a frame is actually presented, which is what Elite is counting by
      hand today.
      **Two things to settle rather than assume.** Whether the text is drawn
      by the shared layer through `IGraphics.DrawTextLeft` or handed back to
      each game - Elite draws HUD text through its rendition's `IBaseView`
      and its per-tier fonts, SCR through `StuntCarRacerMain.SmallFont` -
      and whether the two switches become one. This is developer furniture,
      screen-absolute and in no rendition's style, so it is not what the
      shared-HUD-chrome survey in the Won't section declined; that one was
      about viewport-relative game chrome, and its reasoning does not reach
      this.
      **The golden frames are the acceptance test.** Both overlays default
      to off - `ShowFps` is false in the shipped config and `ShowStats`
      starts false - so every baseline in both games should stay
      byte-identical; a moved baseline means the default leaked.

### The 'Modern' rendition

Scoped with the maintainer on 2026-09-12. A third rendition standing in for
no machine at all: **1280x720, one window scale, no restriction on colour**,
with the scanner centred at the bottom of the screen and the rendered
universe filling every side of it, where both shipped tiers reserve a
full-width strip for the HUD and stop the viewport above it. The HUD art is
a placeholder for 3D instrumentation, so nothing here should invest in it.

Three decisions the maintainer settled, so none of them is reopened below:

- **1280x720, not 1920x1080.** A 1080p render is larger than the usable
  desktop area on a 1080p display once the titlebar and taskbar are counted.
  The intention is that Modern eventually resizes or goes fullscreen at the
  native resolution, which is the last item here and is genuinely separate
  work — `SDLGraphics.ScreenWidth`/`ScreenHeight` are set once in the
  constructor and the render target, depth buffer and surface-id buffer are
  all sized from them
  ([SDLGraphics.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.SDL/SDLGraphics.cs)), and
  `ViewLayout` is an immutable record every view caches its geometry against.
- **`DesignScale` 2, the same as the 16-bit tier.** The design scale is a
  property of the artwork rather than of the resolution, so it does not have
  to divide 720 — and it cannot be fractional anyway (`IRendition.DesignScale`
  is `int`, `EliteDraw` truncates it for the explosion blocks, and
  [decisions.md](decisions.md) rules half-pixel chrome out). Modern's chrome
  is the 16-bit tier's, so it is drawn at the 16-bit tier's scale with more
  room around it. 3 needs 768 of 720 rows and overflows.
- **32-bit colour, and no other restriction.** Free: `AssetColourLimits`
  already defaults to `ChannelBits = 8`, and `ChannelGridQuantiser` treats 8
  bits as "every level there is" and passes colour through untouched, so the
  manifest declares it and no code changes.

Do the layer work above first: Modern is the rendition whose window region
is the whole screen, and there is nowhere to say so until a layer owns that
rectangle. The three below are then independent of each other.

- [ ] [EliteSharp.Renditions.Modern] The rendition itself: a new plugin
      assembly and `Assets` folder alongside the two shipped ones, 1280x720,
      `DesignScale` 2, `WindowScales => [1]`, `ShadesShips`, and a window
      region of the whole screen. Assets are a copy of the 16-bit tree —
      self-contained like the other two, so each file can be replaced
      independently as real artwork arrives — with the manifest's `Colours` block widened to 32-bit.
      Screens derive from the 16-bit set and centre against `ViewportCentre`
      rather than inheriting its absolute offsets. The 640-wide scanner
      centres at x=320 of 1280, which is exact. Wire it into
      [EliteSharp.csproj](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/apps/EliteSharp/EliteSharp.csproj)'s
      `RenditionProject`/`RenditionAsset` lists; it then appears on the
      settings screen with no settings code change, since `ChoiceSetting`
      offers whatever is installed. Smoke-test it — this is a game loop.
- [ ] [EliteSharpLib] Log the field of view at startup. The startup settings
      line logs `rendition` and `windowScale` but not `fieldOfView`
      ([GameApp.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.App/GameApp.cs)), so a bug
      report from a commander who has widened it does not say so — and it
      changes what is on screen, which is exactly what a screenshot in a bug
      report is of. One field.
- [ ] [SharpKind.SDL] **[LARGE]** A resizeable window, and fullscreen at the
      native resolution. This is what Modern is ultimately for, and it is the
      reason `ScreenWidth`/`ScreenHeight` being constructor-fixed matters:
      the render target, depth buffer and surface-id buffer are all sized from
      them ([SDLGraphics.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.SDL/SDLGraphics.cs)).
      So it needs a resize signal, a rebuild of those buffers, and a way to
      replace the immutable `ViewLayout` every view was built against —
      `BaseView8Bit` and `BaseView16Bit` capture the record in their
      constructors, and every other view reads it off the surface per draw.
      Harder than it looks: most views and controllers are written against
      *pixel* positions rather than scaled ones, so they would all have to
      move when the screen does. Survey that before splitting.

### Input

Nothing open. Two items closed on 2026-09-13: the SideWinder mapping (the
hat selects the view, the throttle lever sets the speed, missiles reach the
stick, and each device gets its own layout), and moving all of it out of
code into `elite.controls.sharp` with a setting for which attached
controller is flown. See [CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md) and
[elite-readme.md](elite-readme.md).

- [ ] [StuntCarRacerSharpLib] Stunt Car Racer could read its controls from a
      file too. The machinery is now a library
      ([SharpKind.Abstraction.Controls](https://github.com/aphawkins/the-sharp-kind/tree/main/src/useful/libs/SharpKind.Abstraction/Controls)),
      generic over a game's own actions and axes, so what SCR needs is an
      action enum, a set of defaults and the registration - the same three
      pieces Elite keeps. It gains less than Elite did: its steering is
      fixed at +/-15 either way, so there are no axes to bind and the file
      would only move buttons. Worth doing for consistency rather than for
      capability.

      Note the library takes an enum's zero value to mean "nothing", so
      SCR's own enums have to reserve it - see the Elite entry in
      [CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md).

Two things were deliberately left. A Competition Pro has four buttons and
five controls worth having, so targeting a missile stays on the keyboard's
`T`. And galactic hyperspace stays on `Ctrl-H`: a modifier is not an action
and has no place in the bindings file.

### From decisions (2026-07-27)

Committed by the maintainer decisions in [decisions.md](decisions.md). The
frame-rate item was audited and scoped on 2026-08-26 and is now the ordered
list below; the data-driven content item was scoped and split the
same way on 2026-08-31.

Elite's frame rate vs the 13.5Hz tick — **done 2026-08-26** (see
[CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md) and the two 2026-08-26 entries in
[decisions.md](decisions.md)). All seven items landed: the golden-trace and
frame-check harness, the simulate/compose split, the housekeeping clock, the
motion, the AI pacing, the animations, and running at the configured `Fps`.

The two gaps it left were closed the same day: the starfield no longer draws
from the game's random stream, and an E.C.M. burst is counted in ticks. The
same twenty seconds at 13.5Hz and at 60Hz now end on the same screen, within
one housekeeping step, and meet the same ships.

The [LARGE] item was surveyed and split on 2026-08-31, and two of its
three session-sized parts landed the same day — the ship definitions became
a table, and `ShipType` became the ids in it (see
[CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md)). Only the equipment table is left.

The precedent they all follow is the goods set: **the assembly is the door,
the file is the table** (see the 2026-08-27 entries in
[decisions.md](decisions.md)) — a plugin referencing only
`EliteSharp.Abstractions`, found beside the executable by a loader, reading
its own JSON from beside itself. The survey also corrected the
original wording three times: `ShipFactory` never built ships by reflection
(`CreateShipFromName` was long gone; it was an explicit dictionary of 33
constructor lambdas, so what was left was a table), none of the 33 ship
classes overrode anything, which is why the table replaced them outright,
and the save format does not record ship types at all — the universe is not
saved, only where the commander is docked — so the `ShipType` work turned
out to have no save-compatibility problem to solve.

- [ ] [EliteSharpLib] Equipment table to a plugin: the 34-row
      `_equipmentStock` literal at
      [EquipmentController.cs:23-58](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Views/EquipmentController.cs)
      is the last hardcoded content table. It goes the goods route —
      `IEquipmentSet` in `EliteSharp.Abstractions`, an
      `EliteSharp.Equipment.Classic` plugin with `equipment.json`, a
      loader beside `GoodsLoader`, and the app's csproj dropping the DLL
      into an `Equipment` folder. Unlike a good, an equipment item
      *acts* — `EquipmentType`
      ([EquipmentType.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Equipment/EquipmentType.cs))
      is switched on to fit an E.C.M., mount a laser, or expand the hold —
      so the enum stays as the behaviour key the game understands and the
      file states only the inert half: name, price, tech level, the
      `Show`/`CanBuy` flags and which behaviour key the row uses. A row
      naming a key the game does not know is what the loader refuses.
      Note the laser rows are a two-level list (a `+`/`-` category
      expanding into four `>` mounts), so the file needs that shape, not
      a flat array.

### 3D pipeline — modern-pipeline gaps

From the 2026-07-31 gap analysis of both games against a modern
rasterisation pipeline. The *defects* it found are in
[backlog-issues.md](backlog-issues.md); what follows is the genuinely
absent machinery. Most of it is absent **by design** — 1984 Elite and 1989
Stunt Car Racer had none of it — so each item below is a deliberate
departure from the source material, not a correction. Nothing here should
start before the issues file is clear, and the maintainer should decide
per item whether authenticity or modernity wins.

The homogeneous clip-space [LARGE] item was surveyed and split on
2026-09-10 into the six ordered parts below. The survey corrected the
original wording three times. "Neither game builds view/projection
matrices" was too strong: Elite already transforms by a float
`Matrix4x4` (`Rotmat`) and both games already share one projection stage,
`PerspectiveProjector` — what neither builds is a *projection matrix*, so
what is really missing is the `w`, not the matrices. "Frustum clipping has
to be special-cased" is half overtaken: `ViewFrustum.FromViewport`
already derives all six planes by reading the projection backwards, and
Elite culls whole ships against them
([ShipBase.IsWithinView](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Ships/ShipBase.cs));
what no stage does is clip a *face* against the side planes. And the near
plane is shared too — both games clip camera-space polygons through
`NearPlaneClip`, colours and texture coordinates included. So the gap
that is left is narrower than the original entry reads: no `w`, no NDC,
and the viewport transform folded into the projection rather than
standing as its own stage.

Two things the split has to preserve, because they are the reason the
current shape works. `PerspectiveProjector.Project` returns *pixels*
(`Centre + Focus * x / z`), not NDC, and every caller expects pixels.
And the renderers take a camera-space depth per point (`float[] depths`
on [IPolygonRenderer](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.Graphics/Rendering/IPolygonRenderer.cs)),
so the z-buffer's depth is camera z, and a move to a normalised depth
changes what is stored, not only what is computed.

Sequence the whole run after the "convert angles and trig" step of the
SCR float-physics conversion below, and build on
`SharpKind.Graphics.PerspectiveProjector`, the first small slice of the
same idea that landed 2026-08-09 (see [CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md)).
Parts 1 to 3 are library-only and change no game output; parts 4 and 5
move one game each; part 6 is what the clip space was for. Only part 6
closes the side-plane clipping entry under Won't in
[backlog-issues.md](backlog-issues.md).

- [ ] [SharpKind.Graphics] Viewport transform as its own stage: split
      [PerspectiveProjector.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.Graphics/PerspectiveProjector.cs)
      into the perspective divide (camera space to NDC) and a `Viewport`
      that maps NDC to pixels, with the existing `Project` kept as the
      composition of the two. Nothing else moves — both games keep
      calling `Project` and keep getting the same pixels — so this is a
      library change with library tests, and it is what makes an NDC
      exist to clip against later.
- [ ] [SharpKind.Graphics] A projection matrix and a `w`: build a
      `Matrix4x4` from focus, aspect, near and far, so a camera-space
      point transforms to clip space and the divide by `w` reproduces
      today's projector. Library only, no caller changes; the test that
      matters compares the matrix path against `PerspectiveProjector`
      over a sweep of points, to a tolerance, so the two are known to
      agree before anything depends on it.
- [ ] [SharpKind.Graphics] Clip in clip space: generalise
      [NearPlaneClip.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.Graphics/NearPlaneClip.cs)
      into a Sutherland-Hodgman clipper against all six clip-space
      planes (`-w <= x, y, z <= w`), keeping the two interpolating
      overloads — a corner the clipper invents still needs its colour and
      its texture coordinate. The existing camera-space entry points stay
      until the games move, so this adds a path rather than replacing
      one. Library and tests only.
- [ ] [EliteSharpLib] Move Elite onto the clip-space path: `ShipBase`
      transforms to clip space, clips there, divides, and applies the
      viewport
      ([ShipBase.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Ships/ShipBase.cs)).
      Two things resist and need deciding, not deleting: `ProjectPoint`'s
      `vec.Z <= 0` clamp, which exists only so a laser aim behind the
      camera still yields a point, and `ShowsDetail`, which reads
      `Focus * radius / z` directly as a screen size. Verify with the
      golden-trace harness, and smoke-test — this is a game loop.
- [ ] [StuntCarRacerSharpLib] Move SCR onto the clip-space path:
      `Scene3D.TransformPoint`/`ProjectPoint` and `TrackRenderer`'s
      per-triangle clip loop
      ([Scene3D.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Rendering/Scene3D.cs),
      [TrackRenderer.cs:200-232](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Rendering/TrackRenderer.cs)).
      Strictly after the float trig conversion below: the view transform
      is still a fixed-point 3x3 with a `>> Track.LogPrecision` rescale
      until then, and putting a matrix in front of it twice is wasted
      work. Verify against the physics golden traces and drive a lap.
- [ ] [SharpKind.Graphics] Depth range and guard band: with both games in
      clip space, `z / w` is a normalised depth, so the z-buffer can store
      that instead of camera z — an `IPolygonRenderer` signature change,
      since `Submit`'s `depths` are camera-space today — and a guard band
      is a side plane pushed outwards rather than a special case. Retire the `Math.Max`/`Min` span clamps in
      `DrawTriangleFilled` only if a measurement says the clip now pays
      for itself; the 2026-08-05 benchmark under Won't in
      [backlog-issues.md](backlog-issues.md) says an off-screen face is
      cheap today, and that entry is the one this part closes.
- [ ] [SharpKind.Graphics] Sub-pixel rasterisation precision: triangle edges
      snap to integer scanlines (`MathF.Ceiling`/`Floor` in
      `DrawTriangleFilled` and its variants), so geometry jitters as it
      moves rather than sliding smoothly. Fixing it means carrying
      fractional edge coverage through all four triangle routines — worth
      it only alongside anti-aliasing, which is separately absent.
- [ ] [SharpKind.Graphics] Per-frame allocation in the polygon renderers:
      `Submit` does `new Vector2[points.Length]` (plus `new float[...]` in
      the z-buffer path) for every polygon every frame, and
      `ShipBase.BuildFacePolygon` returns a fresh `Vector2[]` — which
      undoes the care `ShipBase` takes elsewhere to pool `_pointList`/
      `_cameraList` and stackalloc its clip buffers. A persistent
      vertex/index buffer is the modern shape. Bounded by the
      rasteriser-throughput Won't below: nothing here is a measured
      bottleneck at either game's frame rate, so do it for the tidiness or
      not at all. Sequence after the z-buffer defect fix, which changes
      `Submit`'s signature.

### Cleanups and small refactors

3D pipeline sharing (split 2026-07-14 from the "unify the two 3D
pipelines" [LARGE] item; a code survey found the pipelines differ more
than assumed — Elite: float `Matrix4x4` transform, `vec.Z = 1` clamp
retained for the winding test and laser aim, screen-winding cull; SCR:
fixed-point Amiga-trig view transform — so full unification is off the
table; instead extract the stages that are genuinely shareable, each
independently. Both games now clip against a shared near plane
(`NearPlaneClip`) and fill via the shared z-buffer: the spike that moved
Elite's filled ships off the painter's chain landed 2026-07-14, and real
face clipping followed, see CHANGELOG):

(none open — see the Won't section for the HUD-helper survey's outcome.)

### Stunt Car Racer conversion — features (from the retired conversion plan)

- [ ] [StuntCarRacerSharpLib] Road-line textures could sample the shared `atlas.bmp` (ptitSeb's `eRoadYellowDark` etc.) instead of the procedural strips in `Rendering/RoadTextures` — closer visual match, but the current strips already look correct; cosmetic.
Unplugged remake art screens (added 2026-07-19 after the ptitSeb parity
audit): ptitSeb ships `Bitmap/menu.png`, `racewin.png`, `racelost.png`,
`wrecked.png` and `heads.png` but its code never loads any of them
(upstream commit 7ad79f7, "More bitmaps, but not pluged in yet") — they
are the original Amiga screens, included as an unrealised intention.
The port already wired up menu.png (the track-menu frame art, commit
2a476b4, 2026-07-08); these items wire up the rest the same way:
one-time convert to `.bmp`, add to `Assets/Images` +
`AssetManifest.json`, draw scaled from the 320x200 canvas as
`TrackMenuScreen` does. Doing them means going beyond ptitSeb's code
while staying inside its assets and the Amiga's behaviour:

- [ ] [StuntCarRacerSharpLib] Race-result screens: draw `racewin.png` /
      `racelost.png` full-screen during the six-second result window and
      the GAME_OVER hold, with the existing flashing RACE WON / RACE
      LOST and "Press 'M'" text overlaid (timing at
      [StuntCarRacerMain.DrawHud](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/StuntCarRacerMain.cs)
      and `StuntCarRacer.cpp:1403-1453`) — the Amiga showed these
      screens at race end.
- [ ] [StuntCarRacerSharpLib] Wrecked screen: draw `wrecked.png` when the
      race ends with the player's car wrecked, in place of the
      race-lost art. `CarPhysics.Wrecked` now goes true at full damage
      (landed 2026-07-20, see CHANGELOG) so this can be wired up.
- [ ] [StuntCarRacerSharpLib] Opponent portraits: `heads.png` is a portrait
      sheet of the eleven opponents; show the matching portrait
      alongside the four-second "Opponent: <name>" announcement at race
      start. Needs the per-portrait tile coordinates measured from the
      sheet first (no atlas table exists upstream); assume the sheet
      order matches `opponentNames` (`Opponent_Behaviour.cpp:138-151`)
      and verify against the Amiga before committing.
Super League (split 2026-07-14 from the [LARGE] item; reference is
ptitSeb's `bSuperLeague` — toggled at `StuntCarRacer.cpp:1222`, applied
at `:1298-1308`; do the first item first, the two visual items then in
either order):

- [ ] [StuntCarRacerSharpLib] League toggle + physics constants: add an
      `IsSuperLeague` mode flag, toggle it with 'L' on `TrackMenuScreen`
      (show the current league on the menu as the remake does), and
      thread it to the physics: `CarPhysics._enginePower` 240→320 and
      the boost unit 16→12 (the "standard vs super" comments at
      [CarPhysics.cs:105-106](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Cars/CarPhysics.cs)
      already mark the spots), boost reserve Standard→Super,
      `RoadCushionValue` 0→1 (exists but nothing sets it — damage
      threshold at [CarPhysics.Motion.cs:286](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Cars/CarPhysics.Motion.cs)),
      `OpponentPhysics._enginePower` 236→314, and the opponent speed
      +32 super-league offsets: the `Opponent_Speed_Value()` port
      landed 2026-07-19, so `OpponentData.TrackSpeedValues` already
      carries the full 64-byte table with the super-league rows — the
      max-speed lookup in `OpponentPhysics.StartRace` and the
      mask/base lookups in `OpponentPhysics.SpeedValue` just need +32
      added when super league is active (the reference's path is
      `opp_track_speed_values[TrackID+32]`,
      `Opponent_Behaviour.cpp:366-367, 1299-1300`).
- [ ] [StuntCarRacerSharpLib] Super League track colours: odd/even road and
      side colours swap to `SCR_BASE_COLOUR+17/16` and `+18/+15`
      (standard: `+2/+10`, `+1/+15`; reference `Track.cpp:1360-1385`)
      — add the alternates to `ScrPalette`/`RoadTextures` and select by
      the mode flag in `TrackRenderer`. Note the reference's own
      `Track.cpp:2490` TODO says some super-league values are
      unverified; match ptitSeb, don't guess beyond it.
- [ ] [StuntCarRacerSharpLib] Super League car + cockpit visuals: opponent
      car body colours swap to `SCR_BASE_COLOUR+19/20/21` (standard
      `+9/+10/+12`, reference `Car.cpp:659-690`) in `CarMesh`, and the
      cockpit/damage overlays use the atlas's "2"-suffixed sprites
      (`eCockpitWL2`, `eCracking2`, `eHole2`, `Car.cpp:868-886`) —
      check how `HudRenderer`/`CockpitState` draw these today; if the
      port draws them procedurally rather than from `atlas.bmp` (see
      the road-line-textures cosmetic item below), this reduces to
      alternate colours.
Float physics conversion (split 2026-07-14 from the [LARGE] item; ~4,130
lines of 68000-style scaled-integer code — `CarPhysics` 2,733 lines over
four partials (incl. `.Chains`), `OpponentPhysics` 1,397 over two. Do
strictly in order:
the golden-trace harness is the safety net for everything after it.
Sequence AFTER the pending SCR correctness items (damage-wreck,
`Opponent_Speed_Value`) and the Super League physics item —
they edit the same files and their integer semantics should be captured
by the traces):

- [ ] [StuntCarRacerSharpLib.Tests] Golden-trace characterization harness:
      drive the existing integer physics with scripted `CarInput`
      sequences on two or three tracks, record the car/opponent state per
      physics tick (position, speeds, angles, damage, boost) to committed
      baseline files, and add a test that replays and compares. This is
      pure test code — no physics changes — and becomes the regression
      net for the conversion steps; the exact-integer unit-test
      assertions stay untouched until each class converts.
- [ ] [StuntCarRacerSharpLib] Convert angles and trig: replace
      `AmigaTrig`'s 16384-scaled short table and `TrigCoefficients` with
      float equivalents; decide the angle unit (keeping 0..65536 as a
      float unit is the least-churn option) and replace the
      `& (MaxAngle - 1)` bitmask wraps with a float wrap helper. Decide
      the rendering boundary here too: `Scene3D.TransformPoint` consumes
      `TrigCoefficients` and `Track.LogPrecision`
      ([Scene3D.cs:102-113](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Rendering/Scene3D.cs))
      — either convert its fixed-point view transform in the same step
      or leave it a shim that scales from the float trig. Issue #3
      ("System.Numerics for all matrix and vector maths") applies to SCR
      too: SCR's rendering layer (`Scene3D`, `TrackRenderer`,
      `BackdropRenderer`, `CarMesh`, `HudRenderer`) already uses
      `System.Numerics`/`Matrix4x4` like Elite does — `TrigCoefficients`
      and the fixed-point view transform here are the one remaining
      non-`System.Numerics` piece, and this item is where that gets
      resolved; no separate item needed. This is also the prerequisite
      for the clip-space pipeline items above.
- [ ] [StuntCarRacerSharpLib] Convert `CarPhysics` (four partials:
      [CarPhysics.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Cars/CarPhysics.cs),
      `.Motion`, `.Road`, and the crane/chain-recovery `.Chains` — the
      last landed after the original three-partial estimate and also
      uses scaled `int` state, so it's in scope too) to float: fields,
      locals, and the `>> LogPrecision` rescales. Hunt the semantics
      traps — arithmetic right-shift on negatives rounds toward
      -infinity while integer division truncates toward zero, and any
      deliberate short/int
      overflow wrap needs an explicit equivalent. Validate against the
      golden traces with tolerances; rework `CarPhysicsTests`' exact
      assertions as tolerance-based in the same session.
- [ ] [StuntCarRacerSharpLib] Convert `OpponentPhysics` (+ `.Interaction`,
      `OpponentData`) the same way, including its `_random`-driven speed
      logic (seedable, so traces stay reproducible); rework
      `OpponentPhysicsTests`; then delete the now-unused integer trig
      (`AmigaTrig`, and `TrigCoefficients` if `Scene3D` was converted)
      and drop `Track.LogPrecision` from the physics path. Full
      definition of done: race a complete lap against the opponent on
      several tracks comparing feel against the integer build.
Resizable window / widescreen (split 2026-07-14 from the [LARGE] item;
the first item is a standalone quick win delivering issues.md's "make
window resizable" for both games; the rest build on each other toward
true widescreen. A 2026-07-14 survey found `HudRenderer` already scales
from a 640x480 virtual canvas via `ScreenWidth/BaseWidth` ratios, so SCR
is closer to resolution-independence than the original item assumed.
**The 2026-07-27 multi-resolution tier decision in
[decisions.md](decisions.md) expands the two widescreen items below —
re-scope them against the 8-bit/16-bit/modern tier scheme and the
per-game `AssetManager`/config changes it requires before starting
either. Narrowed again on 2026-08-01: widescreen is a **modern-tier
concern only** — the 8-bit and 16-bit tiers are fixed-width, so the
widescreen half of these items applies to the modern tier alone. See
[decisions.md](decisions.md).**):

- [ ] [SharpKind.SDL] Resizable window with letterboxed scaling: add
      `SDL_WINDOW_RESIZABLE` in [SDLWindow.cs:23-29](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.SDL/SDLWindow.cs)
      and handle `SDL_EVENT_WINDOW_RESIZED` in the event loop, so the
      window can be any size while both games keep rendering at their
      native 512x512 / 640x400 — zero game-code changes. Most of this
      landed with `WindowScale` (2026-07-29, see CHANGELOG):
      `SDLRenderer.SetLogicalSize` already fixes the logical coordinate
      space, so what remains is the resizable flag, switching that call's
      mode from `INTEGER_SCALE` to `LETTERBOX` when the window is free to
      be any size, and deciding how the two settings interact.
- [ ] [Apps] Decide whether the render resolution needs to be
      configurable at all. Elite no longer hardcodes it: it derives
      from the tier in `SDLProgram.ResolutionFor`, which is the shape
      the tier decision wanted (the asset set and the resolution can
      never disagree). What is left is the question this item was
      really asking — whether a launch resolution/aspect should also be
      settable *independently* of the tier, in
      `EngineConfigSettings` next to `Tier`. If yes, note it exposes
      rather than fixes fixed-size assumptions, so it must follow the
      non-512x512 audit below. SCR still hardcodes its own consts
      either way.
- [ ] [StuntCarRacerSharpLib] SCR widescreen: with resolution configurable,
      make the 3D viewport render at the window aspect (SCR's
      `Scene3D.SetView` already takes width/height) and apply ptitSeb's
      cockpit widescreen treatment (`GetScreenDimensions`,
      `COCKPIT_WIDESCREEN_OFFSET` — side panels pushed out, HUD
      centred); audit `TrackMenuScreen` and the other 2D screens for
      640x400 assumptions. `HudRenderer`'s virtual-canvas scaling
      mostly survives as-is.

Elite at non-512x512 resolutions (split 2026-09-10 from the [LARGE] item).
The survey found the item mostly overtaken, and its wording wrong in four
places.

**A tier is a rendition, and a rendition declares its own size** (decided
2026-08-03, see [decisions.md](decisions.md)). `SystemTier` is gone, and
`IRendition.ScreenWidth`/`ScreenHeight`/`Scale` are the rendition's own
answers rather than facts the game holds about two known machines. So the
"8-bit/16-bit/modern tier scheme" this item asked to be re-scoped against no
longer exists as a scheme, and 512x512 is not a resolution either shipped
rendition uses: they are 320x256 and 640x512. **Elite already renders at
non-512x512 resolutions.** What is left is narrower — whether the *game*
side, which serves every rendition, still holds anything that only works at
the two sizes it has been run at.

**The widescreen half is dropped, not split.** Per
[decisions.md](decisions.md) (2026-08-01) widescreen is a modern-rendition
concern only, and that rendition does not exist; its layout is designed when
it is. The two shipped renditions are fixed-width by the same decision.

**The fourth of the 2026-07-29 trial's problems is closed too** — the 16-bit
screens were re-laid-out at 640 (the `640/512` ratio the rendition's own
comments record), so the item's pointer into
[backlog-issues.md](backlog-issues.md) is stale along with the other three,
which it already recorded as resolved.

**The audit itself came back nearly clean**, which is what makes the rest
session-sized. `ViewLayout`, `Stars`, `WorldProjection`, `ShipBase.IsWithinView`,
`ViewFrustum.FromViewport` and the planet/sun renderers all derive from the
screen size or from a radius, at any width and any aspect. Three places do
not, and two of those three are broken on the shipped renditions rather than
only on an unbuilt one - the break pattern's rings and the explosion cloud's
scatter, both now in [backlog-issues.md](backlog-issues.md), where they take
priority over everything here. What is left for the roadmap is the third,
which is only wrong at a width nothing has yet, and the harness that would
have caught all three:

- [ ] [EliteSharpLib] A rendition of an arbitrary size in the tests, so the
      claim above is checked rather than read. Today
      [FakeAbstraction.cs:17-22](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/test/EliteSharpLib.Fakes/FakeAbstraction.cs)
      hardcodes 512x512 and says it does so to match `SDLProgram` — which
      has not decided the resolution since renditions did, so the comment is
      stale as well as the size. Give the fakes a size that is neither
      square nor either shipped rendition's (say 800x480), and assert the
      derived layout, the projection and the starfield bounds against it.
      This lands first: it is what turns the two defects in
      [backlog-issues.md](backlog-issues.md), and the item below, from a
      reading of the code into a failing test.
- [ ] [EliteSharpLib] Delete the square field-of-vision test in
      [EliteDraw.DrawObject](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Graphics/EliteDraw.cs)
      (`MathF.Abs(obj.Location.X) > obj.Location.Z`, and the same for Y).
      It is a hardcoded 90 degree cone from the original, and it is the one
      surviving place that assumes the viewport. Below 90 degrees across it
      is merely redundant — `ShipBase.Draw` culls against the real frustum
      immediately afterwards — but a viewport wider than it is tall enough
      to see past 45 degrees loses ships that are genuinely on screen at the
      sides. Explosions, planets and suns return before it, so ships are the
      whole of its reach. Note in passing that `IsWithinView` passes
      `ViewportWidth`/`ViewportHeight` where `FromViewport` documents right
      and bottom *edges*; one pixel generous, so conservative, but the two
      should agree. Nothing is wrong on either shipped rendition, both being
      well under 90 degrees across, which is why this stays here while the
      other two findings moved to the issues file.
- [ ] [Repo] **Low-priority spike**: WASM build for Playwright-driven
      visual testing. Today `run-elite`/`run-scr`
      ([sdl-drive/drive.ps1](https://github.com/aphawkins/the-sharp-kind/blob/main/.claude/skills/sdl-drive/drive.ps1))
      drive the native SDL window via Win32 `PostMessage`/
      `CopyFromScreen`, since Playwright can't see a raw SDL window
      (no DOM, no meaningful UI Automation tree — same reason
      WinAppDriver wouldn't help either). `SharpKind.Graphics`/
      `SharpKind.Audio`'s Software backends and both game libs are
      already pure managed C# with no SDL dependency, so a
      browser-hosted build (new `SharpKind.Wasm` + per-game `*.Wasm` app
      targeting `browser-wasm`) could turn the app into an actual web
      page and let Playwright drive it for real: `page.goto()`,
      `page.keyboard.press()`, `page.screenshot()`, headless, no real
      desktop session needed. Needs: a DOM keyboard adapter into the
      existing `IKeyboardSink` seam (`SoftwareKeyboard` already
      consumes exactly this abstraction), a per-tick canvas blit of
      the software framebuffer (`putImageData`), and asset loading
      swapped from disk paths to HTTP fetch or embedded resources.
      Only exercises the `Software` graphics backend, not
      `Hardware`/SDL — would complement rather than replace the
      native driver. Spike/prototype only; not a commitment to
      shipping a browser build.

## Won't

- [ ] [SharpKind.Graphics] Programmable shading stages, stencil buffer, fog /
      depth cueing, anti-aliasing, post-processing and instancing
      (2026-07-31 modern-pipeline gap analysis) — all absent, all
      deliberately so. These are the parts of a modern pipeline that
      would make either game stop looking like its 1984/1989 original
      rather than merely look better, and none of them has a use case in
      a flat-shaded vector space sim or a flat-shaded polygon racer. The
      buildable subset (lighting, LOD, alpha, filtering, far-plane
      culling) is listed under Could above; this line records the rest as
      considered and declined, not overlooked.
- [ ] [Assets] SCR's 8-bit asset set — not for now (2026-07-31). The
      2026-07-28 asset-structure decision in [decisions.md](decisions.md)
      covers both games and Elite has both tiers, but SCR ships
      `SixteenBit` only: its manifest's `Tiers` is `[ "SixteenBit" ]`, so
      selecting `EightBit` throws at startup. Closing that needs a
      16-colour `palette.json`, an `AssetManifest.EightBit.json`, an 8x8
      bitmap font, and hand-authored `atlas.bmp` and `menu.bmp` — the two
      bitmaps being the blocker, since downscaling the 16-bit 1024x1024
      atlas and 320x200 menu gives filtered 16-bit imagery rather than
      8-bit-era art. SCR also has no 8-bit resolution: Elite derives its
      from `SDLProgram.ResolutionFor` (320x256 / 640x512) while SCR still
      hardcodes its own consts, so the tier would need one chosen
      (`menu.bmp` is already the Amiga's 320x200). Revisit if the art gets
      authored. The colour-budget validator applies to whatever lands, so
      nothing has to be built first.
- [ ] [Repo] Remaining code-complexity rules from issue #5 (closed
      2026-07-27, see [CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md)): `S1541`/`S3776`
      (method/cognitive complexity, 102 sites) and `S107` (parameter
      count, 20 sites) stay `severity = none` — the survivors are mostly
      ported 6502/Amiga reference methods that are inherently long, and
      `CA1502`/`CA1506` already cover the same ground as an enforced
      gate. `S109` (magic numbers, 4087 sites) likewise stays off:
      hardcoded constants (`* 256 / vec.Z`, etc.) are endemic to the
      ported algorithms. Revisit only if a specific project is scoped for
      it.
- [ ] [SharpKind.Graphics] Software rasterizer throughput (per-pixel `SetPixel`,
      insertion-sorted painter chain of ≤100 polys, no spans/SIMD) — **the
      bare rasteriser stays a Won't; the reason changed 2026-09-06.** This
      entry used to say the game "is fixed at 13.5fps by design and none of
      this is a bottleneck at that rate". That premise is gone: Elite has
      simulated *and composed* at the commander's configured rate since
      2026-08-26 ([EliteMain.Run](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/EliteMain.cs)),
      so 13.5Hz is the rate the simulation's maths is written against, not
      the rate frames are drawn at, and frames are drawn as fast as the
      setting asks. What replaces it is a measurement rather than an
      assumption: the 2026-09-06 profile behind the docking item under
      Should puts bare rasterisation at **0.42 ns/pixel** — 110 us for a
      full-screen face, the smallest ingredient of a fill by a wide margin
      and 1 % of a 60fps budget. So per-pixel `SetPixel` and the absence of
      SIMD are still not worth attacking. The painter chain is doubly moot:
      `DepthSort` defaults to `ZBuffer`, which ignores the sort entirely.
      What the same profile *does* find worth fixing — the per-pixel
      quantiser at 13-24 ns/pixel, and the per-pixel clip test at 1.14 — is
      itemised under Should, not here. Note two items elsewhere refer to
      this one: the side-plane clipping entry in
      [backlog-issues.md](backlog-issues.md) (which the same profile
      confirms) and the per-frame-allocation entry above.
- [ ] [SharpKind.Graphics] Shared text/HUD-panel helper for the two games'
      HUD code — **surveyed 2026-08-19, nothing to lift.** The item asked
      for a survey first and to lift only what both games actually use;
      the survey says that set is empty. Left/centre/right text layout is
      already shared: `DrawTextLeft`/`DrawTextCentre`/`DrawTextRight` sit
      on `IGraphics`, and both games call them directly. Above that there
      is no common ground. Elite's chrome (`DrawBorder`,
      `DrawViewHeader`, `DrawInfoMessage`, `DrawTextPretty` in
      `BaseView8Bit`/`BaseView16Bit`) is viewport-relative and per tier —
      an 8x8 character grid on one, a proportional font on the other —
      and the `IBaseView` interface already shares everything the two
      tiers have in common. SCR has no chrome at all: no border, no
      header rule, no panel rect. Its text is left-aligned at hand-placed
      offsets, against three different conventions
      (`HudRenderer` scales x and y from a 640x480 canvas,
      `TrackMenuScreen` scales uniformly from a 320-wide one,
      `TrackPreviewScreen` measures raw pixels up from the bottom). A
      helper covering both would have one caller per shape, which is the
      abstraction-for-single-use the repo's principles reject. Revisit
      only if SCR grows real panel chrome — the race-pause and
      race-result screens below are the plausible source.
- [ ] [StuntCarRacerSharpLib] The original remake's Windows-only infrastructure (DXUT registry prefs, clipboard, DirectSound path, `MessageBox` dialogs) is deliberately not ported — see the porting notes in [scr-readme.md](scr-readme.md).
- [ ] [StuntCarRacerSharpLib] ptitSeb's remaining debug/infrastructure toggles are deliberately not ported (2026-07-19 parity audit): F1 test key, F2 triangle-list/strip vertex-buffer toggle (meaningless in the software rasterizer), the 'Z' reposition test key, the disabled action-replay / Amiga-recording harness (`#ifdef NOT_USED` / `USE_AMIGA_RECORDING` even upstream), the French-keyboard digit remaps, and the SDL command-line video flags (superseded by the resizable-window/config-resolution items under Could). (The F5 stats overlay and F6/F7 per-car freezes were the exception and are now ported, 2026-08-19, see [CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md).) `Chime.wav` is unused by both code bases (kept as an asset only).
