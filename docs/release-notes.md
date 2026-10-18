# Release Notes

## Unreleased

## v2.0.0 (2026-10-18)

### Breaking changes

- `SharpKind.Audio` no longer depends on NVorbis or MeltySynth — it decodes WAV and plays MIDI through a SoundFont 2 player of its own, so `.ogg` effects are no longer read
- Caesar cryptanalysis moved out of `SharpKind.Security.Cryptography` into the new `SharpKind.Security.Cryptography.Cryptanalysis` package
- `GenerateSettings` moved onto `ICipher`, and the Mono, Reflector and Homophonic settings now copy what they are given and compare by content

### Bubble Bobble

#### Features

- New game: a conversion of the Commodore 64 Bubble Bobble, at the C64's own 320x200, checked against the real game run on an emulator
- All 100 levels, one or two players, lives, extra lives from the score, the level timer, HURRY UP! and the Baron
- Special bubbles, water, the giant bubble, the bonus rooms, the super bonus room and the EXTEND fanfare
- The original SID music, played on an emulated SID chip
- Super mode: hold S, U, P, O, R and Tab on the title screen for the harder game

### Elite

#### Changes

- The 8-bit rendition sounds like the Commodore 64 version — its music and its sound effects play on an emulated SID chip, and the effects fall silent while the music plays, as they did there
- The 16-bit rendition's music is played by the game's own synthesiser rather than a library — the same two tunes, at the same level

- The joystick hat and throttle lever now work — the hat picks the cockpit view, and a SideWinder's lever sets the speed outright
- Controls can be rebound — every cockpit and view control is a line in `elite.controls.sharp`, written out on first run
- More than one controller can be plugged in, and the Engine Settings screen picks which one flies the ship
- A SideWinder's eight buttons are now all mapped — missiles, ECM, warp jump, the docking computer and hyperspace
- Missiles reach the stick — fire and target are on buttons, or the triggers on a pad
- Each stick gets its own button layout, because the three supported devices disagree about every button. See the tables in the Elite readme
- An analog joystick now flies by stick position — how far it is pushed is the rate of turn, reached and released at once, with none of the ramp a key climbs through. A digital stick keeps that ramp, which is the original's feel
- Ordered dithering costs nothing extra on solid faces — the same picture, at the undithered frame rate
- Gouraud shading draws about three times as fast — the same picture, cheaper colour matching and blending
- The 3D view no longer pays to check each pixel against its own edges — every fill in the universe is a little faster
- The label below the 8-bit scanner now reads "ELITE#"

#### Fixes

- On the 8-bit galactic chart, the fuel ring now covers every planet in range
- The chart cross-hairs move smoothly while a key is held, and the joystick moves them too
- Hyperspace can be started from the galactic and short-range charts, as in the original
- Dying during a hyperspace countdown no longer leaves the old destination on the status line after the restart
- The launch, docking and hyperspace rings now spread out to fill the whole view, corners included
- The launch, docking and hyperspace rings are octagons on the 8-bit rendition and sixteen-sided on the 16-bit, as the originals drew them
- Explosions are round — the debris cloud was a square box
- Explosion clouds are the same size relative to the ship on both renditions
- 8-bit left and right screen border drawn one pixel wide instead of two
- The ship parade's fire button now starts the game, as its prompt says it does
- 16-bit speed bar drawn short and one row out of its slot

### Engine

#### Features

- `SharpKind.Emulation`, a new library: a 6502 CPU, a SID chip and a PSID player, and both sound backends play SID songs
- An audio stack of its own — WAV, MIDI with SoundFont 2, and C64 SID, one format per tier; no third-party audio package
- A frame is a list of render layers — backdrop, world and overlay — in Elite and Stunt Car Racer

### Cryptography

#### Features

- New ciphers: ADFGVX, Affine, Autokey, Beaufort, Book, Columnar, Homophonic, Playfair and Rail Fence
- `SharpKind.Security.Cryptography.Cryptanalysis`, a new library — cracks Caesar, MonoAlphabetic and Homophonic ciphers by quadgram hill climbing; it solves the first three stages of Singh's cipher challenge
- `SharpKind.Cryptography`, a PowerShell 7.6 module over both libraries — encrypt, decrypt, make settings and crack keys from the command line; on the PowerShell Gallery and attached to the release

#### Fixes

- Enigma steps its rotors as the machine does, and each message starts from the key
- A `CryptoStream` that is written more than one character at a time gives the right text, keeps surrogate pairs whole and leaves the caller's streams open
- MonoAlphabetic no longer replaces soft hyphens and zero-width joiners with a letter
- Caesar keys are generated without bias and parsed the same in every culture

### Project

- Bubble Bobble is built and attached to the release for win-x64, linux-x64 and linux-arm64, like the other two games
- The documentation site is now generated by DocFX, and carries an API reference for every `SharpKind.*`, `EliteSharp.*` and Stunt Car Racer library
- The benchmark dashboard is published as part of that site, under Benchmarks, instead of as a site of its own

## v1.1.0 (2026-09-05)

### Elite

#### Features

- Yaw — a third flight axis, opt-in via `ELITE_DEBUG_YAW`, joystick-twist support
- Gouraud shading and flat per-face (Lambert) lighting on the 16-bit rendition
- Frustum culling and level-of-detail dropping for distant ships
- Elite now defaults to the 8-bit rendition
- Ships: 33 hardcoded classes replaced by one data table, `ships.json`
- Market wares become a plugin + JSON file; market and inventory screens now scroll

#### Fixes

- Intro screen was leaking chart keys and title music into the game
- Scanner stalk-to-blip gap
- Case-sensitive save-file loading on Linux
- A rejected save now says which field failed, and why

### Stunt Car Racer

#### Features

- Gamepad and joystick support — pad and one-stick joystick, live together
- Outside/chase camera view, on Backspace
- Mid-race 'M' returns to the track menu
- 'R' turns the car around
- Race pause (P/O) and debug freezes + stats overlay (F5/F6/F7)
- F9/F10 tune the physics frame gap live

#### Fixes

- A brief key press on the track-preview screen was sometimes ignored

### Engine

#### Features

- Gamepad and joystick support — shared input abstraction, hot-plug, both games
    - Speedlink Competition Pro Extra Joystick (USB)
    - Microsoft Wireless Controller for XBox One (Model 1708)
    - Microsoft Precision 2 Joystick (USB)
- Alpha blending (transparency) in both graphics backends
- Bilinear filtering and mipmaps for textured triangles
- One text rasteriser for both backends — Bitmap, `.fon` and TrueType font kinds, switchable live
- Game loop now runs at the configured frame rate, not a fixed 13.5 Hz tick
- Golden-trace and frame-check regression harness
- Goods (economy/market wares) become a plugin family, like missions and renditions
- Unit/integration test-level split; coverage measurement corrected

#### Fixes

- A startup failure now shows a dialog instead of silently crashing

## v1.0.0 (2026-08-09)

Initial release of Elite and Stunt Car Racer. Both games playable, but not fully feature complete and some known bugs remain.

### Elite

#### Features

- Renditions - Every screen given its own 8-bit and 16-bit layout
  - 8-bit, 320x256, 16-colour BBC Micro style
  - 16-bit, 640x512, 4096-colour Acorn Archimedes style
- Missions plugins
  - Constrictor hunt
  - Thargoid plans
  - Debug mission-briefing cycling, `ELITE_DEBUG_MISSIONS`
- Renditions plugins
  - Views, HUD and art as a swappable assembly
- Wireframe planet surface detail — equator, meridian, crater
- Lasers
    - Per-laser-type crosshairs and beam colours
    - Laser Style setting (wireframe/solid), independent of ship style
- Credits screen, working Back navigation out of Options, a Window Scale row
- Proper z-buffered ship rendering — fixes hidden surfaces showing through
- 100-polygon-per-frame cap removed

#### Fixes

- Missile detonation distance — was missing corner hits
- Mining/Military Laser tech-level gating
- Bounty hunters never turned hostile
- Police-spawn legal-status check was inverted
- Asteroid loot drops (splinters vs. alloy/cargo)
- Witchspace Thargoid ambush count
- Fuel-scooping rate, ~40x too fast
- Full-hold scooping always damaged the ship
- Legal-status band boundary (Offender/Fugitive)
- Pack-hunter selection excluded Cobra Mk III
- Hyperspace misjump chance, too low
- Docking Computer price
- Energy bomb sparing the Constrictor
- Sun rotating with the player's roll/pitch
- Contraband calculation — double-counted Slaves, ignored Narcotics

### Stunt Car Racer

#### Features

- Full conversion playable end to end — all eight original tracks
- Track menu, preview, race and game-over flow with orbiting camera
- Graphical cockpit dashboard — wheels, engine flare, damage crack, gauges
- Car mesh loaded as a `.obj` asset
- Opponent AI — scripted speeds, steering, collision, push/obstruct
- Amiga-derived fixed-point physics for car and opponent
- Z-buffered track rendering — fixes floating-track and spurious-triangle bugs
- Car wrecks at full damage
- Lap-time clock — current and best lap
- Per-effect sound volume, pitch and pan — damage-scaled creak, pitched off-road/scrape
- Persisted settings — music/effects on or off

### Engine (`SharpKind.*`)

- Shared engine libraries split out of both games — graphics, audio, input, assets, config, timing
- Cross-platform SDL3 backend: Windows, Linux x64, Linux ARM64
- Software (CPU) and Hardware (SDL-accelerated) rendering, switchable in config
- Shared software z-buffer polygon renderer for both games
- Custom BMP and PNG decoders — no third-party imaging library
- Multi-resolution tiers: 8-bit and 16-bit asset sets with their own palette, fonts and colour budget
- Shared near-plane clipping and real per-vertex depth testing
- Hidden-line removal for wireframe rendering
- Versioned JSON config — auto-repairs a bad value instead of resetting the whole file
- Window Scale setting — integer window magnification, independent of resolution
- Graphic style, depth-sort strategy and colour-quantisation settings
- Widget/control library (`SharpKind.UI`) — Label, ComboBox, settings screens built on it
- Shared composition root (`SharpKind.App`) — one DI-based startup for both games
- Structured logging to console + rolling daily file, level set by environment variable
- Seedable, injectable randomness shared by both games
- Headless test harnesses — scripted keyboard input and frame dumps, for both games
- Central NuGet package management and code-quality analyser gates
- Tag-driven semantic versioning and automated GitHub Releases (win-x64 / linux-x64 / linux-arm64)
- `SharpKind.*` libraries published to NuGet.org on release, via Trusted Publishing
- Coverage badge and benchmark-history tracking in CI
