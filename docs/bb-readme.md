# Bubble Bobble - The Sharp Kind

![Bubble Bobble - The Sharp Kind - Screenshot (8-bit)](images/bb-screenshot-8-bit.png)

A C# port of 'Bubble Bobble', converted from the C64 disassembly of Software Creations' 1987 conversion of Taito's original arcade game.

Part of [The Sharp Kind](https://github.com/aphawkins/the-sharp-kind/blob/main/README.md).

## Status

Playable from start to finish: title, story, all 100 levels, bonus rounds, super mode and the ending. The music is the C64's own, played on an emulated SID chip.

Not yet in: two special items' effects (the six-sprite giant and the super bonus room). Known bugs: the music restarts when a player loses a life, and popped water flows too low along platforms. See the [backlog](backlog-roadmap.md).

## Run

```bash
dotnet run --project src/bb/apps/BubbleBobbleSharp
```

## Controls

| Action | Player 1 | Player 2 |
| --- | --- | --- |
| Move (up jumps) | Arrow keys | W, A, S, D |
| Blow a bubble | Space | Q |
| Joystick or gamepad | First one plugged in | Second one |

- On the title screen, press 1 or 2 to start a game for that many players.
- Esc quits.
- Hold S, U, P, O, R and Tab together on the title screen for super mode, a harder game.

## Configuration

Settings are in `bubblebobble.sharp`; see the [main readme](https://github.com/aphawkins/the-sharp-kind/blob/main/README.md#configuration). Bubble Bobble has no settings of its own, and only the 8-bit rendition is installed. `BB_LOG_LEVEL` overrides the logging level.

## Credits

'Bubble Bobble - The Sharp Kind' re-engineered in C# by Andy Hawkins 2026, from a disassembly of Software Creations' C64 conversion.

Bubble Bobble is (C) Taito 1986. C64 conversion by Software Creations 1987.

Music (`bubble-bobble.sid`) by Peter Clarke, 1987 Firebird, from the [High Voltage SID Collection](https://hvsc.c64.org/).
