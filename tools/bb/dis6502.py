#!/usr/bin/env python3
"""Disassemble rebb64's memory image (rebb64-raw.prg, loaded at $0400) for a range, from the bytes.

The comments in rebb64's own sources are often wrong (a label that names the wrong zero-page byte, a "level 99" that is
$63), so a routine is read from here when it matters.

    python tools/bb/dis6502.py 2785 2808        # $2785 up to, not including, $2808
    python tools/bb/dis6502.py 2785 2808 -d     # and follow with a data dump

The image path comes from $REBB64_PRG, else C:\\code\\github\\zaidka\\rebb64\\build\\rebb64-raw.prg.
"""
import os
import sys

MODES = {
    "imp": 1, "acc": 1, "imm": 2, "zp": 2, "zpx": 2, "zpy": 2, "abs": 3, "abx": 3, "aby": 3,
    "ind": 3, "izx": 2, "izy": 2, "rel": 2,
}

OPS = {}


def add(name, base, modes):
    for mode, code in modes.items():
        OPS[code] = (name, mode)


ALU = {"ora": 0x01, "and": 0x21, "eor": 0x41, "adc": 0x61, "sta": 0x81, "lda": 0xA1, "cmp": 0xC1, "sbc": 0xE1}
for n, b in ALU.items():
    m = {"izx": b, "zp": b + 4, "imm": b + 8, "abs": b + 12, "izy": b + 16, "zpx": b + 20, "aby": b + 24, "abx": b + 28}
    if n == "sta":
        del m["imm"]
    add(n, b, m)

RMW = {"asl": 0x02, "rol": 0x22, "lsr": 0x42, "ror": 0x62, "dec": 0xC2, "inc": 0xE2}
for n, b in RMW.items():
    m = {"zp": b + 4, "abs": b + 12, "zpx": b + 20, "abx": b + 28}
    if n in ("asl", "rol", "lsr", "ror"):
        m["acc"] = b + 8
    add(n, b, m)

add("ldx", 0, {"imm": 0xA2, "zp": 0xA6, "abs": 0xAE, "zpy": 0xB6, "aby": 0xBE})
add("ldy", 0, {"imm": 0xA0, "zp": 0xA4, "abs": 0xAC, "zpx": 0xB4, "abx": 0xBC})
add("stx", 0, {"zp": 0x86, "abs": 0x8E, "zpy": 0x96})
add("sty", 0, {"zp": 0x84, "abs": 0x8C, "zpx": 0x94})
add("cpx", 0, {"imm": 0xE0, "zp": 0xE4, "abs": 0xEC})
add("cpy", 0, {"imm": 0xC0, "zp": 0xC4, "abs": 0xCC})
add("bit", 0, {"zp": 0x24, "abs": 0x2C})
add("jmp", 0, {"abs": 0x4C, "ind": 0x6C})
add("jsr", 0, {"abs": 0x20})
for n, c in {"bpl": 0x10, "bmi": 0x30, "bvc": 0x50, "bvs": 0x70, "bcc": 0x90, "bcs": 0xB0, "bne": 0xD0, "beq": 0xF0}.items():
    add(n, 0, {"rel": c})
for n, c in {"brk": 0x00, "php": 0x08, "clc": 0x18, "plp": 0x28, "sec": 0x38, "rti": 0x40, "pha": 0x48, "cli": 0x58,
             "rts": 0x60, "pla": 0x68, "sei": 0x78, "dey": 0x88, "txa": 0x8A, "tya": 0x98, "txs": 0x9A, "tay": 0xA8,
             "tax": 0xAA, "clv": 0xB8, "tsx": 0xBA, "iny": 0xC8, "dex": 0xCA, "cld": 0xD8, "inx": 0xE8, "nop": 0xEA,
             "sed": 0xF8}.items():
    add(n, 0, {"imp": c})


def load():
    path = os.environ.get("REBB64_PRG", r"C:\code\github\zaidka\rebb64\build\rebb64-raw.prg")
    data = open(path, "rb").read()
    base = data[0] | (data[1] << 8)
    ram = bytearray(65536)
    ram[base:base + len(data) - 2] = data[2:]
    return ram


def dis(ram, start, end):
    pc = start
    while pc < end:
        code = ram[pc]
        if code not in OPS:
            print(f"{pc:04X}  {code:02X}          .byte ${code:02X}")
            pc += 1
            continue
        name, mode = OPS[code]
        size = MODES[mode]
        b = ram[pc:pc + size]
        raw = " ".join(f"{x:02X}" for x in b)
        lo = b[1] if size > 1 else 0
        w = lo | (b[2] << 8) if size > 2 else lo
        if mode == "imp":
            op = ""
        elif mode == "acc":
            op = "a"
        elif mode == "imm":
            op = f"#${lo:02X}"
        elif mode == "rel":
            op = f"${(pc + 2 + (lo - 256 if lo > 127 else lo)) & 0xFFFF:04X}"
        elif mode in ("zp", "abs"):
            op = f"${w:0{2 if mode == 'zp' else 4}X}"
        elif mode in ("zpx", "abx"):
            op = f"${w:0{2 if mode == 'zpx' else 4}X},x"
        elif mode in ("zpy", "aby"):
            op = f"${w:0{2 if mode == 'zpy' else 4}X},y"
        elif mode == "ind":
            op = f"(${w:04X})"
        elif mode == "izx":
            op = f"(${lo:02X},x)"
        else:
            op = f"(${lo:02X}),y"
        print(f"{pc:04X}  {raw:<9}  {name} {op}")
        pc += size


if __name__ == "__main__":
    args = [a for a in sys.argv[1:] if not a.startswith("-")]
    ram = load()
    lo, hi = int(args[0], 16), int(args[1], 16)
    dis(ram, lo, hi)
    if "-d" in sys.argv:
        for a in range(lo, hi, 16):
            print(f"{a:04X}: " + " ".join(f"{ram[i]:02X}" for i in range(a, min(a + 16, hi))))
