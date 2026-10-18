---
description: Launch and visually smoke-test EliteSharp (the SDL desktop app under src/elite/apps/EliteSharp) by driving its real OS window - inject key presses, capture screenshots. Use when asked to run, screenshot, or visually verify EliteSharp, or to confirm a change to it works in the real app.
---

EliteSharp is a native Win32/SDL desktop app - there is no browser or
Electron surface, so it can't be driven with Playwright. Verifying it
visually means capturing the real OS window. This skill drives it via
the shared driver at `.claude/skills/sdl-drive/drive.ps1` (also used
by `run-scr` for Stunt Car Racer - see that skill's SKILL.md and the
driver's own header comment for what it does and how; this file only
covers what's Elite-specific).

## Build first

```bash
dotnet build TheSharpKind.slnx -c Debug
```

Exe path: `src/elite/apps/EliteSharp/bin/Debug/net10.0/EliteSharp.exe`.

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
  -ExePath "src\elite\apps\EliteSharp\bin\Debug\net10.0\EliteSharp.exe" `
  -Name "market" `
  -KeyScript @'
# intro -> market, cursor down 3 rows, capture
30 Tap N
90 Tap Spacebar
150 Tap F8
180 Tap X
190 Tap X
200 Tap X
210 SaveFrame
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
by prefixing `pwsh` (see Gotchas in `sdl-drive`'s SKILL-shared notes
below - nested `pwsh.exe` breaks array binding):

```powershell
& ".claude/skills/sdl-drive/drive.ps1" `
  -ExePath "src\elite\apps\EliteSharp\bin\Debug\net10.0\EliteSharp.exe" `
  -Steps @(
    "launch",
    "screenshot:01-intro1",
    "key:N", "wait:600", "screenshot:02-intro2",
    "key:Space", "wait:600", "screenshot:03-commanderstatus",
    "key:F1", "wait:1500", "screenshot:04-front",
    "key:F2", "wait:600", "screenshot:05-rear",
    "key:F3", "wait:600", "screenshot:06-left",
    "key:F4", "wait:600", "screenshot:07-right",
    "quit"
  )
```

Screenshots land in `%TEMP%\sdl-app-shots\` (override with the
`SCREENSHOT_DIR` env var or `-ScreenshotDir`). Then actually open each
PNG with the Read tool - don't just check the process didn't crash, and
don't trust one that came with a foreground warning.

## Known screen flow (from EliteMain.cs / Views/*.cs)

Useful for building new `-Steps` sequences:

- Launch starts on **Intro1** ("Load New Commander (Y/N)?"). `key:N`
  skips to **Intro2**.
- Intro2 (ship parade) - `key:Space` goes to **CommanderStatus**
  (docked at the start system).
- From docked, `F1` = launch/undock (goes to **Undocking** then
  **FrontView**), `F2`/`F3`/`F4` = Rear/Left/Right view (only work
  once undocked). Other `F`-keys switch to chart/status/options
  screens (`EliteMain.HandleViewKeys`) - `F1` while docked instead
  goes to **Undocking**, `F4` while docked goes to **EquipShip**.
- The **mission briefings** are hours of play away in normal use. Set
  `ELITE_DEBUG_MISSIONS` (to any value) before launching and `key:Ctrl+M`
  cycles all five - Constrictor brief, Constrictor debrief, the two
  Thargoid briefs, the Thargoid debrief. Each is a cheat that leaves the
  commander mid-mission, so use a fresh run per check
  ([docs/elite-readme.md](../../../docs/elite-readme.md#environment-variables)).
- Full mapping: [Screen.cs](../../../src/elite/libs/EliteSharpLib/Views/Screen.cs),
  key handling: [EliteMain.cs](../../../src/elite/libs/EliteSharpLib/EliteMain.cs).

## Gotchas

See `.claude/skills/sdl-drive/drive.ps1`'s header comment and
`run-scr/SKILL.md`'s Gotchas for the shared ones (the `pwsh`-vs-`&`
invocation trap, why `PostMessage` is used instead of
`SendKeys`/`SendInput`, and the key-hold-duration timing issue) - they
apply here unchanged since both skills drive the same script.

Both of the shared gotchas about *timing* and the one about a covered
window are `-Steps` problems only. `-KeyScript` sidesteps all of them:
input goes into the keyboard sink rather than through SDL's event queue,
so no hold duration can be swallowed, and the frame comes from the game
rather than the screen, so nothing can cover it. Prefer it.

Elite-specific: when `engine.backend` in
`%APPDATA%\The Sharp Kind\elite.sharp` is `"Hardware"` (the
maintainer's normal value is `"Software"`), give the app a longer
settle before the first key and hold each key longer - `"wait:5000"`
after `launch`, and `key:N:400` instead of `key:N`. A `-KeyScript` run
needs no such allowance: a tick is a tick whatever the backend is doing.

Input itself is backend-independent: `SDLAbstraction` and
`SoftwareAbstraction` build the same `SoftwareKeyboard(new SDLInput())`
over the same `SDLWindow`, and a Hardware run was verified key-driveable
end-to-end (Intro1 -> FrontView, 4/4 runs). The risk is purely timing.
`SDLInput.Poll` drains the whole SDL queue per update, so if one loop
iteration ever outlasts the key hold, that update sees `KeyDown` and
`KeyUp` together and `SoftwareKeyboard.KeyUp` clears `_lastKeyPressed`
before `Update` reads it - the press is silently swallowed and the
screen just never advances. At 13.5 updates/sec that needs a stall of
more than ~150ms, which a cold Hardware first launch (pipeline
creation, JIT, disk) can produce even though the steady state is 60fps.
