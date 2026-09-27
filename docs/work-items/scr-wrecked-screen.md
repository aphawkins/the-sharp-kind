# Stunt Car Racer: wrecked screen

[StuntCarRacerSharpLib] Draw `wrecked.png` when the race ends with the player's
car wrecked, in place of the race-lost art. `CarPhysics.Wrecked` goes true at
full damage (2026-07-20), so this can be wired up.

**Background (2026-07-19 ptitSeb parity audit).** ptitSeb ships
`Bitmap/menu.png`, `racewin.png`, `racelost.png`, `wrecked.png` and `heads.png`
but never loads them (upstream 7ad79f7, "More bitmaps, but not pluged in yet") —
they are the original Amiga screens. The port wired up `menu.png` (2a476b4);
wire the rest the same way: convert once to `.bmp`, add to `Assets/Images` and
`AssetManifest.json`, draw scaled from the 320x200 canvas as `TrackMenuScreen`
does.
