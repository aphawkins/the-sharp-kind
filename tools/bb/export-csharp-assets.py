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
levels_mod = None
zones_mod = None
tga_mod = None


def load_converters(rebb64):
    """
    Import rebb64's own converters from a checkout, and point DATA at its data.

    They are imported by path because their filenames carry hyphens, which no
    import statement would accept.
    """
    global DATA, levels_mod, zones_mod, tga_mod

    build = os.path.join(rebb64, "build")
    DATA = os.path.join(rebb64, "data")

    for required in (build, DATA):
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
        "type": rect["type"],
    }


def level_rects(level):
    """
    Run one level's command list into a plain rectangle list.

    The binary keeps `mirror` as an opcode the 6502 acts on while it walks
    the data. The port has no reason to: applying it here means the game
    reads rectangles and nothing else.
    """
    rects = []
    for command in level["commands"]:
        if command["type"] == "rect":
            rects.append(
                {
                    "x": command["x"],
                    "y": command["y"],
                    "width": command["width"],
                    "height": command["height"],
                    "type": command["zone_type"],
                }
            )
        elif command["type"] == "mirror":
            rects.extend(mirror_rect(x) for x in reversed(rects))

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


if __name__ == "__main__":
    sys.exit(main())
