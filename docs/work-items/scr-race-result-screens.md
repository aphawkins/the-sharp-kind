# Stunt Car Racer: race-won and race-lost screens

[StuntCarRacerSharpLib] Draw `racewin.png` / `racelost.png` full-screen during
the six-second result window and the GAME_OVER hold, with the existing flashing
RACE WON / RACE LOST and "Press 'M'" text overlaid (timing at
[StuntCarRacerMain.DrawHud](https://github.com/aphawkins/the-sharp-kind/blob/main/src/scr/libs/StuntCarRacerSharpLib/StuntCarRacerMain.cs)
and `StuntCarRacer.cpp:1403-1453`). The Amiga showed these at race end.

**Background (2026-07-19 ptitSeb parity audit).** ptitSeb ships
`Bitmap/menu.png`, `racewin.png`, `racelost.png`, `wrecked.png` and `heads.png`
but never loads them (upstream 7ad79f7, "More bitmaps, but not pluged in yet") —
they are the original Amiga screens. The port wired up `menu.png` (2a476b4);
wire the rest the same way: convert once to `.bmp`, add to `Assets/Images` and
`AssetManifest.json`, draw scaled from the 320x200 canvas as `TrackMenuScreen`
does.
