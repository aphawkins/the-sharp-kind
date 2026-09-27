# Stunt Car Racer: widescreen (Modern tier only)

[StuntCarRacerSharpLib] **Blocked by**
[render-resolution-decision.md](render-resolution-decision.md). Widescreen is a
Modern-tier concern only (2026-08-01, [decisions.md](../decisions.md)).

With resolution configurable, make the 3D viewport render at the window aspect
(`Scene3D.SetView` already takes width/height) and apply ptitSeb's cockpit
widescreen treatment (`GetScreenDimensions`, `COCKPIT_WIDESCREEN_OFFSET` — side
panels pushed out, HUD centred); audit `TrackMenuScreen` and the other 2D
screens for 640x400 assumptions. `HudRenderer`'s 640x480 virtual-canvas scaling
mostly survives as-is.
