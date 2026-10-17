# Bubble Bobble: extra lives from the score (`$F1AC`)

`$F1AC`, called at `$0A10`, awards extra lives from the score. It is
untranslated and on no step of [bb-port-plan.md](../bb-port-plan.md) (noted under step 3). `PlayerJoin`
already writes its state. Translate it into `GameLoop`, prove against a VICE
capture of a life being awarded.
