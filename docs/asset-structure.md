# Asset Structure — The Sharp Kind

How game assets are laid out, resolved, decoded and validated. This began as
the design note behind the multi-resolution tier decision in
[decisions.md](decisions.md).

**Read it knowing that tiers became renditions on 2026-08-03**, and that
several of the calls below were reversed by that. Each section says so where
it applies; the reasoning is kept rather than deleted, because why a thing was
rejected is worth having when it turns out to be the answer.

## Layout

> **Superseded on 2026-08-03.** The layout below was category-first with a
> tier subfolder, and the alternative described further down — a tree per
> tier — was rejected. Elite now uses that alternative, because a tier became
> a **rendition**: a plugin assembly that draws the game, found at startup, and
> a plugin has to be able to bring its assets with it. What each section still
> gets right is marked as it comes. Stunt Car Racer, which has one rendition
> and no plugin model yet, keeps a single flat `Assets/` tree.

An asset belongs either to a rendition or to the game. The artwork, fonts,
palette and models are what a rendition looks like, so they travel with its
assembly; the audio is not a rendition concern and stays with the executable.
Every kind of font - the rendition's own sheets, a Windows `.fon` and a
TrueType face - shares one `Fonts/` folder, since what differs between them is
how the manifest declares them rather than where the file sits:

```
Renditions/
  EliteSharp.Renditions.EightBit/
    EliteSharp.Renditions.EightBit.dll
    Assets/
      AssetManifest.json
      Images/       hud.bmp, ...
      Fonts/        bbc-micro.bmp, Bm437_Master_512.FON, Mx437_Master_512.ttf
      Palette/      palette.json
      Models/       adder.obj, ..., palette.mtl
  EliteSharp.Renditions.SixteenBit/
    ... the same, its own
Assets/
  AssetManifest.json
  SFX/  Music/  SoundFonts/               <- the game's own
```

The manifest groups them the same way the folder does - one `Fonts` section
holding a subsection per kind, named as the `FontKind` setting names them, so
a manifest section and a setting value read the same:

```json
"Fonts": {
  "Bitmap":   { "Small": { "File": "bbc-micro.bmp", "CellWidth": 8, ... } },
  "Fon":      { "Small": { "File": "Bm437_Master_512.FON", "PixelHeight": 8 } },
  "TrueType": { "Small": { "File": "Mx437_Master_512.ttf", "PointSize": 8 } }
}
```

A kind a set declares nothing for is simply absent, and the engine draws with
that set's own sheets instead. A section the build does not recognise is
refused outright rather than read as declaring nothing, so a misspelling says
so when the manifest is read rather than as a crash on the first screen that
draws text.

`RenditionAssets` composes the two into one `IAssetLocator`, so nothing that
consumes an asset knows there are two places to look.

### Why the rejected alternative won

The objection recorded below was that a tree per tier "forces every `.obj` and
`.ogg` to be triplicated or a `Shared/` pseudo-tier to be invented". That has
expired, and by the same 2026-07-30 change that recorded it:

* **The `.obj` files were already split.** Models joined the tier-varying
  categories in that change, because a model's `usemtl` names resolve through
  the palette and the two palettes stopped agreeing. There is nothing left to
  triplicate.
* **The `.ogg` files never move.** Audio stays in the game's own `Assets/`,
  which is not a pseudo-tier — it is the game's, and a rendition has no
  opinion about the sound of a laser.

What tipped it is that a rendition is a plugin now. A stranger's rendition
that could draw but had nothing to draw with would not be a rendition at all.

Models joined the tier-varying categories on 2026-07-30. Geometry is
resolution-independent, so they were tier-neutral at first — but a
model's colours are not geometry: `ModelReader` resolves each
`usemtl <name>` straight through the active palette, and once the 8-bit
palette stopped being a subset of the 16-bit one (see below), 13 of the
21 material names the ships use no longer existed at 8-bit. A shared set
cannot name colours that only one tier has.

Each tier's folder carries its own `palette.mtl`, regenerated from that
tier's `palette.json`. The game never reads it — `ModelReader` resolves
`usemtl` through the palette directly — but the `.obj` files declare
`mtllib palette.mtl`, so without one per tier the models open with
missing materials in external tools.

The rejected alternative was tier-first (`Assets/EightBit/Images/...`,
the whole tree duplicated per tier). It makes adding or removing a tier a
single folder operation, but forces every `.obj` and `.ogg` to be
triplicated or a `Shared/` pseudo-tier to be invented — which is the
category-first layout again, with an extra concept. **This is the call that
was reversed on 2026-08-03; see the note at the top for why it no longer
holds.**

The palette is tier-scoped because the colour budget below makes it so:
an 8-bit palette cannot be the 16-bit palette.

## Manifest

**Superseded on 2026-08-03.** One manifest per rendition, plus one for the
game, each listing only what it owns. A rendition's manifest is its whole
manifest, so there is nothing to overlay onto and the `Tiers` list and
`TierOverrides` escape hatch are both gone - a rendition that is not installed
is caught by the loader, and a rendition whose set genuinely differs simply
says so, because it is the only one writing its manifest.

What a rendition adds instead is what it declares about itself:

```json
{
  "Colours": { "MaxColours": 16, "PaletteNamesEveryColour": true, "ChannelBits": 8 }
}
```

The game cannot know this about a rendition it was never built against - a
stranger's could be four colours or sixteen million - so the limits enforced
at load are the ones the rendition claimed. Declaring nothing means
unconstrained, which is the only honest default: nothing a rendition ships can
then be rejected for a limit it never claimed to have.

## Resolution

**Superseded on 2026-08-03.** `AssetLocator` is still the only place asset
paths are built, but there is no tier in a path any more. The rule is one
line - `<Category>/<file>`, under whatever directory the locator was pointed
at - because the rendition's folder is the answer. The two-step lookup, the
fallback and `TierPath` are gone with it.

A game wanting both its own assets and a rendition's composes two locators;
Elite's `RenditionAssets` does that, and `IAssetLocator`'s members are
unchanged, so no consumer of `ImagePaths` and friends knows either exists.

`SystemTier` is gone. A rendition names itself with a string, because the game
cannot enumerate what it has never met, and carries its own colour budget in
its manifest.

## Decoding

Assets should not be restricted to one bitmap flavour. `SharpKind.Graphics`
gains an `ImageReader.Read` that sniffs magic bytes and dispatches to a
per-format decoder.

* **BMP** — the existing `BitmapReader` becomes a real decoder rather
  than a reader of one specific export. Today it hard-rejects anything
  that is not 32bpp and assumes pixel data begins at a hardcoded offset
  of 150 bytes (`54 + 96`), which happens to be true of every file
  currently committed. It must instead read the header's real data
  offset and handle 1/4/8/24/32bpp, 4-byte row padding, and top-down
  (negative height) images.
* **PNG** — hand-rolled on `System.IO.Compression.ZLibStream`: inflate,
  unfilter, then colour-type handling. No new dependency, and it stays
  pure managed so the Software backend and the headless tests keep
  working (per the architecture principle preferring framework
  intrinsics over third-party libraries). Non-interlaced only.

`BitmapWriter` stays BMP-only; nothing needs to write PNG.

## Colour budgets

**Amended on 2026-08-03: a rendition declares these rather than the game
holding them.** The two Elite ships still declare what the table says, but the
numbers now live in each one's manifest, because the game cannot know them
about a rendition it was never built against.

| Rendition | Cap |
| --- | --- |
| EightBit | 16 |
| SixteenBit | 4096 |

The cap is a count of distinct values only; which values those may be is
the separate channel-depth rule below.

The cap applies to the **union across one rendition's whole asset set**, not
per image: a rendition standing in for a machine stands in for one palette.

Fully transparent pixels are excluded from the count. Alpha is enforced
to be either 0 or 255 on both tiers, matching the renderer, which
already treats transparency as binary.

## The palette as the whole colour set (2026-07-30)

On some tiers the cap is not the only palette constraint: a bitmap may
only use colours the palette names.

| Tier | Palette names every colour |
| --- | --- |
| EightBit | yes |
| SixteenBit | no |

The split follows the hardware each tier stands in for. 8-bit machines
were **indexed-colour** — a pixel *was* a palette entry, and one palette
served the whole display — so a bitmap colour the palette does not name
is a colour the machine could not have shown. 16-bit hardware is
direct-colour, where `palette.json` is only a set of names the geometry
draws with (`usemtl` in the models, `Palette["Gold"]` in the views) and
bitmaps are independent of it.

This is a **subset** test, not equality: the palette may name colours no
bitmap uses, which it must, since most named colours only ever reach the
screen as filled geometry rather than as pixels in a file. The check
excludes fully transparent pixels, for the same reason the cap does —
they carry no colour.

Applying it to 16-bit would fail immediately and for no good reason:
Elite's 16-bit set holds 134 distinct colours against a 29-entry
palette, so most of them are "unnamed" by construction.

`AssetColourBudget.OutsidePalette` records the offending colours per
asset on **every** tier so they can be logged, but only
`PaletteNamesEveryColour` tiers fail startup over them.

## Channel depth (2026-08-01)

The third rule, and the only one that says which colours a tier may use
rather than how many or which are named:

| Tier | Bits per channel | Levels per channel |
| --- | --- | --- |
| EightBit | 8 | 256 |
| SixteenBit | 4 | 16 |

The 16-bit machines the tier stands in for drove a **12-bit DAC** — four
bits each of red, green and blue, 4096 colours — so a channel there may
only hold one of sixteen levels. An n-bit channel widens to eight by
**replication**: `0xE` becomes `0xEE`. Not by a left shift, which gives
`0xE0` and tops out short of white.

The 8-bit tier is limited by its 16-entry palette instead, not by channel
depth, so it keeps all eight bits and the rule is a no-op there.

This is independent of the palette rule above. 16-bit is still
direct-colour, so a bitmap need not use a colour the palette *names* — it
just has to use one the tier could *produce*. Both the palette entries and
every opaque bitmap pixel are checked.

`AssetColourBudget.ChannelBits` gives the depth, `IsOnGrid` tests one
colour and `NearestLevel` snaps one channel; `AssetColourBudget.OffGrid`
records the strays per asset and `AssetSet.Load` throws with the asset and
the colour named. Alpha is not a DAC channel and is not checked here — it
is covered by `PartialAlphaCount`.

### Baseline, and how the assets were brought inside it (2026-07-28)

Counted over every committed `.bmp`, treating alpha 0 as transparent,
the sets started out at:

| Game | Distinct opaque colours | vs 4096 |
| --- | --- | --- |
| Elite | 2481 | passed |
| Stunt Car Racer | 5095 | **failed** |

The cause was anti-aliasing in two files: `font2.bmp` alone contributed
2431 colours (Elite's entire set was only 2481, so that one font *was*
Elite's budget), and SCR added `atlas.bmp`'s 2676 on top with almost no
overlap. `atlas.bmp` also held all 2765 of the partial-alpha pixels.

A bitmap font with 2431 distinct colours is not a 16-bit-era asset by
any reading — those were effectively modern-tier assets occupying the
16-bit slot, so the fix was to posterise them, not to soften the rule.
Both files were quantised to the **12-bit RGB space real 16-bit hardware
used** (keep each channel's high nibble and replicate it, so 0xFF stays
0xFF and 0x00 stays 0x00), and `atlas.bmp`'s alpha snapped to 0 or 255
at a threshold of 128. That choice follows from the tier's own premise
rather than from picking a number that merely fits, and it caps what any
one 16-bit asset can contribute at 4096 by construction.

| Game | After quantising | Partial alpha |
| --- | --- | --- |
| Elite | 145 | 0 |
| Stunt Car Racer | 349 | 0 |

Only `font2.bmp` and `atlas.bmp` were touched; every other asset was
already well inside the budget and unchanged by quantisation. Because
the rest of the set is not quantised, the union staying under 4096 is
not structurally guaranteed — but at 145 and 349 the headroom is large.

Note this quantisation applies to *authoring* the 16-bit assets. The
validator itself still only counts distinct values, exactly as decided;
it does not require colours to sit in the 12-bit space.

## Eager loading and validation

Assets are loaded and validated up front, never on demand.

Images are already loaded eagerly, but in two separate places —
`SoftwareGraphics` decodes via `BitmapReader`, while `SDLGraphics`
decodes via SDL. The SDL path therefore bypasses the managed reader
entirely, and would bypass any validation added to it.

Both are replaced by a single eager `AssetSet` load that decodes every
image and bitmap font for the active tier, validates them, and hands
both graphics backends the same `FastBitmap` instances. One code path,
one place the tier rule is enforced.

`AssetSet` lives in `SharpKind.Graphics`, not `SharpKind.Assets` as first
sketched: it holds `FastBitmap`s, and `SharpKind.Graphics` already depends
on `SharpKind.Assets`, so putting it the other way round would invert that
reference. `IAssetLocator` gained a `Tier` property, since the validator
has to know which cap applies.

The bitmap fonts are decoded even for the SDL backend, which draws text
with TrueType fonts and never uses them: they are part of the tier's
set, so they count against its budget, and validation has to give the
same answer whichever backend is running.

Validation accumulates the set of distinct opaque ARGB values across the
whole tier's assets and fails startup if the count exceeds the tier's
cap, or if any pixel's alpha is neither 0 nor 255. A per-asset breakdown
is logged at Information first, so the failure names the files to look
at.

## Build

Both game `.csproj` files enumerate their assets as roughly 120
individual `<None Update>` entries. Adding a second tier makes that
untenable, so the lists are replaced by a glob before any assets move:

```xml
<None Update="Assets\**\*" CopyToOutputDirectory="PreserveNewest" />
<None Update="Assets\SFX\*.wav;Assets\Music\*.ogg"
      CopyToOutputDirectory="Never" />
```

The second line preserves the current intent of keeping the
uncompressed sources in source control but out of the build output.

## Sequencing

Each step is independently verifiable, and steps 1–5 do not depend on
the 8-bit art existing.

**This plan is done, and is kept as the record of how it was built.** Step 3's
`SystemTier` and tier resolution were later undone when tiers became
renditions - see the notes above.

1. Glob the asset lists in both game `.csproj` files → verify: build
   both games, asset output directory is unchanged.
2. `ImageReader` with the reworked BMP decoder and the new PNG decoder
   → verify: unit tests per bit depth, padding case and top-down case;
   existing bitmaps still load identically.
3. Move the existing images, bitmap fonts and palette into
   `SixteenBit/`; add `SystemTier` and tier resolution to
   `AssetLocator`, defaulting to `SixteenBit` → verify: no behaviour
   change, both games smoke-test identically.
4. Eager `AssetSet` replacing the two backend-specific loads, with the
   colour validator warn-only → verify: both backends render
   identically; the warning reports SCR at 5095.
5. Posterise `font2.bmp` and `atlas.bmp` within budget; flip the
   validator to hard-fail → verify: both games start; visual
   smoke-test for acceptable font and cockpit quality.
6. Add the 8-bit bitmaps under `EightBit/` and declare the tier →
   verify: the tier loads and passes the 16-colour cap.

None of these steps require the resolution-tier rendering work in
[backlog-roadmap.md](backlog-roadmap.md); selecting a tier from
configuration meets that work at step 6.
