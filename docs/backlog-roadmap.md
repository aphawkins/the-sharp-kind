# Backlog — The Sharp Kind

Every open work item, in priority order. Each line links to a file in
[work-items/](work-items/) holding the detail.

## Rules

- **Priority.** MoSCoW (per [architecture-principles.md](architecture-principles.md)).
  Bugs are always **Must**. Within each section the order is: bugs, then
  SharpKind (shared libraries and repo), then Elite, then Bubble Bobble, then
  Stunt Car Racer. Work top to bottom.
- **Never skip an item.** Take the first open item, and finish it before moving
  on:
  - **[LARGE]** — too big for one session. Break it down into session-sized
    items (new work-item files, listed in its place) *before* anything below
    it starts.
  - **[DECISION]** — needs a decision from the maintainer. Raise it and get the
    decision; don't pass over it to the next item. Record the outcome in
    [decisions.md](decisions.md) and update the work item.
  - **Blocked by** another item — do the blocker first, then come back.
- **One concern per item**, small enough for a single focused session. Its file
  is self-contained: paths, problem, fix direction. Line numbers may have
  drifted; verify before editing.
- **Definition of done.** Build the full solution, run the complete test suite,
  and smoke-test the affected app(s) live if the change touches shared code or
  a game loop. A bug fix arrives with a test that fails without it, wherever
  the seam allows one.
- **On completion.** Delete the line here *and* its work-item file, and record
  it in [CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md).
  If a player or contributor would want to know at a glance, add a line to
  [release-notes.md](release-notes.md) too.
- **Adding an item.** A short title (a dozen words at most) here, and the
  detail in a new `work-items/<area>-<slug>.md`. A bug is anything where the
  code produces a wrong result, drops work silently, or contradicts its own
  documented intent.
- Maintainer decisions live in [decisions.md](decisions.md). Performance
  evidence lives in [performance-notes.md](performance-notes.md). Things
  deliberately not done live in [work-items/wont-do.md](work-items/wont-do.md).

## Must

### Bugs

- [ ] [Elite: docking rings don't reach the viewport's side edges](work-items/elite-docking-rings-extent.md)
- [ ] [Elite: docking rings should be octagons/hexadecagons per rendition](work-items/elite-docking-rings-shape.md)
- [ ] [Elite: universe not fully reset after the commander dies](work-items/elite-universe-reset-on-death.md)
- [ ] [Elite 8-bit: move the hyperspace countdown two columns right](work-items/elite-8bit-hyperspace-countdown-position.md)
- [ ] [Elite: allow hyperspace from the galactic and short-range charts](work-items/elite-hyperspace-from-charts.md)
- [ ] [Elite: galaxy chart cursor movement is jerky](work-items/elite-chart-cursor-jerky.md)
- [ ] [Elite 8-bit: halve the HUD art and centre the scanner](work-items/elite-8bit-hud-scanner-centre.md) — blocked by the maintainer's new `hud.bmp`

### Bubble Bobble

Steps from section 5 of [bb-port-plan.md](bb-port-plan.md), in order.

- [ ] [2d: draw the special bubbles](work-items/bb-2d-draw-specials.md)
- [ ] [3d: lives and respawn](work-items/bb-3d-lives-respawn.md)
- [ ] [3e: join and game over](work-items/bb-3e-join-game-over.md)
- [ ] [4: level flow](work-items/bb-4-level-flow.md)
- [ ] [5: special bubbles and EXTEND](work-items/bb-5-special-bubbles-extend.md)
- [ ] [6: the other special-item effects](work-items/bb-6-special-item-effects.md)
- [ ] [7: super mode, bonus rounds and level 100](work-items/bb-7-super-mode-bonus-level-100.md)
- [ ] [8: sound effects](work-items/bb-8-sound-effects.md)
- [ ] [9: front end](work-items/bb-9-front-end.md)
- [ ] [10: join the repo](work-items/bb-10-join-the-repo.md)

## Should

### SharpKind

- [ ] [Own audio stack: WAV, MIDI/SoundFont2 and C64 SID](work-items/audio-own-stack.md)

## Could

### SharpKind

- [ ] [Shared debug overlays for both games](work-items/shared-debug-overlays.md)
- [ ] [Clip space 1/6: viewport transform as its own stage](work-items/graphics-viewport-stage.md)
- [ ] [Clip space 2/6: projection matrix and a w](work-items/graphics-projection-matrix.md)
- [ ] [Clip space 3/6: clip against all six clip-space planes](work-items/graphics-clip-space-clipper.md)
- [ ] [Clip space 6/6: normalised depth and guard band](work-items/graphics-depth-range-guard-band.md) — blocked by clip space 4/6 and 5/6
- [ ] [Remove per-frame allocations in the polygon renderers](work-items/graphics-renderer-allocations.md)
- [ ] **[DECISION]** [Sub-pixel rasterisation precision](work-items/graphics-subpixel-precision.md)
- [ ] [Resizable window with letterboxed scaling](work-items/sdl-resizable-letterbox.md)
- [ ] **[LARGE]** [Resizeable window and native fullscreen for Modern](work-items/sdl-resizable-fullscreen.md) — blocked by the Modern rendition
- [ ] **[DECISION]** [Is render resolution configurable independently of rendition?](work-items/render-resolution-decision.md)
- [ ] [Spike: WASM build for Playwright visual testing](work-items/wasm-playwright-spike.md)

### Elite

- [ ] [Add the Modern rendition (1280x720, 32-bit colour)](work-items/elite-modern-rendition.md)
- [ ] [Log the field of view at startup](work-items/elite-log-field-of-view.md)
- [ ] [Move the equipment table to a plugin](work-items/elite-equipment-plugin.md)
- [ ] [Test with a rendition of arbitrary size](work-items/elite-arbitrary-size-test-rendition.md)
- [ ] [Delete the square field-of-vision test](work-items/elite-remove-square-fov-test.md)
- [ ] **[DECISION]** [Clip space 4/6: move ship rendering onto the clip-space path](work-items/elite-clip-space-path.md)

### Stunt Car Racer

- [ ] [Read controls from a file](work-items/scr-controls-file.md)
- [ ] [Split the sprite atlas into one image per sprite](work-items/scr-atlas-split.md)
- [ ] [Road-line textures from the sprite art](work-items/scr-road-line-textures.md)
- [ ] [Race-won and race-lost screens](work-items/scr-race-result-screens.md)
- [ ] [Wrecked screen](work-items/scr-wrecked-screen.md)
- [ ] [Opponent portraits at race start](work-items/scr-opponent-portraits.md)
- [ ] [Super League toggle and physics constants](work-items/scr-super-league-toggle.md)
- [ ] [Super League track colours](work-items/scr-super-league-track-colours.md)
- [ ] [Super League car and cockpit visuals](work-items/scr-super-league-car-visuals.md)
- [ ] [Float physics 1/4: golden-trace harness](work-items/scr-golden-trace-harness.md)
- [ ] [Float physics 2/4: angles and trig](work-items/scr-float-trig.md)
- [ ] [Float physics 3/4: CarPhysics](work-items/scr-float-car-physics.md)
- [ ] [Float physics 4/4: OpponentPhysics](work-items/scr-float-opponent-physics.md)
- [ ] [Clip space 5/6: move rendering onto the clip-space path](work-items/scr-clip-space-path.md)
- [ ] [Widescreen (Modern tier only)](work-items/scr-widescreen.md) — blocked by the render-resolution decision

## Won't

See [work-items/wont-do.md](work-items/wont-do.md).
