# Bubble Bobble: level complete and the next level

[BubbleBobbleSharp] Step **4d** of section 5 of [bb-port-plan.md](../bb-port-plan.md). The plan holds the
detail — addresses, what is already done, and how the step is proved. Read it
there and update it as the step lands.

From the last pop to the next level's first pass.

- `$15FB`-`$1620`: with `$2D` negative and `$2A` at zero, each player in the game
  must be in state 1 or the state `$5A7F` holds (`$10`, the respawn flash), which
  goes to 1 (`$A813` to `$8728`, `$8570` to `$8548`); a player in any other state
  (dying, dead) stops it for now. Then `$21` is set, and `$0A6A` sees it.
- `$0A6E`-`$0A96`: `$2E79` (entity reset, and `$2E61`/`$1A6F`), SUBFLG + 1,
  `$0409,x` for each player in the game, `$09A5` with the carry set. Level 100
  goes to `$A5B7` (step 7).
- `$09A5`-`$09DC`: the round intro (`$37C9`, `setup_level_screen`, `$7B53`) and the
  level start `GameLoop.Start` already has. The intro is drawing (ROUND n, READY!!).

Blocked by 4c. Verify: a scripted playthrough of level 1 to level 2 on the port,
matched against VICE for the scores and state carried over (SUBFLG, lives,
score, `$0409`).
