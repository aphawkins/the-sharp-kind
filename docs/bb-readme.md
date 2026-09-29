# Bubble Bobble - The Sharp Kind

A C# port of 'Bubble Bobble', converted from the C64 disassembly of Software Creations' 1987 conversion of Taito's original arcade game.

The port shares the `SharpKind` libraries with 'Elite - The Sharp Kind' and 'Stunt Car Racer - The Sharp Kind': hardware access is hidden behind interfaces, with a software renderer drawing through SDL3.

Part of [The Sharp Kind](https://github.com/aphawkins/the-sharp-kind/blob/main/README.md), alongside [Elite - The Sharp Kind](elite-readme.md) and [Stunt Car Racer - The Sharp Kind](scr-readme.md). Conversion work is tracked in [bb-port-plan.md](bb-port-plan.md).

## Status

Work in progress. The game opens on the title screen and plays through the 100 levels: the players, the enemies, the bubbles (water, fire, thunder and EXTEND), the items, fruit, and the music from the C64's SID chip, which runs on the shared `SharpKind.Emulation` library. Not yet in: the bonus rounds, the super mode, the ending and some of the special items' effects. See [bb-port-plan.md](bb-port-plan.md).

The 56-line banner above the screen is house chrome and not part of the C64 game.

## Getting Started

To build and run from source, install the .NET SDK and run:

``` bash
dotnet run --project src/bb/apps/BubbleBobbleSharp
```

It can also be run and debugged directly from an IDE: open [TheSharpKind.slnx](https://github.com/aphawkins/the-sharp-kind/blob/main/TheSharpKind.slnx) in Visual Studio and set `BubbleBobbleSharp` as the startup project.

## Credits

'Bubble Bobble - The Sharp Kind' re-engineered in C# by Andy Hawkins 2026.
- Converted from a disassembly of Software Creations' C64 conversion.

Bubble Bobble is (C) Taito 1986. C64 conversion by Software Creations 1987.

The 8-bit rendition's music (`bubble-bobble.sid`) is the C64 version's theme, composed by Peter Clarke, 1987 Firebird. Sourced from the High Voltage SID Collection https://hvsc.c64.org/.
