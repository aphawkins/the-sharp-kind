# Elite 8-bit: explosions quieter with distance

[EliteSharpLib] C64 Elite's `EXNO` and `EXNO2` set the explosion's sustain
volume from 11 to 15 by the distance of the ship hit (`z_hi`). The port always
plays the nearest, loudest one (`$F3`), because `ISound` has no distance and a
SID effect ignores the volume it is given.

Fix direction: pass the distance band through as five sound keys (or a
volume the SID path maps to the sustain nibble), following the two routines.
