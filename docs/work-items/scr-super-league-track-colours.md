# Stunt Car Racer: Super League track colours

[StuntCarRacerSharpLib] After [scr-super-league-toggle.md](scr-super-league-toggle.md).

Odd/even road and side colours swap to `SCR_BASE_COLOUR+17/16` and `+18/+15`
(standard `+2/+10`, `+1/+15`; reference `Track.cpp:1360-1385`). Add the
alternates to `ScrPalette`/`RoadTextures` and select by the mode flag in
`TrackRenderer`. The reference's own `Track.cpp:2490` TODO says some values are
unverified; match ptitSeb, don't guess beyond it.
