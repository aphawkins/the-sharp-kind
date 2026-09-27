# Elite 8-bit: move the hyperspace countdown two columns right

**Bug.** Reported by the maintainer, 2026-09-27. On the 8-bit rendition the
hyperspace countdown sits two character columns too far left.

`BaseView8Bit.DrawHyperspaceCountdown`
([BaseView8Bit.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharp.Renditions.EightBit/BaseView8Bit.cs))
draws right-aligned at `new(Column(2), Row(ChromeRow))`, so the fix is most
likely `Column(4)` — check it against the original's placement, and update the
8-bit golden frames that show a countdown.
