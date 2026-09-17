# Bubble Bobble Port Plan

How Bubble Bobble becomes the third game in The Sharp Kind, beside Elite and
Stunt Car Racer.

The port is a translation of
[zaidka/rebb64](https://github.com/zaidka/rebb64), a complete, documented,
byte-exact 6502 disassembly of the Commodore 64 conversion. That checkout is
referred to below as **`rebb64/`**, and a path like `rebb64/src/enemy-ai.s`
means a file in it, not in this repo.

This is a developer's document. It is the plan and the progress: tick a box
when the item is done and its verify step passes. Players want
[bb-readme.md](bb-readme.md), which does not exist yet.

## 1. Goal and scope

**Goal.** A faithful C# version of the C64 conversion of Bubble Bobble.

**Faithful to what.** To `rebb64/src/`, the byte-exact 6502. Not to the
arcade original. Where the C64 version differs from the arcade, the C64
version wins, because that is the version the reference documents and can
prove.

This is the same rule [reference-sources.md](reference-sources.md) sets for
the other two games, with one difference worth stating plainly. Elite's
golden source is the original 6502 and SCR's is a maintained C++ remake.
Bubble Bobble's is a disassembly of a home conversion, so the port inherits
every way Software Creations' C64 version differs from Taito's arcade
machine. That is accepted, not accidental.

**In scope.** 100 levels, two players, bubbles, enemies, items, EXTEND,
super mode, bonus rounds, scoring, sound.

**Out of scope.** VIC-II emulation. SID emulation. Cycle accuracy. The tape
loader. Any arcade-accuracy work.

## 2. Two standing rules

### Copy Elite, not Stunt Car Racer

When you need to know how something is done in this repo, read the Elite
equivalent — and particularly `EliteSharp.Renditions.EightBit`, which stands
in for an 8-bit machine and so faces the same problems this port does.
Elite is far more complete. SCR is the younger, thinner port, and copying it
propagates its gaps.

### SharpKind moves under you

The engine is in active development on `main`. Expect `IGraphics`,
`SharpKind.Graphics` and the test scaffolding to change while this port is
being written. So:

- Pull and rebuild before starting any phase.
- When something stops compiling, read what changed on `main` first. Do not
  work around it locally.
- After changing anything under `src/useful/`, run **Elite's and SCR's full
  test suites**, not just Bubble Bobble's. Neither game's golden frames may
  move.

## 3. Preconditions

- [x] **Licence.** `rebb64` has no `LICENSE` file and contains Taito and
      Firebird material in derived form. **Decided: the port ships the
      derived assets in this repo.** Andy's call, made deliberately and with
      the position understood. Revisit it before any release that is
      distributed more widely than the repo itself.
- [ ] **Toolchain.** .NET 10 SDK, Python 3, and `cc65`. .NET 10 and Python 3
      are present; `cc65` is not installed yet.
- [ ] **VICE.** Installed. It is the reference runner for visual comparison.
      Not installed yet, which is what blocks every VICE verify step below.
- [ ] **Baseline.** `make verify` passes in `rebb64/build/`. It checks the
      assembled output against the SHA256 of the original PRG, and it is
      what makes every later fidelity claim mean anything.

## 4. Shape

Three assemblies plus the app, mirroring Elite:

```
src/bb/libs/BubbleBobbleSharp.Abstractions/     contracts and view models
src/bb/libs/BubbleBobbleSharpLib/               the game: the 6502 translation
src/bb/libs/BubbleBobbleSharp.Renditions.EightBit/  the drawing, and the assets
src/bb/apps/BubbleBobbleSharp/                  entry point
src/bb/test/BubbleBobbleSharpLib.Fakes/
src/bb/test/BubbleBobbleSharpLib.Tests/
```

The division is Elite's, and it earns its place here for the same reasons:

- The game computes a **model**; the rendition **draws** it. A view is
  `IView<in TModel>`, holds no state and derives nothing.
- A view is handed an `IViewSurface` and sees nothing else of the game:
  graphics, layout, palette.
- The **rendition declares the screen size**, not `Program.Main`. The 8-bit
  rendition says 320x200 and picks its own window scales.
- A rendition is one of the three the engine names - `8-bit`, `16-bit` or
  `Modern`. This game's C64 is the 8-bit tier, the same one Elite's BBC Micro
  tier is, so it is named for the tier and not for the machine.
- The rendition carries its own `Assets/` folder, and the app's build drops
  it into `Renditions/` beside the executable, where the loader finds it.
  The app references it with `ReferenceOutputAssembly="false"`.

One rendition today. The shape is Elite's so that a second one — an arcade
or Amiga presentation — is an assembly rather than a branch in the game.

Every `.cs` file starts with the header the S1451 rule requires:

```csharp
// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.
```

## 5. What we reuse from SharpKind

| Need | Use |
|---|---|
| Window, loop, backends | `SharpKind.App.GameApp.Run`, `AddGameEngine`, `GameHost` |
| Drawing | `SharpKind.Graphics.IGraphics` |
| Sprites | **`SharpKind.Graphics` sprite engine — to be written, see Phase 1** |
| Frame composition | `LayerRunner`, `RenderLayer`, `ILayerDrawer` |
| Input | `IKeyboard`, `IGamepad`, `ControlBindings` |
| Sound | `ISound` plus `AudioController` |
| Assets | `IAssetLocator` and `AssetManifest.json` |
| Config | `ConfigFile<T>` |
| Headless tests | `SharpKind.Fakes.Harness.HeadlessGameHarnessBase<TState>` |
| Frame regression | `SharpKind.GoldenFrames` |

Elite and SCR both compose a frame as an ordered list of clipped
`RenderLayer`s, and Bubble Bobble should too. Its frame falls into that
shape without forcing: the sidebars, the 32-column playfield between them,
and the HUD.

## 6. Translation rules

Read this before writing any game code.

### 6.1 Arithmetic is 8-bit and wraps on purpose

Positions, velocities and counters are bytes. Overflow is not a bug — it is
the behaviour. Never widen to `int` to "fix" a wrap.

```csharp
// clc : adc  ->
byte sum = unchecked((byte)(a + b));
bool carry = a + b > 0xFF;

// sec : sbc  ->
byte diff = unchecked((byte)(a - b));
bool carry = a >= b;          // carry set means NO borrow
```

### 6.2 Branch translation

| 6502 | After `cmp`, means |
|---|---|
| `bcs` | `a >= operand` (unsigned) |
| `bcc` | `a < operand` (unsigned) |
| `beq` | `a == operand` |
| `bne` | `a != operand` |
| `bmi` | bit 7 set, i.e. `(a & 0x80) != 0` |
| `bpl` | bit 7 clear |

### 6.3 Common idioms

```
eor #$FF : adc #$01     ->  negate: unchecked((byte)(-x))
asl a                   ->  x << 1, carry = (x & 0x80) != 0
lsr a                   ->  x >> 1, carry = (x & 0x01) != 0
lda TABLE,x             ->  table[x]
```

The absolute-difference idiom appears throughout. In
`rebb64/src/enemy-ai.s`:

```
        lda     $BA,y          ; player X
        clc
        adc     #$02
        sec
        sbc     D_AA0C,x       ; minus enemy X
        bcs     L0D21
        eor     #$FF           ; absolute value
        adc     #$01
L0D21:  cmp     #$10           ; within 16 pixels?
        bcs     L0D45          ; no, skip
```

becomes:

```csharp
byte dx = unchecked((byte)(players.X[p] + 2 - enemies.X[e]));
if (players.X[p] + 2 < enemies.X[e])
{
    dx = unchecked((byte)-dx);
}

if (dx >= 0x10)
{
    return;     // too far apart on X
}
```

Each axis is tested separately. Keep it that way. A combined distance test
is the defect that cost two wrong diagnoses in Elite — see
[decisions.md](decisions.md).

### 6.4 Entity state

The 6502 keeps entity state in parallel arrays indexed by entity number:
`$CA,x` is type, `D_A9B2,x` is AI state, `D_AA0C,x` is X, `D_AA1E,x` is Y.

Keep the parallel arrays — they are what the algorithms assume — inside one
class with named accessors:

```csharp
internal sealed class EntityTable
{
    internal const int Capacity = 18;   // ldx #$11, counting down to 0

    private readonly byte[] _type = new byte[Capacity];
    private readonly byte[] _aiState = new byte[Capacity];
    private readonly byte[] _x = new byte[Capacity];
    private readonly byte[] _y = new byte[Capacity];

    internal Span<byte> Type => _type;
    internal Span<byte> AiState => _aiState;
    internal Span<byte> X => _x;
    internal Span<byte> Y => _y;
}
```

This is a deliberate exception to the repo's "derive don't store" rule. Do
not refactor it into a list of `Entity` records until the whole game is
verified working: the array layout is load-bearing while translating, and
`architecture-principles.md` is the target to move towards afterwards, not
a reason to diverge from the reference now.

This table stays on the game's side. What crosses to a view is a model.

### 6.5 Rules that are not negotiable

- One `.s` routine becomes one C# method, named after the routine.
- Keep the original address in a comment: `// $0CF2`.
- Never change a constant because the result "feels wrong". Re-read the
  `.s` file instead.
- Never invent behaviour the `.s` file does not contain.
- If a routine is unclear, stop and ask. Do not guess.

---

## Phase 0 — Scaffolding

Partly done as a single SCR-shaped library. The three-assembly split below
is what is left.

**Done**

- [x] `src/bb/` under the repo's own build files, in `TheSharpKind.slnx`
- [x] `BubbleBobbleSharp.csproj`, `BubbleBobbleSharpLib.csproj`, and the two
      test projects
- [x] `SDLProgram.cs`, modelled on Elite's
- [x] `BubbleBobbleMain : IGame, IGameApp` — clears the screen, quits on Escape
- [x] `BubbleBobbleServiceCollectionExtensions` with `AddBbConfig`, `AddBbMain`
- [x] `Config/BbConfig.cs`, `Config/BbConfigSettings.cs`
- [x] `FakeAbstraction` and two passing tests
- [x] Font: `bbc-micro.bmp` from Elite's 8-bit rendition — 8x8 cells,
      12 columns, which is the C64 character grid exactly
- [x] `palette.json`: the 16 C64 colours, Pepto values
- [x] `AssetManifest.json`: `MaxColours: 16`, `PaletteNamesEveryColour: true`,
      `ChannelBits: 8` — the same profile Elite's 8-bit rendition declares
- [x] Verified: solution builds with 0 warnings, 2/2 tests pass, and a
      scripted run dumps a 320x200 frame of 64,000 black pixels, exit 0

**Done — the three-assembly split**

- [x] `BubbleBobbleSharp.Abstractions`, referencing the engine's contracts and
      nothing else
- [x] `IBbRendition`, on Elite's `IRendition`: the rendition declares which of
      the engine's three it is, and its `Name` is derived from that
- [x] `BubbleBobbleSharp.Renditions.EightBit`, referencing Abstractions only.
      `EightBitRendition`, 320x200, window scales 1-4
- [x] `Assets/` moved into the rendition, where a rendition's assets belong
- [x] `RenditionLoader` and `InstalledRenditions`, on Elite's
- [x] The app's `CopyRenditions` target and its
      `ReferenceOutputAssembly="false"` reference
- [x] Screen size read off the loaded rendition, not `SDLProgram`
- [x] `BbConfig` sets both the default rendition and the fallback to `8-bit`,
      as `EliteConfig` does
- [x] Both new projects in `TheSharpKind.slnx`
- [x] Four loader tests and three config tests

**Verified:** the startup log reads `"rendition":"8-bit"` and `Loaded 1
rendition(s) from 1 plugin assemblies; drawing 8-bit.`, the frame is still
320x200 and black, and the app exits 0 on Escape.

Still to do, when there is a settings screen to do it in:

- [ ] Stop the game offering `16-bit` or `Modern`. Both are legal names the
      engine knows, and this game ships art for neither, so choosing one
      leaves it with nothing to draw with. Elite is unaffected - it ships
      both of the ones it offers.

## Phase 1 — The sprite engine

New shared code in `SharpKind.Graphics`, on top of `IGraphics`. It is shared
rather than local because a 2D sprite layer is engine work, not Bubble
Bobble's.

- [x] `SpriteSheet` — pairs the atlas bitmap's image name with its JSON
      index, and checks every rectangle lies inside the sheet at load. The
      bitmap itself stays with `AssetSet`, as every other image does, rather
      than being decoded a second time.
- [x] `SpriteAtlas` — name to `SpriteRect`, read from the index file. An
      unknown name faults rather than drawing nothing.
- [x] `SpriteRenderer` — draws a named sprite at a position, mirrored on
      request, over `IGraphics.DrawImagePart`. Named for what it does: it
      holds no frame state, so `SpriteBatch` would have misread.
- [x] Unit tests in `SharpKind.Graphics.Tests`.
- [x] `RecordingGraphics` records whatever new calls it makes — it needed
      nothing new. The renderer adds no method to `IGraphics`, and
      `ImageParts` already records `DrawImagePart`.

Rules, because this touches shared code:

- Additive only. No changed signatures on `IGraphics`.
- If `IGraphics` does gain a method, update `SoftwareGraphics`,
  `SDLGraphics` and `RecordingGraphics` in the same change.
- Elite's and SCR's full suites pass before and after, golden frames
  included.

**Verified:** the solution builds with 0 warnings; `SharpKind.Graphics.Tests`
is 289/289 with the new types covered; Elite (726 + 39), SCR (246) and every
other suite pass, golden frames included.

## Phase 2 — Asset extraction

`rebb64/data/` already holds the data in editable form. This converts it to
what the engine reads.

Being done in two chunks: the data first, the artwork second.

**Done — the data**

- [x] `tools/bb/export-csharp-assets.py`, reusing rebb64's own parsers in
      `convert-levels.py`, `convert-zone-data.py` and `convert-tga.py` by
      importing them from a checkout at run time, so what it exports is
      parsed by the very code `make verify` proves.

      It lives in this repo, not in the rebb64 checkout: it is our code
      generating our assets, and an upstream pull is where it would go
      missing. It takes `--rebb64 <path to a checkout>` and defaults
      `--out` to the 8-bit rendition's `Assets`. Re-running it over an
      unchanged checkout reproduces every committed asset byte for byte,
      which is the cheapest check that the assets and the script still
      agree.
- [x] `Levels/levels.json` — 100 levels: the 32x23 bitmap as a string array,
      plus `colors`, `sidebar`, `bubbleCurrent`, `wrapOpenings`, `foodDrop`,
      `powerupSpawn`, `spawnFlags` and the enemy list.
- [x] `Levels/zones.json` — from `zone-data.txt`, with `mirror` expanded and
      `level N -> M` redirects resolved, so the port reads rectangles and
      nothing else. The mirror axis is column 16, which is the one
      `convert-levels.py`'s `is_symmetric` uses.
- [x] `LevelStore` in the lib, plus `Level`, `EnemySpawn` and `LevelPoint`.
      Levels are asked for by the number the game counts in, 1 to 100.

**Verified:** 15/15 in `BubbleBobbleSharpLib.Tests`. A test loads all 100
levels, asserts there are 100 and that they are numbered 1 to 100, asserts
every bitmap is 23 rows of 32 `#`/`.`, and asserts level 1's row 8 is
`..##...##################...##..`.

**To do — the artwork**

Amended 2026-09-17: no packed atlas. The original plan packed every `.tga`
into one BMP with a recoloured copy of the tile set per level colour. Both
halves of that are dropped, for separate reasons.

*Packing buys nothing here.* An atlas exists to keep a batched renderer
from rebinding textures. Neither backend batches: `SDLGraphics` holds one
texture per image name and issues one `SDL_RenderTexture` per
`DrawImagePart`, and `SoftwareGraphics` blits per pixel from a `FastBitmap`
it looks up by name. Thirty sprites cost thirty draw calls either way, and
at 320x200 the difference is unmeasurable. Against that, a packed atlas is
a generated blob that has to be rebuilt whenever any `.tga` changes, whose
diffs say nothing, and it needs an index file that is a second source of
truth. The engine reads TGA directly and `AssetManifest.Images` is already
a name-to-file map, so separate files are also the shorter path.

*Recolouring belongs at draw time, not in a committed asset.* See Phase 3.

- [x] Each `.tga` is its own image, declared in `AssetManifest.Images` and
      read by `TgaReader`. Eleven sheets. The export rewrites the 24-bit
      colour map as 32-bit so index 0 can carry alpha 0: these are C64
      multicolour sheets, where bit-pair 00 is the background showing
      through rather than a colour, and `TgaReader` would otherwise make it
      opaque black. Every colour in every sheet was already one of the 16
      Pepto values `palette.json` names, so the budget needed no work.
- [x] `SpriteAtlas.Grid` in the engine, for sheets that are a plain grid of
      equal cells - which all of these are, so no JSON index is needed.
      Names run `{prefix}-{n}` in reading order. Each sheet's cell size gets
      declared with the view that draws it, in Phase 3, rather than guessed
      at now.
- [x] Replaced `bbc-micro.bmp` with the game's own `charset.tga`. It had to
      be reordered, not copied: the game's charset runs digits first, and
      only the letters happen to fall where `char - ' '` looks for them.
      The export rewrites it into the order `BitmapFont` reads, 16 columns
      of 8x8, leaving unmapped cells blank.

      Amended 2026-09-17 with the HUD below: the sheet carries one cell that
      is not an ASCII character. Screen code `$1D` is the marker the HUD
      counts a player's lives in, and it goes in the sheet's last cell,
      which ASCII spells underscore and this game never prints. A charset
      cell belongs in the font rather than in an image of its own, because
      that is what lets a view colour it the way colour RAM does.
- [x] `tile-edges.tga`, added 2026-09-17 with the playfield below. The level
      renderer draws a tile's right-hand edges and the shadow it casts from
      screen codes `$0A` to `$0F`, which live in the charset rather than in a
      sheet of their own, so the export cuts the six of them out of it. They
      are multicolour - `game-loop.s` fills colour RAM with `$0D` before the
      level is drawn, and bit 3 of that is what puts every cell of the
      playfield in multicolour mode - while `charset.tga` stores the charset
      one pixel per bit, so the export re-reads each row's eight bits as the
      four bit-pairs the hardware makes of them. That is also the proof: read
      as bits the six are a dither, and read as pairs they are solid edges.
- [ ] `hud-font.tga` is still unexported - 10 cells of 8x8, guessed here to
      be the HUD's digits. Phase 3 has settled that it is not: the HUD draws
      screen codes `$00` to `$09`, which are the charset's own digits, so
      the HUD needs none of this file. What it *is* for is still open, and
      still only decidable from something that draws it. `digit-font.tga`
      is in the same position.

**Verified:** 26/26 in `BubbleBobbleSharpLib.Tests`. `AssetSet.Load` opens
every file the manifest names and passes the C64 colour budget - 16
colours, all named, no partial alpha. Tests prove the sheets kept both
transparent and opaque pixels, and that `0`, `9`, `A` and `Z` have ink
where `BitmapFont` looks for them while space stays blank. Elite (726 +
39), SCR (246) and `SharpKind.Graphics` (294) all pass.

## Phase 3 — Static rendering

No game logic. Put a level on screen. Three `RenderLayer`s, each with its own
`ILayerDrawer`, each drawing a model the game computed.

Translate: `level-renderer.s`, `level-display.s`, `graphics-copy.s`, and the
sidebar handling in `render-screen.s`.

Amended 2026-09-17: two things this phase assumed turned out not to be so,
and the reference settles both.

*The sidebars are inside the playfield, not beside it.* `setup_level_screen`
writes the level from the start of each screen row and stops after 32
columns, and `draw_border` then clears column 32 as the first column past it
— so the level is the **leftmost** 32 of the screen's 40 columns, hard
against the left edge, and the eight columns to the right of it are not the
level's. The sidebar decoration sits at the level's own columns 0-1 and
30-31, over bitmap that `init_level_renderer` forces solid. What the right
eight columns hold is still unknown and is the HUD bullet's problem.

*Multicolour sheets are half screen width.* A multicolour character is four
pixels in the sheet and eight on screen, because a multicolour pixel is two
pixels wide. The export copies the `.tga` files across as they stand, so the
doubling is done at draw time by the view, which passes a destination width
of twice the source. `SpriteRenderer` draws at native size only and so is no
use for these sheets; a view calls `DrawImagePart` itself. If a later phase
wants the sprite engine here, the additive change is a scale on
`SpriteRenderer.Draw`.

- [x] `SidebarModel` / `SidebarView8Bit` — sidebars from `sidebars.tga`,
      chosen by the level's `sidebar` byte. Named for the tier, not the
      machine, as section 4 and Elite both have it.

      A design is four characters, drawn as a two-by-two block and repeated
      down both edges: twelve blocks cover rows 0-23 and the last row takes
      the block's top half on its own. The index is tested against a
      hundred rather than against the fifty-nine designs that exist, and the
      41 levels past it have no design — they repeat their own header tile,
      out of `level-tiles.tga`, into all four characters.

      Brought the view seam over from Elite to do it: `IView<TModel>`,
      `IViewSurface`, `BbViewLayout`, and `CreateSidebarView` on
      `IBbRendition`. `BubbleBobbleSharp.Abstractions` gained the graphics
      and asset references that costs.

      `BbViewLayout` states its metrics as `init` properties rather than
      constants, and exposes the screen's grid and the level's through
      `CharacterGrid`, new in `SharpKind.Graphics`. Elite moved onto the
      same type in the same change, so the two games share one
      implementation rather than diverging — see
      [decisions.md](decisions.md), 2026-09-17.

**Verified:** the solution builds with 0 warnings and
`BubbleBobbleSharpLib.Tests` is 33/33. Elite (726 + 39), SCR (246) and every
engine suite pass unchanged. The view's drawing is proved against
`RecordingGraphics`: a hundred characters an edge pair, at the four columns
and twenty-five rows expected, from the right row of the right sheet.

Still unverified against VICE, which is not installed: that the doubling and
the block order look right on screen.
- [x] `PlayfieldModel` / `PlayfieldView8Bit` — the level itself. Named for the
      tier rather than the machine, as the sidebar is and as section 4 has it.

      It is a 32x**25** grid, not 32x23. `init_level_renderer` builds the
      hundred bytes the renderer walks out of three parts: a ceiling on row 0
      and a floor on row 24, each half of each taken from `$E36E`/`$E371`
      according to that half's `wrapOpenings` bit, with the level's own
      twenty-three rows between them. Every one of those rows has its top two
      bits set as it lands, so the level's leftmost two columns are wall
      whatever the level drew - which is what the sidebar decoration sits on.

      What crosses to the view is screen codes, because the renderer's
      decisions are made in them: a set bit puts the level's own tile down and
      three characters with it, and which edge and which shadow those are
      depends on what the cell holds already. That is what joins a row of
      tiles into one platform edged once at its end. `$E16F` then turns the
      top row's plain edges into the other kind, since nothing above it casts
      the shadow the plain one leaves room for.

      Two bits of the reference are deliberately not here. The bits
      `init_level_renderer` ors the `bubbleCurrent` nibble into at `$8B03` and
      `$8B63` cannot change what is drawn: both tables end `$FF`, so every one
      of them is set already. And the level is
      drawn across all 32 of its columns rather than clipped to the 28 between
      the sidebars, because `draw_border` writes over the outer two each side
      straight afterwards and doing the same work in the same order keeps the
      two views from having to know about each other. The clip belongs to the
      `RenderLayer` when the frame is composed.

      Not done here, and still the next bullet: the level's `colors` byte.
      Everything this draws is the artwork's own colours.

**Verified:** the solution builds with 0 warnings and
`BubbleBobbleSharpLib.Tests` is 48/48. All 100 levels build a grid holding
nothing but the eight characters the renderer can write, and all 100 have
their leftmost two columns walled off. Particular cells traced through the
reference by hand are checked on particular levels: a ceiling with an
opening in it, a ceiling without, and level 1's ninth row - four tiles, the
edge that ends them, and the three shadow characters on the row beneath.
The view's drawing is proved against `RecordingGraphics`: an empty level
draws nothing, a full one draws 800 characters ending at the bottom right,
and each character comes from the right part of the right sheet.

Still unverified against VICE, which is not installed: that the shadows and
the edges land the right way round on screen.

- [x] Recolour the tile sheet at level load, from the level's `colors` byte,
      and hold the recoloured `FastBitmap` for as long as the level lasts.
      The tiles are 2-bit multicolour and the colour byte picks what the
      four entries mean, so keeping the indices until draw time is both the
      faithful model and the one that costs nothing: a recolour per level
      beats a baked copy per colour combination.

      `MulticolourSheet` in the rendition does it, one per sheet a view
      draws from. It repaints the sheet under a name of its own -
      `LevelTiles.Recoloured` - and skips the work while the colour byte is
      the one it last painted, so the cost is a level rather than a frame.

      Where the four entries point is settled by the reference: `$E0CE`
      splits the colour byte into `$1D` and `$1F`, and the split-screen IRQ
      at `$072E` writes those to the two background registers in that order,
      so entry 01 is the high nibble and 10 the low. Entry 11 is the cell's
      colour RAM, which `game-loop.s` fills the playfield with as `$0D`
      before the level is drawn - and multicolour mode reads only its low
      three bits, so entry 11 is colour 5 on every cell. Entry 00 is the
      screen background, which the export already wrote as a transparent
      pixel and nothing repaints.

      **All three of the playfield's sheets, not just the tiles.** The
      sidebar decoration is inside the level rather than beside it, over the
      same two registers, and `tile-edges.tga` is cut from a charset drawn
      in multicolour too. Repainting one and not the others would put a
      level's colours on its tiles and leave the edges and the decoration in
      the artwork's.

      Two additions to `SharpKind.Graphics` paid for it: `FastBitmap.Recolour`,
      which exchanges named colours for others, and `IGraphics.Image` /
      `IGraphics.SetImage`, which read a loaded image back and give a name a
      new one. The SDL backend rebuilds that name's texture when it takes
      one, which is why this is a per-level operation and not a per-frame
      one.

**Verified:** the solution builds with 0 warnings and
`BubbleBobbleSharpLib.Tests` is 51/51. Elite (726 + 39), SCR (246) and every
engine suite pass unchanged. The repaint is proved against sheets whose
pixels are the entry number wearing the palette colour of the same number, so
a repainted pixel says outright which entry the level pointed where: entry 01
lands on the high nibble's colour, 10 on the low nibble's, 11 on colour 5, and
00 is left alone. A second draw at the same colour byte is proved to reuse the
bitmap it painted, and a third at a different one to repaint.

Still unverified against VICE, which is not installed: that the colours
themselves are the ones the C64 shows.
- [x] Compose the frame, so what the views draw reaches the screen. This was
      not a bullet and should have been: the phase's preamble asks for
      `RenderLayer`s and nothing built them, so every view above was proved
      against `RecordingGraphics` and drawn by nobody. The game ran to a
      black screen.

      `BbViewSurface` in the game is the three members `IViewSurface` is -
      graphics, `BbViewLayout`, palette - on Elite's `EliteDraw`, minus the
      drawing Elite does for itself and this game has none of yet.
      `BubbleBobbleMain` builds it, asks the rendition for both views, and
      holds a `LayerRunner` of two bands.

      The bands are in the reference's own order: the level, then the
      decoration over it. The level's band is trimmed to the 28 columns the
      decoration leaves, which is where the clip the playfield bullet
      promised finally lands - the view still draws all 32 columns and still
      knows nothing about the sidebar, exactly as `draw_border` writes over
      the outermost two each side after `setup_level_screen` has run.
      `BbViewLayout` gained `PlayfieldArea` and `PlayfieldInterior` to say
      where those two rectangles are.

      The rendition and a `LevelStore` are services now. The levels are read
      from `<rendition>/Assets/Levels/levels.json` rather than through the
      manifest, which names images, fonts and a palette and has no business
      knowing about a hundred levels.

**Verified:** the solution builds with 0 warnings and
`BubbleBobbleSharpLib.Tests` is 54/54. Elite (726 + 39), SCR (246) and every
engine suite pass unchanged. The composition is proved headlessly: two clip
regions, the level's and the decoration's, at the rectangles above; and the
decoration drawn after the level, which is shown by where it lands rather
than by which sheet it comes from, since level 1 has no design and repeats
its header tile out of the tile sheet.

Proved in the real app too, through `GAME_KEY_SCRIPT` and
`GAME_FRAME_DUMP_DIR`: the game runs, exits 0 and dumps a 320x200 frame with
content in all 32 character columns and all 25 rows. That frame also settles
the recolour end to end. Level 1's colour byte is `$21`, which swaps entries
01 and 10, and its tile holds 16 pixels of each - so the tiles say nothing.
The edge characters hold entry 01 only, and they came out colour 2 rather
than colour 1: 32 x 222 tile characters is the 7 104 white pixels, and the
3 072 more red ones are 1 536 edge pixels at double width. Entry 01 was
painted the high nibble, as the reference has it.

- [x] `HudModel` / `HudView8Bit` — both scores, high score, lives. Named for
      the tier like the other two views, rather than `HudViewC64` as this
      bullet first had it.

      The HUD is the eight columns the level leaves at the right of the
      screen. Five of its rows fall out of the pointers
      `update_sprite_animations` builds at `$E3A7` and the arguments `$046C`
      passes for the lives: on a forty-column screen from `$5000`, `$50E9`,
      `$5139`, `$5201`, `$5251` and `$5341` are rows 5, 7, 12, 14 and 20,
      all at column 33. The labels come from the string at `$AB93`, which
      positions each one itself at column 35, a row above the score it
      names.

      A score is three bytes of packed BCD, which is six digits, because
      `add_score` at `$7C24` adds in decimal mode and carries from one byte
      to the next. `$3FB0` blanks a leading zero until it has seen a digit
      that is not one, and always writes the last digit, so a cleared score
      reads as a single zero rather than an empty row. That is a rule about
      the score rather than about drawing it, so `HudModel` settles it and
      the view draws what it is handed. The lives fill their row of seven
      from the right-hand end, and a negative count leaves the row alone
      altogether — which is how a player who is out gets no row rather than
      a row of spaces.

      The HUD is hires, not multicolour. `fill_color_ram` does put `$0D`
      over the whole screen before the level is drawn, but
      `display_text_string` writes a colour byte beside every character it
      prints, so `$AB93` repaints these columns on its way past. None of the
      four bytes it leaves has bit 3 set: the labels are colour 7, player
      one's rows 5, player two's 3, and the high score 1. So nothing here is
      repainted per level and no sheet is touched — it is all font.

      The digits are screen codes `$00` to `$09`, which the charset already
      holds in that order and the export already maps to ASCII. The life
      marker is screen code `$1D`, and it now travels in the font sheet too,
      in the cell ASCII spells underscore and this game never prints. That
      is what lets the view colour it the way colour RAM does, since a grid
      font's white ink takes whatever colour it is drawn in. `hud-font.tga`
      is therefore still unexported and is **not** the HUD's digits, as the
      Phase 2 bullet guessed — the HUD draws none of it.

**Verified:** the solution builds with 0 warnings and
`BubbleBobbleSharpLib.Tests` is 85/85. Elite (726 + 39), SCR (246) and every
engine suite pass unchanged. The digits and the blanking are proved against
the model, and the positions, colours and right-hand fill against
`RecordingGraphics`.

Proved in the real app too, through `GAME_KEY_SCRIPT` and
`GAME_FRAME_DUMP_DIR`: the game exits 0 and dumps a frame whose HUD columns
hold ink on exactly the eight rows above, in exactly those four colours. The
counts settle the rest. Rows 5, 12 and 20 hold 30 pixels each, which is one
`0` glyph — six drawn digits would be 180, so the blanking is real. Rows 7
and 14 hold 96, which is three markers at the 32 pixels cell `$1D` carries.

- [ ] A debug key that steps to the next level.
- [ ] Confirm the fourteen unverified palette entries against VICE.

**Verify:** VICE screenshots of levels 1, 30 and 100 against F12 frame dumps
of the same three. Tile layout, colours and sidebars match. Commit those
three as the first golden frames.

## Phase 4 — Player

Translate: `joystick-input.s`, `player-movement.s`, `player-state.s`,
`player-animation.s`, `platform-collision.s`, and the sprite-choosing part
of `player-sprites.s`.

- [ ] `PlayerTable`, holding the per-player bytes from `master.s`.
- [ ] `Input.Read()` producing the same bit layout the 6502 joystick routine
      does. Keep the bit layout — the movement code reads it directly.
- [ ] Walk, jump, fall, land, face left and right, animate.
- [ ] `PlayerModel` and its view.

**Verify:** headless harness. Hold right for 40 ticks from a known start on
level 1; the player's X byte matches VICE at the same tick. And: the player
never falls through a `#` tile on any of the 100 levels.

## Phase 5 — Bubbles

Translate: `bubbles-sprites.s`, `bubble-handler.s`, `entity-bubble-handler.s`.

- [ ] Fire, travel, expire, pop on contact.
- [ ] Drift on the level's `bubbleCurrent`.
- [ ] Wrap-around openings from `wrapOpenings`.

**Verify:** a bubble fired on level 1 reaches the same tile as in VICE after
30 ticks, and one left alone expires after exactly the number of ticks the
`.s` file counts.

## Phase 6 — Enemies

Translate: `entity-system.s`, `entity-state-tables.s`, `enemy-ai.s`,
`special-enemies.s`, `collision.s`, `entity-collision.s`,
`entity-interaction.s`, `entity-spawn.s`, `spawn-handlers.s`.

The largest phase. One enemy type at a time. `entity-system.s` is 1,447
lines and goes first, because everything else indexes into it.

- [ ] `entity-system.s` and the state tables.
- [ ] `AddBbRandom`, with the RNG at `LE9EA`.
- [ ] Each enemy type, one at a time.
- [ ] Baron Von Blubba (`baron_von_blubba` in `special-enemies.s`).
- [ ] The anger state that speeds enemies up.

**Verify:** each type spawned on an empty test level matches a recorded byte
trace from VICE over 200 ticks. A golden frame per enemy type.

## Phase 7 — Items, scoring and progression

Translate: `item-collision.s`, `special-item-effects.s`, `extend-bonus.s`,
`super-bonus.s`, `bonus-round.s`, `bonus-stage-extended.s`, `level-setup.s`,
`level-start.s`, `level-complete.s`, `level-transition.s`, `game-loop.s`,
`game-init.s`, `game-init-early.s`.

- [ ] Food drops and powerups.
- [ ] The EXTEND letters.
- [ ] Super mode.
- [ ] Bonus rounds.
- [ ] Level advance, lives, game over.

**Verify:** a scripted playthrough of level 1 to level 2 scores the same as
the real game, and six EXTEND letters award a life.

## Phase 8 — Sound

Do **not** port `rebb64/src/sound.s`. It is a SID player.

- [ ] List every trigger ID in `rebb64/src/sid-wrapper.s`.
- [ ] `make sid`, then record one `.wav` per effect and per tune from VICE
      or sidplay.
- [ ] `Assets/SFX/` and `Assets/Music/`, declared in the manifest.
- [ ] Drive them with `ISound` and `AudioController`, called from the same
      places `sid-wrapper.s` is called from.

**Verify:** a test over the manifest proves every trigger ID has an asset.

## Phase 9 — Front end

Translate: `title-screen-data.s`, `credits-handler-partial.s`,
`screen-scroll.s`.

- [ ] Title screen and attract mode.
- [ ] One or two player select.
- [ ] Game over and high score.
- [ ] Wired as `IGameScreen`s in a `ScreenManager<GameMode, IGameScreen>`.

**Verify:** every screen is reachable and every screen returns to the title.

## Phase 10 — Joining the repo properly

**This phase is ordered.** The resolution change below invalidates every
golden frame, so it lands *before* the frames are taken, not after.

- [ ] **320x256, with a banner across the top.** The repo's 8-bit tier is
      320x256 — Elite's is — and Bubble Bobble should match it rather than
      being the one game at 320x200. The C64's screen is 25 rows of 8, so
      the level takes 200 of those lines and the remaining 56 become a
      banner at the top: exactly 7 character rows, the mirror of Elite's
      56-pixel console band at the bottom.

      Deferred here deliberately (2026-09-17), not overlooked. It is cheap
      while nothing encodes the geometry and dear once the VICE work does,
      so if it slips any earlier in the schedule, take it earlier.

      What it touches:

      - `EightBitRendition.ScreenHeight` to 256.
      - `PlayfieldRow` to 7 where the layout is built. It is already an
        `init` property for this, so the playfield shift is one value.
      - Whatever asserts screen positions by then — today that is only
        `SidebarView8BitTests`.
      - `BannerModel` and its view, drawing an image Andy supplies. It is
        the one part of the frame with no counterpart in `rebb64`.

      Two consequences:

      - **The frame stops being the C64 screen.** Every VICE comparison
        from here on has to crop to the 200-line band below the banner
        rather than matching the whole frame. Phases 3 to 9 all compare
        against VICE, so any of their verify steps still being re-run need
        this said.
      - **The banner cannot be verified against anything.** It is house
        chrome, so it is outside the fidelity rule in section 1 rather than
        an exception to it — worth a line in [decisions.md](decisions.md)
        saying so, since the port's whole premise is that the C64 wins.

- [ ] Golden frames for all 100 levels. **After** the resolution change.
- [ ] `docs/bb-readme.md`, modelled on [elite-readme.md](elite-readme.md).
- [ ] A Bubble Bobble section in [reference-sources.md](reference-sources.md),
      naming `rebb64` as golden and saying plainly that the port targets the
      C64 version, not the arcade.
- [ ] `docs/index.md`, `docs/toc.yml` and `README.md` list three games.
- [ ] A screenshot in `docs/images/`.
- [ ] A line in `CHANGELOG.md`.

**Verify:** the whole solution builds with zero warnings, and Elite's, SCR's
and Bubble Bobble's tests all pass.

## 7. How to know the port is right

Three checks, in increasing strength:

1. **Golden frames** catch changes the port makes to itself. They do not
   prove the port matches the C64.
2. **VICE byte traces** do. Break in the VICE monitor at a known tick, dump
   the entity arrays, and compare them to the port's `EntityTable`. Use this
   whenever behaviour is in doubt.
3. **`make verify`** proves `rebb64/src/` is still the real game.

When the port and the C64 disagree, the C64 is right. Read the `.s` file.
Do not tune the constant.

## 8. Notes for the implementer

- One phase at a time. Commit at the end of a phase, not during.
- Do not commit automatically. Andy commits.
- UK spelling everywhere, including in code comments.
- `dotnet test` is blocked in the sandbox. Run the test executables
  directly from `bin/`.
- If a `.s` routine does not make sense, say so and stop. A guess that looks
  right is worse than a question.
