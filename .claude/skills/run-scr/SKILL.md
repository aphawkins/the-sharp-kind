---
description: Launch and visually smoke-test StuntCarRacerSharp (the SDL desktop app under src/scr/apps/StuntCarRacerSharp) by driving its real OS window - inject key presses, capture screenshots. Use when asked to run, screenshot, or visually verify SCR/Stunt Car Racer, or to confirm a change to it works in the real app.
---

StuntCarRacerSharp is a native Win32/SDL desktop app - there is no
browser or Electron surface, so it can't be driven with Playwright.
Verifying it visually means capturing the real OS window. This skill
drives it via the shared driver at
`.claude/skills/sdl-drive/drive.ps1` (also used by `run-elite` for
Elite - the driver itself is fully game-agnostic; only the exe path,
key sequence, and screen flow below are SCR-specific).

## Build first

```bash
dotnet build TheSharpKind.slnx -c Debug
```

Exe path:
`src/scr/apps/StuntCarRacerSharp/bin/Debug/net10.0/StuntCarRacerSharp.exe`.

## Run - the reliable way (scripted, preferred)

The app drives itself and dumps its own framebuffer. `GAME_KEY_SCRIPT`
feeds tick-exact input straight into the game's keyboard sink and
`GAME_FRAME_DUMP_DIR` makes it write a frame out on a `SaveFrame`
command, so **nothing depends on the window being focused, on top, or
even visible**, and nothing depends on wall-clock key-hold timing. Two
runs of the same script produce byte-identical frames.

Use this whenever you are capturing something to look at or to compare.
Reach for `-Steps` below only to poke at a window interactively.

```powershell
& ".claude/skills/sdl-drive/drive.ps1" `
  -ExePath "src\scr\apps\StuntCarRacerSharp\bin\Debug\net10.0\StuntCarRacerSharp.exe" `
  -Name "race" `
  -KeyScript @'
# track menu -> preview -> race, accelerating
30 Tap S
90 Tap S
150 Hold UpArrow
300 SaveFrame
'@
```

Frames land in `%TEMP%\sdl-app-shots\` as `<Name>.png`, or
`<Name>-01.png`, `<Name>-02.png`... in the order they were taken when a
script asks for several. They are the **native render target** - 320x256
for Elite's 8-bit rendition - so there is no window chrome and no window
magnification to undo, which makes two frames directly diffable.

Script syntax is one event per line (`KeyScriptParser` in
`SharpKind.Input`):

| line | what it does |
|---|---|
| `<tick> Tap <ConsoleKey> [Mods]` | press and release within that one tick |
| `<tick> Hold <ConsoleKey> [Mods]` | press and leave held |
| `<tick> Release <ConsoleKey> [Mods]` | release a held key |
| `<tick> SaveFrame` | dump the current framebuffer |

`<ConsoleKey>` is a `System.ConsoleKey` name - `Spacebar`, not `Space`;
`F8`; `X`; `UpArrow` - and modifiers are `Shift,Control,Alt`. Blank
lines and `#` comments are ignored. A tick is one game update, so how
much wall-clock and simulated time a tick is worth follows the app's
configured update rate (`engine.fps`); a script is reproducible against
a given configuration, not across different ones. The run stops as soon
as every requested frame is written, or after `-TimeoutSeconds`
(default 30).

## Run - driving the live window (`-Steps`, legacy)


Use the PowerShell tool and invoke with the call operator `&`, **not**
by prefixing `pwsh` (see Gotchas - nested `pwsh.exe` breaks array
binding):

```powershell
& ".claude/skills/sdl-drive/drive.ps1" `
  -ExePath "src\scr\apps\StuntCarRacerSharp\bin\Debug\net10.0\StuntCarRacerSharp.exe" `
  -Steps @(
    "launch",
    "screenshot:01-trackmenu",
    "key:S", "wait:800", "screenshot:02-trackpreview",
    "key:S", "wait:1200", "screenshot:03-race",
    "key:Up:800", "wait:200", "screenshot:04-race-accel",
    "key:Left:500", "wait:200", "screenshot:05-race-steer-left",
    "key:Escape",
    "quit"
  )
```

Screenshots land in `%TEMP%\sdl-app-shots\` (override with the
`SCREENSHOT_DIR` env var or `-ScreenshotDir`). Then actually open each
PNG with the Read tool - don't just check the process didn't crash, and
don't trust one that came with a foreground warning.

### Steps (shared driver - same for every skill that uses it)

| step | what it does |
|---|---|
| `launch` | start the exe, wait for its window (throws after `-LaunchTimeoutMs`, default 15s), foreground it |
| `screenshot:<name>` | capture the window to `<ScreenshotDir>/<name>.png` |
| `key:<KeyName>` | press and release a key (150ms hold) |
| `key:<KeyName>:<holdMs>` | press, hold for `<holdMs>`, then release - use this for SCR's driving keys (`Up`/`Down`/`Left`/`Right`/`Space`), which read as *held*, not *pressed* (see Gotchas) |
| `wait:<ms>` | sleep |
| `quit` | stop the process |

See `ConvertTo-VirtualKeyCode` in `../sdl-drive/drive.ps1` for
supported key names (`F1`-`F12`, `A`-`Z`, `0`-`9`, `Space`, `Enter`,
`Esc`/`Escape`, `Up`/`Down`/`Left`/`Right`); add more there if you need
one - it's shared, so a name you add works for both games. Any step
not given a `quit` is cleaned up automatically at the end of the
script (with a warning), so a thrown error mid-sequence doesn't leave
the process running.

## Known screen flow (from Screens/*.cs, GameMode.cs)

Useful for building new `-Steps` sequences. Four modes
(`GameMode` enum): `TrackMenu` (0, the start screen) -> `TrackPreview`
(1) -> `GameInProgress` (2, the race) -> `GameOver` (3).

- **TrackMenu** ([TrackMenuScreen.cs](../../../src/scr/libs/StuntCarRacerSharpLib/Screens/TrackMenuScreen.cs)):
  digits `1`-`8` pick a track (defaults to Little Ramp); `S` goes to
  **TrackPreview**; `Escape` quits.
- **TrackPreview** ([TrackPreviewScreen.cs](../../../src/scr/libs/StuntCarRacerSharpLib/Screens/TrackPreviewScreen.cs)):
  `S` starts the race (**GameInProgress**), `M` back to **TrackMenu**.
- **GameInProgress / race** ([RaceScreen.cs](../../../src/scr/libs/StuntCarRacerSharpLib/Screens/RaceScreen.cs)):
  driving keys are read with `IsHeld`, not `IsPressed` - `Left`/`Right`
  = steer, `Up` = accelerate, `Down` = brake, `Space` = boost. `N`
  changes scenery. `M` abandons the race for **TrackMenu** (works while
  paused too); `P` pauses and `O` resumes; `Backspace` toggles the
  outside/chase view; `Escape` quits.
- **GameOver** ([GameOverScreen.cs](../../../src/scr/libs/StuntCarRacerSharpLib/Screens/GameOverScreen.cs)):
  `M` back to **TrackMenu**.
- `Escape` quits from anywhere ([StuntCarRacerMain.cs](../../../src/scr/libs/StuntCarRacerSharpLib/StuntCarRacerMain.cs)).

## Gotchas

These apply to `sdl-drive/drive.ps1` itself, so they're the same for
`run-elite` too:

- **Invoking as `pwsh drive.ps1 -Steps @(...)` from inside an
  already-running `pwsh` session spawns a *nested* `pwsh.exe` process,
  and its CLI argument parsing silently breaks array binding** -
  `-Steps` only picks up the first element, and later `-Steps` items
  shift into `-ScreenshotDir`/`-LaunchTimeoutMs` positionally,
  producing a confusing type-conversion error on `-LaunchTimeoutMs`
  (e.g. `Cannot convert value "wait:600" to type "System.Int32"`). Use
  the call operator (`& "drive.ps1" -Steps @(...)`) instead - that
  runs the script in the *current* session, where PowerShell's real
  parameter binder handles the array correctly. Hit and confirmed
  while building this skill, not a theoretical concern.
- **`System.Windows.Forms.SendKeys` and the `SendInput` API do not
  work against these apps.** Both were tried first: the target window
  was confirmed correctly foregrounded and focused
  (`GetForegroundWindow()` matched the target `hwnd` after
  `SetForegroundWindow` and even after a synthetic click on the
  window), but SDL's event pump never observed either kind of
  injected input - screenshots kept showing the same frame,
  unchanged. Posting `WM_KEYDOWN`/`WM_KEYUP` directly to the window
  handle via `PostMessage` is the only method that produced an actual
  in-game screen transition. If you're tempted to "simplify" this to
  `SendKeys` - don't, without re-verifying against a screenshot first.
- **Hold the key down across a poll cycle.** Both games tick well
  under 100ms per frame, and `SoftwareKeyboard.KeyUp` resets the same
  "just-pressed" state that `IsPressed` reads, so a down+up pair that
  both land in the same `SDL_PollEvent` drain can cancel out before
  `IsPressed` ever sees them. The driver's default 150ms hold covers
  this; SCR's driving controls use `IsHeld` instead (see above) and
  read correctly for as long as you hold the key via `key:<Name>:<ms>`.
- **`-Steps` screenshots need the window on-screen and unobstructed** -
  capture is a real `CopyFromScreen`, not an off-screen render. Don't
  minimize or cover the window between `launch` and `quit`. The driver
  re-foregrounds the window before every `screenshot` step and waits for
  `GetForegroundWindow` to agree (`SetForegroundWindow` often fails on
  the first call - Windows' foreground lock refuses a process that
  doesn't already own the foreground - and a window merely *behind*
  another one silently captures that other window's pixels, which reads
  as "the app ignored my keys"). If it can't get there within two
  seconds it warns; **treat that warning as "these screenshots are not
  the app"** - it has bitten real work, capturing an unrelated
  full-screen application and a browser window, and a diff against one
  of those is worse than no diff at all. `-KeyScript` has none of this
  exposure and is why it exists: use it for anything you intend to look
  at closely or compare.
- **`PrintWindow` is not a fix for that** - it was tried, with and
  without `PW_RENDERFULLCONTENT`, and returns the window frame with a
  black client area, as it does for any compositor-presented window.
  The app dumping its own framebuffer is the way round it.
