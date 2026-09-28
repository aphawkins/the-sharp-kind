# Bubble Bobble: the freeze

[BubbleBobbleSharp] Step **4b** of section 5 of [bb-port-plan.md](../bb-port-plan.md). The plan holds the
detail — addresses, what is already done, and how the step is proved. Read it
there and update it as the step lands.

The consumer of `$67` (`SESSION`); the setter (`$2F74`, time-stop) is an item
effect, which is step 6.

- `$15C9`-`$15E0` (the second half of `$1578`, after `LevelItems.Update`): `$67`
  above zero counts down; at zero the two background colours EOR 7 and `$2B`
  reloads `$32`.
- `$1CF5`-`$1D03` (`PlayerFrame.Slot`, for slots 2 onwards): while `$67` is set,
  an enemy in a state below `$0B` skips its turn.

Verify: a VICE capture with `$67` written (an intervention, since nothing
translated sets it): the colour toggle at the count's end, and which slots skip.
