"""Capture bubble draws from rebb64 under VICE, in the format of Views/Captures/objects-level-1.txt.

An INTERVENTION, and the captures say so: at each case's $E90E (the jsr at $0A48) one object slot is written with the type,
cell and shifts under test. Then, as SKILL.md's "Comparing pixels" has it, the bytes are read at the next $0A4B and the frame at
the $E90E after next (the screen is double buffered), as C64 colour numbers, raster line 50 as row 0, column 0 at x 136.

The 24x16 box is cut at left = column x 8 and top = Y - 45, the same cell the port draws. A case whose box a hardware sprite
overlaps, at either read, is left out and said so. Run `monitor.py start` first, and check it printed `in play`.

    python capture_objects.py OUT.txt            # the lot
    python capture_objects.py OUT.txt calibrate  # just the two cases the repo already has
"""

import sys
import time

from monitor import Monitor, OP_EXEC

DRAW = 0xE90E
BYTES = 0x0A4B
SLOT = 16
COLUMN, ROW = 0x0E, 0x08
BOX_W, BOX_H = 24, 16
FRAME_X, FRAME_Y = 136, 50

FIELDS = {  # name: base address
    "type": 0x00CA, "col": 0x00DC, "row": 0x00EE, "state": 0xA9B2, "subx": 0xA9C4, "suby": 0xA9D6,
    "behaviour": 0xA9E8, "flags": 0xA9FA, "x": 0xAA0C, "y": 0xAA1E, "variant": 0xAA30, "enemy": 0xAA42,
}


def next_stop(mon, timeout=10.0):
    """Let the machine run, and return the PC of the next stop of the two checkpoints armed for the whole run.

    The machine is left halted there. Commands send a STOPPED of their own at wherever the machine is, so everything kept
    before the resume is stale; only a hit that arrived while resume() waited for EXIT's reply is real.
    """
    mon.resume()
    body = mon._stops.pop(0) if mon._stops else mon.wait_for_stop(timeout=timeout)
    return body[0] | (body[1] << 8)


def hit(mon, address, timeout=10.0):
    """Run to the next stop at address (the other armed address is run past), halted there."""
    while True:
        pc = next_stop(mon, timeout)
        if pc == address:
            return pc


def y_for(row, suby):
    return 8 * row + suby + (8 if suby == 0 else 0) + 13


def sprites(mon):
    regs = mon.mem(0xD000, 0xD02E)
    return {"x": list(regs[0:16:2]), "y": list(regs[1:16:2]), "msb": regs[0x10], "on": regs[0x15],
            "yexp": regs[0x17], "xexp": regs[0x1D]}


def overlaps(spr, left, top):
    for n in range(8):
        if not spr["on"] >> n & 1:
            continue
        sx = spr["x"][n] | ((spr["msb"] >> n & 1) << 8)
        sx -= 24
        sy = spr["y"][n] - 50
        w = 24 * (2 if spr["xexp"] >> n & 1 else 1)
        h = 21 * (2 if spr["yexp"] >> n & 1 else 1)
        if sx < left + BOX_W and left < sx + w and sy < top + BOX_H and top < sy + h:
            return True
    return False


def one_case(mon, type_, subx, suby):
    row, col = ROW, COLUMN
    y = y_for(row, suby)
    x = col * 8 + 20 + 2 * subx
    hit(mon, DRAW)
    mon.set_mem(0x045A, [9])
    values = {"type": type_, "col": col, "row": row, "state": 0x88, "subx": subx, "suby": suby, "behaviour": 0x7D,
              "flags": 0x7D, "x": x, "y": y, "variant": 4, "enemy": 4}
    for name, base in FIELDS.items():
        mon.set_mem(base + SLOT, [values[name]])
    hit(mon, BYTES)
    seen = {name: mon.byte(base + SLOT) for name, base in FIELDS.items()}
    seen['type'] = type_  # the type the draw saw; by $0A4B a pop has stepped on
    before = sprites(mon)
    hit(mon, DRAW)
    hit(mon, DRAW)
    after = sprites(mon)
    width, height, pixels = mon.display()
    left, top = col * 8, y - 45
    rows = []
    for dy in range(BOX_H):
        start = (FRAME_Y + top + dy) * width + FRAME_X + left
        rows.append("".join("%x" % (b & 15) for b in pixels[start:start + BOX_W]))
    return seen, left, top, rows, overlaps(before, left, top) or overlaps(after, left, top)


def block(label, index, seen, left, top, rows):
    head = f"case {label} {index:02d} slot {SLOT}\n"
    head += (f"  type {seen['type']:02x} col {seen['col']:02x} row {seen['row']:02x} subx {seen['subx']:02x} "
             f"suby {seen['suby']:02x} x {seen['x']:02x} y {seen['y']:02x}\n")
    head += f"  screen {left},{top} {BOX_W}x{BOX_H}\n"
    return head + "".join(f"  {r}\n" for r in rows)


def cases():
    pops = [(t, sx, sy) for t in (0x3C, 0x3E, 0x40) for sx in range(4) for sy in (0, 2, 4, 6)]
    floating = [(t, sx, 0) for t in range(0, 0x16, 2) for sx in range(4)] + [(4, 3, 4), (4, 3, 6)]
    return pops + floating


def main(argv):
    out = argv[0]
    todo = [(4, 1, 0), (0x40, 0, 2)] if len(argv) > 1 and argv[1] == "calibrate" else cases()
    mon = Monitor()
    mon.require_game()
    written, skipped = [], []
    armed = [mon.set_checkpoint(DRAW, operation=OP_EXEC), mon.set_checkpoint(BYTES, operation=OP_EXEC)]
    try:
        for index, (type_, subx, suby) in enumerate(todo):
            state = mon.byte(0x00B2)
            if state == 0:
                print(f"player state ${state:02X} before case {index}: the game is no longer in play - stopping")
                break
            seen, left, top, rows, covered = one_case(mon, type_, subx, suby)
            tag = f"type {type_:02x} subx {subx} suby {suby}"
            if covered:
                skipped.append(tag)
                print(f"{index:3d} {tag}: a sprite covers the box - left out")
            else:
                written.append(block("pops", index, seen, left, top, rows))
                print(f"{index:3d} {tag}: ok, ${seen['type']:02X} at {left},{top}")
    finally:
        for number in armed:
            mon.delete_checkpoint(number)
        mon.close()
    with open(out, "w", newline="\n") as f:
        f.write("".join(written))
    print(f"{len(written)} written, {len(skipped)} left out -> {out}")
    time.sleep(0)


if __name__ == "__main__":
    main(sys.argv[1:])
