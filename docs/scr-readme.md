# Stunt Car Racer - The Sharp Kind

![Stunt Car Racer - The Sharp Kind - Screenshot](images/scr-screenshot.png)

A C# port of the classic Geoff Crammond racing game 'Stunt Car Racer'.  It is converted from the C++/DirectX9 Windows remake of the Amiga version, and is meant to feel and play the same as the original.

The port shares the `SharpKind` libraries with 'Elite - The Sharp Kind': hardware access is hidden behind interfaces, with a software renderer drawing through SDL3.  The physics uses the original Amiga fixed-point algorithms and track data.

Part of [The Sharp Kind](https://github.com/aphawkins/the-sharp-kind/blob/main/README.md), alongside [Elite - The Sharp Kind](elite-readme.md).  Remaining conversion work is tracked in the [backlog](backlog-roadmap.md).

Race the clock and an AI opponent over four laps of elevated track.  The car takes damage from heavy landings and collisions, boost is limited, and falling off the track costs time - after too long off the track the car is dropped back on.

## Status

Playable, preview.  The game starts at the track selection menu with all eight original tracks, framed with the remake's title artwork.

## Getting Started

To build and run from source, install the .NET SDK and run:

``` bash
dotnet run --project src/scr/apps/StuntCarRacerSharp
```

It can also be run and debugged directly from an IDE: open [TheSharpKind.slnx](https://github.com/aphawkins/the-sharp-kind/blob/main/TheSharpKind.slnx) in Visual Studio and set `StuntCarRacerSharp` as the startup project, or open the repo root in VS Code and use the "Stunt Car Racer" launch configuration (`.vscode/launch.json`).

## Controls

The keys follow the current remake's scheme: arrow keys to steer/accelerate/brake, Space to boost.

At the track menu:

| Key | Function |
| --- | -------- |
| 1-8 | Choose a track |
| S | Preview the selected track |
| Esc | Quit |

At the track preview:

| Key | Function |
| --- | -------- |
| S | Start the race |
| M | Back to the track menu |
| Esc | Quit |

During the race:

| Key | Function |
| --- | -------- |
| Left arrow | Steer left |
| Right arrow | Steer right |
| Up arrow | Accelerate |
| Down arrow | Brake / reverse |
| Space | Boost |
| Backspace | Toggle the outside/chase camera |
| R | Point the car the opposite way |
| N | Change the scenery type |
| P | Pause |
| O | Resume |
| F5 | Show/hide the stats overlay |
| F6 | Freeze/unfreeze your car |
| F7 | Freeze/unfreeze the opponent's car |
| F9 | Step the physics more often |
| F10 | Step the physics less often |
| M | Back to the track menu (after GAME OVER) |
| Esc | Quit |

Notes:
- Boost (Space) only does anything while accelerating or braking, and only while the reserve shown on the dashboard isn't empty; accelerate and brake work on their own without it.
- Steering only works while the wheels are touching the road.
- Backspace toggles between the cockpit view and a chase camera behind and above the car. Like R, it works even while paused.
- R points the car the opposite way, as the remake does. It is the way back for a car that ends up facing backwards. The new heading applies on the next physics frame.
- P pauses and O resumes, as the remake does. Pausing stops the engine sound and freezes everything the race advances - the car, the opponent, the drawbridge, and the lap and race timers - so a pause cannot inflate a lap time. Unlike N and the F-keys, the pause keys only work during the race.
- F9 and F10 tune the physics frame gap, as the remake's do: the physics steps every fourth 50Hz tick by default, F9 shortens that gap (down to every tick) and F10 lengthens it. N, F5, F6, F7, F9 and F10 work on every screen, not only during the race.
- F5, F6 and F7 are the remake's development aids rather than game controls. F5 shows an overlay of the frame gap and the two freeze states; F6 and F7 freeze the cars independently, so one can be studied while the other drives. Unlike the P/O pause, a freeze leaves the race clock and the lap timers running, and both freezes clear when a race starts.

### Gamepad and joystick

Optional, and entirely alongside the keyboard: every key above keeps
working, and the pad is only read on a tick where no key is held - the same
rule ptitSeb's remake uses (`Car_Behaviour.cpp:791`). A device is picked up
whenever it is plugged in, so it need not be attached before the game
starts.

Two layouts are live at once, because the two kinds of device have nothing
in common but the stick. Buttons are numbered as the device numbers them,
which for a device SDL has no mapping for is simply the order its buttons
are wired in.

| Control | Joystick | Gamepad |
| ------- | -------- | ------- |
| Steer left / right | Stick left / right | Left stick |
| Accelerate | Stick forward | Right trigger |
| Brake / reverse | Stick back | (B) or left trigger |
| Boost | Button 1 or 3 | (A) or (X) |
| Select a track (track menu) | Stick left / right | Left stick |
| Start the race (menu, preview) | Button 1 | (A) |
| Back to the track menu (preview) | Button 2 | (B) |
| Back to the track menu (after GAME OVER) | Button 1 | (A) |
| Pause / resume (during the race) | Button 8 | Start |
| Abandon the race | Button 7 | Back |

Notes:
- Brake wins over accelerate when both are asked for, as it does in the
  remake.
- Steering is digital, so an analog stick has to travel past halfway before
  it counts. A joystick sits at the ends of its range, so it always does.
- On the track menu the stick steps one track per push: it has to return to
  centre before it moves again, or one flick would run through the list.
- The track preview lists the pad controls on screen while a device is
  attached.

## Dashboard

The cockpit is drawn as a set of sprites over the track view: front wheels that bounce with the suspension and spin with road speed, an engine that flares when boosting, a damage crack across the top beam that grows with accumulated damage (and leaves a hole once it fully cracks), a speed gauge, and the lap number, boost reserve and opponent distance shown in the dashboard's read-out panels.

## Configuration

Game settings are held in the `stuntcarracer.sharp` file, stored in JSON format, in the user's application data directory (`%AppData%\The Sharp Kind` on Windows, `~/.config/The Sharp Kind` on Linux/macOS) — shared with [Elite - The Sharp Kind](elite-readme.md). Logs (`logs\scr-*.log`, daily rolling, 7 kept by default) live in the same directory. If the config file is missing or invalid the game falls back to defaults. There is no in-game settings screen yet, so `stuntcarracer.sharp` must be edited by hand.

The file's `engine` element holds the settings shared by every game — the backend, the tier, the window scale, the frame rate, the sound switches and the logging levels among them — and is documented in the [main readme](https://github.com/aphawkins/the-sharp-kind/blob/main/README.md#configuration). Of those, `fillMode`, `depthSort`, `shading`, `quantisation` and `showFps` are written out but not acted on yet: Stunt Car Racer draws its track through its own pipeline.

Stunt Car Racer's own settings would sit alongside under `game`, but it has none of its own yet, so the element is written out empty:

``` json
{
    "game": {}
}
```

### Environment variables

Stunt Car Racer's own diagnostic opt-in, read at runtime rather than compiled in, so it works in a Release build. The ones shared with Elite, `GAME_KEY_SCRIPT` and `GAME_FRAME_DUMP_DIR`, are in the [main readme](https://github.com/aphawkins/the-sharp-kind/blob/main/README.md#environment-variables).

| Variable | Value | Effect |
| -------- | ----- | ------ |
| `SCR_LOG_LEVEL` | A Serilog level name: `Verbose`, `Debug`, `Information`, `Warning`, `Error` or `Fatal` — not the config file's own `Trace`…`None` names. Case-insensitive; anything unparseable is ignored and the config value stands | Overrides `engine.logging.minimumLevel`, for when the config file itself is what needs debugging |

## Porting notes

- Source of the conversion: `github.com/ptitSeb/stuntcarremake` (C++, DirectX9/DXUT + SDL2), a maintained fork of `fluffyfreak/stuntcarracer`.  Earlier work was ported from fluffyfreak before the switch; where the two diverge, ptitSeb's is the source of truth — see [reference-sources.md](reference-sources.md).
- Hardware access stays behind the `SharpKind.Abstraction` interfaces (`IGraphics`, `IKeyboard`, `ISound`); the software rasterizer (`SharpKind.Graphics.SoftwareGraphics`) is the primary rendering path.
- Before writing SCR-specific code, check whether the equivalent already exists in `src/useful/*` and extend that library instead of duplicating it.  A genuine SCR-only need (e.g. track-segment collision) is fine to keep local.
- Behavioural fidelity ("feels like the original"), not bit-exact numerical replication, is the bar — there is no requirement to match the original's frame-by-frame physics output.
- The original's binary asset formats (tracks/bitmaps/sounds) are read-once inputs to a one-time conversion step, not a live format the C# code must parse identically forever.
- The original remake's Windows-only infrastructure (DXUT registry prefs, clipboard, DirectSound path, `MessageBox` dialogs) is deliberately not ported — it belongs to the DirectX stack this port bypasses in favour of SDL3 + `SharpKind.Audio`.

## Credits

'Stunt Car Racer - The Sharp Kind' re-engineered in C# by Andy Hawkins 2026.
- Converted from the C++/DirectX9 Windows remake: sourceforge.net/projects/stuntcarremake (fork: github.com/ptitSeb/stuntcarremake, itself forked from github.com/fluffyfreak/stuntcarracer).
- The remake uses the original Amiga track data, sound samples and car physics algorithms.

The original Stunt Car Racer is (C) Geoff Crammond / MicroStyle / MicroProse 1989, now believed to be copyright Infogrames or Interactive Game Group.
