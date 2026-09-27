# Stunt Car Racer: opponent portraits at race start

[StuntCarRacerSharpLib] `heads.png` is a portrait sheet of the eleven opponents;
show the matching portrait alongside the four-second "Opponent: <name>"
announcement. Measure per-portrait tile coordinates from the sheet first (no
table exists upstream); assume sheet order matches `opponentNames`
(`Opponent_Behaviour.cpp:138-151`) and verify against the Amiga before
committing.

**Background (2026-07-19 ptitSeb parity audit).** ptitSeb ships
`Bitmap/menu.png`, `racewin.png`, `racelost.png`, `wrecked.png` and `heads.png`
but never loads them (upstream 7ad79f7, "More bitmaps, but not pluged in yet") —
they are the original Amiga screens. The port wired up `menu.png` (2a476b4);
wire the rest the same way: convert once to `.bmp`, add to `Assets/Images` and
`AssetManifest.json`, draw scaled from the 320x200 canvas as `TrackMenuScreen`
does.
