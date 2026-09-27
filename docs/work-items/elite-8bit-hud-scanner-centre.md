# Elite 8-bit: halve the HUD art and centre the scanner

**Bug.** Reported by the maintainer, 2026-09-27. **Blocked by the maintainer**:
they will halve the 8-bit `hud.bmp` horizontally
([Assets/Images/hud.bmp](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharp.Renditions.EightBit/Assets/Images/hud.bmp)).
Ask for the new art before starting.

Then realign the scanner to the horizontal centre: `ScannerView8Bit.ScannerCentre`
([ScannerView8Bit.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharp.Renditions.EightBit/ScannerView8Bit.cs))
is `ViewportCentre.X - 2`. Check the compass (`CompassCentre`, hard-coded
`(257, 10)`) and the other HUD indicators drawn over the art (missiles, ECM,
lasers) still line up with the new bitmap, and update the 8-bit golden frames.
