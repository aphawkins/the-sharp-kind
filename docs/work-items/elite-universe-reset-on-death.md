# Elite: universe not fully reset after the commander dies

**Bug.** Reported by the maintainer, 2026-09-15: after a death the restarted
game does not appear to start from a clean universe, and on a *second* death
it seems to glitch back to the universe of the run before. Not yet reproduced
under a harness, so the shape of the bleed is the first thing to establish —
the report is a symptom, not a diagnosis.

**Already ruled out.** The restart path exists and runs:
`GameOverController.Update` clears `IsInitialised` after 100 ticks
([GameOverController.cs:113](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Views/GameOverController.cs)),
and the next `Simulate` re-enters `InitialiseGame`
([EliteMain.cs:481](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/EliteMain.cs)),
which calls `Universe.ClearUniverse()` — and that does null `Planet` and
`StationOrSun` and empty both `_objects` and `_shipCount`
([Universe.cs:107](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Universe.cs)).

**Where to look — state that survives a restart.** `InitialiseGame` resets
`GameState`, `Pilot`, `PlayerShip`, `Combat`, `Stars` and the universe, and
reloads the commander with `SaveFile.GetLastSave()`. It resets nothing on
`Space` beyond `IsHyperspaceReady`: `_destinationPlanet`, `_hyperDistance`,
`_universeVisible`, `HyperCountdown`, `HyperGalactic` and `HyperName` all carry
over from the dead run
([Space.cs:68-109](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Space.cs)).
`Trade` and `PlanetController` are not reset either. The restarted system comes
from the reloaded save's `DockedPlanet`, so a `GetLastSave()` that silently
leaves the previous run's seed in place would read exactly as "the previous
universe".

**Reproducing it is the work.** `HeadlessGameHarness` drives the real
composition tick by tick and can be stepped through a death, a restart and a
second death, asserting `GameStateSummary` and the universe's contents at each
boundary. A test that fails without the fix should be possible there without
touching the game loop.
