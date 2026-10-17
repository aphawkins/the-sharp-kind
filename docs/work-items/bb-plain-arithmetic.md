# Bubble Bobble: plain coordinates, directions and random

[BubbleBobbleSharpLib] Movement uses byte wrap by mask (`& 0x07`, `& 0x80` for
"negative", `unchecked((byte)...)`), a forty-byte map stride with a row bias,
directions as `0x00`/`0x80`, and `BbRandom` reproduces the 6502 generator with
its carry flag.

Fix direction: `int` coordinates and a `Point`, a direction enum, and a seedable
`Random`. **[DECISION]** first: this gives up exact replay of the original's
quirks, which the golden tests and `bb-conversion-notes.md` record. Blocked by
the entity-struct item.
