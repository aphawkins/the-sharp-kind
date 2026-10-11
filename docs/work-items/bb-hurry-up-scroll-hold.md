# Bubble Bobble: the HURRY UP! scroll and its pause

[BubbleBobbleSharp] Left out of step **4e** of section 5 of [bb-port-plan.md](../bb-port-plan.md). Wants a plan and sign-off
before code (AGENTS.md).

`LevelEnd.HurryUp` (`$15E8`-`$15F4`, `Levels/LevelEnd.cs`) makes the enemies angry and resets the clock, and the hurry-up song is
queued, but the HURRY UP! text's scroll and the pause the game makes for it are not modelled: on the C64 the game waits in the
routine while the IRQ runs the clock on. Run it as an `IGameHold` (`Levels/IGameHold.cs`), paused or not as `$37` is, with the
scroll drawn by the 8-bit rendition.
Verify: a capture of the scroll's frames and the clock from the real game (`Rebb64Machine`, or VICE for the picture).
