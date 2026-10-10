# Stunt Car Racer - The Sharp Kind

![Stunt Car Racer - The Sharp Kind - Screenshot](images/scr-screenshot.png)

A C# port of Geoff Crammond's 'Stunt Car Racer', converted from the C++/DirectX9 Windows remake of the Amiga version. Race the clock and an AI opponent over elevated tracks, with the original Amiga track data and physics.

Part of [The Sharp Kind](https://github.com/aphawkins/the-sharp-kind/blob/main/README.md).

## Status

Playable, preview. All eight original tracks are in.

## Run

```bash
dotnet run --project src/scr/apps/StuntCarRacerSharp
```

## Controls

| Key | Function |
| --- | -------- |
| Arrow keys | Steer, accelerate, brake / reverse |
| Space | Boost |
| 1-8 | Choose a track (menu) |
| S | Preview the track (menu) / start the race (preview) |
| M | Back to the track menu |
| Backspace | Toggle the chase camera |
| R | Point the car the opposite way |
| P / O | Pause / resume |
| Esc | Quit |

Developer keys from the remake: N changes the scenery, F5 shows the stats overlay, F6 and F7 freeze your car and the opponent's, F9 and F10 change the physics step.

A joystick or gamepad also works alongside the keyboard: stick or left stick to steer, right trigger or stick forward to accelerate, button A or 1 to start and boost, Start to pause.

## Configuration

Settings are in `stuntcarracer.sharp`; see the [main readme](https://github.com/aphawkins/the-sharp-kind/blob/main/README.md#configuration). Stunt Car Racer has no settings of its own. `SCR_LOG_LEVEL` overrides the logging level.

## Credits

'Stunt Car Racer - The Sharp Kind' re-engineered in C# by Andy Hawkins 2026, from the C++/DirectX9 Windows remake ([ptitSeb/stuntcarremake](https://github.com/ptitSeb/stuntcarremake), forked from [fluffyfreak/stuntcarracer](https://github.com/fluffyfreak/stuntcarracer)). See [reference-sources.md](reference-sources.md).

The original Stunt Car Racer is (C) Geoff Crammond / MicroStyle / MicroProse 1989, now believed to be copyright Infogrames or Interactive Game Group.
