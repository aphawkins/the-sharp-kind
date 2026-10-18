---
title: The Sharp Kind
---

# The Sharp Kind

[![Build and Package](https://github.com/aphawkins/the-sharp-kind/actions/workflows/build-and-package.yml/badge.svg)](https://github.com/aphawkins/the-sharp-kind/actions/workflows/build-and-package.yml)
![Line coverage](images/coverage-badge.svg)
[![Latest release](https://img.shields.io/github/v/release/aphawkins/the-sharp-kind)](https://github.com/aphawkins/the-sharp-kind/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/aphawkins/the-sharp-kind/blob/main/LICENSE)

Classic 8/16-bit computer games re-engineered in C# / .NET to look, feel and play like the originals while running cross-platform on modern systems.

A games engine built like a business application, that nobody asked for, but might be just what you need!

* Choose your era of game front end (8-bit, 16-bit, or Modern)
* Choose your graphics rendering style (wireframe, solid, shaded)
* Authentic sound and music (C64 SID, MIDI, WAVE)
* Keyboard, Controller, and Joystick support (analog and digital)

Source on GitHub: [aphawkins/the-sharp-kind](https://github.com/aphawkins/the-sharp-kind).

## Games

### [Elite - The Sharp Kind](elite-readme.md)

![Elite - The Sharp Kind - Screenshot](images/elite-screenshot-8-bit.png)![Elite - The Sharp Kind - Screenshot](images/elite-screenshot-16-bit.png)

Status: Playable, feature-complete - 8-bit and 16-bit versions

### [Bubble Bobble - The Sharp Kind](bb-readme.md)

![Bubble Bobble - The Sharp Kind - Screenshot](images/bb-screenshot-8-bit.png)

Status: Playable, feature-complete - 8-bit version

### [Stunt Car Racer - The Sharp Kind](scr-readme.md)

![Stunt Car Racer - The Sharp Kind - Screenshot](images/scr-screenshot-16-bit.png)

Status: Playable, preview.

## Getting started

Requires the [.NET SDK](https://dotnet.microsoft.com/download) (see `Directory.Build.props` for the target framework version).

```bash
# Elite
dotnet run --project src/elite/apps/EliteSharp

# Bubble Bobble
dotnet run --project src/bb/apps/BubbleBobbleSharp

# Stunt Car Racer
dotnet run --project src/scr/apps/StuntCarRacerSharp
```

SDL3 and its native libraries come inside their NuGet packages (win-x64, linux-x64 and linux-arm64), so there is nothing else to install: `dotnet run` or `dotnet build` is enough.

Tested platforms: Windows (x64), Ubuntu (x64), Raspberry Pi 4 (ARM64).

### Pick your era

A rendition isn't a skin: it's a whole machine's worth of personality. Pick **8-bit** for chunky pixels, sixteen colours and a SID chip singing its heart out. Pick **16-bit** for 4,096 colours, smooth shading and a SoundFont orchestra. **Modern** is on its way, and it throws the limits away.

| | `8-bit` | `16-bit` | `Modern` (work in progress) |
| --- | --- | --- | --- |
| Render resolution (Elite) | 320 x 256 | 640 x 512 | 1280 x 720 |
| Render resolution (Bubble Bobble) | 320 x 200 | - | - |
| Colours | 16 | 4,096 | 16.7 million |
| Fonts | 8 x 8 bitmap glyphs <br /> 40 columns x 32 rows | 32 x 32 Bitmap glyphs | TrueType or Bitmaps |
| Sound | C64 SID emulation | MIDI & SoundFont | WAV, MIDI, SID |
| Window scaling | 1, 2 or 4 | 1 or 2 | 1 |

## Configuration

Each game keeps its settings in a JSON file in the shared per-user data directory (`%AppData%\The Sharp Kind` on Windows, `~/.config/The Sharp Kind` on Linux/macOS), written with defaults on first run. The full list of settings is in the [project README](https://github.com/aphawkins/the-sharp-kind#configuration).

## SharpKind libraries

All three games are built on a shared set of engine libraries under `src/useful/` - graphics, audio, input, assets and the game loop - published as `SharpKind.*` NuGet packages.

## Documentation

- [Elite - The Sharp Kind](elite-readme.md)
- [Bubble Bobble - The Sharp Kind](bb-readme.md)
- [Stunt Car Racer - The Sharp Kind](scr-readme.md)
- [Architecture principles](architecture-principles.md)
- [Backlog](backlog-roadmap.md) - every open work item, bugs first
- [Changelog](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md)
- [Release notes](release-notes.md) - a concise, categorised summary of the changelog, split at v1.0.0
- [Release process](release-process.md) - how to cut a tagged release
- [Contributing](https://github.com/aphawkins/the-sharp-kind/blob/main/CONTRIBUTING.md)

## Licence

[MIT](https://github.com/aphawkins/the-sharp-kind/blob/main/LICENSE). Original game copyrights remain with their respective owners - see each game's readme for credits.
