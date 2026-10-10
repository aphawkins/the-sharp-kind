# Listen to the own audio stack

[SharpKind.Audio] The own audio stack ([audio-stack-plan.md](../audio-stack-plan.md))
is built and tested, but nobody has heard it yet. These checks need a listener:

- **Elite 16-bit:** both tunes (the intro's theme, the docking computer's Blue
  Danube) against the MeltySynth build before the change, on both backends.
  The levels match by measurement; the instruments have not been compared.
- **Elite 8-bit:** `elite.sid` song 1 should be the theme and song 2 the Blue
  Danube. That was read from the notes each plays, not heard. If they are the
  other way round, swap them in
  `EliteSharp.Renditions.EightBit/Assets/AssetManifest.json`.
- **Elite 8-bit effects:** laser, hit, explosion, beep, boop, ECM and
  hyperspace, against C64 Elite under VICE (disk images in the C64 source's
  `5-compiled-game-disks`, see `.claude/skills/vice-drive/SKILL.md`).
- **Stunt Car Racer:** its effects and its pitched engine loop sound as before.

`ShippedSidTuneTests` (explicit) writes every SID song's first 30 s to the temp
folder for listening. Record what was found in [decisions.md](../decisions.md).
