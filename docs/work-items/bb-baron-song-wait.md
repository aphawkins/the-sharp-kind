# Bubble Bobble: the Barons' call waits for its tune to end

[BubbleBobbleSharp] Left out of step **4** of section 5 of [bb-port-plan.md](../bb-port-plan.md).

After the Barons are placed, `$1621` (`game_init_sequence`, `rebb64/src/bubbles-sprites.s`) spins at `$1685` until `$A4` (the
song playing) is 0: the round start's tune (song `$43`), asked for at the routine's start, has to end before the level
complete tune (`$27`) is asked for and the clock is set. `SOUND_SONGS_LOOP` is 0 in the real game, so the wait is there.
The game is paused (`$37`) throughout, while the IRQ goes on.

`LevelEnd`'s `BaronHold` (`Levels/LevelEnd.cs`) models the two `$1B02` waits but not this one, so the Barons come and the
pass goes on as soon as the slots are free. The port does not know how long a song lasts: that has to come from the sound
driver's data (`rebb64/src/sound.s`), not a guess. Add the tune's length in frames and a third phase to the hold that waits
it out before `Finish`. Check what `$05AD` with `$27` does after the wait.
Verify: a `GameHoldTests` test that the hold lasts the tune's frames after the second slot wait, checked against a VICE or
oracle trace of `$A4` from the Barons' call.
