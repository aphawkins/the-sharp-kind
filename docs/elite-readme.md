# Elite - The Sharp Kind

![Elite - The Sharp Kind - Screenshot (8-bit)](images/elite-screenshot-8-bit.png)
8-bit

![Elite - The Sharp Kind - Screenshot (16-bit)](images/elite-screenshot-16-bit.png)
16-bit

A C# port of the classic BBC Micro game 'Elite', meant to look, feel and play like the original 8-bit and 16-bit versions. It runs at the configured frame rate (60 by default), so it plays close to the same but feels slightly different.

Part of [The Sharp Kind](https://github.com/aphawkins/the-sharp-kind/blob/main/README.md).

## Run

```bash
dotnet run --project src/elite/apps/EliteSharp
```

CI also publishes self-contained single-file builds for win-x64 and linux-x64.

## Controls

| Key | Function |
| --- | -------- |
| F1 | Front view (in flight) / Launch (docked) |
| F2, F3, F4 | Rear, left, right view (F4 equips the ship when docked) |
| F5, F6 | Galactic chart, short range chart |
| F7 | Selected planet information |
| F8 | Stock market |
| F9, F10 | Commander information, inventory |
| F11 | Options (press again to close) |
| P / R | Pause / resume |

### Flight

| Key | Function |
| --- | -------- |
| S or Up, X or Down | Dive, climb |
| < or Left, > or Right | Roll left, roll right |
| Space, / | Speed up, slow down |
| A | Fire lasers |
| T, M, U | Target, fire, un-target missile |
| E | ECM |
| H, Ctrl+H | Hyperspace, galactic hyperspace |
| J | Warp jump |
| C, D | Docking computer on, off |
| Tab | Energy bomb |
| Esc | Escape capsule |

### Menus

- Cursor keys (or S and X) move, Enter chooses.
- Charts: D shows the distance to a planet, F finds one by name, O returns the cursor home.
- Stock market: Left sells, Right buys.

### Gamepad and joystick

Optional, alongside the keyboard. Flight, firing, speed, missiles and views are mapped for the Microsoft SideWinder Precision 2, the Speedlink Competition Pro Extra and Xbox-style pads; anything unrecognised gets the pad layout. With several devices plugged in, choose which one flies on the Engine Settings screen.

### Rebinding

Every control is a line in `elite.controls.sharp`, written beside `elite.sharp` on first run. Edit a line to move a control, or delete the file to restore the defaults. A line the game can't honour is dropped and the original kept as `elite.controls.sharp.bad`.

## Settings

Press F11 for the Options screen. Game Settings and Engine Settings change things live and save as you go; backend, rendition and window scale take effect on the next start.

## Configuration

Settings are in `elite.sharp`; the shared `engine` element is described in the [main readme](https://github.com/aphawkins/the-sharp-kind/blob/main/README.md#configuration). Elite's own settings sit under `game`:

``` json
{
    "game": {
        "planetStyle": "Fractal",              // Solid, Striped or Fractal
        "sunStyle": "Gradient",                // Solid or Gradient
        "planetDescriptions": "TreeGrubs",     // TreeGrubs (BBC) or HoopyCasinos (MSX)
        "instantDock": false                   // Dock instantly when the docking computer is engaged
    }
}
```

Commander saves (`.cmdr`) live in the same folder. A save the game couldn't have written is rejected and Commander Jameson is loaded instead.

### Environment variables

Shared variables are in the [main readme](https://github.com/aphawkins/the-sharp-kind/blob/main/README.md#environment-variables). Elite's own:

| Variable | Effect |
| -------- | ------ |
| `ELITE_LOG_LEVEL` | Overrides the logging level |
| `ELITE_DEBUG_COMMANDER` | Starts with the fully equipped `Max` commander |
| `ELITE_DEBUG_MISSIONS` | Enables Ctrl+M to cycle the mission briefings |
| `ELITE_DEBUG_YAW` | Adds yaw: Q and W, or a joystick twist |

## Plugins

Missions, goods and renditions are plugins loaded at startup from `Missions`, `Goods` and `Renditions` folders beside the executable. Missions are optional; exactly one goods set and at least the rendition named by `engine.rendition` (default `8-bit`) must be present. The 8-bit rendition plays the C64's music and effects on an emulated SID chip; the 16-bit one plays MIDI through a SoundFont with sampled effects.

## Credits

'Elite - The Sharp Kind' re-engineered in C# by Andy Hawkins 2023-2026, from C.J. Pinder's C version.

'Elite - The New Kind' by C.J. Pinder 1999-2001 (www.newkind.co.uk), reverse engineered from the BBC disk version, with ship data adapted from Ian Bell's published data and contributions by T. Harte. The original Elite is (C) I. Bell & D. Braben 1984. See [reference-sources.md](reference-sources.md).

- Gabriel Gambetta, [Computer Graphics from Scratch](https://gabrielgambetta.com/computer-graphics-from-scratch/).
- 8-bit fonts from VileR's [Oldschool PC Font Resource](https://int10h.org/oldschool-pc-fonts/).
- 8-bit music (`elite.sid`) by David Dunn & Aidan Bell, 1985 Firebird, from the [High Voltage SID Collection](https://hvsc.c64.org/). Sound effects are the C64 version's, read from Mark Moxon's [annotated source](https://github.com/markmoxon/elite-source-code-commodore-64).
