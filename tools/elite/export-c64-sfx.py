#!/usr/bin/env python3
"""Export Commodore 64 Elite's sound effects to the 8-bit rendition's elite.sidfx.

The effects are the 16 rows of the C64 source's sound tables (SFXPR SFXCNT SFXFQ SFXCR SFXATK SFXSUS SFXFRCH SFXVCH),
named by its sfx* constants. The sounds are what the game's SoundEffect keys play, each a list of the steps the C64
routine named beside it takes; SidEffectEngine in SharpKind.Audio is the port of the C64's NOISE, NOISE2 and SOINT that
plays them.

    python tools/elite/export-c64-sfx.py C:\\code\\github\\markmoxon\\elite-source-code-commodore-64\\1-source-files\\main-sources\\elite-source.asm

Writes src/elite/libs/EliteSharp.Renditions.EightBit/Assets/SFX/elite.sidfx. Running it twice gives the same file.
"""
import json
import os
import re
import sys

TABLES = {
    "SFXPR": "Priority",
    "SFXCNT": "Frames",
    "SFXFQ": "Frequency",
    "SFXCR": "Control",
    "SFXATK": "AttackDecay",
    "SFXSUS": "SustainRelease",
    "SFXFRCH": "FrequencyChange",
    "SFXVCH": "VolumeMask",
}

EFFECTS = 16

# What each SoundEffect key plays, and the C64 routine it copies.
SOUNDS = {
    "Launch": [{"Effect": "sfxwhosh"}],  # LAUN
    "Crash": [{"Effect": "sfxexpl"}],  # EXNO3, from MA63 (collision) and OO3 (shield hit)
    "Dock": [],  # the C64 makes no docking sound
    "Gameover": [{"Effect": "sfxexpl"}],  # EXNO3, from DEATH
    "Pulse": [{"Effect": "sfxplas"}],  # MA68; it picks sfxplas, sfxblas, sfxalas or sfxmlas by laser, the port has one key
    "HitEnemy": [{"Effect": "sfxhit", "Frequency": 208, "SustainRelease": 0xF3}],  # EXNO, at the nearest range
    "Explode": [{"Effect": "sfxexpl", "Frequency": 81, "SustainRelease": 0xF3}],  # EXNO2, at the nearest range
    "Ecm": [{"Effect": "sfxecm"}],  # ECBLB2; ECMOF stops it with NOISEOFF, which is not ported
    "Missile": [{"Effect": "sfxwhosh"}],  # SFRMIS, FRMIS
    "Hyperspace": [  # HYPNOISE
        {"Effect": "sfxhyp1", "Frequency": 240, "SustainRelease": 0xF5},
        {"Effect": "sfxwhosh"},
        {"Effect": "sfxhyp1", "Layer": True, "DelayFrames": 1},
    ],
    "IncomingFire1": [{"Effect": "sfxelas"}, {"Effect": "sfxelas2"}],  # TA3 plays both on every hit
    "IncomingFire2": [{"Effect": "sfxelas"}, {"Effect": "sfxelas2"}],  # TA3; the 1/2 split by shield is TNK's
    "Beep": [{"Effect": "sfxbeep"}],  # BEEP
    "Boop": [{"Effect": "sfxboop"}],  # MA4, HME6, WA1
}

OUTPUT = os.path.join(
    os.path.dirname(__file__), "..", "..", "src", "elite", "libs", "EliteSharp.Renditions.EightBit", "Assets", "SFX",
    "elite.sidfx")


def number(text):
    if text.startswith("$"):
        return int(text[1:], 16)
    if text.startswith("%"):
        return int(text[1:], 2)
    return int(text)


def read(path):
    with open(path, encoding="latin-1") as source:
        lines = [line.split(";")[0].rstrip() for line in source]

    names = {}
    for line in lines:
        match = re.match(r"^\s*(sfx\w+)\s*=\s*(\d+)\s*$", line)
        if match:
            names[int(match.group(2))] = match.group(1)

    tables = {}
    for label in TABLES:
        start = lines.index("." + label) + 1
        values = []
        for line in lines[start:]:
            match = re.match(r"^\s*EQUB\s+(\S+)\s*$", line)
            if match:
                values.append(number(match.group(1)))
                if len(values) == EFFECTS:
                    break
        tables[label] = values

    if sorted(names) != list(range(EFFECTS)) or any(len(v) != EFFECTS for v in tables.values()):
        sys.exit("The source does not have 16 named effects in every table.")

    effects = {}
    for index in range(EFFECTS):
        row = {field: tables[label][index] for label, field in TABLES.items()}
        if row["FrequencyChange"] > 127:
            row["FrequencyChange"] -= 256
        effects[names[index]] = row

    return effects


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)

    effects = read(sys.argv[1])

    for steps in SOUNDS.values():
        for step in steps:
            if step["Effect"] not in effects:
                sys.exit(f"The source has no {step['Effect']}.")

    lines = ["{", '  "Effects": {']
    lines += [f'    "{name}": {json.dumps(row)}' + ("," if i < len(effects) - 1 else "")
              for i, (name, row) in enumerate(effects.items())]
    lines += ["  },", '  "Sounds": {']
    lines += [f'    "{name}": {json.dumps(steps)}' + ("," if i < len(SOUNDS) - 1 else "")
              for i, (name, steps) in enumerate(SOUNDS.items())]
    lines += ["  }", "}", ""]

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    with open(OUTPUT, "w", encoding="utf-8", newline="\n") as out:
        out.write("\n".join(lines))

    print(f"Wrote {len(effects)} effects and {len(SOUNDS)} sounds to {os.path.normpath(OUTPUT)}")


if __name__ == "__main__":
    main()
