# Backlog — Issues

Defects in shipped behaviour: things that are **wrong**, not things that are
missing. Prioritised with MoSCoW (per
[architecture-principles.md](architecture-principles.md)).

Both games work. The repo-wide priority is therefore to close what is broken
here before starting anything in [backlog-roadmap.md](backlog-roadmap.md) —
features, refactors and cleanups wait on this file being empty of Musts.

How to use this file:

- An item belongs here only if the current code produces a wrong result, drops
  work silently, or contradicts its own documented intent. Anything that is
  merely absent, slower than ideal, or nicer-if-restructured belongs in
  [backlog-roadmap.md](backlog-roadmap.md).
- Each item is one concern, small enough for a single focused session, and
  self-contained (paths, problem, fix direction). Items too big for one
  session are tagged **[LARGE]** — split them in a follow-up conversation
  before starting, not here.
- Definition of done: build the full solution, run the complete test suite,
  and smoke-test the affected app(s) live if the change touches shared code
  or either game loop. A defect fix should also arrive with a test that fails
  without it, wherever the seam allows one.
- When an item completes, delete it here and record it in
  [CHANGELOG.md](../CHANGELOG.md). If the fix is significant enough that a
  player or contributor would want to know about it at a glance, add a line
  for it to [release-notes.md](release-notes.md) too.

## Decisions

Maintainer decisions live in [decisions.md](decisions.md), not here — check
there before starting an item that mentions a decision.

## Must

## Should

## Could

## Won't

Investigated and deliberately not fixed. Kept so the same ground isn't
re-covered.

- [ ] [EliteSharp] Flight doesn't pause on focus-loss keys while piloting —
      **authentic, not a defect.** Pressing F5-F11 in space switches the
      screen and the universe keeps moving behind it: `SetView` is all the
      key does, and `Simulate`
      ([EliteMain.cs:183](../src/elite/libs/EliteSharpLib/EliteMain.cs))
      runs `MoveUniverse` and `UpdateInFlight` on every tick that is not
      docked, whatever screen is up. The original does the same — in space
      the BBC main loop iterates all six parts every time round, and the
      view (`QQ11`) only selects what is *drawn*; only the docked loop drops
      to parts 5 and 6
      ([elite-source-flight.asm:24376-25007](../../../markmoxon/elite-source-code-bbc-micro-disc/1-source-files/main-sources/elite-source-flight.asm)).
      So a pause here would be a deliberate deviation from the original
      rather than a fix, and the port's objective is authenticity. Noted while
      investigating 2026-09-05: the existing P/R pause could not have been
      reused for it anyway — a paused tick returns before `Compose`, so the
      chart would never be drawn. Revisit only as a roadmap item, if a
      playability deviation is wanted on purpose.
- [ ] [SharpKind.Graphics] No frustum side-plane clipping — **profiled, not
      worth fixing.** `NearPlaneClip` clips one plane; left/right/top/bottom
      are handled *after* projection by `SetClipRegion` plus the
      `Math.Max/Min` span clamps in `DrawTriangleFilled`, so a face off to
      the side still walks its clamped scanline range. Output was never
      wrong — the only question was cost, and a 2026-08-05 measurement
      (`OffScreenTriangleBenchmarks`, 512×512, i5-14600K) settles it: a
      full-height face entirely off to the left costs **1.79 µs**, a face
      straddling the right edge **1.78 µs**, against **93.7 µs** for one
      full-screen face and **536 ns** for a small on-screen one. A face off
      the top or bottom is already free (**8 ns**) — the Y clamp rejects it
      before the loop. So the worst wasted face costs 2% of a single
      legitimate full-screen fill, and it would take roughly 9,000 of them
      per frame to spend a 60fps budget; both games draw tens. Revisit only
      if a profile of real gameplay contradicts this.
- [ ] [SharpKind.Graphics] Fan triangulation of concave polygons — **not
      reachable with the current assets.** `DrawPolygonFilled` fans from
      vertex 0, which is wrong for a concave polygon. A 2026-07-31 sweep of
      every face with four or more vertices across both model sets
      (`Assets/Models/EightBit` and `SixteenBit`, faces up to 8 vertices)
      found zero concave faces, and `NearPlaneClip`'s Sutherland-Hodgman
      output of a convex input is convex, so the clipped polygons are convex
      too. Revisit only if hand-authored models are added.
- [ ] [EliteSharpLib] Buying more than 255g of Gold/Platinum doesn't work —
      authentic to the original ("broken as designed"); documented, not fixed.
- [ ] [EliteSharpLib] Elite Intro2 parade shows 29 of ~33 ship models
      ([ShipFactory.cs:98-129](../src/elite/libs/EliteSharpLib/Ships/ShipFactory.cs))
      — Cougar, Constrictor and the Lone variants are mission-specific ships,
      deliberately excluded from the parade; confirmed intentional, not a bug.
- [ ] [Assets] Selecting an 8-bit rendition in SCR does nothing — **not a
      throw, and the reason it once was has gone.** Re-checked 2026-08-09:
      SCR has one asset set rather than a rendition per machine, and
      `AssetLocator.Create(engine.Rendition)` reads it from beside the
      executable whatever the name says, using that name only to label the
      set in messages. The `Tiers` list this entry relied on was removed
      with the tier rework (see [asset-structure.md](asset-structure.md)),
      so nothing validates the name and nothing fails. What is left is
      cosmetic — the setting reads back a rendition SCR is not drawing as.
      Authoring a real 8-bit set is the item under Won't in
      [backlog-roadmap.md](backlog-roadmap.md); short of that, the honest
      fix is for SCR to say the value is ignored rather than appear to
      accept it.
- [ ] [EliteSharpLib] Equipment tech-level gating for E.C.M., Fuel Scoops,
      Escape Pod, Energy Bomb, Energy Unit, Docking Computer and Galactic
      Hyperdrive — **not a bug.** A 2026-08-04 re-derivation of the
      original's positional formula (item at `PRXS` position *N* shows once
      `planet_tech >= N - 2`, from the `EQL1` list loop at
      [elite-source-docked.asm:16371-16408](../../../markmoxon/elite-source-code-bbc-micro-disc/1-source-files/main-sources/elite-source-docked.asm))
      matches every one of these items' current `TechLevel` exactly. A
      prior sweep had mis-derived the formula and flagged E.C.M. as
      one-level-too-strict; that was wrong. Mining Laser and Military
      Laser were genuinely off by the same mis-derivation's mirror error
      (too lenient) and have been fixed — see CHANGELOG.
- [ ] [EliteSharpLib] Docking-bay roll-alignment threshold
      (`FlyToDockingBay`, `Pilot.cs:341`, `MathF.Abs(dir) >= 0.9166f`) —
      **not a bug.** A prior sweep computed the original's `CPX #66`
      threshold as `33/96 ≈ 0.344`, dividing by the *unit vector's* own
      magnitude (96). But `TAS4`'s dot product (like the unrelated `CNT`
      dot product used elsewhere in `TACTICS`) is right-shifted by 8 as
      part of the multiply, capping its own high byte at `96*96/256 = 36`,
      not 96 — the same-shaped error as the equipment tech-level item
      above. The correct normalization is `33/36 ≈ 0.9167`, which is
      exactly `0.9166f` — already in the port, already correct, and
      already symmetric (`MathF.Abs`) as the original's "positive or
      negative, just needs to be parallel" comment describes.
- [ ] [EliteSharpLib] Possible missing 80-point splash damage when a
      missile self-destructs near the player — original `TA35`/`TA87`
      ([elite-source-flight.asm:9219-9245](../../../markmoxon/elite-source-code-bbc-micro-disc/1-source-files/main-sources/elite-source-flight.asm))
      applies 80 damage to the player whenever a missile is destroyed *for
      any reason* while near the player, separate from the 250-damage
      direct-hit case. `TryGetMissileHeading` (`Conflict/Combat.cs:1147-1194`) has
      no such path. **2026-08-04: blocked, not abandoned.** The mechanic is
      real and clearly missing, but TA35's own proximity test (`x_lo OR
      y_lo OR z_lo` of the missile's position) only examines each axis's
      *low* byte, ignoring the high byte/sign entirely — confirmed via
      `MAS4`'s own byte-offset convention (`INWK+1/+4/+7` are the *high*
      bytes), so this isn't a misreading of which bytes are which. Read
      literally that's satisfied on roughly 1-in-256-cubed ticks, which
      can't be what "just got destroyed near us" means for ordinary play.
      Left unfixed rather than guess a distance convention (e.g. reusing
      the direct-hit box a few lines above) that isn't actually what the
      original does. Revisit if someone can resolve what this byte-level
      check is really testing.
