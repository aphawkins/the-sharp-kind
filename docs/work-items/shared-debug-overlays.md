# Shared debug overlays for both games

[SharpKind.Abstraction] Each game grew its own developer furniture with
opposite halves of the same feature: Elite shows a frame rate and no stats,
SCR shows stats and no frame rate. Make the overlays one thing both games draw,
most likely as a fourth layer — always closest, full screen, drawn after the
HUD, appended to each game's existing `LayerRunner`.

**Now.** Elite counts presents into `_framesDrawn` (`Stopwatch` timestamps
trimmed to the last second) in `Draw()`, and draws through `_baseView.DrawFps`
inside its HUD layer
([EliteMain.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/EliteMain.cs)).
SCR draws `Race.DrawStats` — frame gap and the F6/F7 per-car freezes — from
`StuntCarRacerMain.Draw` after the current screen, in no layer
([Race.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Race.cs)).
Elite's switch is the `engine.graphics.showFps` setting; SCR's is F5.

**Seams.** `ShowFps` is on the shared
[GraphicsConfigSettings](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.Abstraction/Config/GraphicsConfigSettings.cs)
and only Elite reads it. Frame timing belongs in `GameHost.Run`
([GameHost.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.Abstraction/GameHost.cs)),
the one place that knows when a frame is presented.

**Settle, don't assume:** whether text is drawn by the shared layer through
`IGraphics.DrawTextLeft` or handed back to each game (Elite uses its
rendition's `IBaseView` fonts, SCR `StuntCarRacerMain.SmallFont`), and whether
the two switches become one. This is screen-absolute developer furniture, so
the declined shared-HUD-chrome survey in [wont-do.md](wont-do.md) does not
apply.

**Acceptance.** Both overlays default to off, so every golden-frame baseline in
both games should stay byte-identical; a moved baseline means the default
leaked.
