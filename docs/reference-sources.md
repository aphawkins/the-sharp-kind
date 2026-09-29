# Reference Sources — The Sharp Kind

Both games are ports, and when a port's behaviour is in question the answer is
in the source it was ported from. These are the checkouts to read, and which
one wins when they disagree.

This is a developer's document. Players want
[elite-readme.md](elite-readme.md) and [scr-readme.md](scr-readme.md).

## Why this file exists

On 2026-08-04 a missile defect in Elite was diagnosed twice from measurement
alone, without opening the sources. Both answers were wrong, and both were
convincing: an inverted sign in the tracking code (the port matched both
references exactly), and missiles needing a sharper turn than ships (`TACTICS`
sets `RAT = 3` for everything it steers, and only `DOCKIT` changes it). The
real defect was a distance test where the original tests each axis - see
[decisions.md](decisions.md).

A port measured only against itself produces confident wrong answers. Read the
reference before proposing a change to gameplay maths; do not tune constants
by feel.

## Elite

- **[markmoxon/elite-source-code-bbc-micro-disc](https://github.com/markmoxon/elite-source-code-bbc-micro-disc)**
  - the annotated 6502 of the BBC Micro disc version. **Golden**: where it and
  anything else disagree, it wins. The flight and combat maths is in
  `1-source-files/main-sources/elite-source-flight.asm`, and the commentary
  names the constants (`RAT`, `RAT2`, `CNT2`) the C version only gives as
  numbers.
- **[fesh0r/newkind](https://github.com/fesh0r/newkind)** - C.J.Pinder's C
  rewrite, which this port was converted from and whose structure it still
  follows function for function. Nearer to hand than the assembly, but a
  rewrite rather than a transcription: read it first, then confirm against the
  BBC source before treating anything as settled.

## Bubble Bobble

- **[zaidka/rebb64](https://github.com/zaidka/rebb64)** - a byte-exact
  disassembly of the C64 game that rebuilds to the original binary. **Golden**.
  Its comments and labels are often wrong, so a routine is named from what it
  does. The port's tests run the built image on the `SharpKind.Emulation` CPU as
  an oracle, and compare the port's memory with the game's.
- **The HVSC `bubble-bobble.sid`** - the game's own sound driver, played on the
  same emulation.

## Stunt Car Racer

- **[ptitSeb/stuntcarremake](https://github.com/ptitSeb/stuntcarremake)** -
  C++, DirectX9/DXUT + SDL2, a maintained fork of the one below. **Golden**:
  where the two disagree, it wins.
- **[fluffyfreak/stuntcarracer](https://github.com/fluffyfreak/stuntcarracer)**
  - the conversion this port started from, before the switch to the fork above.
  Worth reading where the fork has moved on, but not authority.

Both render the track through the Direct3D z-buffer (`DrawTrack` in
`Track.cpp`), which is what settled this port's move off a painter's algorithm.

The bar for SCR is behavioural fidelity rather than bit-exact replication (see
the porting notes in [scr-readme.md](scr-readme.md)), so the references settle
what the original *does*, not what every number in it was.
