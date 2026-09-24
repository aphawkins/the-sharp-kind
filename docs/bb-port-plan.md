# Bubble Bobble Port Plan

How Bubble Bobble becomes the third game in The Sharp Kind, beside Elite and
Stunt Car Racer.

The port is a translation of
[zaidka/rebb64](https://github.com/zaidka/rebb64), a complete, documented,
byte-exact 6502 disassembly of the Commodore 64 conversion. That checkout is
referred to below as **`rebb64/`**, and a path like `rebb64/src/enemy-ai.s`
means a file in it, not in this repo. On this machine it is checked out at
`C:\code\github\zaidka\rebb64` - the `vice-drive` skill already names this
path for building and running it; this is so a plain reading session finds it
too, without a build step.

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
- [x] Walk, jump, fall, land, face left and right, animate. All six are in and
      proved against VICE, the trigger with them, and the driver below now
      calls them - so a player on screen walks, jumps and lands.
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
- [x] `PlayerModel` and its view, which is `$1805` and the sheet it points at.

      `$1805` is the whole of the drawing side and it is short. For each of the
      eight slots it writes `$BA` to the sprite's X register and `$C2` to its
      Y, `$8548` to the sprite's colour register, and `$8520` masked to five
      bits and added to `$8598` to the sprite pointer. There is no other
      arithmetic between a player's bytes and the screen, which is why the
      model carries the position bytes as they stand and the rendition applies
      the offset.

      `$8598` is `$60` for both players, and `$60` is exactly where the game's
      own sprite data starts - `__SPRITE_PTR_BASE__` is `($5800-$4000)/64`. So
      the pointer a player is drawn with is the masked frame and nothing else,
      and the sheet index is the frame. `sprites-game.tga` is those 97 sprites
      in one row, 12 pixels each in the sheet and 24 on screen, because a
      multicolour sprite carries its 24 as twelve entry numbers.

      A sprite resolves those entries differently from a character, which is
      why `MulticolourSprites` is not `MulticolourSheet` with other arguments:
      01 and 11 are `$D025` and `$D026`, one pair for every sprite on the
      screen, and only 10 is the sprite's own colour. `$44E9` sets that pair to
      1 and 2 and nothing writes it again, so what varies between one sprite
      and the next is a single entry - and a repainted copy is held per colour
      rather than per sprite.

      The offset from a position byte to a pixel is the VIC-II's own, since
      `$1805` applies none: on a 40-by-25 display `$18` and `$32` are the
      coordinates that put a sprite's top-left pixel at the screen's. **That is
      not the offset a probe uses, and the two are not meant to agree.**
      `PlayerCell` works from `$14` and `$15`, which land four pixels in and
      five down from the sprite's own corner - the game probing a point inside
      the character rather than the corner of the box it is drawn in, which is
      why the probes that reach the ground are written one and two rows below
      it.

      Two gaps, named. `$182F` stores the masked frame back to `$8520` before
      it adds the base; nothing translated leaves a frame above `$1F` there for
      it to trim, and a model does not write to the game's state, so the store
      is not reproduced. And `$1822` can override a player's colour from
      `$8570` while they flash - but for the two players `$8548` and `$8570`
      hold the same pair, 5 and 3, so the override changes nothing and the
      flash goes with invincibility.

      `StartPlayers` places them where `$04BB` and `$05C5` do - `$C2` is `$DD`,
      `$BA` comes from `$A735`, the frame from `$A737`, the colour from
      `$05C5`, and the counters go back to `$FF`. One player, since there is no
      front end to join a second with. What moves them from there is
      `PlayerFrame`, the item below.

- [x] `PlayerFall`, `$257C` - a player falling with no jump behind them, which
      is what walking off a ledge leaves. It is a third falling routine, and
      the near-twin of `$EB48` rather than a copy of it: same two pixels a
      frame, same `$F5` to `$15` wrap, same pair of probe rows (`$51` clear,
      `$79` solid). Three things differ, and each has a test of its own -
      the height below which the ground is not looked for is `$26` rather than
      `$1F`, a landing squares X up to an even column where `$EB48` leaves X
      alone, and the tail runs an animation counter that every second frame
      falls straight into `$25F1`.

      That last one corrects the note on `PlayerSteer`. `$2515` is *not* the
      only way into the steer: `$257C`'s `L_25E2` falls into `$25F1` as well,
      so a player who walks off a ledge is steerable on the same every-second-
      frame cadence a jump is.

      The frame sets the X snap uses are not `$2519`'s, which is the reason
      this is written out rather than shared with `PlayerLanding`. There `$04`
      to `$09` round down and the rest round up. Here `$08` and `$09` round up
      with the low frames and `$0A` upwards rounds down with the middle ones.
      A translation that reused the landing's rule passes every other case and
      fails those four.
- [x] `PlayerFrame`, which is `$1CBD`, `$1E6C` and `$2162` - the thing that
      calls everything above, and the answer to the "nothing drives the
      players yet" note this section carried until now.

      It is short, because all it is is four questions asked in order. Is the
      rise counter out of its `$FF` idle - then the arc owns the frame
      (`$21BD`). Is the ground byte out of *its* idle - then the fall does
      (`$21C5`). On a row boundary with nothing underneath - then the ground
      byte comes out of `$FF` and the fall runs on that same frame (`$21CD`).
      Otherwise the stick: anything pushed reaches `$220C`, and nothing pushed
      leaves a standing player breathing on a period of seven (`$21EB`).

      Three pieces of `$2162` were deliberately absent, each named in the class
      rather than skipped silently: `$2165`'s call to `$2301`, which a player
      out of a bubble never reaches; the scan over the eighteen entity slots
      at `$2171` that catches an enemy while up is held, which with no enemies
      in play finds nothing and falls through to exactly where the arm that
      skips it goes; and what happens to a captured player. **Phase 5 has since
      wired `$2165`**, so two remain. Only state 1 is
      dispatched - the jump table at `$1E3A` has an entry per state and the
      rest are dying, captured, freed and the level-99 special. Only the two
      player slots are driven, though `$1CBD`'s loop covers eight.

      **A gap in `StartPlayers` fell out of writing this.** `$05F5` sets
      `$8818` to `$FF` for all eight slots as a level starts and the port was
      not doing it, so the byte sat at zero - which every mover reads as "in a
      bubble" and refuses to turn a player on. Nothing had noticed, because
      until now nothing called a mover.

**Verified so far:** the solution builds with 0 warnings and
`BubbleBobbleSharpLib.Tests` is 223/223. The table's width, its zeroed start
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

The drawing side is proved on the recording surface, the way the other three
views are: one rectangle per active player, 24 by 21 from a 12 by 21 source,
the sprite taken from its own column of the sheet, an empty slot drawing
nothing, player one drawn over player two, and the sheet painted with the
sprite's own colour in entry 10 rather than entry 11. The mask is proved a byte
at a time, `$20` and `$FF` included, since a frame that kept its sixteens would
name no sprite at all.

One test ties the drawing offsets to the game's own numbers rather than to the
VIC-II's documentation, and it is the only one that could have caught them
being four and five pixels out: a player placed where `$04BB` starts a life -
`$2C`, `$DD` - comes out with the sprite's bottom edge on pixel 192, which is
the top of row 24, the floor the level is drawn inside. Standing on the floor
is what starting a life looks like, and `$DD` and both origins have to agree
for it to land there.

The driver has thirteen cases of its own, and they prove the routing rather
than the routines: an empty slot and a slot in an untranslated state both left
alone, the walk on a floor, the fall started on the frame the floor goes, the
ground check skipped off a row boundary, the arc winning over both the ground
check and the stick, up and then the arc on the frame after, the idle animation
on its period of seven and a bubbled player getting none of it, two players
driven from their own sticks, and a standing player standing still - which
reads as a test of nothing and is the one that fails if the ground check is
inverted. `$257C` has its own eighteen, of which eight are the X snap alone.

And the whole chain is proved in the app rather than in a harness, which is
the first time anything in this port has been. `sdl-drive` held right for forty
ticks and then tapped up, dumping frames as it went: the player walks right
along the floor, the jump lifts them, and they land on the platform above and
stay on it. Every one of the eight routines ran to produce that, and a break
anywhere in the chain would have left them standing where `$04BB` put them.

The real sheet is checked as well, beside the asset set's own tests: 97 sprites
of 12 by 21 in one row. A sheet that lost one, or that was packed into a grid
rather than copied across as rebb64 holds it, would put every player on the
wrong artwork without failing to load.

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

**The file list this section carried was wrong on all three counts,** and it
cost a reading to find out. `entity-bubble-handler.s` does not exist in the
checkout. `bubble-handler.s` is not the bubble - it is what happens when one
pops: sound, item collection and the power-up flags. And `update_bubbles` in
`bubbles-sprites.s` is the blowing animation drawn in *characters* behind the
player, which is a screen effect rather than a rule.

Blowing a bubble is in `entity-interaction.s`, beside the whole of Phase 4.

**What this phase covers, after the move below:** blowing a bubble, and the
mover the whole entity table shares. Where a bubble then goes is Phase 6's,
because the code that takes it there is the enemy AI.

- [x] **Fire.** `$22E8` and `$2301`, as `Bubbles/BubbleBlow.cs`, with the
      eighteen-slot table it fills as `Bubbles/ObjectTable.cs`.

      Pressing fire does not make a bubble, which is the thing worth knowing
      about this routine. `$22E8` only starts a clock - `$06` into the bubble
      timer, and the facing latched into `$ACCB` - and `$2301`, called from
      `$2165` at the top of every later frame, runs that clock down through
      `$ACC4`. The bubble is made on the one frame the timer reads `$04`, three
      frames after the button, and the seventh frame puts the player's sprite
      back. A player who dies in between never makes one.

      `ObjectTable` is the *second* of the game's two tables of moving things,
      and the distinction is load-bearing. `EntityTable` is the eight slots at
      `$85xx` to `$88xx` - the players and the enemies that walk like them.
      This is the eighteen slots `$CA` indexes: bubbles, items, and enemies once
      they are inside one. `$2321` searches it from seventeen downwards and
      takes the first slot whose type byte is negative; `$0620` sets all
      eighteen to `$FF` as a level starts. It is named for the bubble because
      the bubble is the only thing that fills it so far, and Phase 6 will want
      a wider name.

      **`$2165` is now wired,** which closes the first of the three gaps
      `PlayerFrame` was carrying. Two remain.

      **The reload is `$A824`, and `game-loop.s` calls it an invincibility
      timer.** It is the sixth comment in the reference to point the wrong way.
      `$2385` sets it to eight as the bubble leaves - measured from the bubble
      rather than from the button, so a player holding fire is on a fourteen
      frame cycle - and `$0A28` counts it down one a frame. Nothing else reads
      those two bytes, so the decrement is translated in `BubbleBlow.Tick`
      rather than waiting for a game loop.

      **Three arms of `$2301` are deliberately absent,** each gated by a byte
      that a level start leaves in the state that skips it, and each named in
      the class: `$23B8`'s special bubble on `$A783` (Phase 7), `$23D7`'s
      sound on `$65` (Phase 8), and `$23DE`'s flag rewrite on `$37C7`
      (Phase 7).

      **`$1EEE` is settled: it is enemy code, and it cannot reach `$2301`.**

      The worry was that `$1EEE` writes `$64` to `$8818`, and `$2301` indexes
      `$ACC4` - seven entries - with that same byte. Four things settle it, and
      the last two are the ones that make it more than a plausible story.

      * **Different states.** `$1EEE` sits inside `$1E9F`, which the jump table
        at `$1E3A` reaches for state 9. `$2301` is reached only from `$2162`,
        which is state 1's handler. One slot cannot be both.
      * **The cooldown pair belongs to the enemies.** `$1E9F` reads `$8890` and
        `$8818`, and the routine that sets them up is `D_1A6F` - in which
        *every* store is the base-plus-two form: `$b4` for `$b2`, `$881A` for
        `$8818`, `$8892` for `$8890`, and five more. It addresses slots 2 and
        up and never touches the two player slots.
      * **`$1EB5` guards the write anyway.** State 9 refuses to run at all
        unless `$8818` is already negative, so it can never land on a blow that
        is part way through.
      * **Watched, with a control.** Sixty seconds of level 1 - firing,
        walking and jumping - with an exec checkpoint on `$2301` conditioned on
        `Y > 6`: never fired. `$1EEE` itself, with no condition: never
        executed. The control, the same checkpoint conditioned on `Y < 7`,
        fired at once and read `$8818` as `$06`, so the address, the condition
        and the play script all work. A checkpoint that never fires proves
        nothing without one that does - this file has paid for that lesson
        already.

      What remains is not a risk in the port today: nothing here drives a slot
      past 1, or a state other than 1, so `$1EEE` has no translation and no
      caller. If a later phase ever puts a player slot into state 9, the port
      throws rather than reading past the array, so it fails loudly.

- [x] **The shared mover**, `$0E23`, as `Bubbles/EntityMover.cs`.

      It is here rather than in Phase 6 only because the bubble is the first
      thing to need it. Two pixels in one of four directions plus the cell
      bookkeeping, and the whole entity table shares it.

      **Found with VICE, not by reading.** A store watchpoint on slot 17's Y
      traps twice a frame while a bubble rises, at `$0E30` and `$0E33`, which
      are the two `dec D_AA1E,x` of the up arm. That is how a mover buried in
      the middle of the enemy AI was found at all.

      Three things in it would be got wrong by a careful reading:

      * **Right is two pixels, not one.** `$0E52`'s `cmp #$01` falls through
        with the carry set, so `$0E56`'s `adc #$01` adds two. That is what
        makes it the mirror of the left arm's `sbc #$02`.
      * **Down steps its row one sub-position later than up steps its.** Up
        steps the row when the sub-position reaches zero; down steps it when
        the sub-position reaches two, one step after the wrap at eight rather
        than on it. A mover written as a mirror puts every falling thing a row
        out for one step in eight.
      * `$0E6F`'s `bne` falls through into the down arm if a column wraps to
        zero. A column is 0 to 31, so it is unreachable, and it is not
        reproduced.

      **The eight-pixel shot is a different mover.** A bubble opens by
      travelling eight pixels a frame, and that is the dispatcher at `$0F98`,
      not this. This is the slow half - the rise, and the drift at the top.

**Travel, expiry, popping, the `bubbleCurrent` drift and the `wrapOpenings`
have moved to Phase 6,** and the reason is the third time this section's
premise has turned out wrong.

A bubble is not driven by any bubble routine. It is driven by
`enemy_ai_update` at `$0CF2`, the main loop over all eighteen slots, by way of
`$0F48`, `$0F61` and `$0F91`, and it needs `$105B` (bubble collision) and
`$7BFE` (platform check) with it. That is roughly 350 of `enemy-ai.s`'s 626
lines, and every enemy runs the same code. Translating it under a Phase 5
heading would be writing Phase 6 while pretending otherwise, so the items are
where the code is.

What that leaves in Phase 5 is what is above: blowing a bubble, and the mover
the whole table shares. Both are done.

**Verified so far:** the solution builds with 0 warnings and
`BubbleBobbleSharpLib.Tests` is 271/271, up from the 223 Phase 4 left. The blow
took it to 256 and the mover added the rest.

The blow has twenty cases of its own. The six frames are asserted frame by
frame in both facings, because the table is read at the timer's own value and
an index one out gives a sequence that is still plausible. The bubble is proved
to appear on the third frame and not the second, and once per blow however many
frames are left. Its position is worked by hand off the reference: a player at
`$44`/`$55` puts a bubble at column six, row eight, and facing right shifts it
one column and eight pixels on. Facing left does not, and that asymmetry has a
test of its own - the sprite's origin is already where the mouth is, and a
mirrored translation would smooth it away. Both halves of `$2399` are proved
apart: airborne and low in the cell blows into the row below, and airborne
alone or low alone does not. The two failure paths are proved to fail the way
the 6502 fails - a player above the top of the playfield leaves four bytes
written in a slot that is still free, and a full table costs the player nothing,
so they can try again next frame.

The routing has six cases. Fire on the ground reaches `$22E8` through `$220C`'s
last arm; fire and right together do both, because `$220C` takes all its arms in
one pass; up and fire together do *not*, because `$220F` is a jmp and the fire
arm is never reached. The steer's own arm at `$25F5` is a jsr and is proved
separately. And the chain end to end: one press, then nothing but the driver,
and a bubble in the table three frames later - the case that fails if `$2301` is
never called back.

One existing expectation was superseded, and the reason is worth keeping.
`DoesNotAnimateAPlayerInABubble` used to set `$8818` to `$10` by hand. No
routine in Phase 4 or 5 can put that value in that byte for a player in state 1
- `$22EE` writes `$06` and nothing else writes it at all - and now that `$2165`
reads the same byte, an invented value would be an invented frame of animation
with it. The test blows instead, and it now proves something sharper: the
breathing is suppressed for the whole blow and resumes on the very frame the
blow ends, because `$21F3` reads the byte after `$2301` has already taken it
back to `$FF`.

**Now proved against VICE, and it matches exactly.** A one-player game on
level 1, the player standing where `$04BB` puts them at `$2C`/`$DD` facing
right, fire tapped once, and the machine stepped a frame at a time with the
eighteen-slot arrays read at every step:

```
  n   $8818 $8520 $A824   slot 17
  29    06    00    00           <- the button. A clock starts, nothing else
  30    05    08    00
  31    04    08    00
  32    03    09    07    34,DD  <- the bubble
  33    02    09    07    3C,DD
  34    01    08    06    44,DD
  35    00    08    05    44,DD
  36    FF    00    05    4C,DD  <- the sprite goes back, the timer to its idle
```

Every claim in this item is in those eight rows. The button only starts a
clock. The sprite runs `$08 $08 $09 $09 $08 $08` and then back to `$00`, which
is `$ACC4` value for value and is the array `RunsTheSixFramesAndPutsTheSpriteBack`
already asserted. The bubble is made on the frame the timer reads `$04` and on
no other. It lands at `$34`, which is the player's own `$2C` plus the eight
pixels a right-facing blow adds - the asymmetry that has a test of its own. Its
Y is the player's `$DD` untouched, because a standing player gets no row bump.
And it goes in slot 17, which is where a downward search from seventeen lands.

The reload reads `$07` rather than the `$08` `$A77B` holds, and that is the
translation being right rather than wrong: `$2385` sets eight during the entity
pass, and `$0A28` has already taken one off by the time the next frame's
checkpoint is reached.

**The type byte does not stay `$16`.** `$2352` writes it, but by the top of the
next frame the entity state machine has moved it on - the capture reads `$00`,
then `$02` at frame 37 and `$04` at frame 42. So `$16` is a value the spawn
passes through, and no later test may assert that it persists.

The same run also caught the bubble's whole life, which is golden data for the
travel. That has moved to Phase 6 with the items it belongs to.

**Watch out, three ways, and each cost a run.** VICE's binary monitor halts the
emulator on *every* command, so a memory read stops the machine and it stays
stopped until an explicit exit - nothing may be read while the game needs to
run. Responses must be matched to the request that asked for them, or a read
returns an earlier read's answer and ninety identical frames come back in no
time at all. And **the frame anchor must not be `$E498`**: `wait_one_frame` is
a spin loop, the program counter is already sitting in it, so an exec
checkpoint there re-triggers at once and the machine never advances an
instruction. `$1CBD`, the top of the entity pass, is the right anchor - it runs
once a game frame, and `$08` advances by two across each one, which is the
double-buffer wait making the game 25fps against the 50Hz interrupt.

**The mover is verified against the game.** The whole of one bubble's rise -
seventy-two steps, `$DD` down to `$4D`, every distinct Y the capture read - is
asserted step for step in `EntityMoverTests`, along with X staying at `$74`
throughout. A mover that moved one pixel, or two on alternate calls, passes a
test of the first step and fails on the second. The other three arms are worked
by hand from the reference, because a bubble on level 1 only ever rises and
nothing observed uses them yet. 271/271, 0 warnings.

**Verify:** done, and both halves are above. Fire on level 1 puts a bubble in
slot 17 at `$34`/`$DD` three frames after the button, which is what the game
does byte for byte; and the mover reproduces the whole of one captured rise,
seventy-two steps of it. Where the bubble goes next is Phase 6's verify step.

## Phase 6 — Enemies

Translate: `entity-system.s`, `entity-state-tables.s`, `enemy-ai.s`,
`special-enemies.s`, `collision.s`, `entity-collision.s`,
`entity-interaction.s`, `entity-spawn.s`, `spawn-handlers.s`.

The largest phase. One enemy type at a time. `entity-system.s` is 1,447
lines and goes first, because everything else indexes into it.

**The bubble's own movement is in here, not in Phase 5.** A bubble is driven by
`enemy_ai_update` at `$0CF2` like everything else in the eighteen slots, so
travel, expiry and popping were moved here once that was found. Phase 5 keeps
what is genuinely the bubble's own: blowing one, and the shared mover at
`$0E23` that the bubble happened to need first.

**Where this is up to, as of 2026-09-24.** All eight enemy classes are
translated, and every one matches VICE frame for frame - see "Each enemy type"
below, and so does the Baron. Nothing is left in this phase. What calls it all
is Phase 7's.

**As of 2026-09-23.** A bubble runs from blow to pop.
The loop at `$0CF2` reaches every arm it has for an ordinary slot now - the
dispatcher, the catch, the random AI at `$0D86` and the map read at `$0E00` -
and a replay of the captured level 1 bubble matches the capture at every
checkpoint, at the measured two thirds. That is the first golden check in
this phase. The direction field is done and in use: `SolidMap` carries the
reference's own byte, and `$0E00` reads it.

**Nothing calls the loop yet.** `BubbleBobbleMain` does not run `$0CF2` or
`$13BE`, so a bubble moves in the tests and not in the app. Joining them to a
game loop belongs with `game-loop.s`, which is Phase 7's.

**Pick it up at Phase 7.** `EnemyFrame` and `EnemyAiLoop` need a game loop
to call them, and the hurry-up that spawns the Baron and angers every enemy is
the level timer's.

**One caution over everything translated in this phase.** Nothing from the AI
loop onwards has a golden trace behind it. `EnemyAiLoop`, `EnemyDispatcher`,
`BubbleCollision` and `EntityTimers` are each tested against a reading of the
6502, so their tests are self-consistent rather than proof. The `RowBias` item
below is what that looks like when it goes wrong: the tests were written against
the same constant they check and would have passed either way, and it took data
from outside them to settle. Now that the machine can be driven, every one of
those four is worth re-checking against it.

- [x] `entity-system.s` and the state tables. `$E9FD` to `$EFEA` is
      translated, across `EnemyWalker`, `EnemyJump`, `EnemyShot`,
      `EnemyChase`, `DiagonalMover`, `EntityAnimation` and the Phase 4
      player classes. What is left is `$F005`, the title screen, which is
      Phase 9's. `entity-state-tables.s` holds no state tables: `$3CB2` is
      screen-scroll code and data, and `$3CD7` sets up sprite pointers, so
      neither is Phase 6's.
- [x] `AddBbRandom`, with the RNG at `LE9EA`. `BbRandom` is `$E9EA` exactly,
      except the `CIA1_TBLO` byte, which comes from an injected
      `IRandomSource`. See "The RNG at `LE9EA`" below.
- [x] **The loop's own walk**, `$0CF2`, as `Enemies/EnemyAiLoop.cs`.

      This is the loop and the two decisions it makes without leaving itself:
      the special-type gate at `$0CF4` and the catch at `$0D02`. What it
      reaches is the item below, and both places it falls out are marked in
      the code.

      **The catch box is not centred on the player.** `$0D14` nudges the
      player's X two pixels right before the subtraction and `$0D28` does
      nothing of the kind to their Y. So the sixteen-pixel box reaches two
      pixels further right than left, and a translation that mirrors the two
      axes is wrong on one side of every enemy in the game. `NudgesThePlayerXAndNotThePlayerY`
      puts two slots the same distance from the player's own X, one each way,
      and proves only one of them is caught.

      **`$0D52`'s subtraction takes twenty-four, not twenty-three.** It is
      `sbc #$23`, but it runs with the carry clear, because `$0D50`'s `bcs`
      fell through to reach it. So types `$24` to `$33` index the sixteen
      entry jump table as zero to fifteen. Reading the operand alone puts
      every spawn handler in the game out by one. The handlers are not
      translated, so nothing depends on this yet - it is recorded because the
      next reader will make the same mistake.

      **`$0193` means two different things.** The blow at `$2352` writes the
      blower's facing into it, `$00` or `$80`, and `$0F98` reads the same byte
      back and tests its top bit. The catch at `$0D33` writes a player index,
      `0` or `1`, into that byte instead. Both are positive, so a caught slot
      reads as facing right. That is the reference's overload, not a slip in
      the translation, and anything later that reads `$0193` must know which
      of the two it is holding.

      **A caught slot is safe from being caught twice**, and by accident. The
      catch writes `$34`, which is itself above `$0CF4`'s gate of `$24`, so
      the next frame routes the slot to `$0D4E` and the type stored underneath
      it survives. `DoesNotCatchTheSameSlotTwice` proves it, because a loop
      without the gate would overwrite `$AA42` with `$34` and lose what the
      thing was.

      **`$AA42` is `EnemyType`, not `Variant`.** `ObjectTable` holds `$A9B2`,
      `$AA30` and `$AA42` as `State`, `Variant` and `EnemyType`, in that
      order, and `$0D3A` stores into the third of them. The first version of
      this item wrote the second, which is `$AA30` - a byte `$0F66` reads to
      decide whether to halve an animation index. Caught. The three names are
      close enough that the next reader should check the addresses rather
      than the words.

      **Not proved against VICE.** Nothing drives this loop yet - the
      dispatcher below is what the game spends its time in, and a capture of
      the walk alone would show nothing. The catch has no golden trace behind
      it either: level 1 with one player never puts a free enemy inside
      sixteen pixels of them in the frames captured so far. Every assertion
      here is worked by hand off the reference, and the first VICE run that
      exercises a catch may yet correct it.

      **Verified:** solution builds with 0 warnings, and
      `BubbleBobbleSharpLib.Tests` is 290/290, up from the 271 Phase 5 left.

- [x] **The movement dispatcher**, `$0F48`, `$0F61`, `$0F91`, `$0F98` and
      `$100B`, with `$7BFE` folded in, as `Enemies/EnemyDispatcher.cs`.
      `$0E23` is Phase 5 and `$0CF2` is above. **`$105B` is not done** - see
      the item below it.

      This is the eight-pixel half of entity movement, and it is not a variant
      of `$0E23`. A bubble opens by travelling through here and then rises
      through that.

      **The index is the counter from before the decrement.** `$0F61` is `dec
      D_A9B2,x` followed by `and #$7F`, and `dec` is a memory instruction that
      never touches the accumulator. So the mask falls on whatever A already
      held - the value `$0CF8` loaded, before the decrement. `$0F48` calls the
      routine twice, and the second call passes the value the first left. A
      translation that indexes with the decremented counter is one frame ahead
      of the machine in every animation in the game, and it still looks
      plausible. `IndexesWithTheCounterFromBeforeTheDecrement` is the test that
      separates them: a counter of six must read `$00` and not `$02`.

      **An entity's stored row is four more than a `SolidMap` row.** `$7BFE`
      indexes the row table at `$AC01`, and its first four entries are `$FF60`,
      `$FF88`, `$FFB0` and `$FFD8`. With the `$85` page added those are `$8460`,
      `$8488`, `$84B0` and `$84D8` - four forty-byte rows above the map's base
      at `$8500`. That is read off the table's own bytes, not off a comment,
      and it is confirmed sideways: `EntityMover` wraps a row at `$1D`, which
      is map row 25, exactly one past the map's twenty-five.

      Worth noting that `PlayerCell` reaches the same map through `$AC03`,
      one entry further along, with a bias of three. The two are consistent;
      they simply start from different places.

      **The two arms are not mirrors, three ways.** The right arm refuses to
      leave column `$1C` and the left arm has no bound at all. The left probes
      `$00`, `$29` and `$50`; the right probes `$01`, `$28` and `$51`, which is
      not that set reflected. And both move *before* they probe, so a blocked
      thing has already been moved and `$100B` is what puts it back.

      **`$1012`'s `adc #$13` adds twenty.** The carry the subtraction at
      `$100F` left is still set for any X at or above `$14`, so the snap back
      to the eight pixel grid returns `$45` as `$44`. Read as nineteen it puts
      every blocked thing one pixel short of its cell.

      **`$101C` looks at player two and nobody else.** `$BB` and `$C3` are the
      second byte of each of those arrays, and there is no index register in
      the routine. Player one has no say, which `IgnoresPlayerOneEntirely`
      proves by putting player one on top of the thing and getting a zero. The
      proximity does not decide whether the thing is caught either - a blocked
      thing with a negative counter is caught either way. All it decides is the
      byte written to `$0193`, which is the same overloaded byte the catch at
      `$0D33` writes.

      **`$A9D6` is the sub-position, not a direction.** `$7BFE` returns it and
      `$0FB4` branches on it to skip the first of the three probes, so the
      probe is skipped exactly when the thing sits on a cell boundary. The
      reference calls it a direction. `EntityMover` drives the same byte as a
      sub-position and a VICE capture of one bubble's rise proves it counts
      eight and wraps, so the comment is wrong and this is the eighth like it
      in these two files.

      **Not proved against VICE.** The bubble capture shows the eight-pixel
      shot - X `$34` to `$74`, two moves then a pause - which is this routine,
      but it was read one frame at a time and `$0F48` can run twice in a frame.
      So it cannot be asserted step for step the way the rise is. Each arm and
      each trap is tested on its own instead, worked by hand off the reference.
      The unexplained cadence is still unexplained, and `$105B` is still
      missing, so a capture that lines up is not possible yet.

      **Verified:** solution builds with 0 warnings, and
      `BubbleBobbleSharpLib.Tests` is 307/307, up from 290.

- [ ] **The bubble collision**, `$105B` and the capture at `$1090`, which the
      dispatcher calls twice and which is the last piece of `$0F98`.

      **Blocked on a decision, not on reading.** `$105B` runs `ldy #$05` and
      indexes `$B4,y`, `$BC,y` and `$C4,y`. Those are `$B2 + 2`, `$BA + 2` and
      `$C2 + 2` - the state, X and Y arrays `PlayerTable` already holds, two
      bytes along. `$1090` does the same with `$87A2`, `$87CA`, `$87F2`,
      `$863A` and `$85C2`, each of which is an `EntityTable` base plus two.

      So the two players and the six enemies share one set of eight-slot
      arrays: players are slots 0 and 1, enemies are slots 2 to 7. The
      reference has no separate enemy table at all.

      `EntityTable` is already eight slots and took this without a change -
      an enemy is just slot `y + 2`.

      **Settled: the three arrays have moved to `EntityTable`.** `State`, `X`
      and `Y` are eight slots there now, with players at 0 and 1. They are the
      odd ones out in that class - zero page rather than `$85xx`, so not a
      forty-byte row of the region the rest of it comes from - and the class
      says so.

      What that left in `PlayerTable` is what the 6502 really does keep two
      of: `$5D`, `$045A`, `$A824` and `$ACCB`. Holding state, X and Y there
      was a guess, and the reference has contradicted it.

      The move cost 181 call sites across 23 files, and it took the whole of
      `PlayerTable` out of eight classes - `PlayerFrame`, `PlayerMovement`,
      `PlayerJump`, `PlayerFall`, `PlayerDrift`, `PlayerSteer`, `PlayerLanding`
      and `PlayerDescent` no longer reference it at all, because those three
      arrays were the only thing they wanted from it. That is the split coming
      out clean rather than anything being lost.

      `EntityTableTests` is new and holds the width as a fact: eight slots,
      not two, with `$105B`'s own walk as the reason.

      **Verified:** the move changes no behaviour, and the whole of the
      existing suite proves it - solution builds with 0 warnings and
      `BubbleBobbleSharpLib.Tests` is 310/310, which is the 307 from the
      dispatcher plus the three new width tests.

- [x] **`$105B` and `$1090` themselves**, as `Enemies/BubbleCollision.cs`,
      called from both arms of `$0F98`.

      **Both paths end at the same store, and which byte arrives is decided
      fifteen instructions earlier.** `$10B1` is `sta D_AA30,x`, and the
      accumulator reaching it holds either the enemy's state byte, loaded at
      `$1095`, or the rise counter it used to have, loaded at `$10A1`. What
      picks between them is a `cmp` at `$1098` that does not touch the
      accumulator at all. The score at `$10BF` is then indexed with whichever
      one it was, so the two paths read different entries of the same table.
      `CarriesTheStateByteOnTheShortPath` and
      `CarriesTheOldRiseCounterOnTheLongPath` are the pair that separate them.

      **The walk stops at slot 2, and that is load-bearing.** `$105B` counts
      `y` from five down to zero over `$B4`, `$BC` and `$C4`, which is slots 7
      down to 2 of the eight. Slots 0 and 1 are the two players, and a walk
      that ran to the bottom would swallow a player standing where a bubble
      is. `NeverLooksAtAPlayer` holds that.

      **The band is `$0B` to `$15`, and `$0A` is outside it.** `$1060` and
      `$1064` pass over an empty slot and a slot in that band, and test
      everything else - including `$0A`, which sits just below the band and
      which `$1098` then sends down the long path. So `$0A` is both tested and
      reset, and a reading that lumps it in with the band misses an enemy
      state entirely.

      **The box is square here.** Sixteen pixels in both axes with no nudge,
      unlike `$0D02`, which shifts its X two pixels. Three sixteen-pixel tests
      now exist in this phase and only one of them is offset.

      **Open question: `$AB81`'s real length.** The reference labels it eight
      bytes, then `$AB89` begins. The byte that indexes it is the one just
      written to `$AA30`, which is bounded by nothing - an enemy still
      carrying the `$FF` its rise counter starts a level at walks straight off
      the end, and even the short path can reach nine. The 6502 reads on into
      `$AB89`.

      This throws rather than inventing bytes, the way `$ACB6` does in the
      dispatcher. It is not a settled rule and it may well need changing: a
      VICE capture of a real capture is what will say where the table stops,
      and that cannot happen until an enemy can be spawned.

      Two arrays arrived with this: `$85C0` as `Mode` and `$8638` as
      `HoldTimer`. Both are rows of the same forty-byte region the rest of
      `EntityTable` comes from, both have other users elsewhere in the
      reference, and both are named for what they do rather than for what they
      are, because what they are is not yet known.

      **Not proved against VICE.** Nothing spawns an enemy yet, so there is
      nothing to capture and no golden trace behind any of this. Every case is
      worked by hand off the reference.

      **Verified:** solution builds with 0 warnings, and
      `BubbleBobbleSharpLib.Tests` is 332/332, up from 310.

      **The type byte is an animation index, not a kind of thing.** `$0F91`
      reads `$ACB6` indexed by the AI state counter `$A9B2`, which `$0F61`
      counts down. The table is `$10 $04 $04 $02 $02 $02 $00`, so a counter
      walking down from six gives `$00 $02 $02 $02 $04 $04 $10` - and `$00`,
      then `$02`, then `$04` is exactly the order the bubble capture read.
      Reading and watching agree, which is rare enough in this file to record.

- [x] **A bubble's travel, expiry and popping**, which is the first thing the
      loop above will be able to drive. Golden data is already captured: one
      bubble on level 1, born at frame 32 and gone at frame 227.

      | Frames | What it does |
      |---|---|
      | 32-44 | shoots right, X `$34` to `$74`, eight pixels a move, two moves then a pause |
      | 45-151 | rises, Y `$DD` to `$4D`, two pixels a move, same two-then-pause cadence |
      | 153-157 | drifts right, X `$74` to `$7C`, two pixels a move |
      | 157-195 | parked at `$7C`/`$47`, under the ceiling |
      | 195-215 | type alternating `$04` and `$48` - the wobble |
      | 219-226 | type `$3A $3C $3E $40 $38 $36` - the pop |

      The rise is `$0E23` and is done. The eight-pixel shot is a *different*
      mover, in the dispatcher at `$0F98`, and the cadence - two moves then a
      pause, in both phases - belongs to whatever drives the mover rather than
      to the mover itself. That cadence is unexplained and is the first thing
      to settle here.

      **Done 2026-09-23.** The cadence is settled below. The last piece was the
      drift, which is `$0D86` and `$0E00` in `Enemies/EnemyAiLoop.cs`, and a
      replay of this capture is `BubbleTravelTests`.

      **`$0E00` reads offset `$29`, one row down and one column across.** Its
      row table at `$AD1E` gives `$8500` for row 4, which is the dispatcher's
      base and its bias of 4. The row it reads from is held to `$04`-`$1C`.
      `DoesNotReadItsOwnCell` holds the offset: a translation that reads the
      thing's own cell still rises and drifts, but at the wrong column.

      **A new bubble reads past `$ACB6`.** `$232A` blows a bubble with `$88` in
      its counter, so the dispatcher's first two steps index 8 and 7 in a
      seven-byte table. The 6502 reads on into `$ACBD`, whose first two bytes
      are `$00 $00`. The dispatcher threw there, so every bubble would have
      crashed it on its first frame. Its table now carries those two bytes, and
      it still throws past them. Nothing found this until a real bubble went
      through the loop. The three-types story in the dispatcher item is
      unchanged: `$00` for the first four steps, then `$02`, then `$04`.

      **The random arm's threshold is the level, not `$1E`.** `cmp #$1E` at
      `$0DC5` is self-modified. `$F23C` stores into its operand at every
      level's setup, and it stores A, which holds `SUBFLG`. The `$1E` or `$E6`
      that `$F230` works out goes into Y, and Y is never stored. So on level 1
      every random move is sideways, and deeper levels move vertically more
      often. On `SUBFLG` `$48` a fill loop has left `$02` in A, and `$02` is
      stored. The same fill loop writes `$02` to `$88C9`, `$88F1`, `$8919` and
      `$8941`. Those addresses are map row 24 and three rows below the map.
      `SolidMap` now reads the three rows below as copies of row 24 (see the
      row copy item), but it does not model this write yet.

      **The replay, in passes from the blow**, with the capture's frames
      converted at two thirds:

      | Phase | Capture | Port |
      |---|---|---|
      | Shot, X `$34` to `$74` | 0-8 | 0-8 |
      | Rise, Y `$DD` to `$4D` | 9-79 | 9-80 |
      | Drift, X `$74` to `$7C` | 81-83 | 81-84 |
      | Wobble | 109-122 | 109-124 |
      | Pop | about 125 | 125, which is `$7D` |

      **One disagreement is open.** Under the ceiling the port moves up and
      down between Y `$45` and `$47`. Map row 3 at column 14 pushes down and
      row 4 pushes up, so the code gives this result. The capture's summary
      above says "parked at `$47`". A capture that samples two passes out of
      three sees both values, so the summary may have smoothed it. Or the
      reading is wrong. A per-frame read of `$AA1E` for slot 17 in that
      window settles it.

      **Verified:** solution builds with 0 warnings, and
      `BubbleBobbleSharpLib.Tests` is 374/374, up from 350.

- [x] **The clock, the wobble and the pop**, `$13BE` in `player-sprites.s`, as
      `Enemies/EntityTimers.cs`. The game loop calls it at `$0A51`, straight
      after the AI loop.

      This was taken on to explain the cadence, and it did not. It explained
      two of the four phases instead.

      **Nothing in `enemy-ai.s` expires anything.** A bubble is made with `$7D`
      in `$A9FA`, and this is the only routine that takes anything off it. It
      counts a different byte from the AI entirely: `$A9FA`, which
      `ObjectTable` calls `Flags`, not the state counter `$A9B2`.

      **The wobble is `eor #$4C`.** `$04` becomes `$48` and `$48` becomes
      `$04`, so one exclusive-or does both halves. It runs only while the
      clock is below `$11` and only on the even counts, which is why it is
      half speed. That is the alternation the capture read at frames 195 to
      215, and it is now reproduced.

      **`$145C` is a `jsr` to the very next instruction.** The routine at
      `$145F` therefore runs twice - once through the call and once by falling
      into it - so the pressure pass pops two bubbles, not one. Read as a
      plain call it is half the rate.

      **And that pass is very nearly dead code.** `$144D` counts every slot at
      or above `$34`, and a free slot is `$FF`, which is above `$34`. So the
      empty table counts towards the total, the count reaches two the moment
      any slot is free, and the pass returns having done nothing. It can only
      fire with all eighteen slots full of low types. It reads like the thing
      that stops a player parking a level full of bubbles, and on these bytes
      it cannot fire until they already have.

      **The release is asymmetric.** `$13E8` sets the bottom bit of the row
      and `$13EE` clears the bottom bit of the column. A translation that
      tidies those into a matching pair puts every released enemy in the wrong
      place.

      `$85C0`, `$8638` and `$8728` arrived with this and the item before it,
      as `Mode`, `HoldTimer` and `FlashTimer`. All three are rows of the same
      forty-byte region and are named for what they do.

      The VIC sprite-enable flicker at `$142E` is not translated. It toggles a
      bit in a hardware register through the mask table at `$AB55`, which is
      VIC-II work and out of scope by section 1.

      **Verified:** solution builds with 0 warnings, and
      `BubbleBobbleSharpLib.Tests` is 346/346, up from 332.

- [x] **The cadence**, measured under VICE on 2026-09-19 and settled. It is
      not a pause in the AI at all - it is the capture's own clock.

      The capture shows two moves then a pause, in both the shot and the rise.
      Three things are now known that were not:

      `$0D86` falls straight through into `$0E23`. So the two movers are not
      chosen by type - they are chosen by the AI state counter. Non-zero sends
      the slot to the dispatcher and eight-pixel moves; zero sends it to the
      catch test, then the random AI, then the two-pixel mover. The shot and
      the rise are the same bubble either side of that counter emptying.

      `$0CF2` is called unconditionally, once per pass of the game loop. So
      nothing gates the loop.

      And the arithmetic now has a second, independent witness. `$A9FA` starts
      at `$7D`, which is 125, and `$13BE` takes one off it every pass. The
      capture's bubble is blown at frame 32 and pops at about frame 219, which
      is 187 frames for 125 decrements - almost exactly the same two-thirds as
      the mover steps show.

      Two unrelated quantities running at two-thirds of the capture's frame
      rate is not a pause in the AI. It points at the capture's own clock: the
      `$1CBD` entity pass, which the anchor counts, running about three times
      for every two passes of the `$0A00` game loop. The wait at `$0A5A` is on
      a self-modified value, so a variable ratio is possible.

      **`$1CBD` is called from the IRQ handler, not the game loop.**
      `irq-handlers.s:152` is the only caller. So the capture's anchor is on
      the interrupt side and the movers are on the game loop side, and the two
      really are different clocks - the question is only what the ratio is.
      That much is settled by reading, and it is the strongest support the
      hypothesis has.

      **A VICE run was attempted to measure the ratio and did not produce a
      usable number.** What it did establish: the emulator launches, the game
      loads and is verified in RAM at `$0A4E`, and the keyset drives fire into
      the window. What defeated it: the game sits on its title screen and the
      held fire presses never reached the game loop, so `$A9FA` never moved,
      and separately the monitor client returned values that cannot be real -
      a three-byte jiffy clock reading `010101` and then `040404`, and the same
      `$08` delta twice from different runs.

      Three traps were found and are written up in the session memory notes:
      every monitor command halts the emulator and not only a memory read; the
      `MON_CMD_EXIT` reply must be consumed or the stream desynchronises; and
      non-stopping checkpoints still emit a response per hit, which floods the
      socket until the emulator stalls. The way to count is RAM the game
      already ticks - `$13BE` decrements `$A9FA` for all eighteen slots every
      pass with no gating, so any slot is a free pass counter.

      **Measured: 3.0000 IRQ frames per game loop pass.** `$08` runs at 50.5 a
      second and the pass counter `$A9FA` at 17.0, over a four second window,
      and a three second window agrees at 2.98. So the game loop turns at
      50/3, about 16.7Hz, and not at the 25Hz a plain reading of the
      double-buffer wait suggests.

      `$1CBD` is the capture's anchor and runs at 25Hz. Its `jsr` sits behind a
      self-modifying jump at `$077C`, so the IRQ does not call it every time
      and its rate cannot be read off the source at all - the 25Hz is the
      earlier session's measurement of `$08` advancing by two across each
      trigger.

      That gives game loop passes per anchor of 16.7 / 25 = **0.667**. The
      capture's own two numbers were 0.679 from the mover steps and 0.668 from
      the timer. Three independent quantities at two thirds.

      **So there is no pause.** Everything moves every game loop pass, exactly
      as the code reads. The capture counted anchors, which tick three times
      for every two passes, so one pass in three had no move to show. The
      travel items can now be written against the code and converted to the
      captured frame numbers with a factor of two thirds.

      One caution carried forward: the ratio was measured on level 1 with one
      player and nothing much happening. A `$0A5A` wait on a self-modified
      value could differ when the loop has more work to do, so a busy level
      should be re-measured rather than assumed.

- [x] **The RNG at `LE9EA`**, and a constraint that came with reading it.

      It mixes in `CIA1_TBLO`, a CIA timer's low byte. Section 1 puts CIA
      emulation out of scope, so the sequence cannot be reproduced and
      `$0D86`'s random arms can never be matched byte for byte against a
      capture. Only the position-based arm at `$0E00` is deterministic.

      That is a constraint on how every enemy in this phase can be verified,
      not just the bubble, and it should be settled before the verify step
      below is relied on.

      **Settled 2026-09-23.** `BbRandom` translates `$E9EA`: the sixteen-bit
      shift, the fold, and the carry that `rol RESHO` leaves. It takes the
      timer byte from an injected `IRandomSource`. A test with a fake source
      reaches every branch. A capture can check a path only where the draw
      has no effect. That includes a lone bubble: with no neighbour, `$0D86`
      ends at `$0E00` whatever the draw is. The level-setup seed at `$2BFC`
      saves and restores the state, so nothing seeds it for play.

- [x] **The direction field in the collision map**, which is what a bubble's
      drift actually comes from, and which blocks the travel items above.

      **`$0E00` picks a direction by reading the map.** A slot whose AI counter
      has run down goes to `$0D86`, and `$0D86` falls straight through into
      `$0E00` for anything with nothing near it - so a lone bubble is driven
      entirely by `$0E00`. That routine builds a pointer from the entity's own
      row and column, reads the byte at offset `$29` - the cell below and one
      across - and falls into `$0E23` with it. `$0E23` masks it to two bits and
      that is the direction.

      **The map bytes carry that field, and `SolidMap` throws it away.** Read
      off level 1 under VICE on 2026-09-19, forty bytes to the row, first
      thirty-two shown:

      | Map row | Tile bytes | Low two bits |
      |---|---|---|
      | 0 | all `$82` | 2, down |
      | 1-3 | `$81`, then `$01` x13, `$02` x4, `$03` x12, `$83` | 1 right, 2 down, 3 left |
      | 4 | as above but `$00` across the middle | 0, up |
      | 5-7 | `$80` at the edges, `$00` between | 0, up |

      Bit 7 is solid, exactly as the port has it. The low two bits are a
      per-cell current, and the port's map is a `bool` per cell, so they are
      gone.

      **This is not the level scalar the plan assumed.** The item here used to
      read "bubble drift on the level's `bubbleCurrent`", as though one value
      per level pushed every bubble the same way. The map says otherwise: the
      top of level 1 pushes right over columns 2 to 14, down over 15 to 18, and
      left over 19 to 30, while the open playfield below reads zero and so
      sends everything straight up.

      That is the rise and the drift in one field. The captured bubble rose
      through the `$00` rows and then drifted **right** from X `$74` to `$7C` -
      column fourteen, inside the `$01` band. Two independent things agree.

      **Withdrawn: "`bubbleCurrent` is not where the field comes from."** That
      claim rested on a text search for `D_8B03` and `D_8B63` as symbols, and
      missed the one place either is read: `decompress_level_data` in
      `level-renderer.s`, which reaches them through a runtime pointer
      (`($13),y`) that no grep finds. Traced and confirmed against VICE below -
      `bubbleCurrent` is a real input to this field, just not the only one.

      **Mostly settled 2026-09-23, by reading plus a VICE capture on level 1
      (`SUBFLG` `$00`).** Three mechanisms, found in `level-renderer.s`, and
      each checked against the live map at `$8500`:

      **1. A rectangle's `zone_type` *is* the direction, one for one.**
      `decompress_level_data` (`$E18B`) reads from `zone_data`, which is
      `zones.json`'s own source (`build/convert-zone-data.py`). A rectangle
      encodes as `byte1 = 0x80 | (zone_type << 5) | x`, and `$E1F3`'s fill-
      pattern math - `and #$60` then two `asl` and two `rol` - is exactly the
      inverse: it takes bits 6-5 back out and packs them into bits 1-0. Level
      0's zone data (`level 1 -> 0` redirects here) has one rectangle,
      `rect 0 1 14 4 type=1`, and the VICE read of row 1 has direction `1`
      across exactly fourteen columns, `0` to `13` - the rectangle's own
      `x=0 width=14`, byte for byte.

      **2. The terminator byte triggers an unconditional mirror.** After a
      rectangle, `$E282` compares the command cursor against `$11` and jumps
      back to `$E1F3` if they differ - and `$11` holds the block's *total*
      length, not the command length, so the terminator (`$00`) is fed to the
      same dispatch as a command byte. `$E1F3` chooses format by bit 7 alone
      (`bpl` to the mirror path), and a `$00` terminator has bit 7 clear. So
      every level's zone data ends by falling into `$E24C`, the horizontal
      mirror, whether or not `zone-data.txt` says `mirror` - the explicit
      command and the terminator both land there, and Phase 2's "mirror
      expanded" bookkeeping only ever tracked the explicit one.

      The mirror copies column `c` (0-15) to column `31 - c`, and flips
      direction 1 and 3 into each other - `eor #$02` - whenever the source's
      bit 0 is set, leaving 0 and 2 alone. Checked column by column against
      the capture: source column 1 is `$81` (solid, direction 1); its mirror
      is column 30, and `1 eor 2 = 3` predicts `$83` there - which is what
      VICE reads. Column 13 (direction 1, the rectangle's last column) mirrors
      to column 18 and predicts direction 3 - also read. All twelve of columns
      18-29 check out the same way, as mirrors of the rectangle's columns
      2-13. This is what closes the "direction 3 for twelve columns" part of
      the pattern without needing a second rectangle - reading the zone data
      alone, there IS only one.

      Columns 0-1 and 30-31 read solid regardless of any of this - that is
      `init_level_renderer` forcing the sidebar columns wall, already
      established two bullets up, and orthogonal to the direction math.

      **3. Row 0's own default, where nothing else has drawn on it yet, is
      `bubbleCurrent`.** Before any rectangle runs, `$E1D1` fills each row
      uniformly from one byte: `AND #$03` against `($13),y` with `y = 3`, and
      `$13` walks from `$8B00` four bytes a row. Row 0 reads that byte at
      `$8B03` - and `init_level_renderer` (`$E2C3`-`$E374`) writes `$8B03`
      itself, in the "Update hole bits" step: `physics_flags[level]` is
      consumed four bits at a time, the low nibble (wrap openings) driving two
      table lookups into `$8B00`-`$8B03` and `$8B60`-`$8B63`, and what is left
      after those four `lsr`s - the high nibble, `bubbleCurrent` - has its own
      low two bits `ora`'d into `$8B03` and its high two into `$8B63`. Level
      0's `physics_flags` byte, read at `$C58E` (`physics_flags` is a fixed
      100-byte table, one per level, per `binaries.s`), is `$20` -
      `bubbleCurrent = 2`. Row 0's read direction, uniformly, is `2`. That is
      the whole of row 0's `$82 82 82 ...`, matched exactly.

      Four independent numbers now agree with `bubbleCurrent`, not with a
      guess: the exported field, the `physics_flags` byte read live, the
      `$8B03` derivation traced by hand, and row 0's own bytes.

      **Rows 1-23's own default, confirmed 2026-09-23 by our own capture.**
      [A write-up of this exact mechanism](https://geon.github.io/programming/2025/01/05/bubble-bobble-c64-wind)
      exists, from someone who disassembled the same routine independently. It
      says outright: "Read the last 2 bits off the unpacked level bitmap for
      each line to use as wind current. The top and bottom lines get their
      data from the hole metadata... Those 2 bits can be used since the sides
      of the levels always are solid anyway." That is our `$8B03`/`$8B63`
      "hole metadata" path for row 0 and row 24, and for rows 1-23 it is
      exactly the earlier guess: the fourth of each row's four copied bitmap
      bytes, reread as a direction - safe to repurpose because
      `init_level_renderer` forces the level's own leftmost and rightmost
      columns solid regardless of what the level bitmap says there, so
      whatever the true wall bit at that position would have been is already
      known and thrown away.

      Confirmed against an exec checkpoint on our own build, not taken on the
      other write-up's word alone. `$8B00`-`$8B5F` is `__SCREEN_BACKUP__`, and
      by the time a level is running it has been overwritten for its ordinary
      job - a read taken in a running level shows exactly that, `$8B04`
      onward reading `$10` repeating, a screen colour, not level data. So the
      capture breaks *inside* `decompress_level_data`, at `$E1A2` - the
      instruction right after its own `jsr init_level_renderer` returns, found
      from a `ca65 --listing` of `master.s` rather than guessed - which is
      after the buffer is filled and before `$E1D1` starts consuming it. See
      the `vice-drive` skill's "Exec checkpoints, working" section for the
      recipe; the checkpoint landed exactly on `$E1A2`, confirmed by the
      `STOPPED` event's own program counter.

      Level 1 (`SUBFLG $00`), row by row, the copied buffer's fourth byte
      against the live `$8500` map's own leftover columns, same level
      instance, both read in the same session:

      | Row | Buffer bytes | Last byte low 2 | Live map's own default |
      |---|---|---|---|
      | 0 | `FF FF FF FE` | 2 | 2 (`$8B03`/`bubbleCurrent`, not this table) |
      | 1-3 | `C0 00 00 02` | 2 | 2 |
      | 4-8 | `C0 00 00 00` | 0 | 0 |
      | 9, 14, 19 | `F1 FF FF 8C` | 0 | 0 |
      | 10-13, 15-18, 20-23 | `C0 00 00 00` | 0 | 0 |

      Every row matches. Rows 9, 14 and 19 are the platform rows the `RowBias`
      item measured independently (`SolidMap` rows 9, 14, 19 - entity row minus
      four), which is a second, unrelated confirmation that this buffer really
      is the level's own wall bitmap: the distinctive byte pattern lands
      exactly where the platforms are already known to be. The leading `C0` on
      the ordinary rows is `init_level_renderer`'s own "Set border wall bits"
      step (`ora #$C0` on each row's first copied byte), matching row 0-1's
      own forced-solid left edge.

      This closes the item: all four pieces - a rectangle's `zone_type`, the
      unconditional mirror, `bubbleCurrent` for rows 0 and 24, and each other
      row's own wall-bitmap byte - are now confirmed by reading plus our own
      VICE capture, not resting on a second source alone.

      **The map is rebuilt per level**, which is worth stating because it was
      observed rather than assumed. Map row 2 reads `8080` then twenty-eight
      `00` then `8080` on the select screen, and `8181`, thirteen `01`, four
      `02`, eleven `03`, `8383` once level 1 is running.

      **Built, 2026-09-23.** `SolidMap` keeps one byte a cell now - bit 7
      solid, low two bits direction - and `SolidMap.Build(Level, IReadOnlyList<ZoneRect>)`
      layers it the way the reference does: a uniform row default first
      (`bubbleCurrent` for row 0 and row 24, the last two columns of that
      row's own bitmap for every other row), the existing solid computation
      unchanged except for one real bug this surfaced - the rightmost two
      columns were never forced solid the way the leftmost two already were,
      which `WallsOffTheOutermostTwoColumnsOfEveryLevel` now covers on both
      edges - and the level's own rectangles overlaid last, direction only,
      never touching the solid bit zone data never carries.

      **The export tool had the matching bug**, found while wiring this up
      rather than before. `mirror_rect` copied a rectangle's direction
      unchanged instead of flipping 1 and 3, and `level_rects` only mirrored
      when an explicit `mirror` command appeared in `zone-data.txt` - which,
      checked directly, happens **zero times in the whole file**. Every
      level's mirror, where one applies, comes from the terminator byte alone
      - `has_trailing_zero` now runs the real `encode_level` to ask whether a
      terminator exists at all, rather than re-deriving its branch and
      risking drifting from what the build actually produces. Level 1's
      `zones.json` entry gained its second, mirrored rectangle
      (`x=18 width=14 type=3`) from this fix alone; level 14, whose
      rectangles fill their declared block with no room for a terminator, is
      unchanged, which is the exact case that made width-29 rectangles look
      wrong until the terminator check explained them.

      `ZoneRect`, `LevelZones` and `ZoneStore` mirror `Level`/`LevelStore`,
      reading `zones.json` as a file of its own rather than folding it into
      `Level` - Phase 2 kept the two exports apart on purpose, and `SolidMap`
      is the one place they now meet, at build time rather than load time.
      Wired through `BubbleBobbleServiceCollectionExtensions` and
      `BubbleBobbleMain` the same way `LevelStore` already was. `Playfield`
      passes `[]`: it only ever reads the solid bit, which zone data never
      changes, so it owes SolidMap nothing further.

      **Verified:** solution builds with 0 warnings, `BubbleBobbleSharpLib.Tests`
      is 350/350, up from 346, including a byte-exact check of level 1's own
      row 1 against the VICE capture above - `81 81` then thirteen `01`, four
      `02`, twelve `03`, `83 83` - and confirmed in the real app: `ShowLevel(1)`
      builds a `SolidMap` through the new `ZoneStore` wiring without error, and
      a `GAME_FRAME_DUMP_DIR` capture of level 1 is pixel-identical to earlier
      ones, as it has to be - the two changed columns are always under the
      sidebar decoration in the composited frame, so nothing about this was
      ever going to be visible, only reachable by a probe.

- [x] **`EnemyDispatcher`'s `RowBias` checked. It is 4, and it is right** -
      but the reasoning that produced it was not, so the comment has been
      rewritten rather than left resting on a wrong argument.

      The doubt was real. `$8500` is **not** the first row of level tiles:
      `sprites2-tables.s` lays each forty-byte group out as eight bytes of
      entity metadata followed by thirty-two tile bytes, and `$8500-$851F` is
      the screen wrap permission table and the two spawn point availability
      tables. Tile rows start at `$8528`, which is `$AC01` entry five. On that
      reading the bias should have been 5.

      What rescues the 4 is that the wrap permission block doubles as the
      ceiling a probe hits - it reads solid - so it is `SolidMap`'s row 0. That
      gives `SolidMap` row = tile row + 1, and tile row = entity row - 5, and
      the two together give entity row - 4.

      **Measured on both sides rather than argued.** Level 1 read in play has
      its platforms on tile rows 8, 13 and 18. The port's own level 1 bitmap
      puts them on `SolidMap` rows 9, 14 and 19. The +1 is observed twice, from
      two independent places, and the geometry matches exactly - which also
      says `SolidMap.Build` reproduces the game's level correctly.

      The tests were no help here and could not have been: they were written
      against the same bias they check. What settled it was data from outside
      them.

- [x] **Wrap-around openings from `wrapOpenings`**, as the whole of `$EFC0`
      in `Enemies/DiagonalMover.cs`. `$0E23` was already done: a row off
      either end comes back at the other and the thing becomes type `$38`,
      and that mover asks nothing about the level.

      **`$EF4C` and `$EFA0` are the only readers of the wrap tables.** They are
      the up and down halves of `$EFC0`, which is state 5 in the `$1E3A`
      table: a thing that moves one pixel up or down and two across on each
      pass, and turns round off whatever it meets. `$8501` and `$88C1` are
      `SolidMap` rows 0 and 24, one column in, and `Edge` already opens them
      from `wrapOpenings`. So nothing new was needed from the level data.
      The routine was translated whole, because the wrap is part of its
      vertical halves. `$85E8` arrived with it as `EntityTable.Heading`: bit
      0 set is left, and bit 2 set is up.

      **The two wrap checks are one pixel apart.** Both reference comments say
      the `sbc` runs with the carry clear. In both cases the carry is set.
      The top check is reached through a `bne` that is not taken, and the
      bottom check through a `bcc` that is not taken. So the top check
      subtracts `$13` and the bottom check subtracts `$14`. At X `$53` the top
      asks about column 9 and the bottom asks about column 8.
      `AsksAboutTheColumnAPixelFurtherRightAtTheTop` holds both edges of an
      opening.

      **A wrap does not end the pass.** Unlike `$2586` in `PlayerFall`, the
      wrap returns into `$EFD6`. So the thing still moves across and animates
      on the pass it wraps.

      `$EEEB` and `$EF2A` have other callers. Those callers store `$BD` into
      `$EF15`, so a turn also flips the sprite's facing. `$EFD6` stores `$60`,
      an `rts`, so that path is not here.

      **Not proved against VICE.** Which enemy takes state 5 is not known, and
      nothing in the port calls the mover yet. Every case is worked by hand
      off the reference.

      **Verified:** solution builds with 0 warnings, and
      `BubbleBobbleSharpLib.Tests` is 389/389, up from 374.

- [x] **The enemy spawn**, `$1E2E` and `$39D2` in `entity-spawn.s`, as
      `Enemies/EnemySpawner.cs`. A level's list goes into slots 2 to 7.

      **The state is the class plus two.** `$39FB` adds two to the class
      bits and stores the result in `$B4`, which is `State` two slots along.
      So class 3 takes state 5, and state 5 is `DiagonalMover`. That answers
      the question the wrap item left open.

      **`$1E2E` is not "get random spawn data".** It clears `State`, `X` and
      `Y` of slots 2 to 7 and returns the zero it cleared them with. That
      zero is then the enemy count at `$4A`, the first slot and the first
      byte of the list.

      **Classes 6 and 7 read past each table.** `$AB61`, `$AB69`, `$AB71`
      and `$AB79` are eight bytes each, indexed by the state, and states 8
      and 9 go past the end. The tables are next to each other, so the 6502
      reads the first two bytes of the next one. Class 7 gets its frame count
      from `$AB81`, which is `$00`, and so a frame mask of `$FF`. This is not
      rare: 133 of the 572 spawns in the game are class 6 or 7.

      **`$8638` is the spawn delay.** `$39F0` stores twice the record's delay
      there, and it clears the two players' bytes. `HoldTimer` was named for
      what it does before anything told us what it holds.

      **The column's carry goes into the row.** `$3A0B` adds with the carry
      that `$3A03` left, so a column of 30 or more puts the thing one pixel
      lower. No level has such a column.

      Five arrays arrived with this: `$8598` `SpriteBase`, `$8660`
      `TurnTimer`, `$8688` `TurnInterval`, `$8750` `FrameCount` and `$8778`
      `FrameMask`. `$4A` is `EntityTable.EnemyCount`.

      **Not done here.** `$3A6C` copies the map's edge rows outwards. That is
      the next item. `$3A89` sets up a level's special bubbles through
      operands in the game loop, so it goes to Phase 7 with `game-loop.s`.
      Nothing in the app calls the spawn yet.

      **Proved against VICE on 2026-09-23.** An exec checkpoint on `$3A89`,
      the first instruction after the spawn loop and the row copy, stopped
      the machine, and every array the spawn writes was read for slots 2 to
      7. Level 1 was a real game. Levels 6 and 60 were an intervention: the
      game stopped at `$39B0`, the `ldx SUBFLG` that picks the list, and
      `SUBFLG` was written there. That gave classes 6 and 7. Every byte of
      all eleven enemies matches the port, including class 7's frame count
      of `$00` and mask of `$FF`. The eleven turn timers are the RNG's, so
      the test checks only that each is in the range its table allows, and
      all of them are. `EnemySpawnerGoldenTests` holds the capture.

      **Verified:** solution builds with 0 warnings, and
      `BubbleBobbleSharpLib.Tests` is 400/400, up from 389. The golden test
      came with the next item.

- [x] **The row copy**, `$3A6C` in `entity-spawn.s`, in `SolidMap`.

      **The rows past the map's edges are not a solid ceiling.** The tables
      in `sprites2-tables.s` start `$8488`, `$84B0` and `$84D8` as rows of
      `$80`. But `$3A6C` copies all forty bytes of row 0 over each of the
      three as every level starts, and row 24 over `$88E8`, `$8910` and
      `$8938`. So a wrap opening goes on through three more rows, and those
      rows carry the edge's direction. `SolidMap` had them as solid and
      direction 0. That is correct for the tables' first bytes and wrong
      after the first level starts.

      `SolidMap` does not store the copies. It reads rows −3 to −1 as row 0
      and rows 25 to 27 as row 24. The map does not change after it is
      built, so this gives the same result as the copy. Row −4, `$8460`,
      is not copied. It still reads solid, and what it really holds is not
      known.

      **This changes one reading in `EnemyAiLoop`.** `$0E00` clamped at the
      bottom reads one row past the map, at `$88E8`. That read now gives the
      floor's direction, where it gave 0 before. No existing test depended
      on the old value.

      **Proved against VICE on 2026-09-23,** in the same capture as the
      spawn. On level 1, `$8488`, `$84B0` and `$84D8` read `$82` in all
      thirty-two columns. That is row 0, and not the `$80` that the tables
      start them as. `$88E8`, `$8910` and `$8938` read `$80`, which is row
      24. Level 1 has no wrap opening, so the opening case is not captured.
      But the copy is all forty bytes, so it cannot treat an opening in a
      different way.

      **Row −4 is not solid.** `$8460` reads `ff f0 c0 00 00 ...` - a
      pattern with `$00` cells in the middle of it. `SolidMap` reads that row
      as solid. What the bytes are, and whether any probe really reaches
      them, is not known. `EnemyDispatcher`'s bias of 4 puts entity row 0 on
      it, so this is where to look first if a thing at the top of the screen
      disagrees with a capture.

      **Verified:** solution builds with 0 warnings, and
      `BubbleBobbleSharpLib.Tests` is 407/407, up from 400. One off-map test
      case moved out to rows −4 and 28. Four hand-worked cases and one
      golden case cover the copy, and three golden cases cover the spawn.

- [x] **Each enemy type**, as `Enemies/EnemyFrame.cs` - `$1CBD`'s loop over
      slots 7 to 2, `$1CA0`, `$1E87` and `$1E6C` - and one class per
      handler in the `$1E3A` table.

      | Class | State | Handler | Class in the port | Levels |
      |---|---|---|---|---|
      | 0 | 2 | `$EA08` | `EnemyWalker`, `EnemyJump` | 1, 3 |
      | 1 | 3 | `$E9FD` | `EnemyShot`, then the walker | 42 |
      | 2 | 4 | `$1F2E` | `EnemyHopper` | 36 |
      | 3 | 5 | `$EFC0` | `DiagonalMover.Step` | 22 |
      | 4 | 6 | `$EEB2` | `DiagonalMover.StepFacing` | 12 |
      | 5 | 7 | `$E9FD` | `EnemyShot`, then the walker | 50 |
      | 6 | 8 | `$E9FD` | `EnemyShot`, then the walker | 7 |
      | 7 | 9 | `$1E9F` | `EnemyRunner` | 60 |

      **Proved against VICE on 2026-09-23, every class, a frame at a time.**
      Two exec checkpoints bracket one enemy slot's turn of `$1CBD`: `$1CDB`,
      the top of the loop, and `$1D24`, its end. Every array the slot owns was
      read at both, with the two players, `$26`, `$27` and ObjectTable. The
      game stopped at `$09A5`, the level's initialisation, and `SUBFLG` was
      written there for every level but 1. It counts from zero. The player
      pressed up every three seconds. `EnemyFrameGoldenTests` replays 9,450
      slot frames on the eight levels in the table, and
      `EnemyWalkerGoldenTests` replays 2,100 calls of `$1E6C` on levels 1
      and 3. All match, byte for byte.

      The random draws are recovered, not matched. The test works out the CIA
      byte that takes `$26` and `$27` from the capture's before to its after,
      and feeds it back through `IRandomSource`. That proves every decision
      taken on a draw.

      **Classes 1, 5 and 6 are class 0 with a gun.** `$E9FD` runs `$EDCD`,
      which books a shot in ObjectTable, and then either the walker or `$ED3B`,
      which fires the shot seven frames later. While a shot is booked, `$8818`
      is the countdown and `$87A0` holds the booked slot. The indexes are two
      along: the 6502's slot 0 is ObjectTable's slot 2.

      **`$EDF1` subtracts with the RNG's carry.** The `and #$03` after
      `jsr $E9EA` does not touch the carry. So an enemy exactly level with its
      player in X takes either arm, by the draw. `BbRandom.Carry` exposes it.

      **`PlayerCell.Solid` now carries a column into the row.** `$ED3B`'s
      probe at `$27` is the row below and one column left, and the old
      arithmetic read the same row 39 columns on. Level 7's shots found it.
      Every offset used before gives the same answer both ways.

      **The map's rows end in entity arrays, and a probe can read them.**
      `$EA92`'s leap looks nine columns along. Near a wall, that is one of the
      eight bytes after the level's 32 in the forty-byte row - `$8840` on row
      20, and so on. Level 50 found it: a class 5 enemy at column 25 did not
      leap, because slot 2's `$8840` is zero. `SolidMap` now reads columns 32
      to 39 through `IRowTails`, which `EntityTable` answers for, and
      `ShowLevel` passes it in. Rows 23 and 24's tails read zero in every
      capture.

      **`$EBB8` changes only an opcode.** The hopper falls by `$EB48` with an
      `rts` continuation, but a landing leaves through `$EB94`, whose operand
      is still `$EB0F`. So a hopper steps its frame as it lands, and at no
      other time in a fall. Level 36 found it.

      **Several reference headings are wrong.** `$1F2E` and `$1E9F` are enemy
      handlers, not player code. `$ED68`'s shot goes left, not right.
      `$EE62`'s "player below" is the player above. The class comments record
      each one.

      **Not here:** `$1CFB`'s test of `$67`, which freezes enemies below state
      `$0B` while an item's effect runs. That is Phase 7's, and no capture
      reached it. Nothing in the app calls `EnemyFrame` yet.

      **Verified:** solution builds with 0 warnings, and
      `BubbleBobbleSharpLib.Tests` is 12,001/12,001, up from 407.

- [x] **Baron Von Blubba**, as `Enemies/Baron.cs` - `$1473` to `$1577`,
      reached from `EnemyAiLoop` for types `$2E` and `$30`.

      **He is not `baron_von_blubba`.** `special-enemies.s` gives that name to
      `$10D3` and type `$44`. He was watched in VICE on 2026-09-23: level 1,
      the enemy slots `$B4` to `$B9` zeroed, the player idle, and the types
      and states sampled each second. No `$44` appeared. When the hurry-up ran
      out, about 40 seconds in, `$1677` put type `$2E` in ObjectTable slot 0.
      Twenty seconds later player 1 was in state `$0E`, with no enemy in any
      slot. `$0D4E`'s table sends `$2E` and `$30` to `$1473`, which the
      reference calls `player_death_handler`. `$44` is a bubble type that
      `$23D5` writes.

      There is one Baron per player, in the ObjectTable slot of the player's
      number. He moves one cell a pass, in runs along one axis and then the
      other, with a thirty-pass pause between pairs of runs, and through
      walls. He kills a player within sixteen pixels in each axis, and he goes
      when his own player dies.

      **Not here, and Phase 7's:** `$1621`, which spawns him as the hurry-up
      ends, and `$1490`'s store in `$2B`, the level timer.

      **Proved against VICE on 2026-09-24.** Level 1, with `$B4` to `$B9`
      zeroed as the level started so the player lived to the hurry-up. Once
      `$CA` held `$2E`, exec checkpoints on `$1473` and the next `$0D4A`
      bracketed 800 of his passes, with the player moved by a key every two
      and a half seconds. They include 43 column moves, 40 row moves, 24 new
      runs, 12 arrivals, both facings and two kills. `BaronGoldenTests`
      replays every pass, and all match byte for byte. The 19 cases in
      `BaronTests` stay as the hand-worked proof.

      **Verified:** solution builds with 0 warnings, and
      `BubbleBobbleSharpLib.Tests` is 12,802/12,802.

- [x] **The anger state.** At `$1D03`, an enemy whose `$8728` is not zero
      takes a second `$1E6C` on every frame whose counter has bit 1 clear, so
      it moves half as fast again. `EnemyFrame` has it, and level 1's capture
      proves it: `$8728` was written `$FF` eight seconds in. What sets it is
      already translated for the bubble release (`$13FA`, `EntityTimers`). The
      hurry-up and the last enemy standing also set it, at `$16E4`. That is
      the level timer's, and so Phase 7's.

**Verify:** each type spawned on an empty test level matches a recorded byte
trace from VICE over 200 ticks. A golden frame per enemy type. And the bubble:
it reaches the same tile as in VICE after 30 ticks, and one left alone expires
after exactly the number of ticks the capture counts.

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

**That decision is under review** - see "Audio per rendition" in
[backlog-roadmap.md](backlog-roadmap.md). If a SID emulator arrives for the
8-bit renditions, porting the player becomes the better option rather than the
forbidden one: same trigger IDs, every tune exact, and no recorded audio to
ship. Check there before starting this phase.

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
