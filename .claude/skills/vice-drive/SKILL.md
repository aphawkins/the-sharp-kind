---
description: Drive the C64 emulator VICE to capture ground truth from rebb64 (the Bubble Bobble reference) - launch it, verify the game really loaded, send joystick input, and read RAM a frame at a time. Use when porting Bubble Bobble needs a byte trace from the real game, when a reading of the 6502 has to be checked against what the machine does, or when asked to run, capture, or verify anything under VICE.
---

The Bubble Bobble port is verified against the C64 conversion running under
VICE. Reading the 6502 is not enough on its own: `docs/bb-port-plan.md`
records several routines whose comments point the wrong way and at least one
mover that was only found by watching the machine. This skill is how to get
that ground truth without rediscovering the protocol every time.

**The whole difficulty is that a failed capture looks exactly like a good
one.** Every trap below yields a full table of plausible numbers from a machine
that is frozen, or not running the game, or answering an earlier question. None
of them announces itself. The guards in `scripts/monitor.py` exist for that
reason - use them rather than reading raw bytes and hoping.

## Setup

VICE 3.10 (GTK3, win64) is installed via winget and is **not on PATH**:

```
C:\Users\aph82\AppData\Local\Microsoft\WinGet\Packages\VICE-Team.VICE.GTK3_Microsoft.Winget.Source_8wekyb3d8bbwe\GTK3VICE-3.10-win64\bin\x64sc.exe
```

The reference PRG is built from the rebb64 checkout at
`C:\code\github\zaidka\rebb64` with `make` in `build/`, giving
`build/rebb64.prg`. `make verify` proves the image is byte-exact.

Joystick input needs `%APPDATA%/vice/vice.ini` to have, under `[C64SC]`:
`JoyDevice2=2`, `KeySetEnable=1`, `SaveResourcesOnExit=0`, and `KeySet1`
North/South/West/East/Fire = `119/115/97/100/32` (W, S, A, D, space). That is
already configured on this machine.

## Launch

Run it in the background - it is a GUI app and will not return:

```bash
"$VICE" -binarymonitor -binarymonitoraddress ip4://127.0.0.1:6502 -autostart /c/code/github/zaidka/rebb64/build/rebb64.prg
```

Then **wait about 30 seconds before sending any monitor traffic at all.**
Autostart runs in warp and the C64 has to boot and load first. Connecting early
halts the machine mid-boot and the game never loads.

Relaunch VICE for every run. Kill a stuck one with `taskkill //F //IM x64sc.exe`.

## Check it worked, always

```bash
python .claude/skills/vice-drive/scripts/monitor.py selftest
```

Three answers, and all three matter:

- **game loaded** - `$0A4E` holds `20 f2 0c`, the game loop's `jsr $0CF2`. If
  not, nothing else read is worth anything.
- **machine running** - `$08` advances at about 50 a second. This is the
  liveness guard.
- **game loop turning** - `$A9FA` counts down. Zero means the title screen, so
  the game is up but no level is running.

Never skip this. A frozen machine answers every read with the same bytes.

## Getting into a level

Autostart leaves the game on its title screen for ever. It reads a joystick,
not the keyboard buffer, so `-keybuf` cannot start it, and the game samples key
*state* - a single keystroke is usually missed.

```powershell
.claude/skills/vice-drive/scripts/press.ps1 -Keys fire -Holds 5
```

The window must be foreground; the script raises it. Check with `selftest`
that the pass counter is now moving before capturing anything.

**This is the one step not yet proved to work.** As of 2026-09-19 the presses
land - the script finds and raises the window - but the game stays on its title
screen and `$A9FA` never moves, so no capture that needs a live level has been
taken. Everything else in this skill is verified: launch, load, liveness and
the reads.

Ruled out already, so do not spend the time again:

- the title screen does read the fire button. `game-init-early.s` around $255
  reads `CIA1_PRA` for port 2 and `CIA1_PRB` for port 1, and scans the keyboard
  besides, so it is not a case of the wrong button or the wrong port.
- the keyset is selected correctly. `JoyDevice2=2` is keyset A, which is what
  the `KeySet1*` entries configure.

So the fault is in delivery rather than in the choice of key. Worth trying next:
whether `keybd_event` reaches VICE's GTK input at all (test with a key that has
a visible effect, or watch `$DC00` while a key is held), a much longer hold, and
whether the title sequence has to finish before it accepts anything.

## Reading state

```bash
python .claude/skills/vice-drive/scripts/monitor.py passes 4
python .claude/skills/vice-drive/scripts/monitor.py watch 4 A9FA AA0C AA1E
```

Or use `Monitor` as a library - see the module docstring.

Useful addresses, all from `rebb64/src/master.s` and the port's own tables:

| Address | What |
|---|---|
| `$08` | `ENDCHR`, the IRQ frame counter, +1 at 50Hz |
| `$CA,x` | entity type, eighteen slots |
| `$A9B2,x` | AI state counter |
| `$A9FA,x` | the per-slot clock `$13BE` decrements every game loop pass |
| `$AA0C,x` / `$AA1E,x` | entity X and Y |
| `$B2,y` / `$BA,y` / `$C2,y` | state, X, Y - eight slots, players 0 and 1 |

## The four traps

Each of these has cost a session. They are the reason this skill exists.

**Every command halts the emulator - every command, not just a memory read.**
Setting a checkpoint halts it too. The machine stays stopped until an explicit
`MON_CMD_EXIT` (0xaa). `Monitor.resume()` is that, and nothing may be sent while
the game needs to run. A run that connected and set three checkpoints froze the
C64 mid-boot; every counter then read zero, which is indistinguishable from
"the game loop is not running".

**Every reply must be consumed, including EXIT's.** Leaving one in the socket
desynchronises the stream - the next read resynchronises on a stray `STX` inside
the unread packet and returns bytes that still parse into plausible numbers.
`Monitor.request` matches each reply to the request id that asked for it;
unsolicited replies use `0xFFFFFFFF`.

**Non-stopping checkpoints are not a way to count hits.** VICE emits a reply per
hit even with `stop_when_hit` clear, which floods the socket until it blocks and
the emulator stalls. Count with RAM the game already ticks instead: `$13BE`
decrements `$A9FA` for all eighteen slots every pass with no gating, so any slot
is a free pass counter mod 256.

**The game owns zero page - do not use KERNAL addresses as a guard.** Reading
`$A0-$A2` as a jiffy clock gave `010101` and then `040404`, which looked like a
desynchronised stream and was not: rebb64 takes over the machine and those bytes
are its own data. `$08` is the frame counter, and `master.s` says so. A guard
that reads the wrong address will condemn a working capture.

If stepping rather than sampling, there is a fifth: **the frame anchor must not
be `$E498`.** `wait_one_frame` is a spin loop, so an exec checkpoint there
re-triggers instantly without advancing an instruction. `$1CBD` is the usual
anchor - but note it is called from the IRQ handler (`irq-handlers.s:152`), not
from the game loop, so it and the game loop are different clocks.

## Cleaning up

Delete checkpoints before disconnecting. If a client vanishes with one pending,
VICE opens its own monitor window and the emulator freezes; `vice.log` then says
`Monitor UI: ... enabling PETSCII output`. `Monitor.close()` resumes and closes
tidily.
