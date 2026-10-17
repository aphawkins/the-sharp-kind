# Bubble Bobble: a view model free of C64 hardware

[BubbleBobbleSharp.Abstractions] `ObjectsModel` carries a colour RAM byte
(default `0x0D`), `NotDrawn = 0xFF`, an `object-sprites.tga` entry and a
character-cell split. `EntityTable` has `SpriteEnable` and `SpriteExpand`
(`$D017`/`$D01D`), and the game exposes a `Border` colour.

Fix direction: models say what to draw (sprite id, position, tint, scale);
each rendition turns that into cells or sprites. Needed before any rendition
that is not 8-bit.
