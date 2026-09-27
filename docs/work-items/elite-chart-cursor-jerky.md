# Elite: chart cursor is jerky and ignores the joystick

**Bug.** Reported by the maintainer, 2026-09-27. Moving the cross-hairs on the
galactic and short-range charts is jerky rather than smooth.

Not yet diagnosed. Where to look: `MoveCross` in
[GalacticChartController.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Views/GalacticChartController.cs)
and
[ShortRangeChartController.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Views/ShortRangeChartController.cs)
steps the cross by a fixed amount (Y doubled) each time `HandleInput` sees a key
held via `IsPressed`, and sets `_crossTimer = 5`. Establish whether
`HandleInput` runs per simulation tick (13.5Hz) while frames compose at the
configured `Fps` — which would move the cross in visible steps — or whether the
unevenness comes from key-repeat or the timer. Movement should be smooth at any
frame rate while keeping the original's speed.

**Also: the joystick does nothing on the charts.** Reported by the maintainer,
2026-09-27; merged here because both are the same cross-hair movement code.
Both chart controllers read only `_keyboard.IsPressed` (S/X/,/. and the arrows)
and never go through `EliteControlMap`, so a stick's pitch and roll axes, and
the actions they hold, never move the cross. The fix should move the cross
from the mapped `PitchUp`/`PitchDown`/`RollLeft`/`RollRight` actions (or their
axes' deflection) so every bound device works. It should also fix the jerkiness,
so movement is smooth for keys and stick alike.
