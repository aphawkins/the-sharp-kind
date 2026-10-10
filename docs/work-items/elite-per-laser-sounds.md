# Elite 8-bit: a sound for each laser

[EliteSharpLib] C64 Elite's `MA68` plays a different effect for each laser:
`sfxplas` (pulse), `sfxblas` (beam), `sfxalas` (military) and `sfxmlas`
(mining). The port has one `SoundEffect.Pulse` key, so every laser sounds like
the pulse laser. `elite.sidfx` already holds all four effects.

Fix direction: give the game one key per laser type, chosen where it fires,
and add the matching sounds to `tools/elite/export-c64-sfx.py` (and a sample
for each on the 16-bit tier).
