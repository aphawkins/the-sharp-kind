# Bubble Bobble: golden sprite frames against VICE

Step 2 of [bb-port-plan.md](../bb-port-plan.md) (Draw what moves) has every sub-step done but not its Verify:
golden frames compared pixel for pixel with VICE screenshots (bytes at
`$0A4B`, picture at the `$E90E` after next, raster line 50 as row 0) for a
bubble in each shift, each pop frame, a caught enemy, each enemy class and the
food. Only the food is captured (`Views/Captures/items-level-1.txt`); the
special item and the `$18` flash are not. Capture with the `vice-drive`
skill, add golden tests, then tick step 2.
