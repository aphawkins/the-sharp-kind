# Performance notes

Evidence from profiling, kept so the next performance item starts from here
rather than re-profiling from nothing. Work items live in
[backlog-roadmap.md](backlog-roadmap.md).

## Station rendering while docking (2026-09-06, updated 2026-09-12)

Profiled on an i5-14600K at 512x512 after a frame-rate drop was noticed while
docking, as a Coriolis fills the view. Every item it produced has landed. The
benchmarks are `StationBenchmarks`/`GraphicsPreset` (a Coriolis through the
real pipeline — `RenderStart`, `DrawObject`, `RenderEnd`) and
`DepthFillBenchmarks`/`QuantiserBenchmarks` (one full-screen depth-tested
face, and the quantisers alone); re-run them to check any of this.

**The settings decide whether there is a problem at all.** The presets that
ask the quantiser per pixel are the Gouraud pair. Figures are 2026-09-12; in
brackets, the same preset on 2026-09-06:

| Preset          | Z=1000        | Z=250             | vs default |
|-----------------|--------------:|------------------:|-----------:|
| Unlit/Nearest   | 113 us (117)  |   807 us (  804)  |       1.0x |
| Lambert/Ordered | 118 us (567)  |   801 us (4 841)  |       1.0x |
| Gouraud/Nearest | 212 us (519)  | 1 468 us (4 028)  |       1.8x |
| Gouraud/Ordered | 355 us (771)  | 2 624 us (6 397)  |       3.3x |

On the defaults the worst frame spends 807 us on the station — 5 % of the
budget.

**Where the per-pixel time goes** (one full-screen face, 262 144 pixels,
2026-09-06; the right column is what the row adds over the one above):

| Ingredient                         |      Total | Adds per pixel |
|------------------------------------|-----------:|---------------:|
| `ClearDepth` (per frame, no draw)  |    24.8 us |              - |
| Rasterise only, no depth buffer    |   109.8 us |        0.42 ns |
| + per-pixel depth test             |   500.9 us |        1.49 ns |

The later rows of the original table are gone: the clip test (fills clamp to
the clip rectangle since 2026-09-12), the per-pixel dither (resolved once per
face since 2026-09-10; a flat fill is ~667-680 us), and the Gouraud span
(**1 299 us**, 2.4 ns a pixel over a flat fill). Resolving the quantiser to a
concrete type at the top of the Gouraud span saves only 0.18 ns a pixel, which
does not pay for a copy of the pixel loop per quantiser.

**Quantisers alone** (2026-09-10): `ChannelGridQuantiser` **0.1 ns** a call
(256-entry table), `PaletteQuantiser` **15.5 ns**, and the
`OrderedDitherQuantiser` wrapper a further **4.1-4.9 ns**. Resolving a flat
face's sixteen dithered answers once and indexing per pixel costs **0.15 ns**.

**Settled by this profile:**

- **Off-screen faces are not the cost.** The fills clamp before the loop, so an
  off-screen pixel is never visited: from Z=500 to Z=250 the station's
  unclipped area grows 4x while cost grows 1.8x. Agrees with the side-plane
  clipping entry in [work-items/wont-do.md](work-items/wont-do.md).
- **`ClearDepth` costs 25 us per frame** — 0.15 % of a 60fps budget, so
  deliberately not an item.
