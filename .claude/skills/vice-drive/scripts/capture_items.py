"""Capture the level's special item from rebb64 under VICE: the item drawn plain, and the `$18` item that flashes.

An INTERVENTION, said in the capture's header: on level 1 the special item is hidden (`$53` has bit 7 set), so at an $E90E stop
`$53` is written with its type, bit 7 clear (the lives are topped up too). The game's own `$1844` then draws its 2x2 block from the
characters `$46`-`$49` that the level's set-up copied in, and colours it from `$60`; `$2CB7` flips `$60` with EOR 5 each pass
while the type is `$18`. Colour RAM is not double buffered, so each read is at an $E90E stop with the cells' colour RAM beside the
frame on show. Run `monitor.py start` first and check it printed `in play`.

    python capture_items.py OUT.txt
"""

import sys

from monitor import Monitor, OP_EXEC
import capture_objects as c

SPECIAL = 1
CELL = 0x4F
STOPS = 6


def frame_box(mon, left, top, size=16):
    width, height, pixels = mon.display()
    rows = []
    for dy in range(size):
        start = (c.FRAME_Y + top + dy) * width + c.FRAME_X + left
        rows.append("".join("%x" % (b & 15) for b in pixels[start:start + size]))
    return rows


def read(mon):
    zp = mon.mem(0x4E, 0x60)
    offset = zp[CELL - 0x4E] | ((zp[0x51 - 0x4E] & 0x03) << 8)
    colour = list(mon.mem(0xD800 + offset, 0xD800 + offset + 1)) + list(mon.mem(0xD800 + offset + 40, 0xD800 + offset + 41))
    vic = mon.mem(0xD020, 0xD024)
    chars = mon.mem(0x4230, 0x424F)
    col, row = offset % 40, offset // 40
    rows = frame_box(mon, col * 8, row * 8)
    return zp, offset, colour, vic, chars, rows


def block(label, index, type_, zp, offset, colour, shown, vic, chars, rows):
    col, row = offset % 40, offset // 40
    out = f"case {label} {index:02d} type {type_:02x} offset {offset:04x} colour {shown[0] & 15:02x}\n"
    out += f"  zp 4E-60 {zp.hex()}\n"
    out += f"  D800+ {bytes(colour).hex()} shown {bytes(shown).hex()}\n"
    out += f"  chars 4230 {chars.hex()}\n"
    out += f"  D020-D024 {vic.hex()}\n"
    out += f"  screen {col * 8},{row * 8} 16x16\n"
    return out + "".join(f"  {r}\n" for r in rows)


def main(argv):
    mon = Monitor()
    mon.require_game()
    armed = [mon.set_checkpoint(c.DRAW, operation=OP_EXEC), mon.set_checkpoint(c.BYTES, operation=OP_EXEC)]
    text = ""
    try:
        for label, type_ in (("special", 0x00), ("flash", 0x18)):
            c.hit(mon, c.DRAW)
            mon.set_mem(0x045A, [9])
            mon.set_mem(0x53, [type_])
            shown = [0, 0, 0, 0]
            for index in range(STOPS):
                c.hit(mon, c.DRAW)
                zp, offset, colour, vic, chars, rows = read(mon)
                before, shown = shown, colour
                if index < 2:
                    continue  # the first stops let $1844 draw the block into both screens
                # The VIC reads colour RAM live, and $1844 writes it a frame into the pass: the frame on show at a stop
                # is the last one finished, which saw the colour the stop before it read.
                text += block(label, index, type_, zp, offset, colour, before, vic, chars, rows)
                print(f"{label} stop {index}: offset ${offset:04X} colour RAM {bytes(colour).hex()} $60=${zp[0x60 - 0x4E]:02X} $53=${zp[5]:02X}")
            c.hit(mon, c.DRAW)
            mon.set_mem(0x53, [0x80 | type_])
    finally:
        for number in armed:
            mon.delete_checkpoint(number)
        mon.close()
    with open(argv[0], "w", newline="\n") as f:
        f.write(text)


if __name__ == "__main__":
    main(sys.argv[1:])
