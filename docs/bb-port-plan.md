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
- [x] **Toolchain.** .NET 10 SDK, Python 3, and `cc65`. All present.
      `cc65` is the V2.19 win32 snapshot, unpacked to `%USERPROFILE%/cc65`
      and not on `PATH`, so a build prepends `%USERPROFILE%/cc65/bin` to it.
      `make` comes from winget's `ezwinports.make` and is on `PATH`.
- [x] **VICE.** Installed, 3.10 GTK3 win64, from winget. It is not on `PATH`
      either; `x64sc.exe` is under
      `%LOCALAPPDATA%/Microsoft/WinGet/Packages/VICE-Team.VICE.GTK3_*/GTK3VICE-3.10-win64/bin`.

      It does not have to be driven by hand. `-remotemonitor` puts the text
      monitor on a TCP socket, and everything the verify steps below want is
      scriptable over it: `ll` loads `rebb64.lbl` so that traps report rebb64's
      own routine names, `l` loads a PRG, `break exec`/`break store` set
      breakpoints and watchpoints, `cond <n> if <expr>` narrows them by
      register or memory value, `m`/`d` read memory, and `screenshot "<f>" 2`
      writes a PNG. The monitor answers while the emulator is running, but it
      resumes free when the connection drops, so one experiment is one
      connection.

      **Use `-binarymonitor`, not `-remotemonitor`.** The binary monitor's
      checkpoints work and report hits properly: a `CHECKPOINT` message with a
      hit count, then `STOPPED` carrying the program counter. That is the
      whole investigation tool, and the text monitor never produced a single
      trap in a day of trying.

      The program counter in `STOPPED` is the instruction **after** the one
      that stored, which is confirmed twice over - once against a store worked
      out by hand from `player-state.s`, once against `L22C2`, whose address
      its own label gives away.

      An earlier draft of this document stated flatly that checkpoints never
      fire over the text monitor. **That was not proved.** The control behind
      it set a breakpoint on `$A605` and waited at the title screen, but
      `$A605` is `game-init.s`'s wait loop and the title screen runs the one at
      `$4565` in `game-init-early.s`, so the address never executed and the
      control proved nothing. The same flaw sank two other controls. Only a
      breakpoint on `$E498`, which is `wait_one_frame` and where the program
      counter provably sits, settled it - and that was on the binary monitor.
      Whether the text monitor would pass the same control is still untested.
      Pick a control address you have watched the program counter sit in.

      An earlier draft of this document described getting a credit in by
      hunting for the title's wait loop, breaking on the port read at `$A605`
      and setting `PC` to its `rts` at `$A624`. **That recipe is withdrawn.**
      The breakpoint never fired; forcing `PC` while the CPU sat in
      `wait_one_frame` popped the stack and happened to land somewhere that
      advanced the game. It worked once, by accident, and not by the mechanism
      claimed. Use real input instead, which does work.

      **Input.** VICE's keyset joystick is the way in. There are no
      command-line options for the mappings, only for the device, so the
      mappings go in `%APPDATA%/vice/vice.ini` under `[C64SC]`: `JoyDevice2=2`
      picks keyset 1 for port 2, which is player 1 here, `KeySetEnable=1`, and
      `KeySet1North`/`South`/`West`/`East`/`Fire` take GDK keyvals, which for
      ordinary letters are simply their ASCII codes - 119, 115, 97, 100 and 32
      give W, S, A, D and space. Set `SaveResourcesOnExit=0` as well or VICE
      overwrites the file when it closes.

      The keys themselves go in with `SendInput`, as separate key-down and
      key-up events after `SetForegroundWindow`, because a direction has to be
      *held*: anything that sends a keystroke as one event cannot hold a stick
      down for forty ticks. Verified at the hardware rather than by eye -
      `$DC00` reads `$7F` idle and `$77` while D is held, which is bit 3, which
      is right.

      **The front end, driven for real:** autostart the PRG, wait for the
      title, send space, wait, send `1`. That is a one-player game on level 1,
      with `$B2` = `$01`, `$BA` = `$2C`, `$C2` = `$DD` and three lives in both
      of `$045A` and `$045B`.

      **Watch out:** the text monitor is flaky across connections. A fresh
      connection after a previous one closed sometimes returns nothing at all,
      and the emulator resumes free when a connection drops. Keep one
      connection for a whole experiment, including whatever you want to read
      *after* a trap fires. The binary monitor has an explicit stop and is
      probably the fix if this keeps costing runs.
- [x] **Baseline.** `make verify` passes in `rebb64/build/`. It checks the
      assembled output against the SHA256 of the original PRG, and it is
      what makes every later fidelity claim mean anything.

      **Passed 2026-09-18.** `rebb64-raw.prg`, 64,509 bytes, hash matches. The
      build also drops `rebb64.lbl`, which is what makes the monitor's traps
      readable.
- [x] **A runnable PRG.** `make verify` proves the image; it does not produce
      one that starts. `make release` does, and it needs `tscrunch`. The
      TSCrunch checkout at `C:/code/github/tonysavon/TSCrunch` ships a built
      `bin/windows_amd64/tscrunch.exe`, so the Makefile's own variable is
      enough: `make release TSCRUNCH=<that path>`. It writes `rebb64.prg`,
      44,673 bytes, self-starting at `$0801`, and `x64sc -autostart` runs it.

      **Correction.** An earlier draft of this item claimed the game hung at
      `$E496` on `lda $08 / cmp $08 / beq`, and blamed the missing `tscrunch`
      stage. That was wrong on both counts. `$E496` is `wait_one_frame`: the
      ordinary wait for the interrupt to tick `$08`, which is where the game
      sits for most of every frame, so any sample of the program counter is
      likely to land in it. The crunched PRG reaches the same loop and is
      perfectly healthy. Sample the screen, not the program counter.

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
- [x] `palette.json`: the 16 C64 colours, Pepto values. Corrected
      2026-09-17 — the values first committed here were not Pepto's. Only
      black and white matched; the other fourteen were wrong, and are now
      taken from `Assets/Images/c64-pepto-colodore.png`, a 16x1 truecolour
      strip of one pixel per colour, running 0 to 15 in C64 colour order
      and verified to match `palette.json` entry for entry. These sixteen
      are now the only colours the rendition uses.

      It carries no palette chunk, being truecolour rather than indexed,
      so it is read by its pixels. Nothing in the build reads it at all -
      it is the palette's provenance kept beside the palette, not a
      loaded asset.
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
      opaque black. The colour map written is the rendition's own rather
      than the one rebb64's TGAs carry — see `load_canonical_palette`.

      Amended 2026-09-17 with the palette correction above. This bullet
      used to claim every colour in every sheet was already one of the 16
      values `palette.json` named, so the budget needed no work. That was
      true only because the palette had been written to agree with the
      sheets. Against Pepto's real values the sheets carried two colours
      no entry named, and the union came to 18 against a cap of 16.

      The fix is not to repaint the sheets but to stop reading a colour
      out of them. A sheet's pixel value is a C64 entry number rather than
      a colour — `MulticolourSheet` repaints entries 1, 2 and 3 at load
      time, so what is committed is never what reaches the screen — which
      makes the colour map pure naming. The export now writes the
      rendition's palette, cut to the length the source indexed, so a
      sheet cannot disagree with `palette.json` again. Ten of the sheets
      changed; `bubble-masks.tga`, `charset.tga` and `tile-edges.tga` did
      not, because they index only black and white.
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

- [x] A debug key that steps to the next level. `N`, read in
      `BubbleBobbleMain.Update` with the keyboard's one-shot `IsPressed`, so a
      held key steps one level rather than fifty a second. It wraps at 100
      back to 1, because the hundredth level's neighbour has to be something
      and the first is the only level this port can name without the
      progression Phase 7 translates.

      Ungated, unlike Elite's `ELITE_DEBUG_YAW`. That switch hides a departure
      from the original from a player in normal play; there is no player and
      no normal play here, only a hundred levels and one way to look at them.
      The key goes when the front end arrives.

**Verified:** the solution builds with 0 warnings and
`BubbleBobbleSharpLib.Tests` is 88/88. Elite (726 + 39), SCR (246) and every
engine suite pass unchanged. The step, the wrap and the one-level-per-press
are proved against the fake keyboard.

Proved in the real app too, through `GAME_KEY_SCRIPT` and
`GAME_FRAME_DUMP_DIR`: a script that taps `N` 29 times, then 70 more, then
once more, and dumps a frame at each stop, exits 0 and leaves four frames.
Levels 1, 30 and 100 are three different pictures, and the frame after the
wrap is byte-identical to the opening one - so the key moves the screen, and
moves it back to where it started.

- [x] Confirm the fourteen unverified palette entries against VICE. Settled
      2026-09-17 without VICE, and settled the other way: the fourteen were
      not merely unverified but wrong. Andy supplied Pepto's own values and
      `palette.json` now carries them, so there is nothing left to confirm
      — the palette is the published one rather than a guess awaiting a
      check. What VICE is still wanted for is the tile layout and the
      per-level colour choices, which the verify step below covers.

**Verify:** VICE screenshots of levels 1, 30 and 100 against F12 frame dumps
of the same three. Tile layout, colours and sidebars match. Commit those
three as the first golden frames.

## Phase 4 — Player

Translate: `joystick-input.s`, `player-movement.s`, `player-state.s`,
`player-animation.s`, `platform-collision.s`, and the sprite-choosing part
of `player-sprites.s`.

- [x] `PlayerTable`, holding the per-player bytes from `master.s`. One array
      per field, two entries long, indexed by player - the section 6.4 shape,
      not one object per player. Five fields, because five is what `master.s`
      names as a player's own: state (`$B2`), X (`$BA`), Y (`$C2`), the bubble
      timer (`$5D`/`$5E`) and lives (`$045A`/`$045B`, from
      `game-variables.s`). The rest of what a player moves on lives in the
      `$85xx`-`$88xx` arrays, which are eight entries long and shared with
      every other entity, so they belong to Phase 6's `EntityTable` rather
      than here. Each field is added as the routine that reads it arrives.

      State, X and Y are zero-page arrays the 6502 indexes exactly as this
      class does. The bubble timer and lives are not arrays there at all -
      each is a pair of separately named bytes - but they are indexed here
      too, because the caller is holding a player number and a pair of named
      fields would make every caller branch on it.
- [x] `Input.Read()` producing the same bit layout the 6502 joystick routine
      does. Keep the bit layout — the movement code reads it directly. A CIA
      port reading, bit for bit: bit 0 up, 1 down, 2 left, 3 right, 4 fire,
      and a bit reads *clear* while its direction is pushed, because each is a
      switch to ground. Two bytes, indexed by player as `PlayerTable` is -
      player 1 is port 2 (`$DC00`, read first at `$1CBD`) and player 2 is port
      1 (`$DC01`). Bits 5 to 7 are left high: on the C64 they carry a keyboard
      matrix row rather than the stick, and nothing in the reference reads
      them off a joystick byte.

      Keys for both players, and a pad for player 1 only - `IGamepad` answers
      for one active device at a time, so a second stick is attached but
      cannot be read alongside the first. Keys and pad drive the same five
      bits, so holding both is one push. `IsHeld` throughout, never
      `IsPressed`: the 6502 re-reads the ports every frame and sees a held
      stick held.

      No keyboard scheme comes from the reference - the C64 had a stick in
      each port and no keyboard alternative at all - so the two schemes are
      this port's own: arrows plus Space, and W/A/S/D plus Q. No
      `ControlBindings` file yet, and section 5 still expects one; it wants a
      per-player device as well, which is a change to `ControlMap` rather
      than to this.
- [ ] Walk, jump, fall, land, face left and right, animate. **All six are in
      and proved against VICE.** What is left is the trigger, which is now
      found but not translated - see below. The arc runs correctly the moment
      something sets the rise counter.
  - [x] `SolidMap`, the level's own collision map. `$8500` keeps it as forty
        bytes a row, thirty-two of level and eight of something else, and every
        collision test in the game is an indexed read off a pointer into it.
        The port keeps the bit grid instead: `init_level_renderer` builds that
        map out of the very level bitmap the renderer draws, so addressing it
        the C64's way would be the same truth written twice. `Playfield` now
        reads its tiles out of this rather than building its own copy.

        Off the map reads solid, which is the map's arrangement rather than a
        guard: `$8488`, `$84B0` and `$84D8` are three forty-byte rows of `$80`
        above it, and a thing near the top of the playfield probes them.
  - [x] `EntityTable`, the arrays a player shares with everything else that
        moves. Eight slots, because `$1CBD` starts its loop at seven and counts
        down, and slots 0 and 1 are the two players - which is why a routine
        holding a player number indexes these with it. Three fields so far:
        `$8520` the sprite frame, `$8610` the animation timer and `$8818` the
        bubble timer, each added because a routine translated below reads it.

        These are not a table in the 6502 at all. They are the eight-byte tails
        of the same forty-byte rows whose other thirty-two bytes are the level
        tiles, which is why every one of them is forty apart. `SolidMap` is the
        other half of that region.
  - [x] `PlayerMovement` - `$220C` and the two movers, `$226B` and `$22C3`,
        with the tail at `L22BB` they share. The walk, which way a player
        faces, and the walk cycle.

        The shape is worth stating because it is not one routine per
        direction. Each mover writes its own delta into `$04`, picks its own
        probe offset, and decides from the current frame whether the player
        already faces that way; then both fall into one piece of code that
        checks a wall and adds the delta. So a player facing the wrong way
        turns on the first push and walks on the next, and the turn is refused
        outright while `$8818` says they are in a bubble.

        Two arms of the dispatch are left for later. Up reaches `$222B` with a
        `jmp` rather than a `jsr`, so it is translated only as far as
        suppressing the walk - which is real behaviour, not a shortcut: a
        player pushing up does not also walk. Fire leaves for `$22E8`, which is
        Phase 5. And `$22B4` reads `$63,x` and calls `$7C21` when it is set;
        that routine is Phase 6's and the byte is clear for a walking player,
        so the call is not translated. All three are gaps, named here so they
        are not mistaken for finished work.
  - [x] `PlayerJump` - `$23EA`, the rise, the fall and the landing, with the
        two counters they run on added to `EntityTable`: `$87A0` counting the
        rise down and `$87C8` counting the fall up.

        One table gives a jump its shape. `$ACCD` holds sixteen deltas and both
        halves of the arc read it, the rise with its counter running down and
        the fall with its counter running up, which is what makes a jump slow
        as it tops out and gather speed coming back. Each reads the counter
        *before* it moves it, because the reference does `dec` or `inc` and
        then `tay`, and `tay` carries what the accumulator was already holding
        rather than what was just written to memory. Getting that one step
        wrong would put the whole arc a frame out.

        Landing is two stores in one frame: `$243A` puts the player where the
        arc says, then `$247E` snaps them to the row grid. Both were caught.

        Three gaps, named. `$23FC` and `$243A` leave for `$2483`, a
        flag-driven horizontal mover that is not the walk and is not
        translated. `$2480` and the end of the fall leave for `$2519`, which
        is `PlayerLanding`. And nothing here decides to jump - see above.
  - [x] The jump trigger, `$222B`, as `PlayerMovement.Launch` - the up arm of
        the dispatch, which until now only suppressed the walk. It sets the
        rise counter to `$0F` and latches the two drift flags, and the sprite
        gains bit 1 unless the frame is already `$08` or above. `$2268`
        increments `$B1`, one byte rather than one per player, which nothing
        translated reads; not translated.
  - [x] `PlayerDrift`, `$2483`, the flag-driven mover both halves of the arc
        jump to. One pixel a frame rather than the walk's two, and an animation
        period of two rather than four.

        Its wall test is not the shape anyone would write from scratch and is
        translated as the reference has it: the player moves when the cell
        ahead is open, **and also** when both it and the one past it are solid.
        Only a solid cell with an open one behind it stops them.

        **This does not finish airborne movement.** A second mover at `$263D`
        also moves X, off the live joystick byte, and is not translated - see
        the correction below. `PlayerJump` calls the drift where the reference
        jumps to `$2483` and `$248D`, so the wiring is right; the missing piece
        is a routine, not a connection.
  - [x] `PlayerSteer`, `$25F1` - the stick read again while a player is off the
        ground, and what makes a jump steerable. Reached only from the drift's
        animation tail, so it runs on every second airborne frame.

        Its two halves are the walk's movers rewritten rather than reused: same
        turn-before-you-move, same probe offsets, but a pixel a step instead of
        two, an edge test that only catches an exact arrival on `$24` or `$F4`,
        and a different set of frames each half will move on. One asymmetry is
        easy to miss and is translated as written - the height shortcut past the
        wall check is only on the split-probe path, so a player sitting on a row
        boundary gets the wall check wherever they are.
  - [x] `PlayerLanding`, `$2519` - the end of an arc, however it ends. Both
        exits reach it: the landing at `$2480` and the fall running out of
        table at `$2423`. It puts both arc counters back to `$FF`, squares X up
        to an even column, and checks the ground one last time.

        The X alignment is the part nothing else hints at, and it is why a
        landing tidies the player on both axes - `$247E` puts Y on the row grid
        and this puts X on a column. Which way it rounds follows which way they
        face, so they are squared up in the direction they were travelling.

        `$2530` rounds down and then falls through into the rounding up when
        what it stored was zero. Zero rounded up is zero again, so the
        fall-through changes nothing and is not reproduced; the comment in the
        class says so. The level-91 exception is translated with its off-by-one
        made explicit: the reference compares `SUBFLG`, which counts from zero,
        so `$5B` is the ninety-second level and not the ninety-first its
        comment claims. `$2578`'s jump to `$EB3F`, which sets up the restarted
        fall, is not translated.
  - [x] `PlayerDescent`, `$EB48` - falling at a flat two pixels a frame until
        something is underneath, which is the plain descent rather than the
        jump's arc. It wraps exactly `$F5` back to `$15`, looks for ground only
        on a row boundary, and on finding it does `dec $87F0,x`, which is what
        the rest of the port reads as standing on something.

        **The self-modifying code is not reproduced.** `$EB34`, `$EB3F` and
        `$EBB8` write different continuations into `$EB94` and `$EBB5` and then
        fall into this one body, so what they choose between is where to go
        afterwards. Phase 4 arrives through `$EB3F`, whose continuation is
        `$EB0F` by both exits. `$EB0F` is the generic entity animation and
        wants `$8778` and `$8750`, which nothing here reads yet, so the class
        stops where the continuation begins and Phase 6 picks it up.

        **This one has no golden trace**, and it is the only piece of Phase 4
        that does not. Every trap on `$EB3F` in VICE was an entity slot; the
        route a player takes to it, from `$2575`, is read rather than watched.
        Its tests are worked from the reference, which is the weaker evidence
        this port has. See the note below on what it would take to settle.
  - [x] `PlayerCell`, which is `$E9B8` and nothing else: a position byte pair
        to a row, a column and the two fine offsets `$23` and `$24`. The 6502
        leaves the answer as a pointer in `$11`/`$12` so that every later test
        is one indexed read - `$51`, `$28`, `$79` - and `PlayerCell.Solid`
        takes one of those offsets and does the index register's arithmetic, so
        the probe constants stay the ones the reference is written in.

        Two biases come out of the pointer rather than the position: the row
        table at `$AC03` starts three forty-byte rows below the map, and the
        column has a `dec` against it. Both are folded in, so a probe is the
        offset the reference names and nothing else.
- [ ] `PlayerModel` and its view.

**Verified so far:** the solution builds with 0 warnings and
`BubbleBobbleSharpLib.Tests` is 168/168. The table's width, its zeroed start
and the independence of the two players' bytes are proved against the table
itself. The port byte is proved a bit at a time - each direction clearing only
its own bit, from either the keys or the pad, an idle port reading `$FF`, a
held key staying held across two reads, and one player's stick never reaching
the other's port. The map is proved against every one of the hundred levels -
its bitmap on the rows below the ceiling, the leftmost two columns walled
whatever the level drew, the ceiling and floor openings exactly four cells
wide, and nothing off the map reading open. `$E9B8` is proved case by case,
worked by hand off the reference: the leftmost landed position, the rightmost
respawn one, the fine offsets, the byte wrap above the playfield, and each
probe offset reaching the one cell it names and no other.

The walk is proved against the game itself, which is what the verify step below
asks for. VICE ran the PRG `make verify` calls byte-identical to the original,
a one-player game on level 1, a store checkpoint on `$BA` and D held; every time
the game wrote the player's X, the checkpoint wrote down `$BA` and `$8520`.
Replaying the same start through the port reproduces both sequences exactly -
twenty-four stores, X climbing `$2E` to `$5C` two at a time, and the walk cycle
turning over on every fourth, which makes the first run of a frame three long
and the rest four. The branches either side of that are proved on their own as
well: the turn before the walk, a wall stopping the step, a wall on the far side
not stopping it, the two ways past the wall check, up suppressing the walk, and
a player in a bubble refusing to turn. And the second half of the verify step,
over all hundred levels and both directions: a player never walks out of a cell
into a solid one.

The jump is proved the same way, and the capture is worth keeping: with the
program counter and both counters read beside every `$C2`, one arc separates
into fifteen stores at `$23FE`, four at `$243C` and one at `$2480`, with
`$87A0` running `$0F` down to `$00` and `$87C8` running `$01` up to `$04`.
Replaying that start through the port reproduces the whole of it - `$D9` down
to `$B2`, the turn, `$B2 $B3 $B4`, and the landing putting the player on `$B5`,
which is `$B6` snapped to the grid. The counters are asserted step by step as
well, so an arc that came out right by luck rather than by indexing would
still fail.

The landing frame is proved on its own, from the one frame of a capture that
lands: a player mid-fall at X `$53`, Y `$B4` with the fall counter on `$03`
comes out at Y `$B5`, X `$54`, both counters back to `$FF` and `$87F0` still
negative, which is the game's own reading of that frame byte for byte.

Translating `$EB3F` turned up a fidelity error in `$2519` that the tests had not
caught. `PlayerLanding` was working its own cell out, after squaring X up.
Nothing between `$2450`'s call to `$E9B8` and the probes at `$2541` calls it
again, so those probes read the player a pixel earlier, and across a squaring-up
that can be a different column. The cell is now passed in - from `$2450` on the
landing path, and from the top of the frame on the one where a fall runs out of
table. It changed no test, which is the point: the outcome happened to agree on
the frames that were captured, and the code was still wrong.

One expectation was superseded on the way. The counters test used to assert a
fall counter of four on the landing frame, which is what the capture reads at
`$2480` - one instruction before `$2519` resets it. With `$2519` translated the
frame ends with the reset instead, and the test now asserts that and says why.

A steered jump - W and D held together - is proved the same way, and it is the
case that caught the drift being only half the story. Seventeen frames of X and
Y both match: X climbing two pixels then one, over and over, while the arc runs
underneath it. A translation with only the drift in it gives a steady pixel a
frame and fails immediately.

The capture samples at the arc's own store, before the frame's horizontal work,
so the X it reads belongs to the previous frame; the test pairs them back up
rather than pretending otherwise. It also stops one frame short of the landing,
because at the time the single pixel X moved across that frame had nothing
translated to account for it. It does now: `$2519` squares X up to an even
column, and a test of that frame on its own is in the jump's golden set.

**Watch out:** `rebb64/docs/TECHNICAL.md` and `rebb64/src/` disagree about the
entity arrays. The doc's table calls `$8840` the horizontal velocity and
`$87A0` a platform climbing counter, but `player-movement.s` at `$26BF` adds
`$87A0,x` to X and uses `$8840,x` as an index into an animation table, and
`$8868,x` is the byte it adds to Y. Section 1 settles which wins - the golden
source is `src/`, and the doc is a summary of it. Name from the code when the
next item reaches these.

**Watch out:** which routine walks a player is not settled, and the movement
item stops there. `D_1E6C` dispatches on the player's state byte `$B2`, and a
live player is state 1, whose handler is `D_2162` in `entity-interaction.s`.
State 1 is now observed rather than inferred, and in play rather than at the
select screen: on level 1 with a one-player game running, `$B2` reads `$01`,
`$BA` `$2C` and `$C2` `$DD` - and `$DD` is exactly what `check_player_state`
writes to `ZP_C2,x` when it respawns a player, which is a second source
agreeing.

The player also demonstrably walks: holding D takes `$BA` from `$2C` up to
`$F4`, which is exactly the right-hand clamp `$2836` writes. So the verify step
below is reachable - the input, the hold and the byte to compare all work.

**The routine is found.** A store checkpoint on `$BA`, with D held, traps 300
times at `$22C2` - against three hits on the respawn placement in
`check_player_state` and one on its game-over path. `$22C2` is `L22C2` in
`entity-interaction.s`, so the store is the `sta FA,x` at `$22C0`, the last
instruction of the tail beginning at `L22BB`:

```
L22BB:  lda FA,x ; clc ; adc ZP_04 ; sta FA,x
L22C2:  rts
```

That tail is **shared**. `D_226B` sets `ZP_04` to `$FE` and `D_22C3` sets it to
`$02`, and both fall into it, so one store moves a player two pixels either
way. The input dispatch at `D_220C` picks between them by shifting the port
byte: bit 2 for `D_226B`, bit 3 for `D_22C3`, which makes `D_226B` left and
`D_22C3` right.

So the path is the one the static trace found first and then talked itself out
of: `D_2162` to `D_220C` to `D_226B`/`D_22C3`. It was dismissed as bubble code
on the strength of the comments, which call these "move left in bubble" and
"move right in bubble". They are the player's ordinary walking movers. That is
the third comment in this file to point the wrong way, after `D_ED12`/`D_ED18`
and the `$8480` arrays, and it is the clearest argument yet for section 1's
rule that the code is the golden source and the prose around it is not.

This also settles the four candidates the other way round, on positive evidence
rather than silence: `$2146` and `$2107` in `D_20D7`, and `D_ED12`/`D_ED18`,
are not the player's walk. They belong to the entity movers.

**The vertical half is found**, by the same method and with the same result:
the routines are where the reference's comments say something else entirely.
A store checkpoint on `$C2` separates cleanly into three, none of which fires
while the player stands still.

| Trapped at | Store at | What it is |
|---|---|---|
| `$23FE` | `$23FC`, `L23FC` | the rise |
| `$243C` | `$243A`, `L243A` | the fall |
| `$2480` | `$247E`, `L2474` | the landing |

All three are inside the routine at `$23EA` that the disassembly heads "bubble
ascent (captured enemy rising)". It is the player's jump. That is the fourth
comment in `entity-interaction.s` to point the wrong way, after the two walk
movers.

The rise subtracts `D_ACCD` indexed by `$87A0` counting down, the fall adds the
same table indexed by `$87C8` counting up, and the landing snaps `$C2` to the
eight-pixel grid with the `sbc #$2D / and #$F8 / adc #$2D` that the other
landing checks use. `D_ACCD` is `$00 $01 $01 $02 $02 $02 $03 $03 $03 $03 $03
$04 $04 $04 $04 $04`, and the disassembly calls it the "fall speed table",
which is half right: both halves of the arc read it.

The captured arc proves the indexing without any reading at all. Sixteen steps
of rise came back as four four four four four, three three three three three,
two two two, one one, zero - which is `D_ACCD` walked from its end to its
start, value for value.

Two loose ends. One trap landed at `$E496`, which is `wait_one_frame` and has
no business storing `$C2`; it happened once in forty and is unexplained, so
watch for it rather than assume it was noise. And `$2480` continues to `$2519`,
which has not been looked at.

**The jump trigger is `D_222B`**, and for once the static reading had it right
- but it does more than set a counter, which the reading had not noticed.

A store checkpoint on `$87A0` is unambiguous. Nothing writes it while a player
stands still except `$04CE`, the respawn clear putting `$FF` there. With up
held, `$2230` fires three times in a run and writes `$0F` every time, which is
the `sta D_87A0,x` at `$222D`. Three traps, three jumps. It is reached from
`$220C`'s up arm by `jmp`, which is exactly the arm `PlayerMovement.Step`
currently returns on.

What else `$222B` does matters for the translation:

- It latches the horizontal direction into `$8840` and `$8868`, one flag each.
  Left is latched when the port's bit 2 is clear *and* the frame is `$04` to
  `$07`; right when bit 3 is clear *and* the frame is below `$04`. So the
  *drift* is decided once, at take-off, from the stick **and** from which way
  the player was already facing.
- It sets bit 1 of the sprite, unless the frame is already `$08` or above.
- It increments `$B1`. The reference calls that the bubble count; given how
  much else in this file is misnamed, treat that as unverified.

On the ground the stick drives `$226B` and `$22C3` directly. In the air `$222B`
turns the stick into two flags and `$2483` moves the player from those.

**Correction.** An earlier draft of this section said that settled it - that in
the air the stick is not read again. That is wrong, and a capture of a jump with
right held shows it plainly. X advances from two places on alternate frames:
`$24FA`, which is `$2483`'s `inc FA,x` at `$24F8`, and `$267B`, which is the
`inc FA,x` at `$2679`. So airborne movement is the drift **plus** a stick-driven
mover, which is why holding right through a jump covers ground faster than
either alone would - two pixels then one, over and over.

The second mover is not independent, which took a second look to see. Its entry
is `$25F1`, and the only thing that reaches `$25F1` is the `jmp` at `$2515`, at
the end of the drift's own animation tail. So a player steers on exactly the
frames the drift animates - every second one, and only while the sprite is below
`$08`. The routine `$263D` is the right-hand half of it and `$25FE` the left.

Two other things fell out of the same capture. `$251E` writes `$FF` to `$87A0`,
so `$2519`, the routine a landing leaves for, is at least in part the reset that
puts the counter back to idle. And `$23EF` is the rise's own `dec` at `$23EC`,
which confirms the rise addresses a second time from a different direction.

**`$2519` is found, and it is the end of the arc rather than anything to do
with bubbles.** The reference heads it "bubble reached top - check for pop".
Both ways out of a jump lead here: the landing at `$2480`, and the fall running
out at `$2423`. One landing, caught whole in a single trap, says what it does:

```
  PC      X   Y  frame $87A0 $87C8 $87F0
  $2541  54  B5   00    FF    FF    FF
```

Three things at once. Both arc counters go back to `$FF`, which is the idle the
caller at `$21BD` tests for. `$87F0` stays `$FF`, meaning the branch that
restarts a fall did not fire, because there was a floor. And **X is aligned to
an even column** - `$53` became `$54` - which is the pixel that went missing
across the landing frame in the steered-jump capture and could not be accounted
for at the time. A landing squares the player up on both axes: `$247E` snaps Y
to the row grid and this snaps X to an even column.

Which way it rounds depends on which way the player faces. Frames below `$04`
and from `$0A` up round up, at `$2538`; frames `$04` to `$09` round down, at
`$2530`. That is right-facing up and left-facing down - aligned in the direction
of travel. The trap came in at `$2541`, the round-up path, from a frame of `$00`,
which is exactly what that rule predicts.

After the alignment it probes the same two rows the landing check uses. If
nothing is underneath, it increments `$87F0` and leaves for `$EB3F` to start the
player falling again; otherwise it returns. There is a level 91 special case on
`$C2` equal to `$DD` which is left as read.

**`$EB3F` is found: a plain descent at two pixels a frame, until something is
underneath.** It is not a routine on its own. It is one of three entry points -
`$EB34`, `$EB3F` and `$EBB8` - that each write a different continuation into
self-modifying code at `$EB94` and `$EBB5`, then fall into one shared body at
`$EB48`. The body adds two to `$C2`, wraps `$F5` round to `$15`, and on an
eight-pixel boundary probes two rows; finding the lower one solid it does
`dec $87F0,x`, which takes that byte back to `$FF` - on the ground. Then it
takes whichever continuation the entry point set.

Watched live it is unmistakable. Thirty traps on `$EB3F`, every one of them for
slot `$02` or `$03`, with `$C2` for those slots climbing `$15 $17 $19 $1B` and
on by twos, from the top of the screen downwards. That is the constant-speed
descent the source describes, and the wrap is why they start at `$15`.

**Two cautions for whoever translates it.**

The self-modifying code must not be reproduced literally. What `$EB34`, `$EB3F`
and `$EBB8` actually choose between is a continuation - the generic animation
step at `$EB0F`, `$EE4A` reached past a `BIT`, and a bare `rts` - so in this
port that is an argument, not a rewritten instruction.

And **the player was never seen reaching it.** Every trap was an entity. The
path Phase 4 cares about, `$2575` incrementing `$87F0` and jumping here when a
landing finds no floor, is read from the source and not yet watched happening,
because the player landed on a floor every time. Driving a player off a ledge
with scripted input is the missing piece of that check. Until it is done, treat
the player's use of this routine as inferred rather than proved.

**Trying to catch a player on `$EB3F`, and failing.** Three runs, none of which
produced it. Worth writing down, because the failures say more than another
green test would.

**A correction first.** An earlier note here said that driving a player off a
ledge would settle it. That is the wrong manoeuvre. Walking off an edge is the
walk's own ground check at `$1F03`, which increments `$87F0` at `$1F17` and
leaves for `$EB34` - a different entry point into the same shared body, with a
different continuation. `$2575` is reached only from `$2519`, so what is
actually needed is **a jump arc that expires with no floor under it**: either a
landing whose probes come up empty, or the fall counter reaching `$10` in
mid-air.

What the runs showed:

1. Holding jump and right, the player climbs onto the platform where level 1
   keeps its enemies, is killed on contact, and spends eleven seconds dying.
2. `$DC00` is not a reliable witness for whether a key is held. The game writes
   `$7F` to that port itself, so a sample can catch either the write or the
   stick depending on where in the frame it lands. Judge input by whether the
   player moves.
3. With the six entity slots zeroed through the monitor so the player survives -
   an intervention, and disclosed as one - twenty-four seconds of jumping got
   the player to the right-hand clamp at `$F4`, bouncing between Y `$3A` and
   `$65`, with `$87F0` never once leaving `$FF`. Every arc ended on a floor.

So on level 1 the case may simply not arise: its platforms are wide and its
drops are shorter than the arc. Settling it wants a level with a real gap under
a jump, which means either reaching one in play or a targeted setup - and a
targeted setup is close enough to constructing the answer that it would be
worth little. Left open, deliberately.

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

        Supplied 2026-09-17, ahead of the rest of this bullet:
        `Assets/Images/banner.png` in the 8-bit rendition. It is 320x56 -
        the screen's full width, and exactly the 56 lines this bullet
        asks for - as a 4-bit indexed PNG of three colours.

        Three things follow from it, none of them blocking:

        - **The format is fine.** `ImageReader` takes the format from the
          file's magic bytes rather than its extension and tries PNG
          first, so a `.png` among the exporter's `.tga` files loads
          like any other image.
        - **It is not in `AssetManifest.json` yet**, so nothing loads it
          and nothing checks it. That is the right state until there is a
          view to draw it.
        - **It conforms to the palette.** Andy's decision was that the
          banner conforms rather than being exempt, and as revised on
          2026-09-17 it does: three colours, all named — `#000000`
          (entry 0) over 72.68% of it, `#75CEC8` (entry 3) over 26.31%,
          and `#FFFFFF` (entry 1) over the remaining 1.02%. So it will
          pass `IsWithinPalette` when the manifest names it, and the
          budget check needs no exemption.

          Two earlier drafts did not conform. The first was `#181818`,
          `#B5FFFF` and white, of which only white was a C64 colour; the
          second fixed the black and the cyan but left 40 pixels of
          `#B5FFFF` as a highlight. Those 40 are now cyan, which is why
          entry 3's share is 26.31% rather than 26.08%.

        It is also the one asset in that folder the exporter does not
        produce. `export-csharp-assets.py` neither regenerates nor
        clobbers it, which is what we want - but it does mean it is not
        reproducible from a `rebb64` checkout the way everything else
        there is.

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

- Say what the item is before starting it. A few sentences to Andy: what
  the item covers, which `.s` routines it translates, and what will exist
  when it is done. A box's own wording is a reminder to whoever wrote it,
  not a briefing, so it is worth restating in plain terms before any code
  is written.
- One phase at a time. Commit at the end of a phase, not during.
- Do not commit automatically. Andy commits.
- UK spelling everywhere, including in code comments.
- `dotnet test` is blocked in the sandbox. Run the test executables
  directly from `bin/`.
- If a `.s` routine does not make sense, say so and stop. A guess that looks
  right is worse than a question.
