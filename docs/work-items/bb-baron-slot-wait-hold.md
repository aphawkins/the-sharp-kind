# Bubble Bobble: the Barons' call waits frame by frame

[BubbleBobbleSharp] Left out of step **4** of section 5 of [bb-port-plan.md](../bb-port-plan.md).

`LevelEnd.SpawnBarons` (`$1621`, `Levels/LevelEnd.cs`) pops the players' object slots and calls `SlotPop.Wait()`, which runs the
object updates all at once and throws away the frames it returns: the clock does not run through them and the pops are not seen.
The giant bubble's start (`GiantBubble`, `$1AE8`-`$1B18`) now runs the same wait as an `IGameHold`, an update a frame
(`SlotPop.Update`, `SlotPop.Free`); do the same for the Barons, with the pass going on once the slots are free. Check `$1621`
for a flash or other wait of its own.
Verify: `GameHoldTests`-style test in the loop, and the existing Baron golden tests still pass.
