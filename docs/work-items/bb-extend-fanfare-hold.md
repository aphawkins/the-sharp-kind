# Bubble Bobble: the EXTEND fanfare and screen of fruit

[BubbleBobbleSharp] Left out of step **5d** of section 5 of [bb-port-plan.md](../bb-port-plan.md). Wants a plan and sign-off
before code (AGENTS.md).

`ExtendBonus` (`$32C1`-`$3420`, `Levels/ExtendBonus.cs`) gives the life, clears the letters and ends the level, but the game also
stops for the fanfare (song 7, already queued) and a screen of fruit: about five seconds of frames in which only the IRQ runs. None
of it is modelled, so the level clock and the hurry-up do not run on through it as they do on the C64, and nothing is drawn.
Run it as an `IGameHold` (`Levels/IGameHold.cs`, as `SpriteShow` and `ScreenWipe` do): the waits frame by frame with the clock,
and a view of the screen of fruit (`$3420`'s drawing; the reference's comments here are not to be trusted).
Verify: a capture of the frame count and the screen from the real game (`Rebb64Machine`, or VICE for the picture).
