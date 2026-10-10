# Elite 8-bit: the ECM sound stops when the ECM does

[EliteSharpLib] C64 Elite's `ECMOF` stops the ECM's sound with `NOISEOFF`,
which sets that voice's counter to 1 so it ends on the next frame. The port
plays `sfxecm` and lets it run its full 255 frames, about five seconds, however
short the ECM's burst.

Fix direction: an `ISound` call (or a sound key) that ends one effect, and a
`SidEffectEngine` port of `NOISEOFF`.
