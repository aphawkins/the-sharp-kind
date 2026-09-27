# Stunt Car Racer: Super League toggle and physics constants

[StuntCarRacerSharpLib] Super League part 1 (reference ptitSeb's `bSuperLeague`,
toggled at `StuntCarRacer.cpp:1222`, applied at `:1298-1308`). Do this before
the two visual items.

Add an `IsSuperLeague` mode flag, toggled with 'L' on `TrackMenuScreen` (show
the current league on the menu as the remake does), and thread it to the
physics:

- `CarPhysics._enginePower` 240→320 and the boost unit 16→12 (the "standard vs
  super" comments at
  [CarPhysics.cs:105-106](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Cars/CarPhysics.cs)
  mark the spots); boost reserve Standard→Super.
- `RoadCushionValue` 0→1 (exists but nothing sets it — damage threshold at
  [CarPhysics.Motion.cs:286](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/Cars/CarPhysics.Motion.cs)).
- `OpponentPhysics._enginePower` 236→314.
- Opponent speed +32 offsets: `OpponentData.TrackSpeedValues` already carries
  the 64-byte table; the max-speed lookup in `OpponentPhysics.StartRace` and the
  mask/base lookups in `OpponentPhysics.SpeedValue` add +32 when active
  (`opp_track_speed_values[TrackID+32]`, `Opponent_Behaviour.cpp:366-367,
  1299-1300`).
