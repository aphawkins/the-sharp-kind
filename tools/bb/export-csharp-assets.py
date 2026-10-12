#!/usr/bin/env python3
"""
Export rebb64's data to the form The Sharp Kind's C# port reads.

The port needs the same level, zone and image data the C64 build does, but
not in the same shape: it has no 6502 to feed, and a byte layout chosen to
fit a ROM only obscures the data on this side.

The parsers are NOT re-implemented here. rebb64's own convert-levels.py,
convert-zone-data.py and convert-tga.py already parse its text and image
formats, and they are the ones its byte-exact build uses - so anything this
script exports is parsed by the same code that proves `make verify`. They
are imported from the checkout at run time. Only the output stage is ours.

This script lives here rather than in the rebb64 checkout because it is our
code, generating our assets; a checkout of someone else's project is no
place to keep it, and an upstream pull is where it would go missing.

Usage:
    python3 export-csharp-assets.py --rebb64 <path to a rebb64 checkout>

    --out defaults to the 8-bit rendition's Assets folder in this repo.
"""

import argparse
import importlib.util
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))

DEFAULT_OUT = os.path.join(
    REPO, "src", "bb", "libs", "BubbleBobbleSharp.Renditions.EightBit", "Assets"
)


# Set by load_converters() once the checkout's path is known.
DATA = None
SRC = None
levels_mod = None
zones_mod = None
tga_mod = None


def load_converters(rebb64):
    """
    Import rebb64's own converters from a checkout, and point DATA at its data.

    They are imported by path because their filenames carry hyphens, which no
    import statement would accept.
    """
    global DATA, SRC, levels_mod, zones_mod, tga_mod

    build = os.path.join(rebb64, "build")
    DATA = os.path.join(rebb64, "data")
    SRC = os.path.join(rebb64, "src")

    for required in (build, DATA, SRC):
        if not os.path.isdir(required):
            raise SystemExit(
                f"'{rebb64}' does not look like a rebb64 checkout: {required} is missing."
            )

    def load(filename, name):
        spec = importlib.util.spec_from_file_location(
            name, os.path.join(build, filename)
        )
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module

    levels_mod = load("convert-levels.py", "convert_levels")
    zones_mod = load("convert-zone-data.py", "convert_zone_data")
    tga_mod = load("convert-tga.py", "convert_tga")


# =============================================================================
# TGA output
# =============================================================================

# The sheets the game draws sprites from. Each is copied across as it stands -
# no packing, no recolouring - because the engine reads TGA directly and
# neither backend batches, so one file per sheet costs nothing. See
# docs/bb-port-plan.md, Phase 2.
SPRITE_SHEETS = [
    "bonus-cake.tga",
    "bonus-diamond.tga",
    "bonus-melon.tga",
    "bubble-dragon-in-bubble.tga",
    "bubble-masks.tga",
    "diamond-sprite.tga",
    "grumple-gromit.tga",
    "level-tiles.tga",
    "sidebars.tga",
    "software-sprites.tga",
    "sprites-game.tga",
]


def load_canonical_palette(out_dir):
    """
    The rendition's own palette.json, as a list of (r, g, b), by entry number.

    The sheets are written in these rather than in the colours rebb64's TGAs
    carry. A sheet's pixel value is a C64 entry number, not a colour - that is
    what MulticolourSheet repaints at load time - so the colour map is only a
    naming convention, and the one name that matters is the rendition's. Taking
    it from anywhere else means a sheet can disagree with the palette, and with
    PaletteNamesEveryColour declared that is a broken asset set rather than a
    cosmetic difference.
    """
    path = os.path.join(out_dir, "Palette", "palette.json")

    with open(path, encoding="utf-8") as f:
        palette = json.load(f)

    return [
        (
            int(palette[str(entry)][2:4], 16),
            int(palette[str(entry)][4:6], 16),
            int(palette[str(entry)][6:8], 16),
        )
        for entry in range(len(palette))
    ]


def write_tga(path, image, transparent_index, canonical):
    """
    Write a colour-mapped TGA with a 32-bit colour map.

    The source files carry a 24-bit map, which leaves no way to say that a
    colour index is not drawn at all. These are C64 multicolour sheets, where
    bit-pair 00 is the background showing through rather than a colour, so the
    index that stands for it is written with alpha 0 and the renderer skips it.
    Passing transparent_index=None keeps every entry opaque, which is what a
    font sheet wants: the engine treats a grid font's black as the key itself.

    The map written is the rendition's own, cut to the length the source used -
    see load_canonical_palette.
    """
    if len(image["palette"]) > len(canonical):
        raise SystemExit(
            f"{path} indexes {len(image['palette'])} colours, and the "
            f"rendition's palette names {len(canonical)}."
        )

    palette = canonical[: len(image["palette"])]

    header = bytearray(18)
    header[1] = 1  # colour map present
    header[2] = 1  # uncompressed, colour-mapped
    header[5] = len(palette) & 0xFF
    header[6] = (len(palette) >> 8) & 0xFF
    header[7] = 32  # colour map depth
    header[12] = image["width"] & 0xFF
    header[13] = (image["width"] >> 8) & 0xFF
    header[14] = image["height"] & 0xFF
    header[15] = (image["height"] >> 8) & 0xFF
    header[16] = 8  # one index per pixel
    header[17] = 0x20  # rows top to bottom

    body = bytearray()
    for index, (r, g, b) in enumerate(palette):
        alpha = 0 if index == transparent_index else 255
        body.extend((b, g, r, alpha))

    body.extend(image["pixels"])

    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(bytes(header) + bytes(body))


def export_images(out_dir, canonical):
    """Copy each sprite sheet across, with colour index 0 made transparent."""
    for name in SPRITE_SHEETS:
        image = tga_mod.parse_tga(os.path.join(DATA, name))
        write_tga(os.path.join(out_dir, "Images", name), image, 0, canonical)

    return len(SPRITE_SHEETS)


# =============================================================================
# Object sprites (bubbles and pop frames), composed from masked ROM data
# =============================================================================
#
# See docs/bb-port-plan.md, item 2c. $E779/sprite-composer.s draws a bubble or
# pop frame as three 8-pixel-wide character columns, sixteen rows tall, by
# `(screen AND mask) OR graphic` per byte - not a plain image copy, so the
# graphic and mask bytes have to be recombined here the way the 6502 would.
#
# Both bubble-masks.tga and software-sprites.tga are already exported as
# plain sheets (SPRITE_SHEETS above); this reads the same source images again,
# through rebb64's own convert_bubble_masks/convert_software_sprites, to get
# the exact ROM bytes the build produces, then decodes those bytes back into
# per-pixel values by the memory map docs/bb-port-plan.md records. It does not
# re-derive that map; it is copied from there.

OBJECT_CELL_WIDTH = 12
OBJECT_CELL_HEIGHT = 16
OBJECT_ENTRY_COUNT = 44
OBJECT_POP_FRAME_COUNT = 8

BUBBLE_MASKS_BASE = 0x8000
SOFTWARE_SPRITES_BASE = 0x8F00
ANIM_MASKS_BASE = 0x8960

# Graphic pointer for entry e (bb-port-plan.md, item 2c). Ranges give the
# first entry's address; every later entry in the range is $30 further on.
_ENTRY_GRAPHIC_RANGES = [
    (0, 12, 0x8000),
    (12, 15, 0x9080),
    (15, 20, 0x9110),
    (20, 24, 0x9200),
    (24, 27, 0x9480),
    (27, 32, 0x9510),
    (32, 38, 0x9600),
    (38, 43, 0x9720),
    (43, 44, 0x9810),
]

# The four AND-mask blocks entries 12+ cycle through, by e & 3.
_HIGH_ENTRY_MASKS = (0x83C0, 0x83F0, 0x8420, 0x8450)

# $3CE5 (entity-state-tables.s) points every pop frame's AND-mask operand at
# $8260 - entry 0's own third-column mask, which is entirely "keep
# background" since entry 0's small bubble never reaches that column. Composed
# through it, every pop frame is blank; see decode_pop_frame_cell for why pop
# frames are composed without any mask at all.


def _entry_graphic_addr(entry):
    for lo, hi, base in _ENTRY_GRAPHIC_RANGES:
        if lo <= entry < hi:
            return base + 0x30 * (entry - lo)
    raise ValueError(f"entry {entry} is out of range")


def _entry_mask_addr(entry):
    if entry < 12:
        return 0x8240 + 0x30 * entry
    return _HIGH_ENTRY_MASKS[entry & 3]


def _read_bytes(data, addr, base, length):
    offset = addr - base
    if offset < 0 or offset + length > len(data):
        raise SystemExit(
            f"address {addr:#06x} plus {length} bytes falls outside the "
            f"{length and base:#06x}-based block ({len(data)} bytes long)."
        )
    return data[offset : offset + length]


def _block_bytes(addr, bubble_masks, software_sprites):
    """The 48 bytes (three 16-byte columns) at a graphic/mask address."""
    if BUBBLE_MASKS_BASE <= addr < BUBBLE_MASKS_BASE + len(bubble_masks):
        return _read_bytes(bubble_masks, addr, BUBBLE_MASKS_BASE, 48)
    if SOFTWARE_SPRITES_BASE <= addr < SOFTWARE_SPRITES_BASE + len(software_sprites):
        return _read_bytes(software_sprites, addr, SOFTWARE_SPRITES_BASE, 48)
    raise SystemExit(f"address {addr:#06x} is in neither known ROM block.")


def _bit_pairs(byte):
    """A byte's four 2-bit fields, most significant first."""
    return [(byte >> shift) & 0x03 for shift in (6, 4, 2, 0)]


def parse_bubble_anim_masks(src):
    """
    bubble_anim_masks ($8960-$8AFF, sprites2-tables.s): 26 blocks of 16 bytes,
    already plain 2-bit-per-pixel multicolour bytes (unlike the bit-per-pixel
    bubble-masks.tga/software-sprites.tga data). It is source-only - no build
    artefact carries it - so it is parsed straight out of the .s file.
    """
    path = os.path.join(src, "sprites2-tables.s")
    with open(path, encoding="utf-8") as f:
        text = f.read()

    start = text.index("bubble_anim_masks:") + len("bubble_anim_masks:")
    end = text.index("Region 3", start)
    body = text[start:end]

    blocks = []
    for line in body.splitlines():
        line = line.split(";", 1)[0].strip()
        if line.startswith(".res"):
            # `.res 16, $00` - 16 repeats of one byte.
            args = line[len(".res") :].split(",")
            count = int(args[0].strip())
            value = int(args[1].strip().lstrip("$"), 16)
            blocks.append(bytes([value]) * count)
        elif line.startswith(".byte"):
            args = line[len(".byte") :].split(",")
            blocks.append(bytes(int(a.strip().lstrip("$"), 16) for a in args))
        elif line:
            raise SystemExit(f"Unexpected line in bubble_anim_masks: {line!r}")

    data = b"".join(blocks)
    if len(data) != 26 * 16:
        raise SystemExit(
            f"bubble_anim_masks parsed to {len(data)} bytes, expected {26 * 16}."
        )

    return data


def decode_object_cell(graphic48, mask48, report):
    """
    Compose one entry's or pop frame's 12x16 cell from its 48-byte graphic and
    mask blocks (three 16-byte columns, one byte per row).

    Per bit-pair: mask 11 keeps the background, drawn here as index 0
    (transparent, matching every other sheet) regardless of the graphic -
    that a masked-out pixel still carries leftover graphic data is normal,
    since hiding it is what the mask is for. Mask 00 takes the graphic,
    drawn as the graphic's own value, or index 4 (opaque black - a colour no
    level ever repaints) when that value is 0. Any other mask value is data
    this rule cannot explain, so it is counted in `report` rather than
    guessed at.
    """
    pixels = [[0] * OBJECT_CELL_WIDTH for _ in range(OBJECT_CELL_HEIGHT)]

    for row in range(OBJECT_CELL_HEIGHT):
        for col in range(3):
            g_vals = _bit_pairs(graphic48[col * 16 + row])
            m_vals = _bit_pairs(mask48[col * 16 + row])

            for i in range(4):
                g, m = g_vals[i], m_vals[i]

                if m == 0x03:
                    pixel = 0
                elif m == 0x00:
                    pixel = g if g != 0 else 4
                else:
                    report["mixed_mask"] += 1
                    pixel = 0

                pixels[row][col * 4 + i] = pixel

    return pixels


def decode_pop_frame_cell(graphic48):
    """
    Compose a pop frame's 12x16 cell directly from its graphic bytes.

    $3CE5 (entity-state-tables.s) sets every AND-mask operand to the same
    $8260 for a pop frame - which turns out to be entry 0's own third-column
    mask, entirely "keep background" (that column is unused by entry 0's
    small bubble). Composited through it, every pop frame would be blank,
    which the game never is. bubble_anim_masks' own bytes only ever hold 0
    or 3 per bit-pair (checked against every frame), which is what a
    direct-copy graphic looks like: 0 draws nothing, 3 draws colour 3 -
    exactly the spinning-spark shape a bubble's pop animation should be. So
    a pop frame is composed straight from the graphic, with no masking step.
    """
    pixels = [[0] * OBJECT_CELL_WIDTH for _ in range(OBJECT_CELL_HEIGHT)]

    for row in range(OBJECT_CELL_HEIGHT):
        for col in range(3):
            g_vals = _bit_pairs(graphic48[col * 16 + row])
            for i in range(4):
                pixels[row][col * 4 + i] = g_vals[i]

    return pixels


# The enemy shots and the Baron (bb-port-plan.md, item 2d(i)), after the pop
# frames. A vector's third column is $8020 through $8260, as $3EB6 sets it.
_THIRD_GRAPHIC = 0x8020
_THIRD_MASK = 0x8260

# $E767 (the Baron, $2E/$30) and $E752 ($2A): entries of $AA54/$AAC8.
_SHOT_TABLE_ENTRIES = (48, 49, 50, 51, 56, 57)
_SHOT_TABLE_GRAPHICS = {48: 0x8F00, 49: 0x8F30, 50: 0x8F60, 51: 0x8F90, 56: 0x9840, 57: 0x9870}
_SHOT_TABLE_MASKS = {48: 0x8FC0, 49: 0x8FF0, 50: 0x9020, 51: 0x9050, 56: 0x93C0, 57: 0x9420}

# $3EFD ($28), $3ED4 ($2C), each by $A9C4 0-3, then $3E77 ($32).
_SHOT_COLUMNS = (
    [(0x99E0 + 0x20 * y, 0x99F0 + 0x20 * y, 0x9A60 + 0x20 * y, 0x9A70 + 0x20 * y) for y in range(4)]
    + [(0x98A0 + 0x20 * y, 0x98B0 + 0x20 * y, 0x9940 + 0x20 * y, 0x9950 + 0x20 * y) for y in range(4)]
    + [(0x9920, 0x9930, 0x99C0, 0x99D0)]
)


# $E758 ($24, the fire drop): entry $34 | $A9C4 of $AA54/$AAC8, four cells, then $3E94 ($26, the bolt), one cell, whose
# columns are soft_spr_0x3c0 to 0x3f0 ($8F00 + the offset).
_DROP_TABLE_GRAPHICS = (0x9300, 0x9330, 0x9360, 0x9390)
_DROP_TABLE_MASKS = (0x93C0, 0x93F0, 0x9420, 0x9450)
_BOLT_COLUMNS = (0x92C0, 0x92D0, 0x92E0, 0x92F0)

# $0EC5 ($4C, the fruit that rains after a bonus level) is drawn by $3F88 from the item art $3517 loads into $4230, with
# the mask $3576 makes of it: transparent where the art is 00. One cell for each of the $3A blocks (ITEM_BLOCK_COUNT), the
# two columns of the art and an empty third.


def _column_bytes(addr, bubble_masks, software_sprites):
    """The 16 bytes (one column, one byte a row) at a graphic/mask address."""
    if BUBBLE_MASKS_BASE <= addr < BUBBLE_MASKS_BASE + len(bubble_masks):
        return _read_bytes(bubble_masks, addr, BUBBLE_MASKS_BASE, 16)
    if SOFTWARE_SPRITES_BASE <= addr < SOFTWARE_SPRITES_BASE + len(software_sprites):
        return _read_bytes(software_sprites, addr, SOFTWARE_SPRITES_BASE, 16)
    raise SystemExit(f"address {addr:#06x} is in neither known ROM block.")


def export_object_sprites(out_dir, canonical):
    """
    Compose object-sprites.tga: entries 0-43 (bubbles and lightning), the
    eight pop-animation frames, then the shots and the Baron, the fire drop's
    four cells and the bolt, one 12x16 cell each, laid out in a single row.
    """
    bubble_masks_image = tga_mod.parse_tga(os.path.join(DATA, "bubble-masks.tga"))
    software_sprites_image = tga_mod.parse_tga(os.path.join(DATA, "software-sprites.tga"))

    bubble_masks = tga_mod.convert_bubble_masks(bubble_masks_image, 8, 16, 0)
    software_sprites = tga_mod.convert_software_sprites(software_sprites_image, 4, 16, 0)

    anim_masks = parse_bubble_anim_masks(SRC)

    report = {"mixed_mask": 0}
    cells = []

    for entry in range(OBJECT_ENTRY_COUNT):
        graphic48 = _block_bytes(
            _entry_graphic_addr(entry), bubble_masks, software_sprites
        )
        mask48 = _block_bytes(_entry_mask_addr(entry), bubble_masks, software_sprites)
        cells.append(decode_object_cell(graphic48, mask48, report))

    for frame in range(OBJECT_POP_FRAME_COUNT):
        block_start = (0x20 + 0x30 * frame) // 16
        graphic48 = anim_masks[block_start * 16 : block_start * 16 + 48]
        cells.append(decode_pop_frame_cell(graphic48))

    for entry in _SHOT_TABLE_ENTRIES:
        graphic48 = _block_bytes(_SHOT_TABLE_GRAPHICS[entry], bubble_masks, software_sprites)
        mask48 = _block_bytes(_SHOT_TABLE_MASKS[entry], bubble_masks, software_sprites)
        cells.append(decode_object_cell(graphic48, mask48, report))

    for g1, g2, m1, m2 in _SHOT_COLUMNS:
        graphic48 = b"".join(
            _column_bytes(a, bubble_masks, software_sprites) for a in (g1, g2, _THIRD_GRAPHIC)
        )
        mask48 = b"".join(
            _column_bytes(a, bubble_masks, software_sprites) for a in (m1, m2, _THIRD_MASK)
        )
        cells.append(decode_object_cell(graphic48, mask48, report))

    for graphic_addr, mask_addr in zip(_DROP_TABLE_GRAPHICS, _DROP_TABLE_MASKS):
        graphic48 = _block_bytes(graphic_addr, bubble_masks, software_sprites)
        mask48 = _block_bytes(mask_addr, bubble_masks, software_sprites)
        cells.append(decode_object_cell(graphic48, mask48, report))

    g1, g2, m1, m2 = _BOLT_COLUMNS
    graphic48 = b"".join(
        _column_bytes(a, bubble_masks, software_sprites) for a in (g1, g2, _THIRD_GRAPHIC)
    )
    mask48 = b"".join(
        _column_bytes(a, bubble_masks, software_sprites) for a in (m1, m2, _THIRD_MASK)
    )
    cells.append(decode_object_cell(graphic48, mask48, report))

    software_sprites_for_items = software_sprites

    for block in range(ITEM_BLOCK_COUNT):
        data = _read_bytes(
            software_sprites_for_items,
            ITEMS_ROM + ITEM_BLOCK_BYTES * block,
            SOFTWARE_SPRITES_BASE,
            ITEM_BLOCK_BYTES,
        )
        cell = [[0] * OBJECT_CELL_WIDTH for _ in range(OBJECT_CELL_HEIGHT)]

        for row in range(OBJECT_CELL_HEIGHT):
            for col in range(2):
                for i, value in enumerate(_bit_pairs(data[col * 16 + row])):
                    cell[row][col * 4 + i] = value

        cells.append(cell)

    if report["mixed_mask"]:
        raise SystemExit(
            "object-sprites.tga: the AND/OR masking rule does not explain "
            f"this data - {report['mixed_mask']} mixed mask bit-pairs (01/10). "
            "Not guessing; the rule or the addresses need checking against "
            "docs/bb-port-plan.md, item 2c."
        )

    width = OBJECT_CELL_WIDTH * len(cells)
    pixels = bytearray(width * OBJECT_CELL_HEIGHT)
    for index, cell in enumerate(cells):
        for row in range(OBJECT_CELL_HEIGHT):
            for x in range(OBJECT_CELL_WIDTH):
                pixels[row * width + index * OBJECT_CELL_WIDTH + x] = cell[row][x]

    image = {
        "width": width,
        "height": OBJECT_CELL_HEIGHT,
        "pixels": pixels,
        "palette": [None] * 5,
    }

    # Index 4 is not a real palette entry - it borrows canonical[0]'s colour
    # (the game's own colour 0, black) but keeps its own alpha, so it stays
    # opaque black where index 0 in the same sheet is transparent. See
    # decode_object_cell.
    object_canonical = list(canonical[:4]) + [canonical[0]]

    write_tga(
        os.path.join(out_dir, "Images", "object-sprites.tga"),
        image,
        0,
        object_canonical,
    )

    return len(cells)


# =============================================================================
# Level items (the bonus food and the special item), drawn as characters
# =============================================================================
#
# See docs/bb-port-plan.md, item 2e. $2B31 copies 32 bytes from
# sprites_rom + index x 32 ($9AE0, inside software-sprites.bin) into four
# characters, and $1844 puts them on the screen as a 2x2 block in column order:
# top-left, bottom-left, top-right, bottom-right. So a block is two 16-byte
# columns, one byte a row - the software sprites' own layout, two columns wide.
#
# The indices are $A892 (food) and $A8C1 (special item); the highest is $39. The bonus rooms of items 16, 17 and 18 (`$2F8D`)
# draw blocks $3F, $41 and $40 (`$A2C0`, `$A300`, `$A2E0`), so the sheet runs on to $41.

ITEM_CELL_WIDTH = 8
ITEM_CELL_HEIGHT = 16
ITEM_BLOCK_BYTES = 32
ITEM_BLOCK_COUNT = 0x42
ITEMS_ROM = 0x9AE0


def export_item_chars(out_dir, canonical):
    """
    Write item-chars.tga: blocks 0-$39 from sprites_rom, one 8x16 multicolour
    cell each, in a single row. Entry 00 is the screen's background, which the
    item's characters draw over whatever was in the cell - so it is index 4,
    opaque black, as in object-sprites.tga, not transparent.
    """
    image = tga_mod.parse_tga(os.path.join(DATA, "software-sprites.tga"))
    software_sprites = tga_mod.convert_software_sprites(image, 4, 16, 0)

    width = ITEM_CELL_WIDTH * ITEM_BLOCK_COUNT
    pixels = bytearray(width * ITEM_CELL_HEIGHT)

    for block in range(ITEM_BLOCK_COUNT):
        data = _read_bytes(
            software_sprites,
            ITEMS_ROM + ITEM_BLOCK_BYTES * block,
            SOFTWARE_SPRITES_BASE,
            ITEM_BLOCK_BYTES,
        )
        for row in range(ITEM_CELL_HEIGHT):
            for col in range(2):
                for i, value in enumerate(_bit_pairs(data[col * 16 + row])):
                    x = block * ITEM_CELL_WIDTH + col * 4 + i
                    pixels[row * width + x] = value if value != 0 else 4

    write_tga(
        os.path.join(out_dir, "Images", "item-chars.tga"),
        {"width": width, "height": ITEM_CELL_HEIGHT, "pixels": pixels, "palette": [None] * 5},
        0,
        list(canonical[:4]) + [canonical[0]],
    )

    return ITEM_BLOCK_COUNT


GIANT_IMAGE = 0xA220
GIANT_MASK = 0xA270
GIANT_BLOCKS = 10

# `$A82E`: where each of the giant bubble's ten characters goes in its four by four cells, as the screen offset (40 to a row)
# from the cell its object is in (the object's row less two). `$3F28` fills the table from the last: block 0 is the last entry.
GIANT_CELLS = [0x79, 0x78, 0x53, 0x52, 0x51, 0x2A, 0x29, 0x28, 0x03, 0x02]
GIANT_SIDE = 4


def export_giant_bubble(out_dir, canonical):
    """
    Write giant-bubble.tga: the item of type 7's picture, four cells by four of 4 by 8 multicolour pixels. `$3F28` draws
    it as `(screen AND mask) OR graphic` over ten of the cells, so where the mask keeps the screen the pixel is
    transparent (index 0) and the level shows through, and the six cells it does not touch are transparent throughout.
    """
    image = tga_mod.parse_tga(os.path.join(DATA, "software-sprites.tga"))
    software_sprites = tga_mod.convert_software_sprites(image, 4, 16, 0)

    graphic = _read_bytes(software_sprites, GIANT_IMAGE, SOFTWARE_SPRITES_BASE, GIANT_BLOCKS * 8)
    mask = _read_bytes(software_sprites, GIANT_MASK, SOFTWARE_SPRITES_BASE, GIANT_BLOCKS * 8)

    width = GIANT_SIDE * 4
    height = GIANT_SIDE * 8
    pixels = bytearray(width * height)

    for block in range(GIANT_BLOCKS):
        offset = GIANT_CELLS[GIANT_BLOCKS - 1 - block]
        cell_x, cell_y = offset % 40, offset // 40

        for row in range(8):
            for i, (value, keep) in enumerate(
                zip(_bit_pairs(graphic[block * 8 + row]), _bit_pairs(mask[block * 8 + row]))
            ):
                if keep not in (0, 3):
                    raise SystemExit(f"giant bubble block {block} row {row} has a mask of {keep}, not all or none.")

                x = cell_x * 4 + i
                y = cell_y * 8 + row
                pixels[y * width + x] = 0 if keep == 3 else (value if value != 0 else 4)

    write_tga(
        os.path.join(out_dir, "Images", "giant-bubble.tga"),
        {"width": width, "height": height, "pixels": pixels, "palette": [None] * 5},
        0,
        list(canonical[:4]) + [canonical[0]],
    )

    return GIANT_BLOCKS


# =============================================================================
# Font
# =============================================================================

# charset.tga is a 14x10 grid in the game's own order, which is not ASCII: the
# digits come first, and the letters happen to land where ASCII minus 32 would
# put them. The engine's BitmapFont reads a sheet of sixteen columns from
# character 0, a character's cell at (c & 0xF, c >> 4), so the sheet is
# rewritten by character code and the cells nothing maps to are left blank.
CHARSET_COLUMNS = 14
FONT_COLUMNS = 16
FONT_ROWS = 8
CELL = 8

# The HUD marks a player's remaining lives with screen code $1D, which the
# routine at $046C writes seven times over. It is a charset cell like any
# other, so it travels in the font sheet rather than in an image of its own -
# that is what lets the view colour it the way colour RAM does, since a grid
# font's white ink takes whatever colour it is drawn in.
#
# It is not an ASCII character, so it takes the sheet's last cell, which ASCII
# spells underscore and this game never prints. LIFE_ICON is the character the
# view asks for; LIFE_ICON_CELL is where it comes from in the charset.
LIFE_ICON = "_"
LIFE_ICON_CELL = 0x1D


def charset_source_cells():
    """Character code -> the cell in charset.tga holding it."""
    cells = {}

    for digit in range(10):
        cells[ord("0") + digit] = digit

    for letter in range(26):
        cells[ord("A") + letter] = 33 + letter

    cells[ord(LIFE_ICON)] = LIFE_ICON_CELL

    # The title screen's credits end a line with screen code $3B and wrap the last in $3C, which are a full stop
    # and an exclamation mark. The story ($AC41) writes a comma as $3E and an apostrophe as $3F.
    cells[ord(".")] = 0x3B
    cells[ord("!")] = 0x3C
    cells[ord(",")] = 0x3E
    cells[ord("'")] = 0x3F

    return cells


def export_font(out_dir, canonical):
    """charset.tga -> a grid font sheet in the order BitmapFont reads."""
    source = tga_mod.parse_tga(os.path.join(DATA, "charset.tga"))
    cells = charset_source_cells()

    width = FONT_COLUMNS * CELL
    height = FONT_ROWS * CELL
    pixels = bytearray(width * height)

    for index, cell in cells.items():
        src_x = (cell % CHARSET_COLUMNS) * CELL
        src_y = (cell // CHARSET_COLUMNS) * CELL
        dst_x = (index % FONT_COLUMNS) * CELL
        dst_y = (index // FONT_COLUMNS) * CELL

        for y in range(CELL):
            for x in range(CELL):
                value = tga_mod.get_pixel(source, src_x + x, src_y + y)
                pixels[(dst_y + y) * width + dst_x + x] = value

    font = {
        "width": width,
        "height": height,
        "pixels": pixels,
        "palette": source["palette"],
    }

    # Opaque: a grid sheet's black background is the transparency key the
    # renderer already knows, so an alpha channel would only fight it.
    write_tga(os.path.join(out_dir, "Fonts", "charset.tga"), font, None, canonical)

    return len(cells)


# The game's two small fonts, both four double-width pixels by eight, are font sheets in the layout BitmapFont reads
# now: sixteen columns from character 0, a character's cell at (c & 0xF, c >> 4). White ink on opaque black, as the
# charset's is, so the view draws them in whatever colour it asks for.
SMALL_FONT_CELL_WIDTH = 4
SMALL_FONT_ROWS = 6
INK = 1

# digit-font.tga: twelve 5x8 glyphs, column-major ($38BB copies each glyph's five bytes into rows 0-4 of characters
# $1A and $1B), and each byte is read as four multicolour pixels. Glyphs 10 and 11 are level 100's squeezed "10" and
# "0" ($38E3), which SidebarModel asks for as '[' and ']'.
DIGIT_FONT_WIDTH = 5
LEVEL_NUMBER_CHARACTERS = "0123456789[]"

# hud-font.tga: twenty 4x8 multicolour glyphs, ten to a row ($FE90): the digits, R O U N D E A Y, a double
# exclamation mark and a blank, which the ROUND and READY!! text ($3C01) spells with.
HUD_FONT_COLUMNS = 10
HUD_FONT_CHARACTERS = "0123456789ROUNDEAY! "


def _small_font(glyphs):
    """{character: 8 rows of 4 booleans} -> a font sheet image."""
    width = FONT_COLUMNS * SMALL_FONT_CELL_WIDTH
    height = SMALL_FONT_ROWS * CELL
    pixels = bytearray(width * height)

    for character, rows in glyphs.items():
        code = ord(character)
        dst_x = (code % FONT_COLUMNS) * SMALL_FONT_CELL_WIDTH
        dst_y = (code // FONT_COLUMNS) * CELL

        for y, row in enumerate(rows):
            for x, lit in enumerate(row):
                if lit:
                    pixels[(dst_y + y) * width + dst_x + x] = INK

    return {"width": width, "height": height, "pixels": pixels, "palette": [(0, 0, 0), (255, 255, 255)]}


def export_level_number_font(out_dir, canonical):
    """digit-font.tga -> the level number's font sheet, each glyph turned on its side as the game copies it."""
    source = tga_mod.parse_tga(os.path.join(DATA, "digit-font.tga"))
    glyphs = {}

    for index, character in enumerate(LEVEL_NUMBER_CHARACTERS):
        rows = []

        for column in range(DIGIT_FONT_WIDTH):
            bits = [tga_mod.get_pixel(source, index * DIGIT_FONT_WIDTH + column, y) != 0 for y in range(CELL)]

            # A multicolour pixel is a pair of bits; the font only ever sets both or neither.
            rows.append([bits[2 * x] or bits[(2 * x) + 1] for x in range(SMALL_FONT_CELL_WIDTH)])

        glyphs[character] = rows

    write_tga(os.path.join(out_dir, "Fonts", "level-number.tga"), _small_font(glyphs), None, canonical)

    return len(glyphs)


def export_hud_font(out_dir, canonical):
    """hud-font.tga -> the ROUND and READY!! text's font sheet."""
    source = tga_mod.parse_tga(os.path.join(DATA, "hud-font.tga"))
    glyphs = {}

    for index, character in enumerate(HUD_FONT_CHARACTERS):
        src_x = (index % HUD_FONT_COLUMNS) * SMALL_FONT_CELL_WIDTH
        src_y = (index // HUD_FONT_COLUMNS) * CELL

        # The text is ORed into the characters under it, so only bit-pair 11, the colour RAM's colour, is drawn.
        glyphs[character] = [
            [tga_mod.get_pixel(source, src_x + x, src_y + y) == 3 for x in range(SMALL_FONT_CELL_WIDTH)]
            for y in range(CELL)
        ]

    write_tga(os.path.join(out_dir, "Fonts", "hud-font.tga"), _small_font(glyphs), None, canonical)

    return len(glyphs)


# =============================================================================
# Tile edges
# =============================================================================

# The level renderer draws a solid cell as the level's own tile with five other
# characters around it - screen codes $0A to $0F, the tile's right-hand edges
# and the shadow it casts on the row below. Those six live in the charset
# rather than in a sheet of their own, so they are cut out of it here.
#
# They are drawn in multicolour: game-loop.s fills colour RAM with $0D before
# the level is rendered, and bit 3 of that is what puts every cell of the
# playfield in multicolour mode. charset.tga stores the charset the way
# convert-tga.py's convert_hires_chars() reads it, one TGA pixel per bit, so
# the eight bits of a row are re-read here as the four bit-pairs the hardware
# makes of them. That leaves a 4x8 cell - the same shape level-tiles.tga and
# sidebars.tga are already in, and the same doubling at draw time.
TILE_EDGE_FIRST = 0x0A
TILE_EDGE_COUNT = 6
TILE_EDGE_WIDTH = 4


def export_tile_edges(out_dir, canonical):
    """charset.tga's screen codes $0A-$0F -> a multicolour sheet of 4x8 cells."""
    source = tga_mod.parse_tga(os.path.join(DATA, "charset.tga"))
    palette = source["palette"]

    width = TILE_EDGE_COUNT * TILE_EDGE_WIDTH
    pixels = bytearray(width * CELL)

    for cell in range(TILE_EDGE_COUNT):
        src_x = ((TILE_EDGE_FIRST + cell) % CHARSET_COLUMNS) * CELL
        src_y = ((TILE_EDGE_FIRST + cell) // CHARSET_COLUMNS) * CELL

        for y in range(CELL):
            for x in range(TILE_EDGE_WIDTH):
                high = tga_mod.get_pixel(source, src_x + (x * 2), src_y + y)
                low = tga_mod.get_pixel(source, src_x + (x * 2) + 1, src_y + y)
                pair = (high << 1) | low

                # Every pair in these six is 00 or 01, so the charset's two
                # colours cover them. A third would mean the cells are not
                # what this reads them as, and guessing a colour for it would
                # be the wrong answer quietly.
                if pair >= len(palette):
                    raise SystemExit(
                        f"charset.tga cell {TILE_EDGE_FIRST + cell:#04x} has bit-pair "
                        f"{pair}, which its {len(palette)}-colour map has no entry for."
                    )

                pixels[(y * width) + (cell * TILE_EDGE_WIDTH) + x] = pair

    edges = {
        "width": width,
        "height": CELL,
        "pixels": pixels,
        "palette": palette,
    }

    write_tga(os.path.join(out_dir, "Images", "tile-edges.tga"), edges, 0, canonical)

    return TILE_EDGE_COUNT


# =============================================================================
# Effect characters (the water and the fire)
# =============================================================================
#
# The water ($1B40) draws screen code $1F, and the fire ($1319) $1C, $40 and
# $41, into the playfield's characters. Like the tile edges they are cut out of
# charset.tga and read as bit-pairs, but these use all four values, so the
# sheet has item-chars.tga's five entries: entry 00 is the screen's background,
# opaque black, as it is behind the items.

EFFECT_CHARS = (0x1C, 0x1F, 0x40, 0x41)


def export_effect_chars(out_dir, canonical):
    """charset.tga's screen codes $1C, $1F, $40 and $41 -> 4x8 multicolour cells."""
    source = tga_mod.parse_tga(os.path.join(DATA, "charset.tga"))

    width = len(EFFECT_CHARS) * TILE_EDGE_WIDTH
    pixels = bytearray(width * CELL)

    for cell, code in enumerate(EFFECT_CHARS):
        src_x = (code % CHARSET_COLUMNS) * CELL
        src_y = (code // CHARSET_COLUMNS) * CELL

        for y in range(CELL):
            for x in range(TILE_EDGE_WIDTH):
                high = tga_mod.get_pixel(source, src_x + (x * 2), src_y + y)
                low = tga_mod.get_pixel(source, src_x + (x * 2) + 1, src_y + y)
                pair = (high << 1) | low

                pixels[(y * width) + (cell * TILE_EDGE_WIDTH) + x] = pair if pair != 0 else 4

    chars = {"width": width, "height": CELL, "pixels": pixels, "palette": [None] * 5}

    write_tga(
        os.path.join(out_dir, "Images", "effect-chars.tga"),
        chars,
        0,
        list(canonical[:4]) + [canonical[0]],
    )

    return len(EFFECT_CHARS)


# =============================================================================
# The super bonus room's characters
# =============================================================================
#
# $3639 copies the 256 bytes from D_A660 (game-tables-1.s) over screen codes
# $60-$7F, running on through the tables after it, and $3644 then makes $76,
# $77 and $78 plain patterns on every row. They are plain multicolour bytes,
# so each is read as four bit-pairs. $3660 also puts the first bytes in $15-$1B,
# which $392A copies to $10-$14: those are $60-$64 again, and the view reads
# them from here. Entry 00 is the screen's background, drawn transparent.

SUPER_ROOM_CHARS = 0x20
SUPER_ROOM_PATCH = {0x16: 0x40, 0x17: 0x55, 0x18: 0x95}


def parse_super_room_bytes(src):
    """The 256 bytes from D_A660 on, through whatever labels follow it."""
    path = os.path.join(src, "game-tables-1.s")
    with open(path, encoding="utf-8") as f:
        lines = f.read().splitlines()

    start = next(i for i, line in enumerate(lines) if line.startswith("D_A660:"))
    data = bytearray()

    for line in lines[start + 1 :]:
        line = line.split(";", 1)[0].strip()

        if not line or line.endswith(":"):
            continue

        if not line.startswith(".byte"):
            raise SystemExit(f"Unexpected line after D_A660: {line!r}")

        data.extend(int(a.strip().lstrip("$"), 16) for a in line[len(".byte") :].split(","))

        if len(data) >= SUPER_ROOM_CHARS * CELL:
            break

    data = data[: SUPER_ROOM_CHARS * CELL]

    for cell, value in SUPER_ROOM_PATCH.items():
        data[cell * CELL : (cell + 1) * CELL] = bytes([value]) * CELL

    return data


def export_super_room_chars(out_dir, canonical):
    """D_A660, as $3639 and $3644 leave it in screen codes $60-$7F -> 4x8 multicolour cells."""
    data = parse_super_room_bytes(SRC)

    width = SUPER_ROOM_CHARS * TILE_EDGE_WIDTH
    pixels = bytearray(width * CELL)

    for cell in range(SUPER_ROOM_CHARS):
        for y in range(CELL):
            for x, pair in enumerate(_bit_pairs(data[(cell * CELL) + y])):
                pixels[(y * width) + (cell * TILE_EDGE_WIDTH) + x] = pair

    write_tga(
        os.path.join(out_dir, "Images", "super-room-chars.tga"),
        {"width": width, "height": CELL, "pixels": pixels, "palette": [None] * 4},
        0,
        canonical,
    )

    return SUPER_ROOM_CHARS


# =============================================================================
# EXTEND letters
# =============================================================================
#
# $E658 builds the letters down the side of the playfield into screen codes
# $4A-$5D, which $E6CD writes as one 2x2 block a letter (c, c+1 below it, c+2
# right of it, c+3 below that): E, X, T, N and D. Each block is the level's
# wall (chars $11-$14, which $3940 copies the wall into) ANDed with the letter
# bubble's mask, ORed with its art (bubble entries $18, $1C, $20, $24 and $28,
# through $AA54 and $AAC8 at $18 + 4n), then ANDed with $A988's circle.
#
# Entry 00 is where the wall shows through, so the view draws the wall first;
# entry 04 is opaque black, outside the circle or cleared by the mask.

EXTEND_LETTERS = 5
EXTEND_FIRST_ENTRY = 0x18

# $A988: the circle, the left column's sixteen rows and then the right's.
EXTEND_CIRCLE = bytes((
    0xFC, 0xF3, 0xCF, 0xCF, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0xCF, 0xCF, 0xF3, 0xFC,
    0x3F, 0xCF, 0xF3, 0xF3, 0xFC, 0xFC, 0xFC, 0xFC, 0xFC, 0xFC, 0xFC, 0xFC, 0xF3, 0xF3, 0xCF, 0x3F,
))


def export_extend_chars(out_dir, canonical):
    """The five EXTEND letters $E658 composes -> 8x16 multicolour blocks over the wall."""
    bubble_masks_image = tga_mod.parse_tga(os.path.join(DATA, "bubble-masks.tga"))
    software_sprites_image = tga_mod.parse_tga(os.path.join(DATA, "software-sprites.tga"))

    bubble_masks = tga_mod.convert_bubble_masks(bubble_masks_image, 8, 16, 0)
    software_sprites = tga_mod.convert_software_sprites(software_sprites_image, 4, 16, 0)

    block_width = 2 * TILE_EDGE_WIDTH
    width = EXTEND_LETTERS * block_width
    height = 2 * CELL
    pixels = bytearray(width * height)

    for letter in range(EXTEND_LETTERS):
        entry = EXTEND_FIRST_ENTRY + (4 * letter)
        art = _block_bytes(_entry_graphic_addr(entry), bubble_masks, software_sprites)
        mask = _block_bytes(_entry_mask_addr(entry), bubble_masks, software_sprites)

        for column in range(2):
            for y in range(height):
                at = (column * height) + y
                pairs = zip(_bit_pairs(art[at]), _bit_pairs(mask[at]), _bit_pairs(EXTEND_CIRCLE[at]))

                for x, (ink, keep, circle) in enumerate(pairs):
                    if circle == 0:
                        value = 4
                    elif circle == 3 and keep == 3 and ink == 0:
                        value = 0
                    elif circle == 3 and keep == 0:
                        value = ink if ink != 0 else 4
                    else:
                        # Half a pair kept would mix the wall's bits with the art's, which one colour cannot show.
                        raise SystemExit(
                            f"EXTEND letter {letter} at column {column}, row {y}, pixel {x}: art {ink}, "
                            f"mask {keep}, circle {circle} is not a whole pair kept or replaced."
                        )

                    pixels[(y * width) + (letter * block_width) + (column * TILE_EDGE_WIDTH) + x] = value

    chars = {"width": width, "height": height, "pixels": pixels, "palette": [None] * 5}

    write_tga(
        os.path.join(out_dir, "Images", "extend-chars.tga"),
        chars,
        0,
        list(canonical[:4]) + [canonical[0]],
    )

    return EXTEND_LETTERS


# =============================================================================
# HURRY UP! characters
# =============================================================================
#
# $3AB8 scrolls HURRY UP! up the playfield in screen codes $37-$3F, columns 12
# to 20. Each is the level's character under it ORed with $ADB1's letter; at
# row 11 it is ANDed with $ADF9 as well, which clears the level around the
# letters to black, and ORed with the letter again. Both tables are whole bit-
# pairs, so the sheet's top row is the letters and its bottom row the black
# backing, each as entry 11 over entry 00, which is drawn transparent: the view
# paints 11 the text's colour RAM for the letters and black for the backing.

HURRY_CHARS = 9


def _table_bytes(src, filename, label, length):
    """`length` bytes of .byte data from `label` on, through whatever labels follow it."""
    with open(os.path.join(src, filename), encoding="utf-8") as f:
        lines = f.read().splitlines()

    start = next(i for i, line in enumerate(lines) if line.startswith(label + ":"))
    data = bytearray()

    for line in lines[start + 1 :]:
        line = line.split(";", 1)[0].strip()

        if not line or line.endswith(":"):
            continue

        if not line.startswith(".byte"):
            raise SystemExit(f"Unexpected line after {label}: {line!r}")

        data.extend(int(a.strip().lstrip("$"), 16) for a in line[len(".byte") :].split(","))

        if len(data) >= length:
            return data[:length]

    raise SystemExit(f"{label} has fewer than {length} bytes.")


def export_hurry_chars(out_dir, canonical):
    """$ADB1's letters over $ADF9's backing -> 4x8 multicolour cells, two rows of nine."""
    letters = _table_bytes(SRC, "game-tables-3.s", "D_ADB1", HURRY_CHARS * CELL)
    backing = _table_bytes(SRC, "game-tables-3.s", "D_ADF9", HURRY_CHARS * CELL)

    width = HURRY_CHARS * TILE_EDGE_WIDTH
    pixels = bytearray(width * CELL * 2)

    for cell in range(HURRY_CHARS):
        for y in range(CELL):
            at = (cell * CELL) + y
            pairs = zip(_bit_pairs(letters[at]), _bit_pairs(backing[at]))

            for x, (ink, keep) in enumerate(pairs):
                if ink not in (0, 3) or keep not in (0, 3):
                    raise SystemExit(f"HURRY UP! cell {cell}, row {y}, pixel {x}: {ink}, {keep} is not a whole pair.")

                column = (cell * TILE_EDGE_WIDTH) + x
                pixels[(y * width) + column] = ink
                pixels[((CELL + y) * width) + column] = 3 if keep == 0 else 0

    write_tga(
        os.path.join(out_dir, "Images", "hurry-chars.tga"),
        {"width": width, "height": CELL * 2, "pixels": pixels, "palette": [None] * 4},
        0,
        canonical,
    )

    return HURRY_CHARS


# =============================================================================
# Levels
# =============================================================================


def zone_sources():
    """
    The zone data's redirects, `level N -> M`, resolved: the (1-based) level whose zone data a redirected level runs.

    The game's `$E18B` runs the target's data whole, its bitmap included, so a redirected level's map starts from the
    target's wall bytes and bubble current, not its own.
    """
    with open(os.path.join(DATA, "zone-data.txt"), "r") as f:
        levels = zones_mod.parse_text(f.read())

    by_number = {x["number"]: x for x in levels}
    sources = {}

    for level in levels:
        source = level

        while source["redirect"] is not None:
            source = by_number[source["redirect"]]

        if source is not level:
            sources[level["number"] + 1] = source["number"] + 1

    return sources


def export_levels(path):
    """levels.txt -> levels.json, via convert-levels.py's own parser."""
    levels = levels_mod.parse_levels(os.path.join(DATA, "levels.txt"))
    sources = zone_sources()

    out = []
    for level in levels:
        # parse_levels packs both nibbles into one physics byte; the port
        # reads them as the two separate fields levels.txt spells out.
        physics = level["physics"]

        out.append(
            {
                "number": level["num"],
                "colors": level["colors"],
                "sidebar": level["sidebar"],
                "bubbleCurrent": (physics >> 4) & 0x0F,
                "wrapOpenings": physics & 0x0F,
                "foodDrop": {"x": level["food_drop"][0], "y": level["food_drop"][1]},
                "powerupSpawn": {
                    "x": level["powerup_spawn"][0],
                    "y": level["powerup_spawn"][1],
                },
                "spawnFlags": level["spawn_flags"],
                "zoneSource": sources.get(level["num"], 0),
                "enemies": [
                    {
                        "x": e["x_col"],
                        "y": e["y_row"],
                        "type": e["enemy_class"],
                        "delay": e["delay"],
                        "faceLeft": bool(e["face_left"]),
                        "moveLeft": bool(e["move_left"]),
                        "byte1Low": e["byte1_low"],
                    }
                    for e in level["enemies"]
                ],
                "bitmap": level["bitmap_rows"],
            }
        )

    write_json(path, out)
    return len(out)


# =============================================================================
# Zones
# =============================================================================


def level_commands(level):
    """
    The game's command list for one level: the rectangles and mirrors `$E1F3` runs, in its order.

    Decoded from the bytes the real encoder produces rather than from the command list in zone-data.txt, because
    a level's `trailing=` bytes are commands too: they come after the terminator, which is itself the mirror
    command, so they draw over the mirrored map. A byte with bit 7 clear is a mirror; any other starts a
    three-byte rectangle. The list runs to the level's size byte, as the game's does.
    """
    encoded = zones_mod.encode_level(level)
    size = encoded[0]
    commands = []
    index = 1

    while index < size:
        first = encoded[index]

        if first & 0x80 == 0:
            commands.append({"mirror": True})
            index += 1
            continue

        second = encoded[index + 1]
        third = encoded[index + 2]
        commands.append(
            {
                "x": first & 0x1F,
                "y": second >> 3,
                "width": (((second & 0x07) << 2) | (third >> 6)) + 1,
                "height": (third & 0x1F) + 1,
                "type": (first >> 5) & 0x03,
            }
        )
        index += 3

    return commands


def export_zones(path):
    """zone-data.txt -> zones.json, with redirects and mirroring resolved."""
    with open(os.path.join(DATA, "zone-data.txt"), "r") as f:
        levels = zones_mod.parse_text(f.read())

    by_number = {x["number"]: x for x in levels}

    out = []
    for level in sorted(levels, key=lambda x: x["number"]):
        source = level
        seen = set()

        # `level N -> M` shares M's rectangles. Resolving it here keeps the
        # port from carrying an indirection that says nothing about the game.
        while source["redirect"] is not None:
            if source["number"] in seen:
                raise ValueError(f"Zone redirect loop at level {source['number']}")
            seen.add(source["number"])
            source = by_number[source["redirect"]]

        out.append({"number": level["number"], "rects": level_commands(source)})

    write_json(path, out)
    return len(out)


# =============================================================================
# Output
# =============================================================================


def write_json(path, payload):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", newline="\n") as f:
        json.dump(payload, f, indent=2)
        f.write("\n")


def main():
    parser = argparse.ArgumentParser(
        description="Export rebb64's data to the form the C# port reads."
    )
    parser.add_argument(
        "--rebb64", required=True, help="Path to a checkout of zaidka/rebb64"
    )
    parser.add_argument(
        "--out",
        default=DEFAULT_OUT,
        help="The rendition's Assets directory to write into",
    )
    args = parser.parse_args()

    load_converters(os.path.abspath(args.rebb64))

    canonical = load_canonical_palette(args.out)
    print(f"Writing sheets in the rendition's {len(canonical)} palette colours")

    levels_path = os.path.join(args.out, "Levels", "levels.json")
    zones_path = os.path.join(args.out, "Levels", "zones.json")

    count = export_levels(levels_path)
    print(f"Wrote {count} levels -> {levels_path}")

    count = export_zones(zones_path)
    print(f"Wrote {count} zone levels -> {zones_path}")

    count = export_images(args.out, canonical)
    print(f"Wrote {count} sprite sheets -> {os.path.join(args.out, 'Images')}")

    count = export_font(args.out, canonical)
    print(f"Wrote a {count}-glyph font -> {os.path.join(args.out, 'Fonts', 'charset.tga')}")

    count = export_level_number_font(args.out, canonical)
    print(f"Wrote a {count}-glyph level number font -> {os.path.join(args.out, 'Fonts', 'level-number.tga')}")

    count = export_hud_font(args.out, canonical)
    print(f"Wrote a {count}-glyph HUD font -> {os.path.join(args.out, 'Fonts', 'hud-font.tga')}")

    count = export_tile_edges(args.out, canonical)
    print(
        f"Wrote {count} tile edge characters -> "
        f"{os.path.join(args.out, 'Images', 'tile-edges.tga')}"
    )

    count = export_object_sprites(args.out, canonical)
    print(
        f"Wrote {count} object sprite cells -> "
        f"{os.path.join(args.out, 'Images', 'object-sprites.tga')}"
    )

    count = export_item_chars(args.out, canonical)
    print(
        f"Wrote {count} item blocks -> "
        f"{os.path.join(args.out, 'Images', 'item-chars.tga')}"
    )

    count = export_giant_bubble(args.out, canonical)
    print(
        f"Wrote {count} giant bubble characters -> "
        f"{os.path.join(args.out, 'Images', 'giant-bubble.tga')}"
    )

    count = export_effect_chars(args.out, canonical)
    print(
        f"Wrote {count} effect characters -> "
        f"{os.path.join(args.out, 'Images', 'effect-chars.tga')}"
    )

    count = export_extend_chars(args.out, canonical)
    print(
        f"Wrote {count} EXTEND letters -> "
        f"{os.path.join(args.out, 'Images', 'extend-chars.tga')}"
    )

    count = export_hurry_chars(args.out, canonical)
    print(
        f"Wrote {count} HURRY UP! characters -> "
        f"{os.path.join(args.out, 'Images', 'hurry-chars.tga')}"
    )

    count = export_super_room_chars(args.out, canonical)
    print(
        f"Wrote {count} super bonus room characters -> "
        f"{os.path.join(args.out, 'Images', 'super-room-chars.tga')}"
    )


if __name__ == "__main__":
    sys.exit(main())
