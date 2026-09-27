# Elite: add the Modern rendition

[EliteSharp.Renditions.Modern] Scoped with the maintainer 2026-09-12. A third
rendition standing for no machine: **1280x720, one window scale, no colour
restriction**, scanner centred at the bottom and the universe filling every
side of it. The HUD art is a placeholder for 3D instrumentation — don't invest
in it.

Settled decisions (do not reopen):

- **1280x720, not 1080p** — a 1080p render exceeds usable desktop on a 1080p
  display. Native-resolution resize/fullscreen is
  [sdl-resizable-fullscreen.md](sdl-resizable-fullscreen.md).
- **`DesignScale` 2**, as 16-bit. It is a property of the artwork, must be an
  `int`, and 3 needs 768 of 720 rows.
- **32-bit colour.** `AssetColourLimits` defaults to `ChannelBits = 8` and
  `ChannelGridQuantiser` passes 8 bits through untouched — manifest only.

**The work.** A new plugin assembly and `Assets` folder alongside the two shipped
ones: 1280x720, `DesignScale` 2, `WindowScales => [1]`, `ShadesShips`, window
region = whole screen. Assets are a self-contained copy of the 16-bit tree, with
the manifest's `Colours` widened to 32-bit. Screens derive from the 16-bit set
and centre against `ViewportCentre`. The 640-wide scanner centres at x=320 of
1280. Wire it into
[EliteSharp.csproj](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/apps/EliteSharp/EliteSharp.csproj)'s
`RenditionProject`/`RenditionAsset` lists; it then appears on the settings
screen with no settings code change. Smoke-test — this is a game loop.

**Focal length is not the region's.** `Focus` derives from `ScreenHeight`, not
the viewport (decided 2026-07-29). Leave it alone. Modern is the first rendition
whose window region is not the viewport.
