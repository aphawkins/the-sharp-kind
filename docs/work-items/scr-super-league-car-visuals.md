# Stunt Car Racer: Super League car and cockpit visuals

[StuntCarRacerSharpLib] After [scr-super-league-toggle.md](scr-super-league-toggle.md).

Opponent car body colours swap to `SCR_BASE_COLOUR+19/20/21` (standard
`+9/+10/+12`, reference `Car.cpp:659-690`) in `CarMesh`, and the cockpit/damage
overlays use the "2"-suffixed sprites (`eCockpitWL2`, `eCracking2`, `eHole2`,
`Car.cpp:868-886`). Check how `HudRenderer`/`CockpitState` draw these today; if
procedurally rather than from sprite art, this reduces to alternate colours.
