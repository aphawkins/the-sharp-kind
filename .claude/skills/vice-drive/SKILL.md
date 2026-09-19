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
`C:\code\github\zaidka\rebb64` in `build/`, with `cc65` on PATH from
`%USERPROFILE%/cc65/bin`.

**Two artefacts, and the difference matters.** `make` builds `rebb64-raw.prg`,
the verifiable image, which **will not start**: loaded and entered at
`game_entry` it sits in a wait loop for ever. `make verify` proves that image
byte-exact against the original's SHA256, and every fidelity claim rests on it.
The runnable one is `make release`, which needs `tscrunch`:

```bash
make release TSCRUNCH=C:/code/github/tonysavon/TSCrunch/bin/windows_amd64/tscrunch.exe
```

That writes `rebb64.prg`, 44,673 bytes, self-starting at `$0801`. **Autostart
that one.**

Joystick input needs `%APPDATA%/vice/vice.ini` to have, under `[C64SC]`:
`JoyDevice2=2`, `KeySetEnable=1`, `SaveResourcesOnExit=0`, and `KeySet1`
North/South/West/East/Fire = `119/115/97/100/32` (W, S, A, D, space). That is
already configured on this machine.

## Launch

Run it in the background - it is a GUI app and will not return:

```bash
"$VICE" -binarymonitor -binarymonitoraddress ip4://127.0.0.1:6502 -remotemonitor -remotemonitoraddress ip4://127.0.0.1:6510 -autostart /c/code/github/zaidka/rebb64/build/rebb64.prg
```

Both monitors at once is fine and tested. The binary one on 6502 is what
`monitor.py` drives; the text one on 6510 is worth having only for its
`screenshot` command - see **Seeing the screen**.

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

**Solved - 2026-09-19.** One command:

```bash
python .claude/skills/vice-drive/scripts/monitor.py start
```

It only claims success on `state=$01` with a non-zero X. On level 1 that reads
`state=$01 X=$2C Y=$DD`, and `$DD` is exactly what `check_player_state` writes
when it respawns a player, so two sources agree on it.

**The trick is that it takes two presses, and they are not the same kind of
input.** Fire alone never starts a game. It buys a credit and moves the front
end on to a *second* screen, PRESS 1 OR 2 TO PLAY, and that screen reads the
**keyboard**, not the stick. Holding fire at it does nothing for ever, which is
exactly what the earlier sessions saw and mistook for input not landing.

1. `fire` (space, through the keyset) - title screen, inserts a credit
2. wait about 4 seconds
3. `one` (the real `1` key) - starts a one-player game
4. wait about 9 seconds for the level to appear

`press.ps1` has `one` and `two` beside the directions for this reason.

Two delivery details, both of which cost time:

- **Send scancodes, not virtual keys.** `press.ps1` uses `SendInput` with
  `KEYEVENTF_SCANCODE`. Prove the path end to end by typing a letter at the
  BASIC prompt and screenshotting it, rather than assuming.
- **Never let two holds overlap.** One invocation's key-up lands after the
  next one's key-down and silently releases the key. A run was lost this way,
  the game looking unresponsive while in fact nothing was held. Keys that must
  be held together go in a single call.

Then drive the player with held directions:

```bash
powershell.exe -NoProfile -ExecutionPolicy Bypass   -File .claude/skills/vice-drive/scripts/press.ps1 -Keys right -HoldMs 8000
```

Invoke it that way, not as `./press.ps1`: from the Bash tool a bare path hands
a PowerShell script to sh, which chokes on its first line. `monitor.py`'s own
`press()` helper already does this, so prefer that from Python.

## Seeing the screen

**When a capture makes no sense, look at the screen before theorising.** The
text monitor writes a PNG - send this as a line to port 6510, then `x`:

```
screenshot "C:/full/path/shot.png" 2
```

It is the fastest way to tell a title screen from a select screen from a live
level, and it is how the sequence above was worked out at all. A live level
shows ROUND 1 and READY!! over the playfield.

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

## The traps

Each of these has cost a session. They are the reason this skill exists.

The first four are about the protocol. The rest are about reading what comes
back, which turns out to be where most of the time actually goes.

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

If stepping rather than sampling: **the frame anchor must not be `$E498`.**
`wait_one_frame` is a spin loop, so an exec checkpoint there re-triggers
instantly without advancing an instruction. `$1CBD` is the usual anchor - but
note it is called from the IRQ handler (`irq-handlers.s:152`), not from the game
loop, so it and the game loop are different clocks.

### Reading the results

**Never diagnose from a sampled program counter.** `$E496` is `wait_one_frame`,
where the CPU sits for most of every frame, so a `r` at a random moment lands
there almost always. A whole session concluded from it that the game had hung on
`lda $08 / cmp $08 / beq`, blamed a missing packer, and wrote it into
`bb-port-plan.md` - when the game was sitting happily on its title screen.
Screenshot first.

**A control address has to be one you have watched the program counter sit in.**
Three controls in a row proved nothing because the chosen address never
executed: `$A605` is `game-init.s`'s wait loop, while the title screen runs the
one at `$4565` in `game-init-early.s`. That nearly produced the confident
conclusion that checkpoints do not work at all. Only `$E498` - watched, not
assumed - settled it.

**A store checkpoint reports the PC of the instruction AFTER the store.**
Confirmed twice: against a store worked out by hand in `player-state.s`, and
against `L22C2`, whose address its own label gives away. Subtract the store's
width before matching it to a routine.

**An address is not a label that looks like it.** `$267B` is the instruction
after `inc FA,x` at `$2679`, the tail of one routine; `L267B` is an unrelated
routine that merely starts there. Work back from the store address to the
source, never from the name.

**`$DC00` is not a witness for whether a key is held.** The game writes `$7F` to
that port itself, so a sample catches either its own write or the stick
depending where in the frame it lands. Judge input by whether the player moves.

**Both counters are single bytes, so a long window wraps one of them.** `$08`
runs at about 50 a second and turns over in 5.1 seconds. A six second
measurement came back as 47 frames rather than 303, giving a ratio of 0.47
rather than 3.00 - wrong, and still perfectly plausible on its own. It was
caught only because it disagreed with a `selftest` taken a minute earlier.
`passes` and `watch` now refuse a window over 4.5 seconds rather than answer
it. `$A9FA` at about 17 a second has 15 seconds of room, so `$08` is the
binding constraint.

Cross-check any ratio against `selftest`'s two rates. They are measured
independently, so if they disagree one of them is lying.

### Keeping a manoeuvre alive

Traps that fire for every entity stop the machine dozens of times a second,
which pulls emulated time out of step with the wall-clock timing of a held key.
Filter to the players with a condition on the checkpoint - `MON_CMD_CONDITION_SET`
is `0x22`, body `uint32` checkpoint number, `uint8` length, then ASCII such as
`X < 2`, and registers are named `A X Y PC SP FL`.

If the player keeps dying before reaching whatever is being captured, zeroing
the six entity slots at `$00B4`-`$00B9` clears the enemies for a while. That is
an intervention on the game, not on the routine under test - say so in whatever
the capture ends up supporting.

## Cleaning up

Delete checkpoints before disconnecting. If a client vanishes with one pending,
VICE opens its own monitor window and the emulator freezes; `vice.log` then says
`Monitor UI: ... enabling PETSCII output`. `Monitor.close()` resumes and closes
tidily.
