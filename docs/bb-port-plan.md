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

- [ ] **Licence.** `rebb64` has no `LICENSE` file and contains Taito and
      Firebird material in derived form. Settle what the port may ship
      before writing code that ships it.
- [ ] **Toolchain.** .NET 10 SDK, Python 3, and `cc65`.
- [ ] **VICE.** Installed. It is the reference runner for visual comparison.
- [ ] **Baseline.** `make verify` passes in `rebb64/build/`. It checks the
      assembled output against the SHA256 of the original PRG, and it is
      what makes every later fidelity claim mean anything.

## 4. Shape

Three assemblies plus the app, mirroring Elite:

```
src/bb/libs/BubbleBobbleSharp.Abstractions/     contracts and view models
src/bb/libs/BubbleBobbleSharpLib/               the game: the 6502 translation
src/bb/libs/BubbleBobbleSharp.Renditions.C64/   the drawing, and the assets
src/bb/apps/BubbleBobbleSharp/                  entry point
src/bb/test/BubbleBobbleSharpLib.Fakes/
src/bb/test/BubbleBobbleSharpLib.Tests/
```

The division is Elite's, and it earns its place here for the same reasons:

- The game computes a **model**; the rendition **draws** it. A view is
  `IView<in TModel>`, holds no state and derives nothing.
- A view is handed an `IViewSurface` and sees nothing else of the game:
  graphics, layout, palette.
- The **rendition declares the screen size**, not `Program.Main`. The C64
  rendition says 320x200 and picks its own window scales.
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

**To do — the three-assembly split**

- [ ] Create `BubbleBobbleSharp.Abstractions`. It references only SharpKind,
      never the game.
- [ ] `IBbRendition` in it: `Name`, `ScreenWidth`, `ScreenHeight`,
      `WindowScales`, `DefaultWindowScale`, `CreateViews(IViewSurface)`.
      Model it on `EliteSharp.Abstractions.Renditions.IRendition`, keeping
      only the members Bubble Bobble has a use for.
- [ ] `IBbViewSurface` and `BbViewLayout`, on
      `EliteSharp.Abstractions.Views.IViewSurface` and `ViewLayout`. The
      layout's derived members are the playfield and HUD rectangles.
- [ ] `IView<in TModel>`, copied from Elite's.
- [ ] Create `BubbleBobbleSharp.Renditions.C64`, referencing Abstractions
      only. `C64Rendition : IBbRendition`, `Name => "c64"`, 320x200.
- [ ] Move `Assets/` from `BubbleBobbleSharpLib` into the rendition, with
      the `None Update="Assets\**\*"` item from Elite's rendition csproj.
- [ ] `RenditionLoader` in `BubbleBobbleSharpLib`, on
      `EliteSharpLib/Renditions/RenditionLoader.cs`.
- [ ] The app's `CopyRenditions` target, copied from `EliteSharp.csproj`,
      and the `ReferenceOutputAssembly="false"` project reference.
- [ ] Take `ScreenWidth`/`ScreenHeight` out of `SDLProgram` and read them
      off the loaded rendition.
- [ ] Add both new projects to `TheSharpKind.slnx`.

**Verify:** the app still opens a black 320x200 window and exits on Escape,
and the startup log line names `"rendition":"c64"` rather than `"16-bit"`.

## Phase 1 — The sprite engine

New shared code in `SharpKind.Graphics`, on top of `IGraphics`. It is shared
rather than local because a 2D sprite layer is engine work, not Bubble
Bobble's.

- [ ] `SpriteSheet` — loads an atlas bitmap plus its JSON index.
- [ ] `SpriteAtlas` — name to source rectangle.
- [ ] `SpriteBatch` (or similar) — draws a named sprite at a position,
      mirrored on request, over `IGraphics.DrawImagePart`.
- [ ] Unit tests in `SharpKind.Graphics.Tests`.
- [ ] `RecordingGraphics` records whatever new calls it makes.

Rules, because this touches shared code:

- Additive only. No changed signatures on `IGraphics`.
- If `IGraphics` does gain a method, update `SoftwareGraphics`,
  `SDLGraphics` and `RecordingGraphics` in the same change.
- Elite's and SCR's full suites pass before and after, golden frames
  included.

**Verify:** `SharpKind.Graphics.Tests` covers the new types, and Elite and
SCR are untouched and still green.

## Phase 2 — Asset extraction

`rebb64/data/` already holds the data in editable form. This converts it to
what the engine reads.

- [ ] `rebb64/build/export-csharp-assets.py`, reusing the parsers in
      `convert-levels.py`, `convert-zone-data.py` and `convert-tga.py`. Do
      not re-invent them.
- [ ] `Images/atlas.bmp` — every `.tga` packed into one 24-bit BMP, indices
      resolved to RGB, index 0 the transparent colour, with a recoloured
      copy of the tile set per level colour.
- [ ] `Images/atlas.json` — the sprite index the `SpriteSheet` reads.
- [ ] `Levels/levels.json` — 100 levels: the 32x23 bitmap as a string array,
      plus `colors`, `sidebar`, `bubbleCurrent`, `wrapOpenings`, `foodDrop`,
      `powerupSpawn`, `spawnFlags` and the enemy list.
- [ ] `Levels/zones.json` — from `zone-data.txt`, mirroring already applied.
- [ ] Replace `bbc-micro.bmp` with the game's own font from
      `charset.tga` and `hud-font.tga`.
- [ ] `LevelStore` and `AtlasIndex` in the lib.

**Verify:** a test loads all 100 levels, asserts there are 100, and asserts
level 1's bitmap row 8 is `..##...##################...##..`.

## Phase 3 — Static rendering

No game logic. Put a level on screen. Three `RenderLayer`s, each with its own
`ILayerDrawer`, each drawing a model the game computed.

Translate: `level-renderer.s`, `level-display.s`, `graphics-copy.s`, and the
sidebar handling in `render-screen.s`.

- [ ] `SidebarModel` / `SidebarViewC64` — sidebars from `sidebars.tga`,
      chosen by the level's `sidebar` byte.
- [ ] `PlayfieldModel` / `PlayfieldViewC64` — the 32x23 tile grid, using the
      level's `colors` byte, clipped so nothing spills into the sidebars.
- [ ] `HudModel` / `HudViewC64` — both scores, high score, lives.
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

- [ ] Golden frames for all 100 levels.
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
