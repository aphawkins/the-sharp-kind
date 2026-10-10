# Bubble Bobble: popped water does not flow along the platforms correctly

[BubbleBobbleSharp] **Bug**, found in play testing. When a water bubble is popped the water does not run across the
platforms the way it should: it looks too low, as if it travels a row beneath where it should be.

Where to look: `src/bb/libs/BubbleBobbleSharpLib/Specials/WaterFlow.cs` (the reference's water, `$1B40` on, which
writes screen code `$1F` with colour RAM `$0B`) and how `EffectsView8Bit` places the water cells
(`EffectsModel.WaterRow`, `_layout.Playfield.Cell`). Work out whether the simulation's rows are off by one, or whether
the view draws a correct row a row too low.

Verify: capture a water pop in VICE (the `vice-drive` skill) and compare the water's cells a frame at a time.
