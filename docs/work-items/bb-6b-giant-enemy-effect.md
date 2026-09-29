# Bubble Bobble: item effect 29, the six-sprite giant

[BubbleBobbleSharp] Part of step **6** of section 5 of [bb-port-plan.md](../bb-port-plan.md). Effect 29 (`$35B0`) is still `Untranslated`.
Wants a plan and sign-off before code (AGENTS.md).

Six enemy slots (2-7) become state `$14`: a giant made of the six sprites in a 3 by 2 block (X `$78`, `$90`, `$A8`; Y `$7C`, `$91`),
which walks about (X `$54`, `$84`, `$B4`; Y `$67`), and at frame 120 the six are three items (state `$0B`). `$4A` (the enemy count)
goes to 1 and then 0. `D_859B..` set Y `$F9` down as it comes in. The rebb64 comments and labels here are wrong.
Verify: a capture of the effect.
