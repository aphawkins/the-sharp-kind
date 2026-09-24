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

SCREEN_COLUMNS = 32

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


def export_object_sprites(out_dir, canonical):
    """
    Compose object-sprites.tga: entries 0-43 (bubbles and lightning), then the
    eight pop-animation frames, one 12x16 cell each, laid out in a single row.
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
# The indices are $A892 (food) and $A8C1 (special item); the highest is $39.

ITEM_CELL_WIDTH = 8
ITEM_CELL_HEIGHT = 16
ITEM_BLOCK_BYTES = 32
ITEM_BLOCK_COUNT = 0x3A
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


# =============================================================================
# Font
# =============================================================================

# charset.tga is a 14x10 grid in the game's own order, which is not ASCII: the
# digits come first, and the letters happen to land where ASCII minus 32 would
# put them. The engine's BitmapFont indexes a grid sheet by `char - ' '`, so the
# sheet is rewritten in that order and the cells nothing maps to are left blank.
CHARSET_COLUMNS = 14
FONT_COLUMNS = 16
FONT_ROWS = 4
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
    """ASCII glyph index (char - ' ') -> the cell in charset.tga holding it."""
    cells = {}

    for digit in range(10):
        cells[ord("0") + digit - ord(" ")] = digit

    for letter in range(26):
        cells[ord("A") + letter - ord(" ")] = 33 + letter

    cells[ord(LIFE_ICON) - ord(" ")] = LIFE_ICON_CELL

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
# Levels
# =============================================================================


def export_levels(path):
    """levels.txt -> levels.json, via convert-levels.py's own parser."""
    levels = levels_mod.parse_levels(os.path.join(DATA, "levels.txt"))

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


# The direction 0-3 (up, right, down, left) a mirrored cell ends up with. $E271:
# `and #$01` tests bit 0 first and copies unchanged if it is clear, so only 1
# (right) and 3 (left) ever flip, into each other, by `eor #$02`. 0 and 2 are
# their own mirror.
_MIRRORED_TYPE = [0, 3, 2, 1]


def mirror_rect(rect):
    """
    Reflect a rectangle about the screen's centre column.

    convert-levels.py's is_symmetric() compares row[:16] against
    row[16:] reversed, so the axis sits between columns 15 and 16 - which
    puts a rectangle's mirrored left edge at 32 - x - width.
    """
    return {
        "x": SCREEN_COLUMNS - rect["x"] - rect["width"],
        "y": rect["y"],
        "width": rect["width"],
        "height": rect["height"],
        "type": _MIRRORED_TYPE[rect["type"]],
    }


def has_trailing_zero(level):
    """
    Whether this level's encoded zone data carries a $00 terminator byte.

    Not the same question as "does zone-data.txt say `mirror`" - it never
    does, in the whole of the real data, zero times across all hundred
    levels. `$E1F3` dispatches on bit 7 alone, so `decompress_level_data`
    reads a $00 terminator exactly the way it would read an explicit `mirror`
    opcode, and falls into the horizontal mirror at `$E24C` regardless -
    confirmed against a VICE capture of the transient buffer, see
    docs/bb-port-plan.md's "direction field" item. `encode_level` only omits
    the terminator when a level's declared `size=` leaves no room for one; run
    the real encoder rather than re-deriving its branch, so this can never
    drift from what the build actually produces.
    """
    encoded = zones_mod.encode_level(level)
    rect_count = sum(1 for c in level["commands"] if c["type"] == "rect")
    return len(encoded) > 1 + (3 * rect_count)


def level_rects(level):
    """
    Run one level's command list into a plain rectangle list, mirror included.

    `mirror` never appears as an opcode in zone-data.txt - see
    has_trailing_zero - so what decides whether a level's right half is a
    mirror of its left is only ever the implicit terminator, checked once
    here rather than modelled as a command the loop reacts to.
    """
    rects = [
        {
            "x": command["x"],
            "y": command["y"],
            "width": command["width"],
            "height": command["height"],
            "type": command["zone_type"],
        }
        for command in level["commands"]
        if command["type"] == "rect"
    ]

    if has_trailing_zero(level):
        rects += [mirror_rect(r) for r in reversed(rects)]

    return rects


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

        out.append({"number": level["number"], "rects": level_rects(source)})

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


if __name__ == "__main__":
    sys.exit(main())
