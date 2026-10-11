# Bubble Bobble: the super bonus room's sprites

[BubbleBobbleSharp] Left out of item effect 32 (`SuperBonusRoom`, `$3621`), part of step **6** of section 5 of
[bb-port-plan.md](../bb-port-plan.md).

`$36FF`-`$370A` write `$74`, `$35` and `$06` to `$BC`, `$BD` and `$C0`, and `$370C`-`$371D` clear `$BE`, set `$5AFF` and `$2B`
to `$FF`, then call `$2BBD` with `$29` and `$2C32` with `$22`. These look like the room's sprites: entity slot 2's X, Y and
state, and something drawn from them. Not looked into; the port draws nothing for them.

Fix direction: read `$2BBD` and `$2C32` with `tools/bb/dis6502.py`, find out what the room shows from them, and port it.
Verify: the entity table and sprite registers against the game at `$31A2` (`SuperBonusRoomOracleTests.Enter`), and a screenshot.
