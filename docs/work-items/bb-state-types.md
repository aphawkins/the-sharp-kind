# Bubble Bobble: enums and behaviour objects for entity state and type

[BubbleBobbleSharpLib] State and type are bytes (`0x02`-`0x0B`, `0x24`-`0x4C`)
dispatched by `switch` in `EnemyFrame`, `ObjectAi`, and by `Handles(type)` calls
in `EnemyAiLoop`, `ItemEffects` and `WorldEffects`. The 6502 used a jump table.

Fix direction: enums with real names (the "class one/five/six" states first),
then one behaviour object per kind so the dispatch lives in one place.
