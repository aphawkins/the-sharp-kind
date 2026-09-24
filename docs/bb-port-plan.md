# Bubble Bobble Port Plan

How Bubble Bobble becomes the third game in The Sharp Kind, beside Elite and
Stunt Car Racer. This is the developer's plan and progress record: tick a box
when the item is done and its verify step passes. Players want
[bb-readme.md](bb-readme.md), which does not exist yet.

This file keeps only what the next piece of work needs. Each class's header
comment is the full record of how its routine works and what it gets wrong.
Earlier, longer versions of this plan are in git history.

## 1. Scope and rules

**Goal.** A faithful C# translation of the Commodore 64 conversion of Bubble
Bobble, from [zaidka/rebb64](https://github.com/zaidka/rebb64), a byte-exact,
documented disassembly. It is checked out at `C:\code\github\zaidka\rebb64`,
written below as `rebb64/`.

**Faithful to the C64, not the arcade.** Where they differ, the C64 wins.
`rebb64/src/` is the golden source. `rebb64/docs/TECHNICAL.md` is a summary and
loses when the two disagree.

**In scope:** 100 levels, two players, bubbles, enemies, items, EXTEND, super
mode, bonus rounds, scoring, sound. **Out of scope:** VIC-II, SID and CIA
emulation, cycle accuracy, the tape loader, arcade accuracy.

**Licence.** `rebb64` has no licence file and holds Taito and Firebird
material. Andy decided the port ships the derived assets in this repo. Revisit
before any wider release.

**Standing rules**

- Copy Elite (`EliteSharp.Renditions.EightBit` especially), not Stunt Car
  Racer.
- SharpKind changes under you. Pull and rebuild first. After changing
  anything under `src/useful/`, run Elite's and SCR's full suites too.
- One `.s` routine becomes one C# method, with its address in a comment.
- Bytes wrap on purpose; never widen to `int` to avoid a wrap. Test each axis
  of a distance separately (see [decisions.md](decisions.md)).
- Keep the 6502's parallel arrays (`EntityTable`, `ObjectTable`,
  `PlayerTable`) until the game is verified. This is a deliberate exception to
  "derive don't store".
- Never tune a constant to make a result look right. Never invent behaviour.
  If a routine is unclear, stop and ask.
- **The reference's comments and labels are often wrong.** Name things from
  what the code does. Known cases: `$5D`/`$5E` are item timers, not a bubble
  timer. `$A824` is the blow reload, not invincibility. `$54` holds EXTEND
  letters, not powerups. `$0D02` is a player touching a bubble. `$EE62`'s
  "player below" means above. `baron_von_blubba` (`$10D3`) is not the Baron;
  he is types `$2E`/`$30` at `$1473`.
- Say what an item covers before starting it. Do not commit; Andy commits. UK
  spelling. `dotnet test` is blocked in the sandbox, so run the test
  executables in `bin/` directly.

## 2. Tools and proof

- **Build the reference:** `cc65` at `%USERPROFILE%/cc65/bin` (not on
  `PATH`). `make verify` in `rebb64/build/` proves the image byte-exact.
  `make release TSCRUNCH=C:/code/github/tonysavon/TSCrunch/bin/windows_amd64/tscrunch.exe`
  builds the runnable `rebb64.prg`.
- **VICE:** use the `vice-drive` skill (`.claude/skills/vice-drive/`). It has
  the launch line, `monitor.py` (`selftest`, `start`, checkpoints, memory,
  registers) and every protocol trap found so far. Use the binary monitor.
- **Golden captures** are how behaviour is proved. Bracket a routine with two
  exec checkpoints, record the bytes it can touch before and after, and replay
  every line through the port byte for byte. The captures live in
  `src/bb/test/BubbleBobbleSharpLib.Tests/*/Captures/`.
  - Any write into the machine (clearing enemies, setting `SUBFLG`, moving a
    player) is an intervention. Say so in the test header.
  - `$E9EA` mixes in a CIA timer byte, which the port takes from
    `IRandomSource`. Recover it in the test rather than matching it: record
    `$26`/`$27` at the routine's entry and its `rts` (`$E9FC`), or search the
    timer bytes that give the rolls the routine kept.
  - The IRQ's once-a-second tick (`$06C6`) can land inside a bracket.
- **Golden frames** (`SharpKind.GoldenFrames`) catch the port changing
  itself. They do not prove it matches the C64; VICE captures do.

## 3. Shape

```
src/bb/libs/BubbleBobbleSharp.Abstractions/          contracts and view models
src/bb/libs/BubbleBobbleSharpLib/                    the game: the translation
src/bb/libs/BubbleBobbleSharp.Renditions.EightBit/   the drawing and the assets
src/bb/apps/BubbleBobbleSharp/                       entry point
src/bb/test/BubbleBobbleSharpLib.Tests/
```

- As in Elite, the game builds a **model** and the rendition **draws** it
  (`IView<TModel>`, `IViewSurface`). The rendition declares the screen size
  (320x200) and is the `8-bit` tier.
- Each `.cs` file starts with the three-line header in any existing file.
- Assets come from `tools/bb/export-csharp-assets.py`, which imports rebb64's
  own converters. Re-running it over an unchanged checkout must reproduce every
  asset byte for byte.

## 4. Done

Each line names the classes. Their header comments have the detail.

**Scaffolding, assets and the static screen**

- [x] Three-assembly split, rendition loader, config, `N` debug key for the
      next level (goes when the front end arrives).
- [x] Sprite engine in `SharpKind.Graphics` (`SpriteSheet`, `SpriteAtlas`,
      `SpriteRenderer`, `SpriteAtlas.Grid`).
- [x] Assets: `levels.json`, `zones.json` and eleven `.tga` sheets, plus the
      Pepto palette (`palette.json`, with `c64-pepto-colodore.png` as its
      source). A sheet's pixels are C64 entry numbers; `MulticolourSheet`
      repaints entries 1–3 at draw time. Multicolour sheets are half width and
      are doubled when drawn.
- [x] `PlayfieldModel`/`PlayfieldView8Bit`: the level is the leftmost 32 of 40
      columns, 25 rows (ceiling and floor from `wrapOpenings`), with the two
      outer columns each side forced solid. `SidebarModel`/`SidebarView8Bit`:
      the decoration is drawn over those columns. Per-level colours from the
      `colors` byte (entry 01 = high nibble, 10 = low nibble, 11 = colour 5).
- [x] `HudModel`/`HudView8Bit`: columns 33 onwards; BCD scores with leading
      zeros blanked; lives as screen code `$1D`, carried in the font sheet.
- [x] Frame composed as `RenderLayer`s: level, decoration, players, HUD.

**Players** (state 1 only)

- [x] `Input`, `PlayerTable`, `EntityTable` (eight slots: players 0–1, enemies
      2–7), `PlayerCell` (`$E9B8`), `SolidMap` (bit 7 solid, low two bits the
      bubble current), `PlayerModel`/`PlayerView8Bit` (`$1805`; now
      `SpriteModel`/`SpriteView8Bit`, all eight slots).
- [x] `PlayerFrame` (`$1CBD`, `$1E6C`, `$2162`), `PlayerMovement` (`$220C`,
      walk and `$222B` jump trigger), `PlayerJump` (`$23EA`), `PlayerDrift`
      (`$2483`/`$248D`), `PlayerSteer` (`$25F1`), `PlayerLanding` (`$2519`),
      `PlayerDescent` (`$EB48`), `PlayerFall` (`$257C`). Proved against VICE
      walk, jump and steer captures.
- [x] A level start must put `$8818` to `$FF` in all eight slots, or every
      mover reads the player as being in a bubble.

**Bubbles and enemies**

- [x] `BubbleBlow` (`$22E8`, `$2301`): a bubble appears three frames after
      fire. `EntityMover` (`$0E23`). `ObjectTable` is the eighteen slots.
- [x] `EnemyAiLoop` (`$0CF2`), `EnemyDispatcher` (`$0F48`–`$0F98`),
      `BubbleCollision` (`$105B`, `$1090`), `EntityTimers` (`$13BE`: clock,
      wobble, timeout pop), `BbRandom` (`$E9EA`).
- [x] The bubble current comes from the map: zone rectangles give it, every
      level's zone data ends in an implicit mirror (flipping 1 and 3), rows 0
      and 24 take `bubbleCurrent`, and other rows take the wall-bitmap byte.
- [x] `EnemySpawner` (`$1E2E`, `$39D2`), `SolidMap` row copy (`$3A6C`),
      `DiagonalMover` and wrap openings (`$EFC0`), `EnemyFrame` (`$1CBD` for
      slots 2–7) with every enemy class, anger (`$1D03`), and `Baron` (`$1473`).

**Items and scoring**

- [x] `FoodDrop`: a popped enemy flies (`$0B`), falls (`$0C`), becomes food
      (`$11`, one slot per frame) and waits `$C8` passes (`$12`). `Scores`
      (`$0400`–`$0405`, `$7C26`) does the 6510's decimal add, so `$A79A`'s
      out-of-table `$0A` scores `$10`.
- [x] `BubblePop` (`$E90E`'s vectors for types `$34`–`$40`): chain pops within
      `$18` pixels, lets caught enemies out dead, gives EXTEND letter bits,
      runs the pop frames, and scores a chain once it stops (`$E97F`).
- [x] `LevelItems` (`$2B31`, `$1578`, `$2CB7`, `$06C6`): a bonus food and a
      special item per level, shown and hidden on a timer in seconds.
      `ItemEffects` (`$2D65`): the candies, rings, crystal and effect 0.
      `Rings`: ten points per walk step, drift step from a jump, or bubble.

## 5. To do, in implementation order

Each item ends with how it is proved.

- [x] **1. Run the game loop.** `GameLoop` calls every translated routine in
      the reference's order; `BubbleBobbleMain` holds it, and the HUD shows
      `Scores`.
      - Level start (`$09DC`–`$0A05`): map, enemy spawn, players, and
        `LevelItems.Setup` (not on level 100, whose items throw).
      - A pass (`$0A07`): `$1578`, `$0A28`, `$E90E` pop, `$0CF2`, `$13BE`,
        `$2CB7`; `$06C6` every 25 passes; then `$1CBD`, enemies then players.
      - **The game runs at 25 passes a second.** The main loop waits two
        50 Hz frames and the IRQ runs `$1CBD` on alternate frames, so
        `TickRate` is now 25. At 50 the app ran the players at double speed.
      - `BubbleCollision`'s `$AB81` read runs on into `$AB89` for enemy
        states 8 and 9, which play reaches; those image bytes are now in its
        table. Beyond `$AB89` it still throws.
      - Gaps, named in `GameLoop`: `$0BED`, `$0AAB`, `$32C1`, the level timer
        and level completion. **Taking a special item whose effect is not
        translated throws and stops the app** (item 6), by the loud-gap rule.

      Verified: `GameLoopTests` runs every level for 3,000 passes with a
      scripted stick, and the only thing allowed to stop a run early is a
      named untranslated routine. In the app, level 1 plays through a key
      script to a clean exit.
- [ ] **2. Draw what moves.** Found so far, and split into steps:
      - [x] **2a. The render pass's own state changes.** `BubblePop` now also
        has `$7BD4` (a blown bubble, `$16`, becomes `$00`) and `$3CB2` (a
        caught enemy's hardware sprite, slot 2 onwards, follows its bubble
        with the frame in `$AA42`). So `$AA42` after a capture is a sprite
        frame, and `BubbleCollision`'s `$AB81` table is caught-enemy frames,
        not scores. Proved by hand in `BubblePopTests`.
      - [ ] **2b. Enemies and caught enemies**, through `$1805`: the hardware
        sprite path, for slots 2–7 as well as 0–1. The pointer is
        `(Frame & $1F) + $8598`, in the VIC bank at `$4000`.
        - [x] **Where the enemy art is.** `EnemySpawner`'s `$8598` table is
          indexed by state (class + 2), so its first two bytes (`$03`, `$0F`)
          are never an enemy's; the enemy bases are `$73`, `$7F`, `$8B`,
          `$97`, `$9F`, `$AB`, `$B3`, `$BB`. Measured over the whole port (every
          level, three seeds, 3,000 passes, the `GameLoopTests` stick), every
          enemy pointer in every state (walking `$02`–`$09`, caught `$0A`,
          popped `$0B`/`$0C`, food-bound `$11`) lies in `$73`–`$BF`, all
          inside `sprites-game.tga` (`$60`–`$C0`). No per-level sprite set is
          needed. Caught enemies (`$0A`) only showed pointer `base + 0`.
          Food (`$12`) uses `$F4`–`$F9` from base `$F2`: `$7C80`–`$7E7F`,
          which `$2921` fills at run time, so food waits for `$2921`. Not
          reached by the run: the Baron (`$1473`), since the hurry-up is
          untranslated.
        - [x] **Draw all eight slots.** `PlayerModel`/`PlayerView8Bit` are
          now `SpriteModel`/`SpriteView8Bit`: pointer `(Frame & $1F) +
          SpriteBase`, less `$60` for the sheet column, drawn 7 down to 0.
          `GameLoop` gives the players their `$60` base (`$451E`). `$1822`'s
          flash is in: a slot below state `$11` with `$8728` running takes
          `$8570` (`$47B5`: 5, 3, then 10 for every enemy), so an angry enemy
          is light red. A pointer outside the sheet is not drawn, which today
          is only food (a gap until `$2921`). Proved by hand in
          `SpriteModelTests`/`SpriteView8BitTests`; smoke-tested in the app
          (level 1's enemy drawn on the top platform). Still open: the
          Baron's pointers, and whether `$182F`'s store back to `$8520`
          matters (does any routine leave a frame above `$1F`?).
      - [ ] **2c. Bubbles and pop frames**, drawn as software sprites. Each is a
        3×16 character-column graphic (12×16 multicolour pixels) and a mask,
        composed as `(background AND mask) OR graphic`, in the level's
        character colours (as `MulticolourSheet`).
        - `$E779` (types `$00`–`$14`): entry `type×2 + $A9C4` in
          `$AA54`/`$AA8E` (graphic) and `$AAC8`/`$AB02` (mask). Entries 0–11,
          the plain bubble, point into `$8000`, so their graphics are in
          `bubble-masks.tga`, one bit per pixel, read in pairs. Entries 12
          onwards are in `software-sprites.tga` (16-row strips of 4-pixel byte
          columns). Masks for entries 12–43 cycle `$83C0`–`$8450` by `$A9C4`.
        - Pop frames (`$3CE5`): `$AB41`/`$AB49` indexed by `$A9C4` or 4 (the
          later frames), with one mask, `$8260`, for all three columns. The
          graphics are at `bubble_anim_masks` in `sprites2-tables.s`, which is
          not exported yet.
        - Position: the box's left is `$DC × 8`; the graphic is pre-shifted by
          `$A9C4 × 2` inside it, so the art's left is `X − $14`, exactly, in
          every VICE sample. The first character row composed is screen row
          `$EE − 2` (`$AD22` is row 0) and the box builds upwards; the
          graphic's bottom row is at pixel `($A9D6 + 7) & 7` of that row.
        - A screenshot shows the pass before: `$E90E` draws, then `$0CF2`
          moves. Record bytes at `$0A4B` and screenshot at the next `$E90E`.
        - Type `$48` (wobble) and `$4A` draw nothing, so a wobbling bubble
          flickers.
        - [x] **Compose `object-sprites.tga`.** Done in
          `tools/bb/export-csharp-assets.py` (`export_object_sprites` and
          helpers): gets the ROM bytes with rebb64's own
          `convert_bubble_masks` (8×16) and `convert_software_sprites`
          (4×16), and parses the pop frames from `sprites2-tables.s`
          (`bubble_anim_masks`, `$8960`–`$8AFF`, 26 blocks of 16; frame k is
          three blocks at `+$20 + $30k`). Writes one 12×16 cell per entry
          (entries 0–43, then pop frames 0–7), all in a single row (index
          `i`'s cell is at x = `12i`). Per bit-pair, mask 11 keeps the
          background (index 0, transparent, regardless of the graphic - a
          masked pixel's leftover graphic data is normal and not reported);
          mask 00 takes the graphic (graphic 00 → index 4, opaque black, a
          fifth colour-map entry that borrows canonical colour 0's RGB but
          keeps its own alpha). Any other mask value is reported, not
          guessed at - none occurred. Memory map: masks `$8000`; software
          sprites `$8F00`. Graphic pointers: entry e<12 `$8000+$30e`; 12–14
          `$9080`; 15–19 `$9110`; 20–23 `$9200`; 24–26 `$9480`; 27–31
          `$9510`; 32–37 `$9600`; 38–42 `$9720`; 43 `$9810` (each `+$30` per
          entry). Masks: e<12 `$8240+$30e`; e≥12
          `[$83C0,$83F0,$8420,$8450][e&3]`.
          - **Pop frames are composed without any mask.** `$3CE5`
            (`entity-state-tables.s`) points every pop frame's AND-mask
            operand at `$8260` for all three columns - which is entry 0's
            own third-column mask, entirely 11 (background), since entry
            0's small bubble never reaches that column. Composited through
            it, every pop frame would be blank. `bubble_anim_masks`' bytes
            only ever hold 0 or 3 per bit-pair, which decodes clean into a
            spinning-spark burst shape with no masking at all (0 →
            transparent, 3 → colour 3 directly) - checked by eye against
            all eight frames, not guessed. The AND/OR compositing that
            `sprite-composer.s`'s comments describe is a C64 technique for
            blending a sprite into an arbitrary sub-character screen row (it
            runs only for a few edge rows, decided by `OLDTXT`/`$A9D6`; most
            rows are a straight, unmasked copy) - not a general per-pixel
            transparency rule, so it need not (and for pop frames, cannot)
            be replicated literally for a standalone asset.
        - [x] **`BubblePop` records what it drew.** `Drawn` (a slot's index
          into `object-sprites.tga`, or `NotDrawn`) is computed from the type
          read at the start of `Step`, before that same call mutates it -
          since the screen shows the drawing made before `$0CF2` moves
          anything: `$00`–`$14` → entry `type×2+$A9C4` (a `$16` becomes `$00`
          first, since `$7BD4` converts the type before drawing it); `$34`,
          `$3A`, `$3C` → pop `0|$A9C4`; `$3E`, `$40` → pop `4|$A9C4`; nothing
          for `$18`–`$22`, `$36`, `$38`, `$42`, `$48`, `$4A`, or any type not
          matched (item 2d's specials). Not renamed to `ObjectPass` - the
          rename is cosmetic and the class still steps the eighteen slots as
          it always did.
        - [x] **`ObjectsModel` and `ObjectView8Bit` draw the cells.** A new
          `RenderLayer` between the playfield and the sidebar - the sidebar
          only ever touches the level's outermost two columns, so the order
          between it and the objects does not matter; the hardware sprites
          (players, and item 2b's enemies) come after both. Position: left
          edge `$AA0C - $14`; the bottom pixel row sits in character row `$EE
          - 2` at sub-position `($A9D6 + 7) & 7`, and the sixteen-pixel cell
          builds upward from there. Doubled horizontally through a
          `MulticolourSheet`, as every other multicolour sheet is.
          **Smoke-tested, not yet golden-framed:** driving the app with a
          scripted blow shows a clean, correctly proportioned bubble that
          rises and drifts through the level with no garbling - see
          `.claude/skills/sdl-drive`. That is not the pixel-exact proof this
          item's own verify step asks for, which still needs a VICE capture;
          the position formula above is the one place this item did not
          re-derive from first principles and could be wrong in a way a
          smoke test would not catch (an off-by-one row, for instance).
      - [ ] **2d. The specials**: `$24`–`$32` and `$44`–`$4C` vectors
        (`$E758`, `$3E94`, `$3EFD`, `$E752`, `$3ED4`, `$E767` for the Baron,
        `$3E77`, `$3F28`, `$3F88`), with item 5.
      - [x] **2e. The level's items**, `$52`/`$53`, drawn in characters.
        `$2B31` copies each item's 32 bytes from `sprites_rom` (`$9AE0` +
        index × 32; `$A892` food, `$A8C1` special) into characters
        `$42`–`$45` and `$46`–`$49`. `$1844` (`L_1934`) writes them as a
        2×2 block at the level cell (`$4E`/`$50`), in column order. It
        writes the item's colour (`$5F`/`$60`, from `$A8E4`/`$A913`) to all
        four colour-RAM cells (`L_186B`). `$1578` puts the cells back when
        the item goes. `LevelItems` now holds `Art`, `Colour`, `Column` and
        `Row`, and `$2CB7`'s EOR 5 flash for a visible `$18`. `ItemsModel`
        and `ItemView8Bit` draw from `item-chars.tga` (exported by
        `export_item_chars`, blocks 0–`$39`). They draw in a layer just
        above the playfield, with entry 11 in the item's own colour and entry
        00 opaque black. **Proved against VICE:**
        `Views/Captures/items-level-1.txt` (level 1, food `$19`, no
        intervention) holds the bytes at `$0A4B` and the food's 16×16
        pixels from the screenshot at the next `$E90E`.
        `ItemView8BitGoldenTests` matches them pixel for pixel. The
        characters at `$4210` equal block `$26` of the export byte for byte.
        Not captured: the special item, and the `$18` flash.

      Verify: golden frames compared pixel for pixel with VICE screenshots
      (bytes at `$0A4B`, picture at the next `$E90E`), for a bubble in each
      shift, each pop frame, a caught enemy, each enemy class and the food.
- [ ] **3. Players and enemies meet.** `$0AAB` (a player pushing, riding or
      touching a bubble, and death on an enemy), the player states beyond 1
      (`$0E` dying, `$0F` dead, `$10` respawn, `$18` carried), lives, and game
      over at `$0A64`. `$04BB` also clears the player's `$5B7F` bit.
      Verify: VICE captures of a death and a respawn.
- [ ] **4. Level flow.** The level timer (`$2A`, `$2B`, `$06C6`), the hurry-up
      and anger (`$16E4`), the Baron's spawn (`$1621`), `$1719` setting
      `LevelItems.FoodBase`, the freeze in `$67` (`$2F74`, `$1CFB`), the
      level-complete flag `$21`, and the next level (`$0A6E`, `$09A5`).
      Verify: a scripted playthrough of level 1 to level 2 scores what the C64
      does.
- [ ] **5. Special bubbles and EXTEND.** `$0BED` (bubbles from the corners),
      the special bubbles' own pop arms at `$3E02` (`$06` level-end fruit
      positions and `OPMASK`, `$08`, `$0A`), `$32C1` and `extend-bonus.s`.
      Verify: six letters award a life, against a VICE capture.
- [ ] **6. The other special-item effects:** 1, 2, 6–8, 11 and 15–34 in
      `$2D65`. They are level skips (`$2F7D`–`$2F89`, `$2ECC`), the freeze,
      screen effects, and handlers at `$348A`–`$3621`, `$7FA4`/`$7FA7` and
      `$7C37`/`$7C3C`. Verify: a capture per effect.
- [ ] **7. Super mode, bonus rounds and level 100** (`SUBFLG` `$63`, whose
      items and food throw today, and the ending at `$A5B7`).
- [ ] **8. Sound.** Check "Audio per rendition" in
      [backlog-roadmap.md](backlog-roadmap.md) first: if a SID emulator
      arrives, port the player. Otherwise list the trigger IDs in
      `sid-wrapper.s`, record a `.wav` per effect and tune, and drive them
      through `ISound`/`AudioController` from the same call sites. Verify: a
      manifest test that every trigger has an asset.
- [ ] **9. Front end.** Title and attract mode, one or two players, game over
      and high score, as `IGameScreen`s in a `ScreenManager`. Remove the `N`
      key. Stop offering `16-bit` and `Modern`, which this game has no art
      for. Verify: every screen is reachable and returns to the title.
- [ ] **10. Join the repo.**
      - 320x256 with a 56-line banner on top, the repo's 8-bit tier (Elite's
        size). `EightBitRendition.ScreenHeight` to 256, `PlayfieldRow` to 7,
        and a `BannerModel` drawing `Assets/Images/banner.png` (already
        supplied and within the palette; add it to the manifest then). VICE
        comparisons then crop to the 200 lines below the banner. Record in
        [decisions.md](decisions.md) that the banner is house chrome outside
        the fidelity rule.
      - Then golden frames for all 100 levels.
      - `bb-readme.md`, a section in [reference-sources.md](reference-sources.md),
        three games in `docs/index.md`, `docs/toc.yml` and `README.md`, a
        screenshot, and a `CHANGELOG.md` line.

      Verify: the solution builds with zero warnings and every game's tests
      pass.

## 6. Open questions

- `hud-font.tga` and `digit-font.tga` are not exported. The HUD does not use
  them; what does is unknown until something draws them.
- Under the ceiling, the port moves a bubble between Y `$45` and `$47`; a
  capture summary said "parked at `$47`". A per-frame read of `$AA1E` for
  slot 17 settles it.
- Whether a jump can end in a fall with no floor under it (`$87F0` leaving
  `$FF` mid-arc) is untested. Level 1 has no gap wide enough.
- The static screen was never compared with VICE screenshots (levels 1, 30
  and 100). Item 2's frames cover this.
- `$A783` (the `$23B8` arm that makes type `$44` bubbles) and `$37C7` (the
  `$23DE` arm) belong to effects not yet translated.
