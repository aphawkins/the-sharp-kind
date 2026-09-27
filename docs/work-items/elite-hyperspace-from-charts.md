# Elite: allow hyperspace to be started from the galaxy charts

**Bug.** Reported by the maintainer, 2026-09-27. Hyperspace should be
triggerable from both the galactic chart and the short-range chart, as in the
original, where the hyperspace key works from any screen in flight.

Today `EliteAction.Hyperspace` is only read by `PilotController`
(`WantsHyperspace`,
[PilotController.cs:175](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Views/PilotController.cs)),
so it only works from the flight views. `GalacticChartController.HandleInput`
and `ShortRangeChartController.HandleInput` handle cursor, find and O/D keys but
not hyperspace.

Direction: route the hyperspace action (and galactic hyperspace, `Ctrl-H`) to
`Space.StartHyperspace`/`StartGalacticHyperspace` from both chart controllers
when in flight, not when docked, sharing the check with `PilotController`
rather than copying it. The countdown should keep running while the chart is
shown. Test through `HeadlessGameHarness`.
