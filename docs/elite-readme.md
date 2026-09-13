# Elite - The Sharp Kind

![Elite - The Sharp Kind - Screenshot (8-bit)](images/elite-screenshot-8-bit.png)
8-bit

![Elite - The Sharp Kind - Screenshot (16-bit)](images/elite-screenshot-16-bit.png)
16-bit

A C# port of the classic BBC home computer game 'Elite'.  It is meant to look, feel and play the same as the original 8bit and 16bit versions of the game.

Currently the objective of this port is authenticity, object oriented code and cross platform compatibility using dotnet.
The original BBC game ran its logic at a fixed 13.5 Hz. Elite now simulates and composes at the engine's configured frame rate instead (`engine.graphics.fps`, 60 by default), with its per-tick rates converted to true per-second ones — so the game plays close to the same at any rate, though feel and difficulty shift slightly away from 13.5 Hz.

Part of [The Sharp Kind](../README.md), alongside [Stunt Car Racer - The Sharp Kind](scr-readme.md).

## Getting Started

The program has been tested to run on the following platforms and architectures:
- Windows (x64)
- Ubuntu 24.04 (x64)
- Raspberry Pi 4 (ARM64)

To build and run from source, install the .NET SDK and run:

``` bash
dotnet run --project src/elite/apps/EliteSharp
```

It can also be run and debugged directly from an IDE: open [TheSharpKind.slnx](../TheSharpKind.slnx) in Visual Studio and set `EliteSharp` as the startup project, or open the repo root in VS Code and use the "Elite" launch configuration (`.vscode/launch.json`).

CI also publishes self-contained single-file builds (win-x64 and linux-x64) that do not require the .NET runtime to be installed.

## Controls

Press Y or N on the intro screen
Press Space on the ship parade screen
Use Left/Right cursor keys to scroll through ships on the ship parade screen

| Key | Function |
| --- | -------- |
| F1  | Front View (in flight) <br/> Launch (when docked) |
| F2  | Rear View |
| F3  | Left View |
| F4  | Right View (in flight) <br/> Equip ship (when docked) |
| F5 | Display Galactic Chart |
| F6 | Short Range Chart |
| F7 | Show information on selected planet |
| F8 | Stock market |
| F9 | Commander information |
| F10 | Inventory |
| F11 | Options — pressed again on the options, closes them and returns to where they were opened from |
| F12 | Dump the current frame as a BMP (needs `GAME_FRAME_DUMP_DIR` — see the [main readme](../README.md#environment-variables)) |
| P | Pause game |
| R | Resume game |
| Ctrl-M | Cycle the five mission briefings (needs `ELITE_DEBUG_MISSIONS`) |

### Flight Controls
| Key | Function |
| --- | -------- |
| A | Fire lasers |
| S or Up Arrow | Dive |
| X or Down Arrow | Climb |
| &lt; or Left Arrow | Roll Left |
| &gt; or Right Arrow | Roll Right |
| / | Slow Down |
| Space | Speed up |
| C | Activate docking computer, if fitted |
| D | De-activate docking computer if switched on |
| E | Active ECM, if fitted |
| H | Hyperspace |
| J | Warp Jump |
| M | Fire missile |
| T | Target a missile |
| U | Un-target missile |
| TAB | Detonate energy bomb, if fitted |
| CTRL+H | Galactic Hyperspace, if fitted |
| ESC | Launch escape capsule, if fitted |

### Gamepad and joystick

Optional, and entirely alongside the keyboard. Flight, fire, speed,
missiles and the cockpit views are mapped, as far as each device has
controls for them. The rest of what Elite can do - docking, hyperspace,
ECM, the charts and the trading screens - stays on the keys, because no
stick here has buttons left for it. A device is picked up whenever it is
plugged in.

Each device gets its own layout, because the three disagree about every
button. Button 1 fires the laser on a SideWinder, fires a missile on a
Competition Pro, and slows the ship on an Xbox pad, so there is no single
assignment that serves all three. The device is recognised by the name its
driver reports; anything unrecognised gets the pad layout.

Buttons below are numbered as the device numbers them, which for a stick
with no SDL mapping is the order its buttons are wired in.

#### Microsoft SideWinder Precision 2

The only one of the three with an axis to spare for speed, and the only
one whose buttons are all mapped. The four base buttons take the commands a
pilot reaches for deliberately; the escape capsule is deliberately not
among them, because a base button brushed by accident would end the run.

| Control | Input |
| ------- | ----- |
| Fire lasers | Button 1 (trigger) |
| Fire missile | Button 2 |
| Un-target missile | Button 3 |
| Target missile | Button 4 |
| Speed | Throttle lever — forward is fast |
| Dive / climb | Stick forward / back |
| Roll left / right | Stick left / right |
| Yaw left / right | Twist, with `ELITE_DEBUG_YAW` set |
| Front / rear / left / right view | Hat up / down / left / right |
| ECM | Button 5 |
| Warp jump | Button 6 |
| Docking computer, on and off | Button 7 |
| Hyperspace | Button 8 |

#### Speedlink Competition Pro Extra

Four buttons and nothing else — no hat, no lever, no triggers — so
targeting a missile stays on the keyboard's `T`.

| Control | Input |
| ------- | ----- |
| Fire lasers | Button 3 |
| Fire missile | Button 1 |
| Speed up | Button 2 |
| Slow down | Button 4 |
| Dive / climb | Stick forward / back |
| Roll left / right | Stick left / right |

#### Xbox controller, and any unrecognised device

Both face buttons are spent on speed, so the lasers move to the triggers.

| Control | Input |
| ------- | ----- |
| Fire lasers | Right trigger |
| Fire missile | Left trigger |
| Target missile | Left shoulder |
| Speed up | (B) |
| Slow down | (A) |
| Dive / climb | Left stick forward / back |
| Roll left / right | Left stick |
| Front / rear / left / right view | D-pad up / down / left / right |

How pitch, roll and yaw answer depends on what the stick can say.

A digital stick is a key: it sits at the ends of its range, and each held
direction steps the ship by a fixed amount, building to the full rate of
turn over about a second and bleeding back to level over about two. That
ramp is the feel of the original, and it is what the keyboard uses too.

An analog stick says how far it is pushed, and that is taken as the rate of
turn itself - reached at once, and dropped at once when the stick comes
back. Half over rolls at half rate and holds there. A tenth of the travel
around the centre is ignored, since a worn potentiometer wanders.

The two are told apart per axis, by what the axis does rather than by what
the device claims to be: an axis that reports a position between its
extremes can only be a potentiometer. Per axis, because one stick is
routinely both - a SideWinder's twist is analog while its hat is switches.
The keyboard still works while a stick is plugged in and centred.

Tested with a Microsoft SideWinder Precision 2, a Speedlink Competition Pro
Extra and an Xbox One controller.

### Chart Screens
| Key | Function |
| --- | -------- |
| D | Select a planet and show distance to it |
| F | Find planet by name |
| O | Return cursor to current planet |
| Cursor Keys | Move cross hairs around |

### Equipment Screen
| Key | Function |
| --- | -------- |
| Arrow keys | Navigate options |
| Enter | Buy item |

### Stock Market
| Key | Function |
| --- | -------- |
| S or Up Arrow | Select previous item |
| X or Down Arrow | Select next item |
| &lt; or Left Arrow | Sell item |
| &gt; or Right Arrow | Buy item |

### Options Screen
| Key | Function |
| --- | -------- |
| S or Up Arrow | Select previous row |
| X or Down Arrow | Select next row |
| Enter | Choose the selected row |
| F11 | Close the options, the same as the Back row |

Its rows are Save Commander, Load Commander, Game Settings, Engine Settings, Credits, Quit and Back. Save and Load are greyed out while in flight rather than hidden, so a row never moves.

The cursor is a ring: up from the first row lands on Back, which is the last, and down from Back returns to the first. **Back** returns to whichever screen the options were opened from — including the title and ship-parade screens, which have no other way back — and F11 pressed on the options does the same, so the key that opens them closes them.

**Credits** is a screen of its own, holding the version and who wrote the game; Enter or Escape returns to the options.

### Settings Screens
There are two, matching the two halves of the config file. Both are reached from the Options Screen (F11). Use the cursor keys to select a setting and Enter/Return to change it; every change takes effect immediately and is saved as it is made, so there is no save step. The cursor rings here too, so one press up from the first setting is the way to the Back row at the foot.

- **Game Settings** — how Elite itself looks and plays: planet style, sun style, planet descriptions and instant docking.
- **Engine Settings** — the settings shared by every game in the collection: graphic style (wireframe or solid), depth sort, shading, quantisation, font, music, sound effects, backend, window scale and rendition. The font row chooses between the rendition's own bitmap sheets, a Windows `.fon` and a TrueType face, and applies as soon as it is changed; a rendition that declares no font of the chosen kind keeps drawing with its own sheets.

The last three are marked `*` on the screen: the backend picks the rendering and audio implementation, the rendition picks everything the game draws with, its resolution included, and the window scale decides how far that is magnified into the window. All three are read before the game is built, so a change to any of them is saved now and picked up the next time the game starts.

## Configuration

Game settings are held in the `elite.sharp` file, stored in JSON format, in the user's application data directory (`%AppData%\The Sharp Kind` on Windows, `~/.config/The Sharp Kind` on Linux/macOS) — shared with [Stunt Car Racer - The Sharp Kind](scr-readme.md). Commander saves (`.cmdr` files) and logs (`logs\elite-*.log`, daily rolling, 7 kept by default) live in the same directory. If the config file is missing or invalid the game falls back to defaults.

Commander saves carry a `fileType` and a `version`, and name everything they hold — the goods in the hold and on the market by name, the lasers by the mount they are on, and the legal status by its band as well as its bounty — so a save can be read and hand-edited without a copy of the code beside it. Missions are keyed by name, each with its own stage (`"missions": { "Constrictor": { "stage": "Rewarded" } }`), so they are independent of each other and a mission added later is another key rather than a renumbering. A file the game could not itself have written, whether that is the wrong version, an unknown item of cargo or mission, a stage belonging to a different mission, a legal status whose band contradicts its bounty or a value out of range, is rejected and Commander Jameson is loaded instead. Saves written before the format was versioned are not loadable.

The file's `engine` element holds the settings shared by every game — the backend, the rendition, the frame rate and the graphic style among them — and is documented in the [main readme](../README.md#configuration). Elite's own settings sit alongside it under `game`, and can take the following values:

``` json
{
    "game": {
        "planetStyle": "Fractal",              // The render style of the filled planets (ignored when the engine's fillMode is Wireframe).  Solid or Striped or Fractal
        "sunStyle": "Gradient",                // The render style of the filled sun (ignored when the engine's fillMode is Wireframe).  Solid or Gradient
        "planetDescriptions": "TreeGrubs",     // Description style used for the planets.  TreeGrubs (BBC) or HoopyCasinos (MSX)
        "instantDock": false                   // When the docking computer is engaged, instantly dock (true) or let the auto pilot fly in (false)
    }
}
```

The Game Settings screen changes all of these live, and the engine's graphic style with them; every change is saved as it is made.

### Environment variables

Elite's own diagnostic opt-ins, read at runtime rather than compiled in, so they work in a Release build. The ones shared with Stunt Car Racer, `GAME_KEY_SCRIPT` and `GAME_FRAME_DUMP_DIR`, are in the [main readme](../README.md#environment-variables).

| Variable | Value | Effect |
| -------- | ----- | ------ |
| `ELITE_LOG_LEVEL` | A Serilog level name: `Verbose`, `Debug`, `Information`, `Warning`, `Error` or `Fatal` — not the config file's own `Trace`…`None` names. Case-insensitive; anything unparseable is ignored and the config value stands | Overrides `engine.logging.minimumLevel`, for when the config file itself is what needs debugging |
| `ELITE_DEBUG_COMMANDER` | Anything, including empty — only whether it is set matters | Starts with the fully equipped `Max` commander instead of `Jameson`, for exercising late-game equipment and cargo without a save file |
| `ELITE_DEBUG_MISSIONS` | Anything, including empty — only whether it is set matters | Enables Ctrl-M, which cycles the five mission briefings: Constrictor brief, Constrictor debrief, the two Thargoid briefs, the Thargoid debrief. Each jump cheats the commander into the state that briefing needs and leaves them mid-mission — the last two by moving them to the system it happens at — so restart rather than carrying on from one. Does nothing for a mission that is not installed |
| `ELITE_DEBUG_YAW` | Anything, including empty — only whether it is set matters | Adds a third flight axis. Q yaws left, W yaws right, and a joystick yaws on its twist. Elite has neither the control nor a dial for it — the ship rolls and pitches, and turns by rolling first — so this is a departure from the original, off unless asked for |

## Missions

Missions are plugins. Each is an assembly referencing
`EliteSharp.Abstractions` and nothing else, and the game loads every
one it finds in a **`Missions` folder beside the executable** at startup.

The two missions the game ships with — the Constrictor hunt and the Thargoid
plans run — are in `EliteSharp.Missions.Classic` and are found the same way, so
there is nothing a mission of your own cannot do that these can.

Two things follow from that, and they are the reason to leave the folder alone
unless you mean it:

- **Removing the folder removes the missions.** The game starts and plays, but
  no mission is ever offered.
- **A commander part-way through a mission needs that mission installed.**
  The commander file records the mission by name, and a file naming a mission
  nothing provides is refused rather than half-loaded. The log says which name
  it could not place.

## Goods

The stock market's wares are a plugin too, the same shape as a mission: an
assembly referencing `EliteSharp.Abstractions` and nothing else, found in a
**`Goods` folder beside the executable** at startup. `EliteSharp.Goods.Classic`
— the seventeen classic wares — is the one the game ships with, and it comes
through the same door a stranger's would: the plugin reads its own
`goods.json` from beside its assembly rather than hardcoding what it trades,
so a different economy is a different `goods.json` beside the same DLL.

Unlike a mission, and like a rendition, this folder is not optional:

- **The game needs exactly one set to trade.** No folder, or one with nothing
  recognisable in it, is a startup failure naming the folder; two installed
  sets is the same, naming both — the game trades one economy, and there is
  no sensible way to pick between them.
- **A good is named, not numbered.** As with a mission's stages, a closed
  enum cannot name a good the game was never built against.
- **Four pieces of wreckage need somewhere to put what they drop.** The
  Alloy, the Escape Capsule, the Rock Splinter and the Tharglet each leave a
  named good behind when scooped — Alloys, Slaves, Minerals and Alien Items —
  and a set that does not provide all four is refused at startup, naming
  which.
- **A commander's saved cargo has to match the installed set exactly**, by
  count and by name; a save from a different economy is rejected rather than
  half-loaded.

The market and inventory screens size themselves to whatever the installed
set trades rather than assuming the classic seventeen, so a bigger goods set
simply scrolls.

## Renditions

Everything you see is a plugin. A **rendition** is one interpretation of the
game — its screens, its HUD, the way it draws planets, suns, stars and ships,
and the artwork, palette, fonts and ship models it draws them with. Each is a
folder under **`Renditions` beside the executable**, holding an assembly and
its `Assets`, and the game finds them at startup the same way it finds
missions.

The two that ship stand in for 8-bit and 16-bit machines, but nothing about
the model requires that: a rendition can be futuristic, underwater or
psychedelic, at whatever resolution it likes, because it declares its own name,
its own screen size and its own colour limits. It references
`EliteSharp.Abstractions` and nothing else, so there is nothing these two can
do that one of yours cannot.

Unlike the missions, this folder is not optional:

- **The game picks the rendition named by `engine.rendition`.** Without it
  there is nothing to draw with, so it refuses to start and says which name it
  could not find. **Elite's own default is `8-bit`** — a fresh config, or
  `engine.rendition` left out entirely, starts there — which differs from the
  shared engine default of `16-bit` that a game with no rendition of its own,
  such as Stunt Car Racer, still uses.
- **A rendition missing a screen is refused the same way**, naming every
  screen it does not draw. The check happens before the first frame rather
  than when the commander opens that screen.
- **All installed renditions are offered on the Engine Settings screen**, so a
  commander can only switch to one that is really there. The change applies
  next time the game starts, since the window is made at the size the
  rendition asks for.
- **A rendition also says how far it may be magnified.** The window scales on
  offer are its own — `8-bit` offers 1, 2 and 4 (4 by default) against its
  320 x 256 canvas, `16-bit` 1 and 2 (2 by default) against its 640 x 512 —
  so the Engine Settings screen offers those, and a `windowScale` hand-edited
  into the config file is pegged to the nearest one the rendition allows.
- **Field of View** sets how much of the universe the viewport shows, in
  degrees. Widening it fits more on screen and makes everything in it smaller.
  53° is the original's own projection and is the default; it applies without
  restarting.

Files written before renditions existed say `"tier": "8Bit"`. That is read and
carried over to `"rendition": "EightBit"` the first time the file is saved, so
an old config keeps the look it had.

## Credits

'Elite - The Sharp Kind' re-engineered in C# by Andy Hawkins 2023-2026.
- Converted into C#/.NET from C.J.Pinder's C version.
- Forked from fesh0r/newkind 06 Dec 2022

'Elite - The New Kind' re-engineered in C by C.J.Pinder 1999-2001.
- christian@newkind.co.uk  |  www.newkind.co.uk
- Reverse engineered from the BBC disk version of Elite.
- Additional material by C.J.Pinder.
- Face information for the ships. Adapted from the Elite ship data published by Ian Bell.
- Alterations to vertex ordering by Thomas Harte. <T.Harte@excite.com>
- Routines for drawing anti-aliased lines and circles by T.Harte.
- Check for hidden surface supplied by T.Harte.

The original Elite code is (C) I.Bell & D.Braben 1984.

The BBC Micro original and Pinder's C rewrite are what this port is checked
against — see [reference-sources.md](reference-sources.md).

Gabriel Gambetta - Computer Graphics from Scratch
https://gabrielgambetta.com/computer-graphics-from-scratch/

The 8-bit rendition's `.fon` and TrueType fonts (`Bm437_Master_512.FON`,
`Mx437_Master_512.ttf`) are from VileR's Oldschool PC Font Resource
https://int10h.org/oldschool-pc-fonts/ - faithful reproductions of the
character ROMs of the machines of the period, which is why they are what the
rendition offers beside its own sheet.
