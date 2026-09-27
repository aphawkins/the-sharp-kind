# Stunt Car Racer: split the sprite atlas into one image per sprite

[StuntCarRacerSharpLib] `Assets/Images/atlas.bmp` is 1024x1024 at 32bpp — a 4 MB
binary from ptitSeb's remake — addressed by fifteen hard-coded `AtlasRect`
constants in `Rendering/HudRenderer.cs` (`s_hole`, `s_cockpitTop`,
`s_engineFlames0`, ...). Those are a second source of truth: move a sprite and
nothing fails until it renders wrong.

Packing earns nothing: `SDLGraphics` issues one `SDL_RenderTexture` per
`DrawImagePart`, and `SoftwareGraphics` blits per pixel by name, so neither
backend batches. Splitting makes each sprite a named manifest entry, makes the
art diffable, and lets the colour-budget check report per sprite. Same reasoning
removed the packed atlas from Bubble Bobble's Phase 2
([bb-port-plan.md](../bb-port-plan.md)).

Settle before [scr-road-line-textures.md](scr-road-line-textures.md), which
assumes `atlas.bmp` exists.
